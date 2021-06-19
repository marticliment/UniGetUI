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

    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = Program()
        self.infobox.onClose.connect(self.showQuery)

        self.programbox = QtWidgets.QWidget()

        self.layout = QtWidgets.QVBoxLayout()
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon(realpath+"/reload.png"))

        hLayout = QtWidgets.QHBoxLayout()

        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search something on Winget")
        self.query.textChanged.connect(self.filter)
        self.query.setFixedHeight(30)

        self.discoverLabel = QtWidgets.QLabel("Discover packages")
        self.discoverLabel.setStyleSheet("font-size: 40px;")

        hLayout.addWidget(self.query)
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

        layout = QtWidgets.QVBoxLayout()

        layout.addWidget(self.discoverLabel)
        layout.addWidget(QtWidgets.QLabel())
        layout.addLayout(hLayout)
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

        self.loadWheel = LoadingProgress(self)
        self.loadWheel.resize(64, 64)

        self.hideLoadingWheel.connect(self.hideLoadingWheelIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        

        self.reloadButton.setEnabled(False)
        self.query.setEnabled(False)
    

        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
        self.loadWheel.move(g.x()+g.width()//2-32, g.y()+g.height()//2-32)

    def hideLoadingWheelIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.wingetLoaded = True
        elif(store == "scoop"):
            self.scoopLoaded = True
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadWheel.hide()
            self.reloadButton.setEnabled(True)
            self.query.setEnabled(True)

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        self.loadWheel.move(g.x()+g.width()//2-32, g.y()+g.height()//2-32)
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        item = QtWidgets.QTreeWidgetItem()
        item.setText(0, name)
        item.setText(1, id)
        item.setIcon(0, QtGui.QIcon(realpath+"/install.png"))
        item.setIcon(1, QtGui.QIcon(realpath+"/ID.png"))
        item.setIcon(2, QtGui.QIcon(realpath+"/version.png"))
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
        self.loadWheel.show()
        self.reloadButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
    
    def addInstallation(self, p) -> None:
        self.layout.addWidget(p)
        #self.installersScrollArea.show()
    

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
    def __init__(self, text=""):
        super().__init__(text)
        self.setTextFormat(QtCore.Qt.RichText)
        self.setTextInteractionFlags(QtCore.Qt.TextBrowserInteraction)
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
    def __init__(self, title: str, store: str, version: str = "", parent=None, startInstall = True):
        super().__init__(parent=parent)
        self.store = store.lower()
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(45)
        self.programName = title
        self.version = version
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
        self.id = str(time.time())
        Tools.queueProgram(self.id)
        self.waitThread = Tools.KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        print("[   OK   ] Waiting for install permission...")
        

    
    def startInstallation(self) -> None:
        while self.id != Tools.current_program:
            time.sleep(0.2)
        print("[   OK   ] Have permission to install, starting installation threads...")
        if(self.store == "winget"):
            self.p = subprocess.Popen(["winget", "install", f"{self.programName}"] + self.version, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=False, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=WingetTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif(self.store == "scoop"):
            self.p = subprocess.Popen(' '.join(["scoop", "install", f"{self.programName}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=WingetTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
        Tools.removeProgram(self.id)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
    
    def finish(self, returncode: int, output: str = "") -> None:
        self.cancelButton.setEnabled(True)
        Tools.removeProgram(self.id)
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
            else:
                Tools.notify("WingetUI Store", f"An error occurred while installing {self.programName}")
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.info.setText(f"An error occurred during {self.programName} installation!")
                self.progressbar.setValue(10)
                msgBox = QtWidgets.QMessageBox(self)
                msgBox.setWindowTitle("WingetUI Store")
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
        self.progressDialog = QInfoProgressDialog(self)
        self.progressDialog.setWindowTitle("Winget UI Store")
        self.progressDialog.setModal(True)
        self.progressDialog.setSizePolicy(QtWidgets.QSizePolicy.Fixed, QtWidgets.QSizePolicy.Fixed)
        self.progressDialog.setWindowFlag(QtCore.Qt.WindowContextHelpButtonHint, False)
        self.progressDialog.setWindowFlag(QtCore.Qt.WindowCloseButtonHint, False)
        bar = QtWidgets.QProgressBar()
        bar.setTextVisible(False)
        self.progressDialog.setBar(bar)
        self.progressDialog.setCancelButton(None)
        self.progressDialog.setLabelText("Processing, please wait...")
        self.progressDialog.setRange(0, 0)
        self.progressDialog.close()


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

        self.closeDialog.connect(self.progressDialog.close)

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
        hLayout.addWidget(self.installButton)
        downloadGroupBox.setLayout(hLayout)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addWidget(QtWidgets.QLabel())

        self.id = QLinkLabel("Program ID: Unknown")
        self.id.setWordWrap(True)
        self.layout.addWidget(self.id)
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
        self.centralwidget.setAttribute(QtCore.Qt.WA_NoSystemBackground) 
        self.setWidget(self.centralwidget)


        self.backButton = QtWidgets.QPushButton(QtGui.QIcon(realpath+"/back.png"), "", self)
        self.backButton.setStyleSheet("font-size: 22px;")
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
    
    def closeAndInform(self, returncode: int) -> None:
        print(returncode)
        self.progressDialog.close()
        if(returncode==0):
            QtWidgets.QMessageBox.information(self, "Winget UI Store", f"The package {self.title.text()} was installed successfully. (Winget output code is 0)")
        else:
            QtWidgets.QMessageBox.warning(self, "Winget UI Store", f"An error occurred while installing the package {self.title.text()}. (Winget output code is {returncode})")
    
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
        self.description.setText("Loading...")
        self.author.setText("Author: "+"Loading...")
        self.publisher.setText("Publisher: "+"Loading...")
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"\">{'Loading...'}</a>")
        self.license.setText(f"License: {'Loading...'} (<a style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {'Loading...'}")
        self.link.setText(f"Installer URL (Lastest version): <a  style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>")
        self.type.setText(f"Installer type (Lastest version): {'Loading...'}")
        self.id.setText(f"Package ID: {'Loading...'}")
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
        self.title.setText(appInfo["title"])
        self.description.setText(appInfo["description"])
        self.author.setText("Author: "+appInfo["author"])
        self.publisher.setText("Publisher: "+appInfo["publisher"])
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"{appInfo['homepage']}\">{appInfo['homepage']}</a>")
        self.license.setText(f"License: {appInfo['license']} (<a style=\"color: {blueColor};\" href=\"{appInfo['license-url']}\">{appInfo['license-url']}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {appInfo['installer-sha256']}")
        self.link.setText(f"Installer URL (Lastest version): <a style=\"color: {blueColor};\" href=\"{appInfo['installer-url']}\">{appInfo['installer-url']}</a>")
        self.type.setText(f"Installer type (Lastest version): {appInfo['installer-type']}")
        self.id.setText(f"Package ID: {appInfo['id']}")
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
        print(f"[   OK   ] Starting installation of package {title}")
        if(self.versionCombo.currentText()=="Lastest"):
            version = []
            self.progressDialog.setLabelText(f"Downloading and installing {title}...")
        else:
            version = ["--version", self.versionCombo.currentText()]
            print(f"[  WARN  ]Issuing specific version {self.versionCombo.currentText()}")
            self.progressDialog.setLabelText(f"Downloading and installing {title} version {self.versionCombo.currentText()}...")
        p = PackageInstaller(title, self.store, version)
        self.addProgram.emit(p)
        



if(__name__=="__main__"):
    import __init__