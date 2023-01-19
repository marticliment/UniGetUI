@echo on

python -m pip install -r requirements.txt

python scripts/apply_versions.py

python scripts/get_contributors.py

rmdir /Q /S wingetuiBin
xcopy wingetui wingetui_bin /E /H /C /I /Y
pushd wingetui_bin

python -m compileall -b .
if %errorlevel% neq 0 goto:error

del /S *.py
copy ..\wingetui\__init__.py .\
rmdir /Q /S __pycache__
rmdir /Q /S external\__pycache__
rmdir /Q /S lang\__pycache__
rmdir /Q /S build
rmdir /Q /S dist

python -m PyInstaller "Win.spec"
if %errorlevel% neq 0 goto:error

pushd dist\wingetuiBin\PySide6
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
popd
move dist\wingetuiBin ..\
popd
rmdir /Q /S wingetui_bin

set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "WingetUI.iss"
    "wingetui Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
    echo "Running WingetUI..."
    start /b wingetuiBin/wingetui.exe
)

goto:end

:error
echo "Error!"

:end
pause
