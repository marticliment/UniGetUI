powershell -Command "Set-ExecutionPolicy RemoteSigned -Scope CurrentUser"
powershell -Command "Invoke-WebRequest get.scoop.sh | Invoke-Expression"