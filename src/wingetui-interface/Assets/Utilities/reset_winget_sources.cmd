@echo off
SET dirpathx86="%~dp0..\PackageManagers\winget-cli_x64\"
SET dirpatharm="%~dp0..\PackageManagers\winget-cli_arm64\"
SET wingetpath="%~dp0..\winget-cli\PackageManagers\winget-cli_x64\winget.exe"
SET sudopath="%~dp0..\components\gsudo.exe"
%sudopath% cache on
echo This script will reset winget sources.
echo . Please wait... 
echo winget path: %wingetpath%
echo Performing reset...
cd %dirpathx86%
%sudopath% .\winget.exe source remove winget
%sudopath% .\winget.exe source add winget https://cdn.winget.microsoft.com/cache
%sudopath% .\winget.exe source reset --force
if %PROCESSOR_ARCHITECTURE% == ARM64 (
cd %dirpatharm%
  %sudopath% .\winget.exe source remove winget
  %sudopath% .\winget.exe source add winget https://cdn.winget.microsoft.com/cache
  %sudopath% .\winget.exe source reset --force
)
%sudopath% cache off
echo Task completed!
echo You can now close this window and refresh your package lists
pause
