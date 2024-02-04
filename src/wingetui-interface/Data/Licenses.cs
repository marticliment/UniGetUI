using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace ModernWindow.Data
{
    public static class LicenseData
    {
        public static Dictionary<string, string> LicenseNames = new Dictionary<string, string>() {
            {"WingetUI",                "LGPL v2.1" },
            {"Python 3",                "PSF"},
            {"PySide6",                 "LGPLv3"},
            {"PyWin32",                 "PSF"},
            {"Win32mica",               "MIT"},
            {"PyInstaller",             "GPL 2.0"},
            {"Pythonnet",               "MIT"},
            {"Flask",                   "BSD-3-Clause"},
            {"Flask-Cors",              "MIT"},
            {"Waitress",                "ZPL 2.1"},
            {"PyYaml",                  "MIT"},
            {"Windows Toasts",          "Apache v2"},
            {"Winget",                  "MIT"},
            {"Scoop",                   "MIT"},
            {"Scoop Search",            "MIT"},
            {"Chocolatey",              "Apache v2"},
            {"Npm",                     "Artistic License 2.0"},
            {"Pip",                     "MIT"},
            {"Parse Pip Search",        "MIT"},
            {"PowerShell Gallery",      "Unknown"},
            {"Dotnet Tool",             "Free (Proprietary license)"},
            {"dotnet-tools-outdated",   "MIT"},
            {"Gsudo",                   "MIT"},
            {"Icons",                   "By Icons8"},
        };

        public static Dictionary<string, Uri> LicenseURLs = new Dictionary<string, Uri>(){
            {"WingetUI",           new Uri("https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html")},
            {"Python 3",           new Uri("https://docs.python.org/3/license.html#psf-license")},
            {"PySide6",            new Uri("https://www.tldrlegal.com/license/gnu-lesser-general-public-license-v3-lgpl-3")},
            {"PyWin32",            new Uri("https://docs.python.org/3/license.html#psf-license")},
            {"Win32mica",          new Uri("https://github.com/marticliment/win32mica/blob/main/LICENSE")},
            {"PyInstaller",        new Uri("https://pyinstaller.org/en/stable/license.html")},
            {"Pythonnet",          new Uri("https://github.com/pythonnet/pythonnet/blob/master/LICENSE")},
            {"Windows Toasts",     new Uri("https://github.com/DatGuy1/Windows-Toasts/blob/main/LICENSE")},
            {"Winget",             new Uri("https://github.com/microsoft/winget-cli/blob/master/LICENSE")},
            {"Scoop",              new Uri("https://github.com/ScoopInstaller/Scoop/blob/master/LICENSE")},
            {"Scoop Search",       new Uri("https://github.com/shilangyu/scoop-search/blob/master/LICENSE")},
            {"Chocolatey",         new Uri("https://github.com/chocolatey/choco/blob/develop/LICENSE")},
            {"Npm",                new Uri("https://github.com/npm/cli/blob/latest/LICENSE")},
            {"Pip",                new Uri("https://github.com/pypa/pip/blob/main/LICENSE.txt")},
            {"Parse Pip Search",   new Uri("https://github.com/marticliment/parseable_pip_search/blob/master/LICENSE.md")},
            {"Dotnet Tool",        new Uri("https://dotnet.microsoft.com/en-us/platform/free")},
            {"dotnet-tools-outdated", new Uri("https://github.com/rychlym/dotnet-tools-outdated/blob/master/LICENSE")},
            {"Gsudo",              new Uri("https://github.com/gerardog/gsudo/blob/master/LICENSE.txt")},
            {"Icons",              new Uri("https://icons8.com")},
            {"Flask",              new Uri("https://flask.palletsprojects.com/en/latest/license/")},
            {"Flask-Cors",         new Uri("https://github.com/corydolphin/flask-cors/blob/main/LICENSE")},
            {"Waitress",           new Uri("https://github.com/Pylons/waitress/blob/main/LICENSE.txt")},
            {"PyYaml",             new Uri("https://github.com/yaml/pyyaml/blob/main/LICENSE")},
            {"PowerShell Gallery", new Uri("https://www.powershellgallery.com/")},
        };

    }
}
