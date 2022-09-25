from xml.dom.minidom import Attr
import wingetHelpers, scoopHelpers, sys, subprocess, time, os
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *

import globals

class DiscoverSoftwareSection(QWidget):

    addProgram = Signal(str, str, str, str)
    finishLoading = Signal(str)
    clearList = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = PackageInfoPopupWindow(self)
        self.setStyleSheet("margin: 0px;")

        self.programbox = QWidget()

        self.layout = QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(140)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnInstall"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnInstall", bool(not v)))
         
        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search something on Winget or Scoop")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)
        
        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(getMedia("store_logo")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QLabel("Discover packages")
        self.discoverLabel.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
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

        self.packageList: QTreeWidget = TreeWidget("a")
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Version", "Origin"])
        self.packageList.setColumnCount(4)
        self.packageList.sortByColumn(0, Qt.AscendingOrder)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item.text(0), item.text(1), item.text(3)) if not getSettings("InstallOnDoubleClick") else self.fastinstall(item.text(0), item.text(1), item.text(3)))

        def showMenu(pos: QPoint):
            if not self.packageList.currentItem():
                return
            if self.packageList.currentItem().isHidden():
                return
            contextMenu = QMenu(self)
            contextMenu.setParent(self)
            contextMenu.setStyleSheet("* {background: red;color: black}")
            ApplyMenuBlur(contextMenu.winId().__int__(), contextMenu)
            inf = QAction("Show info")
            inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3)))
            inf.setIcon(QIcon(getMedia("info")))
            ins1 = QAction("Install")
            ins1.setIcon(QIcon(getMedia("performinstall")))
            ins1.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3)))
            ins2 = QAction("Run as administrator")
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), admin=True))
            ins3 = QAction("Skip hash check")
            ins3.setIcon(QIcon(getMedia("checksum")))
            ins3.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), skiphash=True))
            ins4 = QAction("Interactive installation")
            ins4.setIcon(QIcon(getMedia("interactive")))
            ins4.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), interactive=True))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if self.packageList.currentItem().text(3).lower() == "winget":
                contextMenu.addAction(ins4)
            contextMenu.addAction(ins3)
            contextMenu.addSeparator()
            contextMenu.addAction(inf)
            contextMenu.exec(QCursor.pos())

        self.packageList.setContextMenuPolicy(Qt.CustomContextMenu)
        self.packageList.customContextMenuRequested.connect(showMenu)

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Stretch)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Fixed)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        self.packageList.setColumnWidth(2, 150)
        self.packageList.setColumnWidth(3, 150)
        
        self.loadingProgressBar = QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        layout = QVBoxLayout()
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1300)

        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)

        self.countLabel = QLabel("Searching for packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        v.addWidget(self.countLabel)
        layout.addLayout(hLayout)
        layout.setContentsMargins(5, 0, 0, 5)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)

        self.finishLoading.connect(self.finishLoadingIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QIcon(getMedia("install"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("newversion"))
        self.providerIcon = QIcon(getMedia("provider"))

        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
            
        
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
        
    def finishLoadingIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.filter()
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
            item = TreeWidgetItemWithQAction()
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
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 0)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        print(f"[   OK   ] Searching for string \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def openInfo(self, title: str, id: str, store: str) -> None:
        if("…" in id):
            self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), goodTitle=True, store=store)
        else:
            self.infobox.loadProgram(id.replace("…", ""), id.replace("…", ""), goodTitle=False, store=store)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(),self.infobox, avoidOverrideStyleSheet=True, shadow=False)

    def fastinstall(self, title: str, id: str, store: str, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        if not "scoop" in store.lower():
            if ("…" in id):
                self.addInstallation(PackageInstallerWidget(title, "winget", packageId=id.replace("…", ""), admin=admin, args=list(filter(None, ["--interactive" if interactive else "", "--force" if skiphash else ""]))))
            else:
                self.addInstallation(PackageInstallerWidget(title, "winget", useId=True, packageId=id.replace("…", ""), admin=admin, args=list(filter(None, ["--interactive" if interactive else "", "--force" if skiphash else ""]))))
        else:
            if ("…" in id):
                self.addInstallation(PackageInstallerWidget(title, "scoop", packageId=id.replace("…", ""), admin=admin, args=["--skip" if skiphash else ""]))
            else:
                self.addInstallation(PackageInstallerWidget(title, "scoop", useId=True, packageId=id.replace("…", ""), admin=admin, args=["--skip" if skiphash else ""]))
    
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
        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
    
    def addInstallation(self, p) -> None:
        globals.installersWidget.addItem(p)

class UpdateSoftwareSection(QWidget):

    addProgram = Signal(str, str, str, str, str)
    finishLoading = Signal(str)
    clearList = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    callInMain = Signal(object)

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.callInMain.connect(lambda f: f())
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = PackageInfoPopupWindow(self)
        self.setStyleSheet("margin: 0px;")

        self.programbox = QWidget()
        self.setContentsMargins(0, 0, 0, 0)
        self.programbox.setContentsMargins(0, 0, 0, 0)

        self.layout = QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search available updates")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)
        
        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)

        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(140)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnUpgrade"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnUpgrade", bool(not v)))

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(getMedia("upgrade")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QLabel("Available updates")
        self.discoverLabel.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        v.addWidget(self.discoverLabel)

        hLayout.addLayout(v)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = TreeWidget("ª")
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.setColumnCount(6)
        self.packageList.setHeaderLabels(["", "Package name", "Package ID", "Installed Version", "New Version", "Installation source"])
        self.packageList.setColumnWidth(0, 50)
        self.packageList.setColumnWidth(1, 350)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnWidth(3, 125)
        self.packageList.setColumnWidth(4, 125)
        self.packageList.setColumnWidth(5, 100)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.sortByColumn(0, Qt.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.update(item.text(1), item.text(2), item.text(5), packageItem=item))
        
        def showMenu(pos: QPoint):
            if not self.packageList.currentItem():
                return
            if self.packageList.currentItem().isHidden():
                return
            contextMenu = QMenu(self)
            contextMenu.setParent(self)
            contextMenu.setStyleSheet("* {background: red;color: black}")
            ApplyMenuBlur(contextMenu.winId().__int__(), contextMenu)
            inf = QAction("Show info")
            inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), self.packageList.currentItem()))
            inf.setIcon(QIcon(getMedia("info")))
            ins1 = QAction("Update")
            ins1.setIcon(QIcon(getMedia("menu_updates")))
            ins1.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem()))
            ins2 = QAction("Run as administrator")
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), admin=True))
            ins3 = QAction("Skip hash check")
            ins3.setIcon(QIcon(getMedia("checksum")))
            ins3.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), skiphash=True))
            ins4 = QAction("Interactive update")
            ins4.setIcon(QIcon(getMedia("interactive")))
            ins4.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), interactive=True))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if self.packageList.currentItem().text(5).lower() == "winget":
                contextMenu.addAction(ins4)
            contextMenu.addAction(ins3)
            contextMenu.addSeparator()
            ins5 = QAction("Ignore updates for this package")
            ins5.setIcon(QIcon(getMedia("blacklist")))
            ins5.triggered.connect(lambda: (setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates")+self.packageList.currentItem().text(2)+","), self.packageList.currentItem().setHidden(True)))
            contextMenu.addAction(ins5)
            contextMenu.addSeparator()
            contextMenu.addAction(inf)
            contextMenu.exec(QCursor.pos())

        self.packageList.setContextMenuPolicy(Qt.CustomContextMenu)
        self.packageList.customContextMenuRequested.connect(showMenu)

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Fixed)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        header.setSectionResizeMode(4, QHeaderView.Fixed)
        header.setSectionResizeMode(5, QHeaderView.Fixed)
        self.packageList.setColumnWidth(0, 46)
        self.packageList.setColumnWidth(3, 100)
        self.packageList.setColumnWidth(4, 100)
        self.packageList.setColumnWidth(5, 120)
        
        self.loadingProgressBar = QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        layout = QVBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1300)

        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)

        def blacklistSelectedPackages():
            for i in range(self.packageList.topLevelItemCount()):
                program: QTreeWidgetItem = self.packageList.topLevelItem(i)
                if not program.isHidden():
                    try:
                        if self.packageList.itemWidget(program, 0).isChecked():
                            setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates")+program.text(2)+",")
                            program.setHidden(True)
                    except AttributeError:
                        pass

        h2Layout = QHBoxLayout()
        h2Layout.setContentsMargins(27, 0, 27, 0)
        self.upgradeAllButton = QPushButton("Upgrade all packages")
        self.upgradeAllButton.setFixedWidth(200)
        self.upgradeAllButton.clicked.connect(lambda: self.update("", "", all=True))
        self.blacklistButton = QPushButton("Blacklist selected packages")
        self.blacklistButton.setFixedWidth(200)
        self.blacklistButton.clicked.connect(lambda: blacklistSelectedPackages())
        self.upgradeSelected = QPushButton("Upgrade selected packages")
        self.upgradeSelected.clicked.connect(lambda: self.update("", "", selected=True))
        self.upgradeSelected.setFixedWidth(200)
        self.showUnknownSection = QCheckBox("Show unknown versions")
        self.showUnknownSection.setFixedHeight(30)
        self.showUnknownSection.setLayoutDirection(Qt.RightToLeft)
        self.showUnknownSection.setFixedWidth(190)
        self.showUnknownSection.setStyleSheet("margin-top: 10px;")
        self.showUnknownSection.setChecked(getSettings("ShowUnknownResults"))
        self.showUnknownSection.clicked.connect(lambda v: (setSettings("ShowUnknownResults", bool(v)), updatelist()))
        def updatelist(selff = None):
            if not selff:
                nonlocal self
            else:
                self = selff
            for item in [self.packageList.topLevelItem(i) for i in range(self.packageList.topLevelItemCount())]:
                if item.text(3) == "Unknown":
                    item.setHidden(not self.showUnknownSection.isChecked())
        self.updatelist = updatelist

        h2Layout.addWidget(self.upgradeAllButton)
        h2Layout.addWidget(self.upgradeSelected)
        h2Layout.addWidget(self.blacklistButton)
        h2Layout.addStretch()
        h2Layout.addWidget(self.showUnknownSection)

        self.countLabel = QLabel("Checking for updates...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.addLayout(h2Layout)
        layout.setContentsMargins(5, 0, 0, 5)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)

        self.finishLoading.connect(self.finishLoadingIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QIcon(getMedia("install"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.newVersionIcon = QIcon(getMedia("newversion"))
        self.providerIcon = QIcon(getMedia("provider"))

        self.blacklist = getSettingsValue("BlacklistedUpdates")
        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForUpdates, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForUpdates, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
        print("[   OK   ] Upgrades tab loaded")

        g = self.packageList.geometry()
                    
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

    def finishLoadingIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            globals.trayMenuUpdatesList.menuAction().setText(f"{self.packageList.topLevelItemCount()} updates found")
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            globals.trayMenuUpdatesList.menuAction().setText(f"{self.packageList.topLevelItemCount()} updates found")
            self.packageList.label.setText(self.countLabel.text())
            self.scoopLoaded = True
            self.filter()
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.loadingProgressBar.hide()
            count = 0
            lastVisibleItem = None
            for i in range(self.packageList.topLevelItemCount()):
                if not self.packageList.topLevelItem(i).isHidden():
                    count += 1
                    lastVisibleItem = self.packageList.topLevelItem(i)
            self.countLabel.setText("Found packages: "+str(count))
            self.packageList.label.setText(str(count))
            if not getSettings("DisableUpdatesNotifications"):
                if count > 1:
                    notify("Updates found!", f"{count} apps can be updated")
                elif count == 1:
                    notify("Updates found!", f"{lastVisibleItem.text(1)} can be updated")
            if count > 0:
                globals.trayIcon.setIcon(QIcon(getMedia("greenicon")))
            else:
                globals.trayIcon.setIcon(QIcon(getMedia("greyicon")))
            globals.trayMenuUpdatesList.menuAction().setText(f"{count} updates found")
            self.countLabel.setText("Found packages: "+str(count))
            self.packageList.label.setText(self.countLabel.text())
            self.filter()
            self.updatelist()
            if not getSettings("DisableAutoCheckforUpdates"):
                Thread(target=lambda: (time.sleep(3600), self.reloadSources()), daemon=True, name="AutoCheckForUpdates Thread").start()
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, newVersion: str, store) -> None:
        if not "---" in name:
            if not id in self.blacklist:
                item = TreeWidgetItemWithQAction()
                item.setText(1, name)
                item.setIcon(1, self.installIcon)
                item.setText(2, id)
                item.setIcon(2, self.IDIcon)
                item.setText(3, version)
                item.setIcon(3, self.versionIcon)
                item.setText(4, newVersion)
                item.setIcon(4, self.newVersionIcon)
                item.setText(5, store)
                item.setIcon(5, self.providerIcon)
                self.packageList.addTopLevelItem(item)
                c = QCheckBox()
                c.setChecked(True)
                c.setStyleSheet("margin-top: 1px; margin-left: 8px;")
                self.packageList.setItemWidget(item, 0, c)
                action = QAction(name+"  \t"+version+"\t → \t"+newVersion, globals.trayMenuUpdatesList)
                action.triggered.connect(lambda : self.update(name, id, packageItem=item))
                action.setShortcut(version)
                item.setAction(action)
                globals.trayMenuUpdatesList.addAction(action)
            else:
                print(id,"was blackisted")
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 2)
        print(f"[   OK   ] Searching for stringg \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 1):
            if not(item in resultsFound):
                item.setHidden(True)
                item.treeWidget().itemWidget(item, 0).hide()
            else:
                item.setHidden(False)
                if item.text(3) == "Unknown":
                    item.setHidden(not self.showUnknownSection.isChecked())
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def update(self, title: str, id: str, store: str, all: bool = False, selected: bool = False, packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        if all:
            for i in range(self.packageList.topLevelItemCount()):
                program: QTreeWidgetItem = self.packageList.topLevelItem(i)
                if not program.isHidden():
                    self.update(program.text(1), program.text(2), packageItem=program)
        elif selected:
            for i in range(self.packageList.topLevelItemCount()):
                program: QTreeWidgetItem = self.packageList.topLevelItem(i)
                if not program.isHidden():
                    try:
                        if self.packageList.itemWidget(program, 0).isChecked():
                           self.update(program.text(1), program.text(2), packageItem=program)
                    except AttributeError:
                        pass
        else:
            if not "scoop" in store.lower():
                if ("…" in id):
                    self.addInstallation(PackageUpdaterWidget(title, "winget", packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=list(filter(None, ["--interactive" if interactive else "", "--force" if skiphash else ""]))))
                else:
                    self.addInstallation(PackageUpdaterWidget(title, "winget", useId=True, packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=list(filter(None, ["--interactive" if interactive else "", "--force" if skiphash else ""]))))
            else:
                if ("…" in id):
                    self.addInstallation(PackageUpdaterWidget(title, "scoop", packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=["--skip" if skiphash else ""]))
                else:
                    self.addInstallation(PackageUpdaterWidget(title, "scoop", useId=True, packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=["--skip" if skiphash else ""]))
     

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None) -> None:
        if("…" in id):
            self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), goodTitle=True, store=store, update=True, packageItem=packageItem)
        else:
            self.infobox.loadProgram(id.replace("…", ""), id.replace("…", ""), goodTitle=False, store=store, update=True, packageItem=packageItem)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(),self.infobox, avoidOverrideStyleSheet=True, shadow=False)

    def reloadSources(self):
        print("Reloading sources...")
        try:
            o1 = subprocess.run(f"powershell -Command scoop update", shell=True, stdout=subprocess.PIPE)
            print("Updated scoop packages with result", o1.returncode)
            o2 = subprocess.run(f"{wingetHelpers.winget} source update --name winget", shell=True, stdout=subprocess.PIPE)
            print("Updated Winget packages with result", o2.returncode)
            print(o1.stdout)
            print(o2.stdout)
        except Exception as e:
            report(e)
        self.callInMain.emit(self.reload)
    
    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        for action in globals.trayMenuUpdatesList.actions():
            globals.trayMenuUpdatesList.removeAction(action)
        globals.trayMenuUpdatesList.addAction(globals.updatesHeader)
        self.countLabel.setText("Checking for updates...")
        self.packageList.label.setText(self.countLabel.text())
        self.blacklist = getSettingsValue("BlacklistedUpdates")
        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForUpdates, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForUpdates, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
    
    def addInstallation(self, p) -> None:
        globals.installersWidget.addItem(p)

class UninstallSoftwareSection(QWidget):

    addProgram = Signal(str, str, str, str)
    finishLoading = Signal(str)
    clearList = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = PackageInfoPopupWindow(self)
        self.setStyleSheet("margin: 0px;")
        self.infobox.onClose.connect(self.showQuery)

        self.programbox = QWidget()

        self.layout = QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 40)
        self.reloadButton.setStyleSheet("margin-top: 10px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 40)
        self.searchButton.setStyleSheet("margin-top: 10px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search on your software")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(40)
        self.query.setStyleSheet("margin-top: 10px;")
        self.query.setFixedWidth(250)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)
        
        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)


        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(140)
        self.forceCheckBox.setStyleSheet("margin-top: 10px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnUninstall"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnUninstall", bool(not v)))

        img = QLabel()
        img.setFixedWidth(96)
        img.setPixmap(QIcon(getMedia("red_trash")).pixmap(QSize(80, 80)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        self.discoverLabel = QLabel("Installed packages")
        self.discoverLabel.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        v.addWidget(self.discoverLabel)

        hLayout.addLayout(v)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        
        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = TreeWidget("Found 0 Packages")
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.setColumnCount(4)
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Installed Version", "Installation source"])
        #self.packageList.setColumnWidth(0, 300)
        #self.packageList.setColumnWidth(1, 300)
        #self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnHidden(2, False)
        self.packageList.setColumnWidth(3, 120)
        self.packageList.setSortingEnabled(True)
        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Stretch)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        self.packageList.sortByColumn(0, Qt.AscendingOrder)

        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.uninstall(item.text(0), item.text(1), item.text(3), packageItem=item))
        
        def showMenu(pos: QPoint):
            if not self.packageList.currentItem():
                return
            if self.packageList.currentItem().isHidden():
                return
            contextMenu = QMenu(self)
            contextMenu.setParent(self)
            contextMenu.setStyleSheet("* {background: red;color: black}")
            ApplyMenuBlur(contextMenu.winId().__int__(), contextMenu)
            ins1 = QAction("Uninstall")
            ins1.setIcon(QIcon(getMedia("menu_uninstall")))
            ins1.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem()))
            ins2 = QAction("Run as administrator")
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem(), admin=True))
            ins3 = QAction("Remove permanent data")
            ins3.setIcon(QIcon(getMedia("menu_close")))
            ins3.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem(), removeData=True))
            ins4 = QAction("Show package info")
            ins4.setIcon(QIcon(getMedia("info")))
            ins4.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), "scoop"))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if self.packageList.currentItem().text(3).lower() != "winget":
                contextMenu.addAction(ins3)
                contextMenu.addSeparator()
                contextMenu.addAction(ins4)

            contextMenu.exec(QCursor.pos())

        self.packageList.setContextMenuPolicy(Qt.CustomContextMenu)
        self.packageList.customContextMenuRequested.connect(showMenu)
        
        self.loadingProgressBar = QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)

        
        layout = QVBoxLayout()
        w = QWidget()
        w.setLayout(layout)
        w.setMaximumWidth(1300)



        self.bodyWidget = QWidget()
        l = QHBoxLayout()
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(w)
        l.setContentsMargins(0, 0, 0, 0)
        l.addWidget(ScrollWidget(self.packageList), stretch=0)
        l.addWidget(self.packageListScrollBar)
        self.bodyWidget.setLayout(l)


        self.countLabel = QLabel("Searching for installed packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.setContentsMargins(5, 0, 0, 5)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.packageList)
        self.programbox.setLayout(l)
        self.layout.addWidget(self.programbox, stretch=1)
        self.layout.addWidget(self.infobox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)

        self.finishLoading.connect(self.finishLoadingIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))
        

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        
        self.installIcon = QIcon(getMedia("install"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.providerIcon = QIcon(getMedia("provider"))
        
    
        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForInstalledPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForInstalledPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
        print("[   OK   ] Discover tab loaded")

        g = self.packageList.geometry()
            
        
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

    def openInfo(self, title: str, id: str, store: str) -> None:
        if("…" in id):
            self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), goodTitle=True, store=store)
        else:
            self.infobox.loadProgram(id.replace("…", ""), id.replace("…", ""), goodTitle=False, store=store)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(),self.infobox, avoidOverrideStyleSheet=True, shadow=False)


    def finishLoadingIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            globals.trayMenuInstalledList.setTitle(f"{self.packageList.topLevelItemCount()} packages found")
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.filter()
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            globals.trayMenuInstalledList.setTitle(f"{self.packageList.topLevelItemCount()} packages found")
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.filter()
            self.loadingProgressBar.hide()
            globals.trayMenuInstalledList.setTitle(f"{self.packageList.topLevelItemCount()} packages found")
            self.countLabel.setText("Found packages: "+str(self.packageList.topLevelItemCount()))
            self.packageList.label.setText(self.countLabel.text())
            print("[   OK   ] Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event = None):
        g = self.packageList.geometry()
        if(event):
            return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        if not "---" in name:
            item = TreeWidgetItemWithQAction()
            item.setText(0, name)
            item.setText(1, id)
            item.setIcon(0, self.installIcon)
            item.setIcon(1, self.IDIcon)
            item.setIcon(2, self.versionIcon)
            item.setText(2, version)
            item.setIcon(3, self.providerIcon)
            item.setText(3, store)
            self.packageList.addTopLevelItem(item)
            action = QAction(name+" \t"+version, globals.trayMenuInstalledList)
            action.triggered.connect(lambda: (self.uninstall(name, id, store, packageItem=item), print(name, id, store, item)))
            action.setShortcut(version)
            item.setAction(action)
            globals.trayMenuInstalledList.addAction(action)
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 0)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        print(f"[   OK   ] Searching for string \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def uninstall(self, title: str, id: str, store: str, packageItem: QTreeWidgetItem = None, admin: bool = False, removeData: bool = False, interactive: bool = False) -> None:
        if(MessageBox.question(self, "Are you sure?", f"Do you really want to uninstall {title}", MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes):
            print(id)
            if("…" in id):
                self.addInstallation(PackageUninstallerWidget(title, store, useId=False, packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, removeData=removeData))
            else:
                self.addInstallation(PackageUninstallerWidget(title, store, useId=True, packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, removeData=removeData))
    
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
        if not getSettings("DisableWinget"):
            Thread(target=wingetHelpers.searchForInstalledPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.wingetLoaded = True
        if not getSettings("DisableScoop"):
            Thread(target=scoopHelpers.searchForInstalledPackage, args=(self.addProgram, self.finishLoading), daemon=True).start()
        else:
            self.scoopLoaded = True
        for action in globals.trayMenuInstalledList.actions():
            globals.trayMenuInstalledList.removeAction(action)
        globals.trayMenuInstalledList.addAction(globals.installedHeader)
    
    def addInstallation(self, p) -> None:
        globals.installersWidget.addItem(p)


class AboutSection(QScrollArea):
    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.setFrameShape(QFrame.NoFrame)
        self.widget = QWidget()
        self.setWidgetResizable(True)
        self.setStyleSheet("margin-left: 0px;")
        self.layout = QVBoxLayout()
        w = QWidget()
        w.setLayout(self.layout)
        w.setMaximumWidth(1300)
        l = QHBoxLayout()
        l.addSpacing(20)
        l.addStretch()
        l.addWidget(w, stretch=0)
        l.addStretch()
        self.widget.setLayout(l)
        self.setWidget(self.widget)
        self.announcements = QAnnouncements()
        self.layout.addWidget(self.announcements)
        title = QLabel("General Settings")
        title.setStyleSheet("font-size: 40px;")
        self.layout.addWidget(title)

        self.layout.addWidget(QLabel())
        updateCheckBox = QCheckBox("Update WingetUI automatically")
        updateCheckBox.setChecked(not getSettings("DisableAutoUpdateWingetUI"))
        updateCheckBox.clicked.connect(lambda v: setSettings("DisableAutoUpdateWingetUI", not bool(v)))
        self.layout.addWidget(updateCheckBox)
        doCloseWingetUI = QCheckBox("Keep WingetUI always running on the system tray")
        doCloseWingetUI.setChecked(not getSettings("DisablesystemTray"))
        doCloseWingetUI.clicked.connect(lambda v: setSettings("DisablesystemTray", not bool(v)))
        self.layout.addWidget(doCloseWingetUI)
        notifyAboutUpdates = QCheckBox("Show a notification when there are available updates")
        notifyAboutUpdates.setChecked(not getSettings("DisableUpdatesNotifications"))
        notifyAboutUpdates.clicked.connect(lambda v: setSettings("DisableUpdatesNotifications", not bool(v)))
        self.layout.addWidget(notifyAboutUpdates)
        checkForUpdates = QCheckBox("Check for updates periodically")
        checkForUpdates.setChecked(not getSettings("DisableAutoCheckforUpdates"))
        checkForUpdates.clicked.connect(lambda v: setSettings("DisableAutoCheckforUpdates", not bool(v)))
        self.layout.addWidget(checkForUpdates)
        self.layout.addWidget(QLabel())
        parallelInstalls = QCheckBox("Allow parallel installs (NOT RECOMMENDED)")
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.clicked.connect(lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.layout.addWidget(parallelInstalls)
        changeDefaultInstallAction = QCheckBox("Double-clicking should install instead of showing further info")
        changeDefaultInstallAction.setChecked(getSettings("InstallOnDoubleClick"))
        changeDefaultInstallAction.clicked.connect(lambda v: setSettings("InstallOnDoubleClick", bool(v)))
        self.layout.addWidget(changeDefaultInstallAction)
        scoopPreventCaps = QCheckBox("Show scoop apps as lowercase")
        scoopPreventCaps.setChecked(getSettings("LowercaseScoopApps"))
        scoopPreventCaps.clicked.connect(lambda v: setSettings("LowercaseScoopApps", bool(v)))
        self.layout.addWidget(scoopPreventCaps)
        dontUseBuiltInGsudo = QCheckBox("Use installed GSudo instead of the bundled one (requires app restart)")
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.clicked.connect(lambda v: setSettings("UseUserGSudo", bool(v)))
        self.layout.addWidget(dontUseBuiltInGsudo)
        self.layout.addWidget(QLabel())
        disableWinget = QCheckBox("Disable Winget")
        disableWinget.setChecked(getSettings("DisableWinget"))
        disableWinget.clicked.connect(lambda v: setSettings("DisableWinget", bool(v)))
        self.layout.addWidget(disableWinget)
        disableScoop = QCheckBox("Disable Scoop")
        disableScoop.setChecked(getSettings("DisableScoop"))
        disableScoop.clicked.connect(lambda v: setSettings("DisableScoop", bool(v)))
        self.layout.addWidget(disableScoop)
        disableUpdateIndexes = QCheckBox("Do not update package indexes on launch")
        disableUpdateIndexes.setChecked(getSettings("DisableUpdateIndexes"))
        disableUpdateIndexes.clicked.connect(lambda v: setSettings("DisableUpdateIndexes", bool(v)))
        self.layout.addWidget(disableUpdateIndexes)
        enableScoopCleanup = QCheckBox("Enable scoop cleanup on launch")
        enableScoopCleanup.setChecked(getSettings("EnableScoopCleanup"))
        enableScoopCleanup.clicked.connect(lambda v: setSettings("EnableScoopCleanup", bool(v)))
        self.layout.addWidget(enableScoopCleanup)
        
        self.layout.addWidget(QLabel())
        l = QHBoxLayout()
        button = QPushButton("Add a bucket to scoop")
        #button.setFixedWidth(350)
        button.setFixedHeight(30)
        button.clicked.connect(lambda: self.scoopAddExtraBucket())
        l.addWidget(button)
        button = QPushButton("Remove a bucket from scoop")
        #button.setFixedWidth(350)
        button.setFixedHeight(30)
        button.clicked.connect(lambda: self.scoopRemoveExtraBucket())
        l.addWidget(button)
        button = QPushButton("Reset ignored package updates")
        #button.setFixedWidth(350)
        button.setFixedHeight(30)
        button.clicked.connect(lambda: setSettingsValue("BlacklistedUpdates", ""))
        l.addWidget(button)
        l.setContentsMargins(0, 0, 0, 0)
        self.layout.addLayout(l)
        title = QLabel("Component information")
        title.setStyleSheet("font-size: 40px;")
        self.layout.addWidget(title)

        self.layout.addWidget(QLabel())
        
        table = QTableWidget()
        table.setAutoFillBackground(True)
        table.setStyleSheet("*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
        table.setColumnCount(2)
        table.setRowCount(3)
        table.setEnabled(False)
        table.setShowGrid(False)
        table.setHorizontalHeaderLabels(["Status", "Version"])
        table.setColumnWidth(1, 500)
        table.verticalHeader().setFixedWidth(100)
        table.setVerticalHeaderLabels(["Winget", "  Scoop", " GSudo"])
        table.setItem(0, 0, QTableWidgetItem(str("Found" if globals.componentStatus["wingetFound"] else "Not found")))
        table.setItem(0, 1, QTableWidgetItem(str(globals.componentStatus["wingetVersion"])))
        table.setItem(1, 0, QTableWidgetItem(str("Found" if globals.componentStatus["scoopFound"] else "Not found")))
        table.setItem(1, 1, QTableWidgetItem(str(globals.componentStatus["scoopVersion"])))
        table.setItem(2, 0, QTableWidgetItem(str("Found" if globals.componentStatus["sudoFound"] else "Not found")))
        table.setItem(2, 1, QTableWidgetItem(str(globals.componentStatus["sudoVersion"])))
        table.setCornerWidget(QLabel("Components"))
        table.setCornerButtonEnabled(True)
        table.cornerWidget().setStyleSheet("background: transparent;")
        self.layout.addWidget(table)
        title = QLabel("About WingetUI "+str(versionName)+"")
        title.setStyleSheet("font-size: 40px;")

        self.layout.addWidget(title)
        self.layout.addWidget(QLabel())

        description = QLabel("The main goal of this project is to give a GUI Store to the most common CLI Package Managers for windows, such as Winget and Scoop.\nThis project has no connection with the winget-cli official project, and it's totally unofficial.")
        self.layout.addWidget(description)
        self.layout.addSpacing(5)
        self.layout.addWidget(QLinkLabel(f"Project homepage:   <a style=\"color: {blueColor};\" href=\"https://github.com/martinet101/WinGetUI\">https://github.com/martinet101/WinGetUI</a>"))
        self.layout.addSpacing(30)
        self.layout.addWidget(QLinkLabel("Licenses:", "font-size: 27pt;"))
        self.layout.addWidget(QLabel())
        self.layout.addWidget(QLinkLabel(f"WingetUI:&nbsp;&nbsp;&nbsp;&nbsp;LGPL v2.1:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/martinet101/WinGetUI/blob/main/LICENSE\">https://github.com/martinet101/WinGetUI/blob/main/LICENSE</a>"))
        self.layout.addWidget(QLabel())
        self.layout.addWidget(QLinkLabel(f"PySide6:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;LGPLv3:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">https://www.gnu.org/licenses/lgpl-3.0.html</a>"))
        self.layout.addWidget(QLinkLabel(f"Python3:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;PSF License:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://docs.python.org/3/license.html#psf-license\">https://docs.python.org/3/license.html#psf-license</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"Winget:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;MIT License:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli/blob/master/LICENSE\">https://github.com/microsoft/winget-cli/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"Scoop:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;Unlicense:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/lukesampson/scoop/blob/master/LICENSE\">https://github.com/lukesampson/scoop/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"GSudo:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;MIT License:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/gerardog/gsudo/blob/master/LICENSE.txt\">https://github.com/gerardog/gsudo/blob/master/LICENSE.txt</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"Icons:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;By Icons8:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://icons8.com\">https://icons8.com</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel())
        button = QPushButton("About Qt")
        button.setFixedWidth(710)
        button.setFixedHeight(25)
        button.clicked.connect(lambda: MessageBox.aboutQt(self, "WingetUI: About Qt"))
        self.layout.addWidget(button)
        self.layout.addWidget(QLinkLabel())
        button = QPushButton("Update/Reinstall WingetUI")
        button.clicked.connect(lambda: self.layout.addWidget(PackageInstallerWidget("WingetUI", "winget")))
        # self.layout.addWidget(button)
        self.layout.addWidget(QWidget(), stretch=1)
    
        print("[   OK   ] About tab loaded!")
        
    def scoopAddExtraBucket(self) -> None:
        r = QInputDialog.getItem(self, "Scoop bucket manager", "What bucket do you want to add", ["main", "extras", "versions", "nirsoft", "php", "nerd-fonts", "nonportable", "java", "games"], 1, editable=False)
        if r[1]:
            print(r[0])
            globals.installersWidget.addItem(PackageInstallerWidget(f"{r[0]} scoop bucket", "custom", customCommand=f"scoop bucket add {r[0]}"))
    
    def scoopRemoveExtraBucket(self) -> None:
        r = QInputDialog.getItem(self, "Scoop bucket manager", "What bucket do you want to remove", ["main", "extras", "versions", "nirsoft", "php", "nerd-fonts", "nonportable", "java", "games"], 1, editable=False)
        if r[1]:
            print(r[0])
            globals.installersWidget.addItem(PackageInstallerWidget(f"{r[0]} scoop bucket", "custom", customCommand=f"scoop bucket rm {r[0]}"))

    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements, daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)
    
class QInfoProgressDialog(QProgressDialog):
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.setFixedWidth(300)
        
    def addTextLine(self, text: str) -> None:
        self.setLabelText("Downloading and installing, please wait...\n\n"+text)

class PackageInstallerWidget(QGroupBox):
    onCancel = Signal()
    killSubprocess = Signal()
    addInfoLine = Signal(str)
    finishInstallation = Signal(int, str)
    counterSignal = Signal(int)
    callInMain = Signal(object)
    changeBarOrientation = Signal()
    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId="", admin: bool = False, useId: bool = False, packageItem: QTreeWidgetItem = None):
        super().__init__(parent=parent)
        self.actionDone = "installed"
        self.actionDoing = "installing"
        self.actionName = "installation"
        self.actionVerb = "install"
        self.runAsAdmin = admin
        self.useId = useId
        self.adminstr = [sudoPath] if self.runAsAdmin else []
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
        self.layout = QHBoxLayout()
        self.layout.setContentsMargins(30, 10, 10, 10)
        self.label = QLabel(title+" Installation")
        self.layout.addWidget(self.label)
        self.layout.addSpacing(5)
        self.progressbar = QProgressBar()
        self.progressbar.setTextVisible(False)
        self.progressbar.setRange(0, 1000)
        self.progressbar.setValue(0)
        self.progressbar.setFixedHeight(4)
        self.changeBarOrientation.connect(lambda: self.progressbar.setInvertedAppearance(not(self.progressbar.invertedAppearance())))
        self.layout.addWidget(self.progressbar, stretch=1)
        self.info = QLineEdit()
        self.info.setStyleSheet("color: grey; border-bottom: inherit;")
        self.info.setText("Waiting for other installations to finish...")
        self.info.setReadOnly(True)
        self.addInfoLine.connect(lambda text: self.info.setText(text))
        self.finishInstallation.connect(self.finish)
        self.layout.addWidget(self.info)
        self.counterSignal.connect(self.counter)
        self.cancelButton = QPushButton(QIcon(realpath+"/resources/cancel.png"), "Cancel")
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
        print(f"[   OK   ] Waiting for install permission... title={self.programName}, id={self.packageId}, installId={self.installId}")
        
    
    
    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("[   OK   ] Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.finishedInstallation = False
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.addInfoLine.emit("Starting installation...")
        self.progressbar.setValue(0)
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
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "install", f"{self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=scoopHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
        self.cancelButton.setIcon(QIcon(realpath+"/resources/warn.png"))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
    
    def finish(self, returncode: int, output: str = "") -> None:
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
        if not(self.canceled):
            if(returncode == 0):
                self.callInMain.emit(lambda: globals.trayIcon.showMessage(f"{self.actionName.capitalize()} succeeded", f"{self.programName} was {self.actionDone} successfully!", QIcon(getMedia("notif_info"))))
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QIcon(realpath+"/resources/tick.png"))
                self.cancelButton.clicked.connect(self.close)
                self.info.setText(f"{self.programName} was {self.actionDone} successfully!")
                self.progressbar.setValue(1000)
                self.startCoolDown()
                if(self.store == "powershell"):
                    msgBox = MessageBox(self)
                    msgBox.setWindowTitle("WingetUI")
                    msgBox.setText(f"{self.programName} was {self.actionDone} successfully.")
                    msgBox.setInformativeText(f"You will need to restart the application in order to get the {self.programName} new packages")
                    msgBox.setStandardButtons(MessageBox.Ok)
                    msgBox.setDefaultButton(MessageBox.Ok)
                    msgBox.setIcon(MessageBox.Information)
                    msgBox.exec_()
            else:
                globals.trayIcon.setIcon(QIcon(getMedia("yellowicon"))) 
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QIcon(realpath+"/resources/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.progressbar.setValue(1000)
                self.err = ErrorMessage(self.window())
                if(returncode == 2):  # if the installer's hash does not coincide
                    errorData = {
                        "titlebarTitle": f"WingetUI - {self.programName} {self.actionName}",
                        "mainTitle": f"{self.actionName.capitalize()} aborted",
                        "mainText": f"The checksum of the installer does not coincide with the expected value, and the authenticity of the installer can't be verified. If you trust the publisher, {self.actionVerb} the package again skipping the hash check.",
                        "buttonTitle": "Close",
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("warn")),
                        "notifTitle": f"Can't {self.actionVerb} {self.programName}",
                        "notifText": f"The installer has an invalid checksum",
                        "notifIcon": QIcon(getMedia("notif_warn")),
                    }
                else: # if there's a generic error
                    errorData = {
                        "titlebarTitle": f"WingetUI - {self.programName} {self.actionName}",
                        "mainTitle": f"{self.actionName.capitalize()} failed",
                        "mainText": f"We could not {self.actionVerb} {self.programName}. Please try again later. Click on \"Show details\" to get the logs from the installer.",
                        "buttonTitle": "Close",
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("warn")),
                        "notifTitle": f"Can't {self.actionVerb} {self.programName}",
                        "notifText": f"{self.programName} {self.actionName} failed",
                        "notifIcon": QIcon(getMedia("notif_warn")),
                    }
                self.err.showErrorMessage(errorData)

    def startCoolDown(self):
        op1=QGraphicsOpacityEffect(self)
        op2=QGraphicsOpacityEffect(self)
        op3=QGraphicsOpacityEffect(self)
        op4=QGraphicsOpacityEffect(self)
        ops = [op1, op2, op3, op4]
        def updateOp(v: float):
            i = 0
            for widget in [self.cancelButton, self.label, self.progressbar, self.info]:
                ops[i].setOpacity(v)
                widget: QWidget
                widget.setGraphicsEffect(ops[i])
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
        super().destroy()

class PackageUpdaterWidget(PackageInstallerWidget):

    def __init__(self, title: str, store: str, version: list = [], parent=None, customCommand: str = "", args: list = [], packageId="", packageItem: QTreeWidgetItem = None, admin: bool = False, useId: bool = False):
        super().__init__(title, store, version, parent, customCommand, args, packageId, admin, useId)
        self.packageItem = packageItem
        self.actionDone = "updated"
        self.actionDoing = "updating"
        self.actionName = "update"
        self.actionVerb = "update"
    
    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("[   OK   ] Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.finishedInstallation = False
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.addInfoLine.emit("Applying update...")
        self.rightFast.stop()
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        if(self.store.lower() == "winget"):
            print(self.adminstr)
            if self.useId:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "install", "-e", "--id", f"{self.packageId}"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            else:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "install", "-e", "--name", f"{self.programName}"] + self.version + wingetHelpers.common_params + self.cmdline_args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            print(self.p.args)
            self.t = KillableThread(target=wingetHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        elif("scoop" in self.store.lower()):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "update", f"{self.programName}"] + self.cmdline_args), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=scoopHelpers.installAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()
        else:
            self.p = subprocess.Popen(self.customCommand, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=genericInstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
            self.t.start()

    def finish(self, returncode: int, output: str = "") -> None:
        print(returncode)
        if returncode == 0 and not self.canceled:
            print(self.packageItem)
            if self.packageItem:
                try:
                    i = self.packageItem.treeWidget().takeTopLevelItem(self.packageItem.treeWidget().indexOfTopLevelItem(self.packageItem))
                    del i
                except Exception as e:
                    report(e)
        return super().finish(returncode, output)
    
    def close(self):
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
    def __init__(self, title: str, store: str, useId=False, packageId = "", packageItem: QTreeWidgetItem = None, admin: bool = False, removeData: bool = False):
        self.packageItem = packageItem
        self.useId = useId
        self.programName = title
        self.packageId = packageId
        super().__init__(parent=None, title=title, store=store, packageId=packageId, admin=admin)
        self.actionDone = "uninstalled"
        self.removeData = removeData
        self.actionDoing = "uninstalling"
        self.actionName = "uninstallation"
        self.actionVerb = "uninstall"
        self.finishedInstallation = True
        self.runAsAdmin = admin
        self.adminstr = [sudoPath] if self.runAsAdmin else []
        self.store = store.lower()
        self.setStyleSheet("QGroupBox{padding-top:15px; margin-top:-15px; border: none}")
        self.setFixedHeight(50)
        self.label.setText(title+" Uninstallation")
        
    def startInstallation(self) -> None:
        while self.installId != globals.current_program and not getSettings("AllowParallelInstalls"):
            time.sleep(0.2)
        print("[   OK   ] Have permission to install, starting installation threads...")
        self.callInMain.emit(self.runInstallation)

    def runInstallation(self) -> None:
        self.leftSlow.stop()
        self.leftFast.stop()
        self.rightSlow.stop()
        self.rightFast.stop()
        self.progressbar.setValue(0)
        if self.progressbar.invertedAppearance(): self.progressbar.setInvertedAppearance(False)
        self.finishedInstallation = False
        if(self.store == "winget"):
            if self.useId:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "uninstall", "-e", "--id", f"{self.packageId}"]+wingetHelpers.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
                self.t = KillableThread(target=wingetHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
                self.t.start()
                print(self.p.args)
            else:
                self.p = subprocess.Popen(self.adminstr + [wingetHelpers.winget, "uninstall", "-e", "--name", f"{self.programName}"]+wingetHelpers.common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
                self.t = KillableThread(target=wingetHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
                self.t.start()
                print(self.p.args)
        elif("scoop" in self.store):
            self.p = subprocess.Popen(' '.join(self.adminstr + ["powershell", "-Command", "scoop", "uninstall", f"{self.programName}"] + [""] if self.removeData else ["-p"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=sudoLocation, env=os.environ)
            self.t = KillableThread(target=scoopHelpers.uninstallAssistant, args=(self.p, self.finishInstallation, self.addInfoLine, self.counterSignal))
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
        self.cancelButton.setIcon(QIcon(realpath+"/resources/warn.png"))
        self.cancelButton.clicked.connect(self.close)
        self.onCancel.emit()
        self.progressbar.setValue(1000)
        self.canceled=True
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
        
    def finish(self, returncode: int, output: str = "") -> None:
        if returncode == 0 and not self.canceled:
            if self.packageItem:
                try:
                    i = self.packageItem.treeWidget().takeTopLevelItem(self.packageItem.treeWidget().indexOfTopLevelItem(self.packageItem))
                    del i
                except Exception as e:
                    report(e)
        self.finishedInstallation = True
        self.cancelButton.setEnabled(True)
        removeProgram(self.installId)
        try: self.waitThread.kill()
        except: pass
        try: self.t.kill()
        except: pass
        try: self.p.kill()
        except: pass
        if not(self.canceled):
            if(returncode == 0):
                self.callInMain.emit(lambda: globals.trayIcon.showMessage(f"{self.actionName.capitalize()} succeeded", f"{self.programName} was {self.actionDone} successfully!", QIcon(getMedia("notif_info"))))
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QIcon(realpath+"/resources/tick.png"))
                self.cancelButton.clicked.connect(self.close)
                self.info.setText(f"{self.programName} was {self.actionDone} successfully!")
                self.progressbar.setValue(1000)
                self.startCoolDown()
                if(self.store == "powershell"):
                    msgBox = MessageBox(self)
                    msgBox.setWindowTitle("WingetUI")
                    msgBox.setText(f"{self.programName} was uninstalled successfully.")
                    msgBox.setInformativeText(f"You will need to restart the application in order to get the {self.programName} new packages")
                    msgBox.setStandardButtons(MessageBox.Ok)
                    msgBox.setDefaultButton(MessageBox.Ok)
                    msgBox.setIcon(MessageBox.Information)
                    msgBox.exec_()
            else:
                globals.trayIcon.setIcon(QIcon(getMedia("yellowicon"))) 
                self.cancelButton.setText("OK")
                self.cancelButton.setIcon(QIcon(realpath+"/resources/warn.png"))
                self.cancelButton.clicked.connect(self.close)
                self.progressbar.setValue(1000)
                self.err = ErrorMessage(self.window())
                if(returncode == 2):  # if the installer's hash does not coincide
                    errorData = {
                        "titlebarTitle": f"WingetUI - {self.programName} {self.actionName}",
                        "mainTitle": f"{self.actionName.capitalize()} aborted",
                        "mainText": f"The checksum of the installer does not coincide with the expected value, and the authenticity of the installer can't be verified. If you trust the publisher, {self.actionVerb} the package again skipping the hash check.",
                        "buttonTitle": "Close",
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("warn")),
                        "notifTitle": f"Can't {self.actionVerb} {self.programName}",
                        "notifText": f"The installer has an invalid checksum",
                        "notifIcon": QIcon(getMedia("notif_warn")),
                    }
                else: # if there's a generic error
                    errorData = {
                        "titlebarTitle": f"WingetUI - {self.programName} {self.actionName}",
                        "mainTitle": f"{self.actionName.capitalize()} failed",
                        "mainText": f"We could not {self.actionVerb} {self.programName}. Please try again later. Click on \"Show details\" to get the logs from the installer.",
                        "buttonTitle": "Close",
                        "errorDetails": output.replace("-\|/", "").replace("▒", "").replace("█", ""),
                        "icon": QIcon(getMedia("warn")),
                        "notifTitle": f"Can't {self.actionVerb} {self.programName}",
                        "notifText": f"{self.programName} {self.actionName} failed",
                        "notifIcon": QIcon(getMedia("notif_warn")),
                    }
                self.err.showErrorMessage(errorData)
    
    def close(self):
        globals.installersWidget.removeItem(self)
        super().close()
        super().destroy()

class PackageInfoPopupWindow(QMainWindow):
    onClose = Signal()
    loadInfo = Signal(dict)
    closeDialog = Signal()
    addProgram = Signal(PackageInstallerWidget)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    packageItem: QTreeWidgetItem = None
    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.sc = QScrollArea()
        self.setWindowFlags(Qt.Window)
        self.setWindowModality(Qt.WindowModal)
        self.setWindowFlag(Qt.Tool)
        self.setFocusPolicy(Qt.NoFocus)
        self.setWindowFlag(Qt.FramelessWindowHint)
        self.store = ""
        self.sct = QShortcut(QKeySequence("Esc"), self)
        self.sct.activated.connect(lambda: self.close())
        self.sc.setWidgetResizable(True)
        self.setStyleSheet("""
        QScrollArea{
            border-radius: 5px;
            padding: 5px;
        }
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
        self.title.setStyleSheet("font-size: 40px;")
        self.title.setText("Loading...")

        fortyWidget = QWidget()
        fortyWidget.setFixedWidth(120)

        fortyTopWidget = QWidget()
        fortyTopWidget.setFixedWidth(120)
        fortyTopWidget.setMinimumHeight(30)

        self.mainGroupBox = QGroupBox()

        self.layout.addWidget(self.title)
        self.layout.addStretch()

        self.hLayout = QHBoxLayout()
        self.oLayout = QHBoxLayout()
        self.description = QLinkLabel("Description: Unknown")
        self.description.setWordWrap(True)

        self.layout.addWidget(self.description)

        self.homepage = QLinkLabel("Homepage URL: Unknown")
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = QLinkLabel("Publisher: Unknown")
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = QLinkLabel("Author: Unknown")
        self.author.setWordWrap(True)

        self.layout.addWidget(self.author)
        self.layout.addStretch()

        self.license = QLinkLabel("License: Unknown")
        self.license.setWordWrap(True)

        self.layout.addWidget(self.license)
        self.layout.addStretch()
        
        hLayout = QHBoxLayout()
        self.versionLabel = QLinkLabel("Version: ")

        
        class QComboBoxWithFluentMenu(QComboBox):
            def __init__(self, parent=None) -> None:
                super().__init__(parent)
                v = self.view().window()
                ApplyMenuBlur(v.winId().__int__(), v, avoidOverrideStyleSheet=True)
                self.setItemDelegate(QStyledItemDelegate(self))


        self.versionCombo = QComboBoxWithFluentMenu()
        self.versionCombo.setFixedWidth(150)
        self.versionCombo.setIconSize(QSize(24, 24))
        self.versionCombo.setFixedHeight(25)
        self.installButton = QPushButton()
        self.installButton.setText("Install")
        self.installButton.setObjectName("AccentButton")
        self.installButton.setIconSize(QSize(24, 24))
        self.installButton.clicked.connect(self.install)
        self.installButton.setFixedWidth(150)
        self.installButton.setFixedHeight(30)

        downloadGroupBox = QGroupBox()
        downloadGroupBox.setMinimumHeight(100)
        optionsGroupBox = QGroupBox()

        self.forceCheckbox = QCheckBox()
        self.forceCheckbox.setText("Skip hash check")
        self.forceCheckbox.setChecked(False)
        
        self.interactiveCheckbox = QCheckBox()
        self.interactiveCheckbox.setText("Interactive installation")
        self.interactiveCheckbox.setChecked(False)
        
        self.adminCheckbox = QCheckBox()
        self.adminCheckbox.setText("Run as admin")
        self.adminCheckbox.setChecked(False)

        self.oLayout.addStretch()
        self.oLayout.addWidget(self.forceCheckbox)
        self.oLayout.addWidget(self.interactiveCheckbox)
        self.oLayout.addWidget(self.adminCheckbox)
        self.oLayout.addStretch()

        hLayout.addWidget(self.versionLabel)
        hLayout.addWidget(self.versionCombo)
        hLayout.addWidget(QWidget(), stretch=1)
        hLayout.addWidget(self.installButton)

        vl = QVBoxLayout()
        vl.addStretch()
        vl.addLayout(hLayout)
        vl.addLayout(self.oLayout)
        vl.addStretch()

        downloadGroupBox.setLayout(vl)
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

        self.centralwidget = QWidget()
        self.centralwidget.setLayout(self.hLayout)
        if(isDark()):
            print("[        ] Is Dark")
        self.sc.setWidget(self.centralwidget)
        self.setCentralWidget(self.sc)


        self.backButton = QPushButton(QIcon(getMedia("close")), "", self)
        self.backButton.setStyleSheet("font-size: 22px;")
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(self.width()-40, 0)
        self.backButton.resize(40, 40)
        self.backButton.setFlat(True)
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent}QPushButton:hover{background-color:red;}")
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
    
    def resizeEvent(self, event = None):
        self.centralwidget.setFixedWidth(self.width()-18)
        g = self.mainGroupBox.geometry()
        self.loadingProgressBar.move(0, 0)
        self.loadingProgressBar.resize(self.width(), 4)
        self.backButton.move(self.width()-40, 0)
        if(event):
            return super().resizeEvent(event)
    
    def loadProgram(self, title: str, id: str, goodTitle: bool, store: str, update: bool = False, packageItem: QTreeWidgetItem = None) -> None:
        self.packageItem = packageItem
        self.store = store
        self.installButton.setEnabled(False)
        self.versionCombo.setEnabled(False)
        self.isAnUpdate = update
        self.installButton.setText("Please wait...")
        store = store.lower()
        if(goodTitle):
            self.title.setText(title)
        else:
            self.title.setText(id)
            
        self.loadingProgressBar.show()
        self.forceCheckbox.setChecked(False)
        self.forceCheckbox.setEnabled(False)
        self.interactiveCheckbox.setChecked(False)
        self.interactiveCheckbox.setEnabled(False)
        self.adminCheckbox.setChecked(False)
        self.adminCheckbox.setEnabled(False)
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
        
        if(store.lower()=="winget"):
            Thread(target=wingetHelpers.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()
        elif("scoop" in store.lower()):
            Thread(target=scoopHelpers.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()

    def printData(self, appInfo: dict) -> None:
        self.loadingProgressBar.hide()
        if self.isAnUpdate:
            self.installButton.setText("Update")
        else:
            self.installButton.setText("Install")
        self.installButton.setEnabled(True)
        self.versionCombo.setEnabled(True)
        self.adminCheckbox.setEnabled(True)
        self.forceCheckbox.setEnabled(True)
        if(self.store.lower() == "winget"):
            self.interactiveCheckbox.setEnabled(True)
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
            if self.store.lower() == "winget":
                cmdline_args.append("--force")
            elif self.store.lower() == "scoop":
                cmdline_args.append("--skip")
        if(self.interactiveCheckbox.isChecked()):
            cmdline_args.append("--interactive")
        if(self.versionCombo.currentText()=="Latest"):
            version = []
        else:
            version = ["--version", self.versionCombo.currentText()]
            print(f"[  WARN  ] Issuing specific version {self.versionCombo.currentText()}")
        if self.isAnUpdate:
            p = PackageUpdaterWidget(title, self.store, version, args=cmdline_args, packageId=packageId, admin=self.adminCheckbox.isChecked(), packageItem=self.packageItem, useId=not("…" in packageId))
        else:
            p = PackageInstallerWidget(title, self.store, version, args=cmdline_args, packageId=packageId, admin=self.adminCheckbox.isChecked(), packageItem=self.packageItem, useId=not("…" in packageId))
        self.addProgram.emit(p)
        self.close()

    def show(self) -> None:
        g: QRect = self.parent().window().geometry()
        self.resize(600, 600)
        self.parent().window().blackmatt.show()
        self.move(g.x()+g.width()//2-600//2, g.y()+g.height()//2-600//2)
        print(g.x()+g.width()//2-600//2, g.y()+g.height()//2-600//2)
        return super().show()

    def mousePressEvent(self, event: QMouseEvent) -> None:
        #self.parent().window().activateWindow()
        return super().mousePressEvent(event)

    def close(self) -> bool:
        self.parent().window().blackmatt.hide()
        return super().close()

    def hide(self) -> None:
        try:
            self.parent().window().blackmatt.hide()
        except AttributeError:
            pass
        return super().hide()

if(__name__=="__main__"):
    import __init__
