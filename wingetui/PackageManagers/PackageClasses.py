from tools import _, blueColor
from customWidgets import TreeWidgetItemWithQAction
from PySide6.QtCore import *
from PySide6.QtGui import *

class Package():
    Name: str = ""
    Id: str = ""
    Version: str = ""
    Source: str = ""
    PackageItem: TreeWidgetItemWithQAction = None
    PackageManager: 'PackageManagerModule' = None
    
    def __init__(self, Name: str, Id: str, Version: str, Source: str, PackageManager: 'PackageManagerModule'):
        self.Name = Name
        self.Id = Id
        self.Version = Version
        self.Source = Source
        self.PackageManager = PackageManager
        
    def isWinget(self) -> bool:
        return self.Source.lower() == "winget"
        
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
        #if self.PackageItem:
        return self.PackageManager.getIcon(self.Source)
        #print(f"🔴 Null module for package {self.Id}")
        #return QIcon()
        
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
    InstallerSize: int = 0
    InstallerType: str = _("Not available")
    ManifestUrl: str = _("Not available")
    UpdateDate: str = _("Not available")
    ReleaseNotes: str = _("Not available")
    ReleaseNotesUrl: str = _("Not available")
    Versions: list[str] = []
    Architectures: list[str] = []
    Scopes: list[str] = []
    
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
    
class PackageManagerModule():

    def __init__(self):
        pass
    
    def isEnabled() -> bool:
        pass
    
    def getAvailablePackages_v2(self) -> list[Package]:
        f"""
        Will retieve the cached packages for the package manager  in the format of a list[Package] object.
        If the cache is empty, will forcefully cache the packages and return a valid list[Package] object.
        Finally, it will start a background cacher thread.
        """
            
    def getAvailableUpdates_v2(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by the package manager in the format of a list[UpgradablePackage] object.
        """

    def getInstalledPackages_v2(self) -> list[Package]:
        f"""
        Will retieve the intalled packages by the package manager in the format of a list[Package] object.
        """

    def getIcon(self, source: str) -> QIcon:
        """
        Will return the corresponding icon to the given source
        """
