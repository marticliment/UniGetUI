# UniGetUI Command-line parameters

| Parameter                                           | Description | Compatible versions |
| --------------------------------------------------- | ---------- | ------- |
| `--daemon` | Start UniGetUI without spawning a new window. UniGetUI will run minimized on the system tray. UniGetUI is called with this parameter when launched at startup. **Autostart UniGetUI in the notifications area must be enabled for this parameter to work.** | 1.0+ |
| `--welcomewizard` or `--welcome` | Shows the user the Setup Wizard | up to 2.2.0 |
| `--updateapps` | Force enable automatic installation of available updates | 1.6.0+ |
| `--report-all-errors` | Will force UniGetUI to show the error report page on any crash when loading | 3.0.0+ |
| `--uninstall-unigetui` | Will unregister UniGetUI from the notification panel, and silently quit | 3.1.0+ |
| `--migrate-wingetui-to-unigetui` | Will migrate WingetUI data folders and shortcuts to UniGetUI (if possible), and silently quit | 3.1.0+ |
| `X:\Path\To\file` | Provided that the file is a valid bundle, will load the bundle into the Package Bundles page | 3.1.2+ |


# `unigetui://` protocol
Not yet

# Installer command-line parameters 
The installer is inno-setup based. It supports all Inno Setup command-line parameters as well as the following:

| Parameter                                           | Description |
| --------------------------------------------------- | ---------- |
| `/NoAutoStart` | Will not launch UniGetUI after installation |
| `/ALLUSERS` | Will force the installer to install per-machine (requires administrator privileges) |
| `/CURRENTUSER` | Will force the installer to install per-user | 
| `/NoChocolatey` | Do NOT install chocolatey within UniGetUI | 
| `/NoWinGet` | Do NOT install WinGet and Microsoft.WinGet.Client if not installed **(not recommended)** | 
