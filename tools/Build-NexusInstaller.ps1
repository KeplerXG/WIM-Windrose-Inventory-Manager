param(
    [string]$Version = "1.0.0",
    [switch]$SkipSign
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$runner = Join-Path $root "Python_Runner"
$compiledRoot = Join-Path $root "Compiled"
$compiledPython = Join-Path $compiledRoot "Python"
$installerScript = Join-Path $root "installer\WindroseWIM.iss"
$distExe = Join-Path $compiledPython "WindroseWIM.exe"
$distInstaller = Join-Path $compiledRoot "Installer"
$setupExe = Join-Path $distInstaller "WindroseWIM-Setup.exe"
$checksumFile = Join-Path $distInstaller "SHA256SUMS.txt"

function Resolve-Iscc {
    $candidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) { return $c }
    }
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Resolve-SignTool {
    $cmd = Get-Command signtool -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $kits = @(
        "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe",
        "C:\Program Files\Windows Kits\10\bin\*\x64\signtool.exe"
    )
    foreach ($pattern in $kits) {
        $match = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if ($match) { return $match.FullName }
    }
    return $null
}

function Sign-IfConfigured {
    param([string]$PathToFile)
    if ($SkipSign) { return }
    $pfx = $env:SIGN_PFX_PATH
    $pwd = $env:SIGN_PFX_PASSWORD
    $thumb = $env:SIGN_CERT_THUMBPRINT
    $timestamp = if ($env:SIGN_TIMESTAMP_URL) { $env:SIGN_TIMESTAMP_URL } else { "http://timestamp.digicert.com" }

    if (-not $pfx -and -not $thumb) {
        throw "Signing requested but no certificate configured. Set SIGN_PFX_PATH+SIGN_PFX_PASSWORD or SIGN_CERT_THUMBPRINT, or rerun with -SkipSign."
    }

    $signtool = Resolve-SignTool
    if (-not $signtool) {
        throw "signtool.exe not found. Install Windows SDK or set SkipSign."
    }

    if ($pfx) {
        if (-not (Test-Path $pfx)) { throw "SIGN_PFX_PATH does not exist: $pfx" }
        if (-not $pwd) { throw "SIGN_PFX_PASSWORD is required when using SIGN_PFX_PATH" }
        & $signtool sign /fd sha256 /f $pfx /p $pwd /tr $timestamp /td sha256 /a $PathToFile
    } else {
        & $signtool sign /fd sha256 /sha1 $thumb /tr $timestamp /td sha256 $PathToFile
    }
    if ($LASTEXITCODE -ne 0) { throw "Signing failed for $PathToFile" }
}

Write-Host "Building standalone launcher (PyInstaller + bundled WIM)..."
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $runner "build_installer.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not (Test-Path $distExe)) { throw "Missing built launcher: $distExe" }

Sign-IfConfigured -PathToFile $distExe

$iscc = Resolve-Iscc
if (-not $iscc) {
    throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6."
}

New-Item -ItemType Directory -Force -Path $distInstaller | Out-Null
Write-Host "Compiling installer..."
& $iscc "/DMyAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) { throw "ISCC compile failed" }

if (-not (Test-Path $setupExe)) {
    throw "Installer output missing: $setupExe"
}
Sign-IfConfigured -PathToFile $setupExe

$shaSetup = (Get-FileHash -Algorithm SHA256 $setupExe).Hash
$shaLauncher = (Get-FileHash -Algorithm SHA256 $distExe).Hash
@(
    "$shaSetup  WindroseWIM-Setup.exe"
    "$shaLauncher  WindroseWIM.exe"
) | Set-Content -Path $checksumFile -Encoding UTF8

Write-Host ""
Write-Host "Done."
Write-Host "Installer: $setupExe"
Write-Host "Launcher:  $distExe"
Write-Host "Checksums: $checksumFile"
