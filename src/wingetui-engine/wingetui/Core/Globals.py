if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)

    from PySide6.QtWidgets import QApplication, QSystemTrayIcon, QMenu, QMainWindow, QPushButton, QWidget, QLabel
    from PySide6.QtGui import QIcon, QAction
    from PySide6.QtCore import Qt

    from wingetui.Interface.CustomWidgets.GenericWidgets import DynamicScrollArea
    from wingetui.Interface.CustomWidgets.SpecificWidgets import SoftwareSection


import os
from secrets import token_hex
from subprocess import Popen


componentStatus: dict = {
    "sudoFound": False,
    "sudoVersion": False
}

app: 'QApplication' = None
installersWidget: 'DynamicScrollArea' = None
trayIcon: 'QSystemTrayIcon' = None
mainWindow: 'QMainWindow' = None
trayMenu: 'QMenu' = None
trayMenuInstalledList: 'QMenu' = None
trayMenuUpdatesList: 'QMenu' = None
extrasMenuButton: 'QPushButton' = None

pending_programs: list = []
current_program: str = ""

updatesHeader: 'QAction' = None
installedHeader: 'QAction' = None
updatesAction: 'QAction' = None

lightCSS: str = ""
darkCSS: str = ""

discover: 'SoftwareSection' = None
updates: 'SoftwareSection' = None
uninstall: 'SoftwareSection' = None

lastFocusedWindow: int = 0
themeChanged: bool = False
updatesAvailable: bool = False
canUpdate: bool = False
adminRightsGranted: bool = False

packageMeta: dict = {}
infobox: 'QWidget' = None
centralWindowLayout: 'QWidget' = None
centralTextureImage: 'QLabel' = None

scoopBuckets: dict[str:str] = {}
wingetSources: dict[str:str] = {}

shareProcessHandler: Popen = None

textfont: str = "Segoe UI Variable Text"
dispfont: str = "Segoe UI Variable Display"
dispfontsemib: str = "Segoe UI Variable Display Semib"

settingsCache = {}

ENABLE_WINGETUI_NOTIFICATIONS = True
ENABLE_SUCCESS_NOTIFICATIONS = True
ENABLE_ERROR_NOTIFICATIONS = True
ENABLE_UPDATES_NOTIFICATIONS = True

tray_is_installing: bool = False
tray_is_error: bool = False
tray_is_available_updates: bool = False
tray_is_needs_restart: bool = False

PackageManagerOutput: str = "Outputs from package managers on the current session:\n \n"

AUMID: str = ""

maskedImages: dict[str:'QIcon'] = {}

cachedIcons: dict[str:'QIcon'] = {}

CurrentSessionToken: str = token_hex(32)

DEFAULT_PACKAGE_BACKUP_DIR = ""

options = {}
CSharpApp = None