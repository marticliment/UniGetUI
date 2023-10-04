"""

wingetui/PackageManagers/composer.py

This file holds the Composer Package Manager related code.

"""

if __name__ == "__main__":
    import os
    import subprocess
    import sys

    sys.exit(
        subprocess.run(
            ["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "..")
        ).returncode
    )

import os
import subprocess

import globals
from PackageManagers.PackageClasses import (
    DynamicPackageManager, Package, PackageManagerCapabilities, UpgradablePackage
)
from wingetui import compressExcessSpaces, formatPackageIdAsName, getSettings, report


class ComposerPackageManager(DynamicPackageManager):
    EXECUTABLE = "composer global"
    EXECUTABLE_COMMON_ARGS = "--no-interaction --no-plugins --no-scripts --no-ansi"
    NAME = "Composer"
    CACHE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
    CACHE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

    BLACKLISTED_PACKAGE_NAMES = []
    BLACKLISTED_PACKAGE_IDS = []
    BLACKLISTED_PACKAGE_VERSIONS = []

    Capabilities = PackageManagerCapabilities()
    Capabilities.CanRunAsAdmin = False
    Capabilities.CanSkipIntegrityChecks = False
    Capabilities.CanRunInteractively = False
    Capabilities.CanRemoveDataOnUninstall = False
    Capabilities.SupportsCustomVersions = False
    Capabilities.SupportsCustomArchitectures = False
    Capabilities.SupportsCustomScopes = False

    LoadedIcons = False

    if not os.path.exists(CACHE_FILE_PATH):
        os.makedirs(CACHE_FILE_PATH)

    def _parseComposerShowLine(self, line: str, include_new_version: bool = False) -> list[str] | None:
        """
        Handles parsing of a single line from `composer show` into a list as `[name, id, version, new_version]`
        """
        line: str = compressExcessSpaces(line)

        # Skip irrelevant lines
        line_chunks: list[str] = line.split(" ")
        if "/" not in line_chunks[0]:
            return

        package_id: str = line_chunks[0]
        if package_id in self.BLACKLISTED_PACKAGE_IDS:
            return

        package_version: str = line_chunks[1]
        if package_version in self.BLACKLISTED_PACKAGE_VERSIONS:
            return

        package_new_version: str = ""
        if include_new_version:
            package_new_version = line_chunks[3]
            if package_new_version in self.BLACKLISTED_PACKAGE_VERSIONS:
                return

        # `author/package-name` -> `Author - Package Name`
        package_name_chunks: list[str] = line_chunks[0].split("/")
        package_name: str = f"{formatPackageIdAsName(package_name_chunks[0])}"
        package_name += f" - {formatPackageIdAsName(package_name_chunks[1])}"
        if package_name in self.BLACKLISTED_PACKAGE_NAMES:
            return

        return [package_name, package_id, package_version, package_new_version]

    def isEnabled(self) -> bool:
        return True  # TODO: Remove this debug line. Original line: return not getSettings(f"Disable{self.NAME}")

    def getPackagesForQuery(self, query: str) -> list[Package]:
        print(f"🔵 Starting {self.NAME} search for dynamic packages.")
        try:
            packages: list[Package] = []
            process: subprocess.Popen = subprocess.Popen(
                f"{self.EXECUTABLE} {self.EXECUTABLE_COMMON_ARGS} search {query}",
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                stdin=subprocess.PIPE,
                cwd=os.path.expanduser("~"),
                env=os.environ.copy(),
                shell=True
            )
            raw_output: str = f"\n\n---------{self.NAME}"

            while process.poll() is None:
                line: str = str(process.stdout.readline().strip(), "utf-8", errors="ignore")
                raw_output += f"\n{line}"
                if not line:
                    continue
                package_data: list[str] = self._parseComposerShowLine(line, True)
                if not package_data:
                    continue
                packages.append(
                    Package(
                        package_data[0], package_data[1], package_data[2], self.NAME, Composer
                    )
                )

            globals.PackageManagerOutput += raw_output
            print(f"🟢 {self.NAME} search for dynamic packages finished with {len(packages)} result(s)")
            return packages

        except Exception as exception:
            report(exception)
            return []

    def getAvailableUpdates(self) -> list[UpgradablePackage]:
        print(f"🔵 Starting {self.NAME} search for updates.")
        try:
            packages: list[UpgradablePackage] = []
            process: subprocess.Popen = subprocess.Popen(
                f"{self.EXECUTABLE} show {self.EXECUTABLE_COMMON_ARGS} --outdated",
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                stdin=subprocess.PIPE,
                cwd=os.path.expanduser("~"),
                env=os.environ.copy(),
                shell=True
            )
            raw_output: str = f"\n\n---------{self.NAME}"

            while process.poll() is None:
                line: str = str(process.stdout.readline().strip(), "utf-8", errors="ignore")
                raw_output += f"\n{line}"
                if not line:
                    continue
                package_data: list[str] = self._parseComposerShowLine(line, True)
                if not package_data:
                    continue
                packages.append(
                    UpgradablePackage(
                        package_data[0], package_data[1], package_data[2], package_data[3], self.NAME, Composer
                    )
                )

            globals.PackageManagerOutput += raw_output
            print(f"🟢 {self.NAME} search for updates finished with {len(packages)} result(s)")
            return packages

        except Exception as exception:
            report(exception)
            return []

    def getInstalledPackages(self) -> list[Package]:
        print(f"🔵 Starting {self.NAME} search for installed packages.")
        try:
            packages: list[Package] = []
            process: subprocess.Popen = subprocess.Popen(
                f"{self.EXECUTABLE} show {self.EXECUTABLE_COMMON_ARGS}",
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                stdin=subprocess.PIPE,
                cwd=os.path.expanduser("~"),
                env=os.environ.copy(),
                shell=True
            )
            raw_output: str = f"\n\n---------{self.NAME}"

            while process.poll() is None:
                line = str(process.stdout.readline().strip(), "utf-8", errors="ignore")
                raw_output += f"\n{line}"
                if not line:
                    continue
                package_data: list[str] = self._parseComposerShowLine(line)
                if not package_data:
                    continue
                packages.append(
                    Package(package_data[0], package_data[1], package_data[2], self.NAME, Composer)
                )

            globals.PackageManagerOutput += raw_output
            print(f"🟢 {self.NAME} search for installed packages finished with {len(packages)} result(s)")
            return packages

        except Exception as exception:
            report(exception)
            return []


Composer: ComposerPackageManager = ComposerPackageManager()
