from PySide2 import QtCore, QtGui, QtWidgets
import Tabs, os, ctypes, sys, Tools

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

class MainWindow(QtWidgets.QMainWindow):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setWindowTitle("WingetUI Store: A GUI Store for Winget, Scoop and AppGet packages")
        self.setMinimumSize(700, 560)
        self.setWindowIcon(QtGui.QIcon(realpath+"/icon.png"))
        self.resize(QtCore.QSize(1024, 600))
        self.loadWidgets()
        self.installEventFilter(self)
        self.show()
        self.setStyleSheet("""
            QTreeWidget::item{{
                height: 25px;
                padding: 5px;
                padding-left: 10px;
            }}
            QGroupBox:title{{ max-width: 0; max-height: 0; }}
        """)

        if(self.isAdmin()):
            QtWidgets.QMessageBox.warning(self, "Admin rights", "It looks like you have ran this software with admin rights. We do not recommend doing this. Proceed with caution")
        print("[   OK   ] Main application loaded...")

    def loadWidgets(self) -> None:
        self.discover = Tabs.Discover()
        self.discover.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.mainWidget = QtWidgets.QTabWidget()
        self.mainWidget.addTab(self.discover, "Discover Software")
        self.mainWidget.addTab(Tabs.About(), "About WingetUI")
        #self.mainWidget.addTab(Tabs.Installed(), "Installed applications")

        self.setCentralWidget(self.mainWidget)
        self.show()
        self.discover.resizeEvent()

    def isAdmin(self) -> bool:
        try:
            is_admin = (os.getuid() == 0)
        except AttributeError:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        return is_admin
    
    def closeEvent(self, event):
        if(Tools.pending_programs != []):
            if(QtWidgets.QMessageBox.question(self, "Warning", "There is an installation in progress. If you close WingetUI Store, the installation may fail and have unexpected results. Do you still want to close the application?", QtWidgets.QMessageBox.No | QtWidgets.QMessageBox.Yes, QtWidgets.QMessageBox.No) == QtWidgets.QMessageBox.Yes):
                event.accept()
            else:
                event.ignore()
        else:
            event.accept()


if(__name__=="__main__"):
    import __init__