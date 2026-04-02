@echo off
echo Restarting Visitor Management System...
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)
%SystemRoot%\System32\inetsrv\appcmd recycle apppool /apppool.name:"VMSAppPool"
echo Done. VMS has been restarted.
pause
