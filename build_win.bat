del "WingetUI.exe"
rmdir /Q /S build
rmdir /Q /S dist
rmdir /Q /S Output
cd wingetui
python -m PyInstaller "Win.spec"
cd dist
taskkill /im WingetUI.exe /f
move "WingetUI.exe" ../../
cd ..
rmdir /Q /S build
rmdir /Q /S dist
cd ..
"WingetUI"
pause