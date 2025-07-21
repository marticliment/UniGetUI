del UniGetUI.x64.Appx
copy InstallerExtras\AppxManifest.xml unigetui_bin\AppxManifest.xml
makeappx pack /d unigetui_bin /p UniGetUI.x64.Appx  /cf LZMA2

if %ERRORLEVEL% NEQ 0 (
    pause
)

%signcommand% UniGetUI.x64.Appx
set INSTALLATOR="%SYSTEMDRIVE%\Program Files (x86)\Inno Setup 6\ISCC.exe"
%INSTALLATOR% "UniGetUI_MSIX.iss"
UniGetUI.MSIX.exe
