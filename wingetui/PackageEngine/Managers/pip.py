if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)

import os
import re
import subprocess

from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.PackageEngine.Classes import *


class PipPackageManager(PackageManagerModule):

    ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

    EXECUTABLE = "python.exe -m pip"

    NAME = "Pip"

    def __init__(self):
        super().__init__()
        self.Capabilities.CanRunAsAdmin = True
        self.Capabilities.SupportsCustomVersions = True
        self.Capabilities.SupportsCustomScopes = True
        self.Capabilities.SupportsPreRelease = True

        self.Properties.Name = self.NAME
        self.Properties.Description = _("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>")
        self.Properties.Icon = getMedia("python")
        self.Properties.ColorIcon = getMedia("pip_color")
        self.IconPath = self.Properties.Icon

        self.Properties.InstallVerb = "install"
        self.Properties.UpdateVerb = "install --upgrade"
        self.Properties.UninstallVerb = "uninstall"
        self.Properties.ExecutableName = "python -m pip"

        self.BLACKLISTED_PACKAGE_NAMES = ["WARNING:", "[notice]", "Package"]
        self.BLACKLISTED_PACKAGE_IDS = ["WARNING:", "[notice]", "Package"]
        self.BLACKLISTED_PACKAGE_VERSIONS = ["Ignoring", "invalie"]

    def isEnabled(self) -> bool:
        return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        f"""
        Will retieve the packages for the given "query: str" from the package manager {self.NAME} in the format of a list[Package] object.
        """
        print(f"🔵 Starting {self.NAME} search for dynamic packages")
        try:
            if shutil.which("parse_pip_search") is None:
                print("🟡 Installing pip-search, that was missing...")
                Command = self.EXECUTABLE.split(" ") + ["install", "parse_pip_search", "--no-input", "--no-color", "--no-python-version-warning", "--no-cache"]
                p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                p.wait()
            packages: list[Package] = []
            p = subprocess.Popen(f"parse_pip_search {query}", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            DashesPassed = False
            rawoutput = "\n\n---------" + self.NAME
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n" + line
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
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, source, Pip))
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            Globals.PackageManagerOutput += rawoutput
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
            rawoutput = "\n\n---------" + self.NAME
            while p.poll() is None:
                line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
                rawoutput += "\n" + line
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
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS and newVersion not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(UpgradablePackage(name, id, version, newVersion, source, Pip))
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            Globals.PackageManagerOutput += rawoutput
            return packages
        except Exception as e:
            report(e)
            return []

    def getInstalledPackages(self, second_attempt: bool = False) -> list[Package]:
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
                            if name not in self.BLACKLISTED_PACKAGE_NAMES and id not in self.BLACKLISTED_PACKAGE_IDS and version not in self.BLACKLISTED_PACKAGE_VERSIONS:
                                packages.append(Package(name, id, version, self.NAME, Pip))
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            if len(packages) == 0 and not second_attempt:
                print("🟠 Pip got too few installed packages, retrying")
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
            details.ManifestUrl = f"https://pypi.org/project/{package.Id}/"
            details.ReleaseNotesUrl = f"https://pypi.org/project/{package.Id}/#history"
            details.InstallerURL = f"https://pypi.org/project/{package.Id}/#files"
            details.Scopes = [_("User")]
            details.InstallerType = "Pip"

            rawcontent = urlopen(f"https://pypi.org/pypi/{package.Id}/json").read().decode("utf-8", errors="ignore")
            basejson = json.loads(rawcontent)
            content = basejson["info"]

            if "author" in content:
                details.Author = content["author"]
            if "home_page" in content:
                details.HomepageURL = content["home_page"]
            if "package_url" in content:
                details.ManifestUrl = content["package_url"]
            if "summary" in content:
                details.Description = content["summary"]
            if "classifiers" in content:
                for line in content["classifiers"]:
                    if "License ::" in line:
                        details.License = line.split("::")[-1].strip()
                    elif "Topic ::" in line:
                        if line.split("::")[-1].strip() not in details.Tags:
                            details.Tags.append(line.split("::")[-1].strip())
            if "license" in content:
                if content["license"]:
                    details.License = content["license"]
            if "maintainer" in content:
                if content["maintainer"]:
                    details.Publisher = content["maintainer"]

            url = basejson["urls"][0]

            if "upload_time" in url:
                details.UpdateDate = url["upload_time"]
            if "url" in url:
                details.InstallerURL = url["url"]
                details.InstallerType = url["url"].split(".")[-1].capitalize().replace("Whl", "Wheel")
            if "size" in url:
                details.InstallerSize = int(url["size"]) / 1000000
            if "digests" in url:
                if "sha256" in url["digests"]:
                    details.InstallerHash = url["digests"]["sha256"]

            details.Description = ConvertMarkdownToHtml(details.Description)
            details.ReleaseNotes = ConvertMarkdownToHtml(details.ReleaseNotes)

            p = subprocess.Popen(f"{self.EXECUTABLE} index versions {package.Id}", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.path.expanduser("~"), env=os.environ, shell=True)
            output: list[str] = []
            while p.poll() is None:
                line = p.stdout.readline()
                if line:
                    output.append(str(line, encoding='utf-8', errors="ignore").strip())
            for line in output:
                if "Available versions:" in line:
                    details.Versions = [v.strip() for v in line.replace("Available versions:", "").split(",")]
                    break

            print(f"🟢 Get info finished for {package.Name} on {self.NAME}")
            return details
        except Exception as e:
            report(e)
            return details

    def getIcon(self, source: str) -> QIcon:
        if not self.LoadedIcons:
            self.LoadedIcons = True
            self.icon = QIcon(getMedia("python"))
        return self.icon

    def getParameters(self, options: InstallationOptions, isAnUninstall: bool = False) -> list[str]:
        Parameters: list[str] = []
        if options.CustomParameters:
            Parameters += options.CustomParameters
        Parameters += ["--no-input", "--no-color", "--no-python-version-warning", "--no-cache"]
        if not isAnUninstall:
            Parameters += ["--progress-bar", "off"]
            if options.PreRelease:
                Parameters.append("--pre")
            if options.InstallationScope:
                if options.InstallationScope in ("User", _("User")):
                    Parameters.append("--user")
        return Parameters

    def startInstallation(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        idtoInstall = package.Id
        if options.Version:
            idtoInstall += "==" + options.Version
        Command = self.EXECUTABLE.split(" ") + ["install", idtoInstall] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} installation with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: installing {package.Name}").start()
        return p

    def startUpdate(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        idtoInstall = package.Id
        if options.Version:
            idtoInstall += "==" + options.Version
        Command = self.EXECUTABLE.split(" ") + ["install", idtoInstall, "--upgrade"] + self.getParameters(options)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} update with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: update {package.Name}").start()
        return p

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: 'PackageInstallerWidget'):
        output = ""
        while p.poll() is None:
            line, is_newline = getLineFromStdout(p)
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit((line, is_newline))
                if is_newline:
                    output += line + "\n"
        match p.returncode:
            case 0:
                outputCode = RETURNCODE_OPERATION_SUCCEEDED
            case other:
                print(other)
                outputCode = RETURNCODE_FAILED
        if "--user" in output:
            outputCode = RETURNCODE_NEEDS_PIP_ELEVATION
        widget.finishInstallation.emit(outputCode, output)

    def startUninstallation(self, package: Package, options: InstallationOptions, widget: 'PackageInstallerWidget') -> subprocess.Popen:
        Command = self.EXECUTABLE.split(" ") + ["uninstall", package.Id, "-y"] + self.getParameters(options, True)
        if options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {package} uninstall with Command", Command)
        p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.uninstallationThread, args=(p, options, widget,), name=f"{self.NAME} installation thread: uninstall {package.Name}").start()
        return p

    def uninstallationThread(self, p: subprocess.Popen, options: InstallationOptions, widget: 'PackageInstallerWidget'):
        outputCode = 1
        output = ""
        while p.poll() is None:
            line, is_newline = getLineFromStdout(p)
            line = line.strip()
            line = str(line, encoding='utf-8', errors="ignore").strip()
            if line:
                widget.addInfoLine.emit((line, is_newline))
                output += line + "\n"
        match p.returncode:
            case 0:
                outputCode = RETURNCODE_OPERATION_SUCCEEDED
            case other:
                print(other)
                outputCode = RETURNCODE_FAILED
        if "--user" in output:
            outputCode = RETURNCODE_NEEDS_PIP_ELEVATION
        widget.finishInstallation.emit(outputCode, output)

    def detectManager(self, signal: Signal = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -V", shell=True, stdout=subprocess.PIPE)
        Globals.componentStatus[f"{self.NAME}Found"] = shutil.which("python.exe") is not None
        Globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", " ").replace("\r", " ")
        if signal:
            signal.emit()

    def updateSources(self, signal: Signal = None) -> None:
        pass  # Handled by the package manager, no need to manually reload
        if signal:
            signal.emit()


Pip = PipPackageManager()
