set chocolateyinstall=%cd%\choco-cli
cd choco-cli
.\choco upgrade chocolatey
rmdir /Q /S lib
mkdir lib
rmdir /Q /S logs
rmdir /Q /S config
rmdir /Q /S .chocolatey
pause