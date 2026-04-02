@echo off
setlocal

for %%I in ("%~dp0..") do set INSTALL_DIR=%%~fI

powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0apply-update.ps1" %*
set EXITCODE=%ERRORLEVEL%

if not "%EXITCODE%"=="0" (
    echo.
    echo VMS upgrade failed. Review "%INSTALL_DIR%\logs\vms-upgrade.log" and the latest backup folder next to the install directory.
    pause
    exit /b %EXITCODE%
)

echo.
echo VMS upgrade completed successfully.
pause
exit /b 0
