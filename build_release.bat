

@echo off


rem update resources
python scripts/apply_versions.py

pushd scripts
python download_translations.py
popd ..

rem clean old builds
taskkill /im wingetui.exe /f
taskkill /im unigetui.exe /f

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
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\publish unigetui_bin *.* /MOVE /E
rem pushd src\UniGetUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish
pushd unigetui_bin

%SIGNCOMMAND% %cd%\UniGetUI.exe %cd%\UniGetUI.dll

echo .
echo .
echo You may want to sign now the following executables
cd
echo UniGetUI.dll
echo UniGetUI.exe
echo .
echo .
pause
copy UniGetUI.exe WingetUI.exe
popd


set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "UniGetUI.iss"
    echo You may now sign the installer
    %SIGNCOMMAND% UniGetUI.Installer.exe
    del "WingetUI Installer.exe"
    copy "UniGetUI Installer.exe" "WingetUI Installer.exe" 
    pause
    "UniGetUI Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
)

goto:end

:error
echo "Error!"

:end
pause
