del UniGetUI.x64.Appx

set /p userChoice="Do you want to build the project? (Y/N): "
if /I NOT "%userChoice%"=="Y" (
    echo Skipping build.
    goto :CONTINUE
)

rem Clear old builds
rmdir /q /s src\UniGetUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\

rem Build UniGetUI
dotnet build src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Release /property:Platform=x64 -v m 
if %ERRORLEVEL% NEQ 0 ( pause )

rem Sign main executable
%signcommand% "src\UniGetUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\UniGetUI.exe"
if %ERRORLEVEL% NEQ 0 ( pause )

rem Move binaries to unigetui_debug
rmdir /q /s unigetui_debug
mkdir unigetui_debug
robocopy src\UniGetUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64 unigetui_debug *.* /MOVE /E

rem checkpoint
:CONTINUE

rem Create output/ folder
rmdir /q /s output
mkdir output

rem Create MSIX Package
python scripts\IncreaseMsixPatchNum.py
copy InstallerExtras\AppxManifest.xml unigetui_debug\AppxManifest.xml
makeappx pack /d unigetui_debug /p output\UniGetUI.x64.Msix
if %ERRORLEVEL% NEQ 0 ( pause )

rem Sign MSIX package
%signcommand% output\UniGetUI.x64.Msix

taskkill /im unigetui.exe /f
powershell -Command Add-AppxPackage output\UniGetUI.x64.Msix
start unigetui://

pause
exit

rem Create INNO Installer
set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
%INSTALLATOR% "UniGetUI_MSIX.iss"
move "UniGetUI.Installer.exe" output\
if %ERRORLEVEL% NEQ 0 ( pause )

rem Clear and run installer
rem rmdir /q /s unigetui_debug
"output\UniGetUI.Installer.exe" /CURRENTUSER /SILENT /NoChocolatey

pause