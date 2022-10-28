@echo off
cd %userprofile%
mkdir .wingetui
cd .wingetui
IF EXIST ScoopAlreadySetup (
echo Scoop was already set up by the installer
) ELSE (
powershell -NoProfile -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
powershell -NoProfile -Command "iex ""& {$(irm get.scoop.sh)} -RunAsAdmin"""
powershell -NoProfile -Command "scoop install git"
copy nul > ScoopAlreadySetup
)