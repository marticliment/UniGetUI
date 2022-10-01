@echo off
powershell -NoProfile -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
powershell -NoProfile -Command 'iex "& {$(irm get.scoop.sh)} -RunAsAdmin"'
powershell -NoProfile -Command "scoop install git"