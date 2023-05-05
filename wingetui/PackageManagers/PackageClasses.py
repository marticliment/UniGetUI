from tools import _

class Package():
    Name = ""
    Id = ""
    Version = ""
    Source = ""
    
    def __init__(self, Name: str, Id: str, Version: str, Source: str):
        self.Name = Name
        self.Id = Id
        self.Version = Version
        self.Source = Source
        
class UpgradablePackage(Package):
    NewVersion = ""
    NewPackage: Package = None
    
    def __init__(self, Name: str, Id: str, InstalledVersion: str, NewVersion: str, Source: str):
        super().__init__(Name, Id, InstalledVersion, Source)
        self.NewVersion = NewVersion
        self.NewPackage = Package(Name, Id, NewVersion, Source)
        
class PackageDetails(Package):
    Name: str = ""
    Id: str = ""
    Version: str = ""
    NewVersion: str = ""
    Source: str = ""
    Publisher: str = _("Not available")
    Author: str = _("Not available")
    Description: str = _("Not available")
    HomepageURL: str = _("Not available")
    License: str = _("Not available")
    LicenseURL: str = _("Not available")
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
        if type(package) == UpgradablePackage:
            self.NewVersion = package.NewVersion
        
    def asUrl(url: str) -> str:
        return f"<a href='{url}' style='color:%bluecolor%'>{url}</a>"
    
class PackageManagerModule:
    NAME: str
    EXECUTABLE: str
    
    def __init__(self):
        raise NotImplementedError("This class is only used to provide syntax highlighting support")
    
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

