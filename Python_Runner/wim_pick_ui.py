#!/usr/bin/env python3
"""Entry point for PyInstaller: only opens the tk profile-picker + dotnet WIM."""

from __future__ import annotations

from run_wim import main


if __name__ == "__main__":
    raise SystemExit(main(["--pick-profile"]))
