# How to create a MSI Installer for UniGetUI
Sometimes, when deploying software through GPO, msi installers are required. However, UniGetUI does not offer such installers by default.
In order to obtain a .msi installer, the following guide must be followed.


> [!Warning]
> When using MSI Installers, required dependencies will not be installed automatically. The deployer will need to ensure that the following requirements are met on target machines:
> - Microsoft Visual C++ Redistriutable 2015-2022 (x64)
> - Microsoft Edge WebView Runtime (x64)

## Creating a MSI wrapper for the installer
1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/es/downloads/) and the [Microsoft Visual Studio Installer Projects 2022 extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2022InstallerProjects).
2. Clone this repository: 
```
git clone https://github.com/marticliment/UniGetUI
```
3. Download from [GitHub](https://github.com/marticliment/UniGetUI/releases/) the installer version you wish to package as MSI.
4. Move the downloaded exe installer into `InstallerExtras/MsiCreator`. Ensure that the downloaded installer name is **exactly** `UniGetUI Installer.exe`
5. Open the Solution (`MsiInstallerWrapper.sln`) with Visual Studio and build the solution. The files `UniGetUISetup.msi` and `setup.exe` will be created. You may want to delete `setup.exe`, since it will not be used.
6. (Optional) Test that the file `UniGetUISetup.msi` installs UniGetUI properly. You should be now ready to go
