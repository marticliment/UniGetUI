del UniGetUI.x64.Appx

set /p userChoice="Do you want to build the project? (Y/N): "
if /I NOT "%userChoice%"=="Y" (
    echo Skipping build.
    goto :CONTINUE
)

rmdir /q /s src\UniGetUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\
dotnet build src/UniGetUI/UniGetUI.csproj /noLogo /property:Configuration=Debug /property:Platform=x64 -v m 
%signcommand% "src\UniGetUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\UniGetUI.exe"
rmdir /q /s unigetui_debug
mkdir unigetui_debug
robocopy src\UniGetUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64 unigetui_debug *.* /MOVE /E

:CONTINUE

python InstallerExtras\IncreaseMsixPatchNum.py
copy InstallerExtras\AppxManifest.xml unigetui_debug\AppxManifest.xml
makeappx pack /d unigetui_debug /p UniGetUI.x64.Appx

if %ERRORLEVEL% NEQ 0 (
    pause
)

%signcommand% UniGetUI.x64.Appx
set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
%INSTALLATOR% "UniGetUI_MSIX.iss"
UniGetUI.MSIX.exe
