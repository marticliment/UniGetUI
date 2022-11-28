@echo on

python -m pip install -r requirements.txt

xcopy wingetui wingetui_bin /E /H /C /I /Y
cd wingetui_bin
python -m compileall -b .
del /S *.py
copy ..\wingetui\__init__.py .\
rmdir /Q /S __pycache__
rmdir /Q /S external\__pycache__
rmdir /Q /S lang\__pycache__
rmdir /Q /S build
rmdir /Q /S dist
python -m PyInstaller "Win.spec"
cd dist
cd ..
cd ..
rmdir /Q /S wingetuiBin
cd wingetui_bin
cd dist
move wingetuiBin ../../
cd ..
rmdir /Q /S build
rmdir /Q /S dist
cd ..
rmdir /Q /S wingetui_bin
cd wingetuiBin
rem cd tcl
rem rmdir /Q /S tzdata
rem cd ..
rem cd ..
cd PySide6
del opengl32sw.dll
del Qt6Quick.dll
del Qt6Qml.dll
del Qt6OpenGL.dll
del Qt6QmlModels.dll
del Qt6Network.dll
del Qt6DataVisualization.dll
del Qt6VirtualKeyboard.dll
del QtDataVisualization.pyd
del QtOpenGL.pyd
cd ..
cd ..

set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "WingetUI.iss"
    "wingetui Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
    echo "Running WingetUI..."
    start /b wingetuiBin/wingetui.exe
)

pause
