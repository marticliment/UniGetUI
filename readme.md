# <img src="https://raw.githubusercontent.com/marticliment/WingetUI/main/src/wingetui/Assets/Images/icon.png" height="40">WingetUI

[![Downloads@latest](https://img.shields.io/github/downloads/marticliment/WingetUI/2.1.1/total?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe)
[![Release Version Badge](https://img.shields.io/github/v/release/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases)
[![Issues Badge](https://img.shields.io/github/issues/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/issues)
[![Closed Issues Badge](https://img.shields.io/github/issues-closed/marticliment/WingetUI?color=%238256d0&style=for-the-badge)](https://github.com/marticliment/WingetUI/issues?q=is%3Aissue+is%3Aclosed)<br>

The main goal of this project is to create an intuitive GUI for the most common CLI package managers for Windows 10 and Windows 11, such as [Winget](https://learn.microsoft.com/en-us/windows/package-manager/), [Scoop](https://scoop.sh/), [Chocolatey](https://chocolatey.org/), [Pip](https://pypi.org/), [Npm](https://www.npmjs.com/), [.NET Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install) and [PowerShell Gallery](https://www.powershellgallery.com/).
With this app, you'll be able to easily download, install, update and uninstall any software that's published on the supported package managers — and much more!

Check out the [Supported Package Managers Table](#supported-package-managers) for more details!

**This is WingetUI's official repository. If you are searching for WingetUI's homepage, please refer to [https://www.marticliment.com/wingetui/](https://www.marticliment.com/wingetui/)**

**Disclaimer:** This project has no connection with any of the supported package managers — it's completely unofficial. Be aware of the fact that I, the developer of WingetUI, am NOT responsible for the downloaded apps.

![Endpoint Badge](https://img.shields.io/endpoint?url=https%3A%2F%2Fmarticliment.com%2Fresources%2Fbadges%2Fdev-status.json)
 
## Table of contents
 - **[WingetUI Homepage](https://www.marticliment.com/wingetui/)**
 - [Table of contents](#table-of-contents)
 - [Installation](#installation)
 - [Update WingetUI](#update-wingetui)
 - [Support the developer](#support-the-developer)
 - [Features](#features)
   - [Supported Package Managers](#supported-package-managers)
 - [Translating WingetUI](#translating-wingetui-to-other-languages)
   - [Currently supported languages](#currently-supported-languages)
 - [Contributors](#contributors)
 - [Screenshots](#screenshots)
 - [Frequently Asked Questions](#frequently-asked-questions)

 
## Installation
<p>There are multiple ways to install WingetUI — choose whichever one you prefer!<br</p>

**Download WingetUI installer (recommended):**
<p align="left"><b><a href="https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe">Click here to download WingetUI</a></b></p>

**Install WingetUI through Winget:**    
```cmd
winget install SomePythonThings.WingetUIStore
```

**Install WingetUI through Scoop:**
```cmd
scoop bucket add extras
```
```cmd
scoop install extras/wingetui
```

**Install WingetUI through Chocolatey:**    
```cmd
choco install wingetui
```


## Update WingetUI

WingetUI has a built-in autoupdater. However, it can also be updated like any other package within WingetUI (since WingetUI is available through Winget and Scoop).


## Support the developer

It really does make a big difference, and is very much appreciated. Thanks :)

<a href='https://ko-fi.com/martinet101' target='_blank'><img style='border:0px;height:36px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>


## Features

 - Install, update and remove software from your system easily at one click: WingetUI combines the packages from the most used package managers for windows: Winget, Chocolatey, Scoop, Pip, Npm and .NET Tool.
 - Discover new packages and filter them to easily find the package you want.
 - View detailed metadata about any package before installing it. Get the direct download URL or the name of the publisher, as well as the size of the download.
 - Easily bulk-install, update or uninstall multiple packages at once selecting multiple packages before performing an operation
 - Automatically update packages, or be notified when updates become available. Skip versions or completely ignore updates in a per-package basis.
 - Manage your available updates at the touch of a button from the **Widgets pane** or from **Dev Home** pane with [WingetUI Widgets](https://apps.microsoft.com/detail/9NB9M5KZ8SLX)*.
 - The system tray icon will also show the available updates and installed package, to efficiently update a program or remove a package from your system.
 - Easily customize how and where packages are installed. Select different installation options and switches for each package. Install an older version or force to install a 32bit architecture. \[But don't worry, those options will be saved for future updates for this package*]
 - Share packages with your friends to show them off that program you found. Here is an example: [Hey \@friend, Check out this program!](https://marticliment.com/wingetui/share/?pname=Google%20Chrome&pid=Google.Chrome&psource=Winget:%20winget)
 - Export custom lists of packages to then import them to another machine and install those packages with previously-specified, custom installation parameters. Setting up machines or configuring a specific software setup has never been easier.
 - Backup your packages to a local file to easily recover your setup in a matter of seconds when migrating to a new machine*

## Supported Package Managers

**NOTE:** All package managers do support basic install, update and uninstall processes, as well as checking for updates, finding new packages and retrieving details from a package.

| Manager | Skip integrity checks | Interactive installation | Install Older Versions | Install a PreRelease Version | Install a Custom Architecture | Install on a Custom Scope | Custom Install Location | Custom Package Sources | Supported since |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Winget** | ✅ | ⚠️³ | ✅ | ☑️² | ✅ | ⚠️¹ | ⚠️¹ | ✅ | 0.1.0 |
| **Scoop** | ✅ | ❌ | ❌ | ☑️² | ✅ | ✅ | ❌ | ✅ | 0.1.0 |
| **Chocolatey** | ✅ | ⚠️³ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | 1.6.0 |
| **Npm** | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | 2.0.0 |
| **Pip** | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | 2.0.0 |
| **.NET Tool** | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | 2.1.0 |
| **PowerShell** | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ | ✅ | 2.2.0 |

✅: Supported on WingetUI<br>
☑️: Not directly supported but can be easily achieved<br>
⚠️: Some packages might not follow this setting<br>
❌: Not supported by the Package Manager<br>
<br>
**1\.** Some packages do not support installing to a custom location or scope and will ignore this setting<br>
**2\.** Despite the Package Manager may not support _PreReleases_, some packages can be found duplicated, with one of the copies being the beta version of it.<br>
**3\.** Some installers do not have a GUI, and will ignore the `interactive` flag<br>

# Translating WingetUI to other languages
In order to translate WingetUI to other languages or to update an old translation, please see [Translating WingetUI - WingetUI Wiki](https://github.com/marticliment/WingetUI/wiki#translating-wingetui) for more info.


## Currently Supported languages
<!-- Autogenerated translations -->
| Language | Translated | Translator(s) |
| :-- | :-- | --- |

Last updated: Mon Mar  4 00:10:48 2024
<!-- END Autogenerated translations -->


# Contributions
 WingetUI wouldn't have been possible without the help of our dear contributors. From the person who fixed a typo to the person who improved half of the code, WingetUI wouldn't be possible without them! :smile:<br><br>

## Contributors:
 [![My dear contributors](https://contrib.rocks/image?repo=marticliment/WingetUI)](https://github.com/marticliment/WingetUI/graphs/contributors)<br><br>
 

# Screenshots
 
![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_1.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_2.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_3.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_4.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_5.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_6.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_7.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_8.png)

![image](https://raw.githubusercontent.com/marticliment/WingetUI/main/media/winget_9.png)


# Frequently asked questions

**Q: I am unable to install or upgrade a specific Winget package! What should I do?**<br>

A: This is likely an issue with Winget rather than WingetUI. 

Please check if it's possible to install/upgrade the package through PowerShell or the Command Prompt by using the commands `winget upgrade` or `winget install`, depending on the situation (for example: `winget upgrade --id Microsoft.PowerToys`). 

If this doesn't work, consider asking for help at [Winget's own project page](https://github.com/microsoft/winget-cli).<br>

#

**Q: The name of a package is trimmed with ellipsis — how do I see its full name/id?**<br>

A: This is a known limitation of Winget. 

See more details in issue https://github.com/microsoft/winget-cli/issues/2603.<br>

#

**Q: My antivirus is telling me that WingetUI is a virus! / My browser is blocking the download of WingetUI!**<br>

A: A common reason apps (i.e., executables) get blocked and/or detected as a virus — even when there's nothing malicious about them, like in the case of WingetUI — is because they're not being used by a relatively large amount of people.

Combine that with the fact that you might be downloading something that was recently released, and simply blocking unknown apps is in many cases a good precaution to take in order to prevent actual malware.

Since WingetUI is open source and safe to use, simply whitelist the app in the settings of your antivirus/browser.<br>

#

**Q: Are Winget/Scoop packages safe?**<br>

A: WingetUI, Microsoft and Scoop aren't responsible for the packages available for download, which are provided by third parties and can theoretically be compromised.

To mitigate the risks of downloading malware, Microsoft has implemented a few checks for the software available on Winget. Even so, It's recommended to only download software from publishers that you trust. 

<br><p align="center"><i>Check out the <a href="https://github.com/marticliment/WingetUI/wiki">Wiki</a> for more information!</i></p>

## Command-line parameters:
`--daemon`: Start WingetUI without spawnign a new window. WingetUI will run minimized on the system tray. WingetUI is called with this parameter when launched at startup. **Autostart WingetUI in the notifications area** must be enabled in order for this paramater to work.<br>
`--welcomewizard` (or simply `--welcome`): Show a window to choose which package managers to use.<br>
`--updateapps`: Enable automatic install of available updates.

### Installer command-line parameters:
The installer is inno-setup based, so it supports regular Inno Setup command-line parameters. Additionally, it does also support the following ones:
 <br>`/NoAutoStart`: Will not launch WingetUI after installing it.
<br> `/ALLUSERS`: Install WingetUI for every user
<br> `/CURRENTUSER`: Install WingetUI for the current user only
