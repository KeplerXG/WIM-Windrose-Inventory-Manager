# Publishes one self-contained x64 .exe (Windrose-style single-file bundle).
# Output: repo\publish_single\WIM - Windrose Inventory Manager.exe  (+ optional rocksdb.dll if you copy it there)
# File/explorer icon: repo-root app.ico is wired as ApplicationIcon in the .csproj (PE icon).
# Runtime: same file is also EmbeddedResource for AppIcon.ApplyTo (window title bar).
# Requires: .NET 8 SDK. Run from repo root or anywhere (script resolves paths).

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$proj = Join-Path $root 'decompiled\WindroseEditor.csproj'
$out = Join-Path $root 'publish_single'
$nugetCfg = Join-Path $root 'NuGet.Build.config'

if (-not (Test-Path $nugetCfg)) {
    Write-Host "ERROR: Missing $nugetCfg" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing single-file to: $out"
New-Item -ItemType Directory -Force -Path $out | Out-Null

dotnet restore $proj --configfile $nugetCfg -r win-x64 --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$embedDb = Test-Path (Join-Path $root 'windrose_items.db')
$publishArgs = @(
    '-c', 'Release',
    '-r', 'win-x64',
    '-o', $out,
    '--no-restore',
    '--nologo',
    '-p:PublishSingleFile=true',
    '-p:SelfContained=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=None'
)
if ($embedDb) {
    $publishArgs += '-p:EmbedBundledDatabase=true'
    Write-Host "Embedding windrose_items.db from repo root."
} else {
    Write-Host "No windrose_items.db at repo root — publish will not embed item DB (place file + re-publish to bundle)."
}

dotnet publish $proj @publishArgs

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Done. Main payload: $out\WIM - Windrose Inventory Manager.exe"
Write-Host "Copy rocksdb.dll from the game next to that exe if the editor reports RocksDB load errors."
Get-ChildItem $out -File | Select-Object Name, @{N='MB';E={[math]::Round($_.Length/1MB,2)}}
