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

pending_programs: list = []
current_program: str = ""

updatesHeader: QAction = None
installedHeader: QAction = None

lightCSS: str = ""
darkCSS: str = ""