

@echo off


rem update resources
python scripts/apply_versions.py
pushd scripts
python download_translations.py
popd ..

rem clean old builds
taskkill /im wingetui.exe /f
rmdir /Q /S src\UniGetUI\obj
rmdir /Q /S src\UniGetUI\bin

rem build executable
pushd src 
nuget restore
popd ..
"C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\amd64\MSBuild.exe" src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64

rem sign code
pushd src\UniGetUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64
"Y:\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "Y:\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "Y:\- Signing\metadata.json" "wingetui.exe" "wingetui.dll"
popd
pause

set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "WingetUI.iss"
    echo You may now sign the installer
    "Y:\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "Y:\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "Y:\- Signing\metadata.json" "WingetUI Installer.exe"
    pause
    "wingetUI Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
)

goto:end

:error
echo "Error!"

:end
pause
