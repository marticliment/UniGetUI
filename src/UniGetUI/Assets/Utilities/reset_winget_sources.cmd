@echo off
SET wingetpath="%~dp0..\..\winget-cli_x64\winget.exe"
SET sudopath="%~dp0..\..\Assets\Utilities\gsudo.exe"
%sudopath% cache on
echo This script will reset winget sources.
echo . Please wait... 
echo winget path: %wingetpath%
echo Deleting Winget local data sources...
powershell -ExecutionPolicy Bypass -File "%~dp0\delete_winget_databases.ps1"
echo Performing reset...

echo Resetting bundled WinGet
%sudopath% %wingetpath% source remove winget
%sudopath% %wingetpath% source add winget https://cdn.winget.microsoft.com/cache
%sudopath% %wingetpath% source reset --force

echo Resetting Native WinGet
%sudopath% winget.exe source remove winget
%sudopath% winget.exe source add winget https://cdn.winget.microsoft.com/cache
%sudopath% winget.exe source reset --force

%sudopath% cache off
echo Task completed!
echo You can now close this window and restart UniGetUI
pause
exit
