from PySide2 import QtCore, QtGui, QtWidgets
import Tabs, os, ctypes

class MainWindow(QtWidgets.QMainWindow):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setWindowTitle("Winget UI Store")
        self.setMinimumSize(700, 560)
        self.setWindowIcon(QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/icon.png"))
        self.resize(QtCore.QSize(1024, 600))
        self.loadWidgets()
        self.show()
        self.setStyleSheet("""
            QTreeWidget::item{{
                height: 25px;
                padding: 5px;
                padding-left: 10px;
            }}
        """)

        if(self.isAdmin()):
            QtWidgets.QMessageBox.warning(self, "Admin rights", "It looks like you have ran this software with admin rights. We do not recommend doing this. Proceed with caution")
        print("[   OK   ] Main application loaded...")

    def loadWidgets(self) -> None:
        self.mainWidget = Tabs.Discover()#QtWidgets.QTabWidget()
        #self.mainWidget.addTab(Tabs.Discover(), "Discover")
        #self.mainWidget.addTab(Tabs.Update(), "Updates")
        #self.mainWidget.addTab(Tabs.Installed(), "Installed applications")

        self.setCentralWidget(self.mainWidget)
        self.show()
        self.mainWidget.resizeEvent()

    def isAdmin(self) -> bool:
        try:
            is_admin = (os.getuid() == 0)
        except AttributeError:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        return is_admin


if(__name__=="__main__"):
    import __init__