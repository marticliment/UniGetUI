if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import os
import socket
import sys
import time
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from threading import Thread

import wingetui.Core.Globals as Globals
import wingetui.Interface.BackendApi as BackendApi
from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.Interface.CustomWidgets.SpecificWidgets import *
from wingetui.Interface.CustomWidgets.InstallerWidgets import *
from wingetui.Interface.GenericSections import *
from wingetui.PackageEngine.Classes import PackageManagerModule


class DiscoverSoftwareSection(SoftwareSection):


    def loadShared(self, argument: str, second_round: bool = False):
        print(argument)
        if "#" in argument:
            id = argument.split("#")[0]
            store = argument.split("#")[1]
        else:
            id = argument
            store = "Unknown"
        packageFound = False
        for package in self.PackageItemReference.keys():
            package: Package
            if package.Id == id and (package.Source == store or store == "Unknown"):
                self.infobox: PackageInfoPopupWindow
                self.infobox.showPackageDetails(package)
                self.infobox.show()
                self.packageList.setEnabled(True)
                packageFound = True
                break
        if packageFound:
            pass
        elif not second_round:
            self.query.setText(id)
            self.finishFiltering(self.query.text())
            self.packageList.setEnabled(False)
            Thread(target=self.loadSharedId, args=(argument,), daemon=True).start()
        else:
            self.packageList.setEnabled(True)
            self.err = CustomMessageBox(self.window())
            errorData = {
                "titlebarTitle": _("Unable to find package"),
                "mainTitle": _("Unable to find package"),
                "mainText": _("We could not load detailed information about this package, because it was not found in any of your package sources"),
                "buttonTitle": _("Ok"),
                "errorDetails": _("This is probably due to the fact that the package you were sent was removed, or published on a package manager that you don't have enabled. The received ID is {0}").format(argument),
                "icon": QIcon(getMedia("notif_warn")),
            }
            self.err.showErrorMessage(errorData, showNotification=False)
