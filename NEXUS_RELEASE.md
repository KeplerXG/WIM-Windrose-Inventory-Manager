# NexusMods Release Pipeline

## Prerequisites (build machine)

- .NET 8 SDK
- Python 3.10+
- Inno Setup 6 (`ISCC.exe`)
- Optional for signing: Windows SDK `signtool.exe` and a code-sign certificate

## Build installer

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Build-NexusInstaller.ps1 -Version 1.0.0
```

Outputs:

- `Compiled\Python\WindroseWIM.exe` (launcher)
- `Compiled\Installer\WindroseWIM-Setup.exe` (installer for NexusMods)
- `Compiled\Installer\SHA256SUMS.txt`

## Signing configuration

Set one of these before running the script:

- PFX signing:
  - `SIGN_PFX_PATH`
  - `SIGN_PFX_PASSWORD`
- Certificate store signing:
  - `SIGN_CERT_THUMBPRINT`

Optional:

- `SIGN_TIMESTAMP_URL` (default: `http://timestamp.digicert.com`)

To package without signing:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Build-NexusInstaller.ps1 -Version 1.0.0 -SkipSign
```
