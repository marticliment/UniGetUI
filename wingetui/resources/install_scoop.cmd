@echo off
echo This script is going to install scoop and it's required dependencies required by WingetUI. Press any key to continue
pause
powershell -NoProfile -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
echo Installing scoop...
powershell -NoProfile -Command 'iex "& {$(irm get.scoop.sh)} -RunAsAdmin"'
echo Installing git...
powershell -NoProfile -Command "scoop install git"
echo Done!
pause