from __future__ import annotations
from functools import partial
import signal
import wingetHelpers, scoopHelpers, chocoHelpers, sys, subprocess, time, os, json
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from tools import _

from customWidgets import *
import globals

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
        self.adminstr = [sudoPath] if self.runAsAdmin else []
        
        if store.lower() == "winget" and getSettings("AlwaysElevateWinget"):
            print("🟡 Winget installation automatically elevated!")
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
        elif store.lower() == "chocolatey" and getSettings("AlwaysElevateChocolatey"):
            print("🟡 Chocolatey installation automatically elevated!")
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
        elif "scoop" in store.lower() and getSettings("AlwaysElevateScoop"):
            print("🟡 Chocolatey installation automatically elevated!")
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
        if getSettings("DoCacheAdminRights"):
            if self.runAsAdmin and not globals.adminRightsGranted:
                cprint(" ".join([sudoPath, "cache", "on", "--pid", f"{os.getpid()}", "-d" , "-1"]))
                asksudo = subprocess.Popen([sudoPath, "cache", "on", "--pid", f"{os.getpid()}", "-d" , "-1"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
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
        self.adminBadge.setVisible(self.runAsAdmin)
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
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "install", "-e", "--id", f"{self.packageId}"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "install", "-e", "--name", f"{self.programName}"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=wingetHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            cprint(self.store.lower())
            bucket_prefix = ""
            if len(self.store.lower().split(":"))>1 and not "/" in self.packageId:
                bucket_prefix = self.store.lower().split(":")[1].replace(" ", "")+"/"
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "install", f"{bucket_prefix+self.packageId if self.packageId != '' else bucket_prefix+self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=scoopHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [chocoHelpers.choco, "install", self.packageId, "-y"] + self.version + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=chocoHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
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
        if returncode == OC_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.cmdline_args.append("--global")
            self.runInstallation()
            return
        elif returncode == OC_NEEDS_ELEVATION:
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
            return
        elif "winget settings --enable InstallerHashOverride" in output:
            print("🟠 Requiring the user to enable skiphash setting!")
            subprocess.run([sudoPath, wingetHelpers.winget, "settings", "--enable", "InstallerHashOverride"], shell=True)
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
        if returncode in (OC_OPERATION_SUCCEEDED, OC_NEEDS_RESTART):
            if returncode == OC_OPERATION_SUCCEEDED:
                self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode = False)))
                self.info.setText(_("{action} was successfully!").format(action = self.actionDone.capitalize()))
                self.startCoolDown()
                self.callInMain.emit(lambda: globals.trayIcon.showMessage(_("{0} succeeded").format(self.actionName.capitalize()), _("{0} was {1} successfully!").format(self.programName, self.actionDone), QIcon(getMedia("notif_info"))))
            if returncode == OC_NEEDS_RESTART: # if the installer need restart computer
                self.cancelButton.setIcon(QIcon(getMedia("restart_color", autoIconMode = False)))
                self.info.setText(_("Restart your PC to finish installation"))
                self.callInMain.emit(lambda: globals.trayIcon.showMessage(_("{0} succeeded").format(self.actionName.capitalize()), _("{0} was {1} successfully!").format(self.programName, self.actionDone)+" "+_("Restart your PC to finish installation"), QIcon(getMedia("notif_restart"))))
            if type(self) == PackageInstallerWidget:
                if self.packageItem:
                    if not self.packageItem.text(2) in globals.uninstall.packages.keys():
                        globals.uninstall.addItem(self.packageItem.text(1), self.packageItem.text(2), self.packageItem.text(3), self.packageItem.text(4)) # Add the package on the uninstaller
                        globals.uninstall.updatePackageNumber()
        else:
            globals.trayIcon.setIcon(QIcon(getMedia("yellowicon")))
            self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
            self.err = ErrorMessage(self.window())
            warnIcon = QIcon(getMedia("notif_warn"))
            dialogData = {
                    "titlebarTitle": _("WingetUI - {0} {1}").format(self.programName, self.actionName),
                    "buttonTitle": _("Close"),
                    "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                    "icon": warnIcon,
                    "notifTitle": _("Can't {0} {1}").format(self.actionVerb, self.programName),
                    "notifIcon": warnIcon,
            }
            if returncode == OC_INCORRECT_HASH: # if the installer's hash does not coincide
                dialogData["mainTitle"] = _("{0} aborted").format(self.actionName.capitalize())
                dialogData["mainText"] = _("The checksum of the installer does not coincide with the expected value, and the authenticity of the installer can't be verified. If you trust the publisher, {0} the package again skipping the hash check.").format(self.actionVerb)
                dialogData["notifText"] = _("The installer has an invalid checksum")
            else: # if there's a generic error
                dialogData["mainTitle"] = _("{0} failed").format(self.actionName.capitalize())
                dialogData["mainText"] = _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the installer.").format(action=self.actionVerb, package=self.programName, showDetails=_("Show details"))
                dialogData["notifText"] = _("{0} {1} failed").format(self.programName.capitalize(), self.actionName)
            self.err.showErrorMessage(dialogData)

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

            updateOp(1)
            a = QVariantAnimation(self)
            a.setStartValue(1.0)
            a.setEndValue(0.0)
            a.setEasingCurve(QEasingCurve.Linear)
            a.setDuration(300)
            a.valueChanged.connect(lambda v: updateOp(v))
            a.finished.connect(self.heightAnim)
            f = lambda: (time.sleep(3), self.callInMain.emit(a.start))
            Thread(target=f, daemon=True).start()
        else:
            print("🟡 Autohide disabled!")

    def heightAnim(self):
        a = QVariantAnimation(self)
        a.setStartValue(self.height())
        a.setEndValue(0)
        a.setEasingCurve(QEasingCurve.InOutCubic)
        a.setDuration(300)
        a.valueChanged.connect(lambda v: self.setFixedHeight(v))
        a.finished.connect(self.close)
        a.start()

    def close(self):
        globals.installersWidget.removeItem(self)
        super().close()
        self.liveOutputWindow.close()
        super().destroy()

class PackageUpdaterWidget(PackageInstallerWidget):

    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId="", packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, useId: bool = False):
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
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "upgrade", "-e", "--id", f"{self.packageId}", "--include-unknown"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "upgrade", "-e", "--name", f"{self.programName}", "--include-unknown"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=wingetHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "update", f"{self.packageId if self.packageId != '' else self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=scoopHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [chocoHelpers.choco, "upgrade", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=chocoHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

    def finish(self, returncode: int, output: str = "") -> None:
        if returncode == OC_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [sudoPath]
            self.cmdline_args.append("--global")
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        elif returncode == OC_NEEDS_ELEVATION:
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        else:
            if returncode in (OC_OPERATION_SUCCEEDED, OC_NEEDS_RESTART) and not self.canceled:
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
    def __init__(self, title: str, store: str, useId=False, packageId = "", packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, removeData: bool = False, args: list = [], customCommand: list = []):
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
            self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "uninstall", "-e"] + (["--id", self.packageId] if self.useId else ["--name", self.programName]) + ["--accept-source-agreements"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=wingetHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
            print(self.p.args)
        elif("scoop" in self.store):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "uninstall", f"{self.packageId if self.packageId != '' else self.programName}"] + (["-p"] if self.removeData else [""])), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=scoopHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal, "--global" in self.cmdline_args))
            self.t.start()
        elif self.store == "chocolatey":
            self.p = subprocess.Popen(self.adminstr + [chocoHelpers.choco, "uninstall", self.packageId, "-y"] + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=chocoHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
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
        if returncode == OC_NEEDS_SCOOP_ELEVATION:
            self.adminstr = [sudoPath]
            self.cmdline_args.append("--global")
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        elif returncode == OC_NEEDS_ELEVATION:
            self.adminstr = [sudoPath]
            self.runAsAdmin = True
            self.adminBadge.setVisible(self.runAsAdmin)
            self.runInstallation()
        else:
            if returncode in(OC_OPERATION_SUCCEEDED, OC_NEEDS_RESTART) and not self.canceled:
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
                if(returncode in (OC_OPERATION_SUCCEEDED, OC_NEEDS_RESTART)):
                    self.callInMain.emit(lambda: globals.trayIcon.showMessage(_("{0} succeeded").format(self.actionName.capitalize()), _("{0} was {1} successfully!").format(self.programName, self.actionDone), QIcon(getMedia("notif_info"))))
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("tick", autoIconMode = False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.info.setText(_("{action} was successfully!").format(action = self.actionDone.capitalize()))
                    self.progressbar.setValue(1000)
                    self.startCoolDown()
                else:
                    globals.trayIcon.setIcon(QIcon(getMedia("yellowicon")))
                    self.cancelButton.setText(_("OK"))
                    self.cancelButton.setIcon(QIcon(getMedia("warn", autoIconMode = False)))
                    self.cancelButton.clicked.connect(self.close)
                    self.progressbar.setValue(1000)
                    self.err = ErrorMessage(self.window())
                    errorData = {
                        "titlebarTitle": _("WingetUI - {0} {1}").format(self.programName, self.actionName),
                        "mainTitle": _("{0} failed").format(self.actionName.capitalize()),
                        "mainText": _("We could not {action} {package}. Please try again later. Click on \"{showDetails}\" to get the logs from the uninstaller.").format(action=self.actionVerb, package=self.programName, showDetails=_("Show details")),
                        "buttonTitle": _("Close"),
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("notif_warn")),
                        "notifTitle": _("Can't {0} {1}").format(self.actionVerb, self.programName),
                        "notifText": _("{0} {1} failed").format(self.programName.capitalize(), self.actionName),
                        "notifIcon": QIcon(getMedia("notif_warn")),
                        }
                    self.err.showErrorMessage(errorData)

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
    packageItem: TreeWidgetItemWithQAction = None
    finishedCount: int = 0
    backgroundApplied: bool = False
    givenPackageId: str = ""
    isAnUpdate = False
    isAnUninstall = False
    store = ""

    currentProgram = 0
    pressed = False
    oldPos = QPoint(0, 0)

    def __init__(self, parent):
        super().__init__(parent = parent)
        self.iv = ImageViewer(self.window())
        self.callInMain.connect(lambda f: f())
        self.baseScrollArea = QScrollArea()
        self.blurBackgroundEffect = QGraphicsBlurEffect()
        self.store = ""
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
            background-color: {'rgba(30, 30, 30, 50%)' if isDark() else 'rgba(255, 255, 255, 75%)'};
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
        self.appIcon.setStyleSheet(f"padding: 16px; border-radius: 16px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'white'};")
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

        self.homepage = QLinkLabel("<b>"+_('Homepage URL:')+"</b> "+_('Unknown'))
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = QLinkLabel("<b>"+_('Publisher:')+"</b> "+_('Unknown'))
        self.publisher.setOpenExternalLinks(False)
        self.publisher.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = QLinkLabel("<b>"+_('Author:')+"</b> "+_('Unknown'))
        self.author.setOpenExternalLinks(False)
        self.author.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
        self.author.setWordWrap(True)

        self.layout.addWidget(self.author)
        self.layout.addSpacing(10)

        self.license = QLinkLabel("<b>"+_('License:')+"</b> "+_('Unknown'))
        self.license.setWordWrap(True)

        self.layout.addWidget(self.license)
        self.layout.addSpacing(10)

        self.screenshotsWidget = QScrollArea()
        self.screenshotsWidget.setWidgetResizable(True)
        self.screenshotsWidget.setStyleSheet(f"QScrollArea{{padding: 8px; border-radius: 8px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'white'};border: 0px solid black;}};")
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
        
        optionsSection = SmallCollapsableSection("Installation parameters", getMedia("tools"))

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

        firstRow = HorizontalWidgetForSection()
        firstRow.addWidget(self.hashCheckBox)
        firstRow.addWidget(self.interactiveCheckbox)
        firstRow.addWidget(self.adminCheckbox)
        
        optionsSection.addWidget(firstRow)

        self.commandWindow = CommandLineEdit()
        self.commandWindow.setReadOnly(True)
        
        commandWidget = HorizontalWidgetForSection(lastOne = True)
        commandWidget.addWidget(self.commandWindow)
        
        
        self.versionLabel = QLabel(_("Version to install: "))
        self.versionCombo = CustomComboBox()
        self.versionCombo.setFixedWidth(150)
        self.versionCombo.setIconSize(QSize(24, 24))
        self.versionCombo.setFixedHeight(30)
        versionSection = HorizontalWidgetForSection()
        versionSection.addWidget(self.versionLabel)
        versionSection.addWidget(self.versionCombo)
        versionSection.setFixedHeight(50)
        
        self.ignoreFutureUpdates = QCheckBox()
        self.ignoreFutureUpdates.setText(_("Ignore future updates for this package"))
        self.ignoreFutureUpdates.setChecked(False)
        
        ignoreUpdatesSection = HorizontalWidgetForSection()
        ignoreUpdatesSection.addWidget(self.ignoreFutureUpdates)
        
        self.architectureLabel = QLabel(_("Architecture to install: "))
        self.architectureCombo = CustomComboBox()
        self.architectureCombo.setFixedWidth(150)
        self.architectureCombo.setIconSize(QSize(24, 24))
        self.architectureCombo.setFixedHeight(30)
        architectureSection = HorizontalWidgetForSection()
        architectureSection.addWidget(self.architectureLabel)
        architectureSection.addWidget(self.architectureCombo)
        architectureSection.setFixedHeight(50)
        
        self.scopeLabel = QLabel(_("Installation scope: "))
        self.scopeCombo = CustomComboBox()
        self.scopeCombo.setFixedWidth(150)
        self.scopeCombo.setIconSize(QSize(24, 24))
        self.scopeCombo.setFixedHeight(30)
        scopeSection = HorizontalWidgetForSection()
        scopeSection.addWidget(self.scopeLabel)
        scopeSection.addWidget(self.scopeCombo)
        scopeSection.setFixedHeight(50)
        
        customArgumentsSection = HorizontalWidgetForSection()
        customArgumentsLabel = QLabel(_("Custom command-line arguments: "))
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
        self.shareButton.clicked.connect(lambda: nativeWindowsShare(self.title.text(), f"https://marticliment.com/wingetui/share?pid={self.givenPackageId}^&pname={self.givenPackageId}"))
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

        self.packageId = QLinkLabel("<b>"+_('Program ID:')+"</b> "+_('Unknown'))
        self.packageId.setWordWrap(True)
        self.layout.addWidget(self.packageId)
        self.manifest = QLinkLabel("<b>"+_('Manifest:')+"</b> "+_('Unknown'))
        self.manifest.setWordWrap(True)
        self.layout.addWidget(self.manifest)
        self.lastver = QLinkLabel("<b>"+_('Latest Version:')+"</b> "+_('Unknown'))
        self.lastver.setWordWrap(True)
        self.layout.addWidget(self.lastver)
        self.sha = QLinkLabel("<b>"+_('Installer SHA256 (Latest Version):')+"</b> "+_('Unknown'))
        self.sha.setWordWrap(True)
        self.layout.addWidget(self.sha)
        self.link = QLinkLabel("<b>"+_('Installer URL (Latest Version):')+"</b> "+_('Unknown'))
        self.link.setWordWrap(True)
        self.layout.addWidget(self.link)
        self.type = QLinkLabel("<b>"+_('Installer Type (Latest Version):')+"</b> "+_('Unknown'))
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

        self.storeLabel = QLinkLabel("<b>"+_("Source:")+"</b> " + self.store)
        self.storeLabel.setWordWrap(True)
        self.layout.addWidget(self.storeLabel)

        self.layout.addSpacing(10)
        self.layout.addStretch()
        self.advert = QLinkLabel("<b>"+_("DISCLAIMER: NEITHER MICROSOFT NOR THE CREATORS OF WINGETUI ARE RESPONSIBLE FOR THE DOWNLOADED APPS."))
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
        WINGET = "winget" in self.store.lower()
        SCOOP = "scoop" in self.store.lower()
        CHOCO = "chocolatey" in self.store.lower()
        
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
                print(f"🟠 Unknown store {self.store}")
                
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

        if self.versionCombo.currentText() not in (_("Latest"), "Latest", "Loading...", _("Loading...")):
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
            
        if self.architectureCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading...")):
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
                
        if self.scopeCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading...")):
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
        if self.store.lower() == "winget":
            if not "…" in self.givenPackageId:
                self.commandWindow.setText(f"winget {'update' if self.isAnUpdate else ('uninstall' if self.isAnUninstall else 'install')} --id {self.givenPackageId} --exact {parameters} --source winget --accept-source-agreements --force ".strip().replace("  ", " ").replace("  ", " "))
            else:
                self.commandWindow.setText(_("Loading..."))
        elif "scoop" in self.store.lower():
            self.commandWindow.setText(f"scoop {'update' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.givenPackageId} {parameters}".strip().replace("  ", " ").replace("  ", " "))
        elif self.store.lower() == "chocolatey":
            self.commandWindow.setText(f"choco {'upgrade' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.givenPackageId} -y {parameters}".strip().replace("  ", " ").replace("  ", " "))
        else:
            print(f"🟠 Unknown store {self.store}")
        self.commandWindow.setCursorPosition(0)

    def loadProgram(self, title: str, id: str, useId: bool, store: str, update: bool = False, packageItem: TreeWidgetItemWithQAction = None, version = "", uninstall: bool = False, installedVersion: str = "") -> None:
        newProgram = id+store
        if self.currentProgram != newProgram:
            self.currentProgram = newProgram
            self.iv.resetImages()
            self.packageItem = packageItem
            self.givenPackageId = id
            self.isAnUpdate = update
            self.isAnUninstall = uninstall
            self.store = store
            if "…" in id:
                self.installButton.setEnabled(False)
                self.installButton.setText(_("Please wait..."))
            else:
                if self.isAnUpdate:
                    self.installButton.setText(_("Update"))
                elif self.isAnUninstall:
                    self.installButton.setText(_("Uninstall"))
                else:
                    self.installButton.setText(_("Install"))
            store = store.lower()
            self.title.setText(title)

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
            isScoop = "scoop" in self.store.lower()
            self.description.setText(_("Loading..."))
            self.author.setText("<b>"+_("Author")+":</b> "+_("Loading..."))
            self.publisher.setText(f"<b>{_('Publisher')}:</b> "+_("Loading..."))
            self.homepage.setText(f"<b>{_('Homepage')}:</b> <a style=\"color: {blueColor};\"  href=\"\">{_('Loading...')}</a>")
            self.license.setText(f"<b>{_('License')}:</b> {_('Loading...')} (<a style=\"color: {blueColor};\" href=\"\">{_('Loading...')}</a>)")
            lastVerString = ""
            if update:
                lastVerString = f"<b>{_('Installed Version')}:</b> {installedVersion} ({_('Update to {0} available').format(version)})"
            elif uninstall:
                lastVerString = f"<b>{_('Installed Version')}:</b> {version}"
            else:
                if isScoop:
                    lastVerString = f"<b>{_('Current Version')}:</b> {version}"
                else:
                    lastVerString = f"<b>{_('Latest Version')}:</b> {version}"
            self.lastver.setText(lastVerString)

            self.sha.setText(f"<b>{_('Installer SHA512') if self.store.lower() == 'chocolatey' else _('Installer SHA256')} ({_('Latest Version')}):</b> {_('Loading...')}")
            self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> <a  style=\"color: {blueColor};\" href=\"\">{_('Loading...')}</a>")
            self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {_('Loading...')}")
            self.packageId.setText(f"<b>{_('Package ID')}:</b> {id}")
            self.manifest.setText(f"<b>{_('Manifest')}:</b> {_('Loading...')}")
            self.date.setText(f"<b>{_('Last updated:')}</b> {_('Loading...')}")
            self.notes.setText(f"<b>{_('Release notes:')}</b> {_('Loading...')}")
            self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {_('Loading...')}")
            self.storeLabel.setText(f"<b>{_('Source')}:</b> {self.store.capitalize()}")
            self.versionCombo.addItems([_("Loading...")])
            self.architectureCombo.addItems([_("Loading...")])
            self.scopeCombo.addItems([_("Loading...")])

            def resetLayoutWidget():
                for l in self.imagesCarrousel:
                    l.setPixmap(QPixmap(), index=0)
                Thread(target=self.loadPackageScreenshots, args=(id, store)).start()

            self.callInMain.emit(lambda: resetLayoutWidget())
            self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64)))
            Thread(target=self.loadPackageIcon, args=(id, store, version)).start()

            self.finishedCount = 0
            if(store.lower()=="winget"):
                Thread(target=wingetHelpers.getInfo, args=(self.loadInfo, title, id, useId, newProgram), daemon=True).start()
            elif("scoop" in store.lower()):
                bucket_prefix = ""
                if len(self.store.lower().split(":"))>1 and not "/" in id and not "/" in title:
                    bucket_prefix = self.store.lower().split(":")[1].replace(" ", "")+"/"
                Thread(target=scoopHelpers.getInfo, args=(self.loadInfo, bucket_prefix+title, bucket_prefix+id, useId, newProgram), daemon=True).start()
            elif store.lower() == "chocolatey":
                Thread(target=chocoHelpers.getInfo, args=(self.loadInfo, title, id, useId, newProgram), daemon=True).start()

    def printData(self, appInfo: dict, progId) -> None:
        if self.currentProgram == progId:
            self.finishedCount += 1
            if not("scoop" in self.store.lower()) or self.finishedCount > 1:
                self.loadingProgressBar.hide()
            if self.isAnUpdate:
                self.installButton.setText(_("Update"))
            elif self.isAnUninstall:
                self.installButton.setText(_("Uninstall"))
            else:
                self.installButton.setText(_("Install"))
            self.installButton.setEnabled(True)
            self.adminCheckbox.setEnabled(True)
            self.hashCheckBox.setEnabled(not self.isAnUninstall)
            self.versionCombo.setEnabled(not self.isAnUninstall)
            self.architectureCombo.setEnabled(not self.isAnUninstall)
            self.scopeCombo.setEnabled(not self.isAnUninstall)
            
            if(self.store.lower() == "winget" or self.store.lower() == "chocolatey"):
                self.interactiveCheckbox.setEnabled(True)
            self.title.setText(appInfo["title"])
            self.description.setText(appInfo["description"])
            if self.store.lower() == "winget":
                self.author.setText(f"<b>{_('Author')}:</b> <a style=\"color: {blueColor};\" href='{appInfo['id'].split('.')[0]}'>"+appInfo["author"]+"</a>")
                self.publisher.setText(f"<b>{_('Publisher')}:</b> <a style=\"color: {blueColor};\" href='{appInfo['id'].split('.')[0]}'>"+appInfo["publisher"]+"</a>")
            else:
                self.author.setText(f"<b>{_('Author')}:</b> "+appInfo["author"])
                self.publisher.setText(f"<b>{_('Publisher')}:</b> "+appInfo["publisher"])
            self.homepage.setText(f"<b>{_('Homepage')}:</b> <a style=\"color: {blueColor};\"  href=\"{appInfo['homepage']}\">{appInfo['homepage']}</a>")
            self.license.setText(f"<b>{_('License')}:</b> {appInfo['license']} (<a style=\"color: {blueColor};\" href=\"{appInfo['license-url']}\">{appInfo['license-url']}</a>)")
            self.sha.setText(f"<b>{_('Installer SHA512') if self.store.lower() == 'chocolatey' else _('Installer SHA256')} ({_('Latest Version')}):</b> {appInfo['installer-sha256']}")
            self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> <a style=\"color: {blueColor};\" href=\"{appInfo['installer-url']}\">{appInfo['installer-url']}</a> {appInfo['installer-size']}")
            self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {appInfo['installer-type']}")
            self.packageId.setText(f"<b>{_('Package ID')}:</b> {appInfo['id']}")
            self.date.setText(f"<b>{_('Last updated:')}</b> {appInfo['updatedate']}")
            self.notes.setText(f"<b>{_('Release notes:')}</b> {appInfo['releasenotes'].replace(r'%bluecolor%', blueColor)}")
            self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {appInfo['releasenotesurl'].replace(r'%bluecolor%', blueColor)}")
            self.manifest.setText(f"<b>{_('Manifest')}:</b> <a style=\"color: {blueColor};\" href=\"{'file:///' if not 'https' in appInfo['manifest'] else ''}"+appInfo['manifest'].replace('\\', '/')+f"\">{appInfo['manifest']}</a>")
            while self.versionCombo.count()>0:
                self.versionCombo.removeItem(0)
            self.versionCombo.addItems([_("Latest")] + appInfo["versions"])
            while self.architectureCombo.count()>0:
                self.architectureCombo.removeItem(0)
            self.architectureCombo.addItems([_("Default")] + appInfo["architectures"])
            while self.scopeCombo.count()>0:
                self.scopeCombo.removeItem(0)
            self.scopeCombo.addItems([_("Default")] + appInfo["scopes"])
            if "…" in self.givenPackageId:
                self.givenPackageId = appInfo["id"]
                self.loadPackageCommandLine()

    def loadPackageIcon(self, id: str, store: str, version: str) -> None:
        try:
            iconprov = "winget" if not "scoop" in store.lower() else "scoop"
            iconprov = iconprov if not "chocolatey" in store.lower() else "chocolatey"
            iconId = id.lower()
            if store.lower() == "winget":
                iconId = ".".join(iconId.split(".")[1:])
            elif store.lower() == "chocolatey":
                iconId = iconId.replace(".install", "").replace(".portable", "")
            iconId = iconId.replace(" ", "-").replace("_", "-").replace(".", "-")
            iconpath = os.path.join(os.path.expanduser("~"), f".wingetui/cachedmeta/{iconId}.icon.png")
            if not os.path.exists(iconpath):
                if iconprov == "chocolatey":
                    iconurl = f"https://community.chocolatey.org/content/packageimages/{id}.{version}.png"
                else:
                    iconurl = globals.packageMeta["icons_and_screenshots"][iconId]["icon"]
                print("🔵 Found icon: ", iconurl)
                icondata = urlopen(iconurl).read()
                with open(iconpath, "wb") as f:
                    f.write(icondata)
            else:
                cprint(f"🔵 Found cached image in {iconpath}")
            if self.givenPackageId == id:
                self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(iconpath).pixmap(64, 64)))
            else:
                print("Icon arrived too late!")
        except Exception as e:
            try:
                if type(e) != KeyError:
                    report(e)
                else:
                    print(f"🟠 Icon {id} not found in json")
                pass # TODO: implement fallback icon loader
            except Exception as e:
                report(e)

    def loadPackageScreenshots(self, id: str, store: str) -> None:
        try:
            self.validImageCount = 0
            self.canContinueWithImageLoading = 0
            iconId = id.lower()
            if store.lower() == "winget":
                iconId = ".".join(iconId.split(".")[1:])
            elif store.lower() == "chocolatey":
                iconId = iconId.replace(".install", "").replace(".portable", "")
            iconId = iconId.replace(" ", "-").replace("_", "-").replace(".", "-")
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
                        icondata = urlopen(iconurl).read()
                        with open(imagepath, "wb") as f:
                            f.write(icondata)
                    else:
                        cprint(f"🔵 Found cached image in {imagepath}")
                    p = QPixmap(imagepath)
                    if not p.isNull():
                        if self.givenPackageId == id:
                            self.callInMain.emit(partial(self.imagesCarrousel[self.validImageCount].setPixmap, p, self.validImageCount))
                            self.callInMain.emit(self.imagesCarrousel[self.validImageCount].show)
                            self.callInMain.emit(partial(self.iv.addImage, p))
                            self.validImageCount += 1
                        else:
                            print("Screenshot arrived too late!")
                    else:
                        print(f"🟠 {imagepath} is a null image")
                except Exception as e:
                    self.callInMain.emit(self.imagesCarrousel[self.validImageCount].hide)
                    self.validImageCount += 1
                    report(e)
            if self.validImageCount == 0:
                cprint("🟠 No valid screenshots were found")
            else:
                cprint(f"🟢 {self.validImageCount} vaild images found!")
            for i in range(self.validImageCount+1, 20):
                self.callInMain.emit(self.imagesCarrousel[i].hide)

        except Exception as e:
            try:
                if type(e) != KeyError:
                    report(e)
                else:
                    print(f"🟠 Icon {id} not found in json")
                pass # TODO: implement fallback icon loader
            except Exception as e:
                report(e)


    def install(self):
        title = self.title.text()
        packageId = self.givenPackageId
        print(f"🟢 Starting installation of package {title} with id {packageId}")
        cmdline_args = self.getCommandLineParameters()
        print("🔵 The issued command arguments are", cmdline_args)
        
        if self.ignoreFutureUpdates.isChecked():
            blacklistUpdatesForPackage(packageId)
            print(f"🟡 Blacklising package {packageId}")

        if self.isAnUpdate:
            p = PackageUpdaterWidget(title, self.store, version=[], args=cmdline_args, packageId=packageId, admin=self.adminCheckbox.isChecked(), packageItem=self.packageItem, useId=not("…" in packageId))
        elif self.isAnUninstall:            
            p = PackageUninstallerWidget(title, self.store, args=cmdline_args, packageId=packageId, admin=self.adminCheckbox.isChecked(), packageItem=self.packageItem, useId=not("…" in packageId))
        else:
            p = PackageInstallerWidget(title, self.store, version=[], args=cmdline_args, packageId=packageId, admin=self.adminCheckbox.isChecked(), packageItem=self.packageItem, useId=not("…" in packageId))
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

