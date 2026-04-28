@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "NOPAUSE=0"
if /i "%~1"=="nopause" set "NOPAUSE=1"

echo Building signed Nexus installer + launcher...
echo.
echo Requires one of:
echo   SIGN_PFX_PATH + SIGN_PFX_PASSWORD
echo   OR SIGN_CERT_THUMBPRINT
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Build-NexusInstaller.ps1" -Version 1.0.0
if errorlevel 1 goto :fail

echo.
echo Done:
echo   Installer: "%~dp0Compiled\Installer\WindroseWIM-Setup.exe"
echo   Launcher:  "%~dp0Compiled\Python\WindroseWIM.exe"
goto :end

:fail
echo.
echo Build/sign failed. See errors above.
if "%NOPAUSE%"=="0" pause
exit /b 1

:end
if "%NOPAUSE%"=="0" pause
exit /b 0
