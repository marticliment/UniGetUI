from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _
from .PackageClasses import *
from .sampleHelper import *
    
    
class PipPackageManager(DynamicLoadPackageManager):

    ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

    EXECUTABLE = "python.exe -m pip"

    NAME = "Pip"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = ["WARNING:", "[notice]", "Package"]
    BLACKLISTED_PACKAGE_IDS = ["WARNING:", "[notice]", "Package"]
    BLACKLISTED_PACKAGE_VERSIONS = ["Ignoring", "invalie"]

    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = True
    Capabilities.CanSkipIntegrityChecks = False
    Capabilities.CanRunInteractively = False
    Capabilities.CanRemoveDataOnUninstall = False
    Capabilities.SupportsCustomVersions = False # TODO: implement version listing
    Capabilities.SupportsCustomArchitectures = False
    Capabilities.SupportsCustomScopes = False # TODO: implement --user custom scope
    
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
            if shutil.which("parse_pip_search") == None:
                print("🟡 Installing pip-search, that was missing...")
                Command = self.EXECUTABLE.split(" ") + ["install", "parse_pip_search"] + self.getParameters(InstallationOptions())
                p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                p.wait()
            packages: list[Package] = []
            p = subprocess.Popen(f"parse_pip_search {query}", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            DashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not DashesPassed:
                        if "----" in line:
                            DashesPassed = True
                    else:
                        package = list(filter(None, line.split("|")))
                        if len(package) >= 2:
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            source = self.NAME
                            if not name in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Pip))
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

    def getParameters(self, options: InstallationOptions, removeprogressbar: bool = True) -> list[str]:
        Parameters: list[str] = []
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.InstallationScope:
            if options.InstallationScope in ("User", _("User")):
                Parameters.append("--user")
        Parameters += ["--no-input", "--no-color", "--no-python-version-warning", "--no-cache"]
        if removeprogressbar:
            Parameters += ["--progress-bar", "off"]
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        Command = self.EXECUTABLE.split(" ") + ["install", package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()

    def startUpdate(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        Command = self.EXECUTABLE.split(" ") + ["install", package.Id, "--upgrade"] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
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
        Command = self.EXECUTABLE.split(" ") + ["uninstall", package.Id, "-y"] + self.getParameters(options, removeprogressbar=False)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} uninstall with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
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
        o = subprocess.run(f"{self.EXECUTABLE} -V", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = shutil.which("python.exe") != None
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').split("\n")[0]
        if signal:
            signal.emit()
        
    def updateSources(self, signal: Signal = None) -> None:
        pass # Handled by the package manager, no need to manually reload
        if signal:
            signal.emit()

Pip = PipPackageManager()
