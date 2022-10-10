# WingetUI: A package manager UI for Winget and Scoop

[![Downloads@latest](https://img.shields.io/github/downloads/martinet101/WingetUI/total?style=for-the-badge)](https://github.com/martinet101/WingetUI/releases/latest/download/WingetUI.Installer.exe)
[![Release Version Badge](https://img.shields.io/github/v/release/martinet101/WingetUI?style=for-the-badge)](https://github.com/martinet101/WingetUI/releases)
[![Issues Badge](https://img.shields.io/github/issues/martinet101/WingetUI?style=for-the-badge)](https://github.com/martinet101/WingetUI/issues)
[![Closed Issues Badge](https://img.shields.io/github/issues-closed/martinet101/WingetUI?color=%238256d0&style=for-the-badge)](https://github.com/martinet101/WingetUI/issues?q=is%3Aissue+is%3Aclosed)

The main goal of this project is to create a GUI Store for the most common CLI package managers for Windows, such as Winget and Scoop. 

From here, you'll be able to download, install, upgrade and uninstall any software published on Winget or Scoop.

AppGet was supported in release 0.3, but since that project has been discontinued, the support has been removed.

This project has no connection to the official Winget-CLI project — it's completely unofficial.

[![Status](https://img.shields.io/badge/Project%20current%20development%20status-Active-brightgreen?style=for-the-badge)]()

# And you know what? It can uninstall MS Edge
(or at least it did in my machine)


![ezgif-3-901ac5902a](https://user-images.githubusercontent.com/53119851/169247775-e02ed0b1-ba34-4552-966a-676979d89925.png)


# Features

 - The ability to install packages from Scoop and Winget (the idea is to add more package managers in the future).
 
 - The ability to update and uninstall previously installed packages.
 - The user doesn't need to install any of the package managers.
 - Smooth and responsive UI (starting from v1.0).
 - Support for managing Scoop buckets.
 - The user can select the version that should be installed for any of the apps.
 - The user will be notified whether the installation/upgrade/uninstallation was completed successfully or not.
 - The ability to queue installations in order to prevent conflicts.
 - A dark theme is available to prevent you from burning your eyes. :sunglasses:
 - The ability to show some package-related information (like license, SHA256 hash, homepage, etc.) before installing.
 - 5800+ packages available to install, such as Google Chrome, WhatsApp, Adobe Reader or ADB Tools!
 - More features are coming!

# Support me :)

<a href='https://ko-fi.com/martinet101' target='_blank'><img style='border:0px;height:36px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

# Installation
 
It's easy! Download and install the latest version of WingetUI by clicking [here](https://github.com/martinet101/WingetUI/releases/latest/download/WingetUI.Installer.exe)!

You can also install WingetUI using [Winget-CLI](https://learn.microsoft.com/en-us/windows/package-manager/): `winget install wingetui`

You can install the app through [Scoop](https://scoop.sh/) as well (⚠️might cause issues, please install manually or through Winget-CLI for the moment).

To install it that way, first it's necessary to add the Extras bucket: `scoop bucket add extras`

Then, execute the following in a CLI: `scoop install wingetui`

<br><p align="center"><i>(See the <a href="https://github.com/martinet101/WingetUI/wiki">WIKI</a> for more information)</i></p>

# Screenshots
 
![alt text](/media/winget_1.png)

![alt text](/media/winget_2.png)

![alt text](/media/winget_3.png)

![alt text](/media/winget_4.png)

![alt text](/media/winget_6.png)

![alt text](/media/winget_5.png)

![alt text](/media/winget_7.png)

# FAQ

**Q: I am unable to install or update some Winget package**<br>
A: This is likely a Winget-CLI issue. Please check if it is possible to install/update the package through PowerShell or cmd using the commands `winget upgrade` and `winget install` (for example: `winget upgrade --id Microsoft.PowerToys`). If this doesn't work, you may try to get help at https://github.com/microsoft/winget-pkgs<br>

**Q: I am unable to fully see some package name/id (trimmed with ellipsis)**<br>
A: This is a known Winget-CLI limitation. See more details at https://github.com/martinet101/WingetUI/issues/196<br>

**Q: Can WingetUI be in my language?**<br>
A: Not yet. See more details at https://github.com/martinet101/WingetUI/issues/67<br>

**Q: My antivirus is telling me that WingetUI is a virus/My antivirus is uninstalling WingetUI/My browser is blocking WingetUI download**<br>
A: Just whitelist WingetUI on the antivirus quarantine box/antivirus settings.<br>

**Q: Will Chocolatey be supported?**<br>
A: Maybe in the future. See more details at https://github.com/martinet101/WingetUI/issues/56<br>

**Q: Can I add "msstore" as a source for Winget?**<br>
A: This is not possible nor planned for the near future. See more details at https://github.com/martinet101/WingetUI/issues/87<br>

<br><p align="center"><i>(See the <a href="https://github.com/martinet101/WingetUI/wiki">WIKI</a> for more information)</i></p>
