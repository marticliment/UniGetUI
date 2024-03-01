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

