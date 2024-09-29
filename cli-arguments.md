# UniGetUI Command-line parameters

| Parameter                                           | Description | Compatible versions |
| --------------------------------------------------- | ---------- | ------- |
| `--daemon` | Start UniGetUI without spawning a new window. UniGetUI will run minimized on the system tray. UniGetUI is called with this parameter when launched at startup. **Autostart UniGetUI in the notifications area must be enabled for this parameter to work.** | 1.0+ |
| `--welcome` | Shows the user the Setup Wizard | up to 2.2.0 |
| `--updateapps` | Force enable automatic installation of available updates | 1.6.0+ |
| `--report-all-errors` | Will force UniGetUI to show the error report page on any crash when loading | 3.0.0+ |
| `--uninstall-unigetui` | Will unregister UniGetUI from the notification panel, and silently quit | 3.1.0+ |
| `--migrate-wingetui-to-unigetui` | Will migrate WingetUI data folders and shortcuts to UniGetUI (if possible), and silently quit | 3.1.0+ |
| `X:\Path\To\file` | Provided that the file is a valid bundle, will load the bundle into the Package Bundles page. Compatible bundle files include the following extensions: `.ubundle`, `.json`, `.yaml`, `.xml` | 3.1.2+ |

<br><br>
# `unigetui://` deep link
On a system where UniGetUI 3.1.2+ is installed, the following deep links can be used to communicate with UniGetUI:

| Parameter                                           | Description |
| --------------------------------------------------- | ---------- |
| `unigetui://showPackage?id={}&managerName={}&sourceName={}` | Show the Package Details page with the provided package. <br>The parameters `id`, `managerName` and `sourceName` are<br> required and cannot be empty |
| `unigetui://showUniGetUI` | Shows UniGetUI and brings the window to the front |
| `unigetui://showDiscoverPage` | Shows UniGetUI and loads the Updates page | 
| `unigetui://showUpdatesPage` | Shows UniGetUI and loads the Updates page | 
| `unigetui://showInstalledPage` | Shows UniGetUI and loads the Updates page | 

<br><br>

# Installer command-line parameters 
The installer is inno-setup based. It supports all Inno Setup command-line parameters as well as the following:

| Parameter                                           | Description |
| --------------------------------------------------- | ---------- |
| `/NoAutoStart` | Will not launch UniGetUI after installation |
| `/NoVCRedist` | Will not install MS Visual C++ Redistributable x64 (v3.1.2+) |
| `/NoEdgeWebView` | Will not install Microsoft Edge WebView Runtime (v3.1.2+) |
| `/ALLUSERS` | Will force the installer to install per-machine (requires administrator privileges) |
| `/CURRENTUSER` | Will force the installer to install per-user | 
| `/NoChocolatey` | Do NOT install chocolatey within UniGetUI | 
| `/NoWinGet` | Do NOT install WinGet and Microsoft.WinGet.Client if not installed **(not recommended)** | 
