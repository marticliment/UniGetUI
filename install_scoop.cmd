@echo off
powershell -NoProfile -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
powershell -NoProfile -Command "Invoke-WebRequest get.scoop.sh | Invoke-Expression"
powershell -NoProfile -Command "scoop install git"