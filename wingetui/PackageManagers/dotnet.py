"""

wingetui/PackageManagers/sampleHelper.py

This file holds a sample package manager implementation. The code here must be reimplemented before being used

"""

if __name__ == "__main__":
    import subprocess
    import os
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "..")).returncode)


import os
import subprocess

from PySide6.QtCore import *
from tools import *
from tools import _

from .PackageClasses import *


class DotNetToolPackageManager(PackageManagerModule):

    EXECUTABLE = "dotnet.exe"
    NAME = ".NET Tool"

    BLACKLISTED_PACKAGE_NAMES = []
    BLACKLISTED_PACKAGE_IDS = []
    BLACKLISTED_PACKAGE_VERSIONS = []

    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = True
    Capabilities.SupportsCustomVersions = True
    Capabilities.SupportsCustomArchitectures = True
    Capabilities.SupportsPreRelease = True

    LoadedIcons = False

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} query search")
        try:
            p = subprocess.Popen([self.EXECUTABLE, "tool", "search", query], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            packages: list[Package] = []
            dashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not dashesPassed:
                        if "---" in line:
                            dashesPassed = True
                    else:
                        if len(line.split(" ")) >= 2:
                            package = list(filter(None, line.split(" ")))
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            source = self.NAME
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Dotnet))
                        else:
                            continue

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
            if shutil.which("dotnet-tools-outdated") is None:
                print("🟡 Installing dotnet-tools-outdated, that was missing...")
                Command = [self.EXECUTABLE, "tool", "install", "--global", "dotnet-tools-outdated", "--global"]
                p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                p.wait()
                print(p.stdout.readlines())

            rawoutput: str = "\n--------dotnet\n\n"
            packages: list[UpgradablePackage] = []
            p = subprocess.Popen(["dotnet-tools-outdated"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            dashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    rawoutput += line + "\n"
                    if not dashesPassed:
                        if "---" in line:
                            dashesPassed = True
                    else:
                        if len(line.split(" ")) >= 3:
                            package = list(filter(None, line.split(" ")))
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            newVersion = package[2]
                            source = self.NAME
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, version, newVersion, source, Dotnet))

            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            globals.PackageManagerOutput += rawoutput
            return packages
        except Exception as e:
            report(e)
            return []

    def getInstalledPackages(self, second_attempt=False) -> list[Package]:
        f"""
        Will retieve the intalled packages by {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} search for installed packages")
        try:
            rawoutput = "\n\n-------dotnet\n"
            packages: list[Package] = []
            p = subprocess.Popen([self.EXECUTABLE, "tool", "list", "--global"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, env=os.environ.copy())
            dashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    rawoutput += line + "\n"
                    if not dashesPassed:
                        if "---" in line:
                            dashesPassed = True
                    else:
                        if len(line.split(" ")) >= 2:
                            package = list(filter(None, line.split(" ")))
                            name = formatPackageIdAsName(package[0])
                            id = package[0]
                            version = package[1]
                            source = self.NAME
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Dotnet))
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            globals.PackageManagerOutput += rawoutput + "\n\n"
            if len(packages) <= 2 and not second_attempt:
                print("🟠 Chocolatey got too few installed packages, retrying")
                return self.getInstalledPackages(second_attempt=True)
            else:
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

            details.ManifestUrl = "https://www.nuget.org/packages/" + package.Id
            url = f"http://www.nuget.org/api/v2/Packages(Id='{package.Id}',Version='')"

            apiContents = urlopen(url).read().decode("utf-8", errors="ignore")

            details.InstallerURL = f"https://www.nuget.org/api/v2/package/{package.Id}/{package.Version}"
            details.InstallerType = "NuPkg"
            try:
                details.InstallerSize = int(urlopen(details.InstallerURL).length / 1000000)
            except Exception as e:
                report(e)

            for match in re.findall(r"<name>[^<>]+<\/name>", apiContents):
                details.Author = match.replace("<name>", "").replace("</name>", "")
                details.Publisher = match.replace("<name>", "").replace("</name>", "")
                break

            for match in re.findall(r"<d:Description>[^<>]+<\/d:Description>", apiContents):
                details.Description = match.replace("<d:Description>", "").replace("</d:Description>", "")
                break

            for match in re.findall(r"<updated>[^<>]+<\/updated>", apiContents):
                details.UpdateDate = match.replace("<d:LastUpdated>", "").replace("</d:LastUpdated>", "")
                break

            for match in re.findall(r"<d:ProjectUrl>[^<>]+<\/d:ProjectUrl>", apiContents):
                details.HomepageURL = match.replace("<d:ProjectUrl>", "").replace("</d:ProjectUrl>", "")
                break

            for match in re.findall(r"<d:LicenseUrl>[^<>]+<\/d:LicenseUrl>", apiContents):
                details.LicenseURL = match.replace("<d:LicenseUrl>", "").replace("</d:LicenseUrl>", "")
                break

            for match in re.findall(r"<d:PackageHash>[^<>]+<\/d:PackageHash>", apiContents):
                details.InstallerHash = match.replace("<d:PackageHash>", "").replace("</d:PackageHash>", "")
                break

            for match in re.findall(r"<d:ReleaseNotes>[^<>]+<\/d:ReleaseNotes>", apiContents):
                details.ReleaseNotes = match.replace("<d:ReleaseNotes>", "").replace("</d:ReleaseNotes>", "")
                break

            for match in re.findall(r"<d:LicenseNames>[^<>]+<\/d:LicenseNames>", apiContents):
                details.License = match.replace("<d:LicenseNames>", "").replace("</d:LicenseNames>", "")
                break

            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str = "") -> QIcon:
        if not self.LoadedIcons:
            self.LoadedIcons = True
            self.Icon = QIcon(getMedia("dotnet"))
        return self.Icon

    def getParameters(self, options: InstallationOptions, isAnUninstall: bool = False) -> list[str]:
        Parameters: list[str] = ["--global"]
        if not isAnUninstall:
            if options.Architecture:
                Parameters += ["-a", options.Architecture]
            if options.Version:
                Parameters += ["--version", options.Version]
            if options.PreRelease:
                Parameters += ["--prerelease"]
        if options.CustomParameters:
            Parameters += options.CustomParameters
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "tool", "install", package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()
        return p

    def startUpdate(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "tool", "update", package.Id] + self.getParameters(options)
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
        outputCode = p.returncode
        if outputCode != 0:
            if "is already installed" in output:
                outputCode = RETURNCODE_OPERATION_SUCCEEDED
        widget.finishInstallation.emit(outputCode, output)

    def startUninstallation(self, package: Package, options: InstallationOptions, widget: InstallationWidgetType) -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "tool", "uninstall", package.Id] + self.getParameters(options, False)
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
        print(p.returncode)
        widget.finishInstallation.emit(p.returncode, output)

    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE}  --version", shell=True, stdout=subprocess.PIPE)
        globals.componentStatus[f"{self.NAME}Found"] = o.returncode == 0
        globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", "")
        if signal:
            signal.emit()

    def updateSources(self, signal: Signal = None) -> None:
        pass  # This package manager does not need source refreshing
        if signal:
            signal.emit()


Dotnet = DotNetToolPackageManager()
