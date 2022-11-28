from PySide6.QtCore import *
from PySide6.QtWidgets import *
from PySide6.QtGui import *

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
    "wingetFound": False,
    "scoopFound": False,
    "sudoFound": False,
    "wingetVersion": "Unknown",
    "scoopVersion": "Unknown", 
    "sudoVersion": "Unknown", 
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

lightCSS: str = ""
darkCSS: str = ""

discover: Section = None
updates: Section = None
uninstall: Section = None

lastFocusedWindow: int = 0
themeChanged: bool = False
updatesAvailable: bool = False
canUpdate: bool = False

packageMeta: dict = {}
infobox: QWidget = None
centralWindowLayout: QWidget = None

textfont: str = "Segoe UI Variable Text"
dispfont: str = "Segoe UI Variable Display"
dispfontsemib: str = "Segoe UI Variable Display Semib"