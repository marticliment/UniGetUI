from xml.dom.minidom import Attr
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
import os, ctypes, sys, tools, win32mica
import win32gui


import globals
from tools import _

from uiSections import *
from storeEngine import *
from tools import *

class RootWindow(QMainWindow):
    callInMain = Signal(object)
    pressed = False
    oldPos = QPoint(0, 0)
    isWinDark = False
    appliedStyleSheet = False
    closedpos: QPoint = QPoint(-1, -1)

    def __init__(self):
        self.oldbtn = None
        super().__init__()
        self.isWinDark = isDark()
        self.callInMain.connect(lambda f: f())
        self.setWindowTitle("WingetUI")
        self.setMinimumSize(700, 560)
        self.setObjectName("micawin")
        self.setWindowIcon(QIcon(realpath+"/resources/icon.png"))
        self.resize(QSize(1100, 700))
        try:
            rs = getSettingsValue("OldWindowGeometry").split(",")
            assert (len(rs)==4), "Invalid window geometry format"
            geometry = QRect(int(rs[0]), int(rs[1]), int(rs[2]), int(rs[3]))
            if QApplication.primaryScreen().availableVirtualGeometry().contains(geometry):
                self.setGeometry(geometry)
        except Exception as e:
            report(e)
        self.loadWidgets()
        self.blackmatt = QWidget(self)
        self.blackmatt.setStyleSheet("background-color: rgba(0, 0, 0, 30%);border-top-left-radius: 8px;border-top-right-radius: 8px;")
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

        
        print("🟢 Main application loaded...")

    def loadWidgets(self) -> None:

        self.infobox = PackageInfoPopupWindow(self)
        globals.infobox = self.infobox

        self.widgets = {}
        self.mainWidget = QStackedWidget()
        self.extrasMenu = QMenu("", self)
        self.buttonBox = QButtonGroup()
        self.buttonLayout = QHBoxLayout()
        self.buttonLayout.setContentsMargins(2, 6, 4, 6)
        self.buttonLayout.setSpacing(5)
        self.buttonier = QWidget()
        self.buttonier.setFixedHeight(54)
        self.buttonier.setFixedWidth(540)
        self.buttonier.setObjectName("buttonier")
        self.buttonier.setLayout(self.buttonLayout)
        self.extrasMenuButton = QPushButton()
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
        self.widgets[self.discover] = self.addTab(self.discover, _("Discover Packages"))
        self.updates = UpdateSoftwareSection()
        self.updates.setStyleSheet("QGroupBox{border-radius: 5px;}")
        globals.updates = self.updates
        self.widgets[self.updates] = self.addTab(self.updates, _("Software Updates"))
        self.uninstall = UninstallSoftwareSection()
        self.uninstall.setStyleSheet("QGroupBox{border-radius: 5px;}")
        globals.uninstall = self.uninstall
        self.widgets[self.uninstall] = self.addTab(self.uninstall, _("Installed Packages"))
        self.settingsSection = SettingsSection()
        self.widgets[self.settingsSection] = self.addTab(self.settingsSection, _("WingetUI Settings"), addToMenu=True, actionIcon="settings")
        self.aboutSection = AboutSection()
        self.widgets[self.aboutSection] = self.addTab(self.aboutSection, _("About WingetUI"), addToMenu=True, actionIcon="info")
        self.logSection = DebuggingSection()
        self.widgets[self.logSection] = self.addTab(self.logSection, _("WingetUI log"), addToMenu=True, actionIcon="buggy")

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
            hl.addSpacing(8)
            hl.addWidget(self.adminButton)
        else:
            hl.addSpacing(48)
        hl.addStretch()
        hl.addWidget(self.buttonier)
        hl.addStretch()

        def showExtrasMenu():
            ApplyMenuBlur(self.extrasMenu.winId(), self.extrasMenu)
            self.extrasMenu.exec(QCursor.pos())

        self.extrasMenuButton.setIcon(QIcon(getMedia("hamburger")))
        self.extrasMenuButton.clicked.connect(lambda: showExtrasMenu())
        self.extrasMenuButton.setFixedWidth(40)
        self.extrasMenuButton.setIconSize(QSize(24, 24))
        self.extrasMenuButton.setCheckable(True)
        self.extrasMenuButton.setFixedHeight(40)
        self.extrasMenuButton.setObjectName("Headerbutton")
        self.buttonBox.addButton(self.extrasMenuButton)
        globals.extrasMenuButton = self.extrasMenuButton
        hl.addWidget(self.extrasMenuButton)
        hl.addSpacing(8)
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
        globals.centralWindowLayout = w
        sct = QShortcut(QKeySequence("Ctrl+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() + 1) if self.mainWidget.currentIndex() < 4 else 0), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

        sct = QShortcut(QKeySequence("Ctrl+Shift+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() - 1) if self.mainWidget.currentIndex() > 0 else 3), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))



    def addTab(self, widget: QWidget, label: str, addToMenu: bool = False, actionIcon: str = "") -> QPushButton:
        i = self.mainWidget.addWidget(widget)
        btn = PushButtonWithAction(label)
        btn.setCheckable(True)
        btn.setFixedHeight(40)
        btn.setObjectName("Headerbutton")
        btn.setFixedWidth(170)
        btn.clicked.connect(lambda: self.mainWidget.setCurrentIndex(i))
        if self.oldbtn:
            self.oldbtn.setStyleSheet("" + self.oldbtn.styleSheet())
            btn.setStyleSheet("" + btn.styleSheet())
        self.oldbtn = btn
        if addToMenu:
            btn.action.setIcon(QIcon(getMedia(actionIcon)))
            btn.action.setParent(self.extrasMenu)
            btn.clicked.connect(self.extrasMenuButton.setChecked(True))
            self.extrasMenu.addAction(btn.action)
        else:
            self.buttonLayout.addWidget(btn)
            self.buttonBox.addButton(btn)
        return btn

    def warnAboutAdmin(self):
            from tools import _
            self.err = ErrorMessage(self)
            errorData = {
                "titlebarTitle": _("WingetUI"),
                "mainTitle": _("Administrator privileges"),
                "mainText": _("It looks like you ran WingetUI as administrator, which is not recommended. You can still use the program, but we highly recommend not running WingetUI with administrator privileges. Click on \"Show details\" to see why."),
                "buttonTitle": _("Ok"),
                "errorDetails": _("There are two main reasons to not run WingetUI as administrator:\n The first one is that the Scoop package manager might cause problems with some commands when ran with administrator rights.\n The second one is that running WingetUI as administrator means that any package that you download will be ran as administrator (and this is not safe).\n Remeber that if you need to install a specific package as administrator, you can always right-click the item -> Install/Update/Uninstall as administrator."),
                "icon": QIcon(getMedia("icon")),
            }
            self.err.showErrorMessage(errorData, showNotification=False)

    def isAdmin(self) -> bool:
        try:
            is_admin = (os.getuid() == 0)
        except AttributeError:
            is_admin = ctypes.windll.shell32.IsUserAnAdmin() != 0
        return is_admin

    def deleteChildren(self) -> None:
        try:
            self.discover.destroyAnims()
            self.updates.destroyAnims()
            self.uninstall.destroyAnims()
        except Exception as e:
            report(e)
    
    def closeEvent(self, event):
        self.closedpos = self.pos()
        setSettingsValue("OldWindowGeometry", f"{self.closedpos.x()},{self.closedpos.y()+30},{self.width()},{self.height()}")
        if(globals.themeChanged):
            globals.themeChanged = False
            self.deleteChildren()
            event.accept()
        if(globals.pending_programs != []):
            if getSettings("DisablesystemTray"):
                if(QMessageBox.question(self, _("Warning"), _("There is an installation in progress. If you close WingetUI, the installation may fail and have unexpected results. Do you still want to quit WingetUI?"), QMessageBox.No | QMessageBox.Yes, QMessageBox.No) == QMessageBox.Yes):
                    if globals.updatesAvailable:
                        self.hide()
                        globals.canUpdate = True
                        globals.trayIcon.showMessage(_("Updating WingetUI"), _("WingetUI is being updated. When finished, WingetUI will restart itself"), QIcon(getMedia("notif_info")))
                        event.ignore()
                    else:
                        self.deleteChildren()
                        event.accept()
                        globals.app.quit()
                        sys.exit(0)
                else:
                    event.ignore()
            else:
                if globals.updatesAvailable:
                    self.hide()
                    globals.canUpdate = True
                    globals.trayIcon.showMessage(_("Updating WingetUI"), _("WingetUI is being updated. When finished, WingetUI will restart itself"), QIcon(getMedia("notif_info")))
                    event.ignore()
                else:
                    self.hide()
                    globals.lastFocusedWindow = 0
                    event.ignore()
        else:
            if globals.updatesAvailable:
                self.hide()
                globals.canUpdate = True
                globals.trayIcon.showMessage(_("Updating WingetUI"), _("WingetUI is being updated. When finished, WingetUI will restart itself"), QIcon(getMedia("notif_info")))
                event.ignore()
            else:
                if getSettings("DisablesystemTray"):
                    self.deleteChildren()
                    event.accept()
                    globals.app.quit()
                    sys.exit(0)
                else:
                    self.hide()
                    globals.lastFocusedWindow = 0
                    event.ignore()

    def resizeEvent(self, event: QResizeEvent) -> None:
        try:
            self.blackmatt.move(0, 0)
            self.blackmatt.resize(self.size())
        except AttributeError:
            pass
        try:
            s = self.infobox.size()
            self.infobox.move((self.width()-s.width())//2, (self.height()-s.height())//2)
        except AttributeError:
            pass
        setSettingsValue("OldWindowGeometry", f"{self.x()},{self.y()+30},{self.width()},{self.height()}")
        return super().resizeEvent(event)

    def showWindow(self, index = -1):
        if globals.lastFocusedWindow != self.winId():
            if not self.window().isMaximized():
                self.window().show()
                self.window().showNormal()
                if self.closedpos != QPoint(-1, -1):
                    self.window().move(self.closedpos)
            else:
                self.window().show()
                self.window().showMaximized()
            self.window().setFocus()
            self.window().raise_()
            self.window().activateWindow()
            try:
                if self.updates.availableUpdates > 0:
                    self.widgets[self.updates].click()
            except Exception as e:
                report(e)
            globals.lastFocusedWindow = self.winId()
        else:
            self.hide()
            globals.lastFocusedWindow = 0
        if index>=0:
            try:
                match index:
                    case 0:
                        self.widgets[self.discover].click()
                    case 1:
                        self.widgets[self.updates].click()
                    case 2:
                        self.widgets[self.uninstall].click()
                    case 3:
                        self.widgets[self.settingsSection].click()
                    case 4:
                        self.widgets[self.aboutSection].click()
            except Exception as e:
                report(e)

    def showEvent(self, event: QShowEvent) -> None:
        if(not self.isWinDark):
            r = win32mica.ApplyMica(self.winId(), win32mica.MICAMODE.LIGHT)
            print(r)
            if not self.appliedStyleSheet and globals.lightCSS != "":
                self.appliedStyleSheet = True
                self.setStyleSheet(globals.lightCSS.replace("mainbg", "transparent" if r == 0x0 else "#ffffff")) 
        else:
            r = win32mica.ApplyMica(self.winId(), win32mica.MICAMODE.DARK)
            if not self.appliedStyleSheet and globals.darkCSS != "":
                self.appliedStyleSheet = True
                self.setStyleSheet(globals.darkCSS.replace("mainbg", "transparent" if r == 0x0 else "#202020"))
        return super().showEvent(event)

    def enterEvent(self, event: QEnterEvent) -> None:
        globals.lastFocusedWindow = self.winId()
        return super().enterEvent(event)

    def loseFocusUpdate(self):
        globals.lastFocusedWindow = 0
    
    def focusOutEvent(self, event: QEvent) -> None:
        Thread(target=lambda: (time.sleep(0.3), self.loseFocusUpdate())).start()
        return super().focusOutEvent(event)
    
    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        setSettingsValue("OldWindowGeometry", f"{self.x()},{self.y()+30},{self.width()},{self.height()}")
        return super().mouseReleaseEvent(event)

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
