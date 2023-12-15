if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import glob
import hashlib
import os
import subprocess
import sys
import time
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from threading import Thread
from urllib.request import urlopen

import wingetui.Core.Globals as Globals
import wingetui.Interface.BackendApi as BackendApi
from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.ExternalLibraries.BlurWindow import ExtendFrameIntoClientArea, GlobalBlur
from wingetui.Interface.CustomWidgets.SpecificWidgets import *
from wingetui.Interface.MainWindow import *
from wingetui.Interface.WelcomeWizard import WelcomeWindow


def RunMainApplication() -> int:
    """
    Runs the main WingetUI Graphical Application
    """
    Application = WingetUIApplication()
    OutputCode: int = Application.exec()
    Application.running = False
    return OutputCode


class WingetUIApplication(QApplication):
    kill = Signal()
    callInMain = Signal(object)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    showProgram = Signal(str)
    updatesMenu: QMenu = None
    installedMenu: QMenu = None
    running = True
    finishedPreloadingStep: Signal = Signal()
    loadStatus: int = 0

    def __init__(self):
        try:
            super().__init__(sys.argv)

            try:
                translator = QTranslator()
                translator.load(f"qtbase_{langName}.qm", QLibraryInfo.path(QLibraryInfo.LibraryPath.TranslationsPath))
                self.installTranslator(translator)
            except Exception as e:
                report(e)

            self.isDaemon: bool = "--daemon" in sys.argv
            self.popup = DraggableWindow()
            self.popup.FixLag = sys.getwindowsversion().build < 22000
            self.popup.setFixedSize(QSize(600, 400))
            self.popup.setWindowFlag(Qt.WindowType.FramelessWindowHint, on=True)
            self.popup.setLayout(QVBoxLayout())
            self.popup.layout().addStretch()
            self.popup.setWindowTitle("WingetUI")
            titlewidget = QHBoxLayout()
            titlewidget.addStretch()
            icon = QLabel()
            icon.setPixmap(QPixmap(getMedia("icon", autoIconMode=False)).scaledToWidth(128, Qt.TransformationMode.SmoothTransformation))
            text = QLabel("WingetUI")
            text.setFixedWidth(0)
            text.setAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
            text.setStyleSheet(f"font-family: \"Segoe UI Variable Display\";font-weight: bold; color: {'white' if isDark() else 'black'};font-size: 50pt;")
            titlewidget.addWidget(icon)
            titlewidget.addWidget(text)
            titlewidget.addStretch()
            self.popup.layout().addLayout(titlewidget)
            self.popup.layout().addStretch()
            self.loadingText = QLabel(_("Loading WingetUI..."))
            self.loadingText.setStyleSheet(f"font-family: \"{Globals.textfont}\"; color: {'white' if isDark() else 'black'};font-size: 12px;")
            self.popup.layout().addWidget(self.loadingText)
            ApplyMenuBlur(self.popup.winId().__int__(), self.popup)

            skipButton = QPushButton(_("Stuck here? Skip initialization"), self.popup)
            skipButton.setFlat(True)
            skipButton.move(280, 350)
            skipButton.setStyleSheet(f"color: {'white' if isDark() else 'black'}; border-radius: 4px; background-color: rgba({'255, 255, 255, 7%' if isDark() else '0, 0, 0, 7%'}); border: 1px solid rgba({'255, 255, 255, 10%' if isDark() else '0, 0, 0, 10%'})")
            skipButton.resize(300, 30)
            skipButton.hide()

            def forceContinue():
                self.loadStatus = 1000  # Override loading status

            skipButton.clicked.connect(forceContinue)

            self.textEnterAnim = QVariantAnimation(self)
            self.textEnterAnim.setStartValue(0)
            self.textEnterAnim.setEndValue(300)
            self.textEnterAnim.setEasingCurve(QEasingCurve.Type.OutQuart)
            self.textEnterAnim.valueChanged.connect(lambda v: text.setFixedWidth(v))
            self.textEnterAnim.setDuration(600)

            op1 = QGraphicsOpacityEffect()
            op1.setOpacity(0)
            self.loadingText.setGraphicsEffect(op1)
            op2 = QGraphicsOpacityEffect()
            op2.setOpacity(0)

            descriptionEnter = QVariantAnimation(self)
            descriptionEnter.setStartValue(0)
            descriptionEnter.setEndValue(100)
            descriptionEnter.setEasingCurve(QEasingCurve.Type.InOutQuad)
            descriptionEnter.valueChanged.connect(lambda v: (op1.setOpacity(v / 100), op2.setOpacity(v / 100)))
            descriptionEnter.setDuration(100)
            self.textEnterAnim.finished.connect(descriptionEnter.start)

            self.showAnimation = QPropertyAnimation(self.popup, b"windowOpacity")
            self.showAnimation.setEasingCurve(QEasingCurve.Type.OutCubic)
            self.showAnimation.setStartValue(0)
            self.showAnimation.setEndValue(1)
            self.showAnimation.setDuration(250)

            self.loadingProgressBar = QProgressBar(self.popup)
            self.loadingProgressBar.setGraphicsEffect(op2)
            self.loadingProgressBar.setStyleSheet(f"""QProgressBar {{border-radius: 2px;height: 4px;border: 0px;background-color: transparent;}}QProgressBar::chunk {{background-color: rgb({'18, 164, 199' if isDark() else '11, 100, 122'});border-radius: 2px;}}""")
            self.loadingProgressBar.setRange(0, 1000)
            self.loadingProgressBar.setValue(0)
            self.loadingProgressBar.setGeometry(QRect(0, 396, 600, 4))
            self.loadingProgressBar.setFixedHeight(4)
            self.loadingProgressBar.setTextVisible(False)
            self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
            self.startAnim.connect(lambda anim: anim.start())
            self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not self.loadingProgressBar.invertedAppearance()))

            self.leftSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
            self.leftSlow.setStartValue(0)
            self.leftSlow.setEndValue(1000)
            self.leftSlow.setDuration(700)
            self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
            descriptionEnter.finished.connect(self.leftSlow.start)

            self.rightSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
            self.rightSlow.setStartValue(1000)
            self.rightSlow.setEndValue(0)
            self.rightSlow.setDuration(700)
            self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))

            self.leftFast = QPropertyAnimation(self.loadingProgressBar, b"value")
            self.leftFast.setStartValue(0)
            self.leftFast.setEndValue(1000)
            self.leftFast.setDuration(300)
            self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

            self.rightFast = QPropertyAnimation(self.loadingProgressBar, b"value")
            self.rightFast.setStartValue(1000)
            self.rightFast.setEndValue(0)
            self.rightFast.setDuration(300)
            self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))

            if not self.isDaemon:
                self.textEnterAnim.start()
                self.showAnimation.start()
                self.popup.show()

            if not getSettings("AutoDisabledScoopCacheRemoval"):
                getSettings("EnableScoopCleanup", False)
                getSettings("AutoDisabledScoopCacheRemoval", True)

            print("🔵 Starting main application...")
            os.chdir(os.path.expanduser("~"))
            self.kill.connect(lambda: (self.popup.hide(), sys.exit(0)))
            self.callInMain.connect(lambda f: f())

            def increaseStep():
                self.loadStatus += 1
            self.finishedPreloadingStep.connect(increaseStep)
            if getSettings("ShownWelcomeWizard") is False or "--welcomewizard" in sys.argv or "--welcome" in sys.argv:
                self.askAboutPackageManagers(onclose=lambda: (Thread(target=self.loadPreUIComponents, daemon=True).start(), Thread(target=lambda: (time.sleep(15), self.callInMain.emit(skipButton.show)), daemon=True).start()))
            else:
                Thread(target=self.loadPreUIComponents, daemon=True).start()
                Thread(target=lambda: (time.sleep(15), self.callInMain.emit(skipButton.show)), daemon=True).start()
                self.loadingText.setText(_("Checking for other running instances..."))
        except Exception as e:
            raise e

    def askAboutPackageManagers(self, onclose: object):
        self.ww = WelcomeWindow(callback=lambda: (self.popup.show(), onclose()))
        self.popup.hide()
        self.ww.show()

    def loadPreUIComponents(self):
        try:
            self.loadStatus = 0

            # Preparation threads
            Thread(target=self.checkForRunningInstances, daemon=True).start()
            Thread(target=self.downloadPackagesMetadata, daemon=True).start()
            if not getSettings("DisableApi"):
                Thread(target=BackendApi.runBackendApi, args=(self.showProgram,), daemon=True).start()

            for manager in PackageManagersList:
                if manager.isEnabled():
                    Thread(target=manager.detectManager, args=(self.finishedPreloadingStep,), daemon=True).start()
                else:
                    self.loadStatus += 1
                    Globals.componentStatus[f"{manager.NAME}Found"] = False
                    Globals.componentStatus[f"{manager.NAME}Version"] = _("{0} is disabled").format(manager.NAME)

            if not getSettings("DisableUpdateIndexes"):
                for manager in PackageManagersList:
                    if manager.isEnabled():
                        Thread(target=manager.updateSources, args=(self.finishedPreloadingStep,), daemon=True).start()
                    else:
                        self.loadStatus += 1
            else:
                self.loadStatus += len(PackageManagersList)

            Thread(target=self.detectSudo, daemon=True).start()
            Thread(target=self.getAUMID, daemon=True).start()
            Thread(target=self.removeScoopCache, daemon=True).start()

            # Daemon threads
            Thread(target=self.instanceThread, daemon=True).start()
            Thread(target=self.updateIfPossible, daemon=True).start()

            while self.loadStatus < 4 + len(PackageManagersList) * 2:
                time.sleep(0.01)
        except Exception as e:
            print(e)
        finally:
            self.callInMain.emit(lambda: self.loadingText.setText(_("Loading UI components...")))
            self.callInMain.emit(lambda: self.loadingProgressBar.setValue(1000))
            self.callInMain.emit(lambda: self.loadingText.repaint())
            self.callInMain.emit(lambda: self.loadingProgressBar.repaint())
            self.callInMain.emit(self.loadMainUI)
            print(Globals.componentStatus)

    def getAUMID(self):
        print("🔵 Loading WingetUI AUMID...")
        try:
            output = str(subprocess.check_output(["powershell", "-NoProfile", "-Command", "Get-StartApps"], shell=True), encoding='utf-8', errors='ignore')
            for line in output.split("\n"):
                if list(filter(None, line.split(" ")))[0] == "WingetUI":
                    Globals.AUMID = list(filter(None, line.split(" ")))[1]
                    print(f"🟢 Found valid aumid {Globals.AUMID}")
                    self.loadStatus += 1
                    return
            print("🟠 Did not find a valid AUMID")
            self.loadStatus += 1
        except Exception as e:
            self.loadStatus += 1
            report(e)

    def checkForRunningInstances(self):
        print("🔵 Looking for alive instances...")
        self.nowTime = time.time()
        self.lockFileName = f"WingetUI_{self.nowTime}"
        setSettings(self.lockFileName, True)
        try:
            timestamps = [float(file.replace(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_"), "")) for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_*"))]  # get a list with the timestamps
            validTimestamps = [timestamp for timestamp in timestamps if timestamp < self.nowTime]
            self.callInMain.emit(lambda: self.loadingText.setText(_("Checking found instace(s)...")))
            print("🟡 Found lock file(s), reactivating...")
            for tst in validTimestamps:
                setSettings("RaiseWindow_" + str(tst), True)
            if validTimestamps != [] and timestamps != [self.nowTime]:
                for i in range(16):
                    time.sleep(0.1)
                    self.callInMain.emit(lambda: self.loadingText.setText(_("Sent handshake. Waiting for instance listener's answer... ({0}%)").format(int(i / 15 * 100))))
                    for tst in validTimestamps:
                        if not getSettings("RaiseWindow_" + str(tst), cache=False):
                            print(f"🟡 Instance {tst} responded, quitting...")
                            self.callInMain.emit(lambda: self.loadingText.setText(_("Instance {0} responded, quitting...").format(tst)))
                            setSettings(self.lockFileName, False)
                            while self.textEnterAnim.state() == QAbstractAnimation.State.Running:
                                time.sleep(0.1)
                            self.kill.emit()
                            sys.exit(0)
                self.callInMain.emit(lambda: self.loadingText.setText(_("Starting daemons...")))
                print("🔵 Reactivation signal ignored: RaiseWindow_" + str(validTimestamps))
                for tst in validTimestamps:
                    setSettings("RaiseWindow_" + str(tst), False)
                    setSettings("WingetUI_" + str(tst), False)
        except Exception as e:
            print(e)
        self.loadStatus += 1

    def removeScoopCache(self):
        try:
            if getSettings("EnableScoopCleanup"):
                self.callInMain.emit(lambda: self.loadingText.setText(_("Clearing Scoop cache...")))
                p = subprocess.Popen(f"{Scoop.EXECUTABLE} cache rm *", shell=True, stdout=subprocess.PIPE)
                p2 = subprocess.Popen(f"{Scoop.EXECUTABLE} cleanup --all --cache", shell=True, stdout=subprocess.PIPE)
                p3 = subprocess.Popen(f"{Scoop.EXECUTABLE} cleanup --all --global --cache", shell=True, stdout=subprocess.PIPE)
                p.wait()
                p2.wait()
                p3.wait()
        except Exception as e:
            report(e)

    def detectSudo(self):
        global GSUDO_EXE_LOCATION
        try:
            self.callInMain.emit(lambda: self.loadingText.setText(_("Locating {pm}...").format(pm="sudo")))
            o = subprocess.run([GSUDO_EXECUTABLE, '-v'], shell=True, stdout=subprocess.PIPE)
            Globals.componentStatus["sudoFound"] = shutil.which(GSUDO_EXECUTABLE) is not None
            Globals.componentStatus["sudoVersion"] = o.stdout.decode('utf-8').split("\n")[0]
            self.callInMain.emit(lambda: self.loadingText.setText(_("{pm} found: {state}").format(pm="Sudo", state=_("Yes") if Globals.componentStatus['sudoFound'] else _("No"))))
        except Exception as e:
            print(e)
        self.loadStatus += 1

    def downloadPackagesMetadata(self):
        self.callInMain.emit(lambda: self.loadingText.setText(_("Downloading package metadata...")))
        url = "https://raw.githubusercontent.com/marticliment/WingetUI/main/WebBasedData/screenshot-database-v2.json"
        try:
            if getSettings("IconDataBaseURL"):
                url = getSettingsValue("iconDataBaseURL")
            data = urlopen(url).read()

            if not os.path.exists(CACHED_DIR):
                os.makedirs(CACHED_DIR)
            with open(os.path.join(CACHED_DIR, "Icon Database.json"), "wb") as f:
                f.write(data)
            print(f"🟢 Downloaded latest metadata to local file from url {url}")
        except Exception as e:
            print(f"🔴 Could not load latest metadata from remote file {url}")
            report(e)
        try:
            with open(os.path.join(CACHED_DIR, "Icon Database.json"), "rb") as f:
                Globals.packageMeta = json.load(f)
            print("🔵 Loaded metadata from local file")
        except Exception as e:
            report(e)
        self.loadStatus += 1

    def loadMainUI(self):
        print("🔵 Reached main ui load milestone")
        try:
            setSettingsValue("CurrentSessionToken", Globals.CurrentSessionToken)

            Globals.trayIcon = QSystemTrayIcon()
            self.trayIcon = Globals.trayIcon
            Globals.app = self
            self.trayIcon.setToolTip(_("Initializing WingetUI..."))
            self.trayIcon.setVisible(True)

            menu = QMenu("WingetUI")
            Globals.trayMenu = menu
            self.trayIcon.setContextMenu(menu)
            self.discoverPackages = QAction(_("Discover Packages"), menu)
            menu.addAction(self.discoverPackages)
            menu.addSeparator()

            self.updatePackages = QAction(_("Software Updates"), menu)
            Globals.updatesAction = self.updatePackages
            menu.addAction(self.updatePackages)

            self.updatesMenu = menu.addMenu(_("0 updates found"))
            self.updatesMenu.setParent(menu)
            Globals.trayMenuUpdatesList = self.updatesMenu
            menu.addMenu(self.updatesMenu)

            Globals.updatesHeader = QAction(f"{_('App Name')}  \t{_('Installed Version')} \t → \t {_('New version')}", menu)
            Globals.updatesHeader.setEnabled(False)
            self.updatesMenu.addAction(Globals.updatesHeader)

            self.uaAction = QAction(_("Update all"), menu)
            menu.addAction(self.uaAction)
            menu.addSeparator()

            self.uninstallPackages = QAction(_("Installed Packages"), menu)
            menu.addAction(self.uninstallPackages)

            self.installedMenu = menu.addMenu(_("0 packages found"))
            self.installedMenu.setParent(menu)
            Globals.trayMenuInstalledList = self.installedMenu
            menu.addMenu(self.installedMenu)
            menu.addSeparator()

            Globals.installedHeader = QAction(f"{_('App Name')}\t{_('Installed Version')}", menu)
            Globals.installedHeader.setEnabled(False)
            self.installedMenu.addAction(Globals.installedHeader)

            self.infoAction = QAction(_("About WingetUI version {0}").format(versionName), menu)
            menu.addAction(self.infoAction)
            self.showAction = QAction(_("Show WingetUI"), menu)
            menu.addAction(self.showAction)
            menu.addSeparator()

            self.settings = QAction(_("WingetUI Settings"), menu)
            menu.addAction(self.settings)

            self.quitAction = QAction(menu)
            self.quitAction.setText(_("Quit"))
            self.quitAction.triggered.connect(lambda: self.quit())
            menu.addAction(self.quitAction)

            def ApplyMenuIcons():
                self.infoAction.setIcon(QIcon(getMedia("info")))
                self.showAction.setIcon(QIcon(getMedia("icon")))
                Globals.installedHeader.setIcon(QIcon(getMedia("version")))
                self.installedMenu.menuAction().setIcon(QIcon(getMedia("list")))
                Globals.updatesHeader.setIcon(QIcon(getMedia("version")))
                self.uaAction.setIcon(QIcon(getMedia("menu_installall")))
                self.updatesMenu.menuAction().setIcon(QIcon(getMedia("list")))
                self.quitAction.setIcon(QIcon(getMedia("menu_close")))
                self.updatePackages.setIcon(QIcon(getMedia("alert_laptop")))
                self.discoverPackages.setIcon(QIcon(getMedia("desktop_download")))
                self.settings.setIcon(QIcon(getMedia("settings_gear")))
                self.uninstallPackages.setIcon(QIcon(getMedia("workstation")))
                update_tray_icon()

            ApplyMenuIcons()

            def showWindow():
                # This function will be defined when the mainWindow gets defined
                pass

            def showMenu():
                pos = QCursor.pos()
                s = self.screenAt(pos)
                if isWin11 and (pos.y() + 48) > (s.geometry().y() + s.geometry().height()):
                    menu.move(pos)
                    menu.show()
                    sy = s.geometry().y() + s.geometry().height()
                    sx = s.geometry().x() + s.geometry().width()
                    pos.setY(sy - menu.height() - 54)  # Show the context menu a little bit over the taskbar
                    pos.setX(sx - menu.width() - 6 if sx - menu.width() - 6 < pos.x() else pos.x())  # Show the context menu a little bit over the taskbar
                    menu.move(pos)
                else:
                    menu.exec(pos)
            self.trayIcon.activated.connect(lambda r: (applyMenuStyle(), showMenu()) if r == QSystemTrayIcon.Context else showWindow())

            self.trayIcon.messageClicked.connect(lambda: showWindow())
            self.installedMenu.aboutToShow.connect(lambda: applyMenuStyle())
            self.updatesMenu.aboutToShow.connect(lambda: applyMenuStyle())

            def applyMenuStyle():
                for mn in (menu, self.updatesMenu, self.installedMenu):
                    mn.setObjectName("MenuMenuMenu")
                    if not isDark():
                        ss = f'#{mn.objectName()}{{background-color: {"rgba(220, 220, 220, 1%)" if isWin11 else "rgba(255, 255, 255, 30%);border-radius: 0px;" };}}'
                    else:
                        ss = f'#{mn.objectName()}{{background-color: {"rgba(220, 220, 220, 1%)" if isWin11 else "rgba(20, 20, 20, 25%);border-radius: 0px;"};}}'
                    if isDark():
                        ExtendFrameIntoClientArea(mn.winId().__int__())
                        mn.setStyleSheet(menuDarkCSS + ss)
                        GlobalBlur(mn.winId().__int__(), Acrylic=True, hexColor="#21212140", Dark=True)
                    else:
                        ExtendFrameIntoClientArea(mn.winId().__int__())
                        mn.setStyleSheet(menuLightCSS + ss)
                        GlobalBlur(mn.winId().__int__(), Acrylic=True, hexColor="#eeeeee40", Dark=False)

            self.setStyle("winvowsvista")
            Globals.darkCSS = darkCSS.replace("Segoe UI Variable Text", Globals.textfont).replace("Segoe UI Variable Display", Globals.dispfont).replace("Segoe UI Variable Display Semib", Globals.dispfontsemib)
            Globals.lightCSS = lightCSS.replace("Segoe UI Variable Text", Globals.textfont).replace("Segoe UI Variable Display", Globals.dispfont).replace("Segoe UI Variable Display Semib", Globals.dispfontsemib)
            self.window = RootWindow()
            self.window.OnThemeChange.connect(ApplyMenuIcons)

            self.showProgram.connect(lambda id: (self.discoverPackages.trigger(), Globals.discover.loadShared(id)))
            self.discoverPackages.triggered.connect(lambda: self.window.showWindow(0))
            self.updatePackages.triggered.connect(lambda: self.window.showWindow(1))
            self.uninstallPackages.triggered.connect(lambda: self.window.showWindow(2))
            self.infoAction.triggered.connect(lambda: self.window.showWindow(4))
            self.settings.triggered.connect(lambda: self.window.showWindow(3))
            Globals.mainWindow = self.window
            self.showAction.triggered.connect(lambda: self.window.showWindow())
            self.uaAction.triggered.connect(self.window.updates.upgradeAllAction.trigger)

            def showWindow():  # The function is refedined
                self.showAction.trigger()

            self.loadingText.setText(_("Latest details..."))
            if not self.isDaemon:
                self.window.show()
                self.popup.close()
                if self.window.isAdmin():
                    if not getSettings("AlreadyWarnedAboutAdmin"):
                        self.window.warnAboutAdmin()
                        setSettings("AlreadyWarnedAboutAdmin", True)

        except Exception as e:
            import platform
            import traceback
            import webbrowser
            try:
                from wingetui.Core.Tools import version as appversion
            except Exception:
                appversion = "Unknown"
            os_info = "" + \
                f"                        OS: {platform.system()}\n" + \
                f"                   Version: {platform.win32_ver()}\n" + \
                f"           OS Architecture: {platform.machine()}\n" + \
                f"          APP Architecture: {platform.architecture()[0]}\n" + \
                f"                  Language: {langName}\n" + \
                f"               APP Version: {appversion}\n" + \
                "                   Program: WingetUI\n" + \
                "           Program section: UI Loading" + \
                "\n\n-----------------------------------------------------------------------------------------"
            traceback_info = "Traceback (most recent call last):\n"
            try:
                for line in traceback.extract_tb(e.__traceback__).format():
                    traceback_info += line
                traceback_info += f"\n{type(e).__name__}: {str(e)}"
            except Exception:
                traceback_info += "\nUnable to get traceback"
            traceback_info += str(type(e))
            traceback_info += ": "
            traceback_info += str(e)
            webbrowser.open(("https://www.marticliment.com/error-report/?appName=WingetUI&errorBody=" + os_info.replace('\n', '{l}').replace(' ', '{s}') + "{l}{l}{l}{l}WingetUI Log:{l}" + str("\n\n\n\n" + traceback_info).replace('\n', '{l}').replace(' ', '{s}')).replace("#", "|=|"))
            print(traceback_info)
            self.popup.hide()

    def reloadWindow(self):
        cprint("Reloading...")
        self.infoAction.setIcon(QIcon(getMedia("info")))
        self.updatesMenu.menuAction().setIcon(QIcon(getMedia("list")))
        Globals.updatesHeader.setIcon(QIcon(getMedia("version")))
        self.uaAction.setIcon(QIcon(getMedia("menu_installall")))
        self.iAction.setIcon(QIcon(getMedia("menu_uninstall")))
        self.installedMenu.menuAction().setIcon(QIcon(getMedia("list")))
        Globals.installedHeader.setIcon(QIcon(getMedia("version")))
        self.quitAction.setIcon(QIcon(getMedia("menu_close")))
        self.showAction.setIcon(QIcon(getMedia("menu_show")))
        Globals.themeChanged = True
        Globals.mainWindow.setAttribute(Qt.WA_DeleteOnClose, True)
        Globals.mainWindow.close()
        Globals.mainWindow.deleteLater()
        self.window = RootWindow()
        Globals.mainWindow = self.window
        self.showAction.triggered.disconnect()
        self.showAction.triggered.connect(self.window.showWindow)

    def instanceThread(self):
        while True:
            try:
                for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "RaiseWindow_*")):
                    if getSettings("RaiseWindow_" + str(self.nowTime), cache=False):
                        print("🟢 Found reactivation lock file...")
                        setSettings("RaiseWindow_" + str(self.nowTime), False)
                        if not self.window.isMaximized():
                            self.callInMain.emit(self.window.hide)
                            self.callInMain.emit(self.window.showMinimized)
                            self.callInMain.emit(self.window.show)
                            self.callInMain.emit(self.window.showNormal)
                        else:
                            self.callInMain.emit(self.window.hide)
                            self.callInMain.emit(self.window.showMinimized)
                            self.callInMain.emit(self.window.show)
                            self.callInMain.emit(self.window.showMaximized)
                        self.callInMain.emit(self.window.setFocus)
                        self.callInMain.emit(self.window.raise_)
                        self.callInMain.emit(self.window.activateWindow)
            except Exception as e:
                print(e)
            time.sleep(0.5)

    def updateIfPossible(self, round: int = 0):
        if not getSettings("DisableAutoUpdateWingetUI"):
            print("🔵 Starting update check")
            try:
                response = urlopen("https://www.marticliment.com/versions/wingetui.ver")
            except Exception as e:
                print(e)
                response = urlopen("https://versions.marticliment.com/versions/wingetui.ver")
            print("🔵 Version URL:", response.url)
            response = response.read().decode("utf8")
            new_version_number = response.split("///")[0]
            provided_hash = response.split("///")[1].replace("\n", "").lower()
            if float(new_version_number) > version:
                print("🟢 Updates found!")
                url = "https://github.com/marticliment/WingetUI/releases/latest/download/WingetUI.Installer.exe"
                filedata = urlopen(url)
                datatowrite = filedata.read()
                filename = ""
                downloadPath = os.environ["temp"] if "temp" in os.environ.keys() else os.path.expanduser("~")
                with open(os.path.join(downloadPath, "wingetui-updater.exe"), 'wb') as f:
                    f.write(datatowrite)
                    filename = f.name
                if hashlib.sha256(datatowrite).hexdigest().lower() == provided_hash:
                    print("🔵 Hash: ", provided_hash)
                    print("🟢 Hash ok, starting update")
                    Globals.updatesAvailable = True
                    while Globals.mainWindow is None:
                        time.sleep(1)
                    Globals.canUpdate = not Globals.mainWindow.isVisible()
                    while not Globals.canUpdate:
                        time.sleep(0.1)
                    if not getSettings("DisableAutoUpdateWingetUI"):
                        subprocess.run(f'start /B "" "{filename}" /silent', shell=True)
                else:
                    print("🟠 Hash not ok")
                    print("🟠 File hash: ", hashlib.sha256(datatowrite).hexdigest())
                    print("🟠 Provided hash: ", provided_hash)
            else:
                print("🟢 Updates not found")
        if round <= 2:
            time.sleep(600)
            self.updateIfPossible(round + 1)


darkCSS = f"""
* {{
    background-color: transparent;
    color: #eeeeee;
    font-family: "Segoe UI Variable Text";
    outline: none;
}}
#InWindowNotification {{
    background-color: #181818;
    border-radius: 16px;
    height: 32px;
    border: 1px solid #101010;
}}
*::disabled {{
    color: gray;
}}
QInputDialog {{
    background-color: #202020;
}}
#micawin {{
    background-color: mainbg;
}}
QMenu {{
    padding: 2px;
    color: white;
    background: transparent;
    border-radius: 8px;
}}
QMenu::separator {{
    margin: 2px;
    height: 1px;
    background: rgb(60, 60, 60);
}}
QMenu::icon{{
    padding-left: 10px;
}}
QMenu::item{{
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
    margin: 2px;
}}
QMenu::item:disabled{{
    background: transparent;
    height: 30px;
    color: grey;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QMenu::item:selected{{
    background: rgba(255, 255, 255, 10%);
    height: 30px;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QMenu::item:selected:disabled{{
    background: transparent;
    height: 30px;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QMessageBox{{
    background-color: #202020;
}}
#greyLabel {{
    color: #bbbbbb;
}}
QPushButton,#FocusLabel {{
    width: 150px;
    background-color:rgba(81, 81, 81, 25%);
    border-radius: 6px;
    border: 1px solid rgba(86, 86, 86, 25%);
    height: 25px;
    font-size: 9pt;
    border-top: 1px solid rgba(99, 99, 99, 25%);
    margin: 0px;
    font-family: "Segoe UI Variable Display Semib";
}}
#FlatButton {{
    width: 150px;
    background-color: rgba(255, 255, 255, 1%);
    border-radius: 6px;
    border: 0px solid rgba(255, 255, 255, 1%);
    height: 25px;
    font-size: 9pt;
    border-top: 0px solid rgba(255, 255, 255, 1%);
}}
QPushButton:hover {{
    background-color:rgba(86, 86, 86, 25%);
    border-radius: 6px;
    border: 1px solid rgba(100, 100, 100, 25%);
    height: 30px;
    border-top: 1px solid rgba(107, 107, 107, 25%);
}}
QPushButton:checked {{
    background-color:rgba(86, 86, 86, 55%);
    border-radius: 6px;
    border: 1px solid rgba(100, 100, 100, 55%);
    height: 30px;
    border-top: 1px solid rgba(107, 107, 107, 55%);
}}
#Headerbutton {{
    width: 150px;
    background-color:rgba(0, 0, 0, 1%);
    border-radius: 6px;
    border: 0px solid transparent;
    height: 25px;
    font-size: 9pt;
    margin: 0px;
    font-family: "Segoe UI Variable Display Semib";
    font-size: 9pt;
}}
#Headerbutton:hover {{
    background-color:rgba(100, 100, 100, 12%);
    border-radius: 8px;
    height: 30px;
}}
#Headerbutton:checked {{
    background-color:rgba(100, 100, 100, 25%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#package {{
    background-color:rgba(100, 100, 100, 6%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#PackageButton {{
    width: 150px;
    background-color:rgba(81, 81, 81, 15%);
    border-radius: 6px;
    border: 0px solid rgba(86, 86, 86, 25%);
    height: 25px;
    font-size: 9pt;
    margin: 0px;
    font-family: "Segoe UI Variable Display Semib";
}}
#PackageButton:hover {{
    background-color:rgba(86, 86, 86, 25%);
    border-radius: 6px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#buttonier {{
    border: 0px solid rgba(100, 100, 100, 25%);
    border-radius: 12px;
}}
#AccentButton{{
    color: #202020;
    font-size: 9pt;
    font-family: "Segoe UI Variable Display Semib";
    background-color: rgb({colors[1]});
    border-color: rgb({colors[1]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:hover{{
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:pressed{{
    color: #555555;
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:disabled{{
    color: grey;
    background-color: rgba(50,50,50, 80%);
    border-color: rgb(50, 50, 50);
    border-bottom-color: rgb(50, 50, 50);
}}
QLineEdit {{
    background-color: rgba(81, 81, 81, 25%);
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(86, 86, 86, 25%);
    border-bottom: 2px solid rgb({colors[4]});
    selection-background-color: rgb({colors[2]});
}}
QLineEdit:disabled {{
    background-color: rgba(81, 81, 81, 25%);
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(86, 86, 86, 25%);
}}
QLabel{{
    selection-background-color: rgb({colors[2]});
}}
QScrollBar {{
    background: transparent;
    margin: 4px;
    margin-left: 0;
    width: 16px;
    height: 20px;
    border: none;
    border-radius: 5px;
}}
QScrollBar:horizontal {{
    margin-bottom: 0;
    padding-bottom: 0;
    height: 12px;
}}
QScrollBar::handle {{
    margin: 3px;
    min-height: 20px;
    min-width: 20px;
    border-radius: 3px;
    background: rgba(80, 80, 80, 40%);
}}
QScrollBar::handle:hover {{
    margin: 3px;
    border-radius: 3px;
    background: rgba(112, 112, 112, 35%);
}}
QScrollBar::add-line {{
    height: 0;
    width: 0;
    subcontrol-position: bottom;
    subcontrol-origin: margin;
}}
QScrollBar::sub-line {{
    height: 0;
    width: 0;
    subcontrol-position: top;
    subcontrol-origin: margin;
}}
QScrollBar::up-arrow, QScrollBar::down-arrow {{
    background: none;
}}
QScrollBar::add-page, QScrollBar::sub-page {{
    background: none;
}}
QHeaderView,QAbstractItemView {{
    background-color: transparent;
    border-radius: 8px;
    border: none;
    padding: 0px;
    height: 35px;
    border: 0px solid black;
    margin-bottom: 5px;
    margin-left: 0px;
    margin-right: 0px;
}}
QHeaderView {{
    padding-right: 0px;
}}
QHeaderView::section {{
    background-color: rgba(255, 255, 255, 5%);
    border-top: 1px solid rgba(25, 25, 25, 50%);
    border-bottom: 1px solid rgba(25, 25, 25, 50%);
    padding: 0px;
    height: 35px;
    border-radius: 0px;
    margin: 0px;
    padding-bottom: 4px;
    padding-top: 4px;
}}
QHeaderView::section:first {{
    border-left: 1px solid rgba(25, 25, 25, 50%);
    border-top-left-radius: 8px;
    border-bottom-left-radius: 8px;
}}
QHeaderView::section:last {{
    border-right: 1px solid rgba(25, 25, 25, 50%);
    border-top-right-radius: 8px;
    border-bottom-right-radius: 8px;
}}
QTreeWidget {{
    show-decoration-selected: 0;
    background-color: transparent;
    padding: 0px;
    margin: 0px;
    border-radius: 6px;
    border: 0px solid #1f1f1f;
}}
QTreeWidget::item {{
    margin-top: 3px;
    margin-bottom: 3px;
    padding-top: 3px;
    padding-bottom: 3px;
    background-color: rgba(255, 255, 255, 6);
    height: 25px;
    border-bottom: 1px solid rgba(25, 25, 25, 50%);
    border-top: 1px solid rgba(25, 25, 25, 50%);
}}
QTreeWidget#FlatTreeWidget::item,
QTreeWidget#FlatTreeWidget::item:first {{
    margin: 0px;
    padding: 0px;
    background-color: transparent;
    height: 30px;
    border: 0px;
    border-radius: 0px;
    border-bottom: 1px solid rgba(25, 25, 25, 25%);
}}
QTreeWidget#FlatTreeWidget::item:last {{
    padding-right: 10px;
}}
#IslandWidget {{
    padding: 5px;
    margin: 5px;
    margin-top: 0px;
    margin-bottom: 0px;
    background-color: rgba(48, 48, 48, 40%);
    border: 1px solid #1f1f1f;
    border-radius: 8px;
}}
QTreeWidget::item:selected {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    background-color: rgba(255, 255, 255, 8);
    height: 25px;
    border-bottom: 1px solid rgba(25, 25, 25, 25%);
    border-top: 1px solid rgba(25, 25, 25, 25%);
    color: rgb({colors[2]});
}}
QTreeWidget::item:hover {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    background-color: rgba(255, 255, 255, 12);
    height: 25px;
    border-bottom: 1px solid rgba(25, 25, 25, 25%);
    border-top: 1px solid rgba(25, 25, 25, 25%);
}}
QTreeWidget::item:first {{
    border-top-left-radius: 8px;
    border-bottom-left-radius: 8px;
    border-left: 1px solid rgba(25, 25, 25, 25%);
    margin-left: 0px;
    padding-left: 0px;
}}
QTreeWidget::item:last {{
    border-top-right-radius: 8px;
    border-bottom-right-radius: 8px;
    border-right: 1px solid rgba(25, 25, 25, 25%);
    padding-right: 0px;
    margin-right: 0px;
}}
QTreeWidget::item:first:selected {{
    border-left: 1px solid rgba(25, 25, 25, 25%);
}}
QTreeWidget::item:last:selected {{
    border-right: 1px solid rgba(25, 25, 25, 25%);
}}
QTreeWidget::item:first:hover {{
    border-left: 1px solid rgba(25, 25, 25, 25%);
}}
QTreeWidget::item:last:hover {{
    border-right: 1px solid rgba(25, 25, 25, 25%);
}}
QProgressBar {{
    border-radius: 2px;
    height: 4px;
    border: 0px;
}}
QProgressBar::chunk {{
    background-color: rgb({colors[2]});
    border-radius: 2px;
}}
QCheckBox::indicator{{
    height: 16px;
    width: 16px;
}}
QTreeView::indicator{{
    height:18px;
    width: 18px;
    margin: 0px;
    margin-left: 4px;
    margin-top: 2px;
}}
QTreeView::indicator:unchecked,QCheckBox::indicator:unchecked {{
    background-color: rgba(30, 30, 30, 25%);
    border: 1px solid #444444;
    border-radius: 4px;
}}
QTreeView::indicator:disabled,QCheckBox::indicator:disabled {{
    background-color: rgba(30, 30, 30, 5%);
    color: #dddddd;
    border: 1px solid rgba(255, 255, 255, 5%);
    border-radius: 4px;
}}
QTreeView::indicator:unchecked:hover,QCheckBox::indicator:unchecked:hover {{
    background-color: #2a2a2a;
    border: 1px solid #444444;
    border-radius: 4px;
}}
QTreeView::indicator:checked,QCheckBox::indicator:checked {{
    border: 1px solid #444444;
    background-color: rgba({colors[1]}, 80%);
    border-radius: 4px;
    image: url("{getMedia("tick")}");
}}
QTreeView::indicator:disabled,QCheckBox::indicator:checked:disabled {{
    border: 1px solid #444444;
    background-color: #303030;
    color: #dddddd;
    border-radius:4px;
}}
QTreeView::indicator:checked:hover,QCheckBox::indicator:checked:hover {{
    border: 1px solid #444444;
    background-color: rgb({colors[2]});
    border-radius: 4px;
}}
QComboBox {{
    width: 200px;
    background-color:rgba(81, 81, 81, 10%);
    border-radius: 6px;
    border: 1px solid rgba(86, 86, 86, 10%);
    height: 30px;
    border-top: 1px solid rgba(99, 99, 99, 10%);
    padding-left: 10px;
    padding-right: 10px;
}}
QComboBox:hover {{
    background-color:rgba(86, 86, 86, 20%);
    border-radius: 6px;
    border: 1px solid rgba(100, 100, 100, 15%);
    height: 30px;
    border-top: 1px solid rgba(107, 107, 107, 15%);
}}
QComboBox::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    background-color: none;
    padding: 5px;
    border-radius: 6px;
    border: none;
    width: 30px;
}}
QComboBox::down-arrow {{
    image: url("{getMedia("drop-down")}");
    height: 8px;
    width: 8px;
}}
QComboBox::down-arrow:disabled {{
    image: url("{getMedia("drop-down")}");
    height: 8px;
    width: 8px;
}}
QComboBox QAbstractItemView {{
    padding: 4px;
    margin: 0px;
    border-radius: 8px;
}}
QComboBox#transparent QAbstractItemView {{
    border: 1px solid transparent;
    background-color: transparent;
}}
QComboBox QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(255, 255, 255, 6%);
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QListWidget{{
    border: 0px;
    background-color: transparent;
    color: transparent;
}}
QListWidget::item{{
    border: 0px;
    background-color: transparent;
    color: transparent;
}}
QPlainTextEdit{{
    border: 1px solid #1b1b1b;
    border-radius: 6px;
    padding: 6px;
    color: white;
    background-color: #212121;
    font-family: "Consolas";
}}
QToolTip {{
    background-color: #262626;
    border: 1px solid #202020;
    border-radius: 6px;
    padding: 4px;
    border-radius: 0px;
}}
QToolButton {{
    background-color:rgba(0, 0, 0, 1%);
    border-radius: 4px;
    border: 0px solid transparent;
    margin: 5px;
    margin-right: 0px;
    font-size: 9pt;
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    padding: 4px;
    outline: 2px solid white;
    outline-offset: -3px;
    outline-radius: 8px;
}}
QToolButton:hover {{
    background-color:rgba(100, 100, 100, 12%);
    border-radius: 4px;
    margin: 5px;
    margin-right: 0px;
    padding: 4px;
}}
QToolBar:separator {{
    width: 1px;
    margin: 5px;
    margin-right: 0px;
    background-color: rgba(255, 255, 255, 10%);
}}
#greyishLabel {{
    color: #aaaaaa;
}}
#subtitleLabelHover {{
    background-color: rgba(20, 20, 20, 0.01);
    margin: 0px;
    border-radius: 4px;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
    border: 1px solid transparent;
}}
#subtitleLabelHover:hover{{
    background-color: rgba(255, 255, 255, 3%);
    margin: 0px;
    padding-left: {(20)}px;
    padding-top: 0;
    padding-bottom: 0;
    border: 1px solid rgba(255, 255, 255, 7%);
    font-size: 13pt;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
}}
#subtitleLabelHover:pressed{{
    background-color: rgba(0, 0, 0, 12%);
    margin: 0px;
    padding-left: {(20)}px;
    padding-top: 0;
    padding-bottom: 0;
    border: 1px solid rgba(255, 255, 255, 7%);
    font-size: 13pt;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
}}
#micaRegularBackground {{
    border: 0 solid transparent;
    margin: 1px;
    background-color: rgba(255, 255, 255, 5%);
    border-radius: 8px;
}}
#subtitleLabel{{
    margin: 0px;
    padding-left: {(20)}px;
    padding-top: {(15)}px;
    padding-bottom: {(15)}px;
    border: 1px solid rgba(25, 25, 25, 50%);
    font-size: 13pt;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
}}
#StLbl{{
    padding: 0;
    background-color: rgba(71, 71, 71, 0%);
    margin: 0;
    border:none;
    font-size: {(11)}px;
}}
#stBtn{{
    background-color: rgba(255, 255, 255, 5%);
    margin: 10px;
    margin-bottom: 0;
    margin-top: 0;
    border: 1px solid rgba(25, 25, 25, 50%);
    border-bottom-left-radius: 8px;
    border-bottom-right-radius: 8px;
}}
#lastWidget{{
    border-bottom-left-radius: 4px;
    border-bottom-right-radius: 4px;
}}
#stChkBg{{
    padding: 15px;
    padding-left: 45px;
    background-color: rgba(255, 255, 255, 5%);
    margin: 10px;
    margin-bottom: 0;
    margin-top: 0;
    border: 1px solid rgba(25, 25, 25, 50%);
    border-bottom: 0;
}}
QTreeView::indicator,
QCheckBox::indicator,
#stChk::indicator  {{
    height: 20px;
    width: 20px;
}}
QTreeView::indicator:unchecked,
QCheckBox::indicator:unchecked,
#stChk::indicator:unchecked {{
    background-color: rgba(30, 30, 30, 25%);
    border: 1px solid #444444;
    border-radius: 6px;
}}
QTreeView::indicator:disabled,
QCheckBox::indicator:disabled,
#stChk::indicator:disabled {{
    background-color: rgba(71, 71, 71, 0%);
    color: #bbbbbb;
    border: 1px solid #444444;
    border-radius: 6px;
}}
QTreeView::indicator:unchecked:hover,
QCheckBox::indicator:unchecked:hover,
#stChk::indicator:unchecked:hover {{
    background-color: #2a2a2a;
    border: 1px solid #444444;
    border-radius: 6px;
}}
QTreeView::indicator:checked,
QCheckBox::indicator:checked,
#stChk::indicator:checked {{
    border: 1px solid #444444;
    background-color: rgb({colors[1]});
    border-radius: 6px;
    image: url("{getPath("tick_white.png")}");
}}
QTreeView::indicator:checked:disabled,
QCheckBox::indicator:checked:disabled,
#stChk::indicator:checked:disabled {{
    border: 1px solid #444444;
    background-color: #303030;
    color: #bbbbbb;
    border-radius: 6px;
    image: url("{getPath("tick_white.png")}");
}}
QCheckBox::indicator:checked:hover,
QTreeView::indicator:checked:hover,
#stChk::indicator:checked:hover {{
    border: 1px solid #444444;
    background-color: rgb({colors[2]});
    border-radius: 6px;
    image: url("{getPath("tick_white.png")}");
}}
QRadioButton::indicator:checked {{
    height: 12px;
    width: 12px;
    border-radius: 10px;
    border: 4px solid rgb({colors[1]});
    background-color: black;
}}
QRadioButton::indicator:checked:hover {{
    height: 14px;
    width: 14px;
    border-radius: 10px;
    border: 3px solid rgb({colors[1]});
    background-color: black;
}}
QRadioButton::indicator:checked:disabled {{
    height: 12px;
    width: 12px;
    border-radius: 10px;
    border: 4px solid rgba(30, 30, 30, 25%);
    background-color: #202020;
}}
QRadioButton::indicator:unchecked {{
    height: 18px;
    width: 18px;
    border-radius: 10px;
    background-color: rgba(30, 30, 30, 25%);
    border: 1px solid #444444;
}}
QRadioButton::indicator:unchecked:hover {{
    height: 18px;
    width: 18px;
    border-radius: 10px;
    background-color: #2a2a2a;
    border: 1px solid #444444;
}}
#stCmbbx {{
    width: {(100)}px;
    background-color:rgba(81, 81, 81, 25%);
    border-radius: 8px;
    border: 1px solidrgba(86, 86, 86, 25%);
    height: {(25)}px;
    padding-left: 10px;
    border-top: 1px solidrgba(99, 99, 99, 25%);
}}
#stCmbbx:disabled {{
    width: {(100)}px;
    background-color: #303030;
    color: #bbbbbb;
    border-radius: 8px;
    border: 0.6px solid #262626;
    height: {(25)}px;
    padding-left: 10px;
}}
#stCmbbx:hover {{
    background-color:rgba(86, 86, 86, 25%);
    border-radius: 8px;
    border: 1px solidrgba(100, 100, 100, 25%);
    height: {(25)}px;
    padding-left: 10px;
    border-top: 1px solid rgba(107, 107, 107, 25%);
}}
#stCmbbx::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    padding: 5px;
    border-radius: 8px;
    border: none;
    width: 30px;
}}
#stCmbbx QAbstractItemView {{
    border: 1px solid rgba(36, 36, 36, 50%);
    padding: 4px;
    padding-right: 0;
    background-color: #303030;
    border-radius: 8px;
}}
#stCmbbx QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
#stCmbbx QAbstractItemView::item:selected{{
    background: rgba(255, 255, 255, 6%);
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
#DraggableVerticalSection {{
    background-color: rgba(255, 255, 255, 13%);
    border-radius: 2px;
    margin: 5px;
    margin-bottom: 0px;
}}
#DraggableVerticalSection:hover {{
    background-color: rgba(255, 255, 255, 17%);
}}
#CommandLineEdit {{
    border: 1px solid #282828;
    background-color: #191919;
    font-family: "Consolas";
    padding: 15px;
    border-radius: 8px;
    padding-right: 50px;
}}
#CommandLineEditCopyButton {{
    border-radius: 6px;
    background-color: rgba(0, 0, 0, 1%);
    border: 0px;
}}
#CommandLineEditCopyButton:hover {{
    background-color: rgba(255, 255, 255, 5%);
}}
#CommandLineEditCopyButton:pressed {{
    background-color: rgba(255, 255, 255, 10%);
}}
"""

menuDarkCSS = """
* {
    border-radius: 8px;
    background-color: transparent;
}
QWidget {
    background-color: transparent;
    border-radius: 8px;
    menu-scrollable: 1;
}
QMenu {
    padding: 2px;
    outline: 0px;
    color: white;
    font-family: "Segoe UI Variable Text";
    border-radius: 8px;
}
QMenu::separator {
    margin: 2px;
    height: 1px;
    background: rgb(60, 60, 60);
}
QMenu::icon {
    padding-left: 10px;
}
QMenu::item {
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
    margin: 2px;
}
QMenu::item:selected {
    background: rgba(255, 255, 255, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}
QMenu::item:disabled {
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    color: grey;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}
QMenu::item:selected:disabled {
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}
"""

lightCSS = f"""
* {{
    background-color: transparent;
    color: #000000;
    font-family: "Segoe UI Variable Text";
    outline: none;
}}
*::disabled {{
    color: gray;
}}
#InWindowNotification {{
    background-color: #dddddd;
    border-radius: 16px;
    height: 32px;
    border: 1px solid #bbbbbb;
}}
QInputDialog {{
    background-color: #f5f5f5;
}}
#micawin {{
    background-color: mainbg;
    color: red;
}}
QMenu {{
    border: 1px solid rgb(200, 200, 200);
    padding: 2px;
    outline: 0px;
    color: black;
    background: #eeeeee;
    border-radius: 8px;
}}
QMenu::separator {{
    margin: 2px;
    height: 1px;
    background: rgb(200, 200, 200);
}}
QMenu::icon{{
    padding-left: 10px;
}}
QMenu::item{{
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
    margin: 2px;
}}
QMenu::item:selected{{
    background: rgba(0, 0, 0, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QMenu::item:selected:disabled{{
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QMessageBox{{
    background-color: #f9f9f9;
}}
#greyLabel {{
    color: #404040;
}}
QPushButton,#FocusLabel {{
    width: 150px;
    background-color:rgba(255, 255, 255, 45%);
    border: 1px solid rgba(220, 220, 220, 55%);
    border-top: 1px solid rgba(220, 220, 220, 75%);
    border-radius: 6px;
    height: 25px;
    font-size: 9pt;
    margin: 0px;
    font-family: "Segoe UI Variable Display Semib";
}}
#FlatButton {{
    width: 150px;
    background-color: rgba(255, 255, 255, 0.1%);
    border-radius: 6px;
    border: 0px solid rgba(255, 255, 255, 1%);
    height: 25px;
    font-size: 9pt;
    border-top: 0px solid rgba(255, 255, 255, 1%);
}}
QToolTip {{
    background-color: #ffffff;
    border: 1px solid #f0f0f0;
    border-radius: 6px;
    padding: 4px;
    color: black;
}}
QPushButton:hover {{
    background-color: rgba(255, 255, 255, 90%);
    border: 1px solid rgba(220, 220, 220, 65%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
    border-radius: 6px;
    height: 30px;
}}
QPushButton:checked {{
    background-color: rgba(235, 235, 235, 100%);
    border: 1px solid rgba(220, 220, 220, 65%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
    border-radius: 6px;
    height: 30px;
}}
#AccentButton{{
    color: #000000;
    font-size: 9pt;
    background-color: rgb({colors[1]});
    border-color: rgba({colors[2]}, 50%);
    border-bottom-color: rgba({colors[2]}, 50%);
    font-family: "Segoe UI Variable Display Semib";
}}
#AccentButton:hover{{
    background-color: rgba({colors[2]}, 80%);
    border-color: rgba({colors[3]}, 50%);
    border-bottom-color: rgba({colors[3]}, 50%);
}}
#AccentButton:pressed{{
    color: #000000;
    background-color: rgba({colors[3]}, 80%);
    border-color: rgba({colors[4]}, 50%);
    border-bottom-color: rgba({colors[4]}, 50%);
}}
#AccentButton:disabled{{
    color: #000000;
    background-color: rgba(200,200,200, 80%);
    border-color: rgb(200, 200, 200);
    border-bottom-color: rgb(200, 200, 200);
}}
#Headerbutton {{
    width: 150px;
    background-color:rgba(255, 255, 255, 1%);
    border-radius: 6px;
    border: 0px solid transparent;
    height: 25px;
    font-size: 9pt;
    margin: 0px;
    font-family: "Segoe UI Variable Display";
    font-size: 9pt;
}}
#Headerbutton:hover {{
    background-color:rgba(0, 0, 0, 5%);
    border-radius: 8px;
    height: 30px;
}}
#Headerbutton:checked {{
    background-color:rgba(0, 0, 0, 10%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#package {{
    background-color:rgba(1, 1, 1, 3%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#PackageButton {{
    width: 150px;
    background-color:rgba(255, 255, 255, 60%);
    border-radius: 6px;
    border: 0px solid rgba(86, 86, 86, 25%);
    height: 25px;
    font-size: 9pt;
    margin: 0px;
    font-family: "Segoe UI Variable Display Semib";
}}
#PackageButton:hover {{
    background-color:rgba(255, 255, 255, 100%);
    border-radius: 6px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#buttonier {{
    border: 0px solid rgba(100, 100, 100, 25%);
    border-radius: 12px;
}}
QLineEdit {{
    background-color: rgba(255, 255, 255, 100%);
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    width: 300px;
    color: black;
    padding: 5px;
    border-radius: 6px;
    border: 1px solid rgba(86, 86, 86, 25%);
    border-bottom: 2px solid rgb({colors[3]});
}}
QLineEdit:disabled {{
    background-color: rgba(255, 255, 255, 25%);
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 1px solid rgba(255, 255, 255, 55%);
}}
QScrollBar {{
    background: transparent;
    margin: 4px;
    margin-left: 0;
    width: 16px;
    height: 20px;
    border: none;
    border-radius: 5px;
}}
QScrollBar:horizontal {{
    margin-bottom: 0;
    padding-bottom: 0;
    height: 12px;
}}
QScrollBar:vertical {{
    background: rgba(255, 255, 255, 0%);
    margin: 4px;
    width: 16px;
    border: none;
    border-radius: 5px;
}}
QScrollBar::handle:vertical {{
    margin: 3px;
    border-radius: 3px;
    min-height: 20px;
    background: rgba(90, 90, 90, 25%);
}}
QScrollBar::handle:vertical:hover {{
    margin: 3px;
    border-radius: 3px;
    background: rgba(90, 90, 90, 35%);
}}
QScrollBar::add-line:vertical {{
    height: 0;
    subcontrol-position: bottom;
    subcontrol-origin: margin;
}}
QScrollBar::sub-line:vertical {{
    height: 0;
    subcontrol-position: top;
    subcontrol-origin: margin;
}}
QScrollBar::up-arrow:vertical, QScrollBar::down-arrow:vertical {{
    background: none;
}}
QScrollBar::add-page:vertical, QScrollBar::sub-page:vertical {{
    background: none;
}}
QHeaderView,QAbstractItemView {{
    background-color: transparent;
    border-radius: 6px;
    border: none;
    padding: 0px;
    height: 35px;
    border: 0px solid rgba(222, 222, 222, 35%);
    margin-bottom: 5px;
    margin-left: 0px;
    margin-right: 0px;
}}
QHeaderView {{
    padding-right: 0px;
}}
    QHeaderView::section {{
    background-color: rgba(255, 255, 255, 55%);
    border-top: 1px solid rgba(222, 222, 222, 35%);
    border-bottom: 1px solid rgba(222, 222, 222, 35%);
    padding: 0px;
    height: 35px;
    border-radius: 0px;
    margin: 0px;
    padding-bottom: 4px;
    padding-top: 4px;
}}
QHeaderView::section:first {{
    border-left: 1px solid rgba(222, 222, 222, 35%);
    border-top-left-radius: 8px;
    border-bottom-left-radius: 8px;
}}
QHeaderView::section:last {{
    border-right: 1px solid rgba(222, 222, 222, 35%);
    border-top-right-radius: 8px;
    border-bottom-right-radius: 8px;
}}

QTreeWidget {{
    show-decoration-selected: 0;
    background-color: transparent;
    padding: 0px;
    outline: none;
    border-radius: 6px;
    border: 0px solid rgba(240, 240, 240, 55%);
}}
QTreeWidget::item {{
    margin-top: 3px;
    margin-bottom: 3px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    height: 25px;
    background-color: rgba(255, 255, 255, 55%);
    border-top: 1px solid rgba(222, 222, 222, 35%);
    border-bottom: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget#FlatTreeWidget::item,
QTreeWidget#FlatTreeWidget::item:first {{
    margin: 0px;
    padding: 0px;
    background-color: transparent;
    height: 30px;
    border: 0px;
    border-radius: 0px;
    border-bottom: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget#FlatTreeWidget::item:last {{
    padding-right: 10px;
}}
#IslandWidget {{
    padding: 5px;
    margin: 5px;
    margin-top: 0px;
    margin-bottom: 0px;
    background-color: rgba(255, 255, 255, 40%);
    border: 1px solid rgba(222, 222, 222, 35%);
    border-radius: 8px;
}}
QTreeWidget::item:selected {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: rgba(222, 222, 222, 35%);
    height: 25px;
    border-bottom: 1px solid rgba(222, 222, 222, 35%);
    border-top: 1px solid rgba(222, 222, 222, 35%);
    color: rgb({colors[3]});
}}
QTreeWidget::branch {{
    background-color: transparent;
}}
QTreeWidget::item:hover {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: rgba(245, 245, 245, 70%);
    height: 25px;
    border-bottom: 1px solid rgba(222, 222, 222, 35%);
    border-top: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget::item:first {{
    border-top-left-radius: 8px;
    border-bottom-left-radius: 8px;
    border-left: 1px solid rgba(220, 220, 220, 35%);
}}
QTreeWidget::item:last {{
    border-top-right-radius: 8px;
    border-bottom-right-radius: 8px;
    border-right: 1px solid rgba(220, 220, 220, 35%);
}}
QTreeWidget::item:first:selected {{
    border-left: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget::item:last:selected {{
    border-right: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget::item:first:hover {{
    border-left: 1px solid rgba(222, 222, 222, 35%);
}}
QTreeWidget::item:last:hover {{
    border-right: 1px solid rgba(222, 222, 222, 35%);
}}
QProgressBar {{
    border-radius: 2px;
    height: 4px;
    border: 0px;
}}
QProgressBar::chunk {{
    background-color: rgb({colors[3]});
    border-radius: 2px;
}}
QCheckBox::indicator{{
    height: 16px;
    width: 16px;
}}
QTreeView::indicator{{
    height:18px;
    width: 18px;
    margin: 0px;
    margin-left: 4px;
    margin-top: 2px;
}}
QTreeView::indicator:unchecked,QCheckBox::indicator:unchecked {{
    background-color: rgba(255, 255, 255, 25%);
    border: 1px solid rgba(0, 0, 0, 10%);
    border-radius: 4px;
}}
QTreeView::indicator:disabled,QCheckBox::indicator:disabled {{
    background-color: rgba(240, 240, 240, 0%);
    color: #444444;
    border: 1px solid rgba(0, 0, 0, 5%);
    border-radius: 4px;
}}
QTreeView::indicator:unchecked:hover,QCheckBox::indicator:unchecked:hover {{
    background-color: rgba(0, 0, 0, 5%);
    border: 1px solid rgba(0, 0, 0, 20%);
    border-radius: 4px;
}}
QTreeView::indicator:checked,QCheckBox::indicator:checked {{
    border: 1px solid rgb({colors[3]});
    background-color: rgb({colors[2]});
    border-radius: 4px;
    image: url("{getMedia("tick")}");
}}
QTreeView::indicator:checked:disabled,QCheckBox::indicator:checked:disabled {{
    border: 1px solid #444444;
    background-color: #303030;
    color: #444444;
    border-radius: 4px;
}}
QTreeView::indicator:checked:hover,QCheckBox::indicator:checked:hover {{
    border: 1px solid rgb({colors[3]});
    background-color: rgb({colors[3]});
    border-radius: 4px;
}}
QComboBox::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    background-color: none;
    padding: 5px;
    border-radius: 6px;
    border: none;
    color: white;
    width: 30px;
}}
QComboBox::down-arrow {{
    image: url("{getMedia("drop-down")}");
    height: 8px;
    width: 8px;
}}
QComboBox::down-arrow:disabled {{
    image: url("{getMedia("drop-down")}");
    height: 2px;
    width: 2px;
}}
QComboBox QAbstractItemView {{
    padding: 0px;
    margin: 0px;
    outline: 0px;
    background-color: #ffffff;
    border-radius: 8px;
    color: black;
}}
QComboBox#transparent QAbstractItemView {{
    border: 1px solid transparent;
    background-color: transparent;
    padding: 4px;
}}
QComboBox QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:hover{{
    background: rgba(0, 0, 0, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-left: 10px;
    color: black;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(0, 0, 0, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-left: 10px;
    color: black;
    border-radius: 4px;
}}
QComboBox {{
    width: 150px;
    background-color:rgba(255, 255, 255, 45%);
    border: 1px solid rgba(220, 220, 220, 55%);
    border-top: 1px solid rgba(220, 220, 220, 75%);
    border-radius: 6px;
    height: 30px;
    padding-left: 10px;
    font-size: 9pt;
    margin: 0px;
}}
QComboBox:hover {{
    background-color: rgba(255, 255, 255, 90%);
    border: 1px solid rgba(220, 220, 220, 65%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
    border-radius: 6px;
    height: 30px;
}}
QPlainTextEdit{{
    border: 1px solid #eeeeee;
    border-radius: 6px;
    padding: 6px;
    color: black;
    background-color: #ffffff;
    font-family: "Consolas";
}}
QLabel{{
    selection-background-color: rgb({colors[3]});
}}
QToolButton {{
    background-color:rgba(255, 255, 255, 1%);
    border-radius: 4px;
    border: 0px solid transparent;
    margin: 5px;
    margin-right: 0px;
    font-size: 9pt;
    font-family: "Segoe UI Variable Text";
    font-size: 9pt;
    padding: 4px;
}}
QToolButton:hover {{
    background-color:rgba(0, 0, 0, 6%);
    border-radius: 4px;
    margin: 5px;
    margin-right: 0px;
    padding: 4px;
}}
QToolBar:separator {{
    width: 1px;
    margin: 5px;
    margin-right: 0px;
    background-color: rgba(0, 0, 0, 10%);
}}
#greyishLabel {{
    color: #888888;
}}
#subtitleLabel{{
    background-color: rgba(255, 255, 255, 60%);
    margin: 0px;
    padding-left: {(20)}px;
    padding-top: {(15)}px;
    padding-bottom: {(15)}px;
    border-radius: 8px;
    border: 1 solid rgba(222, 222, 222, 50%);
    font-size: 13pt;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
}}
#subtitleLabelHover {{
    background-color: rgba(255, 255, 255, 1%);
    margin: 0px;
    border-radius: 8px;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
    border: 1px solid transparent;
}}
#subtitleLabelHover:hover{{
    background-color: rgba(0, 0, 0, 3%);
    margin: 0px;
    padding-left: {(20)}px;
    padding-top: {(15)}px;
    padding-bottom: {(15)}px;
    border: 1px solid rgba(196, 196, 196, 25%);
    font-size: 13pt;
    border-top-left-radius: 8px;
    border-top-right-radius: 8px;
}}
#StLbl{{
    padding: 0;
    background-color: rgba(255, 255, 255, 10%);
    margin: 0;
    border:none;
    font-size: {(11)}px;
}}
#stBtn{{
    background-color: rgba(255, 255, 255, 5%);
    margin: 10px;
    margin-bottom: 0;
    margin-top: 0;
    border: 1px solid rgba(196, 196, 196, 25%);
    border-bottom: 0;
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
}}
#lastWidget{{
    border-bottom-left-radius: 4px;
    border-bottom-right-radius: 4px;
    border-bottom: 1px;
}}
#stChkBg{{
    padding: 15px;
    padding-left: 15px;
    background-color: rgba(255, 255, 255, 10%);
    margin: 10px;
    margin-bottom: 0;
    margin-top: 0;
    border: 1px solid rgba(196, 196, 196, 25%);
    border-bottom: 0;
}}
QTreeView::indicator,
QCheckBox::indicator,
#stChk::indicator  {{
    height: 20px;
    width: 20px;
}}
QTreeView::indicator:unchecked,
QCheckBox::indicator:unchecked,
#stChk::indicator:unchecked {{
    background-color: rgba(255, 255, 255, 10%);
    border: 1px solid rgba(136, 136, 136, 25%);
    border-radius: 6px;
}}
QTreeView::indicator:disabled,
QCheckBox::indicator:disabled,
#stChk::indicator:disabled {{
    background-color: #eeeeee;
    color: rgba(136, 136, 136, 25%);
    border: 1px solid rgba(136, 136, 136, 25%);
    border-radius: 6px;
}}
QTreeView::indicator:unchecked:hover,
QCheckBox::indicator:unchecked:hover,
#stChk::indicator:unchecked:hover {{
    background-color: #eeeeee;
    border: 1px solid rgba(136, 136, 136, 25%);
    border-radius: 6px;
}}
QTreeView::indicator:checked,
QCheckBox::indicator:checked,
#stChk::indicator:checked {{
    border: 0 solid rgba(136, 136, 136, 25%);
    background-color: rgb({colors[4]});
    border-radius: 5px;
    image: url("{getPath("tick_black.png")}");
}}
QCheckBox::indicator:checked:hover,
QTreeView::indicator:checked:hover,
#stChk::indicator:checked:hover {{
    border: 0 solid rgba(136, 136, 136, 25%);
    background-color: rgb({colors[3]});
    border-radius: 5px;
    image: url("{getPath("tick_black.png")}");
}}
QTreeView::indicator:checked:disabled,
QCheckBox::indicator:checked:disabled,
#stChk::indicator:checked:disabled {{
    border: 1px solid rgba(136, 136, 136, 25%);
    background-color: #eeeeee;
    color: rgba(136, 136, 136, 25%);
    border-radius: 6px;
    image: url("{getPath("tick_black.png")}");
}}
QRadioButton::indicator:checked {{
    height: 12px;
    width: 12px;
    border-radius: 10px;
    border: 4px solid rgb({colors[3]});
    background-color: white;
}}
QRadioButton::indicator:checked:hover {{
    height: 14px;
    width: 14px;
    border-radius: 10px;
    border: 3px solid rgb({colors[3]});
    background-color: white;
}}
QRadioButton::indicator:checked:disabled {{
    height: 12px;
    width: 12px;
    border-radius: 10px;
    background-color: #f3f3f3;
    border: 1px solid #c2c2c2;
}}
QRadioButton::indicator:unchecked {{
    height: 18px;
    width: 18px;
    border-radius: 10px;
    background-color: #f3f3f3;
    border: 1px solid #a2a2a2;
}}
QRadioButton::indicator:unchecked:hover {{
    height: 18px;
    width: 18px;
    border-radius: 10px;
    background-color: #ffffff;
    border: 1px solid #a2a2a2;
}}
#stCmbbx {{
    width: {(100)}px;
    background-color: rgba(255, 255, 255, 10%);
    border-radius: 8px;
    border: 1px solid rgba(196, 196, 196, 25%);
    height: {(25)}px;
    padding-left: 10px;
    border-bottom: 1px solid rgba(204, 204, 204, 25%);
}}
#stCmbbx:disabled {{
    width: {(100)}px;
    background-color: #eeeeee;
    border-radius: 8px;
    border: 1px solid rgba(196, 196, 196, 25%);
    height: {(25)}px;
    padding-left: 10px;
    border-top: 1px solid rgba(196, 196, 196, 25%);
}}
#stCmbbx:hover {{
    background-color: rgba(238, 238, 238, 25%);
    border-radius: 8px;
    border: 1px solid rgba(196, 196, 196, 25%);
    height: {(25)}px;
    padding-left: 10px;
    border-bottom: 1px solid rgba(204, 204, 204, 25%);
}}
#stCmbbx::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    padding: 5px;
    border-radius: 8px;
    border: none;
    width: 30px;
}}
#stCmbbx QAbstractItemView {{
    border: 1px solid rgba(196, 196, 196, 25%);
    padding: 4px;
    outline: 0;
    background-color: rgba(255, 255, 255, 10%);
    border-radius: 8px;
}}
#stCmbbx QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
#stCmbbx QAbstractItemView::item:selected{{
    background: rgba(0, 0, 0, 6%);
    height: 30px;
    outline: none;
    color: black;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
#DraggableVerticalSection {{
    background-color: rgba(0, 0, 0, 17%);
    border-radius: 2px;
    margin: 5px;
    margin-bottom: 0px;
}}
#DraggableVerticalSection:hover {{
    background-color: rgba(0, 0, 0, 25%);
}}
#CommandLineEdit {{
    border: 1px solid #f5f5f5;
    background-color: #ffffff;
    font-family: "Consolas";
    padding: 15px;
    border-radius: 8px;
    padding-right: 50px;
}}
#CommandLineEditCopyButton {{
    border-radius: 6px;
    background-color: rgba(255, 255, 255, 100%);
    border: 0px;
}}
#CommandLineEditCopyButton:hover {{
    background-color: rgba(240, 240, 240, 100%);
}}
#CommandLineEditCopyButton:pressed {{
    background-color: rgba(225, 225, 225, 100%);
}}
"""

menuLightCSS = """
QWidget {
    background-color: transparent;
    menu-scrollable: 1;
    border-radius: 8px;
}
QMenu {
    font-family: "Segoe UI Variable Text";
    border: 1px solid rgb(200, 200, 200);
    padding: 2px;
    outline: 0px;
    color: black;
    icon-size: 32px;
    background: rgba(220, 220, 220, 1%)/*#262626*/;
    border-radius: 8px;
}
QMenu::separator {
    margin: -2px;
    margin-top: 2px;
    margin-bottom: 2px;
    height: 1px;
    background-color: rgba(0, 0, 0, 20%);
}
QMenu::icon {
    padding-left: 10px;
}
QMenu::item {
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 20px;
    padding-left: 0px;
    border-radius: 4px;
    margin: 2px;
}
QMenu::item:selected {
    background: rgba(0, 0, 0, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-right: 20px;
    padding-left: 0px;
    border-radius: 4px;
}
QMenu::item:disabled {
    background: transparent;
    height: 30px;
    outline: none;
    color: grey;
    border: none;
    padding-right: 20px;
    padding-left: 0px;
    border-radius: 4px;
}
QMenu::item:selected:disabled {
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    padding-right: 20px;
    padding-left: 0px;
    border-radius: 4px;
}
"""
