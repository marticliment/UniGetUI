if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


from wingetui.PackageEngine.Managers.choco import Choco
from wingetui.PackageEngine.Managers.npm import Npm
from wingetui.PackageEngine.Classes import *
from wingetui.PackageEngine.Managers.pip import Pip
from wingetui.PackageEngine.Managers.scoop import Scoop
from wingetui.PackageEngine.Managers.winget import Winget
from wingetui.PackageEngine.Managers.dotnet import Dotnet

PackageManagersList: list[PackageManagerModule] = [
    Winget,
    Scoop,
    Choco,
    Pip,
    Npm,
    Dotnet
]

PackagesLoadedDict: dict[PackageManagerModule:bool] = {
    Winget: False,
    Scoop: False,
    Choco: False,
    Pip: False,
    Npm: False,
    Dotnet: False
}

DynaimcPackageManagersList: list[PackageManagerModule] = [
    Pip,
    Npm,
    Choco,
    Winget,
    Scoop,
    Dotnet
]

DynamicPackagesLoadedDict: dict[PackageManagerModule:bool] = {
    Pip: False,
    Npm: False,
    Winget: False,
    Choco: False,
    Scoop: False,
    Dotnet: False
}
