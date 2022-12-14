@echo off
echo This script will install Scoop and the dependencies required by WingetUI.
pause
powershell -ExecutionPolicy ByPass -File "%~dp0\install_scoop.ps1"
pause
