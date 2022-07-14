import os
from xml.dom.minidom import Attr

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
    def __init__(self, componentStatus: dict, updatesMenu: QMenu, installedMenu: QMenu):
        self.oldbtn = None
        super().__init__()
        self.updatesMenu = updatesMenu
        self.installedMenu = installedMenu
        self.componentStatus = componentStatus
        self.setWindowTitle("WingetUI: A Graphical User interface to manage Winget and Scoop packages")
        self.setMinimumSize(700, 560)
        self.setObjectName("micawin")
        self.setWindowIcon(QtGui.QIcon(realpath+"/icon.png"))
        self.resize(QtCore.QSize(1100, 700))
        self.loadWidgets()
        self.blackmatt = QWidget(self)
        self.blackmatt.setStyleSheet("background-color: rgba(0, 0, 0, 50%);")
        self.blackmatt.hide()
        self.blackmatt.move(0, 0)
        self.blackmatt.resize(self.size())
        self.installEventFilter(self)
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
        self.buttonLayout.setContentsMargins(2, 6, 4, 6)
        self.buttonLayout.setSpacing(5)
        self.buttonier = QWidget()
        self.buttonier.setFixedHeight(54)
        self.buttonier.setFixedWidth(703)
        self.buttonier.setObjectName("buttonier")
        self.buttonier.setLayout(self.buttonLayout)
        self.installationsWidget = Tools.DynamicScrollArea()
        self.installerswidget: QLayout = self.installationsWidget.vlayout
        Tools.installersWidget = self.installationsWidget
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        self.mainWidget.setStyleSheet("""
        QTabWidget::tab-bar {{
            alignment: center;
            }}""")
        self.discover = Tabs.Discover(self.installerswidget)
        self.discover.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.discover, "Discover Software")
        self.updates = Tabs.Upgrade(self.installerswidget, self.updatesMenu)
        self.updates.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.updates, "Software updates")
        self.uninstall = Tabs.Uninstall(self.installerswidget, self.installedMenu)
        self.uninstall.setStyleSheet("QGroupBox{border-radius: 5px;}")
        self.addTab(self.uninstall, "Installed applications")
        self.addTab(Tabs.About(self.componentStatus), "About WingetUI")
        class Text(QPlainTextEdit):
            def __init__(self):
                super().__init__()
                self.setPlainText("click to show log")

            def mousePressEvent(self, e: QMouseEvent) -> None:
                self.setPlainText(Tools.buffer.getvalue())
                self.appendPlainText(Tools.errbuffer.getvalue())
                return super().mousePressEvent(e)
        p = Text()
        p.setReadOnly(True)
        #self.addTab(p, "Debugging log")
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        vl = QVBoxLayout()
        hl = QHBoxLayout()
        hl.addStretch()
        hl.addWidget(self.buttonier)
        hl.addStretch()
        hl.setContentsMargins(0, 0, 0, 0)
        vl.addLayout(hl)
        vl.addWidget(self.mainWidget, stretch=1)
        self.buttonBox.buttons()[0].setChecked(True)
        self.showHideButton = QPushButton()
        self.showHideButton.setStyleSheet("padding: 2px;border-radius: 4px;")
        self.showHideButton.setIconSize(QSize(12, 12))
        self.showHideButton.setFixedSize(QSize(32, 16))
        self.showHideButton.setIcon(QIcon(Tools.getMedia("collapse")))
        self.showHideButton.clicked.connect(lambda: (self.installationsWidget.setVisible(not self.installationsWidget.isVisible()), self.showHideButton.setIcon(QIcon(Tools.getMedia("collapse"))) if self.installationsWidget.isVisible() else self.showHideButton.setIcon(QIcon(Tools.getMedia("expand")))))
        ebw = QWidget()
        ebw.setLayout(QHBoxLayout())
        ebw.layout().setContentsMargins(0, 0, 0, 0)
        ebw.layout().addStretch()
        ebw.layout().addWidget(self.showHideButton)
        ebw.layout().addStretch()
        vl.addWidget(ebw)
        vl.addWidget(self.installationsWidget)
        vl.setSpacing(0)
        vl.setContentsMargins(0, 0, 0, 0)
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
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() + 1) if self.mainWidget.currentIndex() < 3 else 0), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

        sct = QShortcut(QKeySequence("Ctrl+Shift+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() - 1) if self.mainWidget.currentIndex() > 0 else 3), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

    def addTab(self, widget: QWidget, label: str) -> None:
        i = self.mainWidget.addWidget(widget)
        btn = QPushButton(label)
        btn.setCheckable(True)
        btn.setFixedHeight(40)
        btn.setObjectName("Headerbutton")
        btn.setFixedWidth(170)
        btn.clicked.connect(lambda: self.mainWidget.setCurrentIndex(i))
        if self.oldbtn:
            self.oldbtn.setStyleSheet("" + self.oldbtn.styleSheet())
            btn.setStyleSheet("" + btn.styleSheet())
        self.oldbtn = btn
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
                self.hide()
                event.ignore()
            else:
                event.ignore()
        else:
            event.ignore()
            self.hide()

    def resizeEvent(self, event: QResizeEvent) -> None:
        try:
            self.blackmatt.move(0, 0)
            self.blackmatt.resize(self.size())
        except AttributeError:
            pass
        return super().resizeEvent(event)

    def showWindow(self):
        if not self.window().isMaximized():
            self.window().hide()
            self.window().showMinimized()
            self.window().show()
            self.window().showNormal()
        else:
            self.window().hide()
            self.window().showMinimized()
            self.window().show()
            self.window().showMaximized()
        self.window().setFocus()
        self.window().raise_()
        self.window().activateWindow()



class DraggableWindow(QWidget):
    pressed = False
    oldPos = QPoint(0, 0)
    def __init__(self, parent = None) -> None:
        super().__init__(parent)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.pressed = True
        self.oldPos = event.pos()
        return super().mousePressEvent(event)

    def mouseMoveEvent(self, event: QtGui.QMouseEvent) -> None:
        if self.pressed:
            self.move(self.pos()+(event.pos()-self.oldPos))
        return super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event: QtGui.QMouseEvent) -> None:
        self.pressed = False
        self.oldPos = event.pos()
        return super().mouseReleaseEvent(event)

if(__name__=="__main__"):
    import __init__
