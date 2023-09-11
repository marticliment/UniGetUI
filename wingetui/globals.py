from subprocess import Popen

from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *


class DynamicScrollAreaType(QWidget):
    def __init__(self, parent = None) -> None:
        super().__init__(parent)
    def rss(self):
        pass
    def removeItem(self, item: QWidget):
        pass
    def addItem(self, item: QWidget):
        pass

class Section(QWidget):
    def __init__(self, parent = None) -> None:
        super().__init__(parent, parent)

    def addTreeWidgetItem(item: QTreeWidgetItem):
       pass

componentStatus: dict = {
    "sudoFound": False,
    "sudoVersion": False
}

app: QApplication = None
installersWidget: DynamicScrollAreaType = None
trayIcon: QSystemTrayIcon = None
mainWindow: QMainWindow = None
trayMenu: QMenu = None
trayMenuInstalledList: QMenu = None
trayMenuUpdatesList: QMenu = None
extrasMenuButton: QPushButton = None

pending_programs: list = []
current_program: str = ""

updatesHeader: QAction = None
installedHeader: QAction = None
updatesAction: QAction = None

lightCSS: str = ""
darkCSS: str = ""

discover: Section = None
updates: Section = None
uninstall: Section = None

lastFocusedWindow: int = 0
themeChanged: bool = False
updatesAvailable: bool = False
canUpdate: bool = False
adminRightsGranted: bool = False

packageMeta: dict = {}
infobox: QWidget = None
centralWindowLayout: QWidget = None
centralTextureImage: QLabel = None

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

maskedImages: dict[str:QIcon] = {}