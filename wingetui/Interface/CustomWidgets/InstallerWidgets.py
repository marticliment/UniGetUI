"""

wingetui/Interface/CustomWidgets/InstallerWidgets.py

This file contains the custom widgets that represent a package when it is being installed/updated/uninstalled.
This file also contains the following classes:
 - ScoopBucketManager
 - WingetBucketManager

"""

if __name__ == "__main__":
    import subprocess
    import os
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "../..")).returncode)


import os
import subprocess
import time
from threading import Thread

import globals
import PySide6.QtGui
from Interface.CustomWidgets.SpecificWidgets import *
from PackageManagers.PackageClasses import Package, PackageDetails, UpgradablePackage
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from tools import _


class PackageInstallerWidget(QWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
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
        self.addInfoLine.connect(lambda s: (self.liveOutputWindow.setPlainText(self.liveOutputWindow.toPlainText() + "\n" + s), self.liveOutputWindow.verticalScrollBar().setValue(self.liveOutputWindow.verticalScrollBar().maximum())))

        if getSettings(f"AlwaysElevate{self.Package.PackageManager.NAME}"):
            print(f"🟡 {self.Package.PackageManager.NAME} installation automatically elevated!")
            self.Options.RunAsAdministrator = True

        if getSettings("DoCacheAdminRights"):
            if self.Options.RunAsAdministrator and not globals.adminRightsGranted:
                cprint(" ".join([GSUDO_EXECUTABLE, "cache", "on", "--pid", f"{os.getpid()}", "-d", "-1"]))
                asksudo = subprocess.Popen([GSUDO_EXECUTABLE, "cache", "on", "--pid", f"{os.getpid()}", "-d", "-1"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                asksudo.wait()
                globals.adminRightsGranted = True

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
        self.addInfoLine.connect(lambda text: self.liveOutputButton.setText(text))
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
        if not self.Options.RunAsAdministrator and not globals.mainWindow.isAdmin():
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

        self.waitThread = KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        Thread(target=self.loadIconThread, daemon=True, name=f"Installer: loading icon for {package}").start()
        print(f"🟢 Waiting for install permission... title={self.Package.Name}, id={self.Package.Id}, installId={self.installId}")
        print("🔵 Given package:", package)
        print("🔵 Installation options:", options)

        ApplyMica(self.liveOutputWindowWindow.winId(), MicaTheme.DARK)

    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
            append = " "
            try:
                append += _("(Number {0} in the queue)").format(globals.pending_programs.index(self.installId))
            except ValueError:
                print(f"🔴 Package {self.Package.Id} not in globals.pending_programs")
            globals.pending_programs.index(self.installId)
            self.addInfoLine.emit(_("Waiting for other installations to finish...") + append)
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
        globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit(_("Running the installer..."))
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
        globals.tray_is_installing = False
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
        t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
        if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED:
            self.setProgressbarColor("#11945a" if isDark() else "#11945a")
            if returncode in (RETURNCODE_OPERATION_SUCCEEDED, RETURNCODE_NO_APPLICABLE_UPDATE_FOUND):
                t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                if globals.ENABLE_SUCCESS_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode=False)))
                self.liveOutputButton.setText(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                self.startCoolDown()
            if returncode == RETURNCODE_NEEDS_RESTART:
                t.setTitle(_("Restart required"))
                t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", ".") + " " + _("Restart your computer to finish the installation"))
                t.setSmallText(_("You may restart your computer later if you wish"))
                t.addAction(_("Restart now"), globals.mainWindow.askRestart)
                t.addAction(_("Restart later"), t.close)
                if globals.ENABLE_WINGETUI_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("restart_color", autoIconMode=False)))
                self.liveOutputButton.setText(_("Restart your PC to finish installation"))
                globals.tray_is_needs_restart = True
                update_tray_icon()
            if type(self) == PackageInstallerWidget:
                self.Package.PackageItem.setCheckState(0, Qt.CheckState.Unchecked)
                self.Package.PackageItem.setIcon(1, getMaskedIcon("installed_masked"))
                self.Package.PackageItem.setToolTip(1, _("This package is already installed") + " - " + self.Package.Name)

                if self.Package.Id not in globals.uninstall.IdPackageReference.keys():
                    print("🔵 Adding package to the uninstall section...")
                    globals.uninstall.addItem(self.Package)
                    globals.uninstall.updatePackageNumber()
        else:
            globals.tray_is_error = True
            update_tray_icon()
            self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
            self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
            self.err = CustomMessageBox(self.window())
            warnIcon = QIcon(getMedia("notif_warn"))
            t.addAction(_("Show details"), lambda: (globals.mainWindow.showWindow(-1)))
            t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.Package.Name))
            dialogData = {
                "titlebarTitle": _("WingetUI - {0} {1}").format(self.Package.Name, self.actionName),
                "buttonTitle": _("Close"),
                "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
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
            if globals.ENABLE_ERROR_NOTIFICATIONS:
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
        globals.installersWidget.removeItem(self)
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
        globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.addInfoLine.emit(_("Running the updater..."))
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
            globals.tray_is_installing = False
            update_tray_icon()
            self.leftSlow.stop()
            self.leftFast.stop()
            self.rightSlow.stop()
            self.rightFast.stop()
            self.progressbar.setValue(1000)
            if self.progressbar.invertedAppearance():
                self.progressbar.setInvertedAppearance(False)
            if self.Package.Version in (_("Unknown"), "Unknown"):
                IgnorePackageUpdates_SpecificVersion(self.Package.Id, self.Package.NewVersion, self.Package.Source)
            if returncode == RETURNCODE_NO_APPLICABLE_UPDATE_FOUND and not self.canceled:
                IgnorePackageUpdates_SpecificVersion(self.Package.Id, self.Package.NewVersion, self.Package.Source)
            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:
                UPDATES_SECTION: SoftwareSection = globals.updates
                try:
                    self.Package.PackageItem.setHidden(True)
                    self.Package.PackageItem.treeWidget().takeTopLevelItem(self.Package.PackageItem.treeWidget().indexOfTopLevelItem(self.Package.PackageItem))
                    UPDATES_SECTION.packageItems.remove(self.Package.PackageItem)
                    if self.Package.PackageItem in UPDATES_SECTION.showableItems:
                        UPDATES_SECTION.showableItems.remove(self.Package.PackageItem)
                except Exception as e:
                    report(e)
                UPDATES_SECTION.updatePackageNumber()
            super().finish(returncode, output)

    def close(self):
        self.liveOutputWindow.close()
        self.liveOutputWindowWindow.close()
        globals.installersWidget.removeItem(self)
        super().destroy()
        super().close()


class PackageUninstallerWidget(PackageInstallerWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
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
        globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.leftSlow.start()
        self.addInfoLine.emit(_("Running the uninstaller..."))
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
            globals.tray_is_installing = False
            update_tray_icon()
            self.leftSlow.stop()
            self.leftFast.stop()
            self.rightSlow.stop()
            self.rightFast.stop()
            self.progressbar.setValue(1000)
            if self.progressbar.invertedAppearance():
                self.progressbar.setInvertedAppearance(False)
            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:
                UPDATES_SECTION: SoftwareSection = globals.updates
                UNINSTALL_SECTION: SoftwareSection = globals.uninstall
                try:
                    print(self.Package.PackageItem, UNINSTALL_SECTION.packageList.indexOfTopLevelItem(self.Package.PackageItem))
                    self.Package.PackageItem.setHidden(True)
                    i = UNINSTALL_SECTION.packageList.takeTopLevelItem(UNINSTALL_SECTION.packageList.indexOfTopLevelItem(self.Package.PackageItem))
                    UNINSTALL_SECTION.packageItems.remove(self.Package.PackageItem)
                    if self.Package.PackageItem in UNINSTALL_SECTION.showableItems:
                        UNINSTALL_SECTION.showableItems.remove(self.Package.PackageItem)
                    del i
                    DISCOVER = globals.discover
                    if self.Package.Id in DISCOVER.IdPackageReference.keys():
                        discoverablePackage: UpgradablePackage = DISCOVER.IdPackageReference[self.Package.Id]
                        discoverableItem = discoverablePackage.PackageItem
                        if discoverableItem in DISCOVER.packageItems:
                            discoverableItem.setIcon(1, DISCOVER.installIcon)
                            discoverableItem.setToolTip(1, self.Package.Name)
                except Exception as e:
                    report(e)
                UNINSTALL_SECTION.updatePackageNumber()
                if self.Package.Id in UPDATES_SECTION.IdPackageReference:
                    packageItem = UPDATES_SECTION.PackageItemReference[UPDATES_SECTION.IdPackageReference[self.Package.Id]]
                    packageItem.setHidden(True)
                    i = UPDATES_SECTION.packageList.takeTopLevelItem(UPDATES_SECTION.packageList.indexOfTopLevelItem(packageItem))
                    try:
                        UPDATES_SECTION.packageItems.remove(packageItem)
                        if i in UPDATES_SECTION.showableItems:
                            UPDATES_SECTION.showableItems.remove(packageItem)
                        del i
                    except Exception as e:
                        report(e)
                    UPDATES_SECTION.updatePackageNumber()
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
                    self.setProgressbarColor("#11945a" if isDark() else "#11945a")
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode=False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.liveOutputButton.setText(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                    self.progressbar.setValue(1000)
                    self.startCoolDown()
                    t = ToastNotification(self, self.callInMain.emit)
                    t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                    t.setDescription(_("{0} was {1} successfully!").format(self.Package.Name, self.actionDone).replace("!", "."))
                    if globals.ENABLE_SUCCESS_NOTIFICATIONS:
                        t.show()
                else:
                    globals.tray_is_error = True
                    update_tray_icon()
                    self.setProgressbarColor("#fec10b" if isDark() else "#fec10b")
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode=False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.progressbar.setValue(1000)
                    self.err = CustomMessageBox(self.window())
                    t = ToastNotification(self, self.callInMain.emit)
                    t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.Package.Name))
                    t.setDescription(_("{0} {1} failed").format(self.Package.Name.capitalize(), self.actionName))
                    t.addAction(_("Retry"), lambda: (self.runInstallation(), self.cancelButton.setText(_("Cancel"))))
                    t.addAction(_("Show details"), lambda: (globals.mainWindow.showWindow(-1)))
                    errorData = {
                        "titlebarTitle": _("WingetUI - {0} {1}").format(self.Package.Name, self.actionName),
                        "mainTitle": _("{0} failed").format(self.actionName.capitalize()),
                        "mainText": _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the uninstaller.").format(action=self.actionVerb, package=self.Package.Name, showDetails=_("Show details")),
                        "buttonTitle": _("Close"),
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("notif_warn")),
                    }
                    if globals.ENABLE_ERROR_NOTIFICATIONS:
                        t.show()
                    self.err.showErrorMessage(errorData, showNotification=False)

    def close(self):
        self.liveOutputWindow.close()
        self.liveOutputWindowWindow.close()
        globals.installersWidget.removeItem(self)
        super().close()
        super().destroy()


class CustomInstallerWidget(PackageInstallerWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    callInMain = Signal(object)
    changeBarOrientation = Signal()

    def __init__(self, name: str, command: list, packageManager: PackageManagerModule, runAsAdministrator: bool = False):
        self.Package = Package(name, name, "N/A", packageManager.NAME, packageManager)
        self.Package.PackageItem = QTreeWidgetItem()
        self.Options = InstallationOptions()
        self.command = command
        if runAsAdministrator:
            self.Options.RunAsAdministrator = True
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit(_("Running the installer..."))
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
                self.addInfoLine.emit(line)
        self.finishInstallation.emit(p.returncode, output)


class CustomUninstallerWidget(PackageUninstallerWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    callInMain = Signal(object)
    changeBarOrientation = Signal()

    def __init__(self, name: str, command: list, packageManager: PackageManagerModule, runAsAdministrator: bool = False):
        self.Package = Package(name, name, "N/A", packageManager.NAME, packageManager)
        self.Package.PackageItem = QTreeWidgetItem()
        self.Options = InstallationOptions()
        if runAsAdministrator:
            self.Options.RunAsAdministrator = True

        self.command = command
        super().__init__(self.Package, self.Options)

    def runInstallation(self) -> None:
        globals.tray_is_installing = True
        self.callInMain.emit(update_tray_icon)
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.addInfoLine.emit(_("Running the uninstaller..."))
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
                self.addInfoLine.emit(line)
        self.finishInstallation.emit(p.returncode, output)


class ScoopBucketManager(QWidget):
    addBucketsignal = Signal(str, str, str, str)
    finishLoading = Signal()
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()

    def __init__(self):
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("stBtn")
        self.addBucketsignal.connect(self.addItem)
        layout = QVBoxLayout()
        hLayout = QHBoxLayout()
        hLayout.addWidget(QLabel(_("Manage scoop buckets")))
        hLayout.addStretch()

        self.loadingProgressBar = QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.loadingProgressBar.hide()
        self.finishLoading.connect(lambda: self.loadingProgressBar.hide())
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not self.loadingProgressBar.invertedAppearance()))

        self.reloadButton = QPushButton()
        self.reloadButton.clicked.connect(self.loadBuckets)
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setAccessibleName(_("Reload"))
        self.addBucketButton = QPushButton(_("Add bucket"))
        self.addBucketButton.setFixedHeight(30)
        self.addBucketButton.clicked.connect(self.scoopAddExtraBucket)
        hLayout.addWidget(self.addBucketButton)
        hLayout.addWidget(self.reloadButton)
        hLayout.setContentsMargins(10, 0, 15, 0)
        layout.setContentsMargins(60, 10, 5, 10)
        self.bucketList = TreeWidget()
        self.bucketList.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)

        self.bucketList.label.setText(_("Loading buckets..."))
        self.bucketList.label.setVisible(True)
        self.bucketList.setColumnCount(4)
        self.bucketList.setHeaderLabels([_("Name"), _("Source"), _("Update date"), _("Manifests"), _("Remove")])
        self.bucketList.sortByColumn(0, Qt.SortOrder.AscendingOrder)
        self.bucketList.setSortingEnabled(True)
        self.bucketList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.bucketList.setIconSize(QSize(24, 24))
        self.bucketList.setColumnWidth(0, 120)
        self.bucketList.setColumnWidth(1, 280)
        self.bucketList.setColumnWidth(2, 120)
        self.bucketList.setColumnWidth(3, 80)
        self.bucketList.setColumnWidth(4, 50)
        layout.addLayout(hLayout)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.bucketList)
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

        self.loadBuckets()
        self.ApplyIcons()
        self.registeredThemeEvent = False

    def ApplyIcons(self):
        if isDark():
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #222222; background-color: rgba(30, 30, 30, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        else:
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #f5f5f5; background-color: rgba(255, 255, 255, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
        self.bucketIcon = QIcon(getMedia("bucket"))
        self.reloadButton.click()

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.loadBuckets()
        return super().showEvent(event)

    def loadBuckets(self):
        if getSettings("DisableScoop"):
            return
        for i in range(self.bucketList.topLevelItemCount()):
            item = self.bucketList.takeTopLevelItem(0)
            del item
        Thread(target=Scoop.loadBuckets, args=(self.addBucketsignal, self.finishLoading), name="MAIN: Load scoop buckets").start()
        self.loadingProgressBar.show()
        self.bucketList.label.show()
        self.bucketList.label.setText("Loading...")
        globals.scoopBuckets = {}

    def addItem(self, name: str, source: str, updatedate: str, manifests: str):
        self.bucketList.label.hide()
        item = QTreeWidgetItem()
        item.setText(0, name)
        item.setToolTip(0, name)
        item.setIcon(0, self.bucketIcon)
        item.setText(1, source)
        item.setToolTip(1, source)
        item.setText(2, updatedate)
        item.setToolTip(2, updatedate)
        item.setText(3, manifests)
        item.setToolTip(3, manifests)
        self.bucketList.addTopLevelItem(item)
        btn = QPushButton()
        btn.clicked.connect(lambda: (self.scoopRemoveExtraBucket(name), self.bucketList.takeTopLevelItem(self.bucketList.indexOfTopLevelItem(item))))
        btn.setFixedSize(24, 24)
        btn.setIcon(QIcon(getMedia("menu_uninstall")))
        self.bucketList.setItemWidget(item, 4, btn)
        globals.scoopBuckets[name] = source

    def scoopAddExtraBucket(self) -> None:
        r = QInputDialog.getItem(self, _("Scoop bucket manager"), _("Which bucket do you want to add?") + " " + _("Select \"{item}\" to add your custom bucket").format(item=_("Another bucket")), ["main", "extras", "versions", "nirsoft", "php", "nerd-fonts", "nonportable", "java", "games", _("Another bucket")], 1, editable=False)
        if r[1]:
            bName = r[0]
            if r[0] == _("Another bucket"):
                r2 = QInputDialog.getText(self, _("Scoop bucket manager"), _("Type here the name and the URL of the bucket you want to add, separated by a space."), text="extras https://github.com/ScoopInstaller/Extras")
                if r2[1]:
                    bName = r2[0].split(" ")[0]
                    p = CustomInstallerWidget(f"{bName} Scoop bucket", f"scoop bucket add {r2[0]}", Scoop)
                    globals.installersWidget.addItem(p)
                    p.finishInstallation.connect(self.loadBuckets)
            else:
                p = CustomInstallerWidget(f"{bName} Scoop bucket", f"scoop bucket add {r[0]}", Scoop)
                globals.installersWidget.addItem(p)
                p.finishInstallation.connect(self.loadBuckets)

    def scoopRemoveExtraBucket(self, bucket: str) -> None:
        globals.installersWidget.addItem(CustomUninstallerWidget(f"{bucket} Scoop bucket", f"scoop bucket rm {bucket}", Scoop))


class WingetBucketManager(QWidget):
    addSourceSignal = Signal(str, str)
    finishLoading = Signal()
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()

    def __init__(self):
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("stBtn")
        self.addSourceSignal.connect(self.addItem)
        layout = QVBoxLayout()
        hLayout = QHBoxLayout()
        hLayout.addWidget(QLabel(_("Manage Winget buckets")))
        hLayout.addStretch()

        self.loadingProgressBar = QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.loadingProgressBar.hide()
        self.finishLoading.connect(lambda: self.loadingProgressBar.hide())
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not self.loadingProgressBar.invertedAppearance()))

        self.reloadButton = QPushButton()
        self.reloadButton.clicked.connect(self.loadSources)
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
        self.addBucketButton = QPushButton(_("Add source"))
        self.addBucketButton.setFixedHeight(30)
        self.addBucketButton.clicked.connect(self.wingetAddExtraSource)
        hLayout.addWidget(self.addBucketButton)
        hLayout.addWidget(self.reloadButton)
        hLayout.setContentsMargins(10, 0, 15, 0)
        layout.setContentsMargins(60, 10, 5, 10)
        self.bucketList = TreeWidget()
        self.bucketList.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)

        self.bucketList.label.setText(_("Loading sources..."))
        self.bucketList.label.show()
        self.bucketList.setColumnCount(3)
        self.bucketList.setHeaderLabels([_("Name"), _("Source"), " "])
        self.bucketList.sortByColumn(0, Qt.SortOrder.AscendingOrder)
        self.bucketList.setSortingEnabled(True)
        self.bucketList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.bucketList.setIconSize(QSize(24, 24))
        self.bucketList.setColumnWidth(0, 160)
        self.bucketList.setColumnWidth(1, 480)
        self.bucketList.setColumnWidth(2, 50)
        layout.addLayout(hLayout)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.bucketList)
        self.setLayout(layout)
        self.bucketIcon = QIcon(getMedia("list"))

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

        self.loadSources()

        self.registeredThemeEvent = False
        self.ApplyIcons()

    def ApplyIcons(self):
        if isDark():
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #222222; background-color: rgba(30, 30, 30, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        else:
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #f5f5f5; background-color: rgba(255, 255, 255, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
        self.bucketIcon = QIcon(getMedia("list"))
        self.reloadButton.click()

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.loadSources()
        return super().showEvent(event)

    def loadSources(self):
        if getSettings("DisableWinget"):
            return
        for i in range(self.bucketList.topLevelItemCount()):
            item = self.bucketList.takeTopLevelItem(0)
            del item
        Thread(target=Winget.loadSources, args=(self.addSourceSignal, self.finishLoading), name="MAIN: Load winget buckets").start()
        self.loadingProgressBar.show()
        self.bucketList.label.show()
        self.bucketList.label.setText("Loading...")
        globals.wingetSources = {}

    def addItem(self, name: str, url: str):
        self.bucketList.label.hide()
        item = QTreeWidgetItem()
        item.setText(0, name)
        item.setToolTip(0, name)
        item.setIcon(0, self.bucketIcon)
        item.setText(1, url)
        item.setToolTip(1, url)
        self.bucketList.addTopLevelItem(item)
        btn = QPushButton()
        btn.clicked.connect(lambda: (self.wingetRemoveExtraSource(name), self.bucketList.takeTopLevelItem(self.bucketList.indexOfTopLevelItem(item))))
        btn.setFixedSize(24, 24)
        btn.setIcon(QIcon(getMedia("menu_uninstall")))
        self.bucketList.setItemWidget(item, 2, btn)
        globals.wingetSources[name] = url

    def wingetAddExtraSource(self) -> None:
        sources = {
            "msstore": "https://storeedgefd.dsx.mp.microsoft.com/v9.0",
            "winget": "https://cdn.winget.microsoft.com/cache",
        }
        r = QInputDialog.getItem(self, _("Winget source manager"), _("Which source do you want to add?") + " " + _("Select \"{item}\" to add your custom bucket").format(item=_("Another source")), list(sources.keys()) + [_("Another source")], 1, editable=False)
        if r[1]:
            sourcename = r[0]
            if sourcename == _("Another source"):
                r2 = QInputDialog.getText(self, _("Winget source manager"), _("Type here the name and the URL of the source you want to add, separated by a space."), text="msstore https://storeedgefd.dsx.mp.microsoft.com/v9.0")
                if r2[1]:
                    p = CustomInstallerWidget(f"{sourcename} Winget source", [Winget.EXECUTABLE, "source", "add", r2[0].split(" ")[0], r2[0].split(" ")[1]], Winget, runAsAdministrator=True)
                    globals.installersWidget.addItem(p)
                    p.finishInstallation.connect(self.loadSources)
            else:
                p = CustomInstallerWidget(f"{sourcename} Winget source", [Winget.EXECUTABLE, "source", "add", sourcename, sources[sourcename]], Winget, runAsAdministrator=True)
                globals.installersWidget.addItem(p)
                p.finishInstallation.connect(self.loadSources)

    def wingetRemoveExtraSource(self, source: str) -> None:
        globals.installersWidget.addItem(CustomUninstallerWidget(f"{source} Winget source", [Winget.EXECUTABLE, "source", "remove", source], Winget, runAsAdministrator=True))
