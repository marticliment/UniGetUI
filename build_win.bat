del "WingetUI Store.exe"
cd wingetui
rmdir /Q /S build
rmdir /Q /S dist
python -m PyInstaller "Win.spec"
cd dist
move "WingetUI Store.exe" ../../
cd ..
rmdir /Q /S build
rmdir /Q /S dist
cd ..
"WingetUI Store"
pause