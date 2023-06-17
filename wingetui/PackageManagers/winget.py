from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *

class WingetPackageManager(DynamicPackageManager):

    if getSettings("UseSystemWinget"):
        EXECUTABLE = "winget.exe"
    else:
        EXECUTABLE = os.path.join(os.path.join(realpath, "winget-cli"), "winget.exe")

    NAME = "Winget"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = [""]
    BLACKLISTED_PACKAGE_IDS = ["", "have", "the", "Id"]
    BLACKLISTED_PACKAGE_VERSIONS = ["have", "an", "'winget", "pin'", "have", "an", "Version"]
    
    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = True
    Capabilities.CanSkipIntegrityChecks = True
    Capabilities.CanRunInteractively = True
    Capabilities.CanRemoveDataOnUninstall = False
    Capabilities.SupportsCustomVersions = True
    Capabilities.SupportsCustomArchitectures = True
    Capabilities.SupportsCustomScopes = True

    wingetIcon = None
    localIcon = None
    steamIcon = None
    gogIcon = None
    uPlayIcon = None
    msStoreIcon = None
    wsaIcon = None

    if not os.path.exists(CACHE_FILE_PATH):
        os.makedirs(CACHE_FILE_PATH)
        
    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getAvailablePackages(self, second_attempt: bool = False) -> list[Package]:
        f"""
        Will retieve the cached packages for the package manager {self.NAME} in the format of a list[Package] object.
        If the cache is empty, will forcefully cache the packages and return a valid list[Package] object.
        Finally, it will start a background cacher thread.
        """
        print(f"🔵 Starting {self.NAME} search for available packages")
        try:
            packages: list[Package] = []
            if os.path.exists(self.CACHE_FILE):
                f = open(self.CACHE_FILE, "r", encoding="utf-8", errors="ignore")
                content = f.read()
                f.close()
                if content != "":
                    print(f"🟢 Found valid, non-empty cache file for {self.NAME}!")
                    for line in content.split("\n"):
                        package = line.split(",")
                        if len(package) >= 2:
                            packages.append(Package(package[0], package[1], package[2], "Winget: winget", Winget))
                    Thread(target=self.cacheAvailablePackages, daemon=True, name=f"{self.NAME} package cacher thread").start()
                    print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
                    return packages
                else:
                    print(f"🟠 {self.NAME} cache file exists but is empty!")
                    f.close()
                    if second_attempt:
                        print(f"🔴 Could not load {self.NAME} packages, returning an empty list!")
                        return []
                    self.cacheAvailablePackages()
                    return self.getAvailablePackages(second_attempt = True)
            else:
                print(f"🟡 {self.NAME} cache file does not exist, creating cache forcefully and returning new package list")
                if second_attempt:
                    print(f"🔴 Could not load {self.NAME} packages, returning an empty list!")
                    return []
                self.cacheAvailablePackages()
                return self.getAvailablePackages(second_attempt = True)
        except Exception as e:
            report(e)
            return []
        
    def cacheAvailablePackages(self) -> None:
        """
        Internal method, should not be called manually externally.
        Will load the available packages and write them into the cache file
        """
        print(f"🔵 Starting {self.NAME} package caching")
        try:
            p = subprocess.Popen([self.EXECUTABLE, "search", "", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            ContentsToCache = ""
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not hasShownId:
                        if " Id " in line:
                            line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                            for char in ("\r", "/", "|", "\\", "-"):
                                line = line.split(char)[-1].strip()
                            hasShownId = True
                            idPosition = len(line.split("Id")[0])
                            versionPosition = len(line.split("Version")[0])
                    elif "---" in line:
                        pass
                    else:
                        try:
                            name = line[0:idPosition].strip()
                            idVersionSubstr = line[idPosition:].strip()
                            if "  " in name:
                                oName = name
                                while "  " in oName:
                                    oName = oName.replace("  ", " ")
                                idVersionSubstr = oName.split(" ")[-1]+idVersionSubstr
                                name = " ".join(oName.split(" ")[:-1])
                            idVersionSubstr.replace("\t", " ")
                            while "  " in idVersionSubstr:
                                idVersionSubstr = idVersionSubstr.replace("  ", " ")
                            iOffset = 0
                            id = idVersionSubstr.split(" ")[iOffset]
                            ver = idVersionSubstr.split(" ")[iOffset+1]
                            if len(id) == 1:
                                iOffset + 1
                                id = idVersionSubstr.split(" ")[iOffset]
                                ver = idVersionSubstr.split(" ")[iOffset+1]
                            if ver.strip() in ("<", "-"):
                                iOffset += 1
                                ver = idVersionSubstr.split(" ")[iOffset+1]
                            if not "  " in name:
                                if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                    ContentsToCache += f"{name},{id},{ver}\n"
                            else:
                                if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                    name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                                    while "##" in name:
                                        name = name.replace("##", "#")
                                    print(f"🟡 package {name} failed parsing, going for method 2...")
                                    ContentsToCache += f"{name},{id},{ver}\n"
                        except Exception as e:
                            ContentsToCache += f"{line[0:idPosition].strip()},{line[idPosition:versionPosition].strip()},{line[versionPosition:].strip()}\n"
                            if type(e) != IndexError:
                                report(e)
            AlreadyCachedPackages = ""
            try:
                if os.path.exists(self.CACHE_FILE):
                    f = open(self.CACHE_FILE, "r", encoding="utf-8", errors="ignore")
                    AlreadyCachedPackages = f.read()
                    f.close()
            except Exception as e:
                report(e)
            for line in AlreadyCachedPackages.split("\n"):
                if line.split(",")[0] not in ContentsToCache:
                    ContentsToCache += line + "\n"
            with open(self.CACHE_FILE, "w", encoding="utf-8", errors="ignore") as f:
                f.write(ContentsToCache)
            print(f"🟢 {self.NAME} packages cached successfuly")
        except Exception as e:
            report(e)
    
    def getPackagesForQuery(self, query: str) -> list[Package]:
        if getSettings("DisableMicrosoftStore"):
            print("🟡 Microsoft Store source is disabled")
            return []
        print(f"🔵 Starting {self.NAME} search for dynamic packages (msstore source)")
        try:
            packages: list[Package] = []
            p = subprocess.Popen([self.EXECUTABLE, "search", query, "--source", "msstore", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            ContentsToCache = ""
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                print(line)
                if line:
                    if not hasShownId:
                        if " Id " in line:
                            line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                            for char in ("\r", "/", "|", "\\", "-"):
                                line = line.split(char)[-1].strip()
                            hasShownId = True
                            idPosition = len(line.split("Id")[0])
                            versionPosition = len(line.split("Version")[0])
                    elif "---" in line:
                        pass
                    else:
                        try:
                            name = line[0:idPosition].strip()
                            idVersionSubstr = line[idPosition:].strip()
                            if "  " in name:
                                oName = name
                                while "  " in oName:
                                    oName = oName.replace("  ", " ")
                                idVersionSubstr = oName.split(" ")[-1]+idVersionSubstr
                                name = " ".join(oName.split(" ")[:-1])
                            idVersionSubstr.replace("\t", " ")
                            while "  " in idVersionSubstr:
                                idVersionSubstr = idVersionSubstr.replace("  ", " ")
                            iOffset = 0
                            id = idVersionSubstr.split(" ")[iOffset]
                            ver = idVersionSubstr.split(" ")[iOffset+1]
                            if len(id) == 1:
                                iOffset + 1
                                id = idVersionSubstr.split(" ")[iOffset]
                                ver = idVersionSubstr.split(" ")[iOffset+1]
                            if ver.strip() in ("<", "-"):
                                iOffset += 1
                                ver = idVersionSubstr.split(" ")[iOffset+1]
                            if not "  " in name:
                                if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                    ContentsToCache += f"{name},{id},{ver}\n"
                            else:
                                if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                    name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                                    while "##" in name:
                                        name = name.replace("##", "#")
                                    print(f"🟡 package {name} failed parsing, going for method 2...")
                                    ContentsToCache += f"{name},{id},{ver}\n"
                        except Exception as e:
                            ContentsToCache += f"{line[0:idPosition].strip()},{line[idPosition:versionPosition].strip()},{line[versionPosition:].strip()}\n"
                            if type(e) != IndexError:
                                report(e)
            
            for line in ContentsToCache.split("\n"):
                package = line.split(",")
                if len(package) >= 2:
                    packages.append(Package(package[0], package[1], package[2], "Winget: msstore", Winget))
            
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s) (msstore)")
            return packages
        
        except Exception as e:
            report(e)
                       
    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by {self.NAME} in the format of a list[UpgradablePackage] object.
        """
        print(f"🔵 Starting {self.NAME} search for updates")
        try:
            packages: list[UpgradablePackage] = []
            p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "upgrade", "--include-unknown", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            newVerPosition: int = 0
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n"+line
                if not hasShownId:
                    if " Id " in line:
                        line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                        for char in ("\r", "/", "|", "\\", "-"):
                            line = line.split(char)[-1].strip()
                        hasShownId = True
                        idPosition = len(line.split("Id")[0])
                        versionPosition = len(line.split("Version")[0])
                        newVerPosition = len(line.split("Available")[0])
                    else:
                        pass
                elif "---" in line:
                    pass
                else:
                    element = line
                    try:
                        verElement = element[idPosition:].strip()
                        verElement.replace("\t", " ")
                        while "  " in verElement:
                            verElement = verElement.replace("  ", " ")
                        iOffset = 0
                        id = verElement.split(" ")[iOffset+0]
                        ver = verElement.split(" ")[iOffset+1]
                        newver = verElement.split(" ")[iOffset+2]
                        if len(id)==1:
                            iOffset + 1
                            id = verElement.split(" ")[iOffset+0]
                            newver = verElement.split(" ")[iOffset+2]
                            ver = verElement.split(" ")[iOffset+1]
                        if ver.strip() in ("<", ">", "-"):
                            iOffset += 1
                            ver = verElement.split(" ")[iOffset+1]
                            newver = verElement.split(" ")[iOffset+2]
                        name = element[0:idPosition].strip()
                        StoreName = "Winget"
                        if "winget" in line:
                            StoreName = "Winget: winget"
                        elif "msstore" in line:
                            StoreName = "Winget: msstore"
                        if not "  " in name:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, ver, newver, StoreName, Winget))
                        else:
                            name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                            while "##" in name:
                                name = name.replace("##", "#")
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name.split("#")[0], name.split("#")[-1]+id, ver, newver, StoreName, Winget))
                    except Exception as e:
                        packages.append(UpgradablePackage(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:newVerPosition].split(" ")[0].strip(), element[newVerPosition:].split(" ")[0].strip(), StoreName, Winget))
                        if type(e) != IndexError:
                            report(e)
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            globals.PackageManagerOutput += rawoutput
            return packages
        except Exception as e:
            report(e)
            return []

    def getInstalledPackages(self) -> list[Package]:
        f"""
        Will retieve the intalled packages by {self.NAME} in the format of a list[Package] object.
        """
        
        def getSource(id: str) -> str:
            id = id.strip()
            androidValid = True
            for letter in id:
                if letter not in "abcdefghijklmnopqrstuvwxyz.":
                    androidValid = False
            if androidValid and id.count(".") > 1:
                return _("Android Subsystem")
            s = "Winget"
            for illegal_char in ("{", "}", " "):
                if illegal_char in id:
                    s = _("Local PC")
                    break
            if s == "Winget":
                if id.count(".") != 1:
                    s = (_("Local PC"))
                    if id.count(".") > 1:
                        for letter in id:
                            if letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ":
                                s = "Winget"
                                break    
            if s == _("Local PC"):
                if id == "Steam":
                    s = "Steam"
                if id == "Uplay":
                    s = "Ubisoft Connect"
                if id.count("_is1") == 1:
                    s = "GOG"
                    for number in id.split("_is1")[0]:
                        if number not in "0123456789":
                            s = _("Local PC")
                            break
                    if len(id) != 14:
                        s = _("Local PC")
                    if id.count("GOG") == 1:
                        s = "GOG"
            if s == "Winget":
                if len(id.split("_")[-1]) in (13, 14) and (len(id.split("_"))==2 or id == id.upper()):
                    s = "Microsoft Store"
                elif len(id.split("_")[-1]) <= 13 and len(id.split("_"))==2 and "…" == id.split("_")[-1][-1]: # Delect microsoft store ellipsed packages 
                    s = "Microsoft Store"
            if len(id) in (13, 14) and (id.upper() == id):
                s = "Winget: msstore"
            if s == "Winget":
                s = "Winget: winget"
            return s
        
        print(f"🔵 Starting {self.NAME} search for installed packages")
        try:
            packages: list[Package] = []
            p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "list", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.PIPE, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n"+line
                if not hasShownId:
                    if " Id " in line:
                        line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                        for char in ("\r", "/", "|", "\\", "-"):
                            line = line.split(char)[-1].strip()
                        hasShownId = True
                        idPosition = len(line.split("Id")[0])
                        versionPosition = len(line.split("Version")[0])
                    else:
                        pass
                elif "---" in line:
                    pass
                else:
                    element = line.replace("2010  x", "2010 x").replace("Microsoft.VCRedist.2010", " Microsoft.VCRedist.2010") # Fix an issue with MSVC++ 2010, where it shows with a double space (see https://github.com/marticliment/WingetUI#450)
                    try:
                        verElement = element[idPosition:].strip()
                        verElement.replace("\t", " ")
                        untrimmedVerelement = verElement
                        while "  " in verElement:
                            verElement = verElement.replace("  ", " ")
                        iOffset = 0
                        id = " ".join(untrimmedVerelement.split(" ")[iOffset:-1])
                        ver = verElement.split(" ")[-1]
                        if len(id) > (versionPosition - idPosition):
                            id = " ".join(untrimmedVerelement.split(" ")[iOffset])
                            id = id.replace("  ", "#").replace(" ", "").replace("#", " ")
                            ver = verElement.split(" ")[iOffset+1]
                        if len(id) == 1:
                            iOffset + 1
                            id = verElement.split(" ")[iOffset+0]
                            ver = verElement.split(" ")[iOffset+1]
                        if ver.strip() in ("<", "-", ">"):
                            iOffset += 1
                            ver = verElement.split(" ")[iOffset+1]
                        name = element[0:idPosition].strip()
                        StoreName = "Winget"
                        if "winget" in line:
                            StoreName = "Winget: winget"
                        elif "msstore" in line:
                            StoreName = "Winget: msstore"
                        if not "  " in name:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id.strip() in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id.strip(), ver, getSource(id), Winget))
                        else:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id.strip() in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                print(f"🟡 package {name} failed parsing, going for method 2...")
                                name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                                while "##" in name:
                                    name = name.replace("##", "#")
                                packages.append(Package(name.split("#")[0], (name.split("#")[-1]+id).strip(), ver, getSource(id), Winget))
                    except Exception as e:
                        packages.append(Package(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:].strip(), getSource(id), Winget))
                        if type(e) != IndexError:
                            report(e)
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            globals.PackageManagerOutput += rawoutput
            return packages
        except Exception as e:
            report(e)
            return []
    
    def getPackageDetails(self, package: Package) -> PackageDetails:
        """
        Will return a PackageDetails object containing the information of the given Package object
        """
        print(f"🔵 Starting get info for {package.Id} on {self.NAME}")
        if "…" in package.Id:
            newId = self.getFullPackageId(package.Id)
            if newId:
                print(f"🔵 Replacing ID {package.Id} for {newId}")
                package.Id = newId
        details = PackageDetails(package)
        try:
            details.Scopes = [_("Current user"), _("Local machine")]
            details.ManifestUrl = f"https://github.com/microsoft/winget-pkgs/tree/master/manifests/{package.Id[0].lower()}/{'/'.join(package.Id.split('.'))}" if not (len(package.Id) == 14 and package.Id == package.Id.upper()) else f"https://apps.microsoft.com/store/detail/{package.Id}"
            details.Architectures = ["x64", "x86", "arm64"]
            loadedInformationPieces = 0
            currentIteration = 0
            while loadedInformationPieces < 2 and currentIteration < 50:
                currentIteration += 1
                outputIsDescribing = False
                outputIsShowingNotes = False
                outputIsShowingTags = False
                p = subprocess.Popen([self.EXECUTABLE, "show", "--id", f"{package.Id}", "--exact", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
                output: list[str] = []
                while p.poll() is None:
                    line = p.stdout.readline()
                    if line:
                        if b"No package found matching input criteria." in line:
                            return details
                        output.append(str(line, encoding='utf-8', errors="ignore"))
                        
                globals.PackageManagerOutput += "\n--------"+"\n".join(output)
                        
                for line in output:
                    if line[0] == " " and outputIsDescribing:
                        details.Description += "<br>"+line
                    else:
                        outputIsDescribing = False
                    if line[0] == " " and outputIsShowingNotes:
                        details.ReleaseNotes += line + "<br>"
                    else:
                        outputIsShowingNotes = False
                    if line[0] == " " and outputIsShowingTags:
                        details.Tags.append(line.strip())
                    else:
                        outputIsShowingTags = False 
                    if "Publisher:" in line:
                        details.Publisher = line.replace("Publisher:", "").strip()
                        loadedInformationPieces += 1
                    elif "Description:" in line:
                        details.Description = line.replace("Description:", "").strip()
                        outputIsDescribing = True
                        loadedInformationPieces += 1
                    elif "Author:" in line:
                        details.Author = line.replace("Author:", "").strip()
                        loadedInformationPieces += 1
                    elif "Homepage:" in line:
                        details.HomepageURL = line.replace("Homepage:", "").strip()
                        loadedInformationPieces += 1
                    elif "License:" in line:
                        details.License = line.replace("License:", "").strip()
                        loadedInformationPieces += 1
                    elif "License Url:" in line:
                        details.LicenseURL = line.replace("License Url:", "").strip()
                        loadedInformationPieces += 1
                    elif "Installer SHA256:" in line:
                        details.InstallerHash = line.replace("Installer SHA256:", "").strip()
                        loadedInformationPieces += 1
                    elif "Installer Url:" in line:
                        details.InstallerURL = line.replace("Installer Url:", "").strip()
                        try:
                            details.InstallerSize = int(urlopen(details.InstallerURL).length/1000000)
                        except Exception as e:
                            print("🟠 Can't get installer size:", type(e), str(e))
                        loadedInformationPieces += 1
                    elif "Release Date:" in line:
                        details.UpdateDate = line.replace("Release Date:", "").strip()
                        loadedInformationPieces += 1
                    elif "Release Notes Url:" in line:
                        details.ReleaseNotesUrl = line.replace("Release Notes Url:", "").strip()
                        loadedInformationPieces += 1
                    elif "Release Notes:" in line:
                        details.ReleaseNotes = ""
                        outputIsShowingNotes = True
                        loadedInformationPieces += 1
                    elif "Tags:" in line:
                        details.Tags = []
                        outputIsShowingTags = True
                        loadedInformationPieces += 1
                    elif "Installer Type:" in line:
                        details.InstallerType = line.replace("Installer Type:", "").strip()
                        
            print(f"🔵 Loading versions for {package.Name}")
            currentIteration = 0
            versions = []
            while versions == [] and currentIteration < 50:
                currentIteration += 1
                p = subprocess.Popen([self.EXECUTABLE, "show", "--id", f"{package.Id}", "-e", "--versions", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
                foundDashes = False
                while p.poll() is None:
                    line = p.stdout.readline().strip()
                    if line:
                        if foundDashes:
                            versions.append(str(line, encoding='utf-8', errors="ignore"))
                        elif b"--" in line:
                            foundDashes = True
            details.Versions = versions
            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str) -> QIcon:
        if not self.wingetIcon:
            self.wingetIcon = QIcon(getMedia("winget"))
            self.localIcon = QIcon(getMedia("localpc"))
            self.msStoreIcon = QIcon(getMedia("msstore"))
            self.wsaIcon = QIcon(getMedia("android"))
            self.SteamIcon = QIcon(getMedia("steam"))
            self.gogIcon = QIcon(getMedia("gog"))
            self.uPlayIcon = QIcon(getMedia("uplay"))
        if "microsoft store" in source.lower():
            return self.msStoreIcon
        elif source in (_("Local PC"), "Local PC"):
            return self.localIcon
        elif "steam" in source.lower():
            return self.steamIcon
        elif "gog" in source.lower():
            return self.gogIcon
        elif "ubisoft connect" in source.lower():
            return self.uPlayIcon
        elif source in (_("Android Subsystem"), "Android Subsystem"):
            return self.wsaIcon
        else:
            return self.wingetIcon
        
    def getParameters(self, options: InstallationOptions) -> list[str]:
        Parameters: list[str] = ["--accept-source-agreements"]
        if options.Architecture:
            Parameters += ["--architecture", options.Architecture]
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.InstallationScope:
            if options.InstallationScope in (_("Current user"), "Current user"):
                Parameters.append("--scope")
                Parameters.append("user")
            elif options.InstallationScope in (_("Local machine"), "Local machine"):
                Parameters.append("--scope")
                Parameters.append("machine")
        if options.InteractiveInstallation:
            Parameters.append("--interactive")
        if options.SkipHashCheck:
            Parameters.append("--ignore-security-hash")
        if options.Version:
            Parameters += ["--version", options.Version, "--force"]
        Parameters += ["--disable-interactivity"]
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "…" in package.Id:
            package.Id = self.getFullPackageId(package.Id)
        Command = [self.EXECUTABLE, "install"] + (["--id", package.Id, "--exact"] if not "…" in package.Id else ["--name", '"'+package.Name+'"']) + self.getParameters(options) + ["--accept-package-agreements"]
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()
        return p
    
    def startUpdate(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "…" in package.Id:
            package.Id = self.getFullPackageId(package.Id)
        Command = [self.EXECUTABLE, "upgrade"] + (["--id", package.Id, "--exact"] if not "…" in package.Id else ["--name", '"'+package.Name+'"']) + ["--include-unknown"] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: update {package.Name}").start()
        return p

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        output = ""
        counter = 0
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit(line)
                counter += 1
                widget.counterSignal.emit(counter)
                output += line+"\n"
        p.wait()
        match p.returncode:
            case 0x8A150011:
                outputCode = RETURNCODE_INCORRECT_HASH
            case 0x8A150109: # need restart
                outputCode = RETURNCODE_NEEDS_RESTART
            case other:
                outputCode = p.returncode
        if "No applicable upgrade found" in output or "No newer package versions are available from the configured sources" in output:
            outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
        widget.finishInstallation.emit(outputCode, output)
        
    def startUninstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "…" in package.Id:
            package.Id = self.getFullPackageId(package.Id)
        Command = [self.EXECUTABLE, "uninstall"] + (["--id", package.Id, "--exact"] if not "…" in package.Id else ["--name", '"'+package.Name+'"']) + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} uninstall with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: uninstall {package.Name}").start()
        return p

    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        counter = RETURNCODE_OPERATION_SUCCEEDED
        output = ""
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit(line)
                counter += 1
                widget.counterSignal.emit(counter)
                output += line+"\n"
        p.wait()
        outputCode = p.returncode
        if "1603" in output or "0x80070005" in output or "Access is denied" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        widget.finishInstallation.emit(outputCode, output)
        
    def getFullPackageId(self, id: str) -> tuple[str, str]:
        p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "search", "--id", id.replace("…", ""), "--accept-source-agreements"] ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        idSeparator = -1
        print(f"🔵 Finding Id for {id}")
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if idSeparator != -1:
                    if not b"---" in line:
                        print(f"🔵 found Id", str(line[idSeparator:], "utf-8", errors="ignore").split(" ")[0].strip())
                        return str(line[idSeparator:], "utf-8", errors="ignore").split(" ")[0].strip()
                else:
                    l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","").split("\r")[-1]
                    if(" Id " in l):
                        idSeparator = len(l.split("Id")[0])
        print("🟡 Better id not found!")
        return id

    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -v", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = shutil.which(self.EXECUTABLE) != None
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", "")
        if signal:
            signal.emit()
        
    def updateSources(self, signal: Signal = None) -> None:
        print(f"🔵 Reloading {self.NAME} sources...")
        subprocess.run(f"{self.EXECUTABLE} source update", shell=True, stdout=subprocess.PIPE)
        if signal:
            signal.emit()

Winget = WingetPackageManager()


if(__name__=="__main__"):
    os.chdir("..")
    import __init__