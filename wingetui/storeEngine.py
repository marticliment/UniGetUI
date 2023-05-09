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
                self.p = subprocess.Popen(self.adminstr + [Winget.EXECUTABLE, "install", "-e", "--id", f"{self.packageId}"] + self.version + Winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [Winget.EXECUTABLE, "install", "-e", "--name", f"{self.programName}"] + self.version + Winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=Winget.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            cprint(self.store.lower())
            bucket_prefix = ""
            if len(self.store.lower().split(":"))>1 and not "/" in self.packageId:
                bucket_prefix = self.store.lower().split(":")[1].replace(" ", "")+"/"
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "install", f"{bucket_prefix+self.packageId if self.packageId != '' else bucket_prefix+self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=Scoop.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [Choco.EXECUTABLE, "install", self.packageId, "-y"] + self.version + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Choco.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
            subprocess.run([GSUDO_EXE_PATH, Winget.EXECUTABLE, "settings", "--enable", "InstallerHashOverride"], shell=True)
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
                self.p = subprocess.Popen(self.adminstr + [Winget.EXECUTABLE, "upgrade", "-e", "--id", f"{self.packageId}", "--include-unknown"] + self.version + Winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [Winget.EXECUTABLE, "upgrade", "-e", "--name", f"{self.programName}", "--include-unknown"] + self.version + Winget.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=Winget.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "update", f"{self.packageId if self.packageId != '' else self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Scoop.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [Choco.EXECUTABLE, "upgrade", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Choco.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
            self.p = subprocess.Popen(self.adminstr + [Winget.EXECUTABLE, "uninstall", "-e"] + (["--id", self.packageId] if self.useId else ["--name", self.programName]) + ["--accept-source-agreements"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Winget.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
            print(self.p.args)
        elif("scoop" in self.store):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "uninstall", f"{self.packageId if self.packageId != '' else self.programName}"] + (["-p"] if self.removeData else [""]) + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Scoop.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [Choco.EXECUTABLE, "uninstall", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            self.t = KillableThread(target=Choco.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
                        if self.packageId in globals.updates.IdPackageReference:
                            packageItem = globals.updates.ItemPackageReference[globals.updates.IdPackageReference[self.packageId]]
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
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        
        self.reloadButton = QPushButton()
        self.reloadButton.clicked.connect(self.loadBuckets)
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
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
        if isDark():
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #222222; background-color: rgba(30, 30, 30, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")
        else:
            self.bucketList.setStyleSheet("QTreeWidget{border: 1px solid #f5f5f5; background-color: rgba(255, 255, 255, 50%); border-radius: 8px; padding: 8px; margin-right: 15px;}")

        self.bucketList.label.setText(_("Loading buckets..."))
        self.bucketList.label.show()
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
        self.bucketIcon = QIcon(getMedia("bucket"))
        
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
        
    def showEvent(self, event: QShowEvent) -> None:
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
            if r[0] == _("Another bucket"):
                r2 = QInputDialog.getText(self, _("Scoop bucket manager"), _("Type here the name and the URL of the bucket you want to add, separated by a space."), text="extras https://github.com/ScoopInstaller/Extras")
                if r2[1]:
                    bName = r2[0].split(" ")[0]
                    p = PackageInstallerWidget(f"{bName} Scoop bucket", "custom", customCommand=f"scoop bucket add {r2[0]}")
                    globals.installersWidget.addItem(p)
                    p.finishInstallation.connect(self.loadBuckets)

            else:
                p = PackageInstallerWidget(f"{r[0]} Scoop bucket", "custom", customCommand=f"scoop bucket add {r[0]}")
                globals.installersWidget.addItem(p)
                p.finishInstallation.connect(self.loadBuckets)
            
    def scoopRemoveExtraBucket(self, bucket: str) -> None:
        globals.installersWidget.addItem(PackageUninstallerWidget(f"{bucket} Scoop bucket", "custom", customCommand=f"scoop bucket rm {bucket}"))




if(__name__=="__main__"):
    import __init__

