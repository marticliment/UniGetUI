from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *
    
    
class ScoopPackageManager(SamplePackageManager):

    ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

    EXECUTABLE = "python.exe -m pip"

    NAME = "Pip"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = ["WARNING:", "[notice]", "Package"]
    BLACKLISTED_PACKAGE_IDS = ["WARNING:", "[notice]", "Package"]
    BLACKLISTED_PACKAGE_VERSIONS = ["Ignoring", "invalie"]

    icon = None

    if not os.path.exists(CACHE_FILE_PATH):
        os.makedirs(CACHE_FILE_PATH)

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getAvailablePackages(self, second_attempt: bool = False) -> list[Package]:
        return []
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
                    Thread(target=self.cacheAvailablePackages, daemon=True, name=f"{self.NAME} package cacher thread").start()
                    print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
                    return packages
                else:
                    print(f"🟠 {self.NAME} cache file exists but is empty!")
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
        return
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
            
    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by {self.NAME} in the format of a list[UpgradablePackage] object.
        """
        print(f"🔵 Starting {self.NAME} search for updates")
        try:
            packages: list[UpgradablePackage] = []
            p = subprocess.Popen(f"{self.EXECUTABLE} list --outdated", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
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
                            newVersion = package[2]
                            source = self.NAME
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS and not newVersion in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, version, newVersion, source, Pip))
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            return packages
        except Exception as e:
            report(e)
            return []

    def getInstalledPackages(self) -> list[Package]:
        f"""
        Will retieve the intalled packages by {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} search for installed packages")
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
                        if len(package) >= 2:
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, self.NAME, Pip))
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            return packages
        except Exception as e:
            report(e)
            return []
        
    def getPackageDetails(self, package: Package) -> PackageDetails:
        """
        Will return a PackageDetails object containing the information of the given Package object
        """
        print(f"🔵 Starting get info for {package.Name} on {self.NAME}")
        details = PackageDetails(package)
        try:
            details.ManifestUrl = f"https://pypi.org/project/{package.Id}/"
            details.ReleaseNotesUrl = f"https://pypi.org/project/{package.Id}/#history"
            details.InstallerURL = f"https://pypi.org/project/{package.Id}/#files"
            details.Scopes = [_("User")]
            details.InstallerType = "Pip"
        
            p = subprocess.Popen(f"{self.EXECUTABLE} show {package.Id} -v", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
            output: list[str] = []
            while p.poll() is None:
                line = p.stdout.readline()
                if line:
                    output.append(str(line, encoding='utf-8', errors="ignore"))
            for line in output:
                if "Name:" in line:
                    details.Name = formatPackageIdAsName(line.replace("Name:", "").strip()) if "-" in line else line.replace("Name:", "").strip()
                elif "Author:" in line:
                    details.Author = line.replace("Author:", "").strip()
                elif "Home-page:" in line:
                    details.HomepageURL = line.replace("Home-page:", "").strip()
                elif "License:" in line:
                    details.License = line.replace("License:", "").strip()
                elif "License ::" in line:
                    details.License = line.split("::")[-1].strip()
                elif "Summary:" in line:
                    details.Description = line.replace("Summary:", "").strip()
                elif "Release Notes" in line:
                    details.ReleaseNotesUrl = line.replace("Release Notes:", "").strip()
                elif "Topic ::" in line:
                    if line.split("::")[-1].strip() not in details.Tags:
                        details.Tags.append(line.split("::")[-1].strip())
                    
            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str) -> QIcon:
        if not self.icon:
            self.icon = QIcon(getMedia("python"))
        return self.icon

    def getParameters(self, options: InstallationOptions) -> list[str]:
        Parameters: list[str] = []
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.InstallationScope:
            if options.InstallationScope in ("User", _("User")):
                Parameters.append("--user")
        Parameters += ["--no-input", "--no-color", "--no-python-version-warning", "--no-cache", "--progress-bar", "off"]
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        raise NotImplementedError
        bucket_prefix = ""
        if len(package.Source.split(":"))>1 and not "/" in package.Source:
            bucket_prefix = package.Source.lower().split(":")[1].replace(" ", "")+"/"
        Command = self.EXECUTABLE.split(" ") + ["install", bucket_prefix+package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command + ["--global"]
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()

    def startUpdate(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        raise NotImplementedError
        bucket_prefix = ""
        if len(package.Source.split(":"))>1 and not "/" in package.Source:
            bucket_prefix = package.Source.lower().split(":")[1].replace(" ", "")+"/"
        Command = self.EXECUTABLE.split(" ") + ["update", bucket_prefix+package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command + ["--global"]
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: update {package.Name}").start()
        
    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        raise NotImplementedError
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                if("Installing" in line):
                    widget.counterSignal.emit(1)
                elif("] 100%" in line or "Downloading" in line):
                    widget.counterSignal.emit(4)
                elif("was installed successfully!" in line):
                    widget.counterSignal.emit(6)
                widget.addInfoLine.emit(line)
                if("was installed successfully" in line):
                    outputCode = 0
                elif ("is already installed" in line):
                    outputCode = 0
                output += line+"\n"
        if "-g" in output and not "successfully" in output and not options.RunAsAdministrator:
            outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
        elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        if "Latest versions for all apps are installed" in output:
            outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
        widget.finishInstallation.emit(outputCode, output)
        
    def startUninstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        raise NotImplementedError
        bucket_prefix = ""
        if len(package.Source.split(":"))>1 and not "/" in package.Source:
            bucket_prefix = package.Source.lower().split(":")[1].replace(" ", "")+"/"
        Command = self.EXECUTABLE.split(" ") + ["uninstall", bucket_prefix+package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command + ["--global"]
        print(f"🔵 Starting {package} uninstall with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: uninstall {package.Name}").start()
        
    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        raise NotImplementedError
        outputCode = 1
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                if("Uninstalling" in line):
                    widget.counterSignal.emit(1)
                elif("Removing shim for" in line):
                    widget.counterSignal.emit(4)
                elif("was uninstalled" in line):
                    widget.counterSignal.emit(6)
                widget.addInfoLine.emit(line)
                if("was uninstalled" in line):
                    outputCode = 0
                output += line+"\n"
        if "-g" in output and not "was uninstalled" in output and not options.RunAsAdministrator:
            outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
        elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
            outputCode = RETURNCODE_NEEDS_ELEVATION
        widget.finishInstallation.emit(outputCode, output)

    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -V", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = shutil.which("python.exe") != None
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').split("\n")[1]
        if signal:
            signal.emit()
        
    def updateSources(self, signal: Signal = None) -> None:
        pass # Handled by the package manager, no need to manually reload
        if signal:
            signal.emit()

Pip = ScoopPackageManager()
