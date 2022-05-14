from posixpath import relpath
from PySide2 import QtWidgets, QtCore, QtGui
import WingetTools, ScoopTools, darkdetect, sys, Tools, subprocess, time, os
from threading import Thread
from PySide2 import QtCore, QtGui, QtWidgets
from PySide2.QtCore import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *


if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

class Uninstall(QtWidgets.QWidget):

    addProgram = QtCore.Signal(str, str, str, str)
    hideLoadingWheel = QtCore.Signal(str)
    clearList = QtCore.Signal()
    askForScoopInstall = QtCore.Signal(str)
    setLoadBarValue = QtCore.Signal(str)
    startAnim = QtCore.Signal(QtCore.QVariantAnimation)
    changeBarOrientation = QtCore.Signal()

    def __init__(self, installerswidget, parent=None):
        self.installerswidget = installerswidget
        super().__init__(parent=parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = Program()
        self.setStyleSheet("margin: 0px;")
        self.infobox.onClose.connect(self.showQuery)

        self.programbox = QtWidgets.QWidget()

        self.layout = QtWidgets.QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon(Tools.getMedia("reload")))

        self.searchButton = QtWidgets.QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QtGui.QIcon(Tools.getMedia("search")))

        hLayout = QtWidgets.QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 0, 0)

        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search on your software")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(self.filter)
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(20)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(90)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(Tools.getMedia("red_trash")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QtWidgets.QLabel("Installed packages")
        self.discoverLabel.setStyleSheet("font-size: 40px;")
        v.addWidget(self.discoverLabel)

        hLayout.addLayout(v)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        
        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = Tools.TreeWidget("a")
        self.packageList.setIconSize(QtCore.QSize(24, 24))
        self.packageList.setColumnCount(4)
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Installed Version", "Installation source"])
        self.packageList.setColumnWidth(0, 300)
        self.packageList.setColumnWidth(1, 300)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnHidden(2, True)
        self.packageList.setColumnWidth(3, 100)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.packageList.setVerticalScrollMode(QtWidgets.QTreeWidget.ScrollPerPixel)
        self.packageList.sortByColumn(0, QtCore.Qt.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.uninstall(item.text(0), item.text(1), item.text(3)))
        
        
        
        self.loadingProgressBar = QtWidgets.QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        
        layout = QtWidgets.QVBoxLayout()
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1000)

        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)


        self.countLabel = QtWidgets.QLabel("Searching for installed packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.setContentsMargins(20, 10, 0, 10)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        self.layout.addWidget(self.infobox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)
        self.askForScoopInstall.connect(self.scoopNotFound)

        self.hideLoadingWheel.connect(self.hideLoadingWheelIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QtGui.QIcon(Tools.getMedia("install"))
        self.IDIcon = QtGui.QIcon(Tools.getMedia("ID"))
        self.versionIcon = QtGui.QIcon(Tools.getMedia("version"))
        self.providerIcon = QtGui.QIcon(Tools.getMedia("provider"))
        
    

        Thread(target=WingetTools.searchForInstalledPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForInstalledPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
            
        Thread(target=self.checkIfScoop, daemon=True)
        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()

    
    def checkIfScoop(self) -> None:
        if(subprocess.call("scooop --version", shell=True) != 0):
            self.askForScoopInstall.emit()
        else:
            print("[   OK   ] Scoop found")
    
    def scoopNotFound(self) -> None:
        if(Tools.MessageBox.question(self, "Warning", "Scoop was not found on the system. Do you want to install scoop?", Tools.MessageBox.No | Tools.MessageBox.Yes, Tools.MessageBox.No) == Tools.MessageBox.Yes):
            self.layout.addWidget(PackageInstaller("Scoop", "PowerShell", "", None, "powershell -Command \"Set-ExecutionPolicy RemoteSigned -scope CurrentUser;Invoke-Expression (New-Object System.Net.WebClient).DownloadString('https://get.scoop.sh')\""))
        

    def hideLoadingWheelIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount()))
            self.packageList.label.setText(self.countLabel.text())
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        if not "---" in name:
            item = QtWidgets.QTreeWidgetItem()
            item.setText(0, name)
            item.setText(1, id)
            item.setIcon(0, self.installIcon)
            item.setIcon(1, self.IDIcon)
            item.setIcon(3, self.providerIcon)
            item.setText(3, store)
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

    def uninstall(self, title: str, id: str, store: str) -> None:
        if(Tools.MessageBox.question(self, "Are you sure?", f"Do you really want to uninstall {title}", Tools.MessageBox.No | Tools.MessageBox.Yes, Tools.MessageBox.Yes) == Tools.MessageBox.Yes):
           
            if("…" in title):
                self.addInstallation(PackageUninstaller(title, store, useId=True, packageId=id.replace("…", "")))
            else:
                self.addInstallation(PackageUninstaller(title, store, packageId=id.replace("…", "")))
    
    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        self.countLabel.setText("Searching for installed packages...")
        self.packageList.label.setText(self.countLabel.text())
        Thread(target=WingetTools.searchForInstalledPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForInstalledPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
    
    def addInstallation(self, p) -> None:
        self.installerswidget.addWidget(p)

class Discover(QtWidgets.QWidget):

    addProgram = QtCore.Signal(str, str, str, str)
    hideLoadingWheel = QtCore.Signal(str)
    clearList = QtCore.Signal()
    askForScoopInstall = QtCore.Signal(str)
    setLoadBarValue = QtCore.Signal(str)
    startAnim = QtCore.Signal(QtCore.QVariantAnimation)
    changeBarOrientation = QtCore.Signal()

    def __init__(self, installerswidget, parent=None):
        self.installerswidget = installerswidget
        super().__init__(parent=parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = Program(self)
        self.setStyleSheet("margin: 0px;")

        self.programbox = QtWidgets.QWidget()

        self.layout = QtWidgets.QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon(Tools.getMedia("reload")))

        self.searchButton = QtWidgets.QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QtGui.QIcon(Tools.getMedia("search")))

        hLayout = QtWidgets.QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 0, 0)


        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(20)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(90)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)
         
        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search something on Winget or Scoop")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(Tools.getMedia("store_logo")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QtWidgets.QLabel("Discover packages")
        self.discoverLabel.setStyleSheet("font-size: 40px;")
        v.addWidget(self.discoverLabel)

        hLayout.addLayout(v)
        hLayout.addStretch()
        forceCheckBox = QVBoxLayout()
        forceCheckBox.addWidget(self.forceCheckBox)
        hLayout.addLayout(forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = Tools.TreeWidget("a")
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Version", "Origin"])
        self.packageList.setColumnCount(4)
        self.packageList.setColumnWidth(0, 300)
        self.packageList.setColumnWidth(1, 300)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnWidth(3, 100)
        self.packageList.sortByColumn(0, QtCore.Qt.AscendingOrder)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.packageList.setVerticalScrollMode(QtWidgets.QTreeWidget.ScrollPerPixel)
        self.packageList.setIconSize(QtCore.QSize(24, 24))
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item.text(0), item.text(1), item.text(3)))
        
        
        self.loadingProgressBar = QtWidgets.QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        layout = QtWidgets.QVBoxLayout()
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1000)

        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)


        self.countLabel = QtWidgets.QLabel("Searching for packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        v.addWidget(self.countLabel)
        layout.addLayout(hLayout)
        layout.setContentsMargins(20, 10, 0, 10)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        # pass
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)
        self.askForScoopInstall.connect(self.scoopNotFound)

        self.hideLoadingWheel.connect(self.hideLoadingWheelIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QtGui.QIcon(Tools.getMedia("install"))
        self.IDIcon = QtGui.QIcon(Tools.getMedia("ID"))
        self.versionIcon = QtGui.QIcon(Tools.getMedia("newversion"))
        self.providerIcon = QtGui.QIcon(Tools.getMedia("provider"))
        
    

        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
            
        Thread(target=self.checkIfScoop, daemon=True)
        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()
        
    
    
    def checkIfScoop(self) -> None:
        if(subprocess.call("scooop --version", shell=True) != 0):
            self.askForScoopInstall.emit()
        else:
            print("[   OK   ] Scoop found")
    
    def scoopNotFound(self) -> None:
        if(Tools.MessageBox.question(self, "Warning", "Scoop was not found on the system. Do you want to install scoop?", Tools.MessageBox.No | Tools.MessageBox.Yes, Tools.MessageBox.No) == Tools.MessageBox.Yes):
            self.layout.addWidget(PackageInstaller("Scoop", "PowerShell", "", None, "powershell -Command \"Set-ExecutionPolicy RemoteSigned -scope CurrentUser;Invoke-Expression (New-Object System.Net.WebClient).DownloadString('https://get.scoop.sh')\""))
        

    def hideLoadingWheelIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount()))
            self.packageList.label.setText(self.countLabel.text())
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        if not "---" in name:
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
        self.infobox.show()
        from win32mica import ApplyMica, MICAMODE
        Tools.ApplyMenuBlur(self.infobox.winId(),self.infobox, avoidOverrideStyleSheet=True, shadow=False)
    
    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        self.countLabel.setText("Searching for packages...")
        self.packageList.label.setText(self.countLabel.text())
        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForPackage, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
    
    def addInstallation(self, p) -> None:
        self.installerswidget.addWidget(p)

class Upgrade(QtWidgets.QWidget):

    addProgram = QtCore.Signal(str, str, str, str, str)
    hideLoadingWheel = QtCore.Signal(str)
    clearList = QtCore.Signal()
    askForScoopInstall = QtCore.Signal(str)
    setLoadBarValue = QtCore.Signal(str)
    startAnim = QtCore.Signal(QtCore.QVariantAnimation)
    changeBarOrientation = QtCore.Signal()

    def __init__(self, installerswidget, parent=None):
        self.installerswidget = installerswidget
        super().__init__(parent=parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = Program()
        self.setStyleSheet("margin: 0px;")
        self.infobox.onClose.connect(self.showQuery)

        self.programbox = QtWidgets.QWidget()

        self.layout = QtWidgets.QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon(Tools.getMedia("reload")))

        self.searchButton = QtWidgets.QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QtGui.QIcon(Tools.getMedia("search")))

        hLayout = QtWidgets.QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 0, 0)

        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search available updates")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(self.filter)
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(20)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(90)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(Tools.getMedia("upgrade")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QtWidgets.QLabel("Available updates")
        self.discoverLabel.setStyleSheet("font-size: 40px;")
        v.addWidget(self.discoverLabel)

        hLayout.addLayout(v)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        
        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = Tools.TreeWidget("a")
        self.packageList.setIconSize(QtCore.QSize(24, 24))
        self.packageList.setColumnCount(5)
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Installed Version", "New Version", "Installation source"])
        self.packageList.setColumnWidth(0, 350)
        self.packageList.setColumnWidth(1, 200)
        self.packageList.setColumnWidth(2, 125)
        self.packageList.setColumnWidth(3, 125)
        self.packageList.setColumnWidth(4, 100)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.packageList.setVerticalScrollMode(QtWidgets.QTreeWidget.ScrollPerPixel)
        self.packageList.sortByColumn(0, QtCore.Qt.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.update(item.text(0), item.text(1)))
        
        
        
        self.loadingProgressBar = QtWidgets.QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        
        layout = QtWidgets.QVBoxLayout()
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1000)

        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(QWidget(), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)


        self.countLabel = QtWidgets.QLabel("Checking for updates...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.setContentsMargins(20, 10, 0, 10)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        self.layout.addWidget(self.infobox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)
        self.askForScoopInstall.connect(self.scoopNotFound)

        self.hideLoadingWheel.connect(self.hideLoadingWheelIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QtGui.QIcon(Tools.getMedia("install"))
        self.IDIcon = QtGui.QIcon(Tools.getMedia("ID"))
        self.versionIcon = QtGui.QIcon(Tools.getMedia("version"))
        self.newVersionIcon = QtGui.QIcon(Tools.getMedia("newversion"))
        self.providerIcon = QtGui.QIcon(Tools.getMedia("provider"))
        
    

        Thread(target=WingetTools.searchForUpdates, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForUpdates, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        print("[   OK   ] Upgrades tab loaded")

        g = self.packageList.geometry()
            
        Thread(target=self.checkIfScoop, daemon=True)
        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()

    
    def checkIfScoop(self) -> None:
        if(subprocess.call("scooop --version", shell=True) != 0):
            self.askForScoopInstall.emit()
        else:
            print("[   OK   ] Scoop found")
    
    def scoopNotFound(self) -> None:
        if(Tools.MessageBox.question(self, "Warning", "Scoop was not found on the system. Do you want to install scoop?", Tools.MessageBox.No | Tools.MessageBox.Yes, Tools.MessageBox.No) == Tools.MessageBox.Yes):
            self.layout.addWidget(PackageInstaller("Scoop", "PowerShell", "", None, "powershell -Command \"Set-ExecutionPolicy RemoteSigned -scope CurrentUser;Invoke-Expression (New-Object System.Net.WebClient).DownloadString('https://get.scoop.sh')\""))
        

    def hideLoadingWheelIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount()))
            self.packageList.label.setText(self.countLabel.text())
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, newVersion: str, store) -> None:
        if not "---" in name:
            item = QtWidgets.QTreeWidgetItem()
            item.setText(0, name)
            item.setIcon(0, self.installIcon)
            item.setText(1, id)
            item.setIcon(1, self.IDIcon)
            item.setText(2, version)
            item.setIcon(2, self.versionIcon)
            item.setText(3, newVersion)
            item.setIcon(3, self.newVersionIcon)
            item.setText(4, store)
            item.setIcon(4, self.providerIcon)
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

    def update(self, title: str, id: str) -> None:
        if not "scoop" in id:
            if("…" in title):
                self.addInstallation(PackageUpdater(title, "winget", useId=True, packageId=id.replace("…", "")))
            else:
                self.addInstallation(PackageUpdater(title, "winget", packageId=id.replace("…", "")))
        else:
            if("…" in title):
                self.addInstallation(PackageUpdater(title, "scoop", useId=True, packageId=id.replace("…", "")))
            else:
                self.addInstallation(PackageUpdater(title, "scoop", packageId=id.replace("…", "")))

    
    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        self.countLabel.setText("Checking for updates...")
        self.packageList.label.setText(self.countLabel.text())
        Thread(target=WingetTools.searchForUpdates, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
        Thread(target=ScoopTools.searchForUpdates, args=(self.addProgram, self.hideLoadingWheel), daemon=True).start()
    
    def addInstallation(self, p) -> None:
        self.installerswidget.addWidget(p)

class About(QtWidgets.QScrollArea):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.widget = QtWidgets.QWidget()
        self.setWidgetResizable(True)
        self.setStyleSheet("margin-left: 0px;")
        self.layout = QtWidgets.QVBoxLayout()
        w = QWidget()
        w.setLayout(self.layout)
        w.setMaximumWidth(1000)
        l = QHBoxLayout()
        l.addStretch()
        l.addWidget(w)
        l.addStretch()
        self.widget.setLayout(l)
        self.setWidget(self.widget)
        self.layout.addWidget(QtWidgets.QLabel())

        title = QtWidgets.QLabel("About WingetUI Store "+str(Tools.version))
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
        button.clicked.connect(lambda: Tools.MessageBox.aboutQt(self, "WingetUI Store: About Qt"))
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
    changeBarOrientation = QtCore.Signal()
    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId=""):
        super().__init__(parent=parent)
        self.finishedInstallation = True
        self.setMinimumHeight(500)
        self.store = store.lower()
        self.customCommand = customCommand
        self.setObjectName("package")
        self.setFixedHeight(50)
        self.programName = title
        self.packageId = packageId
        self.version = version
        self.cmdline_args = args
        self.layout = QtWidgets.QHBoxLayout()
        self.layout.setContentsMargins(30, 10, 10, 10)
        self.label = QtWidgets.QLabel(title+" Installation")
        self.layout.addWidget(self.label)
        self.layout.addSpacing(5)
        self.progressbar = QtWidgets.QProgressBar()
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 1000)
        self.progressbar.setValue(0)
        self.progressbar.setFixedHeight(4)
        self.changeBarOrientation.connect(lambda: self.progressbar.setInvertedAppearance(not(self.progressbar.invertedAppearance())))
        self.layout.addWidget(self.progressbar, stretch=1)
        self.info = QtWidgets.QLineEdit()
        self.info.setText("Waiting for other installations to finish...")
        self.info.setReadOnly(True)
        self.addInfoLine.connect(lambda text: self.info.setText(text))
        self.finishInstallation.connect(self.finish)
        self.layout.addWidget(self.info)
        self.counterSignal.connect(self.counter)
        self.cancelButton = QtWidgets.QPushButton(QtGui.QIcon(realpath+"/cancel.png"), "Cancel")
        self.cancelButton.clicked.connect(self.cancel)
        self.cancelButton.setFixedHeight(30)
        self.info.setFixedHeight(30)
        self.layout.addWidget(self.cancelButton)
        self.setLayout(self.layout)
        self.canceled = False
        self.installId = str(time.time())
        Tools.queueProgram(self.installId)
        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(900)
        self.leftSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(900)
        self.rightSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()

        self.waitThread = Tools.KillableThread(target=self.startInstallation, daemon=True)
        self.waitThread.start()
        print(f"[   OK   ] Waiting for install permission... title={self.programName}, id={self.packageId}, installId={self.installId}")
        

    
    def startInstallation(self) -> None:
        while self.installId != Tools.current_program:
            time.sleep(0.2)
        self.finishedInstallation = False
        print("[   OK   ] Have permission to install, starting installation threads...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        if(self.store == "winget"):
            self.p = subprocess.Popen(["winget", "install", "-e", "--name", f"{self.programName}"] + self.version + WingetTools.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
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
            self.progressbar.setValue(250)
        if(line == 4):
            self.progressbar.setValue(500)
        elif(line == 6):
            self.cancelButton.setEnabled(False)
            self.progressbar.setValue(750)

    def cancel(self):
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        print("[        ] Sending cancel signal...")
        if not self.finishedInstallation:
            subprocess.Popen("taskkill /im winget.exe /f", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ).wait()
            self.finishedInstallation = True
        self.info.setText("Installation canceled by user!")
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText("Close")
        self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        Tools.removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
    
    def finish(self, returncode: int, output: str = "") -> None:
        self.finishedInstallation = True
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
                self.progressbar.setValue(1000)
                if(self.store == "powershell"):
                    msgBox = Tools.MessageBox(self)
                    msgBox.setWindowTitle("WingetUI Store")
                    msgBox.setText(f"{self.programName} was installed successfully.")
                    msgBox.setInformativeText(f"You will need to restart the application in order to get the {self.programName} new packages")
                    msgBox.setStandardButtons(Tools.MessageBox.Ok)
                    msgBox.setDefaultButton(Tools.MessageBox.Ok)
                    msgBox.setIcon(Tools.MessageBox.Information)
                    msgBox.exec_()
            else:
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.progressbar.setValue(1000)
                msgBox = Tools.MessageBox(self)
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
                msgBox.setStandardButtons(Tools.MessageBox.Ok)
                msgBox.setDefaultButton(Tools.MessageBox.Ok)
                msgBox.setIcon(Tools.MessageBox.Warning)
                msgBox.exec_()

class PackageUpdater(PackageInstaller):
    
    def startInstallation(self) -> None:
        while self.installId != Tools.current_program:
            time.sleep(0.2)
        self.finishedInstallation = False
        print("[   OK   ] Have permission to install, starting installation threads...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        if(self.store == "winget"):
            self.p = subprocess.Popen(["winget", "install", "-e", "--name", f"{self.programName}"] + self.version + WingetTools.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=WingetTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif(self.store == "scoop"):
            self.p = subprocess.Popen(' '.join(["scoop", "update", f"{self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=ScoopTools.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=Tools.genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

class PackageUninstaller(QtWidgets.QGroupBox):
    onCancel = QtCore.Signal()
    killSubprocess = QtCore.Signal()
    addInfoLine = QtCore.Signal(str)
    finishInstallation = QtCore.Signal(int, str)
    counterSignal = QtCore.Signal(int)
    changeBarOrientation = QtCore.Signal()
    def __init__(self, title: str, store: str, useId=False, packageId=""):
        super().__init__(parent=None)
        self.finishedInstallation = True
        self.store = store.lower()
        self.useId = useId
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(50)
        self.programName = title
        self.packageId = packageId
        self.layout = QtWidgets.QHBoxLayout()
        self.label = QtWidgets.QLabel(title+" Uninstallation")
        self.label.setFixedWidth(230)
        self.layout.addWidget(self.label)
        self.progressbar = QtWidgets.QProgressBar()
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 1000)
        self.progressbar.setValue(0)
        self.progressbar.setFixedHeight(4)
        self.changeBarOrientation.connect(lambda: self.progressbar.setInvertedAppearance(not(self.progressbar.invertedAppearance())))
        self.layout.addWidget(self.progressbar)
        self.info = QtWidgets.QLineEdit()
        self.info.setText("Waiting for other installations to finish...")
        self.info.setReadOnly(True)
        self.addInfoLine.connect(lambda text: self.info.setText(text))
        self.finishInstallation.connect(self.finish)
        self.layout.addWidget(self.info)
        self.counterSignal.connect(self.counter)
        self.cancelButton = QtWidgets.QPushButton(QtGui.QIcon(realpath+"/cancel.png"), "Cancel")
        self.cancelButton.setFixedHeight(30)
        self.info.setFixedHeight(30)
        self.cancelButton.clicked.connect(self.cancel)
        self.layout.addWidget(self.cancelButton)
        self.setLayout(self.layout)
        self.canceled = False
        self.installId = str(time.time())
        Tools.queueProgram(self.installId)
        self.waitThread = Tools.KillableThread(target=self.startUninstallation, daemon=True)
        self.waitThread.start()
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(900)
        self.leftSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(900)
        self.rightSlow.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.progressbar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()
        
        print(f"[   OK   ] Waiting for install permission... title={self.programName}, id={self.packageId}, installId={self.installId}")
        

    
    def startUninstallation(self) -> None:
        while self.installId != Tools.current_program:
            time.sleep(0.2)
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        self.finishedInstallation = False
        print("[   OK   ] Have permission to install, starting installation threads...")
        if(self.store == "winget"):
            if self.useId:
                self.p = subprocess.Popen([WingetTools.winget, "uninstall", "-e", "--id", f"{self.programName}"]+WingetTools.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
                self.t = Tools.KillableThread(target=WingetTools.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
                self.t.start()
            else:
                self.p = subprocess.Popen([WingetTools.winget, "uninstall", "-e", "--name", f"{self.programName}"]+WingetTools.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
                self.t = Tools.KillableThread(target=WingetTools.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
                self.t.start()
        elif("scoop" in self.store):
            self.p = subprocess.Popen(' '.join(["scoop", "uninstall", f"{self.programName}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ)
            self.t = Tools.KillableThread(target=ScoopTools.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

    
    def counter(self, line: int) -> None:
        if(line == 1):
            self.progressbar.setValue(250)
        if(line == 4):
            self.progressbar.setValue(500)
        elif(line == 6):
            self.cancelButton.setEnabled(False)
            self.progressbar.setValue(750)

    def cancel(self):
        print("[        ] Sending cancel signal...")
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.info.setText("Installation canceled by user!")
        if not self.finishedInstallation:
            subprocess.Popen("taskkill /im winget.exe /f", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=os.getcwd(), env=os.environ).wait()
            self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        self.cancelButton.setText("Close")
        self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        Tools.removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
    
    def finish(self, returncode: int, output: str = "") -> None:
        self.finishedInstallation = True
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
                Tools.notify("WingetUI Store", f"{self.programName} was uninstalled successfully!")
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/tick.png"))
                self.cancelButton.clicked.connect(self.close)
                self.info.setText(f"{self.programName} was uninstalled successfully!")
                self.progressbar.setValue(1000)
                if(self.store == "powershell"):
                    msgBox = Tools.MessageBox(self)
                    msgBox.setWindowTitle("WingetUI Store")
                    msgBox.setText(f"{self.programName} was uninstalled successfully.")
                    msgBox.setInformativeText(f"You will need to restart the application in order to get the {self.programName} new packages")
                    msgBox.setStandardButtons(Tools.MessageBox.Ok)
                    msgBox.setDefaultButton(Tools.MessageBox.Ok)
                    msgBox.setIcon(Tools.MessageBox.Information)
                    msgBox.exec_()
            else:
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QtGui.QIcon(realpath+"/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.progressbar.setValue(1000)
                msgBox = Tools.MessageBox(self)
                msgBox.setWindowTitle("WingetUI Store")
                if(returncode == 2):
                    Tools.notify("WingetUI Store", f"The hash of the uninstaller does not coincide with the hash specified in the manifest. {self.programName} uninstallation has been aborted")
                    self.info.setText(f"The hash of the uninstaller does not coincide with the hash specified in the manifest. {self.programName} uninstallation has been aborted")
                    msgBox.setText(f"The hash of the uninstaller does not coincide with the hash specified in the manifest. {self.programName} uninstallation has been aborted")
                else:
                    Tools.notify("WingetUI Store", f"An error occurred while uninstalling {self.programName}")
                    self.info.setText(f"An error occurred during {self.programName} uninstallation!")
                    msgBox.setText(f"An error occurred while uninstalling {self.programName}")
                msgBox.setInformativeText("Click \"Show Details\" to get the output of the uninstaller.")
                msgBox.setDetailedText(output)
                msgBox.setStandardButtons(Tools.MessageBox.Ok)
                msgBox.setDefaultButton(Tools.MessageBox.Ok)
                msgBox.setIcon(Tools.MessageBox.Warning)
                msgBox.exec_()

class Program(QMainWindow):
    onClose = QtCore.Signal()
    loadInfo = QtCore.Signal(dict)
    closeDialog = QtCore.Signal()
    addProgram = QtCore.Signal(PackageInstaller)
    setLoadBarValue = QtCore.Signal(str)
    startAnim = QtCore.Signal(QtCore.QVariantAnimation)
    changeBarOrientation = QtCore.Signal()
    def __init__(self, parent=None):
        super().__init__(parent)
        self.sc = QtWidgets.QScrollArea()
        self.setWindowFlags(Qt.Window)
        self.setWindowModality(Qt.WindowModal)
        self.setWindowFlag(Qt.Tool)
        self.setFocusPolicy(Qt.NoFocus)
        self.setWindowFlag(Qt.FramelessWindowHint)
        self.store = ""
        self.sc.setWidgetResizable(True)
        self.setStyleSheet("""
        QScrollArea{
            border-radius: 5px;
            padding: 5px;
        }
        """)
        self.loadingProgressBar = QtWidgets.QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        
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

        self.layout.addWidget(self.title)
        self.layout.addStretch()

        self.hLayout = QtWidgets.QHBoxLayout()
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
        self.layout.addStretch()

        self.license = QLinkLabel("License: Unknown")
        self.license.setWordWrap(True)

        self.layout.addWidget(self.license)
        self.layout.addStretch()
        
        hLayout = QtWidgets.QHBoxLayout()
        self.versionLabel = QtWidgets.QLabel("Version: ")

        
        class QComboBoxWithFluentMenu(QComboBox):
            def __init__(self, parent=None) -> None:
                super().__init__(parent)
                v = self.view().window()
                Tools.ApplyMenuBlur(v.winId(), v)
                from win32mica import ApplyMica, MICAMODE
                ApplyMica(v.winId(), v)

        self.versionCombo = QComboBoxWithFluentMenu()
        self.versionCombo.setFixedWidth(150)
        self.versionCombo.setIconSize(QtCore.QSize(24, 24))
        self.versionCombo.setFixedHeight(25)
        self.forceCheckbox = QtWidgets.QCheckBox()
        self.forceCheckbox.setText("Force")
        self.forceCheckbox.setChecked(False)
        self.installButton = QtWidgets.QPushButton()
        self.installButton.setText("Install")
        self.installButton.setObjectName("AccentButton")
        self.installButton.setIconSize(QtCore.QSize(24, 24))
        self.installButton.clicked.connect(self.install)
        self.installButton.setFixedWidth(150)
        self.installButton.setFixedHeight(30)

        downloadGroupBox = QtWidgets.QGroupBox()
        downloadGroupBox.setMinimumHeight(80)

        hLayout.addWidget(self.versionLabel)
        hLayout.addWidget(self.versionCombo)
        hLayout.addWidget(QWidget(), stretch=1)
        hLayout.addWidget(self.forceCheckbox)
        hLayout.addWidget(self.installButton)
        downloadGroupBox.setLayout(hLayout)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addStretch()

        self.packageId = QLinkLabel("Program ID: Unknown")
        self.packageId.setWordWrap(True)
        self.layout.addWidget(self.packageId)
        self.manifest = QLinkLabel("Manifest: Unknown")
        self.manifest.setWordWrap(True)
        self.layout.addWidget(self.manifest)
        self.sha = QLinkLabel("Installer SHA256 (Latest version): Unknown")
        self.sha.setWordWrap(True)
        self.layout.addWidget(self.sha)
        self.link = QLinkLabel("Installer URL (Latest version): Unknown")
        self.link.setWordWrap(True)
        self.layout.addWidget(self.link)
        self.type = QLinkLabel("Installer type (Latest version): Unknown")
        self.type.setWordWrap(True)
        self.layout.addWidget(self.type)
        self.storeLabel = QLinkLabel(f"Source: {self.store}")
        self.storeLabel.setWordWrap(True)
        self.layout.addWidget(self.storeLabel)
        self.layout.addStretch()
        self.advert = QLinkLabel("ALERT: NEITHER MICROSOFT NOR THE CREATORS OF WINGET UI STORE ARE RESPONSIBLE FOR THE DOWNLOADED SOFTWARE. PROCEED WITH CAUTION")
        self.advert.setWordWrap(True)
        self.layout.addWidget(self.advert)

        self.mainGroupBox.setLayout(self.layout)
        self.mainGroupBox.setMinimumHeight(480)
        self.vLayout.addWidget(self.mainGroupBox)
        self.hLayout.addLayout(self.vLayout, stretch=0)

        self.centralwidget = QtWidgets.QWidget()
        self.centralwidget.setLayout(self.hLayout)
        if(darkdetect.isDark()):
            print("[        ] Is Dark")
        self.sc.setWidget(self.centralwidget)
        self.setCentralWidget(self.sc)


        self.backButton = QtWidgets.QPushButton(QtGui.QIcon(Tools.getMedia("close")), "", self)
        self.backButton.setStyleSheet("font-size: 22px;")
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(self.width()-40, 0)
        self.backButton.resize(40, 40)
        self.backButton.setFlat(True)
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent}QPushButton:hover{background-color:red;}")
        self.backButton.clicked.connect(lambda: (self.onClose.emit(), self.close()))
        self.backButton.show()

        
        self.loadWheel = LoadingProgress(self)
        self.loadWheel.resize(64, 64)
        self.loadWheel.hide()
        self.hide()

        self.loadInfo.connect(self.printData)

        
        self.leftSlow = QtCore.QVariantAnimation()
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))
        
        self.rightSlow = QtCore.QVariantAnimation()
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))
        
        self.leftFast = QtCore.QVariantAnimation()
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QtCore.QVariantAnimation()
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.valueChanged.connect(lambda v: self.loadingProgressBar.setValue(v))
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        
        self.leftSlow.start()
    
    def resizeEvent(self, event = None):
        self.centralwidget.setFixedWidth(self.width()-18)
        g = self.mainGroupBox.geometry()
        self.loadingProgressBar.move(0, 0)
        self.loadingProgressBar.resize(self.width(), 4)
        self.backButton.move(self.width()-40, 0)
        if(event):
            return super().resizeEvent(event)
    
    def loadProgram(self, title: str, id: str, goodTitle: bool, store: str) -> None:
        self.store = store
        store = store.lower()
        blueColor = Tools.blueColor
        if(goodTitle):
            self.title.setText(title)
        else:
            self.title.setText(id)
            
        self.loadingProgressBar.show()
        self.forceCheckbox.setChecked(False)
        self.forceCheckbox.setEnabled(False)
        self.description.setText("Loading...")
        self.author.setText("Author: "+"Loading...")
        self.publisher.setText("Publisher: "+"Loading...")
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"\">{'Loading...'}</a>")
        self.license.setText(f"License: {'Loading...'} (<a style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>)")
        self.sha.setText(f"Installer SHA256 (Latest version): {'Loading...'}")
        self.link.setText(f"Installer URL (Latest version): <a  style=\"color: {blueColor};\" href=\"\">{'Loading...'}</a>")
        self.type.setText(f"Installer type (Latest version): {'Loading...'}")
        self.packageId.setText(f"Package ID: {'Loading...'}")
        self.manifest.setText(f"Manifest: {'Loading...'}")
        self.storeLabel.setText(f"Source: {self.store.capitalize()}")
        self.versionCombo.addItems(["Loading..."])
        
        if(store=="winget"):
            Thread(target=WingetTools.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()
        elif(store=="scoop"):
            Thread(target=ScoopTools.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()

    def printData(self, appInfo: dict) -> None:
        blueColor = Tools.blueColor
        self.loadingProgressBar.hide()
        if(self.store.lower() == "winget"):
            self.forceCheckbox.setEnabled(True)
        self.title.setText(appInfo["title"])
        self.description.setText(appInfo["description"])
        self.author.setText("Author: "+appInfo["author"])
        self.publisher.setText("Publisher: "+appInfo["publisher"])
        self.homepage.setText(f"Homepage: <a style=\"color: {blueColor};\"  href=\"{appInfo['homepage']}\">{appInfo['homepage']}</a>")
        self.license.setText(f"License: {appInfo['license']} (<a style=\"color: {blueColor};\" href=\"{appInfo['license-url']}\">{appInfo['license-url']}</a>)")
        self.sha.setText(f"Installer SHA256 (Latest version): {appInfo['installer-sha256']}")
        self.link.setText(f"Installer URL (Latest version): <a style=\"color: {blueColor};\" href=\"{appInfo['installer-url']}\">{appInfo['installer-url']}</a>")
        self.type.setText(f"Installer type (Latest version): {appInfo['installer-type']}")
        self.packageId.setText(f"Package ID: {appInfo['id']}")
        self.manifest.setText(f"Manifest: <a style=\"color: {blueColor};\" href=\"file:///"+appInfo['manifest'].replace('\\', '/')+f"\">{appInfo['manifest']}</a>")
        while self.versionCombo.count()>0:
            self.versionCombo.removeItem(0)
        try:
            self.versionCombo.addItems(["Latest"] + appInfo["versions"])
        except KeyError:
            pass

    def install(self):
        title = self.title.text()
        packageId = self.packageId.text().replace('Package ID:', '').strip()
        print(f"[   OK   ] Starting installation of package {title} with id {packageId}")
        cmdline_args = []
        if(self.forceCheckbox.isChecked()):
            cmdline_args.append("--force")
        if(self.versionCombo.currentText()=="Latest"):
            version = []
        else:
            version = ["--version", self.versionCombo.currentText()]
            print(f"[  WARN  ]Issuing specific version {self.versionCombo.currentText()}")
        p = PackageInstaller(title, self.store, version, args=cmdline_args, packageId=packageId)
        self.addProgram.emit(p)
        self.close()

    def show(self) -> None:
        g: QRect = self.parent().window().geometry()
        self.resize(600, 600)
        self.move(g.x()+g.width()//2-600//2, g.y()+g.height()//2-600//2)
        print(g.x()+g.width()//2-600//2, g.y()+g.height()//2-600//2)
        return super().show()

    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.parent().window().activateWindow()
        return super().mousePressEvent(event)
        



if(__name__=="__main__"):
    import __init__