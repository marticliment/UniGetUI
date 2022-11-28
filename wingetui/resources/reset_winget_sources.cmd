@echo off
SET wingetpath=%~dp0..\winget-cli\winget.exe
SET sudopath=%~dp0..\sudo\gsudo.exe
echo This script will reset winget sources.
echo You might be asked for administrator rights up to three times during this process.
echo|set/p="Press <ENTER> to continue or CLOSE THIS WINDOW to abort this process"&runas/u: "">NUL
echo . Please wait... 
echo winget path: %wingetpath%
echo Performing reset...
%sudopath% %wingetpath% source remove winget
%sudopath% %wingetpath% source add winget https://cdn.winget.microsoft.com/cache
%sudopath% %wingetpath% source reset --force
echo Task completed!
echo You can now close this window and refresh your wingetui listings
pause
