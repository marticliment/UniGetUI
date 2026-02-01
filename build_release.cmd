@echo off

rem update resources
python scripts/apply_versions.py

rem pushd scripts
rem python download_translations.py
rem popd ..

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
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\publish unigetui_bin *.* /MOVE /E


set /p signfiles="Do you want to sign the files? [Y/n]: "
if /i "%signfiles%" neq "n" (
    %signcommand% "unigetui_bin/UniGetUI.exe" "unigetui_bin/*.dll"

    if %errorlevel% neq 0 (
        echo "Signing has failed!"
        pause
    )
)

pushd unigetui_bin
copy UniGetUI.exe WingetUI.exe
popd


rem Generate integrity
python3 scripts\generate_integrity_tree.py %cd%\unigetui_bin

rmdir /q /s output
mkdir output
cd unigetui_bin
7z a -tzip "..\output\UniGetUI.x64.zip" "*"
cd ..
if %errorlevel% neq 0 (
    echo "Compression of unigetui_bin into output/UniGetUI.x64.zip has failed!"
    pause
)

set INSTALLATOR="%localappdata%\Programs\Inno Setup 6\ISCC.exe"
if exist %INSTALLATOR% (
    %INSTALLATOR% "UniGetUI.iss"
    move "UniGetUI Installer.exe" "UniGetUI.Installer.exe"
    del "WingetUI.Installer.exe"
    copy "UniGetUI.Installer.exe" "WingetUI.Installer.exe" 
    move "UniGetUI.Installer.exe" output\
    move "WingetUI.Installer.exe" output\
    rmdir /q /s unigetui_bin
    
    pwsh.exe -Command echo """UniGetUI.Installer.exe SHA256: ``$((Get-FileHash 'output\UniGetUI.Installer.exe').Hash)``"""
    pwsh.exe -Command echo """UniGetUI.x64.zip SHA256: ``$((Get-FileHash 'output\UniGetUI.x64.zip').Hash)``"""
    echo .
    pause
    "output\UniGetUI.Installer.exe"
) else (
    echo "Make installer was skipped, because the installer is missing."
)

pause
