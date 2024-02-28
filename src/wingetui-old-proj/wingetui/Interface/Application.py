class WingetUIApplication(QApplication):

    def __init__(self):
        try:
            if getSettings("ShownWelcomeWizard") is False or "--welcomewizard" in sys.argv or "--welcome" in sys.argv:
                self.askAboutPackageManagers(onclose=lambda: (Thread(target=self.loadPreUIComponents, daemon=True).start(), Thread(target=lambda: (time.sleep(15), self.callInMain.emit(skipButton.show)), daemon=True).start()))
            else:

    def askAboutPackageManagers(self, onclose: object):
        self.ww = WelcomeWindow(callback=lambda: (self.popup.show(), onclose()))
        self.popup.hide()
        self.ww.show()

