import os

from PySide6 import QtCore, QtGui, QtWidgets
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
import Tabs, os, ctypes, sys, Tools

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

class MainWindow(QtWidgets.QMainWindow):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setWindowTitle("WingetUI: A Graphical User interface to manage Winget and Scoop packages")
        self.setMinimumSize(700, 560)
        self.setObjectName("micawin")
        self.setWindowIcon(QtGui.QIcon(realpath+"/icon.png"))
        self.resize(QtCore.QSize(1100, 700))
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
            Tools.MessageBox.warning(self, "Admin rights", "It looks like you have ran this software with admin rights. We do not recommend doing this. Proceed with caution")
        print("[   OK   ] Main application loaded...")

    def loadWidgets(self) -> None:
        self.mainWidget = QtWidgets.QStackedWidget()
        self.buttonBox = QButtonGroup()
        self.buttonLayout = QHBoxLayout()
        self.buttonLayout.setContentsMargins(10, 10, 10, 10)
        self.buttonLayout.setSpacing(5)
        self.installerswidget = QVBoxLayout()
        self.installerswidget.setContentsMargins(0, 0, 0, 0)
        self.installerswidget.setSpacing(5)
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        self.mainWidget.setStyleSheet("""
        QTabWidget::tab-bar {{
            alignment: center;
            }}""")
        self.discover = Tabs.Discover(self.installerswidget)
        self.discover.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.discover, "Discover Software")
        self.updates = Tabs.Upgrade(self.installerswidget)
        self.updates.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.updates, "Software updates")
        self.uninstall = Tabs.Uninstall(self.installerswidget)
        self.uninstall.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.uninstall, "Installed applications")
        self.addTab(Tabs.About(), "About WingetUI")
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        vl = QVBoxLayout()
        vl.addLayout(self.buttonLayout, stretch=0)
        vl.addWidget(self.mainWidget, stretch=1)
        #self.installersScrollArea = QtWidgets.QScrollArea()
        #self.installersScrollArea.setWidgetResizable(True)
        #self.installersScrollArea.setMaximumHeight(150)
        #self.installersScrollArea.hide()
        widget = QtWidgets.QWidget()
        widget.setContentsMargins(0, 0, 0, 0)
        widget.setMinimumHeight(0)
        widget.setLayout(self.installerswidget)
        #self.installersScrollArea.setWidget(widget)
        self.showHideButton = QPushButton()
        self.showHideButton.setStyleSheet("padding: 2px;")
        self.showHideButton.setIconSize(QSize(8, 8))
        self.showHideButton.setFixedSize(QSize(16, 8))
        self.showHideButton.setIcon(QIcon(Tools.getMedia("collapse")))
        self.showHideButton.clicked.connect(lambda: (widget.setVisible(not widget.isVisible()), self.showHideButton.setIcon(QIcon(Tools.getMedia("collapse"))) if widget.isVisible() else self.showHideButton.setIcon(QIcon(Tools.getMedia("expand")))))
        ebw = QWidget()
        ebw.setLayout(QHBoxLayout())
        ebw.layout().setContentsMargins(0, 0, 0, 0)
        ebw.layout().addStretch()
        ebw.layout().addWidget(self.showHideButton)
        ebw.layout().addStretch()
        vl.addWidget(ebw)
        vl.addWidget(widget)
        vl.setSpacing(0)
        vl.setContentsMargins(0, 20, 0, 0)
        w = QWidget()
        w.setContentsMargins(0, 0, 0, 0)
        self.setContentsMargins(0, 0, 0, 0)
        w.setLayout(vl)
        self.setCentralWidget(w)
        self.show()
        self.uninstall.resizeEvent()
        self.discover.resizeEvent()
        self.updates.resizeEvent()

        sct = QShortcut(QKeySequence("Ctrl+Tab"), self)
        sct.activated.connect(lambda: self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() + 1) if self.mainWidget.currentIndex() < 3 else 0))

        sct = QShortcut(QKeySequence("Ctrl+Shift+Tab"), self)
        sct.activated.connect(lambda: self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() - 1) if self.mainWidget.currentIndex() > 0 else 3))

    def addTab(self, widget: QWidget, label: str) -> None:
        i = self.mainWidget.addWidget(widget)
        btn = QPushButton(label)
        btn.setCheckable(True)
        btn.clicked.connect(lambda: self.mainWidget.setCurrentIndex(i))
        self.buttonBox.addButton(btn)
        self.buttonLayout.addWidget(btn)

    def isAdmin(self) -> bool:
        try:
            is_admin = (os.getuid() == 0)
        except AttributeError:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        return is_admin
    
    def closeEvent(self, event):
        if(Tools.pending_programs != []):
            if(Tools.MessageBox.question(self, "Warning", "There is an installation in progress. If you close WingetUI, the installation may fail and have unexpected results. Do you still want to close the application?", Tools.MessageBox.No | Tools.MessageBox.Yes, Tools.MessageBox.No) == Tools.MessageBox.Yes):
                event.accept()
            else:
                event.ignore()
        else:
            event.accept()


if(__name__=="__main__"):
    import __init__
