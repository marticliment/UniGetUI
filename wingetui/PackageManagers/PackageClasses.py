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
    Publisher: str = ""
    Author: str = ""
    Description: str = ""
    HomepageURL: str = ""
    License: str = ""
    LicenseURL: str = ""
    InstallerURL: str = ""
    InstallerHash: str = ""
    InstallerSize: int = 0
    InstallerType: str = ""
    ManifestUrl: str = ""
    UpdateDate: str = ""
    ReleaseNotes: str = ""
    ReleaseNotesUrl: str = ""
    Versions: list[str] = []
    Architectures: list[str] = []
    Scopes: list[str] = []
    
    def __init__(self, package: Package):
        self.Name = package.Name
        self.Id = package.Id
        self.Version = package.Version
        self.Source = package.Source