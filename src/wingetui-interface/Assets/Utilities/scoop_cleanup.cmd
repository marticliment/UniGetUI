@echo off
echo This script will clean the Scoop cache and run the cleanup command.
echo|set/p="Press <ENTER> to continue or CLOSE this window to abort this process"&runas/u: "">NUL
cls
echo Cleaning Scoop cache...
scoop cleanup --all --global
scoop cache rm --all --global
scoop cleanup --all
scoop cache rm --all
echo Done!
pause