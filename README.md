# Windrose Inventory Manager - Nexus Source Bundle

This `GITHUB` folder contains the source and build scripts used to create the uploaded Windows executable(s), so NexusMods reviewers can reproduce builds.

## Included

- `Python_Runner/` - launcher source (`run_wim.py`, picker UI), PyInstaller build script, and embedded WIM source under `Python_Runner/Assets/decompiled/`
- `installer/` - Inno Setup installer script
- `tools/` - Nexus installer/signing build pipeline scripts
- `app.ico` - application icon used during build
- `windrose_items.db` - item database used during publish/embed
- `NEXUS_RELEASE.md` - release pipeline notes
- `CHANGELOG.md` - feature changelog

## Build Steps (Standalone EXE)

1. Install prerequisites:
   - Python 3.10+ (with pip)
   - .NET 8 SDK
   - (Optional for signed binaries) Windows SDK `signtool.exe` + code-sign cert
2. From the repository root, run:

   `powershell -ExecutionPolicy Bypass -File Python_Runner\build_installer.ps1`

3. Output EXE:

   `Compiled\Python\WindroseWIM.exe`

## Build Steps (Nexus Installer)

1. Install Inno Setup 6 (`ISCC.exe`).
2. Optional signing env vars:
   - `SIGN_PFX_PATH` + `SIGN_PFX_PASSWORD`, or
   - `SIGN_CERT_THUMBPRINT`
3. Run:

   `powershell -ExecutionPolicy Bypass -File tools\Build-NexusInstaller.ps1 -Version 1.0.0`

4. Outputs:
   - `Compiled\Installer\WindroseWIM-Setup.exe`
   - `Compiled\Installer\SHA256SUMS.txt`
