# Build a single standalone WindroseWIM.exe (Tk profile picker + embedded self-contained WIM).
# Prerequisites: Python 3.10+, pip, PyInstaller requirements, .NET 8 SDK (build machine only).
# Output: ..\Compiled\Python\WindroseWIM.exe — copying that one file is enough for end users (no dotnet required).
#
# Run from repo root or from Python_Runner:
#   powershell -ExecutionPolicy Bypass -File Python_Runner\build_installer.ps1

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$runner = $PSScriptRoot
$bundledDir = Join-Path $runner "Assets\bundled"
$proj = Join-Path $runner "Assets\decompiled\WIM.csproj"
$nuget = Join-Path $runner "Assets\NuGet.Build.config"
$dbAssets = Join-Path $runner "Assets\windrose_items.db"
$repoRoot = Resolve-Path (Join-Path $runner "..")
$dbRoot = Join-Path $repoRoot "windrose_items.db"
$compiledRoot = Join-Path $repoRoot "Compiled"
$compiledPython = Join-Path $compiledRoot "Python"
$versionInfo = Join-Path $runner "wim_version_info.txt"

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

function Sign-BinaryIfConfigured {
    param([string]$PathToFile)
    $pfx = $env:SIGN_PFX_PATH
    $pwd = $env:SIGN_PFX_PASSWORD
    $thumb = $env:SIGN_CERT_THUMBPRINT
    if (-not $pfx -and -not $thumb) {
        Write-Warning "No code-sign certificate configured. EXE will NOT be trusted by SmartScreen/AV reputation."
        return
    }
    $signtool = Resolve-SignTool
    if (-not $signtool) {
        Write-Error "signtool.exe not found. Install Windows SDK to sign binaries."
        return
    }
    $timestamp = if ($env:SIGN_TIMESTAMP_URL) { $env:SIGN_TIMESTAMP_URL } else { "http://timestamp.digicert.com" }
    if ($pfx) {
        if (-not (Test-Path $pfx)) { throw "SIGN_PFX_PATH does not exist: $pfx" }
        if (-not $pwd) { throw "SIGN_PFX_PASSWORD is required with SIGN_PFX_PATH" }
        & $signtool sign /fd sha256 /f $pfx /p $pwd /tr $timestamp /td sha256 /a $PathToFile
    } else {
        & $signtool sign /fd sha256 /sha1 $thumb /tr $timestamp /td sha256 $PathToFile
    }
    if ($LASTEXITCODE -ne 0) { throw "Signing failed for $PathToFile" }
    & $signtool verify /pa /all $PathToFile
    if ($LASTEXITCODE -ne 0) { throw "Signature verification failed for $PathToFile" }
}

if (-not (Test-Path $proj)) {
    Write-Error "Missing $proj"
}
if (-not (Test-Path $nuget)) {
    Write-Error "Missing $nuget"
}
if (-not (Test-Path $dbAssets)) {
    if (Test-Path $dbRoot) {
        Copy-Item -Force $dbRoot $dbAssets
        Write-Host "Copied windrose_items.db from repo root to Assets\"
    } else {
        Write-Error "Place windrose_items.db in Python_Runner\Assets\ or at repo root."
    }
}

$dotnet = "$env:ProgramFiles\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Write-Host "Publishing self-contained single-file WIM to $bundledDir ..."
Remove-Item -Recurse -Force $bundledDir -ErrorAction SilentlyContinue | Out-Null
New-Item -ItemType Directory -Force -Path $bundledDir | Out-Null

& $dotnet restore $proj --configfile $nuget -r win-x64 --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet publish $proj -c Release -r win-x64 -o $bundledDir --no-restore --nologo `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:EmbedBundledDatabase=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$wimExe = Join-Path $bundledDir "WIM - Windrose Inventory Manager.exe"
if (-not (Test-Path $wimExe)) {
    Write-Error "dotnet publish did not produce WIM - Windrose Inventory Manager.exe"
}

python -m pip install -r (Join-Path $runner "requirements-launcher.txt")

$icon = Join-Path $runner "Assets\app.ico"
$icoArgs = @()
if (Test-Path $icon) {
    $icoArgs = @("--icon", $icon)
}

# Embed the entire bundled folder — at runtime run_wim resolves runner_root via sys._MEIPASS.
$dArg = "$(Join-Path $runner 'Assets\bundled');Assets\bundled"
$pyiWork = Join-Path $env:TEMP "wim_pyinstaller_work"
$pyiSpec = Join-Path $env:TEMP "wim_pyinstaller_spec"
New-Item -ItemType Directory -Force -Path $pyiWork | Out-Null
New-Item -ItemType Directory -Force -Path $pyiSpec | Out-Null

& python -m PyInstaller --clean --noconfirm --onefile --windowed --name "WindroseWIM" `
    --workpath $pyiWork `
    --specpath $pyiSpec `
    --distpath $compiledPython `
    --version-file $versionInfo `
    @icoArgs `
    --collect-submodules "tkinter" `
    "--add-data" $dArg `
    (Join-Path $runner "run_wim.py")

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dist = Join-Path $compiledPython "WindroseWIM.exe"
Sign-BinaryIfConfigured -PathToFile $dist
Write-Host ""
Write-Host "Done - standalone launcher: $dist"
Write-Host "End users: run WindroseWIM.exe (profile picker -> Launch WIM). No dotnet beside the launcher required."
