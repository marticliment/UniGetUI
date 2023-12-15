if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module 
    print("redirecting...")
    import subprocess, os, sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import subprocess
import time
import os
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *

import wingetui.Core.Globals as Globals
from wingetui.Interface.CustomWidgets.SpecificWidgets import *
from wingetui.Interface.Tools import *
from wingetui.Interface.Tools import _
from wingetui.PackageEngine.Classes import *


class PackageInstallerWidget(QWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(tuple)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    callInMain = Signal(object)
    changeBarOrientation = Signal()

    def __init__(self, package: Package, options: InstallationOptions):
        super().__init__()
        self.Package = package
        self.Options = options
        self.actionDone = _("installed")
        self.actionDoing = _("installing")
        self.actionName = _("installation")
        self.actionVerb = _("install")
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)
        self.liveOutputWindowWindow = QMainWindow(self)
        self.liveOutputWindow = CustomPlainTextEdit(self.liveOutputWindowWindow)
        self.liveOutputWindowWindow.setCentralWidget(self.liveOutputWindow)
        self.liveOutputWindowWindow.setContentsMargins(10, 10, 10, 10)
        self.liveOutputWindowWindow.setWindowIcon(self.window().windowIcon())
        self.liveOutputWindow.setReadOnly(True)
        self.liveOutputWindowWindow.resize(700, 400)
        self.liveOutputWindowWindow.setWindowTitle(_("Live command-line output"))
        self.addInfoLine.connect(lambda args: (self.liveOutputWindow.setPlainText(self.liveOutputWindow.toPlainText() + "\n" + args[0]) if args[1] else None, self.liveOutputWindow.verticalScrollBar().setValue(self.liveOutputWindow.verticalScrollBar().maximum())))

        if getSettings(f"AlwaysElevate{self.Package.PackageManager.NAME}"):
            print(f"🟡 {self.Package.PackageManager.NAME} installation automatically elevated!")
            self.Options.RunAsAdministrator = True

        if getSettings("DoCacheAdminRights"):
            if self.Options.RunAsAdministrator and not Globals.adminRightsGranted:
                cprint(" ".join([GSUDO_EXECUTABLE, "cache", "on", "--pid", f"{os.getpid()}", "-d", "-1"]))
                asksudo = subprocess.Popen([GSUDO_EXECUTABLE, "cache", "on", "--pid", f"{os.getpid()}", "-d", "-1"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                asksudo.wait()
                Globals.adminRightsGranted = True

        self.finishedInstallation = True
        self.callInMain.connect(lambda f: f())
        self.setMinimumHeight(500)
        self.setObjectName("package")
        self.setFixedHeight(50)
        self.layout = QHBoxLayout()
        self.layout.setContentsMargins(10, 10, 10, 10)
        self.iconLabel = QPushButton(self)
        self.iconLabel.setIcon(getMaskedIcon("package_masked"))
        self.iconLabel.setFixedHeight(30)
        self.iconLabel.setIconSize(QSize(24, 24))
        self.iconLabel.setFixedWidth(30)
        self.label = QLabel(_("{0} installation").format(package.Name))
        self.layout.addWidget(self.iconLabel)
        self.layout.addSpacing(5)
        self.layout.addWidget(self.label)
        self.layout.addSpacing(5)
        self.progressbar = QProgressBar(self)
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 1000)
        self.setProgressbarColor("grey")
        self.progressbar.setValue(0)
        self.progressbar.show()
        self.progressbar.setFixedHeight(2)
        self.changeBarOrientation.connect(lambda: self.progressbar.setInvertedAppearance(not self.progressbar.invertedAppearance()))
        self.finishInstallation.connect(self.finish)
        self.addInfoLine.connect(lambda args: self.liveOutputButton.setText(args[0]))
        self.counterSignal.connect(self.counter)
        self.liveOutputButton = ButtonWithResizeSignal(QIcon(getMedia("console", autoIconMode=False)), "")
        self.liveOutputButton.clicked.connect(lambda: (self.liveOutputWindowWindow.show(), ApplyMica(self.liveOutputWindowWindow.winId(), isDark()), self.liveOutputWindowWindow.setWindowIcon(self.window().windowIcon())))
        self.liveOutputButton.setToolTip(_("Show the live output"))
        self.liveOutputButton.setText(_("Waiting for other installations to finish..."))
        self.liveOutputButton.resized.connect(self.updateProgressbarSize)
        self.layout.addWidget(self.liveOutputButton, stretch=1)
        self.adminBadge = QPushButton(self)
        self.adminBadge.setFixedSize(QSize(30, 30))
        self.adminBadge.setIcon(QIcon(getMedia("runasadmin")))
        self.adminBadge.setEnabled(False)
        self.adminBadge.setToolTip(_("This process is running with administrator privileges"))
        self.layout.addWidget(self.adminBadge)
        if not self.Options.RunAsAdministrator and not Globals.mainWindow.isAdmin():
            self.adminBadge.setVisible(False)
        self.cancelButton = QPushButton(QIcon(getMedia("cancel", autoIconMode=False)), _("Cancel"))
        self.cancelButton.clicked.connect(self.cancel)
        self.cancelButton.setFixedHeight(30)
        self.liveOutputButton.setFixedHeight(28)
        self.layout.addWidget(self.cancelButton)
        self.setLayout(self.layout)
        self.canceled = False
        self.installId = str(time.time())
        self.cancelButton.setObjectName("PackageButton")
        self.adminBadge.setObjectName("PackageButton")
        self.liveOutputButton.setObjectName("PackageButton")
        self.liveOutputButton.setStyleSheet(f"text-align:left;font-family: \"Consolas\";font-weight: regular;padding-left: 5px;padding-right: 5px; color: {'lightgray' if isDark() else '#262626'};border-bottom-left-radius: 4px;border-bottom-right-radius: 4px;")
        self.iconLabel.setObjectName("FlatButton")
        queueProgram(self.installId)

        self.leftSlow = QPropertyAnimation(self.progressbar, b"value")
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(self.update)
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))

        self.rightSlow = QPropertyAnimation(self.progressbar, b"value")
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(self.update)
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))

        self.leftFast = QPropertyAnimation(self.progressbar, b"value")
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(self.update)
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QPropertyAnimation(self.progressbar, b"value")
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(self.update)
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.Pending)

        self.waitThread = KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        Thread(target=self.loadIconThread, daemon=True, name=f"Installer: loading icon for {package}").start()
        print(f"🟢 Waiting for install permission... title={self.Package.Name}, id={self.Package.Id}, installId={self.installId}")
        print("🔵 Given package:", package)
        print("🔵 Installation options:", options)

        ApplyMica(self.liveOutputWindowWindow.winId(), MicaTheme.DARK)

    def startInstallation(self) -> None:
        last_position_count = -1
        while self.installId != Globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
            append = " "
            if last_position_count != Globals.pending_programs.index(self.installId):
                last_position_count = Globals.pending_programs.index(self.installId)
                try:
                    append += _("(Number {0} in the queue)").format(last_position_count)
                except ValueError:
                    print(f"🔴 Package {self.Package.Id} not in Globals.pending_programs")
                self.addInfoLine.emit((_("Waiting for other installations to finish...") + append, False))
                
        print("🟢 Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def loadIconThread(self):
        iconPath = getPackageIcon(self.Package)
        if os.path.exists(iconPath):
            icon = QIcon(iconPath)
            if not icon.isNull():
                self.callInMain.emit(lambda: self.iconLabel.setIcon(icon))
            else:
                print(f"🟠 Icon for {self.Package.Id} exists but is null")
                self.callInMain.emit(lambda: self.iconLabel.setIcon(getMaskedIcon("package_masked")))
        else:
            self.callInMain.emit(lambda: self.iconLabel.setIcon(getMaskedIcon("package_masked")))
            print(f"🟡 Icon for {self.Package.Id} does not exist")

    def setProgressbarColor(self, color: str):
        self.progressbar.raise_()
        self.progressbar.setStyleSheet(f"QProgressBar::chunk{{border-top-left-radius: 0px;border-top-right-radius: 0px;margin-right: 2px;margin-left: 2px;background-color: {color}}}")

    def runInstallation(self) -> None:
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.BeingProcessed)
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit((_("Running the installer..."), True))
        self.leftSlow.start()
        self.setProgressbarColor(blueColor)
        self.p = self.Package.PackageManager.startInstallation(self.Package, self.Options, self)
        AddOperationToLog("installation", self.Package, '"' + ' '.join(self.p.args) + '"')

    def counter(self, line: int) -> None:
        if line == 1:
            self.progressbar.setValue(250)
        if line == 4:
            self.progressbar.setValue(500)
        elif line == 6:
            self.cancelButton.setEnabled(True)
            self.progressbar.setValue(750)

    def cancel(self):
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.Default)
        print("🔵 Sending cancel signal...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(1000)
        self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
        self.liveOutputButton.setText(_("Installation canceled by the user!"))
        if not self.finishedInstallation:
            try:
                self.p.kill()
            except Exception as e:
                report(e)
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText(_("Close"))
        self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled = True
        removeProgram(self.installId)
        try:
            self.waitThread.kill()
        except Exception:
            pass
        try:
            self.p.kill()
        except Exception:
            pass

    def finish(self, returncode: int, output: str = "") -> None:
        AddResultToLog(output.split("\n"), self.Package, returncode)
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(1000)
        if self.progressbar.invertedAppearance():
            self.progressbar.setInvertedAppearance(False)
        if returncode in (RETURNCODE_NEEDS_ELEVATION, RETURNCODE_NEEDS_SCOOP_ELEVATION):
            self.Options.RunAsAdministrator = True
            self.adminBadge.setVisible(self.Options.RunAsAdministrator)
            self.runInstallation()
            return
        elif returncode == RETURNCODE_NEEDS_PIP_ELEVATION:
            self.Options.CustomParameters.append("--user")
            self.runInstallation()
            return
        elif "winget settings --enable InstallerHashOverride" in output:
            print("🟠 Requiring the user to enable skiphash setting!")
            subprocess.run([GSUDO_EXECUTABLE, Winget.EXECUTABLE, "settings", "--enable", "InstallerHashOverride"], shell=True)
            self.runInstallation()
            return
        Globals.tray_is_installing = False
        update_tray_icon()
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        removeProgram(self.installId)
        try:
            self.waitThread.kill()
        except Exception:
            pass
        try:
            self.p.kill()
        except Exception:
            pass
        if self.canceled:
            return
        self.cancelButton.setText(_("OK"))
        self.cancelButton.clicked.connect(self.close)
        self.progressbar.setValue(1000)
        t = ToastNotification(self, self.callInMain.emit)
        t.addOnClickCallback(lambda: (Globals.mainWindow.showWindow(-1)))
        if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED:     
            if self.Package.PackageItem:
                self.Package.PackageItem.setTag(PackageItem.Tag.Default)
            self.setProgressbarColor("#11945a" if isDark() else "#11945a")
            if returncode in (RETURNCODE_OPERATION_SUCCEEDED, RETURNCODE_NO_APPLICABLE_UPDATE_FOUND):
                t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                if Globals.ENABLE_SUCCESS_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode=False)))
                self.liveOutputButton.setText(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                self.startCoolDown()
            if returncode == RETURNCODE_NEEDS_RESTART:
                t.setTitle(_("Restart required"))
                t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", ".") + " " + _("Restart your computer to finish the installation"))
                t.setSmallText(_("You may restart your computer later if you wish"))
                t.addAction(_("Restart now"), Globals.mainWindow.askRestart)
                t.addAction(_("Restart later"), t.close)
                if Globals.ENABLE_WINGETUI_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("restart_color", autoIconMode=False)))
                self.liveOutputButton.setText(_("Restart your PC to finish installation"))
                Globals.tray_is_needs_restart = True
                update_tray_icon()
            if type(self) is PackageInstallerWidget:
                self.Package.PackageItem.setCheckState(0, Qt.CheckState.Unchecked)
                self.Package.PackageItem.setIcon(1, getMaskedIcon("installed_masked"))
                self.Package.PackageItem.setToolTip(1, _("This package is already installed") + " - " + self.Package.Name)

                if self.Package.Id not in Globals.uninstall.IdPackageReference.keys():
                    print("🔵 Adding package to the uninstall section...")
                    Globals.uninstall.addItem(self.Package)
                    Globals.uninstall.updatePackageNumber()
        else:
            if self.Package.PackageItem:
                self.Package.PackageItem.setTag(PackageItem.Tag.Failed)   
            Globals.tray_is_error = True
            update_tray_icon()
            self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
            self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
            self.err = CustomMessageBox(self.window())
            warnIcon = QIcon(getMedia("notif_warn"))
            t.addAction(_("Show details"), lambda: (Globals.mainWindow.showWindow(-1)))
            t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.Package.Name))
            dialogData = {
                "titlebarTitle": _("WingetUI - {0} {1}").format(self.Package.Name, self.actionName),
                "buttonTitle": _("Close"),
                "errorDetails": output.replace("-\\|/", "").replace("▒", "").replace("█", ""),
                "icon": warnIcon,
                "notifTitle": _("Can't {0} {1}").format(self.actionVerb, self.Package.Name),
                "notifIcon": warnIcon,
            }
            if returncode == RETURNCODE_INCORRECT_HASH:  # if the installer's hash does not coincide
                t.setDescription(_("The installer has an invalid checksum"))
                dialogData["mainTitle"] = _("{0} aborted").format(self.actionName.capitalize())
                dialogData["mainText"] = _("The checksum of the installer does not coincide with the expected value, and the authenticity of the installer can't be verified. If you trust the publisher, {0} the package again skipping the hash check.").format(self.actionVerb)
            else:  # if there's a generic error
                t.setDescription(_("{0} {1} failed").format(self.Package.Name.capitalize(), self.actionName))
                t.addAction(_("Retry"), lambda: (self.runInstallation(), self.cancelButton.setText(_("Cancel"))))
                dialogData["mainTitle"] = _("{0} failed").format(self.actionName.capitalize())
                dialogData["mainText"] = _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the installer.").format(action=self.actionVerb, package=self.Package.Name, showDetails=_("Show details"))
            self.err.showErrorMessage(dialogData, showNotification=False)
            if Globals.ENABLE_ERROR_NOTIFICATIONS:
                t.show()

    def startCoolDown(self):
        if not getSettings("MaintainSuccessfulInstalls"):
            self.ops = -1

            def setUpOPS():
                op1 = QGraphicsOpacityEffect(self)
                op2 = QGraphicsOpacityEffect(self)
                op3 = QGraphicsOpacityEffect(self)
                op4 = QGraphicsOpacityEffect(self)
                op5 = QGraphicsOpacityEffect(self)
                op6 = QGraphicsOpacityEffect(self)
                ops = [op1, op2, op3, op4, op5, op6]
                return ops

            def updateOp(v: float):
                i = 0
                if self.ops == -1:
                    self.ops = setUpOPS()
                for widget in [self.cancelButton, self.label, self.progressbar, self.liveOutputButton, self.adminBadge, self.iconLabel]:
                    self.ops[i].setOpacity(v)
                    widget: QWidget
                    widget.setGraphicsEffect(self.ops[i])
                    widget.setAutoFillBackground(True)
                    i += 1
                    if v == 0:
                        widget.hide()

            # updateOp(1)
            a = QVariantAnimation(self)
            a.setStartValue(1.0)
            a.setEndValue(0.0)
            a.setEasingCurve(QEasingCurve.InOutQuad)
            a.setDuration(200)
            a.valueChanged.connect(lambda v: updateOp(v))
            a.finished.connect(self.heightAnim)

            def timerFunc():
                time.sleep(3)
                self.callInMain.emit(a.start)
            Thread(target=timerFunc, daemon=True).start()
        else:
            print("🟡 Autohide disabled!")

    def heightAnim(self):
        op = QGraphicsOpacityEffect(self)
        self.setGraphicsEffect(op)
        a = QVariantAnimation(self)
        a.setStartValue(100)
        a.setEndValue(0)
        a.setEasingCurve(QEasingCurve.InOutQuad)
        a.setDuration(100)
        a.valueChanged.connect(lambda v: op.setOpacity(v / 100))
        a.finished.connect(self.close)
        a.start()

    def close(self):
        self.liveOutputWindow.close()
        self.liveOutputWindowWindow.close()
        Globals.installersWidget.removeItem(self)
        super().close()
        super().destroy()

    def showEvent(self, event: QShowEvent) -> None:
        self.updateProgressbarSize()
        return super().showEvent(event)

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.updateProgressbarSize()
        return super().resizeEvent(event)

    def updateProgressbarSize(self):
        try:
            pos = self.liveOutputButton.pos()
            pos.setY(pos.y() + self.liveOutputButton.height() - 2)
            self.progressbar.move(pos)
            self.progressbar.setFixedWidth(self.liveOutputButton.width())
        except AttributeError:
            print("AttributeError on PackageInstallerWidget.updateProgressbarSize")


class PackageUpdaterWidget(PackageInstallerWidget):

    def __init__(self, package: UpgradablePackage, options: InstallationOptions):
        super().__init__(package, options)
        self.Package = package
        self.actionDone = _("updated")
        self.actionDoing = _("updating")
        self.actionName = _("update(noun)")
        self.actionVerb = _("update(verb)")
        self.label.setText(_("{0} update").format(package.Name))

    def runInstallation(self) -> None:
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.BeingProcessed)
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.addInfoLine.emit((_("Running the updater..."), True))
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.leftSlow.start()
        self.setProgressbarColor(blueColor)
        self.p = self.Package.PackageManager.startUpdate(self.Package, self.Options, self)
        AddOperationToLog("update", self.Package, '"' + ' '.join(self.p.args) + '"')

    def finish(self, returncode: int, output: str = "") -> None:
        if returncode in (RETURNCODE_NEEDS_ELEVATION, RETURNCODE_NEEDS_SCOOP_ELEVATION):
            self.Options.RunAsAdministrator = True
            self.adminBadge.setVisible(self.Options.RunAsAdministrator)
            self.runInstallation()
        elif returncode == RETURNCODE_NEEDS_PIP_ELEVATION:
            self.Options.CustomParameters.append("--user")
            self.runInstallation()
            return
        else:
            Globals.tray_is_installing = False
            update_tray_icon()
            self.leftSlow.stop()
            self.leftFast.stop()
            self.rightSlow.stop()
            self.rightFast.stop()
            self.progressbar.setValue(1000)
            if self.progressbar.invertedAppearance():
                self.progressbar.setInvertedAppearance(False)

            if self.Package.Version in (_("Unknown"), "Unknown") or (returncode == RETURNCODE_NO_APPLICABLE_UPDATE_FOUND and not self.canceled):
                self.Package.AddToIgnoredUpdates(self.Package.NewVersion)

            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:
                self.Package.PackageItem.removeFromList()
                InstalledItem = self.Package.PackageItem.getInstalledPackageItem()
                if InstalledItem:
                    InstalledItem.setTag(InstalledItem.Tag.Default)
                AvailablePackage = self.Package.PackageItem.getDiscoverPackageItem()
                if AvailablePackage:
                    AvailablePackage.setTag(InstalledItem.Tag.Installed)

            super().finish(returncode, output)

    def close(self):
        self.liveOutputWindow.close()
        self.liveOutputWindowWindow.close()
        Globals.installersWidget.removeItem(self)
        super().destroy()
        super().close()


class PackageUninstallerWidget(PackageInstallerWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(tuple)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    changeBarOrientation = Signal()

    def __init__(self, package: UpgradablePackage, options: InstallationOptions):
        super().__init__(package, options)
        self.actionDone = _("uninstalled")
        self.actionDoing = _("uninstalling")
        self.actionName = _("uninstallation")
        self.actionVerb = _("uninstall")
        self.finishedInstallation = True
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(50)
        self.label.setText(_("{0} Uninstallation").format(package.Name))

    def runInstallation(self) -> None:
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.BeingProcessed)
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.leftSlow.start()
        self.addInfoLine.emit((_("Running the uninstaller..."), True))
        self.setProgressbarColor(blueColor)
        self.p = self.Package.PackageManager.startUninstallation(self.Package, self.Options, self)
        AddOperationToLog("installation", self.Package, '"' + ' '.join(self.p.args) + '"')

    def counter(self, line: int) -> None:
        if line == 1:
            self.progressbar.setValue(250)
        elif line == 4:
            self.progressbar.setValue(500)
        elif line == 6:
            self.cancelButton.setEnabled(True)
            self.progressbar.setValue(750)

    def cancel(self):
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.Default)
        print("🔵 Sending cancel signal...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(1000)
        self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
        self.liveOutputButton.setText(_("Uninstall canceled by the user!"))
        if not self.finishedInstallation:
            try:
                self.p.kill()
            except Exception as e:
                report(e)
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText(_("Close"))
        self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled = True
        removeProgram(self.installId)
        try:
            self.waitThread.kill()
        except Exception:
            pass
        try:
            self.p.kill()
        except Exception:
            pass

    def finish(self, returncode: int, output: str = "") -> None:
        AddResultToLog(output.split("\n"), self.Package, returncode)
        if returncode in (RETURNCODE_NEEDS_ELEVATION, RETURNCODE_NEEDS_SCOOP_ELEVATION):
            self.Options.RunAsAdministrator = True
            self.adminBadge.setVisible(self.Options.RunAsAdministrator)
            self.runInstallation()
        elif returncode == RETURNCODE_NEEDS_PIP_ELEVATION:
            self.Options.CustomParameters.append("--user")
            self.runInstallation()
            return
        else:
            Globals.tray_is_installing = False
            update_tray_icon()
            self.leftSlow.stop()
            self.leftFast.stop()
            self.rightSlow.stop()
            self.rightFast.stop()
            self.progressbar.setValue(1000)
            if self.progressbar.invertedAppearance():
                self.progressbar.setInvertedAppearance(False)
            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:

                self.Package.PackageItem.removeFromList()
                AvailableItem = self.Package.PackageItem.getDiscoverPackageItem()
                if AvailableItem:
                    AvailableItem.setTag(AvailableItem.Tag.Default)

                UpgradableItem = self.Package.PackageItem.getUpdatesPackageItem()
                if UpgradableItem:
                    UpgradableItem.removeFromList()

            self.finishedInstallation = True
            self.cancelButton.setEnabled(True)
            removeProgram(self.installId)
            try:
                self.waitThread.kill()
            except Exception:
                pass
            try:
                self.p.kill()
            except Exception:
                pass
            if not self.canceled:
                if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED:   
                    if self.Package.PackageItem:
                        self.Package.PackageItem.setTag(PackageItem.Tag.Default)
                    self.setProgressbarColor("#11945a" if isDark() else "#11945a")
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode=False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.liveOutputButton.setText(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                    self.progressbar.setValue(1000)
                    self.startCoolDown()
                    t = ToastNotification(self, self.callInMain.emit)
                    t.addOnClickCallback(lambda: (Globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                    t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                    if Globals.ENABLE_SUCCESS_NOTIFICATIONS:
                        t.show()
                else:            
                    if self.Package.PackageItem:
                        self.Package.PackageItem.setTag(PackageItem.Tag.Failed)
                    Globals.tray_is_error = True
                    update_tray_icon()
                    self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.progressbar.setValue(1000)
                    self.err = CustomMessageBox(self.window())
                    t = ToastNotification(self, self.callInMain.emit)
                    t.addOnClickCallback(lambda: (Globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.Package.Name))
                    t.setDescription(_("{0} {1} failed").format(self.Package.Name.capitalize(), self.actionName))
                    t.addAction(_("Retry"), lambda: (self.runInstallation(), self.cancelButton.setText(_("Cancel"))))
                    t.addAction(_("Show details"), lambda: (Globals.mainWindow.showWindow(-1)))
                    errorData = {
                        "titlebarTitle": _("WingetUI - {0} {1}").format(self.Package.Name, self.actionName),
                        "mainTitle": _("{0} failed").format(self.actionName.capitalize()),
                        "mainText": _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the uninstaller.").format(action=self.actionVerb, package=self.Package.Name, showDetails=_("Show details")),
                        "buttonTitle": _("Close"),
                        "errorDetails": output.replace("-\\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("notif_warn")),
                    }
                    if Globals.ENABLE_ERROR_NOTIFICATIONS:
                        t.show()
                    self.err.showErrorMessage(errorData, showNotification=False)

    def close(self):
        self.liveOutputWindow.close()
        self.liveOutputWindowWindow.close()
        Globals.installersWidget.removeItem(self)
        super().close()
        super().destroy()

""" for future use

class CustomInstallerWidget(PackageInstallerWidget):

    def __init__(self, name: str, command: list, packageManager: PackageManagerModule, runAsAdministrator: bool = False):
        self.Package = Package(name, name, "N/A", packageManager.NAME, packageManager)
        self.Package.PackageItem = QTreeWidgetItem()
        self.Options = InstallationOptions(self.Package, reset = True)
        self.command = command
        if runAsAdministrator:
            self.Options.RunAsAdministrator = True
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit((_("Running the installer..."), True))
        self.leftSlow.start()
        self.setProgressbarColor(blueColor)
        Command = self.command
        if self.Options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {self.Package} installation with Command", Command)
        self.p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(self.p, self.Options,), name=f"{self.Package.PackageManager.NAME} installation thread: installing {self.Package.Name}").start()
        AddOperationToLog("installation", self.Package, '"' + ' '.join(self.p.args) + '"')

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions):
        output = ""
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                output += line + "\n"
                self.addInfoLine.emit((line, True))
        self.finishInstallation.emit(p.returncode, output)


class CustomUninstallerWidget(PackageUninstallerWidget):

    def __init__(self, name: str, command: list, packageManager: PackageManagerModule, runAsAdministrator: bool = False):
        self.Package = Package(name, name, "N/A", packageManager.NAME, packageManager)
        self.Package.PackageItem = QTreeWidgetItem()
        self.Options = InstallationOptions(self.Package, reset = True)
        if runAsAdministrator:
            self.Options.RunAsAdministrator = True

        self.command = command
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit((_("Running the uninstaller..."), True))
        self.leftSlow.start()
        self.setProgressbarColor(blueColor)
        Command = self.command
        if self.Options.RunAsAdministrator:
            Command = [GSUDO_EXECUTABLE] + Command
        print(f"🔵 Starting {self.Package} uninstallation with Command", Command)
        self.p = subprocess.Popen(Command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
        Thread(target=self.installationThread, args=(self.p, self.Options,), name=f"{self.Package.PackageManager.NAME} uninstallation thread: uninstalling {self.Package.Name}").start()
        AddOperationToLog("uninstall", self.Package, '"' + ' '.join(self.p.args) + '"')

    def installationThread(self, p: subprocess.Popen, options: InstallationOptions):
        output = ""
        while p.poll() is None:
            line = str(p.stdout.readline(), encoding='utf-8', errors="ignore").strip()
            if line:
                output += line + "\n"
                self.addInfoLine.emit((line, True))
        self.finishInstallation.emit(p.returncode, output)

"""

class SourceInstallerWidget(PackageInstallerWidget):
    Source: ManagerSource = None

    def __init__(self, source: ManagerSource):
        self.Source = source
        self.Package = Package(self.Source.Name, self.Source.Name, "", self.Source.Manager.NAME, self.Source.Manager)
        self.Package.PackageItem = PackageItem(self.Package)
        self.Options = InstallationOptions(self.Package, reset = True)
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.BeingProcessed)
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit((_("Running the installer..."), True))
        self.leftSlow.start()
        self.setProgressbarColor(blueColor)
        self.p = self.Source.Manager.installSource(self.Source, self.Options, self)
        AddOperationToLog("installation", self.Package, '"' + ' '.join(self.p.args) + '"')


class SourceUninstallerWidget(PackageUninstallerWidget):
    Source: ManagerSource = None

    def __init__(self, source: ManagerSource):
        self.Source = source
        self.Package = Package(self.Source.Name, self.Source.Name, "", self.Source.Manager.NAME, self.Source.Manager)
        self.Package.PackageItem = PackageItem(self.Package)
        self.Options = InstallationOptions(self.Package, reset = True)
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        if self.Package.PackageItem:
            self.Package.PackageItem.setTag(PackageItem.Tag.BeingProcessed)
        Globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.leftSlow.start()
        self.addInfoLine.emit((_("Running the uninstaller..."), True))
        self.setProgressbarColor(blueColor)
        self.p = self.Source.Manager.uninstallSource(self.Source, self.Options, self)
        AddOperationToLog("installation", self.Package, '"' + ' '.join(self.p.args) + '"')


class SourceManagerWidget(QWidget):
    setLoadBarValue = Signal(str)
    callInMain = Signal(object)
    changeBarOrientation = Signal()
    Sources = []
    Manager: PackageManagerWithSources = None
    IsLoading = False

    def __init__(self, manager: PackageManagerWithSources):
        super().__init__()
        self.Manager = manager
        self.callInMain.connect(lambda f: f())
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)
        self.setObjectName("stBtn")
        layout = QVBoxLayout()
        hLayout = QHBoxLayout()
        label = CustomLabel(_("Manage {0} sources").format(self.Manager.NAME))
        label.setFixedWidth(300)
        hLayout.addWidget(label)
        hLayout.addStretch()

        self.loadingProgressBar = QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.loadingProgressBar.hide()
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not self.loadingProgressBar.invertedAppearance()))

        self.reloadButton = QPushButton()
        self.reloadButton.clicked.connect(self.LoadSources)
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setAccessibleName(_("Reload"))
        self.addBucketButton = QPushButton(_("Add source"))
        self.addBucketButton.setFixedHeight(30)
        self.addBucketButton.clicked.connect(self.InstallSource)
        hLayout.addWidget(self.addBucketButton)
        hLayout.addWidget(self.reloadButton)
        hLayout.setContentsMargins(10, 0, 15, 0)
        layout.setContentsMargins(60, 10, 5, 10)
        self.TreeWidget = TreeWidget(EnableTopButton = False)
        self.TreeWidget.setColumnCount(3)
        self.TreeWidget.setHeaderLabels([_("Name"), _("Update date"), _("Manifests"), _("Url")])
        self.TreeWidget.sortByColumn(0, Qt.SortOrder.AscendingOrder)
        self.TreeWidget.setSortingEnabled(True)
        self.TreeWidget.setVerticalScrollMode(QTreeWidget.ScrollMode.ScrollPerPixel)
        self.TreeWidget.setIconSize(QSize(24, 24))
        
        self.TreeWidget.setColumnHidden(1, not self.Manager.Capabilities.Sources.KnowsUpdateDate)
        self.TreeWidget.setColumnHidden(2, not self.Manager.Capabilities.Sources.KnowsPackageCount)
        self.TreeWidget.setColumnWidth(0, 120)
        self.TreeWidget.setColumnWidth(3, 280)
        self.TreeWidget.setColumnWidth(1, 80)
        self.TreeWidget.setColumnWidth(2, 120)
        self.TreeWidget.setColumnWidth(4, 24)
        self.TreeWidget.setFixedHeight(300)
        
        layout.addLayout(hLayout)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.TreeWidget)
        self.setLayout(layout)

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

        self.leftSlow.start()

        self.ApplyIcons()
        self.LoadSources()
        self.registeredThemeEvent = False

    def ApplyIcons(self):
        if isDark():
            self.TreeWidget.setStyleSheet("QTreeWidget{border: 1px solid #222222; background-color: rgba(30, 30, 30, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        else:
            self.TreeWidget.setStyleSheet("QTreeWidget{border: 1px solid #f5f5f5; background-color: rgba(255, 255, 255, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
        self.bucketIcon = QIcon(getMedia("list"))
        self.reloadButton.click()

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.LoadSources()
        return super().showEvent(event)

    def LoadSources(self):
        if self.IsLoading:
            return
        self.Sources = []
        self.TreeWidget.clear()
        self.loadingProgressBar.show()
        self.TreeWidget.label.show()
        self.TreeWidget.label.setText(_("Loading..."))
        Thread(target=self.WaitForSources, name=f"Loading {self.Manager.NAME} sources").start()        
        
    def WaitForSources(self):
        self.IsLoading = True
        if not self.Manager.isEnabled():
            self.callInMain.emit(lambda: self.loadingProgressBar.hide())
            self.callInMain.emit(self.TreeWidget.label.setText(_(f"{self.Manager.NAME} is not enabled")))
        
        self.Sources = self.Manager.getSources()
        for source in self.Sources:
            self.callInMain.emit(partial(self.AddSource, source))
                    
        if len(self.Sources) == 0:
            self.callInMain.emit(lambda: self.TreeWidget.label.setText(_("No sources were found")))
        
        self.IsLoading = False
        self.callInMain.emit(lambda: self.loadingProgressBar.hide())

    def AddSource(self, source: ManagerSource):
        self.TreeWidget.label.hide()
        item = QTreeWidgetItem()
        item.setText(0, source.Name)
        item.setToolTip(0, source.Name)
        item.setIcon(0, self.bucketIcon)
        item.setText(3, source.Url)
        item.setToolTip(3, source.Url)
        item.setText(1, source.UpdateDate)
        item.setToolTip(1, source.UpdateDate)
        item.setText(2, str(source.PackageCount))
        item.setToolTip(2, str(source.PackageCount))
        self.TreeWidget.addTopLevelItem(item)
        
        layout = QHBoxLayout()
        layout.addStretch()
        layout.setContentsMargins(8, 1, 8, 1)
        btn = QPushButton()
        layout.addWidget(btn)
        btn.clicked.connect(lambda: (self.UninstallSource(source), self.TreeWidget.takeTopLevelItem(self.TreeWidget.indexOfTopLevelItem(item))))
        btn.setFixedSize(24, 24)
        btn.setIcon(QIcon(getMedia("menu_uninstall")))
        
        w = QWidget()
        w.setLayout(layout)
        
        self.TreeWidget.setItemWidget(item, 3, w)
        
    def InstallSource(self) -> None:
        sourceReference = {source.Name: source for source in self.Manager.KnownSources}
        r = QInputDialog.getItem(self, _("Add a source to {0}").format(self.Manager.NAME), _("Which source do you want to add?") + " " + _("Select \"{item}\" to add your custom bucket").format(item=_("Another source")), list(sourceReference.keys()) + [_("Another source")], 1, editable=False)
        if r[1]:
            if r[0] == _("Another source"):
                r2 = QInputDialog.getText(self, _("Add a source to {0}").format(self.Manager.NAME), _("Type here the name and the URL of the source you want to add, separed by a space."), text="sourcename https://somewhere.net/your-custom/source-endpoint")
                if r2[1]:
                    name = r2[0].split(" ")[0]
                    url = r2[0].split(" ")[1]
                    source = ManagerSource(self.Manager, name, url)
                    p = SourceInstallerWidget(source)
                    Globals.installersWidget.addItem(p)
                    p.finishInstallation.connect(self.LoadSources)
            else:
                source = sourceReference[r[0]]
                p = SourceInstallerWidget(source)
                Globals.installersWidget.addItem(p)
                p.finishInstallation.connect(self.LoadSources)
        pass

    def UninstallSource(self, source: ManagerSource) -> None:
        Globals.installersWidget.addItem(SourceUninstallerWidget(source))