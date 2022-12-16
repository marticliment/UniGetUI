@echo off
echo This script will remove scoop from your machine.
echo Removing scoop implies removing all scoop installed packages, buckets and preferences, and might also erase valuable user data related to the affected packages
echo|set/p="Press <ENTER> to continue or CLOSE this window to abort this process"&runas/u: "">NUL
echo.
echo Uninstalling scoop...
powershell -ExecutionPolicy ByPass -Command "scoop uninstall -p scoop"
echo Done!
pause
