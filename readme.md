# <img src="https://raw.githubusercontent.com/marticliment/WingetUI/main/wingetui/resources/icon.png" height="40">WingetUI

[![Downloads@latest](https://img.shields.io/github/downloads/marticliment/WingetUI/2.0.3/total?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe)
[![Release Version Badge](https://img.shields.io/github/v/release/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/releases)
[![Issues Badge](https://img.shields.io/github/issues/marticliment/WingetUI?style=for-the-badge)](https://github.com/marticliment/WingetUI/issues)
[![Closed Issues Badge](https://img.shields.io/github/issues-closed/marticliment/WingetUI?color=%238256d0&style=for-the-badge)](https://github.com/marticliment/WingetUI/issues?q=is%3Aissue+is%3Aclosed)<br>

The main goal of this project is to create an intuitive GUI for the most common CLI package managers for Windows 10 and Windows 11, such as [Winget](https://learn.microsoft.com/en-us/windows/package-manager/), [Scoop](https://scoop.sh/), [Chocolatey](https://chocolatey.org/), [Pip](https://pypi.org/), [Npm](https://www.npmjs.com/) and [.NET Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install)
With this app, you'll be able to easily download, install, update and uninstall any software that's published on the supported package managers — and so much more.

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
winget install wingetui
```

**Install WingetUI through Scoop:**
```cmd
scoop bucket add extras
```
```cmd
scoop install extras/wingetui
```

_Available soon on Chocolatey (hopefully)._

## Update WingetUI

WingetUI has a built-in autoupdater. However, it can also be updated like any other package within WingetUI (since WingetUI is available through Winget and Scoop).


## Support the developer

It really does make a big difference, and is very much appreciated. Thanks :)

<a href='https://ko-fi.com/martinet101' target='_blank'><img style='border:0px;height:36px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>


## Features

 - WingetUI has the ability to install, update and uninstall packages from Winget (including Microsoft Store), Scoop, Chocolatey, pip, npm and .NET Tool. WingetUI will also detect if your manually-installed apps can be updated!
 - It can also upgrade and uninstall previously installed packages — as well as uninstall built-in Windows apps!
 - WingetUI has the ability to both import and export the packages of your choice, so that you can easily install them in the future.
 - WingetUI supports managing Scoop buckets with an interface.
 - Install an older version of an app.
 - WingetUI shows a notification when there are available updates
 - Manage your updates and installed packages from its context menu
 - The user will be notified whether the installation/update/uninstallation of an app was completed successfully or not.
 - The ability to queue installations in order to prevent conflicts.
 - A dark theme is available to prevent you from burning your eyes. :sunglasses:
 - WingetUI has the ability to show package-related information (like its license, SHA256 hash, homepage, etc.) before installation.
 - There are more than 14000 packages available (if Winget, Scoop and Chocolatey are enabled)!
 - More features coming in the future!

# Translating WingetUI to other languages
In order to translate WingetUI to other languages or to update an old translation, please see [Translating WingetUI - WingetUI Wiki](https://github.com/marticliment/WingetUI/wiki#translating-wingetui) for more info.


## Currently Supported languages
<!-- Autogenerated translations -->
| Language | Translated | Translator(s) |
| :-- | :-- | --- |
| <img src='https://flagcdn.com/sa.svg' width=20> &nbsp; Arabic - عربي‎ | 94% | [Abdu11ahAS](https://github.com/Abdu11ahAS), [FancyCookin](https://github.com/FancyCookin), [mo9a7i](https://github.com/mo9a7i) |
| <img src='https://flagcdn.com/bg.svg' width=20> &nbsp; Bulgarian - български | 47% | Vasil Kolev |
| <img src='https://flagcdn.com/bd.svg' width=20> &nbsp; Bangla - বাংলা | 19% | [fluentmoheshwar](https://github.com/fluentmoheshwar), Mushfiq Iqbal Rayon, Nilavra Bhattacharya |
| <img src='https://flagcdn.com/ad.svg' width=20> &nbsp; Catalan - Català | 100% | [marticliment](https://github.com/marticliment) |
| <img src='https://flagcdn.com/cz.svg' width=20> &nbsp; Czech - Čeština | 95% | [panther7](https://github.com/panther7) |
| <img src='https://flagcdn.com/dk.svg' width=20> &nbsp; Danish - Dansk | 30% | [mikkolukas](https://github.com/mikkolukas) |
| <img src='https://flagcdn.com/de.svg' width=20> &nbsp; German - Deutsch | 95% | [CanePlayz](https://github.com/CanePlayz), [Datacra5H](https://github.com/Datacra5H), [ebnater](https://github.com/ebnater), [michaelmairegger](https://github.com/michaelmairegger), [Seeloewen](https://github.com/Seeloewen) |
| <img src='https://flagcdn.com/gr.svg' width=20> &nbsp; Greek - Ελληνικά | 76% | [antwnhsx](https://github.com/antwnhsx) |
| <img src='https://flagcdn.com/gb.svg' width=20> &nbsp; English - English | 100% | [marticliment](https://github.com/marticliment), [ppvnf](https://github.com/ppvnf) |
| <img src='https://flagcdn.com/es.svg' width=20> &nbsp; Spanish - Castellano | 95% | [apazga](https://github.com/apazga), [dalbitresb12](https://github.com/dalbitresb12), [evaneliasyoung](https://github.com/evaneliasyoung), [guplem](https://github.com/guplem), [JMoreno97](https://github.com/JMoreno97), [marticliment](https://github.com/marticliment), [rubnium](https://github.com/rubnium) |
| <img src='https://flagcdn.com/ir.svg' width=20> &nbsp; Persian - فارسی‎ | 71% | [itsarian](https://github.com/itsarian), [Mahdi-Hazrati](https://github.com/Mahdi-Hazrati), [smsi2001](https://github.com/smsi2001) |
| <img src='https://flagcdn.com/fr.svg' width=20> &nbsp; French - Français | 100% | Evans Costa, Rémi Guerrero, [W1L7dev](https://github.com/W1L7dev) |
| <img src='https://flagcdn.com/in.svg' width=20> &nbsp; Hindi - हिंदी | 87% | [atharva_xoxo](https://github.com/atharva_xoxo), [satanarious](https://github.com/satanarious) |
| <img src='https://flagcdn.com/hr.svg' width=20> &nbsp; Croatian - Hrvatski | 92% | Stjepan Treger |
| <img src='https://flagcdn.com/il.svg' width=20> &nbsp; Hebrew - עִבְרִית‎ | 67% |  |
| <img src='https://flagcdn.com/hu.svg' width=20> &nbsp; Hungarian - Magyar | 100% | [gidano](https://github.com/gidano) |
| <img src='https://flagcdn.com/it.svg' width=20> &nbsp; Italian - Italiano | 100% | David Senoner, [giacobot](https://github.com/giacobot), [maicol07](https://github.com/maicol07), [mapi68](https://github.com/mapi68), [mrfranza](https://github.com/mrfranza), Rosario Di Mauro |
| <img src='https://flagcdn.com/id.svg' width=20> &nbsp; Indonesian - Bahasa Indonesia | 95% | [arthackrc](https://github.com/arthackrc), [joenior](https://github.com/joenior) |
| <img src='https://flagcdn.com/jp.svg' width=20> &nbsp; Japanese - 日本語 | 92% | [nob-swik](https://github.com/nob-swik), sho9029, Yuki Takase |
| <img src='https://flagcdn.com/kr.svg' width=20> &nbsp; Korean - 한국어 | 95% | [minbert](https://github.com/minbert), [shblue21](https://github.com/shblue21) |
| <img src='https://flagcdn.com/no.svg' width=20> &nbsp; Norwegian (bokmål) | 80% | [jomaskm](https://github.com/jomaskm) |
| <img src='https://flagcdn.com/nl.svg' width=20> &nbsp; Dutch - Nederlands | 100% | [abbydiode](https://github.com/abbydiode), [Stephan-P](https://github.com/Stephan-P) |
| <img src='https://flagcdn.com/pl.svg' width=20> &nbsp; Polish - Polski | 99% | [KamilZielinski](https://github.com/KamilZielinski), [kwiateusz](https://github.com/kwiateusz), [RegularGvy13](https://github.com/RegularGvy13) |
| <img src='https://flagcdn.com/br.svg' width=20> &nbsp; Portuguese (Brazil) | 100% | [maisondasilva](https://github.com/maisondasilva), [ppvnf](https://github.com/ppvnf), [wanderleihuttel](https://github.com/wanderleihuttel) |
| <img src='https://flagcdn.com/pt.svg' width=20> &nbsp; Portuguese (Portugal) | 100% | [PoetaGA](https://github.com/PoetaGA), [ppvnf](https://github.com/ppvnf) |
| <img src='https://flagcdn.com/ru.svg' width=20> &nbsp; Russian - Русский | 95% | [bropines](https://github.com/bropines), [flatron4eg](https://github.com/flatron4eg), [katrovsky](https://github.com/katrovsky), Sergey, [sklart](https://github.com/sklart) |
| <img src='https://flagcdn.com/rs.svg' width=20> &nbsp; Serbian - Srpski | 10% | Nemanja Djurcic |
| <img src='https://flagcdn.com/si.svg' width=20> &nbsp; Slovene - Slovenščina | 95% | [rumplin](https://github.com/rumplin) |
| <img src='https://flagcdn.com/ph.svg' width=20> &nbsp; Tagalog - Tagalog | 22% | lasersPew |
| <img src='https://flagcdn.com/th.svg' width=20> &nbsp; Thai - ภาษาไทย | 100% | [apaeisara](https://github.com/apaeisara), [dulapahv](https://github.com/dulapahv) |
| <img src='https://flagcdn.com/tr.svg' width=20> &nbsp; Turkish - Türkçe | 91% | [ahmetozmtn](https://github.com/ahmetozmtn), [gokberkgs](https://github.com/gokberkgs) |
| <img src='https://flagcdn.com/ua.svg' width=20> &nbsp; Ukranian - Yкраї́нська | 91% | Artem Moldovanenko, Operator404 |
| <img src='https://flagcdn.com/vn.svg' width=20> &nbsp; Vietnamese - Tiếng Việt | 95% | [txavlog](https://github.com/txavlog) |
| <img src='https://flagcdn.com/cn.svg' width=20> &nbsp; Simplified Chinese (China) | 99% | Aaron Liu, [adfnekc](https://github.com/adfnekc), BUGP Association, ciaran, CnYeSheng, Cololi |
| <img src='https://flagcdn.com/tw.svg' width=20> &nbsp; Traditional Chinese (Taiwan) | 91% | Aaron Liu, CnYeSheng, Cololi, [yrctw](https://github.com/yrctw) |

Last updated: Fri Oct  6 00:48:13 2023
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
`--daemon`: Start WingetUI without spawnign a new window. WingetUI will run minimized on the system tray. WingetUI is called with this parameter when launched at startup.<br>
`--welcomewizard` (or simply `--welcome`): Show a window to choose which package managers to use.<br>
`--updateapps`: Enable automatic install of available updates.

### Installer command-line parameters:
The installer is inno-setup based, so it supports regular Inno Setup command-line parameters. Additionally, it does also support the following ones:
 <br>`/NoAutoStart`: Will not launch WingetUI after installing it.
<br> `/ALLUSERS`: Install WingetUI for every user
<br> `/CURRENTUSER`: Install WingetUI for the current user only
