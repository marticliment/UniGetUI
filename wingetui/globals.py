from PySide6.QtCore import *
from PySide6.QtWidgets import *
from PySide6.QtGui import *

componentStatus: dict = {
    "wingetFound": False,
    "scoopFound": False,
    "sudoFound": False,
    "wingetVersion": "Unknown",
    "scoopVersion": "Unknown", 
    "sudoVersion": "Unknown", 
}
app: QApplication = None
installersWidget: QLayout = None
trayIcon: QSystemTrayIcon = None
mainWindow: QMainWindow = None
trayMenu: QMenu = None
trayMenuInstalledList: QMenu = None
trayMenuUpdatesList: QMenu = None

pending_programs: list = []
current_program: str = ""