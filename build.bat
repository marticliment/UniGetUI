set "py=%cd%\env\Scripts\python.exe"

IF EXIST %py% (
    echo "Using VENV Python"
) ELSE (
    set "py=python"
    echo "Using system Python"
)


@echo off

%py% -m pip install -r requirements.txt

%py% scripts/apply_versions.py

%py% scripts/get_contributors.py

rmdir /Q /S wingetuiBin

%py% scripts\generate_integrity.py --buildfiles

mkdir wingetui_bin
xcopy wingetui wingetui_bin\wingetui /E /H /C /I /Y
pushd wingetui_bin\wingetui

%py% -m compileall -b .
if %errorlevel% neq 0 goto:error

del /S *.py
copy ..\..\wingetui\launcher.py .\
del launcher.pyc
rmdir /Q /S __pycache__
rmdir /Q /S ExternalLibraries\__pycache__
rmdir /Q /S ExternalLibraries\PyWebView2\__pycache__
rmdir /Q /S Core\__pycache__
rmdir /Q /S Core\Data\__pycache__
rmdir /Q /S Core\Languages\__pycache__
rmdir /Q /S Interface\__pycache__
rmdir /Q /S Interface\CustomWidgets\__pycache__
rmdir /Q /S PackageEngine\__pycache__
rmdir /Q /S PackageEngine\Managers\__pycache__
rmdir /Q /S build
rmdir /Q /S dist

move launcher.py ..\launcher.py
move Win.spec ..\Win.spec

popd
pushd wingetui_bin

%py% -m PyInstaller "Win.spec"
if %errorlevel% neq 0 goto:error

timeout 5

pushd dist\wingetuiBin\wingetui
move choco-cli ..\choco-cli
popd

pushd dist\wingetuiBin\PySide6
del opengl32sw.dll
del Qt6Quick.dll
del Qt6Qml.dll
del Qt6Pdf.dll
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

"Y:\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "Y:\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "Y:\- Signing\metadata.json" "wingetuiBin/wingetui.exe"
pause

set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "WingetUI.iss"
    echo You may now sign the installer
    "Y:\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "Y:\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "Y:\- Signing\metadata.json" "WingetUI Installer.exe"
    pause
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
