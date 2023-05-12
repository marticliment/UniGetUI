set "py=%cd%\env\Scripts\python.exe"

IF EXIST %py% (
    echo "Using VENV Python"
) ELSE (
    set "py=python"
    echo "Using system Python"
)


@echo on

%py% -m pip install -r requirements.txt

%py% scripts/apply_versions.py

%py% scripts/get_contributors.py

rmdir /Q /S wingetuiBin
xcopy wingetui wingetui_bin /E /H /C /I /Y
pushd wingetui_bin

%py% -m compileall -b .
if %errorlevel% neq 0 goto:error

del /S *.py
copy ..\wingetui\__init__.py .\
del __init__.pyc
rmdir /Q /S __pycache__
rmdir /Q /S external\__pycache__
rmdir /Q /S data\__pycache__
rmdir /Q /S PackageManagers\__pycache__
rmdir /Q /S lang\__pycache__
rmdir /Q /S build
rmdir /Q /S dist

%py% -m PyInstaller "Win.spec"
if %errorlevel% neq 0 goto:error

timeout 2

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
pushd dist\wingetuiBin\choco-cli
rmdir /Q /S .chocolatey
rmdir /Q /S lib
rmdir /Q /S lib-bad
rmdir /Q /S lib-bkp
rmdir /Q /S logs
mkdir lib
mkdir logs
mkdir .chocolatey
mkdir lib-bad
mkdir lib-bkp
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
