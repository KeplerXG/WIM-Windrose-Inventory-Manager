# Legacy shim — use build_installer.ps1 for a true standalone launcher (Tk + embedded self-contained WIM, no dotnet on target PCs).

Write-Host @"
Use build_installer.ps1 instead. It publishes WIM into Assets\bundled\ then builds dist\WindroseWIM.exe (one-file, --windowed).

  powershell -ExecutionPolicy Bypass -File $($PSScriptRoot)\build_installer.ps1

"@
