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
dotnet clean src/UniGetUI.sln -v m -nologo
dotnet publish src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64 -v m
if %errorlevel% neq 0 (
    echo "DotNet publish has failed!"
    pause
)
rem sign code

rmdir /Q /S unigetui_bin

mkdir unigetui_bin
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.22621.0\win-x64\publish unigetui_bin *.* /MOVE /E

set /p signfiles="Do you want to sign the files? [Y/n]: "
if /i "%signfiles%" neq "n" (
    %signcommand% "unigetui_bin/UniGetUI.exe" "unigetui_bin/UniGetUI.dll" "unigetui_bin/UniGetUI.*.dll" "unigetui_bin/ExternalLibraries.*.dll"

    if %errorlevel% neq 0 (
        echo "Signing has failed!"
        pause
    )
)

pushd unigetui_bin
copy UniGetUI.exe WingetUI.exe
popd

set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "UniGetUI.iss"
    %signcommand% "UniGetUI Installer.exe"
    del "WingetUI Installer.exe"
    copy "UniGetUI Installer.exe" "WingetUI Installer.exe" 
    pause
    echo Hash: 
    pwsh.exe -Command "(Get-FileHash '.\UniGetUI Installer.exe').Hash"
    echo .
    "UniGetUI Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
)

pause
