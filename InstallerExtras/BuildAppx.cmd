copy InstallerExtras\AppxManifest.xml unigetui_bin\AppxManifest.xml
makeappx pack /d unigetui_bin /p UniGetUI.x64.Appx
%signcommand% UniGetUI.x64.Appx