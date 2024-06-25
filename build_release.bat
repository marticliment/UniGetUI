

@echo off


rem update resources
python scripts/apply_versions.py

pushd scripts
python download_translations.py
popd ..

rem clean old builds
taskkill /im wingetui.exe /f

rem Run tests
dotnet test src/UniGetUI.sln -v q --nologo

rem check exit code of the last command
if %errorlevel% neq 0 (
    echo "The tests failed!."
    pause
)

rem build executable
dotnet clean src/UniGetUI.sln
dotnet publish src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64

rem sign code

rmdir /Q /S unigetui_bin

mkdir unigetui_bin
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish unigetui_bin *.* /MOVE /E
rem pushd src\UniGetUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish
pushd unigetui_bin
"C:\SomePrograms\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "C:\SomePrograms\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "C:\SomePrograms\- Signing\metadata.json" C:\SomePrograms\WingetUI-Store\unigetui_bin\WingetUI.exe
"C:\SomePrograms\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "C:\SomePrograms\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "C:\SomePrograms\- Signing\metadata.json" C:\SomePrograms\WingetUI-Store\unigetui_bin\WingetUI.dll
echo .
echo .
echo You may want to sign now the following executables
cd
echo WingetUI.dll
echo WingetUI.exe
echo .
echo .
pause
cp WingetUI.exe UniGetUI.exe
popd


set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "WingetUI.iss"
    echo You may now sign the installer
    "Y:\- Signing\signtool-x64\signtool.exe" sign /v /debug /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "Y:\- Signing\azure.codesigning.client\x64\Azure.CodeSigning.Dlib.dll" /dmdf "Y:\- Signing\metadata.json" "WingetUI Installer.exe"
    pause
    del "UniGetUI Installer.exe"
    copy "WingetUI Installer.exe" "UniGetUI Installer.exe" 
    "WingetUI Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
)

goto:end

:error
echo "Error!"

:end
pause
