from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *

class WingetPackageManager(SamplePackageManager):

    common_params = ["--source", "winget", "--accept-source-agreements"]

    if getSettings("UseSystemWinget"):
        EXECUTABLE = "winget.exe"
    else:
        EXECUTABLE = os.path.join(os.path.join(realpath, "winget-cli"), "winget.exe")

    NAME = "Winget"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = [""]
    BLACKLISTED_PACKAGE_IDS = [""]
    BLACKLISTED_PACKAGE_VERSIONS = []


    wingetIcon = None
    localIcon = None
    steamIcon = None
    gogIcon = None
    uPlayIcon = None
    msStoreIcon = None

    if not os.path.exists(CACHE_FILE_PATH):
        os.makedirs(CACHE_FILE_PATH)
        
    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getAvailablePackages_v2(self, second_attempt: bool = False) -> list[Package]:
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
                            packages.append(Package(package[0], package[1], package[2], self.NAME, Winget))
                    Thread(target=self.cacheAvailablePackages_v2, daemon=True, name=f"{self.NAME} package cacher thread").start()
                    print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
                    return packages
                else:
                    print(f"🟠 {self.NAME} cache file exists but is empty!")
                    f.close()
                    if second_attempt:
                        print(f"🔴 Could not load {self.NAME} packages, returning an empty list!")
                        return []
                    self.cacheAvailablePackages_v2()
                    return self.getAvailablePackages_v2(second_attempt = True)
            else:
                print(f"🟡 {self.NAME} cache file does not exist, creating cache forcefully and returning new package list")
                if second_attempt:
                    print(f"🔴 Could not load {self.NAME} packages, returning an empty list!")
                    return []
                self.cacheAvailablePackages_v2()
                return self.getAvailablePackages_v2(second_attempt = True)
        except Exception as e:
            report(e)
            return []
        
    def cacheAvailablePackages_v2(self) -> None:
        """
        Internal method, should not be called manually externally.
        Will load the available packages and write them into the cache file
        """
        print(f"🔵 Starting {self.NAME} package caching")
        try:
            p = subprocess.Popen([self.EXECUTABLE, "search", "", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            ContentsToCache = ""
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not hasShownId:
                        if "Id" in line:
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
            
    def getAvailableUpdates_v2(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by {self.NAME} in the format of a list[UpgradablePackage] object.
        """
        print(f"🔵 Starting {self.NAME} search for updates")
        try:
            packages: list[UpgradablePackage] = []
            p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "upgrade", "--include-unknown", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            newVerPosition: int = 0
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if not hasShownId:
                    if "Id" in line:
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
                        if not "  " in name:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, ver, newver, self.NAME, Winget))
                        else:
                            name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                            while "##" in name:
                                name = name.replace("##", "#")
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name.split("#")[0], name.split("#")[-1]+id, ver, newver, self.NAME, Winget))
                    except Exception as e:
                        packages.append(UpgradablePackage(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:newVerPosition].split(" ")[0].strip(), element[newVerPosition:].split(" ")[0].strip(), self.NAME, Winget))
                        if type(e) != IndexError:
                            report(e)
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            return packages
        except Exception as e:
            report(e)
            return []

    def getInstalledPackages_v2(self) -> list[Package]:
        f"""
        Will retieve the intalled packages by {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} search for installed packages")
        try:
            packages: list[Package] = []
            p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "list", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.PIPE, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            hasShownId: bool = False
            idPosition: int = 0
            versionPosition: int = 0
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if not hasShownId:
                    if "Id" in line:
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
                        while "  " in verElement:
                            verElement = verElement.replace("  ", " ")
                        iOffset = 0
                        id = " ".join(verElement.split(" ")[iOffset:-1])
                        ver = verElement.split(" ")[-1]
                        if len(id) == 1:
                            iOffset + 1
                            id = verElement.split(" ")[iOffset+0]
                            ver = verElement.split(" ")[iOffset+1]
                        if ver.strip() in ("<", "-"):
                            iOffset += 1
                            ver = verElement.split(" ")[iOffset+1]
                        name = element[0:idPosition].strip()
                        if not "  " in name:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, ver, self.NAME, Winget))
                        else:
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                print(f"🟡 package {name} failed parsing, going for method 2...")
                                name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                                while "##" in name:
                                    name = name.replace("##", "#")
                                packages.append(Package(name.split("#")[0], name.split("#")[-1]+id, ver, self.NAME, Winget))
                    except Exception as e:
                        packages.append(Package(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:].strip(), self.NAME, Winget))
                        if type(e) != IndexError:
                            report(e)
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            return packages
        except Exception as e:
            report(e)
            return []
    
    def getPackageDetails_v2(self, package: Package) -> PackageDetails:
        """
        Will return a PackageDetails object containing the information of the given Package object
        """
        print(f"🔵 Starting get info for {package.Name} on {self.NAME}")
        if "…" in package.Id:
            newId = self.getFullPackageId(package.Id)
            if newId:
                print(f"🔵 Replacing ID {package.Id} for {newId}")
                package.Id = newId
        details = PackageDetails(package)
        try:
            details.Scopes = [_("Current user"), _("Local machine")]
            details.ManifestUrl = f"https://github.com/microsoft/winget-pkgs/tree/master/manifests/{package.Id[0].lower()}/{'/'.join(package.Id.split('.'))}"
            details.Architectures = ["x64", "x86", "arm64"]
            loadedInformationPieces = 0
            currentIteration = 0
            while loadedInformationPieces < 2 and currentIteration < 50:
                currentIteration += 1
                outputIsDescribing = False
                outputIsShowingNotes = False
                p = subprocess.Popen([self.EXECUTABLE, "show", "--id", f"{package.Id}", "--exact"]+self.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
                output: list[str] = []
                while p.poll() is None:
                    line = p.stdout.readline()
                    if line:
                        output.append(str(line, encoding='utf-8', errors="ignore"))
                        
                for line in output:
                    if line[0] == " " and outputIsDescribing:
                        details.Description += "\n"+line
                    else:
                        outputIsDescribing = False
                    if line[0] == " " and outputIsShowingNotes:
                        details.ReleaseNotes += line + "<br>"
                    else:
                        outputIsShowingNotes = False
                        
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
                    elif "Installer Type:" in line:
                        details.InstallerType = line.replace("Installer Type:", "").strip()
                        
            print(f"🔵 Loading versions for {package.Name}")
            currentIteration = 0
            versions = []
            while versions == [] and currentIteration < 50:
                currentIteration += 1
                p = subprocess.Popen([self.EXECUTABLE, "show", "--id", f"{package.Id}", "-e", "--versions"]+self.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
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
        else:
            return self.wingetIcon

    def installAssistant(self, p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
        print(f"🟢 winget installer assistant thread started for process {p}")
        counter = RETURNCODE_OPERATION_SUCCEEDED
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                infoSignal.emit(line)
                counter += 1
                counterSignal.emit(counter)
                output += line+"\n"
        p.wait()
        match p.returncode:
            case 0x8A150011:
                outputCode = RETURNCODE_INCORRECT_HASH
            case 0x8A150109: # need restart
                outputCode = RETURNCODE_NEEDS_RESTART
            case other:
                outputCode = p.returncode
        if "No applicable upgrade found" in output:
            outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
        closeAndInform.emit(outputCode, output)

    def uninstallAssistant(self, p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
        print(f"🟢 winget uninstaller assistant thread started for process {p}")
        counter = RETURNCODE_OPERATION_SUCCEEDED
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                infoSignal.emit(line)
                counter += 1
                counterSignal.emit(counter)
                output += line+"\n"
        p.wait()
        outputCode = p.returncode
        if "1603" in output or "0x80070005" in output or "Access is denied" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        closeAndInform.emit(outputCode, output)
        
    def getFullPackageId(self, id: str) -> tuple[str, str]:
        print(f"🟢 Starting winget search, winget on {self.EXECUTABLE}...")
        p = subprocess.Popen(["mode", "400,30&", self.EXECUTABLE, "search", "--id", id.replace("…", "")] + self.self.common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        idSeparator = -1
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if idSeparator != -1:
                    if not b"---" in line:
                        return str(line[idSeparator:], "utf-8", errors="ignore").split(" ")[0].strip()
                else:
                    l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","").split("\r")[-1]
                    if("Id" in l):
                        idSeparator = len(l.split("Id")[0])
        return id

Winget = WingetPackageManager()


if(__name__=="__main__"):
    os.chdir("..")
    import __init__