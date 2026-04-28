#!/usr/bin/env python3
"""
WIM — Windrose Inventory Manager (runs with dotnet, or bundled exe after building the installer).

  python run_wim.py                   → dotnet run Assets/decompiled/WIM.csproj (Release).
  python run_wim.py --pick-profile    → Tk: scan LOCALAPPDATA\\\\R5\\\\Saved\\\\SaveProfiles … then launch WIM.

  Standalone launcher (single .exe on the gaming PC, no dotnet):
    powershell -File Python_Runner/build_installer.ps1
    Output: Python_Runner/dist/WindroseWIM.exe — embeds a self-contained
    Assets/bundled/WIM - ….exe (+ item DB embedded in that exe).

Layout
------
  Python_Runner/
    run_wim.py
    Assets/
      decompiled/       (WIM.csproj + sources; dev dotnet run)
      bundled/           (populate via build_installer.ps1 dotnet publish → single-file WIM)
      NuGet.Build.config
      windrose_items.db  (needed to publish/embed item database)
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path


def runner_root() -> Path:
    """Project / bundle root. PyInstaller --onefile unpacks data under sys._MEIPASS."""
    if getattr(sys, "frozen", False):
        meipass = getattr(sys, "_MEIPASS", None)
        if meipass:
            return Path(meipass)
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent


def _dotnet_subprocess_kwargs() -> dict:
    """Hide the dotnet host console on Windows (no flashing cmd window)."""
    if sys.platform != "win32":
        return {}
    flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
    return {"creationflags": flags} if flags else {}


def assets_dir() -> Path:
    return runner_root() / "Assets"


def project_file() -> Path:
    return assets_dir() / "decompiled" / "WIM.csproj"


_WIM_PUBLISHED_EXE = "WIM - Windrose Inventory Manager.exe"


def bundled_wim_executable() -> Path | None:
    """Self-contained publish next to Assets (frozen) or Python_Runner/Assets/bundled (dev)."""
    p = assets_dir() / "bundled" / _WIM_PUBLISHED_EXE
    if p.is_file():
        return p
    return None


_GUID_RE = re.compile(r"^[0-9a-fA-F]{32}$")


def _read_scan_chunk(path: Path, limit: int) -> bytes | None:
    try:
        with path.open("rb") as f:
            return f.read(limit)
    except OSError:
        return None


def try_read_player_name(profile_dir: Path, max_bytes: int = 8 * 1024 * 1024) -> str | None:
    if not profile_dir.is_dir():
        return None
    needles = (b"\x02PlayerName\x00",)

    def extract_from(blob: bytes) -> str | None:
        for needle in needles:
            start = 0
            while True:
                idx = blob.find(needle, start)
                if idx < 0:
                    break
                off = idx + len(needle)
                if off + 4 > len(blob):
                    start = idx + 1
                    continue
                slen = int.from_bytes(blob[off : off + 4], "little")
                if slen <= 1 or slen > 8192:
                    start = idx + 1
                    continue
                end = off + 4 + slen
                if end > len(blob):
                    start = idx + 1
                    continue
                payload = blob[off + 4 : end]
                if not payload or payload[-1] != 0:
                    start = idx + 1
                    continue
                raw_s = payload[:-1]
                try:
                    name = raw_s.decode("utf-8").strip()
                except UnicodeDecodeError:
                    start = idx + 1
                    continue
                if name:
                    return " ".join(name.split())
                start = idx + 1
        return None

    scored: list[tuple[int, Path]] = []
    try:
        for p in profile_dir.iterdir():
            if not p.is_file():
                continue
            try:
                st = p.stat()
            except OSError:
                continue
            name_low = p.name.lower()
            prio = 0 if name_low.endswith(".log") else 1 if name_low == "current" else 2
            if name_low.endswith(".sst"):
                prio = 12
            if st.st_size > max_bytes:
                prio += 50
            scored.append((prio * 10_000_000_000 + min(st.st_size, max_bytes), p))
    except OSError:
        return None

    scored.sort(key=lambda x: x[0])
    for _pr, fp in scored:
        try:
            sz = fp.stat().st_size
        except OSError:
            continue
        if sz > max_bytes:
            continue
        blob = _read_scan_chunk(fp, sz)
        if not blob:
            continue
        got = extract_from(blob)
        if got:
            return got

    return None


def _steam_short(s: str, head: int = 9, tail: int = 6) -> str:
    if len(s) <= head + tail + 2:
        return s
    return f"{s[:head]}…{s[-tail:]}"


def discover_r5_windrose_profiles() -> list[tuple[Path, str, str | None]]:
    """SaveProfiles excluding *_backups, …\\RocksDB\\version\\Players\\profiles."""
    la = os.environ.get("LOCALAPPDATA")
    if not la:
        return []

    base = Path(la) / "R5" / "Saved" / "SaveProfiles"
    if not base.is_dir():
        return []

    roots: list[tuple[Path, str]] = []
    try:
        for steam_dir in sorted(base.iterdir()):
            if not steam_dir.is_dir():
                continue
            if "_backups" in steam_dir.name.lower():
                continue
            rocks = steam_dir / "RocksDB"
            if not rocks.is_dir():
                continue
            for ver_dir in sorted(rocks.iterdir()):
                if not ver_dir.is_dir():
                    continue
                players = ver_dir / "Players"
                if not players.is_dir():
                    continue
                for profile in sorted(players.iterdir()):
                    if not profile.is_dir():
                        continue
                    cur = profile / "CURRENT"
                    try:
                        if not cur.is_file():
                            continue
                    except OSError:
                        continue
                    roots.append((profile.resolve(), steam_dir.name))
    except OSError:
        pass

    out: list[tuple[Path, str, str | None]] = []
    seen: set[str] = set()
    for prof, steam_label in roots:
        key = str(prof).lower()
        if key in seen:
            continue
        seen.add(key)
        pname = try_read_player_name(prof)
        out.append((prof, steam_label, pname))
    return out


def build_dropdown_labels(entries: list[tuple[Path, str, str | None]]) -> tuple[list[str], list[Path]]:
    paths: list[Path] = []
    labels: list[str] = []
    seen: dict[str, int] = {}

    for prof, steam, cname in entries:
        short_steam = _steam_short(steam)
        if cname:
            base = f"{cname}  ·  {short_steam}"
        elif _GUID_RE.match(prof.name):
            base = f"({prof.name[:8]}…)  ·  {short_steam}"
        else:
            base = f"{prof.name}  ·  {short_steam}"

        n = seen.get(base, 0)
        seen[base] = n + 1
        label = base if n == 0 else f"{base}  ({n + 1})"
        paths.append(prof)
        labels.append(label)

    return labels, paths


def build_dotnet_cmd(dotnet: str, configuration: str, save_path: str | None) -> list[str]:
    proj = project_file()
    cmd = [
        dotnet,
        "run",
        "--project",
        str(proj),
        "-c",
        configuration,
        "--nologo",
    ]
    if save_path:
        cmd.extend(["--", save_path])
    return cmd


def build_wim_launch_cmd(configuration: str, save_path: str | None) -> list[str] | None:
    """Launch bundled self-contained exe if present (PyInstaller standalone); else dotnet run."""
    exe = bundled_wim_executable()
    if exe is not None:
        out = [str(exe.resolve())]
        if save_path:
            out.append(save_path)
        return out
    dotnet = shutil.which("dotnet")
    if not dotnet:
        return None
    return build_dotnet_cmd(dotnet, configuration, save_path)


def ensure_project_or_exit() -> int:
    """For dev dotnet run path: need csproj + db. Frozen installer ships bundled exe only."""
    if bundled_wim_executable() is not None:
        return 0
    proj = project_file()
    if not proj.is_file():
        print(
            f"ERROR: Missing {proj}\n"
            "Sync/copy Assets/decompiled/ (WIM.csproj + sources) into Python_Runner, "
            "or run Python_Runner/build_installer.ps1 to embed WIM.exe.",
            file=sys.stderr,
        )
        return 1
    db = assets_dir() / "windrose_items.db"
    if not db.is_file():
        print(f"NOTE: {db} missing — copy windrose_items.db into Assets/.", file=sys.stderr)
    return 0


def run_pick_profile_gui(configuration: str) -> int:
    if sys.platform != "win32":
        print("--pick-profile is only supported on Windows.", file=sys.stderr)
        return 1
    try:
        import tkinter as tk
        from tkinter import filedialog, messagebox, ttk
    except ImportError as e:
        print(f"Tkinter unavailable: {e}", file=sys.stderr)
        return 1

    if ensure_project_or_exit() != 0:
        return 1
    root = runner_root()
    use_bundle = bundled_wim_executable() is not None
    if not use_bundle and not shutil.which("dotnet"):
        messagebox.showerror(
            "dotnet not found",
            "dotnet.exe is not on PATH.\nInstall the .NET 8 Desktop Runtime or SDK,\n"
            "or reinstall from a WindroseWIM installer build that bundles WIM.",
        )
        return 1

    cfg = {"configuration": configuration}

    class Tooltip:
        """Small hover label for tk widgets."""

        def __init__(self, widget: "tk.Widget", message: str) -> None:
            self._widget = widget
            self._tip: tk.Toplevel | None = None
            self._message = message
            widget.bind("<Enter>", self._show)
            widget.bind("<Leave>", self._hide)

        def _hide(self, _e: tk.Event | None = None) -> None:
            if self._tip is not None:
                self._tip.destroy()
                self._tip = None

        def _show(self, _e: tk.Event | None = None) -> None:
            self._hide()
            tip = tk.Toplevel(self._widget)
            self._tip = tip
            tip.wm_overrideredirect(True)
            tip.wm_geometry(
                f"+{self._widget.winfo_rootx() + 24}+{self._widget.winfo_rooty() + 28}"
            )
            tk.Label(
                tip,
                text=self._message,
                background="#ffffe0",
                foreground="#222",
                relief=tk.SOLID,
                borderwidth=1,
            ).pack()

    class PickerApp(tk.Tk):
        def __init__(self) -> None:
            super().__init__()
            self.title("WIM — Windrose Inventory Manager")
            self.geometry("760x460")
            self.minsize(560, 320)

            self._profile_paths: list[Path] = []
            self._manual_path: str | None = None

            frm = ttk.Frame(self, padding=10)
            frm.pack(fill=tk.BOTH, expand=True)

            ttk.Label(
                frm,
                text=(
                    "Choose profile, Sync to rescan or browse manually."
                ),
                wraplength=720,
            ).pack(anchor=tk.W)

            prof_row = ttk.Frame(frm)
            prof_row.pack(fill=tk.X, pady=(10, 4))
            ttk.Label(prof_row, text="Profile:").pack(side=tk.LEFT, padx=(0, 8))
            self._combo_var = tk.StringVar()
            self._combo = ttk.Combobox(
                prof_row,
                textvariable=self._combo_var,
                values=[],
                state="readonly",
                width=68,
            )
            self._combo.pack(side=tk.LEFT, fill=tk.X, expand=True)

            brow = ttk.Frame(frm)
            brow.pack(fill=tk.X, pady=(14, 4))

            btn_sync = ttk.Button(brow, text="\u21bb", width=4, command=self._scan)
            btn_sync.pack(side=tk.LEFT)
            Tooltip(
                btn_sync,
                "Scan Windrose saves under R5\\Saved\\SaveProfiles …\\RocksDB\\…\\Players",
            )

            ttk.Button(brow, text="Browse folder…", command=self._browse).pack(side=tk.LEFT, padx=(8, 0))

            ttk.Button(brow, text="Launch WIM", command=self._launch).pack(side=tk.RIGHT)

            self._status = ttk.Label(frm, text="", foreground="#444", wraplength=720, justify=tk.LEFT)
            self._status.pack(anchor=tk.W, fill=tk.X, pady=(8, 0))

            self._combo.bind("<<ComboboxSelected>>", lambda _e: setattr(self, "_manual_path", None))

            self._scan()

        def _scan(self) -> None:
            self._manual_path = None
            self._status.config(text="Scanning Windrose SaveProfiles …")
            self.update_idletasks()
            rows = discover_r5_windrose_profiles()
            labels, paths = build_dropdown_labels(rows)
            self._profile_paths = paths
            self._combo.configure(values=labels)
            self._combo_var.set("")
            if labels:
                self._combo.current(0)
                self._status.config(text=f"Found {len(labels)} profile(s). Launch WIM when ready.")
            else:
                self._status.config(
                    text=(
                        'No Windrose profiles under LOCALAPPDATA\\R5 … Use "Browse folder…" for a '
                        "folder containing CURRENT."
                    ),
                )

        def _browse(self) -> None:
            path = filedialog.askdirectory(title="Select player folder (contains CURRENT)")
            if not path:
                return
            p = Path(path)
            if not (p / "CURRENT").is_file():
                messagebox.showerror(
                    "Not a player save folder",
                    'That folder has no CURRENT file.\nUsually …\\\\RocksDB\\\\<version>\\\\Players\\\\<hex>.',
                )
                return
            self._manual_path = str(p.resolve())
            self._combo_var.set("")
            self._status.config(text=f"Manual folder: {_steam_short(str(p.resolve()), 56, 20)}")

        def _selected_dir(self) -> str | None:
            if self._manual_path:
                return self._manual_path
            idx = self._combo.current()
            if isinstance(idx, int) and 0 <= idx < len(self._profile_paths):
                return str(self._profile_paths[idx].resolve())
            sel = self._combo_var.get()
            try:
                vals = list(self._combo["values"])
                i = vals.index(sel)
                if 0 <= i < len(self._profile_paths):
                    return str(self._profile_paths[i].resolve())
            except ValueError:
                pass
            return None

        def _launch(self) -> None:
            save_dir = self._selected_dir()
            if not save_dir:
                messagebox.showwarning(
                    "Nothing selected",
                    "Choose a profile or use Browse folder….",
                )
                return
            cmd = build_wim_launch_cmd(cfg["configuration"], save_dir)
            if not cmd:
                messagebox.showerror(
                    "Cannot launch WIM",
                    "No bundled Windrose exe and dotnet is not available.",
                )
                return
            cwd_launch = root
            bexe = bundled_wim_executable()
            if bexe is not None:
                cwd_launch = bexe.parent.resolve()
            sp_kw = _dotnet_subprocess_kwargs()
            try:
                subprocess.Popen(cmd, cwd=str(cwd_launch), **sp_kw)
            except OSError as e:
                messagebox.showerror("Launch failed", str(e))
                return
            self.destroy()

    app = PickerApp()
    app.mainloop()
    return 0


def parse_args(argv: list[str] | None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run WIM via dotnet using Python_Runner Assets.")
    parser.add_argument("--restore", action="store_true", help="dotnet restore first.")
    parser.add_argument(
        "--pick-profile",
        action="store_true",
        help="Tk UI: scan R5 saves, browse, then start WIM with the chosen folder.",
    )
    parser.add_argument(
        "-c",
        "--configuration",
        default="Release",
        choices=("Debug", "Release"),
        help="dotnet build configuration (default: Release).",
    )
    parser.add_argument(
        "--save-path",
        default=None,
        metavar="DIR",
        help="Passed to dotnet after `--` — player folder with CURRENT.",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    if argv is None:
        argv = sys.argv[1:]
    # PyInstaller onefile: opening the launcher shows the save-profile picker (no dotnet required).
    if getattr(sys, "frozen", False) and not argv:
        argv = ["--pick-profile"]
    args = parse_args(argv)
    if args.pick_profile:
        return run_pick_profile_gui(args.configuration)

    root = runner_root()
    os.chdir(root)
    sp_kw = _dotnet_subprocess_kwargs()

    be = bundled_wim_executable()
    if be is not None:
        if args.restore:
            print("Skipping --restore (bundled single-file WIM).", file=sys.stderr)
        cmd = build_wim_launch_cmd(args.configuration, args.save_path)
        if not cmd:
            print("ERROR: Could not assemble WIM launch command.", file=sys.stderr)
            return 1
        cwd_launch = be.parent.resolve()
        print("Starting WIM:", " ".join(cmd))
        return subprocess.call(cmd, cwd=str(cwd_launch), **sp_kw)

    proj = project_file()
    if not proj.is_file():
        print(
            f"ERROR: Missing {proj}\n"
            "Copy Python_Runner\\Assets\\decompiled\\ or run Python_Runner\\build_installer.ps1 "
            "to produce Assets\\bundled\\WIM - Windrose Inventory Manager.exe",
            file=sys.stderr,
        )
        return 1

    db = assets_dir() / "windrose_items.db"
    if not db.is_file():
        print(f"NOTE: {db} missing.", file=sys.stderr)

    dotnet = shutil.which("dotnet")
    if not dotnet:
        print("ERROR: dotnet not on PATH.", file=sys.stderr)
        return 1

    if args.restore:
        nuget = assets_dir() / "NuGet.Build.config"
        if nuget.is_file():
            rc = subprocess.run(
                [
                    dotnet,
                    "restore",
                    str(proj),
                    "--configfile",
                    str(nuget),
                    "--nologo",
                ],
                cwd=str(root),
                **sp_kw,
            ).returncode
        else:
            print(f"WARNING: {nuget} missing", file=sys.stderr)
            rc = subprocess.run(
                [dotnet, "restore", str(proj), "--nologo"],
                cwd=str(root),
                **sp_kw,
            ).returncode
        if rc != 0:
            return rc

    cmd = build_dotnet_cmd(dotnet, args.configuration, args.save_path)
    print("Starting WIM:", " ".join(cmd))
    return subprocess.call(cmd, cwd=str(root), **sp_kw)


if __name__ == "__main__":
    raise SystemExit(main())
