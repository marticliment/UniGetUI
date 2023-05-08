from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *
    
    
class ScoopPackageManager(SamplePackageManager):

    ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

    EXECUTABLE = "powershell -ExecutionPolicy ByPass -Command scoop"

    NAME = "Scoop"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = []
    BLACKLISTED_PACKAGE_IDS = []
    BLACKLISTED_PACKAGE_VERSIONS = []

    icon = None

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
                        if len(package) >= 4 and not package[0] in self.BLACKLISTED_PACKAGE_NAMES and not package[1] in self.BLACKLISTED_PACKAGE_IDS and not package[2] in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(package[0], package[1], package[2], package[3], Scoop))
                    Thread(target=self.cacheAvailablePackages_v2, daemon=True, name=f"{self.NAME} package cacher thread").start()
                    print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
                    return packages
                else:
                    print(f"🟠 {self.NAME} cache file exists but is empty!")
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
        INTERNAL METHOD
        Will load the available packages and write them into the cache file
        """
        print(f"🔵 Starting {self.NAME} package caching")
        try:
            p = subprocess.Popen(f"{self.NAME} search", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
            ContentsToCache = ""
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "----" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        name = formatPackageIdAsName(package[0])
                        id = package[0]
                        version = package[1]
                        source = f"Scoop: {package[2].strip()}"
                        if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                            ContentsToCache += f"{name},{id},{version},{source}\n"
            AlreadyCachedPackages = ""
            try:
                if os.path.exists(self.CACHE_FILE):
                    f = open(self.CACHE_FILE, "r")
                    AlreadyCachedPackages = f.read()
                    f.close()
            except Exception as e:
                report(e)
            for line in AlreadyCachedPackages.split("\n"):
                if line.split(",")[0] not in ContentsToCache:
                    ContentsToCache += line + "\n"
            with open(self.CACHE_FILE, "w") as f:
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
            p = subprocess.Popen(f"{self.EXECUTABLE} status", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "----" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        name = formatPackageIdAsName(package[0])
                        id = package[0]
                        version = package[1]
                        newVersion = package[2]
                        source = self.NAME
                        if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS and not newVersion in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(UpgradablePackage(name, id, version, newVersion, source, Scoop))
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
        time.sleep(2)
        try:
            packages: list[Package] = []
            p = subprocess.Popen(f"{self.EXECUTABLE} list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "----" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        if len(package) >= 3:
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            source = f"Scoop: {package[2].strip()}"
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Scoop))
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
        details = PackageDetails(package)
        try:
            unknownStr = _("Not available")
            bucket = "main" if len(package.Id.split("/")) == 1 else package.Id.split('/')[0]
            if bucket in globals.scoopBuckets:
                bucketRoot = globals.scoopBuckets[bucket].replace(".git", "")
            else:
                bucketRoot = f"https://github.com/ScoopInstaller/{bucket}"
            details.ManifestUrl = f"{bucketRoot}/blob/master/bucket/{package.Id.split('/')[-1]}.json"
            details.Scopes = [_("Local"), _("Global")]
            details.InstallerType = _("Scoop package")
        
            rawOutput = b""
            p = subprocess.Popen(' '.join([self.EXECUTABLE, "cat", f"{package.Id}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
            while p.poll() is None:
                pass
            for line in p.stdout.readlines():
                line = line.strip()
                if line:
                    rawOutput += line+b"\n"

            with open(os.path.join(os.path.expanduser("~"), ".wingetui", "scooptemp.json"), "wb") as f:
                f.write(rawOutput)
            mfest = open(os.path.join(os.path.expanduser("~"), ".wingetui", "scooptemp.json"), "r")
            import json
            data: dict = json.load(mfest)
            if "description" in data.keys():
                details.Description = data["description"]
                
            if "version" in data.keys():
                details.Versions.append(data["version"])
                
            if "innosetup" in data.keys():
                details.InstallerType = "Inno Setup"

            if "homepage" in data.keys():
                w = data["homepage"]
                details.HomepageURL = w
                if "https://github.com/" in w:
                    details.Author = w.replace("https://github.com/", "").split("/")[0]
                else:
                    for e in ("https://", "http://", "www.", ".com", ".net", ".io", ".org", ".us", ".eu", ".es", ".tk", ".co.uk", ".in", ".it", ".fr", ".de", ".kde", ".microsoft"):
                        w = w.replace(e, "")
                    details.Author = w.split("/")[0].capitalize()
                    
            if "notes" in data.keys():
                if type(data["notes"]) == list:
                    details.ReleaseNotes = "\n".join(data["notes"])
                else:
                    details.ReleaseNotes = data["notes"]
            if "license" in data.keys():
                details.License = data["license"] if type(data["license"]) != dict else data["license"]["identifier"]
                details.LicenseURL = unknownStr if type(data["license"]) != dict else data["license"]["url"]

            if "url" in data.keys():
                details.InstallerHash = data["hash"][0] if type(data["hash"]) == list else data["hash"]
                url = data["url"][0] if type(data["url"]) == list else data["url"]
                details.InstallerURL = url
                try:
                    details.InstallerSize = int(urlopen(url).length/1000000)
                except Exception as e:
                    print("🟠 Can't get installer size:", type(e), str(e))
            elif "architecture" in data.keys():
                details.InstallerHash = data["architecture"]["64bit"]["hash"]
                url = data["architecture"]["64bit"]["url"]
                details.InstallerURL = url
                try:
                    details.InstallerSize = int(urlopen(url).length/1000000)
                except Exception as e:
                    print("🟠 Can't get installer size:", type(e), str(e))
                if type(data["architecture"]) == dict:
                    details.Architectures = list(data["architecture"].keys())
            
            if "checkver" in data.keys():
                if "url" in data["checkver"].keys():
                    url = data["checkver"]["url"]
                    details.ReleaseNotesUrl = url
            
            
            if details.ReleaseNotesUrl == unknownStr and "github.com" in details.InstallerURL:
                try:
                    url = "/".join(details.InstallerURL.replace("/download/", "/tag/").split("/")[:-1])
                    details.ReleaseNotesUrl = url
                except Exception as e:
                    report(e)
                    
            output = []   
            p = subprocess.Popen(' '.join([self.EXECUTABLE, "info", package.Id]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
            while p.poll() is None:
                pass
            for line in p.stdout.readlines():
                line = line.strip()
                if line:
                    output.append(self.ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")))
            for line in output:
                for line in output:
                    if("Updated by" in line):
                        details.Publisher = line.replace("Updated by", "").strip()[1:].strip()
                    elif("Updated at" in line):
                        details.UpdateDate = line.replace("Updated at", "").strip()[1:].strip()
                    
            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str) -> QIcon:
        if not self.icon:
            self.icon = QIcon(getMedia("scoop"))
        return self.icon

    def installAssistant(self, p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal, alreadyGlobal: bool = False) -> None:
        print(f"🟢 scoop installer assistant thread started for process {p}")
        outputCode = 1
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                if("Installing" in line):
                    counterSignal.emit(1)
                elif("] 100%" in line or "Downloading" in line):
                    counterSignal.emit(4)
                elif("was installed successfully!" in line):
                    counterSignal.emit(6)
                infoSignal.emit(line)
                if("was installed successfully" in line):
                    outputCode = 0
                elif ("is already installed" in line):
                    outputCode = 0
                output += line+"\n"
        if "-g" in output and not "successfully" in output and not alreadyGlobal:
            outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
        elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        if "Latest versions for all apps are installed" in output:
            outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
        closeAndInform.emit(outputCode, output)

    
    def uninstallAssistant(self, p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal, alreadyGlobal: bool = False) -> None:
        print(f"🟢 scoop uninstaller assistant thread started for process {p}")
        outputCode = 1
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                if("Uninstalling" in line):
                    counterSignal.emit(1)
                elif("Removing shim for" in line):
                    counterSignal.emit(4)
                elif("was uninstalled" in line):
                    counterSignal.emit(6)
                infoSignal.emit(line)
                if("was uninstalled" in line):
                    outputCode = 0
                output += line+"\n"
        if "-g" in output and not "was uninstalled" in output and not alreadyGlobal:
            outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
        elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        closeAndInform.emit(outputCode, output)


    def loadBuckets(self, packageSignal: Signal, finishSignal: Signal) -> None:
        print("🟢 Starting scoop search...")
        p = subprocess.Popen(f"{self.EXECUTABLE} bucket list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        output = []
        counter = 0
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if(counter > 1 and not b"---" in line):
                    output.append(self.ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")))
                else:
                    counter += 1
        counter = 0
        for element in output:
            try:
                while "  " in element.strip():
                    element = element.strip().replace("  ", " ")
                element: list[str] = element.split(" ")
                packageSignal.emit(element[0].strip(), element[1].strip(), element[2].strip()+" "+element[3].strip(), element[4].strip())
            except IndexError as e:
                try:
                    packageSignal.emit(element[0].strip(), element[1].strip(), "Unknown", "Unknown")
                except IndexError as f:
                    print(e, f)
                print("IndexError: "+str(e))

        print("🟢 Scoop bucket search finished")
        finishSignal.emit()

Scoop = ScoopPackageManager()



if(__name__=="__main__"):
    import wingetui.__init__
