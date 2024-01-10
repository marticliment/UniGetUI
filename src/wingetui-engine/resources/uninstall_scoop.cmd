@echo off
SET wingetuipath=%~dp0..\wingetui.exe
echo This script will remove scoop from your machine.
echo Removing Scoop implies removing all Scoop installed packages, buckets and preferences, and might also erase valuable user data related to the affected packages
echo|set/p="Press <ENTER> to continue or CLOSE this window to abort this process"&runas/u: "">NUL
echo.
echo Uninstalling scoop...
powershell -ExecutionPolicy ByPass -Command "scoop uninstall -p scoop"
if %errorlevel% equ 0 (
    echo WingetUI will be restarted to continue.
    pause
    taskkill /im wingetui.exe /f
    start /b %wingetuipath% /i
) else (
    pause
    taskkill /im wingetui.exe /f
    start /b %wingetuipath% /i
)
