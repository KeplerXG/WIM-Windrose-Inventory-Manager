# Windrose Inventory Manager

Source and reproducible build scripts for the Windrose Inventory Manager executable(s).
This repository is intended to satisfy executable-review requirements (for example NexusMods)
by providing the code and exact build steps used to generate release binaries.

## Repository layout

- `Python_Runner/` - launcher source (`run_wim.py`, picker UI), PyInstaller build script, and embedded WIM source under `Python_Runner/Assets/decompiled/`
- `tools/` - release/installer pipeline scripts (`Build-NexusInstaller.ps1`, etc.)
- `installer/` - Inno Setup script used to generate setup packages
- `PACKAGE_CONTENTS.txt` - short inventory of the upload package

## Build standalone EXE

1. Install prerequisites:
   - Python 3.10+ (with `pip`)
   - .NET 8 SDK
2. Run from repository root:

   `powershell -ExecutionPolicy Bypass -File Python_Runner\build_installer.ps1`

3. Output:

   `Compiled\Python\WindroseWIM.exe`

## Build Nexus installer

1. Install Inno Setup 6 (`ISCC.exe`).
2. Optional signing environment variables:
   - `SIGN_PFX_PATH` + `SIGN_PFX_PASSWORD`, or
   - `SIGN_CERT_THUMBPRINT`
   - optional timestamp override: `SIGN_TIMESTAMP_URL`
3. Run:

   `powershell -ExecutionPolicy Bypass -File tools\Build-NexusInstaller.ps1 -Version 1.0.0`

4. Outputs:
   - `Compiled\Installer\WindroseWIM-Setup.exe`
   - `Compiled\Installer\SHA256SUMS.txt`
