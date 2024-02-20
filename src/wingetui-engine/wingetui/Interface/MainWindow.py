

class RootWindow(QMainWindow):

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

