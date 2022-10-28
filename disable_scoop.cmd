@echo off 
cd %userprofile%
mkdir .wingetui
cd .wingetui
IF EXIST ScoopAlreadySetup (
echo Scoop was already set up by the installer
) ELSE (
copy nul > DisableScoop
copy nul > ScoopAlreadySetup
)