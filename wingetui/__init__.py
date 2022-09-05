try:
    _globals = globals
    import sys, os, win32mica, glob, subprocess, socket, hashlib, time
    from threading import Thread
    from urllib.request import urlopen
    from PySide6.QtGui import *
    from PySide6.QtCore import *
    from PySide6.QtWidgets import *
    import wingetHelpers, scoopHelpers
    from mainWindow import *
    from tools import *

    import globals
    from blurwindow import GlobalBlur, ExtendFrameIntoClientArea

    debugging = True

    if hasattr(Qt, 'AA_EnableHighDpiScaling'):
        QApplication.setAttribute(Qt.AA_EnableHighDpiScaling, True)
    if hasattr(Qt, 'AA_UseHighDpiPixmaps'):
        QApplication.setAttribute(Qt.AA_UseHighDpiPixmaps, True)

    class MainApplication(QApplication):
        kill = Signal()
        callInMain = Signal(object)
        setLoadBarValue = Signal(str)
        startAnim = Signal(QVariantAnimation)
        changeBarOrientation = Signal()
        updatesMenu: QMenu = None
        installedMenu: QMenu = None
        running = True
        
        def __init__(self):
            try:
                super().__init__(sys.argv)
                self.isDaemon: bool = "--daemon" in sys.argv
                self.popup = DraggableWindow()
                self.popup.setFixedSize(QSize(600, 400))
                self.popup.setWindowFlag(Qt.FramelessWindowHint)
                self.popup.setWindowFlag(Qt.WindowStaysOnTopHint)
                self.popup.setLayout(QVBoxLayout())
                self.popup.layout().addStretch()
                titlewidget = QHBoxLayout()
                titlewidget.addStretch()
                icon = QLabel()
                icon.setPixmap(QPixmap(realpath+"/resources/icon.png"))
                text = QLabel("WingetUI")
                text.setStyleSheet(f"font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"; color: {'white' if isDark() else 'black'};font-size: 60px;")
                titlewidget.addWidget(icon)
                titlewidget.addWidget(text)
                titlewidget.addStretch()
                self.popup.layout().addLayout(titlewidget)
                self.popup.layout().addStretch()
                self.loadingText = QLabel("Loading WingetUI...")
                self.loadingText.setStyleSheet(f"font-family: \"Segoe UI Variable Display semib\"; color: {'white' if isDark() else 'black'};font-size: 12px;")
                self.popup.layout().addWidget(self.loadingText)
                ApplyMenuBlur(self.popup.winId().__int__(), self.popup)
                
                self.loadingProgressBar = QProgressBar(self.popup)
                self.loadingProgressBar.setStyleSheet(f"""QProgressBar {{border-radius: 2px;height: 4px;border: 0px;}}QProgressBar::chunk {{background-color: rgb({colors[2 if isDark() else 3]});border-radius: 2px;}}""")
                self.loadingProgressBar.setRange(0, 1000)
                self.loadingProgressBar.setValue(0)
                self.loadingProgressBar.setGeometry(QRect(0, 396, 600, 4))
                self.loadingProgressBar.setFixedHeight(4)
                self.loadingProgressBar.setTextVisible(False)
                self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
                self.startAnim.connect(lambda anim: anim.start())
                self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
            
                self.leftSlow = QVariantAnimation()
                self.leftSlow.setStartValue(0)
                self.leftSlow.setEndValue(1000)
                self.leftSlow.setDuration(700)
                self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
                self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
                
                self.rightSlow = QVariantAnimation()
                self.rightSlow.setStartValue(1000)
                self.rightSlow.setEndValue(0)
                self.rightSlow.setDuration(700)
                self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
                self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
                
                self.leftFast = QVariantAnimation()
                self.leftFast.setStartValue(0)
                self.leftFast.setEndValue(1000)
                self.leftFast.setDuration(300)
                self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
                self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

                self.rightFast = QVariantAnimation()
                self.rightFast.setStartValue(1000)
                self.rightFast.setEndValue(0)
                self.rightFast.setDuration(300)
                self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
                self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
                
                if not self.isDaemon:
                    self.leftSlow.start()
                    self.popup.show()

                print("[        ] Starting main application...")
                os.chdir(os.path.expanduser("~"))
                self.kill.connect(self.quit)
                self.callInMain.connect(lambda f: f())
                Thread(target=self.loadStuffThread).start()
                self.loadingText.setText("Checking for other running instances...")
            except Exception as e:
                raise e

        def loadStuffThread(self):
            try:
                self.loadStatus = 0 # There are 6 items (preparation threads)
                
                # Preparation threads
                Thread(target=self.checkForRunningInstances, daemon=True).start()
                if not getSettings("DisableWinget"):
                    Thread(target=self.detectWinget, daemon=True).start()
                else:
                    self.loadStatus += 2
                    globals.componentStatus["wingetFound"] = False
                    globals.componentStatus["wingetVersion"] = "Winget is disabled"
                if not getSettings("DisableScoop"):
                    Thread(target=self.detectScoop, daemon=True).start()
                else:
                    self.loadStatus += 2
                    globals.componentStatus["scoopFound"] = False
                    globals.componentStatus["scoopVersion"] = "Scoop is disabled"
                Thread(target=self.detectSudo, daemon=True).start()

                # Daemon threads
                Thread(target=self.instanceThread, daemon=True).start()
                Thread(target=self.updateIfPossible, daemon=True).start()

                while self.loadStatus < 6:
                    time.sleep(0.01)
            except Exception as e:
                print(e)
            finally:
                self.callInMain.emit(lambda: self.loadingText.setText(f"Loading UI components..."))
                self.callInMain.emit(lambda: self.loadingText.repaint())
                self.callInMain.emit(self.loadMainUI)
                print(globals.componentStatus)

        def checkForRunningInstances(self):
                print("Scanning for instances...")
                self.nowTime = time.time()
                self.lockFileName = f"WingetUI_{self.nowTime}"
                setSettings(self.lockFileName, True)
                try:
                    timestamps = [float(file.replace(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_"), "")) for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_*"))] # get a list with the timestamps
                    validTimestamps = [timestamp for timestamp in timestamps if timestamp < self.nowTime]
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Evaluating found instace(s)..."))
                    print("Found lock file(s), reactivating...")
                    for tst in validTimestamps:
                        setSettings("RaiseWindow_"+str(tst), True)
                    if validTimestamps != [] and timestamps != [self.nowTime]:
                        for i in range(16):
                            time.sleep(0.1)
                            self.callInMain.emit(lambda: self.loadingText.setText(f"Sent handshake. Waiting for instance listener's answer... ({int(i/15*100)}%)"))
                            for tst in validTimestamps:
                                if not getSettings("RaiseWindow_"+str(tst), cache = False):
                                    print(f"Instance {tst} responded, quitting...")
                                    self.callInMain.emit(lambda: self.loadingText.setText(f"Instance {tst} responded, quitting..."))
                                    setSettings(self.lockFileName, False)
                                    self.kill.emit()
                                    sys.exit(0)
                        self.callInMain.emit(lambda: self.loadingText.setText(f"Starting daemons..."))
                        print("Reactivation signal ignored: RaiseWindow_"+str(validTimestamps))
                        for tst in validTimestamps:
                            setSettings("RaiseWindow_"+str(tst), False)
                            setSettings("WingetUI_"+str(tst), False)
                except Exception as e:
                    print(e)
                self.loadStatus += 1

        def detectWinget(self):
            try:
                self.callInMain.emit(lambda: self.loadingText.setText(f"Locating winget..."))
                o = subprocess.run(f"{wingetHelpers.winget} -v", shell=True, stdout=subprocess.PIPE)
                globals.componentStatus["wingetFound"] = o.returncode == 0
                globals.componentStatus["wingetVersion"] = o.stdout.decode('utf-8').replace("\n", "")
                self.callInMain.emit(lambda: self.loadingText.setText(f"Winget found: {globals.componentStatus['wingetFound']}"))
            except Exception as e:
                print(e)
            self.loadStatus += 1
            print("updating winget")
            try:
                if not getSettings("DisableUpdateIndexes"):
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Updating winget sources..."))
                    o = subprocess.run(f"{wingetHelpers.winget} upgrade {' '.join(wingetHelpers.common_params)}", shell=True, stdout=subprocess.PIPE)
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Updated winget sources"))
            except Exception as e:
                print(e)
            self.loadStatus += 1

        def detectScoop(self):
            try:
                self.callInMain.emit(lambda: self.loadingText.setText(f"Locating scoop..."))
                o = subprocess.run(f"powershell -Command scoop -v", shell=True, stdout=subprocess.PIPE)
                globals.componentStatus["scoopFound"] = o.returncode == 0
                globals.componentStatus["scoopVersion"] = o.stdout.decode('utf-8').split("\n")[1]
                self.callInMain.emit(lambda: self.loadingText.setText(f"Scoop found: {globals.componentStatus['scoopFound']}"))
            except Exception as e:
                print(e)
            self.loadStatus += 1
            try:
                if not getSettings("DisableUpdateIndexes"):
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Clearing scoop cache..."))
                    p = subprocess.Popen(f"powershell -Command scoop cache rm *", shell=True, stdout=subprocess.PIPE)
                    p.wait()
            except Exception as e:
                print(e)
            try:
                if not getSettings("DisableUpdateIndexes"):
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Updating scoop sources..."))
                    o = subprocess.run(f"powershell -Command scoop update", shell=True, stdout=subprocess.PIPE)
                    self.callInMain.emit(lambda: self.loadingText.setText(f"Updated scoop sources"))
            except Exception as e:
                print(e)
            self.loadStatus += 1

        def detectSudo(self):
            global sudoLocation
            try:
                self.callInMain.emit(lambda: self.loadingText.setText(f"Locating sudo..."))
                o = subprocess.run(f"{sudoPath} -v", shell=True, stdout=subprocess.PIPE)
                globals.componentStatus["sudoFound"] = o.returncode == 0
                globals.componentStatus["sudoVersion"] = o.stdout.decode('utf-8').split("\n")[0]
                self.callInMain.emit(lambda: self.loadingText.setText(f"Sudo found: {globals.componentStatus['sudoFound']}"))
            except Exception as e:
                print(e)
            self.loadStatus += 1

        def loadMainUI(self):
            print("Reached main ui load milestone")
            try:
                globals.trayIcon = QSystemTrayIcon()
                self.trayIcon = globals.trayIcon
                globals.app = self
                self.trayIcon.setIcon(QIcon(realpath+"/resources/icon.png"))
                self.trayIcon.setToolTip("WingetUI")
                self.trayIcon.setVisible(True)

                menu = QMenu("WingetUI")
                globals.trayMenu = menu
                infoAction = QAction(f"WingetUI v{version}",menu)
                infoAction.setIcon(QIcon(getMedia("info")))
                infoAction.setEnabled(False)
                menu.addAction(infoAction)
                
                showAction = QAction("Show WingetUI",menu)
                showAction.setIcon(QIcon(getMedia("menu_show")))
                menu.addAction(showAction)
                self.trayIcon.setContextMenu(menu)
                menu.addSeparator()
                
                dAction = QAction("Available updates",menu)
                dAction.setIcon(QIcon(getMedia("menu_updates")))
                dAction.setEnabled(False)
                menu.addAction(dAction)
                
                self.updatesMenu = menu.addMenu("0 updates found")
                self.updatesMenu.menuAction().setIcon(QIcon(getMedia("list")))
                self.updatesMenu.setParent(menu)
                globals.trayMenuUpdatesList = self.updatesMenu
                menu.addMenu(self.updatesMenu)
                
                globals.updatesHeader = QAction("App Name  \tInstalled Version \t → \t New version", menu)
                globals.updatesHeader.setEnabled(False)
                globals.updatesHeader.setIcon(QIcon(getMedia("version")))
                self.updatesMenu.addAction(globals.updatesHeader)
                
                uaAction = QAction("Update all", menu)
                uaAction.setIcon(QIcon(getMedia("menu_installall")))
                menu.addAction(uaAction)
                menu.addSeparator()
                
                dAction = QAction("Installed packages",menu)
                dAction.setIcon(QIcon(getMedia("menu_uninstall")))
                dAction.setEnabled(False)
                menu.addAction(dAction)
                
                self.installedMenu = menu.addMenu("0 packages found")
                self.installedMenu.menuAction().setIcon(QIcon(getMedia("list")))
                self.installedMenu.setParent(menu)
                globals.trayMenuInstalledList = self.installedMenu
                menu.addMenu(self.installedMenu)
                menu.addSeparator()
                
                globals.installedHeader = QAction("App Name\tInstalled Version", menu)
                globals.installedHeader.setIcon(QIcon(getMedia("version")))
                globals.installedHeader.setEnabled(False)
                self.installedMenu.addAction(globals.installedHeader)

                quitAction = QAction(menu)
                quitAction.setIcon(QIcon(getMedia("menu_close")))
                quitAction.setText("Quit")
                quitAction.triggered.connect(lambda: (self.quit(), sys.exit(0)))
                menu.addAction(quitAction)
                
                def showWindow():
                    # This function will be defined when the mainWindow gets defined
                    pass
                
                self.trayIcon.activated.connect(lambda r: (applyMenuStyle(),menu.exec(QCursor.pos())) if r == QSystemTrayIcon.Context else showWindow())
                self.trayIcon.messageClicked.connect(lambda: showWindow())
                self.installedMenu.aboutToShow.connect(lambda: applyMenuStyle())
                self.updatesMenu.aboutToShow.connect(lambda: applyMenuStyle())

                def applyMenuStyle():
                    for mn in (menu, self.updatesMenu, self.installedMenu):
                        if isDark():
                            GlobalBlur(mn.winId().__int__(), Acrylic=True, hexColor="#21212140", Dark=True)
                            ExtendFrameIntoClientArea(mn.winId().__int__())
                            mn.setStyleSheet(menuDarkCSS)
                        else:
                            GlobalBlur(mn.winId().__int__(), Acrylic=True, hexColor="#eeeeee40", Dark=False)
                            ExtendFrameIntoClientArea(mn.winId().__int__())
                            mn.setStyleSheet(menuLightCSS)

                self.window = RootWindow()
                showAction.triggered.connect(self.window.showWindow)
                uaAction.triggered.connect(self.window.updates.upgradeAllButton.click)
                showWindow = self.window.showWindow

                if(not isDark()):
                    self.setStyle("windowsvista")
                    r = win32mica.ApplyMica(self.window.winId(), win32mica.MICAMODE.LIGHT)
                    print(r)
                    if r != 0x32:
                        pass
                    self.window.setStyleSheet(lightCSS.replace("mainbg", "transparent" if r == 0x0 else "#ffffff")) 
                else:
                    self.setStyle("windowsvista")
                    r = win32mica.ApplyMica(self.window.winId(), win32mica.MICAMODE.DARK)
                    if r != 0x32:
                        pass
                    self.window.setStyleSheet(darkCSS.replace("mainbg", "transparent" if r == 0x0 else "#202020"))
                self.loadingText.setText(f"Latest details...")
                if not self.isDaemon:
                    self.window.show()
            except Exception as e:
                import webbrowser, traceback, platform
                try:
                    from tools import version as appversion
                except Exception as e:
                    appversion = "Unknown"
                os_info = f"" + \
                    f"                        OS: {platform.system()}\n"+\
                    f"                   Version: {platform.win32_ver()}\n"+\
                    f"           OS Architecture: {platform.machine()}\n"+\
                    f"          APP Architecture: {platform.architecture()[0]}\n"+\
                    f"               APP Version: {appversion}\n"+\
                    f"                   Program: WingetUI\n"+\
                    f"           Program section: UI Loading"+\
                    "\n\n-----------------------------------------------------------------------------------------"
                traceback_info = "Traceback (most recent call last):\n"
                try:
                    for line in traceback.extract_tb(e.__traceback__).format():
                        traceback_info += line
                    traceback_info += f"\n{type(e).__name__}: {str(e)}"
                except:
                    traceback_info += "\nUnable to get traceback"
                traceback_info += str(type(e))
                traceback_info += ": "
                traceback_info += str(e)
                webbrowser.open(("https://www.somepythonthings.tk/error-report/?appName=WingetUI&errorBody="+os_info.replace('\n', '{l}').replace(' ', '{s}')+"{l}{l}{l}{l}WingetUI Log:{l}"+str("\n\n\n\n"+traceback_info).replace('\n', '{l}').replace(' ', '{s}')).replace("#", "|=|"))
                print(traceback_info)
            self.popup.hide()


        def instanceThread(self):
            while True:
                try:
                    for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "RaiseWindow_*")):
                        if getSettings("RaiseWindow_"+str(self.nowTime), cache = False):
                            print("[   OK   ] Found reactivation lock file...")
                            setSettings("RaiseWindow_"+str(self.nowTime), False)
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

        def updateIfPossible(self):
            if not getSettings("DisableAutoUpdateWingetUI"):
                print("🔵 Starting update check")
                integrityPass = False
                dmname = socket.gethostbyname_ex("versions.somepythonthings.tk")[0]
                if(dmname == dmname): # Check provider IP to prevent exploits
                    integrityPass = True
                try:
                    response = urlopen("https://versions.somepythonthings.tk/versions/wingetui.ver")
                except Exception as e:
                    print(e)
                    response = urlopen("http://www.somepythonthings.tk/versions/wingetui.ver")
                    integrityPass = True
                print("🔵 Version URL:", response.url)
                response = response.read().decode("utf8")
                new_version_number = response.split("///")[0]
                provided_hash = response.split("///")[1].replace("\n", "").lower()
                if float(new_version_number) > version:
                    print("🟢 Updates found!")
                    updatesAvailable = True
                    if(integrityPass):
                        url = "https://github.com/martinet101/WingetUI/releases/latest/download/WingetUI.Installer.exe"
                        filedata = urlopen(url)
                        datatowrite = filedata.read()
                        filename = ""
                        with open(os.path.join(os.path.expanduser("~"), "WingetUI-Updater.exe"), 'wb') as f:
                            f.write(datatowrite)
                            filename = f.name
                        if(hashlib.sha256(datatowrite).hexdigest().lower() == provided_hash):
                            print("🔵 Hash: ", provided_hash)
                            print("🟢 Hash ok, starting update")
                            while self.running:
                                time.sleep(0.1)
                            if not getSettings("DisableAutoUpdateWingetUI"):
                                subprocess.run('start /B "" "{0}" /silent'.format(filename), shell=True)
                            else:
                                print("🟠 Hash not ok")
                                print("🟠 File hash: ", hashlib.sha256(datatowrite).hexdigest())
                                print("🟠 Provided hash: ", provided_hash)
                        else:
                            print("🟠 Can't verify update server authenticity, aborting")
                            print("🟠 Provided DmName:", dmname)
                            print("🟠 Expected DmNane: 769432b9-3560-4f94-8f90-01c95844d994.id.repl.co")
                    else:
                        print("🟢 Updates not found")

    colors = getColors()

    darkCSS = f"""
    * {{
        background-color: transparent;
        color: #eeeeee;
        font-family: "Segoe UI Variable Display semib";
        outline: none;
    }}
    #micawin,QInputDialog {{
        background-color: mainbg;
        color: red;
    }}
    QMenu {{
        border: 1px solid rgb(60, 60, 60);
        padding: 2px;
        outline: 0px;
        color: white;
        background: #262626;
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
    QMenu::item:selected{{
        background: rgba(255, 255, 255, 10%);
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
    QComboBox QAbstractItemView {{
        border: 1px solid rgba(36, 36, 36, 50%);
        padding: 4px;
        outline: 0px;
        padding-right: 0px;
        background-color: #303030;
        border-radius: 8px;
    }}
    QComboBox QAbstractItemView::item{{
        height: 10px;
        border: none;
        padding-left: 10px;
        border-radius: 4px;
    }}
    QComboBox QAbstractItemView::item:selected{{
        background: rgba(255, 255, 255, 6%);
        height: 10px;
        outline: none;
        border: none;
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
        font-size: 10pt;
        border-top: 1px solid rgba(99, 99, 99, 25%);
        margin: 0px;
    }}
    #FlatButton {{
        width: 150px;
        background-color: rgba(255, 255, 255, 1%);
        border-radius: 6px;
        border: 0px solid rgba(255, 255, 255, 1%);
        height: 25px;
        font-size: 10pt;
        border-top: 0px solid rgba(255, 255, 255, 1%);
    }}
    QPushButton:hover {{
        background-color:rgba(86, 86, 86, 25%);
        border-radius: 6px;
        border: 1px solid rgba(100, 100, 100, 25%);
        height: 30px;
        border-top: 1px solid rgba(107, 107, 107, 25%);
    }}
    #Headerbutton {{
        width: 150px;
        background-color:rgba(0, 0, 0, 1%);
        border-radius: 6px;
        border: 0px solid transparent;
        height: 25px;
        font-size: 10pt;
        margin: 0px;
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
    #buttonier {{
        border: 0px solid rgba(100, 100, 100, 25%);
        border-radius: 12px;
    }}
    #AccentButton{{
        color: #202020;
        font-size: 8pt;
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
        font-family: "Segoe UI Variable Display";
        font-size: 9pt;
        width: 300px;
        padding: 5px;
        border-radius: 6px;
        border: 0.6px solid rgba(86, 86, 86, 25%);
        border-bottom: 2px solid rgb({colors[4]});
    }}
    QLineEdit:disabled {{
        background-color: rgba(81, 81, 81, 25%);
        font-family: "Segoe UI Variable Display";
        font-size: 9pt;
        width: 300px;
        padding: 5px;
        border-radius: 6px;
        border: 0.6px solid rgba(86, 86, 86, 25%);
    }}

    QScrollBar:vertical {{
        background: transparent;
        border: 1px solid #1f1f1f;
        margin: 3px;
        width: 18px;
        border: none;
        border-radius: 4px;
    }}
    QScrollBar::handle {{
        margin: 3px;
        min-height: 20px;
        min-width: 20px;
        border-radius: 3px;
        background: #505050;
    }}
    QScrollBar::handle:hover {{
        margin: 3px;
        border-radius: 3px;
        background: #808080;
    }}
    QScrollBar::add-line {{
        height: 0;
        subcontrol-position: bottom;
        subcontrol-origin: margin;
    }}
    QScrollBar::sub-line {{
        height: 0;
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
        background-color: #55303030;
        border-radius: 6px;
        border: none;
        padding: 1px;
        height: 25px;
        border: 1px solid #1f1f1f;
        margin-bottom: 5px;
        margin-left: 10px;
        margin-right: 10px;
    }}
    QHeaderView::section {{
        background-color: transparent;
        border-radius: 6px;
        padding: 4px;
        height: 25px;
        margin: 1px;
    }}
    QHeaderView::section:first {{
        background-color: transparent;
        border-radius: 6px;
        padding: 4px;
        height: 25px;
        margin: 1px;
        margin-left: -20px;
        padding-left: 30px;
    }}
    QTreeWidget {{
        show-decoration-selected: 0;
        background-color: transparent;
        padding: 5px;
        outline: none;
        border-radius: 6px;
        border: 0px solid #1f1f1f;
    }}
    QTreeWidget::item {{
        margin-top: 3px;
        margin-bottom: 3px;
        padding-top: 3px;
        padding-bottom: 3px;
        outline: none;
        background-color: #55303030;
        height: 25px;
        border-bottom: 1px solid #1f1f1f;
        border-top: 1px solid #1f1f1f;
    }}
    QTreeWidget::item:selected {{
        margin-top: 2px;
        margin-bottom: 2px;
        padding: 0px;
        padding-top: 3px;
        padding-bottom: 3px;
        outline: none;
        background-color: #77303030;
        height: 25px;
        border-bottom: 1px solid #303030;
        border-top: 1px solid #303030;
        color: rgb({colors[2]});
    }}
    QTreeWidget::item:hover {{
        margin-top: 2px;
        margin-bottom: 2px;
        padding: 0px;
        padding-top: 3px;
        padding-bottom: 3px;
        outline: none;
        background-color: #88343434;
        height: 25px;
        border-bottom: 1px solid #303030;
        border-top: 1px solid #303030;
    }}
    QTreeWidget::item:first {{
        border-top-left-radius: 6px;
        border-bottom-left-radius: 6px;
        border-left: 1px solid #1f1f1f;
    }}
    QTreeWidget::item:last {{
        border-top-right-radius: 6px;
        border-bottom-right-radius: 6px;
        border-right: 1px solid #1f1f1f;
    }}
    QTreeWidget::item:first:selected {{
        border-left: 1px solid #303030;
    }}
    QTreeWidget::item:last:selected {{
        border-right: 1px solid #303030;
    }}
    QTreeWidget::item:first:hover {{
        border-left: 1px solid #303030;
    }}
    QTreeWidget::item:last:hover {{
        border-right: 1px solid #303030;
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
        height: 12px;
        width: 12px;
    }}
    QCheckBox::indicator:unchecked {{
        background-color: rgba(30, 30, 30, 25%);
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:disabled {{
        background-color: rgba(71, 71, 71, 0%);
        color: #dddddd;
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:unchecked:hover {{
        background-color: #2a2a2a;
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked {{
        border: 1px solid #444444;
        background-color: rgb({colors[1]});
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked:disabled {{
        border: 1px solid #444444;
        background-color: #303030;
        color: #dddddd;
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked:hover {{
        border: 1px solid #444444;
        background-color: rgb({colors[2]});
        border-radius: 4px;
    }}
    QComboBox {{
        width: 100px;
        background-color:rgba(81, 81, 81, 25%);
        border-radius: 6px;
        border: 1px solid rgba(86, 86, 86, 25%);
        height: 30px;
        padding-left: 10px;
        border: 1px solid rgba(86, 86, 86, 25%);
    }}
    QComboBox:disabled {{
        width: 100px;
        background-color: #303030;
        color: #bbbbbb;
        border-radius: 6px;
        border: 0.6px solid #262626;
        height: 25px;
        padding-left: 10px;
    }}
    QComboBox:hover {{
        background-color:rgba(86, 86, 86, 25%);
        border-radius: 6px;
        border: 1px solidrgba(100, 100, 100, 25%);
        height: 25px;
        padding-left: 10px;
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
        height: 2px;
        width: 2px;
    }}
    QComboBox QAbstractItemView {{
        border: 1px solid rgba(36, 36, 36, 50%);
        padding: 4px;
        outline: 0px;
        padding-right: 0px;
        background-color: #303030;
        border-radius: 8px;
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
        outline: none;
        border: none;
        padding-left: 10px;
        border-radius: 4px;
    }}
    #package {{
        margin: 0px;
        padding: 0px;
        background-color: #55303030;
        border-radius: 8px;
        border: 1px solid #1f1f1f;
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
    """

    menuDarkCSS = f"""
    * {{
        border-radius: 8px;
        background-color: transparent;
    }}
    QWidget{{
        background-color: transparent;
        border-radius: 8px;
        menu-scrollable: 1;
    }}
    QMenu {{
        font-family: "Segoe UI Variable Display Semib";
        border: 1px solid #111111;
        padding: 2px;
        outline: 0px;
        color: white;
        icon-size: 32px;
        background: rgba(0, 0, 0, 0.01%);
        border-radius: 8px;
    }}
    QMenu::separator {{
        margin: -2px;
        margin-top: 2px;
        margin-bottom: 2px;
        height: 1px;
        background-color: rgba(255, 255, 255, 20%);
    }}
    QMenu::icon {{
        padding-left: 10px;
        padding-left: 10px;
    }}
    QMenu::item {{
        height: 30px;
        border: none;
        background: transparent;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
        margin: 2px;
    }}
    QMenu::item:selected {{
        background: rgba(255, 255, 255, 6%);
        height: 30px;
        outline: none;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}
    QMenu::item:disabled {{
        background: transparent;
        height: 30px;
        outline: none;
        color: grey;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}
    QMenu::item:selected:disabled {{
        background: transparent;
        height: 30px;
        outline: none;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}"""

    lightCSS = f"""
    * {{
        background-color: transparent;
        color: #000000;
        font-family: "Segoe UI Variable Display";
        outline: none;
    }}
    #micawin,QInputDialog {{
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
    QComboBox QAbstractItemView {{
        border: 1px solid rgba(196, 196, 196, 25%);
        padding: 4px;
        outline: 0px;
        background-color: rgba(255, 255, 255, 10%);
        border-radius: 8px;
    }}
    QComboBox QAbstractItemView::item{{
        height: 10px;
        border: none;
        padding-left: 10px;
        border-radius: 4px;
    }}
    QComboBox QAbstractItemView::item:selected{{
        background: rgba(0, 0, 0, 6%);
        height: 10px;
        outline: none;
        color: black;
        border: none;
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
        background-color:rgba(255, 255, 255, 55%);
        border: 1px solid rgba(220, 220, 220, 55%);
        border-top: 1px solid rgba(220, 220, 220, 75%);
        border-radius: 6px;
        height: 25px;
        font-size: 10pt;
        margin: 0px;
    }}
    #FlatButton {{
        width: 150px;
        background-color: rgba(255, 255, 255, 0.1%);
        border-radius: 6px;
        border: 0px solid rgba(255, 255, 255, 1%);
        height: 25px;
        font-size: 10pt;
        border-top: 0px solid rgba(255, 255, 255, 1%);
    }}
    QPushButton:hover {{
        background-color: rgba(255, 255, 255, 90%);
        border: 1px solid rgba(220, 220, 220, 65%);
        border-top: 1px solid rgba(220, 220, 220, 80%);
        border-radius: 6px;
        height: 30px;
    }}
    #AccentButton{{
        color: #000000;
        font-size: 8pt;
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
        color: #000000;
        background-color: rgba({colors[1]}, 80%);
        border-color: rgb({colors[2]});
        border-bottom-color: rgb({colors[2]});
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
        font-size: 10pt;
        margin: 0px;
    }}
    #Headerbutton:hover {{
        background-color:rgba(240, 240, 240, 12%);
        border-radius: 8px;
        height: 30px;
    }}
    #Headerbutton:checked {{
        background-color:rgba(200, 200, 200, 75%);
        border-radius: 8px;
        border: 0px solid rgba(100, 100, 100, 25%);
        height: 30px;
    }}
    #buttonier {{
        border: 0px solid rgba(100, 100, 100, 25%);
        border-radius: 12px;
    }}
    QLineEdit {{
        background-color: rgba(255, 255, 255, 25%);
        font-family: "Segoe UI Variable Display";
        font-size: 9pt;
        width: 300px;
        padding: 5px;
        border-radius: 6px;
        border: 0.6px solid rgba(86, 86, 86, 25%);
        border-bottom: 2px solid rgb({colors[3]});
    }}
    QLineEdit:disabled {{
        background-color: rgba(255, 255, 255, 25%);
        font-family: "Segoe UI Variable Display";
        font-size: 9pt;
        width: 300px;
        padding: 5px;
        border-radius: 6px;
        border: 0.6px solid rgba(255, 255, 255, 55%);
    }}
    QScrollBar:vertical {{
        background: transparent;
        border: 1px solid rgba(240, 240, 240, 55%);
        margin: 3px;
        width: 18px;
        border: none;
        border-radius: 4px;
    }}
    QScrollBar::handle {{
        margin: 3px;
        min-height: 20px;
        min-width: 20px;
        border-radius: 3px;
        background: #a0a0a0;
    }}
    QScrollBar::handle:hover {{
        margin: 3px;
        border-radius: 3px;
        background: #808080;
    }}
    QScrollBar::add-line {{
        height: 0;
        subcontrol-position: bottom;
        subcontrol-origin: margin;
    }}
    QScrollBar::sub-line {{
        height: 0;
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
        background-color: rgba(240, 240, 240, 55%);
        border-radius: 6px;
        border: none;
        padding: 1px;
        height: 25px;
        border: 1px solid rgba(220, 220, 220, 55%);
        margin-bottom: 5px;
        margin-left: 10px;
        margin-right: 10px;
    }}
    QHeaderView::section {{
        background-color: transparent;
        border-radius: 6px;
        padding: 4px;
        height: 25px;
        margin: 1px;
    }}
    QHeaderView::section:first {{
        background-color: transparent;
        border-radius: 6px;
        padding: 4px;
        height: 25px;
        margin: 1px;
        margin-left: -20px;
        padding-left: 30px;
    }}
    QTreeWidget {{
        show-decoration-selected: 0;
        background-color: transparent;
        padding: 5px;
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
        background-color:rgba(255, 255, 255, 55%);
        border-top: 1px solid rgba(220, 220, 220, 55%);
        border-bottom: 1px solid rgba(220, 220, 220, 55%);
    }}
    QTreeWidget::item:selected {{
        margin-top: 2px;
        margin-bottom: 2px;
        padding: 0px;
        padding-top: 3px;
        padding-bottom: 3px;
        outline: none;
        background-color: rgba(240, 240, 240, 90%);
        height: 25px;
        border-bottom: 1px solid rgba(220, 220, 220, 80%);
        border-top: 1px solid rgba(220, 220, 220, 80%);
        color: rgb({colors[5]});
    }}
    QTreeWidget::item:hover {{
        margin-top: 2px;
        margin-bottom: 2px;
        padding: 0px;
        padding-top: 3px;
        padding-bottom: 3px;
        outline: none;
        background-color: rgba(255, 255, 255, 90%);
        height: 25px;
        border-bottom: 1px solid rgba(220, 220, 220, 80%);
        border-top: 1px solid rgba(220, 220, 220, 80%);
    }}
    QTreeWidget::item:first {{
        border-top-left-radius: 6px;
        border-bottom-left-radius: 6px;
        border-left: 1px solid rgba(220, 220, 220, 55%);
    }}
    QTreeWidget::item:last {{
        border-top-right-radius: 6px;
        border-bottom-right-radius: 6px;
        border-right: 1px solid rgba(220, 220, 220, 55%);
    }}
    QTreeWidget::item:first:selected {{
        border-left: 1px solid rgba(220, 220, 220, 80%);
    }}
    QTreeWidget::item:last:selected {{
        border-right: 1px solid rgba(220, 220, 220, 80%);
    }}
    QTreeWidget::item:first:hover {{
        border-left: 1px solid rgba(220, 220, 220, 80%);
    }}
    QTreeWidget::item:last:hover {{
        border-right: 1px solid rgba(220, 220, 220, 80%);
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
        height: 12px;
        width: 12px;
    }}
    QCheckBox::indicator:unchecked {{
        background-color: rgba(30, 30, 30, 25%);
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:disabled {{
        background-color: rgba(71, 71, 71, 0%);
        color: #444444;
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:unchecked:hover {{
        background-color: #2a2a2a;
        border: 1px solid #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked {{
        border: 1px solid #444444;
        background-color: rgb({colors[1]});
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked:disabled {{
        border: 1px solid #444444;
        background-color: #303030;
        color: #444444;
        border-radius: 4px;
    }}
    QCheckBox::indicator:checked:hover {{
        border: 1px solid #444444;
        background-color: rgb({colors[2]});
        border-radius: 4px;
    }}
    QComboBox {{
        width: 100px;
        background-color:rgba(255, 255, 255, 55%);
        border: 1px solid rgba(220, 220, 220, 55%);
        border-radius: 6px;
        height: 30px;
        padding-left: 10px;
    }}
    QComboBox:disabled {{
        width: 100px;
        background-color: #bbbbbb;
        color: #000000;
        border-radius: 6px;
        border: 0.6px solid #262626;
        height: 25px;
        padding-left: 10px;
    }}
    QComboBox:hover {{
        border-radius: 6px;
        height: 25px;
        padding-left: 10px;
        background-color: rgba(255, 255, 255, 90%);
        border: 1px solid rgba(220, 220, 220, 65%);
        border-top: 1px solid rgba(220, 220, 220, 80%);
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
        border: 1px solid rgba(36, 36, 36, 50%);
        padding: 4px;
        outline: 0px;
        padding-right: 0px;
        background-color: #ffffff;
        border-radius: 8px;
        color: black;
    }}
    QComboBox QAbstractItemView::item{{
        height: 30px;
        border: none;
        padding-left: 10px;
        color: black;
        border-radius: 4px;
        background-color: white;
    }}
    QComboBox QAbstractItemView::item:selected{{
        background: rgba(255, 255, 255, 6%);
        height: 30px;
        outline: none;
        border: none;
        padding-left: 10px;
        background-color: white;
        color: black;
        border-radius: 4px;
    }}
    #package {{
        margin: 0px;
        padding: 0px;
        border-radius: 8px;
        background-color:rgba(255, 255, 255, 55%);
        border: 1px solid rgba(220, 220, 220, 55%);
    }}
    QPlainTextEdit{{
        border: 1px solid #eeeeee;
        border-radius: 6px;
        padding: 6px;
        color: black;
        background-color: #ffffff;
        font-family: "Consolas";
    }}
    """

    menuLightCSS = f"""
    QWidget{{
        background-color: transparent;
        menu-scrollable: 1;
        border-radius: 8px;
    }}
    QMenu {{
        font-family: "Segoe UI Variable Display";
        border: 1px solid rgb(200, 200, 200);
        padding: 2px;
        outline: 0px;
        color: black;
        icon-size: 32px;
        background: rgba(220, 220, 220, 1%)/*#262626*/;
        border-radius: 8px;
    }}
    QMenu::separator {{
        margin: -2px;
        margin-top: 2px;
        margin-bottom: 2px;
        height: 1px;
        background-color: rgba(0, 0, 0, 20%);
    }}
    QMenu::icon{{
        padding-left: 10px;
    }}
    QMenu::item{{
        height: 30px;
        border: none;
        background: transparent;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
        margin: 2px;
    }}
    QMenu::item:selected{{
        background: rgba(0, 0, 0, 10%);
        height: 30px;
        outline: none;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}
    QMenu::item:disabled{{
        background: transparent;
        height: 30px;
        outline: none;
        color: grey;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}   
    QMenu::item:selected:disabled{{
        background: transparent;
        height: 30px;
        outline: none;
        border: none;
        padding-right: 20px;
        padding-left: 0px;
        border-radius: 4px;
    }}           
    """

    a = MainApplication()
    a.exec()
    a.running = False
    sys.exit(0)
except Exception as e:
    import webbrowser, traceback, platform
    try:
        from tools import version as appversion
    except Exception as e:
        appversion = "Unknown"
    os_info = f"" + \
        f"                        OS: {platform.system()}\n"+\
        f"                   Version: {platform.win32_ver()}\n"+\
        f"           OS Architecture: {platform.machine()}\n"+\
        f"          APP Architecture: {platform.architecture()[0]}\n"+\
        f"               APP Version: {appversion}\n"+\
        f"                   Program: WingetUI\n"+\
        f"           Program section: Main script"+\
        "\n\n-----------------------------------------------------------------------------------------"
    traceback_info = "Traceback (most recent call last):\n"
    try:
        for line in traceback.extract_tb(e.__traceback__).format():
            traceback_info += line
        traceback_info += f"\n{type(e).__name__}: {str(e)}"
    except:
        traceback_info += "\nUnable to get traceback"
    traceback_info += str(type(e))
    traceback_info += ": "
    traceback_info += str(e)
    webbrowser.open(("https://www.somepythonthings.tk/error-report/?appName=WingetUI&errorBody="+os_info.replace('\n', '{l}').replace(' ', '{s}')+"{l}{l}{l}{l}WingetUI Log:{l}"+str("\n\n\n\n"+traceback_info).replace('\n', '{l}').replace(' ', '{s}')).replace("#", "|=|"))
    print(traceback_info)