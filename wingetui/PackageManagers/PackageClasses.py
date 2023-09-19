"""

wingetui/PackageManagers/PackageClasses.py

This file holds the classes related to Packages and PackageManagers .

"""

import subprocess

import PySide6.QtCore
import PySide6.QtWidgets
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import _, blueColor, GetIgnoredPackageUpdates_Permanent


class Package():
    Name: str = ""
    Id: str = ""
    Version: str = ""
    Source: str = ""
    PackageItem: QTreeWidgetItem = None
    PackageManager: 'PackageManagerModule' = None

    def __init__(self, Name: str, Id: str, Version: str, Source: str, PackageManager: 'PackageManagerModule'):
        self.Name = Name
        self.Id = Id
        self.Version = Version
        self.Source = Source
        self.PackageManager = PackageManager

    def isWinget(self) -> bool:
        return "winget" in self.Source.lower() 

    def isScoop(self) -> bool:
        return "scoop" in self.Source.lower()

    def isChocolatey(self) -> bool:
        return self.Source.lower() == "chocolatey"

    def getIconId(self) -> str:
        iconId = self.Id.lower()
        if self.isWinget():
            iconId = ".".join(iconId.split(".")[1:])
        elif self.isChocolatey():
            iconId = iconId.replace(".install", "").replace(".portable", "")
        elif self.isScoop():
            iconId = iconId.split("/")[-1]
        return iconId.replace(" ", "-").replace("_", "-").replace(".", "-")

    def getSourceIcon(self) -> QIcon:
        return self.PackageManager.getIcon(self.Source)

    def isManager(self, manager: 'PackageManagerModule') -> bool:
        """
        Check if the package manager equals the given package manager
        """
        return manager == self.PackageManager

    def getFloatVersion(self) -> float:
        newver = ""
        dotAdded = False
        for char in self.Version:
            if char in "0123456789":
                newver += char
            elif char == ".":
                if not dotAdded:
                    newver += "."
                    dotAdded = True
        if newver and newver != ".":
            strver = f"{float(newver):040.10f}"
        else:
            strver = f"{0.0:040.10f}"
        return strver

    def isTheSameAs(self, package: 'Package'):
        return self.Id == package.Id and self.Name == package.Name and package.Source == package.Source and package.PackageManager == package.PackageManager

    def __str__(self) -> str:
        return f"<Package: {self.Name};{self.Id};{self.Version};{self.Source};{self.PackageManager};{self.PackageItem}>"
    
    def hasUpdatesIgnoredPermanently(self) -> bool:
        return [self.Id, self.Source.lower().split(":")[0]] in GetIgnoredPackageUpdates_Permanent()

class UpgradablePackage(Package):
    NewVersion = ""
    NewPackage: Package = None

    def __init__(self, Name: str, Id: str, InstalledVersion: str, NewVersion: str, Source: str, PackageManager: 'PackageManagerModule'):
        super().__init__(Name, Id, InstalledVersion, Source, PackageManager)
        self.NewVersion = NewVersion
        self.NewPackage = Package(Name, Id, NewVersion, Source, PackageManager)

class PackageDetails(Package):
    Name: str = ""
    Id: str = ""
    Version: str = ""
    NewVersion: str = ""
    Source: str = ""
    PackageObject: Package = None
    Publisher: str = _("Not available")
    Author: str = _("Not available")
    Description: str = _("Not available")
    HomepageURL: str = _("Not available")
    License: str = ""
    LicenseURL: str = ""
    InstallerURL: str = _("Not available")
    InstallerHash: str = _("Not available")
    InstallerSize: int = 0 # In Megabytes
    InstallerType: str = _("Not available")
    ManifestUrl: str = _("Not available")
    UpdateDate: str = _("Not available")
    ReleaseNotes: str = _("Not available")
    ReleaseNotesUrl: str = _("Not available")
    Versions: list[str] = []
    Architectures: list[str] = []
    Scopes: list[str] = []
    Tags: list[str] = []

    def __init__(self, package: Package):
        self.Name = package.Name
        self.Id = package.Id
        self.Version = package.Version
        self.Source = package.Source
        self.PackageObject = package
        if type(package) == UpgradablePackage:
            self.NewVersion = package.NewVersion

    def asUrl(self, url: str) -> str:
        return f"<a href='{url}' style='color:{blueColor}'>{url}</a>" if "://" in url else url

class InstallationOptions():
    SkipHashCheck: bool = False
    InteractiveInstallation: bool = False
    RunAsAdministrator: bool = False
    Version: str = ""
    Architecture: str = ""
    InstallationScope: str = ""
    CustomParameters: list[str] = []
    RemoveDataOnUninstall: bool = False

    def __str__(self) -> str:
        str = f"<InstallationOptions: SkipHashCheck={self.SkipHashCheck};"
        str += f"InteractiveInstallation={self.InteractiveInstallation};"
        str += f"RunAsAdministrator={self.RunAsAdministrator};"
        str += f"Version={self.Version};"
        str += f"Architecture={self.Architecture};"
        str += f"InstallationScope={self.InstallationScope};"
        str += f"CustomParameters={self.CustomParameters};"
        str += f"RemoveDataOnUninstall={self.RemoveDataOnUninstall}>"
        return str

class PackageManagerCapabilities():
    CanRunAsAdmin: bool = False
    CanSkipIntegrityChecks: bool = False
    CanRunInteractively: bool = False
    CanRemoveDataOnUninstall: bool = False
    SupportsCustomVersions: bool = False
    SupportsCustomArchitectures: bool = False
    SupportsCustomScopes: bool = False

class InstallationWidgetType(QWidget):
    finishInstallation: Signal
    addInfoLine: Signal
    counterSignal: Signal

    def __init__(self) -> None:
        raise RuntimeError("This class is a type declaration!")

class PackageManagerModule():
    NAME: str
    Capabilities: PackageManagerCapabilities
    LoadedIcons: bool
    def __init__(self):
        pass

    def isEnabled() -> bool:
        pass

    def getAvailablePackages(self) -> list[Package]:
        f"""
        Will retieve the cached packages for the package manager  in the format of a list[Package] object.
        If the cache is empty, will forcefully cache the packages and return a valid list[Package] object.
        Finally, it will start a background cacher thread.
        """

    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by the package manager in the format of a list[UpgradablePackage] object.
        """

    def getInstalledPackages(self) -> list[Package]:
        f"""
        Will retieve the intalled packages by the package manager in the format of a list[Package] object.
        """

    def getIcon(self, source: str) -> QIcon:
        """
        Will return the corresponding icon to the given source
        """

    def getPackageDetails(self, package: Package):
        """
        Will return a PackageDetails object containing the information of the given Package object
        """

    def startInstallation(self, package: Package, options: InstallationOptions, installationWidget: InstallationWidgetType) -> subprocess.Popen:
        """
        Starts a thread that installs the specified Package, making use of the given options. Reports the progress through the given InstallationWidget
        """

    def startUpdate(self, package: Package, options: InstallationOptions, installationWidget: InstallationWidgetType) -> subprocess.Popen:
        """
        Starts a thread that updates the specified Package, making use of the given options. Reports the progress through the given InstallationWidget
        """

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, installationWidget: InstallationWidgetType):
        """
        Internal method that handles the installation of the given package
        """

    def startUninstallation(self, package: Package, options: InstallationOptions, installationWidget: InstallationWidgetType) -> subprocess.Popen:
        """
        Starts a thread that removes the specified Package, making use of the given options. Reports the progress through the given InstallationWidget
        """

    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, installationWidget: InstallationWidgetType):
        """
        Internal method that handles the removal of the given package
        """

    def getParameters(self, options: InstallationOptions) -> list[str]:
        """
        Returns the list of parameters that the package manager ib nasis of the given InstallationOptions object
        """

    def detectManager(self, signal: Signal = None) -> None:
        """
        Detect if the package manager components exist.
        """

    def updateSources(self, signal: Signal = None) -> None:
        """
        Force update package manager's sources
        """

class DynamicPackageManager(PackageManagerModule):

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """



RETURNCODE_OPERATION_SUCCEEDED = 0
RETURNCODE_NO_APPLICABLE_UPDATE_FOUND = 92849
RETURNCODE_NEEDS_RESTART = 3

LIST_RETURNCODES_OPERATION_SUCCEEDED = (RETURNCODE_OPERATION_SUCCEEDED, RETURNCODE_NO_APPLICABLE_UPDATE_FOUND, RETURNCODE_NEEDS_RESTART)

RETURNCODE_FAILED = 1
RETURNCODE_INCORRECT_HASH = 2
RETURNCODE_NEEDS_ELEVATION = 1603
RETURNCODE_NEEDS_SCOOP_ELEVATION = -200
RETURNCODE_NEEDS_PIP_ELEVATION = -100

class BlacklistMethod():
    Legacy = 0
    SpecificVersion = 1
    AllVersions = 2
