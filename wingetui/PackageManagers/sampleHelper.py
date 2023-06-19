import os
import subprocess
import sys
import time

from PySide6.QtCore import *
from tools import *
from tools import _

from .PackageClasses import *


class SamplePackageManager(PackageManagerModule):

    EXECUTABLE = "pacman.exe"
    NAME = "PackageManager"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CAHCE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = []
    BLACKLISTED_PACKAGE_IDS = []
    BLACKLISTED_PACKAGE_VERSIONS = []
    
    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = True
    Capabilities.CanSkipIntegrityChecks = True
    Capabilities.CanRunInteractively = False
    Capabilities.CanRemoveDataOnUninstall = False
    Capabilities.SupportsCustomVersions = True
    Capabilities.SupportsCustomArchitectures = False
    Capabilities.SupportsCustomScopes = False

    if not os.path.exists(CAHCE_FILE_PATH):
        os.makedirs(CAHCE_FILE_PATH)
        
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
                        if len(package) >= 3 and not package[0] in self.BLACKLISTED_PACKAGE_NAMES and not package[1] in self.BLACKLISTED_PACKAGE_IDS and not package[2] in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(formatPackageIdAsName(package[0]), package[1], package[2], self.NAME, self))
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
        """
        INTERNAL METHOD
        Will load the available packages and write them into the cache file
        """
        print(f"🔵 Starting {self.NAME} package caching")
        try:
            p = subprocess.Popen([self.NAME, "search", "*"] , stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            ContentsToCache = ""
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    
                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        self.NAME = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        source = self.NAME
                    else:
                        continue
                    
                    if not self.NAME in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                        ContentsToCache += f"{self.NAME},{id},{version},{source}\n"

            AlreadyCachedPackages = ""
            try:
                with open(self.CACHE_FILE, "r") as f:
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
            p = subprocess.Popen([self.EXECUTABLE, "outdated"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n"+line
                if line:
                    
                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        self.NAME = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        newVersion = line.split("|")[2]
                        source = self.NAME
                    else:
                        continue
                    
                    if not self.NAME in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(UpgradablePackage(self.NAME, id, version, newVersion, source, self))
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
        print(f"🔵 Starting {self.NAME} search for installed packages")
        try:
            packages: list[Package] = []
            p = subprocess.Popen([self.EXECUTABLE, "list", "--local-only"] , stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n"+line
                if line:
                    
                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        self.NAME = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        source = self.NAME
                    else:
                        continue
                    
                    if not self.NAME in self.BLACKLISTED_PACKAGE_NAMES and not id in self.BLACKLISTED_PACKAGE_IDS and not version in self.BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(self.NAME, id, version, source, self))
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
        print(f"🔵 Starting get info for {package.self.NAME} on {self.NAME}")
        details = PackageDetails(package)
        try:
            
            # The code that loads the package details goes here
                    
            print(f"🟢 Get info finished for {package.self.NAME} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(source: str = "") -> QIcon:
        return QIcon()
    
    def getParameters(self, options: InstallationOptions) -> list[str]:
        Parameters: list[str] = []
        if options.Architecture:
            Parameters += ["-a", options.Architecture]
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.InstallationScope:
            Parameters += ["-s", options.InstallationScope]
        if options.InteractiveInstallation:
            Parameters.append("--interactive")
        if options.RemoveDataOnUninstall:
            Parameters.append("--remove-user-data")
        if options.SkipHashCheck:
            Parameters += ["--skip-integrity-checks", "--force"]
        if options.Version:
            Parameters += ["--version", options.Version]
        return Parameters
    
    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        print("🔴 This function should be reimplented!")
        Command: list[str] = [self.EXECUTABLE, "install", package.Name] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()
        return p

    def startUpdate(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        print("🔴 This function should be reimplented!")
        Command: list[str] = [self.EXECUTABLE, "install", package.Name] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: updating {package.Name}").start()
        return p

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        output = ""
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                output += line+"\n"
                widget.addInfoLine.emit(line)
                if "downloading" in line:
                    widget.counterSignal.emit(3)
                elif "installing" in line:
                    widget.counterSignal.emit(7)
        print(p.returncode)
        widget.finishInstallation.emit(p.returncode, output)

    def startUninstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        print("🔴 This function should be reimplented!")
        Command: list[str] = [self.EXECUTABLE, "install", package.Name] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: updating {package.Name}").start()
        return p

    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: InstallationWidgetType):
        output = ""
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                output += line+"\n"
                widget.addInfoLine.emit(line)
                if "removing" in line:
                    widget.counterSignal.emit(5)
        print(p.returncode)
        widget.finishInstallation.emit(p.returncode, output)
        
    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -v", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = o.returncode == 0
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", "")
        if signal:
            signal.emit()
        
    def updateSources(self, signal: Signal = None) -> None:
        subprocess.run(f"{self.EXECUTABLE} update self", shell=True, stdout=subprocess.PIPE)
        if signal:
            signal.emit()
            
class DynamicLoadPackageManager(SamplePackageManager):
        
    def getAvailablePackages(self, second_attempt: bool = False) -> list[Package]:
        print(f"🟠 Package manager {self.NAME} does not support listing available packages")
        return []
    
    def cacheAvailablePackages(self) -> None:
        print(f"🟠 Package manager {self.NAME} does not support caching available packages")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        raise NotImplementedError("This method must be reimplemented")

if(__name__=="__main__"):
    import __init__