if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)

import os
import subprocess


from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.PackageEngine.Classes import *


class PowershellPackageManager(PackageManagerWithSources):

    EXECUTABLE = "powershell.exe"
    NAME = "PowerShell"

    def __init__(self):
        super().__init__()
        self.Capabilities = PackageManagerCapabilities()
        self.Capabilities.CanRunAsAdmin = True
        self.Capabilities.CanSkipIntegrityChecks = True
        self.Capabilities.SupportsCustomVersions = True
        self.Capabilities.SupportsCustomScopes = True
        self.Capabilities.SupportsCustomSources = True
        self.Capabilities.SupportsPreRelease = True

        self.Properties.Name = self.NAME
        self.Properties.Description = _("")  # TODO: Add description
        self.Properties.Icon = getMedia("powershell")
        self.Properties.ColorIcon = getMedia("powershell_color")
        self.IconPath = self.Properties.Icon

        self.Properties.InstallVerb = "Install-Module"
        self.Properties.UpdateVerb = "Update-Module"
        self.Properties.UninstallVerb = "Uninstall-Module"
        self.Properties.ExecutableName = "powershell -Command"
        self.IconPath = getMedia("powershell")

        self.KnownSources = [
            ManagerSource(self, "PSGallery", "https://www.powershellgallery.com/api/v2"),
            ManagerSource(self, "PoshTestGallery", "https://www.poshtestgallery.com/api/v2"),
        ]

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} query search")
        try:
            p = subprocess.Popen([self.EXECUTABLE, "-Command", "Find-Module", query], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
            packages: list[Package] = []
            dashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line:
                    if not dashesPassed:
                        if "---" in line:
                            dashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        name = formatPackageIdAsName(package[1])
                        id = package[1]
                        version = package[0]
                        source = f"{self.NAME}: {package[2]}"

                        if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(name, id, version, source, Powershell))

            print(f"🟢 {self.NAME} package query finished successfully")
            return packages
        except Exception as e:
            report(e)
            return []

    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        f"""
        Will retieve the upgradable packages by {self.NAME} in the format of a list[UpgradablePackage] object.
        """
        Sources = self.getSources()
        SourceDict = "{"
        for source in Sources:
            SourceDict += f'"{source.Name}" = "{source.Url}";'
        SourceDict += "}"

        Command = """
        function Test-GalleryModuleUpdate
        {
            param
            (
                [Parameter(Mandatory,ValueFromPipelineByPropertyName)]
                [string]
                $Name,

                [Parameter(Mandatory,ValueFromPipelineByPropertyName)]
                [version]
                $Version,

                [Parameter(Mandatory,ValueFromPipelineByPropertyName)]
                [string]
                $Repository,

                [switch]
                $NeedUpdateOnly
            )

            process
            {


                $URLs = @""" + SourceDict + """

                $page = Invoke-WebRequest -Uri ($URLs[$Repository] + "/package/$Name") -UseBasicParsing -Maximum 0 -ea Ignore
                [version]$latest = Split-Path -Path ($page.Headers.Location -replace "$Name." -replace ".nupkg") -Leaf
                $needsupdate = $Latest -gt $Version

                if ($needsupdate)
                {
                    Write-Output ($Name + "|" + $Version.ToString() + "|" + $Latest.ToString() + "|" + $Repository)
                }
            }
        }

        Get-InstalledModule | Test-GalleryModuleUpdate
        exit
        """
        print(f"🔵 Starting {self.NAME} search for updates")
        try:
            packages: list[UpgradablePackage] = []
            p = subprocess.run(self.EXECUTABLE, input=bytes(Command, "utf-8"), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            for line in p.stdout.decode("utf-8").split("\n"):
                rawoutput += "\n" + line
                if line and not line.startswith(">>") and not line.startswith("PS "):
                    package = list(filter(None, line.split("|")))
                    if len(package) >= 4:
                        name = formatPackageIdAsName(package[0])
                        id = package[0]
                        version = package[1]
                        newVersion = package[2]
                        source = f"{self.NAME}: {package[3]}"

                        if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(UpgradablePackage(name, id, version, newVersion, source, self))

            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            Globals.PackageManagerOutput += rawoutput
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
            p = subprocess.Popen([self.EXECUTABLE, "-Command", "Get-InstalledModule"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            rawoutput = "\n\n---------"
            dashesPassed = False
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n" + line
                if line:
                    if not dashesPassed:
                        if "----" in line:
                            dashesPassed = True
                    else:
                        package = list(filter(None, line.split(" ")))
                        name = formatPackageIdAsName(package[1])
                        id = package[1]
                        version = package[0]
                        source = f"{self.NAME}: {package[2]}"

                        if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(name, id, version, source, Powershell))

            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            Globals.PackageManagerOutput += rawoutput
            return packages
        except Exception as e:
            report(e)
            return []

    def getPackageDetails(self, package: Package) -> PackageDetails:
        """
        Will return a PackageDetails object containing the information of the given Package object
        """
        print(f"🔵 Starting get info for {package.Id} on {self.NAME}")
        details = PackageDetails(package)
        details.Scopes = ["AllUsers", "CurrentUser"]
        try:
            p = subprocess.Popen(f"\"Find-Module -Name {package.Id} | Get-Member -MemberType NoteProperty\"", executable=shutil.which("powershell"), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)

            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line and "NoteProperty" in line:
                    if line.startswith("Description"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.Description = content if content != "null" else ""

                    elif line.startswith("Author"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.Author = content if content != "null" else ""

                    elif line.startswith("CompanyName"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.Publisher = content if content != "null" else ""

                    elif line.startswith("LicenseUri"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.LicenseURL = content if content != "null" else ""

                    elif line.startswith("Copyright"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.License = content if content != "null" else ""

                    elif line.startswith("PackageManagementProvider"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.InstallerType = content if content != "null" else ""

                    elif line.startswith("ProjectUri"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.HomepageURL = content if content != "null" else ""

                    elif line.startswith("PublishedDate"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.UpdateDate = content if content != "null" else ""

                    elif line.startswith("ReleaseNotes"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.ReleaseNotes = content if content != "null" else ""

                    elif line.startswith("PublishedDate"):
                        content = "=".join(line.split("=")[1:]).strip()
                        details.UpdateDate = content if content != "null" else ""

            p = subprocess.Popen(f"\"Find-Module -Name {package.Id} -AllVersions\"", executable=shutil.which("powershell"), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)

            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                if line and package.Id in line:
                    details.Versions += [line.split(" ")[0].strip()]

            print(f"🟢 Get info finished for {package.Id} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str = "") -> str:
        if not self.LoadedIcons:
            self.LoadedIcons = True
            self.icon = getMedia("powershell")
        return self.icon

    def getParameters(self, options: InstallationOptions, isAnUninstall: bool = False, isAnUpdate: bool = False) -> list[str]:
        Parameters: list[str] = ["-Confirm:$false", "-Force"]
        if options.CustomParameters:
            Parameters += options.CustomParameters
        if options.SkipHashCheck:
            Parameters += ["-SkipPublisherCheck"]
        if not isAnUninstall and not isAnUpdate:
            Parameters: list[str] = ["-AcceptLicense", "-AllowClobber"]
            if not options.RunAsAdministrator:
                options.InstallationScope = "CurrentUser"
            else:
                options.InstallationScope = "AllUsers"
            if options.InstallationScope:
                Parameters += ["-Scope", options.InstallationScope]
                if options.InstallationScope == "AllUsers":
                    options.RunAsAdministrator = True
            if options.Version:
                Parameters += ["-RequiredVersion", options.Version]
        if not isAnUninstall:
            if options.PreRelease:
                Parameters += ["-AllowPrerelease"]
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "-Command", "Install-Module", "-Name", package.Id] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Id}").start()
        return p

    def startUpdate(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "-Command", "Update-Module", "-Name", package.Id] + self.getParameters(options, isAnUpdate=True)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: updating {package.Id}").start()
        return p

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: 'PackageInstallerWidget'):
        output = ""
        while p.poll() is None:
            line, is_newline = getLineFromStdout(p)
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                output += line + "\n"
                widget.addInfoLine.emit((line, is_newline))
                if "downloading" in line:
                    widget.counterSignal.emit(3)
                elif "installing" in line:
                    widget.counterSignal.emit(7)
        c = p.returncode
        if "AdminPrivilegesAreRequired" in output:
            c = RETURNCODE_NEEDS_ELEVATION
        widget.finishInstallation.emit(c, output)

    def startUninstallation(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command: list[str] = [self.EXECUTABLE, "-Command", "Uninstall-Module", "-Name", package.Id] + self.getParameters(options, isAnUninstall=True)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: updating {package.Id}").start()
        return p

    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: 'PackageInstallerWidget'):
        output = ""
        while p.poll() is None:
            line, is_newline = getLineFromStdout(p)
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                output += line + "\n"
                widget.addInfoLine.emit((line, is_newline))
                if "removing" in line:
                    widget.counterSignal.emit(5)
        c = p.returncode
        if "AdminPrivilegesAreRequired" in output:
            c = RETURNCODE_NEEDS_ELEVATION
        widget.finishInstallation.emit(c, output)

    def getSources(self) -> list[ManagerSource]:
        print(f"🔵 Starting {self.NAME} source search...")
        p = subprocess.Popen([self.EXECUTABLE, "-Command", "Get-PSRepository"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        output = []
        dashesPassed = False
        sources: list[ManagerSource] = []
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if not dashesPassed:
                    if b"---" in line:
                        dashesPassed = True
                else:
                    output.append(str(line, encoding='utf-8', errors="ignore"))
        for element in output:
            try:
                while "  " in element.strip():
                    element = element.strip().replace("  ", " ")
                element: list[str] = element.split(" ")
                sources.append(ManagerSource(self, element[0].strip(), element[2].strip()))
            except Exception as e:
                report(e)
        print(f"🟢 {self.NAME} source search finished with {len(sources)} sources")
        return sources

    def installSource(self, source: ManagerSource, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command = [self.EXECUTABLE, "-Command", "Register-PSRepository", "-Name", source.Name, "-SourceLocation", source.Url]
        print(f"🔵 Starting source {source.Name} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.sourceProgressThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing source {source.Name}").start()
        return p

    def uninstallSource(self, source: ManagerSource, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command = [self.EXECUTABLE, "-Command", "Unregister-PSRepository", "-Name", source.Name]
        print(f"🔵 Starting source {source.Name} removal with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.sourceProgressThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing source {source.Name}").start()
        return p

    def sourceProgressThread(self, p: subprocess.Popen, options: InstallationOptions, widget: 'PackageInstallerWidget'):
        output = ""
        counter = 0
        while p.poll() is None:
            line, is_newline = getLineFromStdout(p)
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit((line, is_newline))
                counter += 1
                widget.counterSignal.emit(counter)
                if is_newline:
                    output += line + "\n"
        p.wait()
        widget.finishInstallation.emit(p.returncode, output)

    def detectManager(self, signal: 'Signal' = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -v", shell=True, stdout=subprocess.PIPE)
        Globals.componentStatus[f"{self.NAME}Found"] = o.returncode == 0
        Globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", "")
        if signal:
            signal.emit()

    def updateSources(self, signal: 'Signal' = None) -> None:
        subprocess.run(f"{self.EXECUTABLE} update self", shell=True, stdout=subprocess.PIPE)
        if signal:
            signal.emit()


Powershell = PowershellPackageManager()


if __name__ == "__main__":
    import __init__
