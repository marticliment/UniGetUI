from __future__ import annotations
from functools import partial
import signal
import sys, subprocess, time, os, json
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from tools import _


from PackageManagers import winget, scoop, choco
from customWidgets import *
import globals
from PackageManagers.PackageClasses import Package, UpgradablePackage, PackageDetails

class PackageInstallerWidget(QGroupBox):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    callInMain = Signal(object)
    changeBarOrientation = Signal()
    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId="", admin: bool = False, useId: bool = False, packageItem: TreeWidgetItemWithQAction = None):
        super().__init__(parent=parent)
        self.packageItem = packageItem
        self.actionDone = _("installed")
        self.actionDoing = _("installing")
        self.actionName = _("installation")
        self.actionVerb = _("install")
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)
        self.liveOutputWindow = CustomPlainTextEdit(self)
        self.liveOutputWindow.setWindowFlag(Qt.Window)
        self.liveOutputWindow.setWindowIcon(self.window().windowIcon())
        self.liveOutputWindow.setReadOnly(True)
        self.liveOutputWindow.resize(500, 200)
        self.liveOutputWindow.setWindowTitle(_("Live command-line output"))
        self.addInfoLine.connect(lambda s: (self.liveOutputWindow.setPlainText(self.liveOutputWindow.toPlainText()+"\n"+s), self.liveOutputWindow.verticalScrollBar().setValue(self.liveOutputWindow.verticalScrollBar().maximum())))
        ApplyMica(self.liveOutputWindow.winId(), MICAMODE.DARK)
        self.runAsAdmin = admin
        self.useId = useId  
        self.adminstr = [GSUDO_EXE_PATH] if self.runAsAdmin else []
        
        if store.lower() == "winget" and getSettings("AlwaysElevateWinget"):
            print("🟡 Winget installation automatically elevated!")
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
        elif store.lower() == "chocolatey" and getSettings("AlwaysElevateChocolatey"):
            print("🟡 Chocolatey installation automatically elevated!")
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
        elif "scoop" in store.lower() and getSettings("AlwaysElevateScoop"):
            print("🟡 Chocolatey installation automatically elevated!")
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
        if getSettings("DoCacheAdminRights"):
            if self.runAsAdmin and not globals.adminRightsGranted:
                cprint(" ".join([GSUDO_EXE_PATH, "cache", "on", "--pid", f"{os.getpid()}", "-d" , "-1"]))
                asksudo = subprocess.Popen([GSUDO_EXE_PATH, "cache", "on", "--pid", f"{os.getpid()}", "-d" , "-1"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
                asksudo.wait()
                globals.adminRightsGranted = True

        self.finishedInstallation = True
        self.callInMain.connect(lambda f: f())
        self.setMinimumHeight(500)
        self.store = store.lower()
        self.customCommand = customCommand
        self.setObjectName("package")
        self.setFixedHeight(50)
        self.programName = title
        self.packageId = packageId
        self.version = version
        self.cmdline_args = args
        cprint("args")
        self.layout = QHBoxLayout()
        self.layout.setContentsMargins(30, 10, 10, 10)
        self.label = QLabel(_("{0} installation").format(title))
        self.layout.addWidget(self.label)
        self.layout.addSpacing(5)
        self.progressbar = QProgressBar()
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 1000)
        self.progressbar.setValue(0)
        self.progressbar.setFixedHeight(4)
        self.changeBarOrientation.connect(lambda: self.progressbar.setInvertedAppearance(not(self.progressbar.invertedAppearance())))
        self.layout.addWidget(self.progressbar, stretch=1)
        self.adminBadge = QPushButton()
        self.adminBadge.setFixedSize(QSize(30, 30))
        self.adminBadge.setIcon(QIcon(getMedia("runasadmin")))
        self.adminBadge.setEnabled(False)
        self.adminBadge.setToolTip(_("This process is running with administrator privileges"))
        self.layout.addWidget(self.adminBadge)
        if not self.runAsAdmin:
            self.adminBadge.setVisible(False)
        self.info = CustomLineEdit()
        self.info.setClearButtonEnabled(False)
        self.info.setStyleSheet("color: grey; border-bottom: inherit;")
        self.info.setText(_("Waiting for other installations to finish..."))
        self.info.setReadOnly(True)
        self.addInfoLine.connect(lambda text: self.info.setText(text))
        self.finishInstallation.connect(self.finish)
        self.layout.addWidget(self.info)
        self.counterSignal.connect(self.counter)
        self.liveOutputButton = QPushButton(QIcon(getMedia("console", autoIconMode = False)), "")
        self.liveOutputButton.clicked.connect(lambda: (self.liveOutputWindow.show(), ApplyMica(self.liveOutputWindow.winId(), isDark())))
        self.liveOutputButton.setFixedHeight(30)
        self.liveOutputButton.setFixedWidth(30)
        self.liveOutputButton.setToolTip(_("Show the live output"))
        self.liveOutputButton.setIcon(QIcon(getMedia("console")))
        self.layout.addWidget(self.liveOutputButton)
        self.cancelButton = QPushButton(QIcon(getMedia("cancel", autoIconMode = False)), _("Cancel"))
        self.cancelButton.clicked.connect(self.cancel)
        self.cancelButton.setFixedHeight(30)
        self.info.setFixedHeight(30)
        self.layout.addWidget(self.cancelButton)
        self.setLayout(self.layout)
        self.canceled = False
        self.installId = str(time.time())
        queueProgram(self.installId)

        self.leftSlow = QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(900)
        self.leftSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))

        self.rightSlow = QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(900)
        self.rightSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))

        self.leftFast = QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))

        self.leftSlow.start()

        self.waitThread = KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        print(f"🟢 Waiting for install permission... title={self.programName}, id={self.packageId}, installId={self.installId}")

    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("🟢 Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.finishedInstallation = False
        self.callInMain.emit(lambda: self.liveOutputWindow.setPlainText(""))
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.addInfoLine.emit(_("Starting installation..."))
        self.progressbar.setValue(0)
        self.packageId = self.packageId.replace("…", "")
        self.programName = self.programName.replace("…", "")
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        if(self.store.lower() == "winget"):
            if self.useId:
                self.p = subprocess.Popen(self.adminstr + [winget.EXECUTABLE, "install", "-e", "--id", f"{self.packageId}"] + self.version + winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [winget.EXECUTABLE, "install", "-e", "--name", f"{self.programName}"] + self.version + winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=winget.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            cprint(self.store.lower())
            bucket_prefix = ""
            if len(self.store.lower().split(":"))>1 and not "/" in self.packageId:
                bucket_prefix = self.store.lower().split(":")[1].replace(" ", "")+"/"
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "install", f"{bucket_prefix+self.packageId if self.packageId != '' else bucket_prefix+self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=scoop.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [choco.EXECUTABLE, "install", self.packageId, "-y"] + self.version + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=choco.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()


    def counter(self, line: int) -> None:
        if(line == 1):
            self.progressbar.setValue(250)
        if(line == 4):
            self.progressbar.setValue(500)
        elif(line == 6):
            self.cancelButton.setEnabled(True)
            self.progressbar.setValue(750)

    def cancel(self):
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        print("🔵 Sending cancel signal...")
        if not self.finishedInstallation:
            try:
                os.kill(self.p.pid, signal.CTRL_C_EVENT)
            except Exception as e:
                report(e)
        self.finishedInstallation = True
        self.info.setText(_("Installation canceled by the user!"))
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText(_("Close"))
        self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: os.kill(self.p.pid, signal.CTRL_C_EVENT)
        except: pass

    def finish(self, returncode: int, output: str = "") -> None:
        if returncode == RETURNCODE_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.cmdline_args.append("--global")
            self.runInstallation()
            return
        elif returncode == RETURNCODE_NEEDS_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
            return
        elif "winget settings --enable InstallerHashOverride" in output:
            print("🟠 Requiring the user to enable skiphash setting!")
            subprocess.run([GSUDO_EXE_PATH, winget.EXECUTABLE, "settings", "--enable", "InstallerHashOverride"], shell=True)
            self.runInstallation()
            return
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: os.kill(self.p.pid, signal.CTRL_C_EVENT)
        except: pass
        if self.canceled:
            return
        self.cancelButton.setText(_("OK"))
        self.cancelButton.clicked.connect(self.close)
        self.progressbar.setValue(1000)
        t = ToastNotification(self, self.callInMain.emit)
        t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
        if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED:
            if returncode in (RETURNCODE_OPERATION_SUCCEEDED, RETURNCODE_NO_APPLICABLE_UPDATE_FOUND):
                t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                t.setDescription(_("{0} was {1} successfully!").format(self.programName, self.actionDone).replace("!", "."))
                if ENABLE_SUCCESS_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode = False)))
                self.info.setText(_("{action} was successfully!").format(action = self.actionDone.capitalize()))
                self.startCoolDown()
            if returncode == RETURNCODE_NEEDS_RESTART: # if the installer need restart computer
                t.setTitle(_("Restart required"))
                t.setDescription(_("{0} was {1} successfully!").format(self.programName, self.actionDone).replace("!", ".")+" "+_("Restart your computer to finish the installation"))
                t.setSmallText(_("You may restart your computer later if you wish"))
                t.addAction(_("Restart now"), globals.mainWindow.askRestart) #TODO: add restart pc
                t.addAction(_("Restart later"), t.close)
                if ENABLE_WINGETUI_NOTIFICATIONS:
                    t.show()
                self.cancelButton.setIcon(QIcon(getMedia("restart_color", autoIconMode = False)))
                self.info.setText(_("Restart your PC to finish installation"))
            if type(self) == PackageInstallerWidget:
                if self.packageItem:
                    if not self.packageItem.text(2) in globals.uninstall.packages.keys():
                        globals.uninstall.addItem(self.packageItem.text(1), self.packageItem.text(2), self.packageItem.text(3), self.packageItem.text(4)) # Add the package on the uninstaller
                        globals.uninstall.updatePackageNumber()
        else:
            globals.trayIcon.setIcon(QIcon(getMedia("yellowicon")))
            self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
            self.err = CustomMessageBox(self.window())
            warnIcon = QIcon(getMedia("notif_warn"))
            t.addAction(_("Show details"), lambda: (globals.mainWindow.showWindow(-1)))
            t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.programName))
            dialogData = {
                    "titlebarTitle": _("WingetUI - {0} {1}").format(self.programName, self.actionName),
                    "buttonTitle": _("Close"),
                    "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                    "icon": warnIcon,
                    "notifTitle": _("Can't {0} {1}").format(self.actionVerb, self.programName),
                    "notifIcon": warnIcon,
            }
            if returncode == RETURNCODE_INCORRECT_HASH: # if the installer's hash does not coincide
                t.setDescription(_("The installer has an invalid checksum"))
                dialogData["mainTitle"] = _("{0} aborted").format(self.actionName.capitalize())
                dialogData["mainText"] = _("The checksum of the installer does not coincide with the expected value, and the authenticity of the installer can't be verified. If you trust the publisher, {0} the package again skipping the hash check.").format(self.actionVerb)
            else: # if there's a generic error
                t.setDescription(_("{0} {1} failed").format(self.programName.capitalize(), self.actionName))
                t.addAction(_("Retry"), lambda: (self.runInstallation(), self.cancelButton.setText(_("Cancel"))))
                dialogData["mainTitle"] = _("{0} failed").format(self.actionName.capitalize())
                dialogData["mainText"] = _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the installer.").format(action=self.actionVerb, package=self.programName, showDetails=_("Show details"))
            self.err.showErrorMessage(dialogData, showNotification=False)
            if ENABLE_ERROR_NOTIFICATIONS:
                t.show()


    def startCoolDown(self):
        if not getSettings("MaintainSuccessfulInstalls"):
            self.ops = -1
            def setUpOPS():
                op1=QGraphicsOpacityEffect(self)
                op2=QGraphicsOpacityEffect(self)
                op3=QGraphicsOpacityEffect(self)
                op4=QGraphicsOpacityEffect(self)
                op5=QGraphicsOpacityEffect(self)
                op6=QGraphicsOpacityEffect(self)
                ops = [op1, op2, op3, op4, op5, op6]
                return ops

            def updateOp(v: float):
                i = 0
                if self.ops == -1:
                    self.ops = setUpOPS()
                for widget in [self.cancelButton, self.label, self.progressbar, self.info, self.liveOutputButton, self.adminBadge]:
                    self.ops[i].setOpacity(v)
                    widget: QWidget
                    widget.setGraphicsEffect(self.ops[i])
                    widget.setAutoFillBackground(True)
                    i += 1
                    if v == 0:
                        widget.hide()

            #updateOp(1)
            a = QVariantAnimation(self)
            a.setStartValue(1.0)
            a.setEndValue(0.0)
            a.setEasingCurve(QEasingCurve.InOutQuad)
            a.setDuration(200)
            a.valueChanged.connect(lambda v: updateOp(v))
            a.finished.connect(self.heightAnim)
            f = lambda: (time.sleep(3), self.callInMain.emit(a.start))
            Thread(target=f, daemon=True).start()
        else:
            print("🟡 Autohide disabled!")

    def heightAnim(self):
        op=QGraphicsOpacityEffect(self)
        self.setGraphicsEffect(op)
        a = QVariantAnimation(self)
        a.setStartValue(100)
        a.setEndValue(0)
        a.setEasingCurve(QEasingCurve.InOutQuad)
        a.setDuration(100)
        a.valueChanged.connect(lambda v: op.setOpacity(v/100))
        a.finished.connect(self.close)
        a.start()

    def close(self):
        globals.installersWidget.removeItem(self)
        super().close()
        self.liveOutputWindow.close()
        super().destroy()

class PackageUpdaterWidget(PackageInstallerWidget):

    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId="", packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, useId: bool = False, currentVersion: str = "", newVersion: str = ""):
        self.currentVersion = currentVersion
        self.newVersion = newVersion
        super().__init__(title, store, version, parent, customCommand, args, packageId, admin, useId)
        self.packageItem = packageItem
        self.actionDone = _("updated")
        self.actionDoing = _("updating")
        self.actionName = _("update(noun)")
        self.actionVerb = _("update(verb)")
        self.label.setText(_("{0} update").format(title))

    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("🟢 Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.finishedInstallation = False
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.addInfoLine.emit(_("Applying update..."))
        self.rightFast.stop()
        self.progressbar.setValue(0)
        self.packageId = self.packageId.replace("…", "")
        self.programName = self.programName.replace("…", "")
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        if(self.store.lower() == "winget"):
            print(self.adminstr)
            if self.useId:
                self.p = subprocess.Popen(self.adminstr + [winget.EXECUTABLE, "upgrade", "-e", "--id", f"{self.packageId}", "--include-unknown"] + self.version + winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [winget.EXECUTABLE, "upgrade", "-e", "--name", f"{self.programName}", "--include-unknown"] + self.version + winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=winget.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "update", f"{self.packageId if self.packageId != '' else self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=scoop.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [choco.EXECUTABLE, "upgrade", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=choco.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

    def finish(self, returncode: int, output: str = "") -> None:
        if returncode == RETURNCODE_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.cmdline_args.append("--global")
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        elif returncode == RETURNCODE_NEEDS_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        else:
            if self.currentVersion in (_("Unknown"), "Unknown"):
                IgnorePackageUpdates_SpecificVersion(self.packageId, self.newVersion, self.store)
            if returncode == RETURNCODE_NO_APPLICABLE_UPDATE_FOUND and not self.canceled:
                IgnorePackageUpdates_SpecificVersion(self.packageId, self.newVersion, self.store)
            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:
                if self.packageItem:
                    try:
                        self.packageItem.setHidden(True)
                        i = self.packageItem.treeWidget().takeTopLevelItem(self.packageItem.treeWidget().indexOfTopLevelItem(self.packageItem))
                        del i
                    except Exception as e:
                        report(e)
                    globals.updates.updatePackageNumber()
            super().finish(returncode, output)

    def close(self):
        globals.installersWidget.removeItem(self)
        super().destroy()
        self.liveOutputWindow.close()
        super().close()

class PackageUninstallerWidget(PackageInstallerWidget):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    changeBarOrientation = Signal()
    def __init__(self, title: str, store: str, useId=False, packageId = "", packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, removeData: bool = False, args: list = [], customCommand = ""):
        self.packageItem = packageItem
        self.programName = title
        self.packageId = packageId
        super().__init__(parent=None, title=title, store=store, packageId=packageId, admin=admin, args=args, packageItem=packageItem, customCommand=customCommand)
        self.useId = useId
        self.actionDone = _("uninstalled")
        self.removeData = removeData
        self.actionDoing = _("uninstalling")
        self.actionName = _("uninstallation")
        self.actionVerb = _("uninstall")
        self.finishedInstallation = True
        self.runAsAdmin = admin
        self.store = store.lower()
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(50)
        self.label.setText(_("{0} Uninstallation").format(title))

    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("🟢 Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.packageId = self.packageId.replace("…", "")
        self.programName = self.programName.replace("…", "")
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        self.finishedInstallation = False
        if(self.store == "winget" or self.store in ((_("Local PC").lower(), "microsoft store", "steam", "gog", "ubisoft connect"))):
            self.p = subprocess.Popen(self.adminstr + [winget.EXECUTABLE, "uninstall", "-e"] + (["--id", self.packageId] if self.useId else ["--name", self.programName]) + ["--accept-source-agreements"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=winget.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
            print(self.p.args)
        elif("scoop" in self.store):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "uninstall", f"{self.packageId if self.packageId != '' else self.programName}"] + (["-p"] if self.removeData else [""]) + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=scoop.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [choco.EXECUTABLE, "uninstall", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=choco.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()



    def counter(self, line: int) -> None:
        if(line == 1):
            self.progressbar.setValue(250)
        if(line == 4):
            self.progressbar.setValue(500)
        elif(line == 6):
            self.cancelButton.setEnabled(True)
            self.progressbar.setValue(750)

    def cancel(self):
        print("🔵 Sending cancel signal...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.info.setText(_("Uninstall canceled by the user!"))
        if not self.finishedInstallation:
            try:
                os.kill(self.p.pid, signal.CTRL_C_EVENT)
            except Exception as e:
                report(e)
            self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText(_("Close"))
        self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: os.kill(self.p.pid, signal.CTRL_C_EVENT)
        except: pass

    def finish(self, returncode: int, output: str = "") -> None:
        if returncode == RETURNCODE_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.cmdline_args.append("--global")
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        elif returncode == RETURNCODE_NEEDS_ELEVATION:
            self.adminstr = [GSUDO_EXE_PATH]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        else:
            if returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED and not self.canceled:
                if self.packageItem:
                    try:
                        self.packageItem.setHidden(True)
                        i = self.packageItem.treeWidget().takeTopLevelItem(self.packageItem.treeWidget().indexOfTopLevelItem(self.packageItem))
                        del i
                        globals.uninstall.updatePackageNumber()
                        if self.packageId in globals.updates.packages:
                            packageItem = globals.updates.packages[self.packageId]["item"]
                            packageItem.setHidden(True)
                            i = packageItem.treeWidget().takeTopLevelItem(packageItem.treeWidget().indexOfTopLevelItem(packageItem))
                            del i
                            globals.updates.updatePackageNumber()

                    except Exception as e:
                        report(e)
            self.finishedInstallation = True
            self.cancelButton.setEnabled(True)
            removeProgram(self.installId)
            try: self.waitThread.kill()
            except: pass
            try: self.t.kill()
            except: pass
            try: os.kill(self.p.pid, signal.CTRL_C_EVENT)
            except: pass
            if not(self.canceled):
                if(returncode in LIST_RETURNCODES_OPERATION_SUCCEEDED):
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode = False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.info.setText(_("{action} was successfully!").format(action = self.actionDone.capitalize()))
                    self.progressbar.setValue(1000)
                    self.startCoolDown()
                    t = ToastNotification(self, self.callInMain.emit)                    
                    t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("{0} succeeded").format(self.actionName.capitalize()))
                    t.setDescription(_("{0} was {1} successfully!").format(self.programName, self.actionDone).replace("!", "."))
                    if ENABLE_SUCCESS_NOTIFICATIONS:
                        t.show()
                else:
                    globals.trayIcon.setIcon(QIcon(getMedia("yellowicon")))
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.progressbar.setValue(1000)
                    self.err = CustomMessageBox(self.window())
                    t = ToastNotification(self, self.callInMain.emit)                    
                    t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(-1)))
                    t.setTitle(_("Can't {0} {1}").format(self.actionVerb, self.programName))           
                    t.setDescription(_("{0} {1} failed").format(self.programName.capitalize(), self.actionName))
                    t.addAction(_("Retry"), lambda: (self.runInstallation(), self.cancelButton.setText(_("Cancel"))))
                    t.addAction(_("Show details"), lambda: (globals.mainWindow.showWindow(-1)))
                    errorData = {
                        "titlebarTitle": _("WingetUI - {0} {1}").format(self.programName, self.actionName),
                        "mainTitle": _("{0} failed").format(self.actionName.capitalize()),
                        "mainText": _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the uninstaller.").format(action=self.actionVerb, package=self.programName, showDetails=_("Show details")),
                        "buttonTitle": _("Close"),
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("notif_warn")),
                        }
                    if ENABLE_ERROR_NOTIFICATIONS:
                        t.show()
                    self.err.showErrorMessage(errorData, showNotification=False)

    def close(self):
        globals.installersWidget.removeItem(self)
        self.liveOutputWindow.close()
        super().close()
        super().destroy()

class PackageInfoPopupWindow(QWidget):
    onClose = Signal()
    loadInfo = Signal(dict, str)
    closeDialog = Signal()
    addProgram = Signal(PackageInstallerWidget)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    callInMain = Signal(object)
    finishedCount: int = 0
    backgroundApplied: bool = False
    isAnUpdate = False
    isAnUninstall = False
    currentPackage: Package = None

    pressed = False
    oldPos = QPoint(0, 0)
    
    def __init__(self, parent):
        super().__init__(parent = parent)
        self.iv = ImageViewer(self.window())
        self.callInMain.connect(lambda f: f())
        self.baseScrollArea = SmoothScrollArea()
        self.blurBackgroundEffect = QGraphicsBlurEffect()
        self.setObjectName("bg")
        self.sct = QShortcut(QKeySequence("Esc"), self.baseScrollArea)
        self.sct.activated.connect(lambda: self.close())
        self.baseScrollArea.setWidgetResizable(True)
        self.baseScrollArea.setStyleSheet(f"""
        QGroupBox {{
            border: 0px;
        }}
        QScrollArea{{
            border-radius: 5px;
            padding: 5px;
            background-color: {'rgba(30, 30, 30, 50%)' if isDark() else 'rgba(255, 255, 255, 50%)'};
            border-radius: 16px;
            border: 1px solid {"#303030" if isDark() else "#bbbbbb"};
        }}
        """)

        self.loadingProgressBar = QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))

        self.vLayout = QVBoxLayout()
        self.layout = QVBoxLayout()
        self.title = QLinkLabel()
        self.title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.title.setText(_("Loading..."))

        self.appIcon = QLabel()
        self.appIcon.setFixedSize(QSize(96, 96))
        self.appIcon.setStyleSheet(f"padding: 16px; border-radius: 16px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'rgba(255, 255, 255, 60%)'};")
        self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64))
        self.appIcon.setAlignment(Qt.AlignmentFlag.AlignVCenter | Qt.AlignmentFlag.AlignHCenter)

        fortyWidget = QWidget()
        fortyWidget.setFixedWidth(120)

        fortyTopWidget = QWidget()
        fortyTopWidget.setFixedWidth(120)
        fortyTopWidget.setMinimumHeight(30)

        self.mainGroupBox = QGroupBox()
        self.mainGroupBox.setFlat(True)

        hl = QHBoxLayout()
        hl.addWidget(self.appIcon)
        hl.addSpacing(16)
        hl.addWidget(self.title)

        self.layout.addLayout(hl)
        self.layout.addStretch()

        self.hLayout = QHBoxLayout()
        self.oLayout = QHBoxLayout()
        self.description = QLinkLabel("<b>"+_('Description:')+"</b> "+_('Unknown'))
        self.description.setWordWrap(True)

        self.layout.addWidget(self.description)

        self.homepage = QLinkLabel("<b>"+_('Homepage')+":</b> "+_('Unknown'))
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = QLinkLabel("<b>"+_('Publisher')+":</b> "+_('Unknown'))
        self.publisher.setOpenExternalLinks(False)
        self.publisher.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = QLinkLabel("<b>"+_('Author')+":</b> "+_('Unknown'))
        self.author.setOpenExternalLinks(False)
        self.author.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
        self.author.setWordWrap(True)

        self.layout.addWidget(self.author)
        self.layout.addSpacing(10)

        self.license = QLinkLabel("<b>"+_('License')+":</b> "+_('Unknown'))
        self.license.setWordWrap(True)

        self.layout.addWidget(self.license)
        self.layout.addSpacing(10)

        self.screenshotsWidget = QScrollArea()
        self.screenshotsWidget.setWidgetResizable(True)
        self.screenshotsWidget.setStyleSheet(f"QScrollArea{{padding: 8px; border-radius: 8px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'rgba(255, 255, 255, 60%)'};border: 0px solid black;}};")
        self.screenshotsWidget.setFixedHeight(150)
        self.screenshotsWidget.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.screenshotsWidget.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.layout.addWidget(self.screenshotsWidget)
        self.centralwidget = QWidget(self)

        self.blackCover = QWidget(self.centralwidget)
        self.blackCover.setStyleSheet("border: none;border-radius: 16px; margin: 0px;background-color: rgba(0, 0, 0, 30%);")
        self.blackCover.hide()
        blackCover = self.blackCover

        self.imagesLayout = QHBoxLayout()
        self.imagesLayout.setContentsMargins(0, 0, 0, 0)
        self.imagesLayout.setSpacing(0)
        self.imagesWidget = QWidget()
        self.imagesWidget.setLayout(self.imagesLayout)
        self.screenshotsWidget.setWidget(self.imagesWidget)
        self.imagesLayout.addStretch()

        class LabelWithImageViewer(QLabel):
            currentPixmap = QPixmap()
            index = 0
            def __init__(self, parent: QWidget):
                super().__init__()
                self.parentwidget: PackageInfoPopupWindow = parent
                self.clickableButton = QPushButton(self)
                self.setMinimumWidth(0)
                self.clickableButton.clicked.connect(self.showBigImage)
                self.clickableButton.setStyleSheet(f"QPushButton{{background-color: rgba(127, 127, 127, 1%);border: 0px;border-radius: 0px;}}QPushButton:hover{{background-color: rgba({'255, 255, 255' if not isDark() else '0, 0, 0'}, 10%)}}")

            def resizeEvent(self, event: QResizeEvent) -> None:
                self.clickableButton.move(0, 0)
                self.clickableButton.resize(self.size())
                return super().resizeEvent(event)

            def showBigImage(self):
                cprint(self.index)
                self.parentwidget.iv.show(self.index)
                self.parentwidget.iv.raise_()

            def setPixmap(self, arg__1: QPixmap, index = 0) -> None:
                self.index = index
                self.currentPixmap = arg__1
                if arg__1.isNull():
                    self.hide()
                super().setPixmap(arg__1.scaledToHeight(self.height(), Qt.SmoothTransformation))

            def showEvent(self, event: QShowEvent) -> None:
                if self.pixmap().isNull():
                    self.hide()
                return super().showEvent(event)

        self.imagesCarrousel: list[LabelWithImageViewer] = []
        for i in range(20):
            l = LabelWithImageViewer(self)
            l.setStyleSheet("border-radius: 4px;margin: 0px;margin-right: 4px;")
            self.imagesCarrousel.append(l)
            self.imagesLayout.addWidget(l)

        self.contributeLabel = QLabel()
        self.contributeLabel.setText(f"""{_('Is this package missing the icon?')}<br>{_('Are these screenshots wron or blurry?')}<br>{_('The icons and screenshots are maintained by users like you!')}<br><a  style=\"color: {blueColor};\" href=\"https://github.com/marticliment/WingetUI/wiki/Home#the-icon-and-screenshots-database\">{_('Contribute to the icon and screenshot repository')}</a>
        """)
        self.contributeLabel.setAlignment(Qt.AlignCenter | Qt.AlignVCenter)
        self.contributeLabel.setOpenExternalLinks(True)
        self.imagesLayout.addWidget(self.contributeLabel)
        self.imagesLayout.addStretch()

        self.imagesScrollbar = CustomScrollBar()
        self.imagesScrollbar.setOrientation(Qt.Horizontal)
        self.screenshotsWidget.setHorizontalScrollBar(self.imagesScrollbar)
        self.imagesScrollbar.move(self.screenshotsWidget.x(), self.screenshotsWidget.y()+self.screenshotsWidget.width()-16)
        self.imagesScrollbar.show()
        self.imagesScrollbar.setFixedHeight(12)

        self.layout.addWidget(self.imagesScrollbar)

        hLayout = QHBoxLayout()

        downloadGroupBox = QGroupBox()
        downloadGroupBox.setFlat(True)
        
        optionsSection = SmallCollapsableSection(_("Installation options"), getMedia("options"))

        self.hashCheckBox = QCheckBox()
        self.hashCheckBox.setText(_("Skip hash check"))
        self.hashCheckBox.setChecked(False)
        self.hashCheckBox.clicked.connect(self.loadPackageCommandLine)

        self.interactiveCheckbox = QCheckBox()
        self.interactiveCheckbox.setText(_("Interactive installation"))
        self.interactiveCheckbox.setChecked(False) 
        self.interactiveCheckbox.clicked.connect(self.loadPackageCommandLine)

        self.adminCheckbox = QCheckBox()
        self.adminCheckbox.setText(_("Run as admin"))
        self.adminCheckbox.setChecked(False)
        self.adminCheckbox.clicked.connect(self.loadPackageCommandLine)

        firstRow = SectionHWidget()
        firstRow.addWidget(self.hashCheckBox)
        firstRow.addWidget(self.interactiveCheckbox)
        firstRow.addWidget(self.adminCheckbox)
        
        optionsSection.addWidget(firstRow)

        self.commandWindow = CommandLineEdit()
        self.commandWindow.setReadOnly(True)
        
        commandWidget = SectionHWidget(lastOne = True)
        commandWidget.addWidget(self.commandWindow)
        
        
        self.versionLabel = QLabel(_("Version to install:"))
        self.versionCombo = CustomComboBox()
        self.versionCombo.setFixedWidth(150)
        self.versionCombo.setIconSize(QSize(24, 24))
        self.versionCombo.setFixedHeight(30)
        versionSection = SectionHWidget()
        versionSection.addWidget(self.versionLabel)
        versionSection.addWidget(self.versionCombo)
        versionSection.setFixedHeight(50)
        
        self.ignoreFutureUpdates = QCheckBox()
        self.ignoreFutureUpdates.setText(_("Ignore future updates for this package"))
        self.ignoreFutureUpdates.setChecked(False)
        
        ignoreUpdatesSection = SectionHWidget()
        ignoreUpdatesSection.addWidget(self.ignoreFutureUpdates)
        
        self.architectureLabel = QLabel(_("Architecture to install:"))
        self.architectureCombo = CustomComboBox()
        self.architectureCombo.setFixedWidth(150)
        self.architectureCombo.setIconSize(QSize(24, 24))
        self.architectureCombo.setFixedHeight(30)
        architectureSection = SectionHWidget()
        architectureSection.addWidget(self.architectureLabel)
        architectureSection.addWidget(self.architectureCombo)
        architectureSection.setFixedHeight(50)
        
        self.scopeLabel = QLabel(_("Installation scope:"))
        self.scopeCombo = CustomComboBox()
        self.scopeCombo.setFixedWidth(150)
        self.scopeCombo.setIconSize(QSize(24, 24))
        self.scopeCombo.setFixedHeight(30)
        scopeSection = SectionHWidget()
        scopeSection.addWidget(self.scopeLabel)
        scopeSection.addWidget(self.scopeCombo)
        scopeSection.setFixedHeight(50)
        
        customArgumentsSection = SectionHWidget()
        customArgumentsLabel = QLabel(_("Custom command-line arguments:"))
        self.customArgumentsLineEdit = CustomLineEdit()
        self.customArgumentsLineEdit.textChanged.connect(self.loadPackageCommandLine)
        self.customArgumentsLineEdit.setFixedHeight(30)
        customArgumentsSection.addWidget(customArgumentsLabel)
        customArgumentsSection.addWidget(self.customArgumentsLineEdit)
        customArgumentsSection.setFixedHeight(50)
        
        
        optionsSection.addWidget(versionSection)
        optionsSection.addWidget(ignoreUpdatesSection)
        optionsSection.addWidget(architectureSection)
        optionsSection.addWidget(scopeSection)
        optionsSection.addWidget(customArgumentsSection)
        optionsSection.addWidget(commandWidget)
        
        self.shareButton = QPushButton(_("Share this package"))
        self.shareButton.setIcon(QIcon(getMedia("share")))
        self.shareButton.setFixedWidth(200)
        self.shareButton.setStyleSheet("border-radius: 8px;")
        self.shareButton.setFixedHeight(35)
        self.shareButton.clicked.connect(lambda: nativeWindowsShare(self.title.text(), f"https://marticliment.com/wingetui/share?pid={self.currentPackage.Id}^&pname={self.currentPackage.Name}", self.window()))
        self.installButton = QPushButton()
        self.installButton.setText(_("Install"))
        self.installButton.setObjectName("AccentButton")
        self.installButton.setStyleSheet("border-radius: 8px;")
        self.installButton.setIconSize(QSize(24, 24))
        self.installButton.clicked.connect(self.install)
        self.installButton.setFixedWidth(200)
        self.installButton.setFixedHeight(35)
        
        hLayout.addWidget(self.shareButton)
        hLayout.addStretch()
        hLayout.addWidget(self.installButton)

        vl = QVBoxLayout()
        vl.addStretch()
        vl.addLayout(hLayout)

        vl.addStretch()

        downloadGroupBox.setLayout(vl)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addWidget(optionsSection)

        self.layout.addSpacing(10)

        self.packageId = QLinkLabel("<b>"+_('Package ID')+"</b> "+_('Unknown'))
        self.packageId.setWordWrap(True)
        self.layout.addWidget(self.packageId)
        self.manifest = QLinkLabel("<b>"+_('Manifest')+"</b> "+_('Unknown'))
        self.manifest.setWordWrap(True)
        self.layout.addWidget(self.manifest)
        self.lastver = QLinkLabel("<b>"+_('Latest Version:')+"</b> "+_('Unknown'))
        self.lastver.setWordWrap(True)
        self.layout.addWidget(self.lastver)
        self.sha = QLinkLabel(f"<b>{_('Installer SHA256')} ({_('Latest Version')}):</b> "+_('Unknown'))
        self.sha.setWordWrap(True)
        self.layout.addWidget(self.sha)
        self.link = QLinkLabel(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> "+_('Unknown'))
        self.link.setWordWrap(True)
        self.layout.addWidget(self.link)
        self.type = QLinkLabel(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> "+_('Unknown'))
        self.type.setWordWrap(True)
        self.layout.addWidget(self.type)
        self.date = QLinkLabel("<b>"+_('Last updated:')+"</b> "+_('Unknown'))
        self.date.setWordWrap(True)
        self.layout.addWidget(self.date)
        self.notes = QLinkLabel("<b>"+_('Release notes:')+"</b> "+_('Unknown'))
        self.notes.setWordWrap(True)
        self.layout.addWidget(self.notes)
        self.notesurl = QLinkLabel("<b>"+_('Release notes URL:')+"</b> "+_('Unknown'))
        self.notesurl.setWordWrap(True)
        self.layout.addWidget(self.notesurl)

        self.storeLabel = QLinkLabel("<b>"+_("Source:")+"</b> ")
        self.storeLabel.setWordWrap(True)
        self.layout.addWidget(self.storeLabel)

        self.layout.addSpacing(10)
        self.layout.addStretch()
        self.advert = QLinkLabel("<b>"+_("DISCLAIMER: WE ARE NOT RESPONSIBLE FOR THE DOWNLOADED PACKAGES. PLEASE MAKE SURE TO INSTALL ONLY TRUSTED SOFTWARE."))
        self.advert.setWordWrap(True)
        self.layout.addWidget(self.advert)

        self.mainGroupBox.setLayout(self.layout)
        self.mainGroupBox.setMinimumHeight(480)
        self.vLayout.addWidget(self.mainGroupBox)
        self.hLayout.addLayout(self.vLayout, stretch=0)

        self.centralwidget.setLayout(self.hLayout)
        if(isDark()):
            print("🔵 Is Dark")
        self.baseScrollArea.setWidget(self.centralwidget)

        l = QHBoxLayout()
        l.setContentsMargins(0,0, 0, 0)
        l.addWidget(self.baseScrollArea)
        self.setLayout(l)


        self.backButton = QPushButton(QIcon(getMedia("close")), "", self)
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(self.width()-40, 0)
        self.backButton.resize(40, 40)
        self.backButton.setFlat(True)
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.backButton.clicked.connect(lambda: (self.onClose.emit(), self.close()))
        self.backButton.show()

        self.hide()

        self.loadInfo.connect(self.printData)


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

        self.baseScrollArea.horizontalScrollBar().setEnabled(False)
        self.baseScrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.verticalScrollbar = CustomScrollBar()
        self.baseScrollArea.setVerticalScrollBar(self.verticalScrollbar)
        self.verticalScrollbar.setParent(self)
        self.verticalScrollbar.show()
        self.verticalScrollbar.setFixedWidth(12)
        
        self.versionCombo.currentIndexChanged.connect(self.loadPackageCommandLine)
        self.architectureCombo.currentIndexChanged.connect(self.loadPackageCommandLine)
        self.scopeCombo.currentIndexChanged.connect(self.loadPackageCommandLine)

    def resizeEvent(self, event = None):
        self.centralwidget.setFixedWidth(self.width()-18)
        g = self.mainGroupBox.geometry()
        self.loadingProgressBar.move(16, 0)
        self.loadingProgressBar.resize(self.width()-32, 4)
        self.verticalScrollbar.move(self.width()-16, 44)
        self.verticalScrollbar.resize(12, self.height()-64)
        self.backButton.move(self.width()-40, 0)
        self.imagesScrollbar.move(self.screenshotsWidget.x()+22, self.screenshotsWidget.y()+self.screenshotsWidget.height()+4)
        if(event):
            return super().resizeEvent(event)
        
    def getCommandLineParameters(self) -> list[str]:
        cmdline_args = []
        WINGET = self.currentPackage.isWinget()
        SCOOP = self.currentPackage.isScoop()
        CHOCO = self.currentPackage.isChocolatey()
        
        if(self.hashCheckBox.isChecked()):
            if WINGET:
                cmdline_args.append("--ignore-security-hash")
            elif SCOOP:
                cmdline_args.append("--skip")
            elif CHOCO:
                cmdline_args.append("--ignore-checksums")
                if not "--force" in cmdline_args:
                    cmdline_args.append("--force")
            else:
                print(f"🟠 Unknown source {self.currentPackage.Source}")
                
        if(self.interactiveCheckbox.isChecked()):
            if WINGET:
                cmdline_args.append("--interactive")
            elif CHOCO:
                cmdline_args.append("--notsilent")
            else:
                print("🟡 Interactive installation not supported by store")
        else:
            if WINGET:
                cmdline_args.append("--silent")

        if self.versionCombo.currentText() not in (_("Latest"), "Latest", "Loading...", _("Loading..."), ""):
            if WINGET:
                cmdline_args.append("--version")
                cmdline_args.append(self.versionCombo.currentText())
                if not "--force" in cmdline_args:
                    cmdline_args.append("--force")
            elif CHOCO:
                cmdline_args.append("--version="+self.versionCombo.currentText())
                cmdline_args.append("--allow-downgrade")
                if not "--force" in cmdline_args:
                    cmdline_args.append("--force")
            else:
                print("🟡 Custom version not supported by store")
            
        if self.architectureCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            if SCOOP:
                cmdline_args.append("--arch")
                cmdline_args.append(self.architectureCombo.currentText())
            elif WINGET:
                cmdline_args.append("--architecture")
                cmdline_args.append(self.architectureCombo.currentText())
            elif CHOCO:
                if self.architectureCombo.currentText() == "x86":
                    cmdline_args.append("--forcex86")
            else:
                print("🟡 Custom architecture not supported by store")
                
        if self.scopeCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            if SCOOP:
                chosenScope = self.scopeCombo.currentText()
                if chosenScope in (_("Local"), "Local"):
                        pass # Scoop installs locally by default
                elif chosenScope in (_("Global"), "Global"):
                        cmdline_args.append("--global")
                else:
                    print(f"🟠 Scope {chosenScope} not supported by Scoop")
            elif WINGET:
                chosenScope = self.scopeCombo.currentText()
                if chosenScope in (_("Current user"), "Current user"):
                        cmdline_args.append("--scope")
                        cmdline_args.append("user")
                elif chosenScope in (_("Local machine"), "Local machine"):
                        cmdline_args.append("--scope")
                        cmdline_args.append("machine")
                else:
                    print(f"🟠 Scope {chosenScope} not supported by Winget")
            else:
                print("🟡 Custom scope not supported by store")

        cmdline_args += [c for c in self.customArgumentsLineEdit.text().split(" ") if c]
        return cmdline_args

    def loadPackageCommandLine(self):
        parameters = " ".join(self.getCommandLineParameters())
        if self.currentPackage.isWinget():
            if not "…" in self.currentPackage.Id:
                self.commandWindow.setText(f"winget {'update' if self.isAnUpdate else ('uninstall' if self.isAnUninstall else 'install')} --id {self.currentPackage.Id} --exact {parameters} --source winget --accept-source-agreements --force ".strip().replace("  ", " ").replace("  ", " "))
            else:
                self.commandWindow.setText(_("Loading..."))
        elif self.currentPackage.isScoop():
            self.commandWindow.setText(f"scoop {'update' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.currentPackage.Id} {parameters}".strip().replace("  ", " ").replace("  ", " "))
        elif self.currentPackage.isChocolatey():
            self.commandWindow.setText(f"choco {'upgrade' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.currentPackage.Id} -y {parameters}".strip().replace("  ", " ").replace("  ", " "))
        else:
            print(f"🟠 Unknown source {self.currentPackage.Source}")
        self.commandWindow.setCursorPosition(0)

    def loadProgram(self, title: str, id: str, useId: bool, store: str, update: bool = False, packageItem: TreeWidgetItemWithQAction = None, version = "", uninstall: bool = False, installedVersion: str = "") -> None:
        package = Package(title, id, version, store)
        package.PackageItem = packageItem
        if package.isScoop():
            if len(store.split(': '))==2:
                package.Id = store.split(': ')[1]+'/'+id
        self.showPackageDetails_v2(package, update, packageItem, uninstall, installedVersion)
                
    def showPackageDetails_v2(self, package: Package, update: bool = False, packageItem: TreeWidgetItemWithQAction = None, uninstall: bool = False, installedVersion: str = ""):
        self.isAnUpdate = update
        self.isAnUninstall = uninstall
        if self.currentPackage == package:
            return
        self.currentPackage = package
        
        self.iv.resetImages()
        if "…" in package.Id:
            self.installButton.setEnabled(False)
            self.installButton.setText(_("Please wait..."))
        else:
            if self.isAnUpdate:
                self.installButton.setText(_("Update"))
            elif self.isAnUninstall:
                self.installButton.setText(_("Uninstall"))
            else:
                self.installButton.setText(_("Install"))
        
        self.title.setText(package.Name)

        self.loadPackageCommandLine()

        self.loadingProgressBar.show()
        self.hashCheckBox.setChecked(False)
        self.hashCheckBox.setEnabled(False)
        self.interactiveCheckbox.setChecked(False)
        self.interactiveCheckbox.setEnabled(False)
        self.adminCheckbox.setChecked(False)
        self.architectureCombo.setEnabled(False)
        self.scopeCombo.setEnabled(False)
        self.versionCombo.setEnabled(False)
        self.description.setText(_("Loading..."))
        self.author.setText("<b>"+_("Author")+":</b> "+_("Loading..."))
        self.publisher.setText(f"<b>{_('Publisher')}:</b> "+_("Loading..."))
        self.homepage.setText(f"<b>{_('Homepage')}:</b> {_('Loading...')}")
        self.license.setText(f"<b>{_('License')}:</b> {_('Loading...')}")
        lastVerString = ""
        if self.isAnUpdate:
            lastVerString = f"<b>{_('Installed Version')}:</b> {installedVersion} ({_('Update to {0} available').format(package.Version)})"
        elif self.isAnUninstall:
            lastVerString = f"<b>{_('Installed Version')}:</b> {package.Version}"
        else:
            if package.isScoop():
                lastVerString = f"<b>{_('Current Version')}:</b> {package.Version}"
            else:
                lastVerString = f"<b>{_('Latest Version')}:</b> {package.Version}"
        self.lastver.setText(lastVerString)

        self.sha.setText(f"<b>{_('Installer SHA512') if package.isChocolatey() else _('Installer SHA256')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {package.Id}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {_('Loading...')}")
        self.date.setText(f"<b>{_('Last updated:')}</b> {_('Loading...')}")
        self.notes.setText(f"<b>{_('Notes:') if package.isScoop() else _('Release notes:')}</b> {_('Loading...')}")
        self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {_('Loading...')}")
        self.storeLabel.setText(f"<b>{_('Source')}:</b> {package.Source}")
        self.versionCombo.addItems([_("Loading...")])
        self.architectureCombo.addItems([_("Loading...")])
        self.scopeCombo.addItems([_("Loading...")])

        def resetLayoutWidget():
            p = QPixmap()
            for l in self.imagesCarrousel:
                l.setPixmap(p, index=0)
            Thread(target=self.loadPackageScreenshots, args=(package,)).start()

        self.callInMain.emit(lambda: resetLayoutWidget())
        self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64)))
        Thread(target=self.loadPackageIcon, args=(package,)).start()
        
        Thread(target=self.loadPackageDetails, args=(package,), daemon=True, name=f"Loading details for {package}").start()

        self.finishedCount = 0
        
    def loadPackageDetails(self, package: Package):
        if package.isWinget():
            details = winget.getPackageDetails_v2(package)
        elif package.isScoop():
            details = scoop.getPackageDetails_v2(package)
        elif package.isChocolatey():
            details = choco.getPackageDetails_v2(package)
        self.callInMain.emit(lambda: self.printData(details))
            
    def printData(self, details: PackageDetails) -> None:
        if details.PackageObject != self.currentPackage:
            return 
        package = self.currentPackage
        
        self.loadingProgressBar.hide()
        self.installButton.setEnabled(True)
        self.adminCheckbox.setEnabled(True)
        self.hashCheckBox.setEnabled(not self.isAnUninstall)
        self.versionCombo.setEnabled(not self.isAnUninstall and not self.isAnUpdate)
        self.architectureCombo.setEnabled(not self.isAnUninstall)
        self.scopeCombo.setEnabled(not self.isAnUninstall)
        if self.isAnUpdate:
            self.installButton.setText(_("Update"))
        elif self.isAnUninstall:
            self.installButton.setText(_("Uninstall"))
        else:
            self.installButton.setText(_("Install"))
        
        self.interactiveCheckbox.setEnabled(not package.isScoop())
        self.title.setText(details.Name)
        self.description.setText(details.Description)
        if package.isWinget():
            self.author.setText(f"<b>{_('Author')}:</b> <a style=\"color: {blueColor};\" href='{details.Id.split('.')[0]}'>{details.Author}</a>")
            self.publisher.setText(f"<b>{_('Publisher')}:</b> <a style=\"color: {blueColor};\" href='{details.Id.split('.')[0]}'>{details.Publisher}</a>")
        else:
            self.author.setText(f"<b>{_('Author')}:</b> "+details.Author)
            self.publisher.setText(f"<b>{_('Publisher')}:</b> "+details.Publisher)
        self.homepage.setText(f"<b>{_('Homepage')}:</b> {details.asUrl(details.HomepageURL)}")
        if details.License != "" and details.LicenseURL != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.License} ({details.asUrl(details.LicenseURL)})")
        elif details.License != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.License}")
        elif details.LicenseURL != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.asUrl(details.License)}")
        else:
            self.license.setText(f"<b>{_('License')}:</b> {_('Not available')}")
        self.sha.setText(f"<b>{_('Installer SHA512') if package.isChocolatey() else _('Installer SHA256')} ({_('Latest Version')}):</b> {details.InstallerHash}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {details.asUrl(details.InstallerURL)} {f'({details.InstallerSize} MB)' if details.InstallerSize > 0 else ''}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {details.InstallerType}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {details.Id}")
        self.date.setText(f"<b>{_('Last updated:')}</b> {details.UpdateDate}")
        self.notes.setText(f"<b>{_('Notes:') if package.isScoop() else _('Release notes:')}</b> {details.ReleaseNotes}")
        self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {details.asUrl(details.ReleaseNotesUrl)}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {details.asUrl(details.ManifestUrl)}")
        while self.versionCombo.count()>0:
            self.versionCombo.removeItem(0)
        self.versionCombo.addItems([_("Latest")] + details.Versions)
        while self.architectureCombo.count()>0:
            self.architectureCombo.removeItem(0)
        self.architectureCombo.addItems([_("Default")] + details.Architectures)
        while self.scopeCombo.count()>0:
            self.scopeCombo.removeItem(0)
        self.scopeCombo.addItems([_("Default")] + details.Scopes)
        
        self.loadPackageCommandLine()

    def loadPackageIcon(self, package: Package) -> None:
        try:
            id = package.Id
            iconId = package.getIconId()
            iconpath = os.path.join(os.path.expanduser("~"), f".wingetui/cachedmeta/{iconId}.icon.png")
            if not os.path.exists(iconpath):
                if package.isChocolatey():
                    iconurl = f"https://community.chocolatey.org/content/packageimages/{id}.{version}.png"
                else:
                    iconurl = globals.packageMeta["icons_and_screenshots"][iconId]["icon"]
                print("🔵 Found icon: ", iconurl)
                if iconurl:
                    icondata = urlopen(iconurl).read()
                    with open(iconpath, "wb") as f:
                        f.write(icondata)
                else:
                    print("🟡 Icon url empty")
                    raise KeyError(f"{iconurl} was empty")
            else:
                cprint(f"🔵 Found cached image in {iconpath}")
            if self.currentPackage.Id == id:
                self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(iconpath).pixmap(64, 64)))
            else:
                print("Icon arrived too late!")
        except Exception as e:
            try:
                if type(e) != KeyError:
                    report(e)
                else:
                    print(f"🟡 Icon {iconId} not found in json")
            except Exception as e:
                report(e)

    def loadPackageScreenshots(self, package: Package) -> None:
        try:
            id = package.Id
            self.validImageCount = 0
            self.canContinueWithImageLoading = 0
            iconId = package.getIconId()
            count = 0
            for i in range(len(globals.packageMeta["icons_and_screenshots"][iconId]["images"])):
                try:
                    p = QPixmap(getMedia("placeholder_image")).scaledToHeight(128, Qt.SmoothTransformation)
                    if not p.isNull():
                        self.callInMain.emit(self.imagesCarrousel[i].show)
                        self.callInMain.emit(partial(self.imagesCarrousel[i].setPixmap, p, count))
                        count += 1
                except Exception as e:
                    report(e)
            for i in range(count+1, 20):
                self.callInMain.emit(self.imagesCarrousel[i].hide)
            for i in range(len(globals.packageMeta["icons_and_screenshots"][iconId]["images"])):
                try:
                    imagepath = os.path.join(os.path.expanduser("~"), f".wingetui/cachedmeta/{iconId}.screenshot.{i}.png")
                    if not os.path.exists(imagepath):
                        iconurl = globals.packageMeta["icons_and_screenshots"][iconId]["images"][i]
                        print("🔵 Found icon: ", iconurl)
                        if iconurl:
                            icondata = urlopen(iconurl).read()
                            with open(imagepath, "wb") as f:
                                f.write(icondata)
                        else:
                            print("🟡 Image url empty")
                            raise KeyError(f"{iconurl} was empty")
                    else:
                        cprint(f"🔵 Found cached image in {imagepath}")
                    p = QPixmap(imagepath)
                    if not p.isNull():
                        if self.currentPackage.Id == id:
                            self.callInMain.emit(partial(self.imagesCarrousel[self.validImageCount].setPixmap, p, self.validImageCount))
                            self.callInMain.emit(self.imagesCarrousel[self.validImageCount].show)
                            self.callInMain.emit(partial(self.iv.addImage, p))
                            self.validImageCount += 1
                        else:
                            print("Screenshot arrived too late!")
                    else:
                        print(f"🟡 {imagepath} is a null image")
                except Exception as e:
                    self.callInMain.emit(self.imagesCarrousel[self.validImageCount].hide)
                    self.validImageCount += 1
                    report(e)
            if self.validImageCount == 0:
                cprint("🟡 No valid screenshots were found")
            else:
                cprint(f"🟢 {self.validImageCount} vaild images found!")
            for i in range(self.validImageCount+1, 20):
                self.callInMain.emit(self.imagesCarrousel[i].hide)

        except Exception as e:
            try:
                if type(e) != KeyError:
                    report(e)
                else:
                    print(f"🟡 Image {iconId} not found in json")
            except Exception as e:
                report(e)


    def install(self):
        print(f"🟢 Starting installation of package {self.currentPackage.Name} with id {self.currentPackage.Id}")
        cmdline_args = self.getCommandLineParameters()
        print("🔵 The issued command arguments are", cmdline_args)
        
        if self.ignoreFutureUpdates.isChecked():
            IgnorePackageUpdates_Permanent(self.currentPackage.Id, self.currentPackage.Source)
            print(f"🟡 Blacklising package {self.currentPackage.Id}")

        if self.isAnUpdate:
            p = PackageUpdaterWidget(self.currentPackage.Name, self.currentPackage.Source, version=[], args=cmdline_args, packageId=self.currentPackage.Id, admin=self.adminCheckbox.isChecked(), packageItem=self.currentPackage.PackageItem, useId=not("…" in self.currentPackage.Id))
        elif self.isAnUninstall:            
            p = PackageUninstallerWidget(self.currentPackage.Name, self.currentPackage.Source, args=cmdline_args, packageId=self.currentPackage.Id, admin=self.adminCheckbox.isChecked(), packageItem=self.currentPackage.PackageItem, useId=not("…" in self.currentPackage.Id))
        else:
            p = PackageInstallerWidget(self.currentPackage.Name, self.currentPackage.Source, version=[], args=cmdline_args, packageId=self.currentPackage.Id, admin=self.adminCheckbox.isChecked(), packageItem=self.currentPackage.PackageItem, useId=not("…" in self.currentPackage.Id))
        self.addProgram.emit(p)
        self.close()

    def show(self) -> None:
        self.blackCover.hide()
        g = QRect(0, 0, self.parent().window().geometry().width(), self.parent().window().geometry().height())
        self.resize(700, 650)
        self.parent().window().blackmatt.show()
        self.move(g.x()+g.width()//2-700//2, g.y()+g.height()//2-650//2)
        self.raise_()
        if not self.backgroundApplied:
            globals.centralWindowLayout.setGraphicsEffect(self.blurBackgroundEffect)
            self.backgroundApplied = True
        self.blurBackgroundEffect.setEnabled(True)
        self.blurBackgroundEffect.setBlurRadius(40)
        backgroundImage = globals.centralWindowLayout.grab(QRect(QPoint(0, 0), globals.centralWindowLayout.size()))
        self.blurBackgroundEffect.setEnabled(False)
        self.imagesScrollbar.move(self.screenshotsWidget.x()+22, self.screenshotsWidget.y()+self.screenshotsWidget.height()+4)
        self.blackCover.resize(self.width(), self.centralwidget.height())
        if globals.centralWindowLayout:
            globals.centralTextureImage.setPixmap(backgroundImage)
            globals.centralTextureImage.show()
            globals.centralWindowLayout.hide()
        _ = super().show()
        return _

    def close(self) -> bool:
        self.blackCover.hide()
        self.iv.close()
        self.parent().window().blackmatt.hide()
        self.blurBackgroundEffect.setEnabled(False)
        if globals.centralWindowLayout:
            globals.centralTextureImage.hide()
            globals.centralWindowLayout.show()
        return super().close()

    def hide(self) -> None:
        self.blackCover.hide()
        try:
            self.parent().window().blackmatt.hide()
        except AttributeError:
            pass
        self.blurBackgroundEffect.setEnabled(False)
        self.iv.close()
        if globals.centralWindowLayout:
            globals.centralTextureImage.hide()
            globals.centralWindowLayout.show()
        return super().hide()

    def mousePressEvent(self, event: QMouseEvent) -> None:
        #self.pressed = True
        #self.oldPos = event.pos()
        return super().mousePressEvent(event)

    def mouseMoveEvent(self, event: QMouseEvent) -> None:
        #if self.pressed:
        #    self.window().move(self.pos()+(event.pos()-self.oldPos))
        return super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        #self.pressed = False
        #self.oldPos = event.pos()
        return super().mouseReleaseEvent(event)

    def destroy(self, destroyWindow: bool = ..., destroySubWindows: bool = ...) -> None:
        for anim in (self.leftSlow, self.leftFast, self.rightFast, self.rightSlow):
            anim: QVariantAnimation
            anim.pause()
            anim.stop()
            anim.valueChanged.disconnect()
            anim.finished.disconnect()
            anim.deleteLater()
        return super().destroy(destroyWindow, destroySubWindows)

class ImageViewer(QWidget):
    callInMain = Signal(object)
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.callInMain.connect(lambda f: f())
        layout = QHBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        self.images = {}

        try:
            self.sct = QShortcut(Qt.Key.Key_Escape, self)
            self.sct.activated.connect(lambda: self.close())
        except TypeError:
            pass
        self.setStyleSheet(f"""
        QGroupBox {{
            border: 0px;
        }}
        #backgroundWidget{{
            border-radius: 5px;
            padding: 5px;
            background-color: {'rgba(30, 30, 30, 50%)' if isDark() else 'rgba(255, 255, 255, 75%)'};
            border-radius: 16px;
            border: 1px solid #88888888;
        }}
        QPushButton {{
            background-color: {'rgba(20, 20, 20, 80%)' if isDark() else 'rgba(255, 255, 255, 80%)'};
        }}
        """)

        self.stackedWidget = QStackedWidget()
        self.stackedWidget.setObjectName("backgroundWidget")

        layout.addWidget(self.stackedWidget)
        self.setLayout(layout)

        self.closeButton = QPushButton(QIcon(getMedia("close")), "", self)
        self.closeButton.move(self.width()-40, 0)
        self.closeButton.resize(40, 40)
        self.closeButton.setFlat(True)
        self.closeButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.closeButton.clicked.connect(lambda: (self.close()))
        self.closeButton.show()


        self.backButton = QPushButton(QIcon(getMedia("left")), "", self)
        try:
            self.bk = QShortcut(QKeySequence(Qt.Key.Key_Left), parent=self)
            self.bk.activated.connect(lambda: self.backButton.click())
        except TypeError:
            pass
        self.backButton.move(0, self.height()//2-24)
        self.backButton.resize(48, 48)
        self.backButton.setFlat(False)
        #self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.backButton.clicked.connect(lambda: (self.stackedWidget.setCurrentIndex(self.stackedWidget.currentIndex()-1 if self.stackedWidget.currentIndex()>0 else self.stackedWidget.count()-1)))
        self.backButton.show()

        self.nextButton = QPushButton(QIcon(getMedia("right")), "", self)
        try:
            self.nxt = QShortcut(Qt.Key.Key_Right, self)
            self.nxt.activated.connect(lambda: self.nextButton.click())
        except TypeError:
            pass
        self.nextButton.move(self.width()-48, self.height()//2-24)
        self.nextButton.resize(48, 48)
        self.nextButton.setFlat(False)
        #self.nextButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.nextButton.clicked.connect(lambda: (self.stackedWidget.setCurrentIndex(self.stackedWidget.currentIndex()+1 if self.stackedWidget.currentIndex()<(self.stackedWidget.count()-1) else 0)))
        self.nextButton.show()
        self.hide()


    def resizeEvent(self, event = None):
        self.closeButton.move(self.width()-40, 0)
        self.backButton.move(10, self.height()//2-24)
        self.nextButton.move(self.width()-58, self.height()//2-24)
        for i in range(self.stackedWidget.count()):
            l: QLabel = self.stackedWidget.widget(i)
            l.resize(self.stackedWidget.size())
            pixmap: QPixmap = self.images[l]
            l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        if(event):
            return super().resizeEvent(event)

    def show(self, index: int = 0) -> None:
        g = QRect(0, 0, self.window().geometry().width(), self.window().geometry().height())
        self.resize(g.width()-100, g.height()-100)
        self.move(50, 50)
        self.raise_()
        self.stackedWidget.setCurrentIndex(index)
        for i in range(self.stackedWidget.count()):
            l: QLabel = self.stackedWidget.widget(i)
            l.resize(self.stackedWidget.size())
            pixmap: QPixmap = self.images[l]
            l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        return super().show()

    def close(self) -> bool:
        return super().close()

    def hide(self) -> None:
        return super().hide()

    def resetImages(self) -> None:
        del self.images
        self.images = {}
        for i in range(self.stackedWidget.count()):
            widget = self.stackedWidget.widget(0)
            self.stackedWidget.removeWidget(widget)
            widget.close()
            widget.deleteLater()
            del widget

    def addImage(self, pixmap: QPixmap) -> None:
        l = QLabel()
        l.setAlignment(Qt.AlignmentFlag.AlignCenter | Qt.AlignmentFlag.AlignVCenter)
        self.stackedWidget.addWidget(l)
        l.resize(self.stackedWidget.size())
        l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        self.images[l] = pixmap

    def wheelEvent(self, event: QWheelEvent) -> None:
        if abs(event.angleDelta().x()) <= 30:
            if event.angleDelta().y() < -30:
                self.backButton.click()
            elif event.angleDelta().y() > 30:
                self.nextButton.click()
        else:
            if event.angleDelta().x() < -30:
                self.backButton.click()
            elif event.angleDelta().x() > 30:
                self.nextButton.click()
        return super().wheelEvent(event)





if(__name__=="__main__"):
    import __init__

