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

            Thread(target=self.removeScoopCache, daemon=True).start()

            Thread(target=self.updateIfPossible, daemon=True).start()


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

