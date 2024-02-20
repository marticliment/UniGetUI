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


class ChocoPackageManager(PackageManagerWithSources):

    if getSettings("UseSystemChocolatey"):
        print("🟡 System chocolatey used")
        EXECUTABLE = "choco.exe"
    else:
        possiblePath = os.path.join(os.path.expanduser("~"), "AppData/Local/Programs/WingetUI/choco-cli/choco.exe")
        if os.path.isfile(possiblePath):
            print("🔵 Found default chocolatey installation on expected location")
            EXECUTABLE = possiblePath.replace("/", "\\")
        else:
            print("🟡 Chocolatey was not found on the default location, perhaps a portable WingetUI installation?")
            EXECUTABLE = os.path.join(os.path.join(realpath, "choco-cli"), "choco.exe").replace("/", "\\")
        os.environ["chocolateyinstall"] = os.path.dirname(EXECUTABLE)

    def detectManager(self, signal: 'Signal' = None) -> None:
        o = subprocess.run(f"{self.EXECUTABLE} -v", shell=True, stdout=subprocess.PIPE)
        Globals.componentStatus[f"{self.NAME}Found"] = shutil.which(self.EXECUTABLE) is not None
        Globals.componentStatus[f"{self.NAME}Version"] = o.stdout.decode('utf-8').replace("\n", " ").replace("\r", " ")

        if getSettings("ShownWelcomeWizard") and not getSettings("UseSystemChocolatey") and not getSettings("ChocolateyAddedToPath") and not os.path.isfile(r"C:\ProgramData\Chocolatey\bin\choco.exe"):
            # If the user is running bundled chocolatey and chocolatey is not in path, add chocolatey to path
            subprocess.run("powershell -NoProfile -Command [Environment]::SetEnvironmentVariable(\\\"PATH\\\", \\\"" + self.EXECUTABLE.replace('\\choco.exe', '\\bin') + ";\\\"+[Environment]::GetEnvironmentVariable(\\\"PATH\\\", \\\"User\\\"), \\\"User\\\")", shell=True, check=False)
            subprocess.run(f"powershell -NoProfile -Command [Environment]::SetEnvironmentVariable(\\\"chocolateyinstall\\\", \\\"{os.path.dirname(self.EXECUTABLE)}\\\", \\\"User\\\")", shell=True, check=False)
            print("🔵 Adding chocolatey to path...")
            setSettings("ChocolateyAddedToPath", True)

        if signal:
            signal.emit()

    def updateSources(self, signal: 'Signal' = None) -> None:
        pass  # Handled by the package manager, no need to manually reload
        if signal:
            signal.emit()


Choco = ChocoPackageManager()
