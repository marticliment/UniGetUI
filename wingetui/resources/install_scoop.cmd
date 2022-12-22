@echo off
SET wingetuipath=%~dp0..\wingetui.exe
echo This script will install Scoop and the dependencies required by WingetUI.
pause
powershell -ExecutionPolicy ByPass -File "%~dp0\install_scoop.ps1"
if %errorlevel% equ 0 (
    echo WingetUI needs to be restarted now. This script is now going to restart WingetUI
    pause
    taskkill /im wingetui.exe /f
    start /b %wingetuipath%
) else (
    pause
)
