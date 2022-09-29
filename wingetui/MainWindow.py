from turtle import isvisible
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
import os, ctypes, sys, tools, win32mica
import win32gui


import globals

from storeEngine import *
from tools import *

class RootWindow(QMainWindow):
    callInMain = Signal(object)
    pressed = False
    oldPos = QPoint(0, 0)

    def __init__(self):
        self.oldbtn = None
        super().__init__()
        self.callInMain.connect(lambda f: f())
        self.setWindowTitle("WingetUI: A Graphical User interface to manage Winget and Scoop packages")
        self.setMinimumSize(700, 560)
        self.setObjectName("micawin")
        self.setWindowIcon(QIcon(realpath+"/resources/icon.png"))
        self.resize(QSize(1100, 700))
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

        
        print("[   OK   ] Main application loaded...")

    def loadWidgets(self) -> None:
        self.widgets = {}
        self.mainWidget = QStackedWidget()
        self.buttonBox = QButtonGroup()
        self.buttonLayout = QHBoxLayout()
        self.buttonLayout.setContentsMargins(2, 6, 4, 6)
        self.buttonLayout.setSpacing(5)
        self.buttonier = QWidget()
        self.buttonier.setFixedHeight(54)
        self.buttonier.setFixedWidth(703)
        self.buttonier.setObjectName("buttonier")
        self.buttonier.setLayout(self.buttonLayout)
        self.showHideButton = QPushButton()
        self.installationsWidget = DynamicScrollArea(self.showHideButton)
        self.installerswidget: QLayout = self.installationsWidget.vlayout
        globals.installersWidget = self.installationsWidget
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        self.mainWidget.setStyleSheet("""
        QTabWidget::tab-bar {{
            alignment: center;
            }}""")
        self.discover = DiscoverSoftwareSection()
        self.discover.setStyleSheet("QGroupBox{border-radius: 5px;}")
        globals.discover = self.discover
        self.widgets[self.discover] = self.addTab(self.discover, "Discover Software")
        self.updates = UpdateSoftwareSection()
        self.updates.setStyleSheet("QGroupBox{border-radius: 5px;}")
        globals.updates = self.updates
        self.widgets[self.updates] = self.addTab(self.updates, "Software updates")
        self.uninstall = UninstallSoftwareSection()
        self.uninstall.setStyleSheet("QGroupBox{border-radius: 5px;}")
        globals.uninstall = self.uninstall
        self.widgets[self.uninstall] = self.addTab(self.uninstall, "Installed applications")
        self.aboutSection = AboutSection()
        self.widgets[self.aboutSection] = self.addTab(self.aboutSection, "About WingetUI")
        class Text(QPlainTextEdit):
            def __init__(self):
                super().__init__()
                self.setPlainText("click to show log")

            def mousePressEvent(self, e: QMouseEvent) -> None:
                self.setPlainText(buffer.getvalue())
                self.appendPlainText(errbuffer.getvalue())
                return super().mousePressEvent(e)
        p = Text()
        p.setReadOnly(True)
        #self.addTab(p, "Debugging log")
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        vl = QVBoxLayout()
        hl = QHBoxLayout()
        self.adminButton = QPushButton("")
        self.adminButton.setIcon(QIcon(getMedia("runasadmin")))
        self.adminButton.clicked.connect(lambda: (self.warnAboutAdmin(), self.adminButton.setChecked(True)))
        self.adminButton.setFixedWidth(40)
        self.adminButton.setFixedHeight(40)
        self.adminButton.setCheckable(True)
        self.adminButton.setChecked(True)
        self.adminButton.setObjectName("Headerbutton")
        if self.isAdmin():
            hl.addSpacing(4)
            hl.addWidget(self.adminButton)
        hl.addStretch()
        hl.addWidget(self.buttonier)
        hl.addStretch()
        if self.isAdmin():
            hl.addSpacing(40)
        hl.setContentsMargins(0, 0, 0, 0)
        vl.addLayout(hl)
        vl.addWidget(self.mainWidget, stretch=1)
        self.buttonBox.buttons()[0].setChecked(True)
        self.showHideButton.setStyleSheet("padding: 2px;border-radius: 4px;")
        self.showHideButton.setIconSize(QSize(12, 12))
        self.showHideButton.hide()
        self.showHideButton.setFixedSize(QSize(32, 16))
        self.showHideButton.setIcon(QIcon(getMedia("collapse")))
        self.showHideButton.clicked.connect(lambda: (self.installationsWidget.setVisible(not self.installationsWidget.isVisible()), self.showHideButton.setIcon(QIcon(getMedia("collapse"))) if self.installationsWidget.isVisible() else self.showHideButton.setIcon(QIcon(getMedia("expand")))))
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
        self.uninstall.resizeEvent()
        self.discover.resizeEvent()
        self.updates.resizeEvent()
        sct = QShortcut(QKeySequence("Ctrl+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() + 1) if self.mainWidget.currentIndex() < 3 else 0), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

        sct = QShortcut(QKeySequence("Ctrl+Shift+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() - 1) if self.mainWidget.currentIndex() > 0 else 3), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

    def addTab(self, widget: QWidget, label: str) -> QPushButton:
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
        return btn

    def warnAboutAdmin(self):
            self.err = ErrorMessage(self)
            errorData = {
                "titlebarTitle": f"WingetUI",
                "mainTitle": f"Administrator privileges",
                "mainText": f"It looks like you ran WingetUI as administrator, which is not recommended. You can still use the program, but we hightly recommend not running WingetUI with administrator privileges. Click on \"Show details\" to see why.",
                "buttonTitle": "Ok",
                "errorDetails": "There are two main reasons to not run WingetUI as administrator:\n The first one is that the scoop package manager might cause problems with some commands when ran with administrator rights.\n The second one is that running WingetUI as administrator means that any package that you download will be ran as administrator (and this is not safe).\n Remeber that if you need to install a specific package as administrator, you can always right-click tyhe item -> Install/Update/Uninstall as administrator.",
                "icon": QIcon(getMedia("infocolor")),
            }
            self.err.showErrorMessage(errorData, showNotification=False)

    def isAdmin(self) -> bool:
        try:
            is_admin = (os.getuid() == 0)
        except AttributeError:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        return is_admin
    
    def closeEvent(self, event):
        if(globals.pending_programs != []):
            if updatesAvailable or getSettings("DisablesystemTray"):
                if(tools.MessageBox.question(self, "Warning", "There is an installation in progress. If you close WingetUI, the installation may fail and have unexpected results. Do you still want to close the application?", tools.MessageBox.No | tools.MessageBox.Yes, tools.MessageBox.No) == tools.MessageBox.Yes):
                    event.accept()
                    globals.app.quit()
                    sys.exit(0)
                else:
                    event.ignore()
            else:
                self.hide()
                event.ignore()
        else:
            if updatesAvailable or getSettings("DisablesystemTray"):
                event.accept()
                globals.app.quit()
                sys.exit(0)
            else:
                self.hide()
                event.ignore()

    def resizeEvent(self, event: QResizeEvent) -> None:
        try:
            self.blackmatt.move(0, 0)
            self.blackmatt.resize(self.size())
        except AttributeError:
            pass
        return super().resizeEvent(event)

    def showWindow(self):
        if globals.lastFocusedWindow != self.winId():
            if not self.window().isMaximized():
                self.window().show()
                self.window().showNormal()
            else:
                self.window().show()
                self.window().showMaximized()
            self.window().setFocus()
            self.window().raise_()
            self.window().activateWindow()
            try:
                if self.updates.countLabel.text() != "Found packages: 0":
                    self.widgets[self.updates].click()
            except Exception as e:
                report(e)
            globals.lastFocusedWindow = self.winId()
        else:
            self.hide()

    def showEvent(self, event: QShowEvent) -> None:
        if(not isDark()):
            r = win32mica.ApplyMica(self.winId(), win32mica.MICAMODE.LIGHT)
            print(r)
            if r != 0x32:
                pass
            self.setStyleSheet(globals.lightCSS.replace("mainbg", "transparent" if r == 0x0 else "#ffffff")) 
        else:
            r = win32mica.ApplyMica(self.winId(), win32mica.MICAMODE.DARK)
            if r != 0x32:
                pass
            self.setStyleSheet(globals.darkCSS.replace("mainbg", "transparent" if r == 0x0 else "#202020"))
        return super().showEvent(event)

class DraggableWindow(QWidget):
    pressed = False
    oldPos = QPoint(0, 0)
    def __init__(self, parent = None) -> None:
        super().__init__(parent)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.pressed = True
        self.oldPos = event.pos()
        return super().mousePressEvent(event)

    def mouseMoveEvent(self, event: QMouseEvent) -> None:
        if self.pressed:
            self.move(self.pos()+(event.pos()-self.oldPos))
        return super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        self.pressed = False
        self.oldPos = event.pos()
        return super().mouseReleaseEvent(event)

if(__name__=="__main__"):
    import __init__
