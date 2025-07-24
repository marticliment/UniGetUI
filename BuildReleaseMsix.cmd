@echo off

rem update resources
python scripts/apply_versions.py

rem Kill running instances UniGetUI
taskkill /im wingetui.exe /f
taskkill /im unigetui.exe /f

rem Build executable file
dotnet clean src/UniGetUI.sln -v m -nologo
dotnet publish src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64 -v m
if %ERRORLEVEL% NEQ 0 ( pause )

rem Move binaries to unigetui_release
rmdir /Q /S unigetui_release
mkdir unigetui_release
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\publish unigetui_release *.* /MOVE /E
if %ERRORLEVEL% NEQ 0 ( pause )

rem Sign all exe and dll
set /p signfiles="Do you want to sign the files? [Y/n]: "
if /i "%signfiles%" neq "n" (
    %signcommand% "unigetui_release/UniGetUI.exe" "unigetui_release/UniGetUI.dll" "unigetui_release/UniGetUI.*.dll" "unigetui_release/ExternalLibraries.*.dll"
    if %ERRORLEVEL% NEQ 0 ( pause )
)

rem Create output/ folder
rmdir /q /s output
mkdir output

rem Create MSIX Package
python scripts\IncreaseMsixPatchNum.py
copy InstallerExtras\AppxManifest.xml unigetui_release\AppxManifest.xml
makeappx pack /d unigetui_release /p output\UniGetUI.x64.Msix
if %ERRORLEVEL% NEQ 0 ( pause )

rem Sign MSIX package
%signcommand% output\UniGetUI.x64.Msix
if %ERRORLEVEL% NEQ 0 ( pause )

rem Create ZIP file containing bin
pushd unigetui_release
7z a -tzip "..\output\UniGetUI.x64.zip" "*"
popd ..
if %ERRORLEVEL% NEQ 0 ( pause )

rem Create INNO Installer
set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
%INSTALLATOR% "UniGetUI_MSIX.iss"
if %ERRORLEVEL% NEQ 0 ( pause )

move "UniGetUI.Installer.exe" output\
rmdir /q /s unigetui_release

rem Generate release data
pwsh.exe -Command echo """UniGetUI.Installer.exe SHA256: ``$((Get-FileHash 'output\UniGetUI.Installer.exe').Hash)``"""
pwsh.exe -Command echo """UniGetUI.x64.Msix SHA256: ``$((Get-FileHash 'output\UniGetUI.x64.Msix').Hash)``"""
pwsh.exe -Command echo """UniGetUI.x64.zip SHA256: ``$((Get-FileHash 'output\UniGetUI.x64.zip').Hash)``"""
echo .
pause

rem run the generated installer
"output\UniGetUI.Installer.exe"
pause
