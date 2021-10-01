from posixpath import relpath
from PySide2 import QtWidgets, QtCore, QtGui
import WingetTools, ScoopTools, darkdetect, sys, Tools, subprocess, time, os
from threading import Thread


if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

class Discover(QtWidgets.QWidget):

    addProgram = QtCore.Signal(str, str, str, str)
    hideLoadingWheel = QtCore.Signal(str)
    clearList = QtCore.Signal()
    askForScoopInstall = QtCore.Signal(str)
    setLoadBarValue = QtCore.Signal(str)
    startAnim = QtCore.Signal(QtCore.QVariantAnimation)
    changeBarOrientation = QtCore.Signal()

    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = Program()
        self.setStyleSheet("margin: 0px;")
        self.infobox.onClose.connect(self.showQuery)

        self.programbox = QtWidgets.QWidget()

        self.layout = QtWidgets.QVBoxLayout()
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon(realpath+"/reload.png"))

        self.searchButton = QtWidgets.QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QtGui.QIcon(realpath+"/search.png"))

        hLayout = QtWidgets.QHBoxLayout()

        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search something on Winget or Scoop")
        self.query.returnPressed.connect(self.filter)
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        self.discoverLabel = QtWidgets.QLabel("Discover packages")
        self.discoverLabel.setStyleSheet("font-size: 40px;")

        hLayout.addWidget(self.discoverLabel)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        self.packageList = QtWidgets.QTreeWidget()
        self.packageList.setIconSize(QtCore.QSize(24, 24))
        self.packageList.setColumnCount(4)
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Version", "Origin"])
        self.packageList.setColumnWidth(0, 300)
        self.packageList.setColumnWidth(1, 300)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnWidth(3, 100)
        self.packageList.setSortingEnabled(True)
        self.packageList.sortByColumn(0, QtCore.Qt.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item.text(0), item.text(1), item.text(3)))
        
        
        
        self.loadingProgressBar = QtWidgets.QProgressBar()
        self.loadingProgressBar.setRange(0, 500)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(6)
        self.loadingProgressBar.setTextVisible(False)

        layout = QtWidgets.QVBoxLayout()


        self.countLabel = QtWidgets.QLabel("Fetching file list...")
        layout.addLayout(hLayout)
        layout.addWidget(QtWidgets.QLabel())
        layout.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        self.programbox.setLayout(layout)
        self.layout.addWidget(self.programbox, stretch=1)
        self.layout.addWidget(self.infobox, stretch=1)
        self.installersScrollArea = QtWidgets.QScrollArea()
        self.installersScrollArea.setWidgetResizable(True)
        self.installersScrollArea.setFixedHeight(150)
        self.installersScrollArea.hide()
        widget = QtWidgets.QWidget()
        widget.setAttribute(QtCore.Qt.WA_NoSystemBackground) 
        self.installerswidget = QtWidgets.QVBoxLayout()
        widget.setLayout(self.installerswidget)
        self.installersScrollArea.setWidget(widget)
        self.layout.addWidget(self.installersScrollArea, stretch=0)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)
        self.askForScoopInstall.connect(self.scoopNotFound)

        #self.loadWheel = LoadingProgress(self)
        #self.loadWheel.resize(64, 64)

        self.hideLoadingWheel.connect(self.hideLoadingWheelIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QtGui.QIcon(realpath+"/install.png")
        self.IDIcon = QtGui.QIcon(realpath+"/ID.png")
        self.versionIcon = QtGui.QIcon(realpath+"/version.png")
        self.providerIcon = QtGui.QIcon(realpath+"/provider.png")
        
        self.show()
    

        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
        #self.loadWheel.move(g.x()+g.width()//2-32, g.y()+g.height()//2-32)
            
        Thread(target=self.checkIfScoop, daemon=True)
        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(500)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(500)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(500)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        
        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(500)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))#self.setLoadBarValue.emit(v))
        
        Thread(target=self.loadProgressBarLoop, daemon=True).start()
        
    
    def loadProgressBarLoop(self):
        print("starting")
        while True:
            self.startAnim.emit(self.leftSlow)
            time.sleep(0.7)
            self.changeBarOrientation.emit()
            self.startAnim.emit(self.rightSlow)
            time.sleep(0.7)
            self.changeBarOrientation.emit()
            self.startAnim.emit(self.leftFast)
            time.sleep(0.3)
            self.changeBarOrientation.emit()
            self.startAnim.emit(self.rightFast)
            time.sleep(0.3)
            self.changeBarOrientation.emit()

    
    def checkIfScoop(self) -> None:
        if(subprocess.call("scooop --version", shell=True) != 0):
            self.askForScoopInstall.emit()
        else:
            print("[   OK   ] Scoop found")
    
    def scoopNotFound(self) -> None:
        if(QtWidgets.QMessageBox.question(self, "Warning", "Scoop was not found on the system. Do you want to install scoop?", QtWidgets.QMessageBox.No | QtWidgets.QMessageBox.Yes, QtWidgets.QMessageBox.No) == QtWidgets.QMessageBox.Yes):
            self.layout.addWidget(PackageInstaller("Scoop", "PowerShell", "", None, "powershell -Command \"Set-ExecutionPolicy RemoteSigned -scope CurrentUser;Invoke-Expression (New-Object System.Net.WebClient).DownloadString('https://get.scoop.sh')\""))
        

    def hideLoadingWheelIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", still loading...")
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", still loading...")
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount()))
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        #self.loadWheel.move(g.x()+g.width()//2-32, g.y()+g.height()//2-32)
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        item = QtWidgets.QTreeWidgetItem()
        item.setText(0, name)
        item.setText(1, id)
        item.setIcon(0, self.installIcon)
        item.setIcon(1, self.IDIcon)
        item.setIcon(2, self.versionIcon)
        item.setIcon(3, self.providerIcon)
        item.setText(3, store)
        item.setText(2, version)
        self.packageList.addTopLevelItem(item)
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), QtCore.Qt.MatchContains, 0)
        resultsFound += self.packageList.findItems(self.query.text(), QtCore.Qt.MatchContains, 1)
        print(f"[   OK   ] Searching for string \"{self.query.text()}\"")
        for item in self.packageList.findItems('', QtCore.Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def openInfo(self, title: str, id: str, store: str) -> None:
        if("…" in title):
            self.infobox.loadProgram(id.replace("…", ""), id.replace("…", ""), goodTitle=False, store=store)
        else:
            self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), goodTitle=True, store=store)
        self.programbox.hide()
        self.infobox.show()
    
    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
    
    def addInstallation(self, p) -> None:
        self.layout.addWidget(p)
        #self.installersScrollArea.show()

class About(QtWidgets.QScrollArea):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.widget = QtWidgets.QWidget()
        self.setWidgetResizable(True)
        self.setStyleSheet("margin-left: 0px;")
        self.layout = QtWidgets.QVBoxLayout()
        self.widget.setLayout(self.layout)
        self.setWidget(self.widget)
        self.layout.addWidget(QtWidgets.QLabel())

        title = QtWidgets.QLabel("About WingetUI Store v0.4")
        title.setStyleSheet("font-size: 40px;")

        self.layout.addWidget(title)
        self.layout.addWidget(QtWidgets.QLabel())

        description = QtWidgets.QLabel("The main goal of this project is to give a GUI Store to the most common CLI Package Managers for windows, such as Winget and Scoop.\nThis project has no connection with the winget-cli official project, and it's totally unofficial.")
        self.layout.addWidget(description)
        self.layout.addWidget(QLinkLabel(f"Project homepage (<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/martinet101/WinGetUI\">https://github.com/martinet101/WinGetUI</a>)"))
        self.layout.addWidget(QtWidgets.QLabel())
        button = QtWidgets.QPushButton("Add extra bucket (repo) to scoop")
        button.setFixedWidth(250)
        button.setFixedHeight(20)
        button.clicked.connect(lambda: self.scoopAddExtraBucket())
        self.layout.addWidget(button)
        button = QtWidgets.QPushButton("Remove extra bucket (repo) from scoop")
        button.setFixedWidth(250)
        button.setFixedHeight(20)
        button.clicked.connect(lambda: self.scoopRemoveExtraBucket())
        self.layout.addWidget(button)
        self.layout.addWidget(QLinkLabel("Licenses:", "font-size: 25px;"))
        self.layout.addWidget(QtWidgets.QLabel())
        self.layout.addWidget(QLinkLabel(f"WingetUI Store:&nbsp;&nbsp;&nbsp;&nbsp;LGPL v2.1:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/martinet101/WinGetUI/blob/main/LICENSE\">https://github.com/martinet101/WinGetUI/blob/main/LICENSE</a>"))
        self.layout.addWidget(QtWidgets.QLabel())
        self.layout.addWidget(QLinkLabel(f"PySide2:&nbsp;&nbsp;&nbsp;&nbsp;LGPLv3:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">https://www.gnu.org/licenses/lgpl-3.0.html</a>"))
        self.layout.addWidget(QLinkLabel(f"DarkDetect:&nbsp;&nbsp;&nbsp;&nbsp;Copyright (c) 2019, Alberto Sottile:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/albertosottile/darkdetect/blob/master/LICENSE\">https://github.com/albertosottile/darkdetect/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"QtModren:&nbsp;&nbsp;&nbsp;&nbsp;MIT License:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/gmarull/qtmodern/blob/master/LICENSE\">https://github.com/gmarull/qtmodern/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"Python3:&nbsp;&nbsp;&nbsp;&nbsp;Python Software Foundation License Agreement:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://docs.python.org/3/license.html#psf-license\">https://docs.python.org/3/license.html#psf-license</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"Winget:&nbsp;&nbsp;&nbsp;&nbsp;MIT License:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/microsoft/winget-cli/blob/master/LICENSE\">https://github.com/microsoft/winget-cli/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"Scoop:&nbsp;&nbsp;&nbsp;&nbsp;Unlicense:&nbsp;&nbsp;<a style=\"color: {Tools.blueColor};\" href=\"https://github.com/lukesampson/scoop/blob/master/LICENSE\">https://github.com/lukesampson/scoop/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"Icons by Icons8&nbsp;&nbsp;(<a style=\"color: {Tools.blueColor};\" href=\"https://icons8.com\">https://icons8.com</a>)"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel())
        button = QtWidgets.QPushButton("About Qt")
        button.setFixedWidth(150)
        button.setFixedHeight(20)
        button.clicked.connect(lambda: QtWidgets.QMessageBox.aboutQt(self, "WingetUI Store: About Qt"))
        self.layout.addWidget(button)
        self.layout.addWidget(QLinkLabel())
        button = QtWidgets.QPushButton("Update/Reinstall WingetUI Store")
        button.setFixedWidth(250)
        button.setFixedHeight(20)
        button.clicked.connect(lambda: self.layout.addWidget(PackageInstaller("WingetUI Store", "winget")))
        self.layout.addWidget(button)
        self.layout.addWidget(QtWidgets.QWidget(), stretch=1)
    
        print("[   OK   ] About tab loaded!")
        
    def scoopAddExtraBucket(self) -> None:
        os.startfile(os.path.join(realpath, "scoopAddExtrasBucket.cmd"))
        self.raise_()
        self.show()
    
    def scoopRemoveExtraBucket(self) -> None:
        os.startfile(os.path.join(realpath, "scoopRemoveExtrasBucket.cmd"))
        self.raise_()
        self.show()



class Installed(QtWidgets.QWidget):
    def __init__(self, parent=None):
        super().__init__(parent=parent)

class Update(QtWidgets.QWidget):
    def __init__(self, parent=None):
        super().__init__(parent=parent)

class LoadingProgress(QtWidgets.QLabel):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.movie = QtGui.QMovie(realpath+"/loading.gif")
        self.movie.start()
        self.setMovie(self.movie)
        self.show()
    
    def resizeEvent(self, event):
        super().resizeEvent(event)
        self.movie.setScaledSize(self.size())

class QLinkLabel(QtWidgets.QLabel):
    def __init__(self, text: str = "", stylesheet: str = ""):
        super().__init__(text)
        self.setStyleSheet(stylesheet)
        self.setTextFormat(QtCore.Qt.RichText)
        self.setTextInteractionFlags(QtCore.Qt.TextBrowserInteraction)
        self.setWordWrap(True)
        self.setOpenExternalLinks(True)

class QInfoProgressDialog(QtWidgets.QProgressDialog):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setFixedWidth(300)
        
    def addTextLine(self, text: str) -> None:
        self.setLabelText("Downloading and installing, please wait...\n\n"+text)

class PackageInstaller(QtWidgets.QGroupBox):
    onCancel = QtCore.Signal()
    killSubprocess = QtCore.Signal()
    addInfoLine = QtCore.Signal(str)
    finishInstallation = QtCore.Signal(int, str)
    counterSignal = QtCore.Signal(int)
    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId=""):
        super().__init__(parent=parent)
        self.store = store.lower()
        self.customCommand = customCommand
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(45)
        self.programName = title
        self.packageId = packageId
        self.version = version
        self.cmdline_args = args
        self.layout = QtWidgets.QHBoxLayout()
        self.label = QtWidgets.QLabel(title+" installation")
        self.label.setFixedWidth(230)
        self.layout.addWidget(self.label)
        self.progressbar = QtWidgets.QProgressBar()
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 10)
        self.progressbar.setValue(0)
        self.progressbar.setFixedHeight(6)
        self.layout.addWidget(self.progressbar)
        self.info = QtWidgets.QLineEdit()
        self.info.setText("Waiting for other installations to finish...")
        self.info.setReadOnly(True)
        self.addInfoLine.connect(lambda text: self.info.setText(text))
        self.finishInstallation.connect(self.finish)
        self.layout.addWidget(self.info)
        self.counterSignal.connect(self.counter)
        self.cancelButton = QtWidgets.QPushButton(QtGui.QIcon(realpath+"/cancel.png"), "Cancel")
        self.cancelButton.clicked.connect(self.cancel)
        self.layout.addWidget(self.cancelButton)
        self.setLayout(self.layout)
        self.canceled = False
        self.installId = str(time.time())
        Tools.queueProgram(self.installId)
        self.waitThread = Tools.KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        print(f"[   OK   ] Waiting for install permission... title={self.programName}, id={self.packageId}, installId={self.installId}")
        

    
    def startInstallation(self) -> None:
        while self.installId != Tools.current_program:
            time.sleep(0.2)
        print("[   OK   ] Have permission to install, starting installation threads...")
        if(self.store == "winget"):
            self.p = subprocess.Popen(["winget", "install", "-e", "--name", f"{self.programName}"] + self.version + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=WingetTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif(self.store == "scoop"):
            self.p = subprocess.Popen(' '.join(["scoop", "install", f"{self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=ScoopTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=Tools.genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

    
    def counter(self, line: int) -> None:
        if(line == 1):
            self.progressbar.setValue(1)
        if(line == 4):
            self.progressbar.setValue(4)
        elif(line == 6):
            self.cancelButton.setEnabled(False)
            self.progressbar.setValue(7)

    def cancel(self):
        print("[        ] Sending cancel signal...")
        self.info.setText("Installation canceled by user!")
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText("Close")
        self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(0)
        self.canceled=True
        Tools.removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
    
    def finish(self, returncode: int, output: str = "") -> None:
        self.cancelButton.setEnabled(True)
        Tools.removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
        if not(self.canceled):
            if(returncode == 0):
                Tools.notify("WingetUI Store", f"{self.programName} was installed successfully!")
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/tick.png"))
                self.cancelButton.clicked.connect(self.close)
                self.info.setText(f"{self.programName} was installed successfully!")
                self.progressbar.setValue(10)
                if(self.store == "powershell"):
                    msgBox = QtWidgets.QMessageBox(self)
                    msgBox.setWindowTitle("WingetUI Store")
                    msgBox.setText(f"{self.programName} was installed successfully.")
                    msgBox.setInformativeText(f"You will need to restart the application in order to get the {self.programName} new packages")
                    msgBox.setStandardButtons(QtWidgets.QMessageBox.Ok)
                    msgBox.setDefaultButton(QtWidgets.QMessageBox.Ok)
                    msgBox.setIcon(QtWidgets.QMessageBox.Information)
                    msgBox.exec_()
            else:
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.progressbar.setValue(10)
                msgBox = QtWidgets.QMessageBox(self)
                msgBox.setWindowTitle("WingetUI Store")
                if(returncode == 2):
                    Tools.notify("WingetUI Store", f"The hash of the installer does not coincide with the hash specified in the manifest. {self.programName} installation has been aborted")
                    self.info.setText(f"The hash of the installer does not coincide with the hash specified in the manifest. {self.programName} installation has been aborted")
                    msgBox.setText(f"The hash of the installer does not coincide with the hash specified in the manifest. {self.programName} installation has been aborted")
                else:
                    Tools.notify("WingetUI Store", f"An error occurred while installing {self.programName}")
                    self.info.setText(f"An error occurred during {self.programName} installation!")
                    msgBox.setText(f"An error occurred while installing {self.programName}")
                msgBox.setInformativeText("Click \"Show Details\" to get the output of the installer.")
                msgBox.setDetailedText(output)
                msgBox.setStandardButtons(QtWidgets.QMessageBox.Ok)
                msgBox.setDefaultButton(QtWidgets.QMessageBox.Ok)
                msgBox.setIcon(QtWidgets.QMessageBox.Warning)
                msgBox.exec_()

class Program(QtWidgets.QScrollArea):
    onClose = QtCore.Signal()
    loadInfo = QtCore.Signal(dict)
    closeDialog = QtCore.Signal()
    addProgram = QtCore.Signal(PackageInstaller)
    def __init__(self):
        super().__init__()
        self.store = ""
        self.setWidgetResizable(True)
        self.setStyleSheet("""
        QScrollArea{
            border-radius: 5px;
            padding: 5px;
        }
        """)
        
        self.vLayout = QtWidgets.QVBoxLayout()
        self.layout = QtWidgets.QVBoxLayout()
        self.title = QtWidgets.QLabel()
        self.title.setStyleSheet("font-size: 40px;")
        self.title.setText("Loading...")

        fortyWidget = QtWidgets.QWidget()
        fortyWidget.setFixedWidth(120)

        fortyTopWidget = QtWidgets.QWidget()
        fortyTopWidget.setFixedWidth(120)
        fortyTopWidget.setMinimumHeight(30)

        self.mainGroupBox = QtWidgets.QGroupBox()

        self.vLayout.addWidget(fortyTopWidget)
        self.layout.addWidget(self.title)
        self.layout.addWidget(QtWidgets.QLabel())

        self.hLayout = QtWidgets.QHBoxLayout()
        self.hLayout.addWidget(fortyWidget, stretch=1)

        self.description = QtWidgets.QLabel("Description: Unknown")
        self.description.setWordWrap(True)

        self.layout.addWidget(self.description)

        self.homepage = QLinkLabel("Homepage URL: Unknown")
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = QtWidgets.QLabel("Publisher: Unknown")
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = QtWidgets.QLabel("Author: Unknown")
        self.author.setWordWrap(True)

        self.layout.addWidget(self.author)
        self.layout.addWidget(QtWidgets.QLabel())

        self.license = QLinkLabel("License: Unknown")
        self.license.setWordWrap(True)

        self.layout.addWidget(self.license)
        self.layout.addWidget(QtWidgets.QLabel())
        
        hLayout = QtWidgets.QHBoxLayout()
        self.versionLabel = QtWidgets.QLabel("Version: ")
        self.versionCombo = QtWidgets.QComboBox()
        self.versionCombo.setFixedWidth(150)
        self.versionCombo.setIconSize(QtCore.QSize(24, 24))
        self.versionCombo.setFixedHeight(25)
        self.forceCheckbox = QtWidgets.QCheckBox()
        self.forceCheckbox.setText("Force")
        self.forceCheckbox.setChecked(False)
        self.installButton = QtWidgets.QPushButton()
        self.installButton.setText("Install")
        self.installButton.setIcon(QtGui.QIcon(realpath+"/install.png"))
        self.installButton.setIconSize(QtCore.QSize(24, 24))
        self.installButton.clicked.connect(self.install)
        self.installButton.setFixedWidth(150)
        self.installButton.setFixedHeight(30)

        downloadGroupBox = QtWidgets.QGroupBox()
        downloadGroupBox.setMinimumHeight(80)

        hLayout.addWidget(self.versionLabel)
        hLayout.addWidget(self.versionCombo)
        hLayout.addWidget(QtWidgets.QLabel(), stretch=1)
        hLayout.addWidget(self.forceCheckbox)
        hLayout.addWidget(self.installButton)
        downloadGroupBox.setLayout(hLayout)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addWidget(QtWidgets.QLabel())

        self.packageId = QLinkLabel("Program ID: Unknown")
        self.packageId.setWordWrap(True)
        self.layout.addWidget(self.packageId)
        self.manifest = QLinkLabel("Manifest: Unknown")
        self.manifest.setWordWrap(True)
        self.layout.addWidget(self.manifest)
        self.sha = QLinkLabel("Installer SHA256 (Lastest version): Unknown")
        self.sha.setWordWrap(True)
        self.layout.addWidget(self.sha)
        self.link = QLinkLabel("Installer URL (Lastest version): Unknown")
        self.link.setWordWrap(True)
        self.layout.addWidget(self.link)
        self.type = QLinkLabel("Installer type (Lastest version): Unknown")
        self.type.setWordWrap(True)
        self.layout.addWidget(self.type)
        self.storeLabel = QLinkLabel(f"Source: {self.store}")
        self.storeLabel.setWordWrap(True)
        self.layout.addWidget(self.storeLabel)
        self.layout.addWidget(QtWidgets.QLabel())
        self.advert = QLinkLabel("ALERT: NEITHER MICROSOFT NOR THE CREATORS OF WINGET UI STORE ARE RESPONSIBLE FOR THE DOWNLOADED SOFTWARE. PROCEED WITH CAUTION")
        self.advert.setWordWrap(True)
        self.layout.addWidget(self.advert)

        self.mainGroupBox.setLayout(self.layout)
        self.mainGroupBox.setMinimumHeight(480)
        self.vLayout.addWidget(self.mainGroupBox)
        self.vLayout.addWidget(fortyWidget, stretch=1)
        self.hLayout.addLayout(self.vLayout, stretch=0)
        self.hLayout.addWidget(fortyWidget, stretch=1)

        self.centralwidget = QtWidgets.QWidget()
        self.centralwidget.setLayout(self.hLayout)
        if(darkdetect.isDark()):
            print("[        ] Is Dark")
            self.centralwidget.setAttribute(QtCore.Qt.WA_NoSystemBackground)
        self.setWidget(self.centralwidget)


        self.backButton = QtWidgets.QPushButton(QtGui.QIcon(realpath+"/back.png"), "", self)
        self.backButton.setStyleSheet("font-size: 22px;")
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(0, 0)
        self.backButton.resize(30, 30)
        self.backButton.clicked.connect(self.onClose.emit)
        self.backButton.show()

        
        self.loadWheel = LoadingProgress(self)
        self.loadWheel.resize(64, 64)
        self.loadWheel.hide()
        self.hide()

        self.loadInfo.connect(self.printData)
    
    def resizeEvent(self, event = None):
        self.centralwidget.setFixedWidth(self.width()-18)
        g = self.mainGroupBox.geometry()
        self.loadWheel.move(g.x()+g.width()//2-32, g.y()+g.height()//2-32)
        if(event):
            return super().resizeEvent(event)
    
    def loadProgram(self, title: str, id: str, goodTitle: bool, store: str) -> None:
        self.store = store
        store = store.lower()
        if(darkdetect.isDark()):
            blueColor = "CornflowerBlue"
        else:
            blueColor = "blue"
        if(goodTitle):
            self.title.setText(title)
        else:
            self.title.setText(id)
            
        self.loadWheel.show()
        self.forceCheckbox.setChecked(False)
        self.forceCheckbox.setEnabled(False)
        self.description.setText("Loading...")
        self.author.setText("Author: "+"Loading...")
        self.publisher.setText("Publisher: "+"Loading...")
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"\">{'Loading...'}</a>")
        self.license.setText(f"License: {'Loading...'} (<a style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {'Loading...'}")
        self.link.setText(f"Installer URL (Lastest version): <a  style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>")
        self.type.setText(f"Installer type (Lastest version): {'Loading...'}")
        self.packageId.setText(f"Package ID: {'Loading...'}")
        self.manifest.setText(f"Manifest: {'Loading...'}")
        self.storeLabel.setText(f"Source: {self.store.capitalize()}")
        self.versionCombo.addItems(["Loading..."])
        
        if(store=="winget"):
            Thread(target=WingetTools.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()
        elif(store=="scoop"):
            Thread(target=ScoopTools.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()

    def printData(self, appInfo: dict) -> None:
        if(darkdetect.isDark()):
            blueColor = "CornflowerBlue"
        else:
            blueColor = "blue"
        self.loadWheel.hide()
        if(self.store.lower() == "winget"):
            self.forceCheckbox.setEnabled(True)
        self.title.setText(appInfo["title"])
        self.description.setText(appInfo["description"])
        self.author.setText("Author: "+appInfo["author"])
        self.publisher.setText("Publisher: "+appInfo["publisher"])
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"{appInfo['homepage']}\">{appInfo['homepage']}</a>")
        self.license.setText(f"License: {appInfo['license']} (<a style=\"color: {blueColor};\" href=\"{appInfo['license-url']}\">{appInfo['license-url']}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {appInfo['installer-sha256']}")
        self.link.setText(f"Installer URL (Lastest version): <a style=\"color: {blueColor};\" href=\"{appInfo['installer-url']}\">{appInfo['installer-url']}</a>")
        self.type.setText(f"Installer type (Lastest version): {appInfo['installer-type']}")
        self.packageId.setText(f"Package ID: {appInfo['id']}")
        self.manifest.setText(f"Manifest: <a style=\"color: {blueColor};\" href=\"file:///"+appInfo['manifest'].replace('\\', '/')+f"\">{appInfo['manifest']}</a>")
        while self.versionCombo.count()>0:
            self.versionCombo.removeItem(0)
        try:
            self.versionCombo.addItems(["Lastest"] + appInfo["versions"])
        except KeyError:
            pass
        for i in range(self.versionCombo.count()):
            self.versionCombo.setItemIcon(i, QtGui.QIcon(realpath+"/version.png"))

    def install(self):
        title = self.title.text()
        packageId = self.packageId.text().replace('Package ID:', '').strip()
        print(f"[   OK   ] Starting installation of package {title} with id {packageId}")
        cmdline_args = []
        if(self.forceCheckbox.isChecked()):
            cmdline_args.append("--force")
        if(self.versionCombo.currentText()=="Lastest"):
            version = []
        else:
            version = ["--version", self.versionCombo.currentText()]
            print(f"[  WARN  ]Issuing specific version {self.versionCombo.currentText()}")
        p = PackageInstaller(title, self.store, version, args=cmdline_args, packageId=packageId)
        self.addProgram.emit(p)
        



if(__name__=="__main__"):
    import __init__