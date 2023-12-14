"""

wingetui/PackageManagers/sampleHelper.py

This file holds a sample package manager implementation. The code here must be reimplemented before being used

"""

if __name__ == "__main__":
    import subprocess, os, sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "..\\..")).returncode)


import os
import subprocess

from PySide6.QtCore import *
from wingetui.tools import *
from wingetui.tools import _

from .PackageClasses import *


class SamplePackageManager(PackageManagerModule):

    EXECUTABLE = "pacman.exe"
    NAME = "PackageManager"

    def __init__(self):
        super().__init__()
        self.IconPath = getMedia("PackageManagerIcon")
        self.Capabilities.CanRunAsAdmin = True
        self.Capabilities.CanSkipIntegrityChecks = True
        self.Capabilities.CanRunInteractively = True
        self.Capabilities.CanRemoveDataOnUninstall = True
        self.Capabilities.SupportsCustomVersions = True
        self.Capabilities.SupportsCustomArchitectures = True
        self.Capabilities.SupportsCustomScopes = True
        self.Capabilities.SupportsPreRelease = True

    LoadedIcons = False

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} query search")
        try:
            p = subprocess.Popen([self.EXECUTABLE, "search", "*"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            packages: list[Package] = []
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:

                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        name = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        source = self.NAME
                    else:
                        continue

                    if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(name, id, version, source, None))

            print(f"🟢 {self.NAME} package query finished successfully")
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
            p = subprocess.Popen([self.EXECUTABLE, "outdated"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n" + line
                if line:

                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        name = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        newVersion = line.split("|")[2]
                        source = self.NAME
                    else:
                        continue

                    if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(UpgradablePackage(name, id, version, newVersion, source, self))
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
            p = subprocess.Popen([self.EXECUTABLE, "list", "--local-only"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n" + line
                if line:

                    if len(line.split("|")) >= 3:
                        # Replace these lines with the parse mechanism
                        name = formatPackageIdAsName(line.split("|")[0])
                        id = line.split("|")[0]
                        version = line.split("|")[1]
                        source = self.NAME
                    else:
                        continue

                    if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(name, id, version, source, self))
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

    def getIcon(self, source: str = "") -> QIcon:
        if not self.LoadedIcons:
            self.LoadedIcons = True
        return QIcon()

    def getParameters(self, options: InstallationOptions, isAnUninstall: bool = False) -> list[str]:
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
            line, is_newline = getLineFromStdout(p)
            line = str(line, encoding='utf-8', errors="ignore").strip()

            if line:
                if is_newline:
                    output += line + "\n"
                widget.addInfoLine.emit((line, is_newline))
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
            line, is_newline = getLineFromStdout(p)
            line = str(line, encoding='utf-8', errors="ignore").strip()

            if line:
                if is_newline:
                    output += line + "\n"
                widget.addInfoLine.emit((line, is_newline))
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
