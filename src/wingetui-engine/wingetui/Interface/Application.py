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

