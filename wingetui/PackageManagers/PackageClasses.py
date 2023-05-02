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