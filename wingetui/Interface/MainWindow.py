if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module 
    print("redirecting...")
    import subprocess, os, sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)

import ctypes
import os
import sys

import wingetui.Core.Globals as Globals
import win32mica
import winreg
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from wingetui.Interface.CustomWidgets.InstallerWidgets import *
from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.Interface.SoftwareSections import *

WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320
DWMWA_USE_IMMERSIVE_DARK_MODE = 20


class RootWindow(QMainWindow):
    callInMain = Signal(object)
    pressed = False
    oldPos = QPoint(0, 0)
    appliedStyleSheet = False
    closedpos: QPoint = QPoint(-1, -1)
    DynamicIconsToApply: dict[QWidget:str] = {}
    OnThemeChange = Signal()

    def __init__(self):
        self.oldbtn = None
        super().__init__()
        self.callInMain.connect(lambda f: f())
        self.setWindowTitle("WingetUI")
        self.setMinimumSize(700, 560)
        self.setObjectName("micawin")
        self.setWindowIcon(QIcon(getMedia("icon", autoIconMode=False)))
        self.resize(QSize(1200, 700))
        try:
            rs = getSettingsValue("OldWindowGeometry").split(",")
            assert (len(rs) == 4), "Invalid window geometry format"

            geometry = QRect(int(rs[0]), int(rs[1]), int(rs[2]), int(rs[3]))
            ShouldBeMaximized = getSettings("WindowWasMaximized")
            if QApplication.primaryScreen().availableVirtualGeometry().contains(geometry):
                self.move(geometry.x(), geometry.y())
                if not ShouldBeMaximized:
                    self.setGeometry(geometry)
            if ShouldBeMaximized:
                self.setWindowState(Qt.WindowState.WindowMaximized)

        except Exception as e:
            report(e)
        self.loadWidgets()
        self.blackmatt = QWidget(self)
        self.blackmatt.setStyleSheet("background-color: rgba(0, 0, 0, 30%);border-top-left-radius: 8px;border-top-right-radius: 8px;")
        self.blackmatt.hide()
        self.blackmatt.move(0, 0)
        self.blackmatt.resize(self.size())
        self.installEventFilter(self)
        win32mica.ApplyMica(1, win32mica.MicaTheme.AUTO, OnThemeChange=lambda _: self.callInMain.emit(lambda: self.ApplyStyleSheetsAndIcons(True)))  # Spawn a theme thread
        self.ApplyStyleSheetsAndIcons()
        print("🟢 Main application loaded...")

    def loadWidgets(self) -> None:

        Globals.centralTextureImage = QLabel(self)
        Globals.centralTextureImage.hide()

        self.infobox = PackageInfoPopupWindow(self)
        Globals.infobox = self.infobox

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
        self.resizewidget = VerticallyDraggableWidget()
        self.installationsWidget = DynamicScrollArea(self.resizewidget, EnableTopButton=False)
        self.installerswidget: QLayout = self.installationsWidget.vlayout
        Globals.installersWidget = self.installationsWidget
        self.buttonLayout.addWidget(QWidget(), stretch=1)
        self.mainWidget.setStyleSheet("""
        QTabWidget::tab-bar {{
            alignment: center;
            }}""")
        self.discover = DiscoverSoftwareSection(self)
        self.discover.setStyleSheet("QGroupBox{border-radius: 5px;}")
        Globals.discover = self.discover
        self.widgets[self.discover] = self.addTab(self.discover, _("Discover Packages"))
        self.updates = UpdateSoftwareSection(self)
        self.updates.setStyleSheet("QGroupBox{border-radius: 5px;}")
        Globals.updates = self.updates
        self.widgets[self.updates] = self.addTab(self.updates, _("Software Updates"))
        self.uninstall = UninstallSoftwareSection(self)
        self.uninstall.setStyleSheet("QGroupBox{border-radius: 5px;}")
        Globals.uninstall = self.uninstall
        self.widgets[self.uninstall] = self.addTab(self.uninstall, _("Installed Packages"))
        self.settingsSection = SettingsSection(self)
        self.widgets[self.settingsSection] = self.addTab(self.settingsSection, _("WingetUI Settings"), addToMenu=True, actionIcon="settings")
        self.aboutSection = AboutSection()
        self.widgets[self.aboutSection] = self.addTab(self.aboutSection, _("About WingetUI"), addToMenu=True, actionIcon="info")
        self.historySection = OperationHistorySection()
        self.widgets[self.historySection] = self.addTab(self.historySection, _("Operation history"), addToMenu=True, actionIcon="list")
        self.extrasMenu.addSeparator()
        self.helpSection = LogSection()
        self.widgets[self.helpSection] = self.addTab(self.helpSection, _("WingetUI log"), addToMenu=True, actionIcon="buggy")
        self.clilogSection = PackageManagerLogSection()
        self.widgets[self.clilogSection] = self.addTab(self.clilogSection, _("Package Manager logs"), addToMenu=True, actionIcon="console")
        self.helpSection = BaseBrowserSection()
        self.widgets[self.helpSection] = self.addTab(self.helpSection, _("Help and documentation"), addToMenu=True, actionIcon="help")

        self.buttonLayout.addWidget(QWidget(), stretch=1)
        vl = QVBoxLayout()
        hl = QHBoxLayout()
        self.adminButton = QPushButton("")
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

        self.extrasMenuButton.clicked.connect(lambda: showExtrasMenu())
        self.extrasMenuButton.setFixedWidth(40)
        self.extrasMenuButton.setIconSize(QSize(24, 24))
        self.extrasMenuButton.setCheckable(True)
        self.extrasMenuButton.setFixedHeight(40)
        self.extrasMenuButton.setObjectName("Headerbutton")

        def resetSelectionIndex():
            self.widgets[self.mainWidget.currentWidget()].setChecked(True)

        self.extrasMenu.aboutToHide.connect(resetSelectionIndex)
        self.buttonBox.addButton(self.extrasMenuButton)
        Globals.extrasMenuButton = self.extrasMenuButton
        hl.addWidget(self.extrasMenuButton)
        hl.addSpacing(8)
        hl.setContentsMargins(0, 0, 0, 0)
        vl.addLayout(hl)
        vl.addWidget(self.mainWidget, stretch=1)
        self.buttonBox.buttons()[0].setChecked(True)
        self.resizewidget.setObjectName("DraggableVerticalSection")
        self.resizewidget.setFixedHeight(9)
        self.resizewidget.setFixedWidth(300)
        self.resizewidget.hide()
        self.resizewidget.dragged.connect(self.adjustInstallationsSize)
        ebw = QWidget()
        ebw.setLayout(QHBoxLayout())
        ebw.layout().setContentsMargins(0, 0, 0, 0)
        ebw.layout().addStretch()
        ebw.layout().addWidget(self.resizewidget)
        ebw.layout().addStretch()
        vl.addWidget(ebw)
        vl.addWidget(self.installationsWidget)
        vl.setSpacing(0)
        vl.setContentsMargins(0, 0, 0, 0)
        w = QWidget(self)
        w.setContentsMargins(0, 0, 0, 0)
        self.setContentsMargins(0, 0, 0, 0)
        w.setLayout(vl)
        self.setCentralWidget(w)
        Globals.centralWindowLayout = w
        sct = QShortcut(QKeySequence("Ctrl+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() + 1) if self.mainWidget.currentIndex() < 4 else 0), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

        sct = QShortcut(QKeySequence("Ctrl+Shift+Tab"), self)
        sct.activated.connect(lambda: (self.mainWidget.setCurrentIndex((self.mainWidget.currentIndex() - 1) if self.mainWidget.currentIndex() > 0 else 3), self.buttonBox.buttons()[self.mainWidget.currentIndex()].setChecked(True)))

        self.themeTimer = QTimer()
        self.themeTimer.setSingleShot(True)
        self.themeTimer.setInterval(3000)
        self.themeTimer.timeout.connect(self.setBgTheme)

    def toggleInstallationsSection(self) -> None:
        if self.installationsWidget.isVisible():
            self.installationsWidget.setVisible(False)
            self.resizewidget.setVisible(False)
        else:
            self.installationsWidget.setVisible(True)
            self.resizewidget.setVisible(False)
            self.adjustInstallationsSize()

    def showHelpUrl(self, url: str):
        self.helpSection.changeHomeUrl(url)
        self.widgets[self.helpSection].click()

    def adjustInstallationsSize(self, offset: int = 0) -> None:
        if self.installationsWidget.maxHeight > self.installationsWidget.getFullHeight():
            self.installationsWidget.maxHeight = self.installationsWidget.getFullHeight()
        self.installationsWidget.maxHeight = self.installationsWidget.maxHeight + offset
        if self.installationsWidget.maxHeight < 4 and self.installationsWidget.itemCount > 0:
            self.installationsWidget.maxHeight = 4
        self.installationsWidget.calculateSize()

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
            self.DynamicIconsToApply[btn.action] = actionIcon
            btn.action.setParent(self.extrasMenu)
            btn.clicked.connect(lambda: self.extrasMenuButton.setChecked(True))
            self.extrasMenu.addAction(btn.action)
        else:
            self.buttonLayout.addWidget(btn)
            self.buttonBox.addButton(btn)
        return btn

    def setBgTheme(self):
        dwm = ctypes.windll.Dwmapi
        user32 = ctypes.windll.User32
        theme = getSettingsValue("PreferredTheme")
        currentBgTheme = ctypes.c_int()
        dwm.DwmGetWindowAttribute(int(self.winId()),
                                  DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  ctypes.byref(currentBgTheme),
                                  ctypes.sizeof(currentBgTheme))

        value = ctypes.c_int(0)
        match theme:
            case "dark":
                value = ctypes.c_int(1)
            case "light":
                value = ctypes.c_int(0)
            case "auto":
                key = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
                                     r"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")
                themeVal = winreg.QueryValueEx(key, "AppsUseLightTheme")
                if themeVal[0] == 0:
                    value = ctypes.c_int(1)
                else:
                    value = ctypes.c_int(0)

        if value.value != currentBgTheme.value:
            dwm.DwmSetWindowAttribute(int(self.winId()),
                                      DWMWA_USE_IMMERSIVE_DARK_MODE,
                                      ctypes.byref(value),
                                      ctypes.sizeof(value))

            user32.SetWindowPos(int(self.winId()),
                                0, 0, 0, 0, 0,
                                0x0020 | 0x0002 | 0x0004 | 0x0001)

    def nativeEvent(self, eventType, message):
        # Not using "ApplyStyleSheetsAndIcons" because it updates stylesheets and
        # icons causing large cpu usage and this code is only used to set correct
        # titlebar and mica background theme during accent color change
        # Just for resolving the bug until the API is added in Qt
        if eventType == b"windows_generic_MSG":
            msg = ctypes.wintypes.MSG.from_address(int(message))
            if msg.message == WM_DWMCOLORIZATIONCOLORCHANGED:
                if self.themeTimer.isActive():
                    self.themeTimer.stop()
                    self.themeTimer.start()
                else:
                    self.themeTimer.start()
                return True, 0
        return False, 0

    def warnAboutAdmin(self):
        self.err = CustomMessageBox(self)
        errorData = {
            "titlebarTitle": "WingetUI",
            "mainTitle": _("Administrator privileges"),
            "mainText": _("It looks like you ran WingetUI as administrator, which is not recommended. You can still use the program, but we highly recommend not running WingetUI with administrator privileges. Click on \"{showDetails}\" to see why.").format(showDetails=_("Show details")),
            "buttonTitle": _("Ok"),
            "errorDetails": _("There are two main reasons to not run WingetUI as administrator:\n The first one is that the Scoop package manager might cause problems with some commands when ran with administrator rights.\n The second one is that running WingetUI as administrator means that any package that you download will be ran as administrator (and this is not safe).\n Remeber that if you need to install a specific package as administrator, you can always right-click the item -> Install/Update/Uninstall as administrator."),
            "icon": QIcon(getMedia("icon")),
        }
        self.err.showErrorMessage(errorData, showNotification=False)

    def isAdmin(self) -> bool:
        return ctypes.windll.shell32.IsUserAnAdmin() != 0

    def deleteChildren(self) -> None:
        try:
            self.discover.destroyAnims()
            self.updates.destroyAnims()
            self.uninstall.destroyAnims()
        except Exception as e:
            report(e)

    def closeEvent(self, event):
        event.ignore()
        self.closedpos = self.pos()
        setSettingsValue("OldWindowGeometry", f"{self.closedpos.x()},{self.closedpos.y()+30},{self.width()},{self.height()}")
        setSettings("WindowWasMaximized", self.isMaximized())
        if Globals.themeChanged:
            Globals.themeChanged = False
            self.deleteChildren()
            event.accept()
        if getSettings("DisablesystemTray"):
            if Globals.pending_programs != []:
                retValue = QMessageBox.question(self, _("Warning"), _("There is an installation in progress. If you close WingetUI, the installation may fail and have unexpected results. Do you still want to quit WingetUI?"), buttons=QMessageBox.StandardButton.No | QMessageBox.StandardButton.Yes, defaultButton=QMessageBox.StandardButton.No)
                if retValue == QMessageBox.StandardButton.No:
                    event.ignore()
                    return
        self.hide()
        if Globals.updatesAvailable:
            Globals.canUpdate = True
            if Globals.ENABLE_WINGETUI_NOTIFICATIONS:
                Globals.trayIcon.showMessage(_("Updating WingetUI"), _("WingetUI is being updated. When finished, WingetUI will restart itself"), QIcon(getMedia("notif_info")))
        else:
            Globals.lastFocusedWindow = 0
            if getSettings("DisablesystemTray"):
                self.deleteChildren()
                event.accept()
                Globals.app.quit()
                sys.exit(0)

    def askRestart(self):
        e = CustomMessageBox(self)
        Thread(target=self.askRestart_threaded, args=(e,)).start()

    def askRestart_threaded(self, e: CustomMessageBox):
        questionData = {
            "titlebarTitle": _("Restart required"),
            "mainTitle": _("A restart is required"),
            "mainText": _("Do you want to restart your computer now?"),
            "acceptButtonTitle": _("Yes"),
            "cancelButtonTitle": _("No"),
            "icon": QIcon(getMedia("notif_restart")),
        }
        if e.askQuestion(questionData):
            subprocess.run("shutdown /g /t 0 /d P:04:02", shell=True)

    def resizeEvent(self, event: QResizeEvent) -> None:
        try:
            self.blackmatt.move(0, 0)
            self.blackmatt.resize(event.size())
        except AttributeError:
            pass
        try:
            s = self.infobox.size()
            if self.height() - 100 >= 650:
                self.infobox.setFixedHeight(650)
                self.infobox.move((self.width() - s.width()) // 2, (self.height() - 650) // 2)
            else:
                self.infobox.setFixedHeight(self.height() - 100)
                self.infobox.move((self.width() - s.width()) // 2, 50)

            self.infobox.iv.resize(self.width() - 100, self.height() - 100)

            Globals.centralTextureImage.move(0, 0)
            Globals.centralTextureImage.resize(event.size())

        except AttributeError:
            pass
        setSettingsValue("OldWindowGeometry", f"{self.x()},{self.y()+30},{self.width()},{self.height()}")
        setSettings("WindowWasMaximized", self.isMaximized())
        return super().resizeEvent(event)

    def showWindow(self, index=-2):
        if Globals.lastFocusedWindow != self.winId() or index >= -1:
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
            Globals.lastFocusedWindow = self.winId()
            try:
                match index:
                    case -1:
                        if Globals.updatesAvailable > 0:
                            self.widgets[self.updates].click()
                        else:
                            pass  # Show on the default window
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
        else:
            self.hide()
            Globals.lastFocusedWindow = 0

    def showEvent(self, event: QShowEvent) -> None:
        try:
            Globals.uninstall.startLoadingPackages()
        except Exception as e:
            report(e)
            
        if not getSettings("ReleaseNotesVersion2.1.2-beta2"):
            setSettings("ReleaseNotesVersion2.1.2-beta2", True)
            self.showHelpUrl("https://www.marticliment.com/wingetui/notes/2.1.2-beta2.php")
        
        return super().showEvent(event)

    def ApplyStyleSheetsAndIcons(self, skipMica: bool = False):

        if isDark():
            self.setStyleSheet(Globals.darkCSS.replace("mainbg", "transparent" if isWin11 else "#202020"))
        else:
            self.setStyleSheet(Globals.lightCSS.replace("mainbg", "transparent" if isWin11 else "#f6f6f6"))
        self.ApplyIcons()
        self.callInMain.emit(self.OnThemeChange.emit)

        if not skipMica:
            mode = win32mica.MicaTheme.AUTO
            theme = getSettingsValue("PreferredTheme")
            match theme:
                case "dark":
                    mode = win32mica.MicaTheme.DARK
                case "light":
                    mode = win32mica.MicaTheme.LIGHT
            win32mica.ApplyMica(self.winId(), mode)

    def ApplyIcons(self):
        Globals.maskedImages = {}
        Globals.cachedIcons = {}
        self.adminButton.setIcon(QIcon(getMedia("runasadmin")))
        self.extrasMenuButton.setIcon(QIcon(getMedia("hamburger")))
        for widget in self.DynamicIconsToApply.keys():
            widget.setIcon(QIcon(getMedia(self.DynamicIconsToApply[widget])))
        for manager in PackageManagersList:
            manager.LoadedIcons = False

    def enterEvent(self, event: QEnterEvent) -> None:
        Globals.lastFocusedWindow = self.winId()
        return super().enterEvent(event)

    def loseFocusUpdate(self):
        Globals.lastFocusedWindow = 0

    def focusOutEvent(self, event: QEvent) -> None:
        Thread(target=lambda: (time.sleep(0.3), self.loseFocusUpdate())).start()
        return super().focusOutEvent(event)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        setSettingsValue("OldWindowGeometry", f"{self.x()},{self.y()+30},{self.width()},{self.height()}")
        setSettings("WindowWasMaximized", self.isMaximized())
        return super().mouseReleaseEvent(event)
