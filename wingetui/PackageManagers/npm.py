from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *
    
    
class NPMPackageManager(DynamicLoadPackageManager):

    ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

    EXECUTABLE = "npm"

    NAME = "Npm"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = []
    BLACKLISTED_PACKAGE_IDS = []
    BLACKLISTED_PACKAGE_VERSIONS = []
    
    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = True
    Capabilities.CanSkipIntegrityChecks = False
    Capabilities.CanRunInteractively = False
    Capabilities.CanRemoveDataOnUninstall = False
    Capabilities.SupportsCustomVersions = True
    Capabilities.SupportsCustomArchitectures = False
    Capabilities.SupportsCustomScopes = True
    
    icon = None

    if not os.path.exists(CACHE_FILE_PATH):
        os.makedirs(CACHE_FILE_PATH)

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} search for dynamic packages")
        try:
            packages: list[Package] = []
            p = subprocess.Popen(f"{self.EXECUTABLE} search {query}", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ.copy(), shell=True)
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "NAME" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split("|")))
                        if len(package) >= 4:
                            name = formatPackageIdAsName(package[0][1:] if package[0][0] == "@" else package[0]).strip()
                            id = package[0].strip()
                            version = package[4].strip()
                            source = self.NAME
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Npm))
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            return packages
        except Exception as e:
            report(e)
            return []
           
    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by {self.NAME} in the format of a list[UpgradablePackage] object.
        """
        print(f"🔵 Starting {self.NAME} search for updates")
        try:
            packages: list[UpgradablePackage] = []
            p = subprocess.Popen(f"{self.EXECUTABLE} outdated", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ.copy(), shell=True)
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "Package" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        if len(package) >= 4:
                            name = formatPackageIdAsName(package[0][1:] if package[0][0] == "@" else package[0]).strip()
                            id = package[0].strip()
                            version = package[1].strip()
                            newVersion = package[3].strip()
                            source = self.NAME
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS and not newVersion in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, version, newVersion, source, Npm))
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
            p = subprocess.Popen(f"{self.EXECUTABLE} list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ.copy(), shell=True)
            currentScope = ""
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line and len(line) > 4:
                    if line[1:3] in ("--", "──"):
                        line = line[3:]
                        package = line.split("@")
                        if len(package) >= 2:
                            idString = '@'.join(package[:-1]).strip()
                            name = formatPackageIdAsName(idString[1:] if idString[0] == "@" else idString).strip()
                            id = idString.strip()
                            version = package[-1].strip()
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, self.NAME+currentScope, Npm))
                    elif "@" in line.split(" ")[0]:
                        currentScope = "@"+line.split(" ")[0][:-1]
                        print("🔵 NPM changed scope to", currentScope)
            p = subprocess.Popen(f"{self.EXECUTABLE} list -g", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ.copy(), shell=True)
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line and len(line) > 4:
                    if line[1:3] in ("--", "──"):
                        line = line[3:]
                        package = line.split("@")
                        if len(package) >= 2:
                            idString = '@'.join(package[:-1]).strip()
                            name = formatPackageIdAsName(idString[1:] if idString[0] == "@" else idString)
                            id = idString
                            version = package[-1].strip()
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, self.NAME+"@global", Npm))
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
            details.InstallerType = "Tarball"
            details.ManifestUrl = f"https://www.npmjs.com/package/{package.Id}"
            details.ReleaseNotesUrl = f"https://www.npmjs.com/package/{package.Id}?activeTab=versions"
            details.Scopes = ["Global"]       
            p = subprocess.Popen(f"{self.EXECUTABLE} info {package.Id}", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ, shell=True)
            output: list[str] = []
            while p.poll() is None:
                line = p.stdout.readline()
                if line:
                    output.append(str(line, encoding='utf-8', errors="ignore").strip())
            lineNo = 0
            ReadingMaintainer = False
            for line in output:
                lineNo += 1
                if lineNo == 2:
                    details.License = line.split("|")[1]
                elif lineNo == 3:
                    details.Description = line.strip()
                elif lineNo == 4:
                    details.HomepageURL = line.strip()
                elif line.startswith(".tarball"):
                    details.InstallerURL = line.replace(".tarball: ", "").strip()
                    try:
                        details.InstallerSize = int(urlopen(details.InstallerURL).length/1000000)
                    except Exception as e:
                        print("🟠 Can't get installer size:", type(e), str(e))
                elif line.startswith(".integrity"):
                    details.InstallerHash = "<br>"+line.replace(".integrity: sha512-", "").replace("==", "").strip()
                elif line.startswith("maintainers:"):
                    ReadingMaintainer = True
                elif ReadingMaintainer:
                    ReadingMaintainer = False
                    details.Author = line.replace("-", "").split("<")[0].strip()
                elif line.startswith("published"):
                    details.Publisher = line.split("by")[-1].split("<")[0].strip()
                    details.UpdateDate = line.split("by")[0].replace("published", "").strip()
            p = subprocess.Popen(f"{self.EXECUTABLE} info {package.Id} versions --json", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ, shell=True)
            output: list[str] = []
            while p.poll() is None:
                line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
                if line.startswith("\""):
                    details.Versions = [line[:-1].replace("\"", "")] + details.Versions # The addition order is inverted, so the latest version shows at the top
            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str) -> QIcon:
        if not self.icon:
            self.icon = QIcon(getMedia("node"))
        return self.icon

    def getParameters(self, options: InstallationOptions) -> list[str]:
        Parameters: list[str] = []
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.InstallationScope:
            if options.InstallationScope in ("Global", _("Global")):
                Parameters.append("--global")
        Parameters += []
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "@global" in package.Source:
            options.InstallationScope = "Global"
        Command = ["cmd.exe", "/C", self.EXECUTABLE, "install", package.Id+("@latest" if options.Version == "" else f"@{options.Version}")] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.path.expanduser("~"), env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()

    def startUpdate(self, package: UpgradablePackage, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "@global" in package.Source:
            options.InstallationScope = "Global"
        Command = ["cmd.exe", "/C", self.EXECUTABLE, "install", package.Id+"@"+package.NewVersion] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.path.expanduser("~"), env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: update {package.Name}").start()
        
    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit(line)
                output += line+"\n"
        match p.returncode:
            case 0:
                outputCode = RETURNCODE_OPERATION_SUCCEEDED
            case other:
                outputCode = RETURNCODE_FAILED 
                
        widget.finishInstallation.emit(outputCode, output)
        
    def startUninstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        if "@global" in package.Source:
            options.InstallationScope = "Global"
        Command = [self.EXECUTABLE, "uninstall", ] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} uninstall with Command", Command)
        p = subprocess.Popen(" ".join(Command), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.path.expanduser("~"), env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: uninstall {package.Name}").start()
        
    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        outputCode = 1
        output = ""
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit(line)
                output += line+"\n"
        match p.returncode:
            case 0:
                outputCode = RETURNCODE_OPERATION_SUCCEEDED
            case other:
                outputCode = RETURNCODE_FAILED        
        widget.finishInstallation.emit(outputCode, output)

    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} --version", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = shutil.which("npm") != None
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').split("\n")[0]
        if signal:
            signal.emit()
        
    def updateSources(self, signal: Signal = None) -> None:
        pass # Handled by the package manager, no need to manually reload
        if signal:
            signal.emit()

Npm = NPMPackageManager()
