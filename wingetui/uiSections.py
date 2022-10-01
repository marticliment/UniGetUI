from __future__ import annotations # to fix NameError: name 'TreeWidgetItemWithQAction' is not defined
import wingetHelpers, scoopHelpers, sys, subprocess, time, os, json
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from storeEngine import *

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
        self.packageReference: dict[str, TreeWidgetItemWithQAction] = {}

        self.programbox = QWidget()

        self.layout = QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setStyleSheet("margin-top: 0px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 30)
        self.searchButton.setStyleSheet("margin-top: 0px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        self.forceCheckBox = QCheckBox("Instant search")
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(140)
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnInstall"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnInstall", bool(not v)))
         
        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search something on Winget or Scoop")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
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

        self.packageList: TreeWidget = TreeWidget("a")
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Version", "Origin"])
        self.packageList.setColumnCount(4)
        self.packageList.sortByColumn(0, Qt.AscendingOrder)
        self.packageList.setSortingEnabled(True)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item.text(0), item.text(1), item.text(3), item) if not getSettings("InstallOnDoubleClick") else self.fastinstall(item.text(0), item.text(1), item.text(3)))

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
            inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem()))
            inf.setIcon(QIcon(getMedia("info")))
            ins1 = QAction("Install")
            ins1.setIcon(QIcon(getMedia("performinstall")))
            ins1.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem()))
            ins2 = QAction("Run as administrator")
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), admin=True, packageItem=self.packageList.currentItem()))
            ins3 = QAction("Skip hash check")
            ins3.setIcon(QIcon(getMedia("checksum")))
            ins3.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), skiphash=True, packageItem=self.packageList.currentItem()))
            ins4 = QAction("Interactive installation")
            ins4.setIcon(QIcon(getMedia("interactive")))
            ins4.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), interactive=True, packageItem=self.packageList.currentItem()))
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

        self.toolbar = QToolBar(self)
        self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        self.toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("newversion")), "", self.toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(3).lower(), packageItem=self.packageList.currentItem()))
        self.toolbar.addAction(self.upgradeSelected)
        
        inf = QAction("", self.toolbar)# ("Show info")
        inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3).lower(), self.packageList.currentItem()))
        inf.setIcon(QIcon(getMedia("info")))
        ins2 = QAction("", self.toolbar)# ("Run as administrator")
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3).lower(), packageItem=self.packageList.currentItem(), admin=True))
        ins3 = QAction("", self.toolbar)# ("Skip hash check")
        ins3.setIcon(QIcon(getMedia("checksum")))
        ins3.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3).lower(), packageItem=self.packageList.currentItem(), skiphash=True))
        ins4 = QAction("", self.toolbar)# ("Interactive update")
        ins4.setIcon(QIcon(getMedia("interactive")))
        ins4.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3).lower(), packageItem=self.packageList.currentItem(), interactive=True))

        for action in [self.upgradeSelected, inf, ins2, ins3, ins4]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)

        self.toolbar.addSeparator()

        self.importAction = QAction("Import packages from a file", self.toolbar)
        self.importAction.setIcon(QIcon(getMedia("import")))
        self.importAction.triggered.connect(lambda: self.importPackages())
        self.toolbar.addAction(self.importAction)


        self.toolbar.addWidget(TenPxSpacer())
        self.toolbar.addWidget(TenPxSpacer())

        self.countLabel = QLabel("Searching for packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        v.addWidget(self.countLabel)
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
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
        print("🟢 Discover tab loaded")

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

    def importPackages(self):
        try:
            packageList = []
            file = QFileDialog.getOpenFileName(self, "Select package file", filter="JSON (*.json)")[0]
            if file != "":
                f = open(file, "r")
                contents = json.load(f)
                f.close()
                try:
                    packages = contents["winget"]["Sources"][0]["Packages"]
                    for pkg in packages:
                        packageList.append(pkg["PackageIdentifier"])
                except KeyError as e:
                    cprint(e)
                    print("🟠 Invalid winget section")
                try:
                    packages = contents["scoop"]["apps"]
                    for pkg in packages:
                        packageList.append(pkg["Name"])
                except KeyError as e:
                    cprint(e)
                    print("🟠 Invalid scoop section")
                cprint(packageList)
                for packageId in packageList:
                    try:
                        item = self.packageReference[packageId.lower()]
                        self.fastinstall(item.text(0), item.text(1), item.text(3))
                    except KeyError:
                        print(f"🟠 Can't find package {packageId} in the package reference")
        except Exception as e:
            report(e)
        
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
            print("🟢 Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event: QResizeEvent):
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
            self.packageReference[id.lower()] = item
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 0)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        print(f"🟢 Searching for string \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction) -> None:
        self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), useId=not("…" in id), store=store, packageItem=packageItem)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(), self.infobox, avoidOverrideStyleSheet=True, shadow=False)

    def fastinstall(self, title: str, id: str, store: str, admin: bool = False, interactive: bool = False, skiphash: bool = False, packageItem: TreeWidgetItemWithQAction = None) -> None:
        if not "scoop" in store.lower():
                self.addInstallation(PackageInstallerWidget(title, "winget", useId=not("…" in id), packageId=id.replace("…", ""), admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--force" if skiphash else ""])), packageItem=packageItem))
        else:
                self.addInstallation(PackageInstallerWidget(title, "scoop", useId=not("…" in id), packageId=id.replace("…", ""), admin=admin, args=["--skip" if skiphash else ""], packageItem=packageItem))
    
    def reload(self) -> None:
        self.packageReference = {}
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
    availableUpdates: int = 0

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
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setStyleSheet("margin-top: 0px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 30)
        self.searchButton.setStyleSheet("margin-top: 0px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search available updates")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
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
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
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

        self.packageList.itemDoubleClicked.connect(lambda item, column: (self.update(item.text(1), item.text(2), item.text(5), packageItem=item) if not getSettings("DoNotUpdateOnDoubleClick") else self.openInfo(item.text(1), item.text(2), item.text(5), item)))
        
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
            ins1 = QAction("Upgrade")
            ins1.setIcon(QIcon(getMedia("newversion")))
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
            ins5 = QAction("Uninstall package")
            ins5.setIcon(QIcon(getMedia("menu_uninstall")))
            ins5.triggered.connect(lambda: globals.uninstall.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5), packageItem=self.packageList.currentItem()))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if self.packageList.currentItem().text(5).lower() == "winget":
                contextMenu.addAction(ins4)
            contextMenu.addAction(ins3)
            contextMenu.addSeparator()
            ins6 = QAction("Ignore updates for this package")
            ins6.setIcon(QIcon(getMedia("blacklist")))
            ins6.triggered.connect(lambda: (setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates")+self.packageList.currentItem().text(2)+","), self.packageList.currentItem().setHidden(True)))
            contextMenu.addAction(ins6)
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

        def toggleItemState():
            item = self.packageList.currentItem()
            checkbox = self.packageList.itemWidget(item, 0)
            checkbox.setChecked(not checkbox.isChecked())

        sct = QShortcut(QKeySequence(Qt.Key_Space), self.packageList)
        sct.activated.connect(toggleItemState)
        
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
                program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
                if not program.isHidden():
                    try:
                        if self.packageList.itemWidget(program, 0).isChecked():
                            setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates")+program.text(2)+",")
                            program.setHidden(True)
                    except AttributeError:
                        pass

        def setAllSelected(checked: bool) -> None:
            for i in range(self.packageList.topLevelItemCount()):
                program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
                self.packageList.itemWidget(program, 0).setChecked(checked)

        #h2Layout = QHBoxLayout()
        #h2Layout.setContentsMargins(27, 0, 27, 0)
        self.toolbar = QToolBar(self)
        self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        self.toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("newversion")), "", self.toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem()))
        self.toolbar.addAction(self.upgradeSelected)
        
        inf = QAction("", self.toolbar)# ("Show info")
        inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), self.packageList.currentItem()))
        inf.setIcon(QIcon(getMedia("info")))
        ins2 = QAction("", self.toolbar)# ("Run as administrator")
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), admin=True))
        ins3 = QAction("", self.toolbar)# ("Skip hash check")
        ins3.setIcon(QIcon(getMedia("checksum")))
        ins3.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), skiphash=True))
        ins4 = QAction("", self.toolbar)# ("Interactive update")
        ins4.setIcon(QIcon(getMedia("interactive")))
        ins4.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), interactive=True))

        for action in [self.upgradeSelected, inf, ins2, ins3, ins4]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)


        self.toolbar.addSeparator()

        self.upgradeAllAction = QAction(QIcon(getMedia("installall")), "Upgrade all", self.toolbar)
        self.upgradeAllAction.triggered.connect(lambda: self.updateAll())
        self.toolbar.addAction(self.upgradeAllAction)
        self.upgradeSelectedAction = QAction(QIcon(getMedia("list")), "Upgrade selected", self.toolbar)
        self.upgradeSelectedAction.triggered.connect(lambda: self.updateSelected())
        self.toolbar.addAction(self.upgradeSelectedAction)

        self.toolbar.addSeparator()

        self.selectAllAction = QAction(QIcon(getMedia("selectall")), "", self.toolbar)
        self.selectAllAction.triggered.connect(lambda: setAllSelected(True))
        self.toolbar.addAction(self.selectAllAction)
        self.toolbar.widgetForAction(self.selectAllAction).setFixedSize(40, 45)
        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", self.toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        self.toolbar.addAction(self.selectNoneAction)
        self.toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)

        self.toolbar.addSeparator()

        self.selectAllAction = QAction(QIcon(getMedia("blacklist")), "Blacklist apps", self.toolbar)
        self.selectAllAction.triggered.connect(lambda: blacklistSelectedPackages())
        self.toolbar.addAction(self.selectAllAction)
        self.selectAllAction = QAction(QIcon(getMedia("undelete")), "Reset blacklist", self.toolbar)
        self.selectAllAction.triggered.connect(lambda: (setSettingsValue("BlacklistedUpdates", ""), self.reload()))
        self.toolbar.addAction(self.selectAllAction)

        self.showUnknownSection = QCheckBox("Show unknown versions")
        self.showUnknownSection.setFixedHeight(30)
        self.showUnknownSection.setLayoutDirection(Qt.RightToLeft)
        self.showUnknownSection.setFixedWidth(190)
        self.showUnknownSection.setStyleSheet("margin-top: 0px;")
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
            self.updatePackageNumber()
        self.updatelist = updatelist

        w = QWidget()
        w.setMinimumWidth(1)
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.toolbar.addWidget(w)
        self.toolbar.addWidget(self.showUnknownSection)
        self.toolbar.addWidget(TenPxSpacer())
        self.toolbar.addWidget(TenPxSpacer())


        #h2Layout.addWidget(self.upgradeAllButton)
        #h2Layout.addWidget(self.upgradeSelected)
        #h2Layout.addWidget(self.blacklistButton)
        #h2Layout.addStretch()
        #h2Layout.addWidget(self.showUnknownVersions)

        self.countLabel = QLabel("Checking for updates...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
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
        print("🟢 Upgrades tab loaded")

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
            self.countLabel.setText("Available updates: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
            self.packageList.label.setText(self.countLabel.text())
            globals.trayMenuUpdatesList.menuAction().setText(f"{self.packageList.topLevelItemCount()} updates found")
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText("Available updates: "+str(self.packageList.topLevelItemCount())+", not finished yet...")
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
            self.updatePackageNumber()
            self.packageList.label.setText(self.countLabel.text())
            self.filter()
            self.updatelist()
            if not getSettings("DisableAutoCheckforUpdates"):
                try:
                    waitTime = int(getSettingsValue("UpdatesCheckInterval"))
                except ValueError:
                    print(f"🟡 Can't get custom interval time! (got value was '{getSettingsValue('UpdatesCheckInterval')}')")
                    waitTime = 3600
                Thread(target=lambda: (time.sleep(waitTime), self.reloadSources()), daemon=True, name="AutoCheckForUpdates Thread").start()
            print("🟢 Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event: QResizeEvent):
        self.toolbar.setToolButtonStyle(Qt.ToolButtonIconOnly if self.width()<1070 else Qt.ToolButtonTextBesideIcon)
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
        print(f"🟢 Searching for stringg \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 1):
            if not(item in resultsFound):
                item.setHidden(True)
                item.treeWidget().itemWidget(item, 0).hide()
            else:
                item.setHidden(False)
                if item.text(3) == "Unknown":
                    item.setHidden(not self.showUnknownSection.isChecked())
        self.packageList.scrollToItem(self.packageList.currentItem())

    def updatePackageNumber(self, showQueried: bool = False, foundResults: int = 0):
        self.availableUpdates = 0
        for item in self.packageList.findItems('', Qt.MatchContains, 1):
            if not item.isHidden():
                self.availableUpdates += 1
        self.countLabel.setText(f"Available updates: {self.availableUpdates}")
        globals.trayIcon.setToolTip("WingetUI" if self.availableUpdates == 0 else (f"WingetUI - {self.availableUpdates} update is available" if self.availableUpdates == 1 else f"WingetUI - {self.availableUpdates} updates are available") )
        globals.trayMenuUpdatesList.menuAction().setText(f"{self.availableUpdates} updates found")
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def updateAll(self) -> None:
        for i in range(self.packageList.topLevelItemCount()):
            program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
            if not program.isHidden():
                self.update(program.text(1), program.text(2), program.text(5), packageItem=program)

    def updateSelected(self) -> None:
            for i in range(self.packageList.topLevelItemCount()):
                program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
                if not program.isHidden():
                    try:
                        if self.packageList.itemWidget(program, 0).isChecked():
                           self.update(program.text(1), program.text(2), program.text(5), packageItem=program)
                    except AttributeError:
                        pass
    
    def update(self, title: str, id: str, store: str, all: bool = False, selected: bool = False, packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
            if not "scoop" in store.lower():
                    self.addInstallation(PackageUpdaterWidget(title, "winget", useId=not("…" in id), packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--force" if skiphash else ""]))))
            else:
                    self.addInstallation(PackageUpdaterWidget(title, "scoop",  useId=not("…" in id), packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, args=["--skip" if skiphash else ""]))
     

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None) -> None:
        self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), useId=not("…" in id), store=store, update=True, packageItem=packageItem)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(), self.infobox, avoidOverrideStyleSheet=True, shadow=False)

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
        self.availableUpdates = 0
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
        self.allPkgSelected = False

        self.programbox = QWidget()

        self.layout = QVBoxLayout()
        self.layout.setContentsMargins(5, 5, 5, 5)
        self.setLayout(self.layout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setStyleSheet("margin-top: 0px;")
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QIcon(getMedia("reload")))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 30)
        self.searchButton.setStyleSheet("margin-top: 0px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setIcon(QIcon(getMedia("search")))

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(25, 0, 25, 0)

        #hLayoutExport = QHBoxLayout()
        #hLayout.setContentsMargins(25, 0, 25, 0)

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" Search on your software")
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
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
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
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

        #hLayoutExport.addLayout(v)
        #hLayoutExport.addWidget(self.selectAllPkgsCheckBox)
        #hLayoutExport.addStretch()
        #hLayoutExport.setContentsMargins(0, 0, 0, 0)
        #hLayoutExport.addWidget(self.exportSelectionButton)
        
        self.packageListScrollBar = QScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = TreeWidget("Found 0 Packages")
        self.packageList.setIconSize(QSize(24, 24))
        self.headers = ["", "Package name", "Package ID", "Installed Version", "Installation source"] # empty header added for checkbox
        self.packageList.setColumnCount(len(self.headers))
        self.packageList.setHeaderLabels(self.headers)
        self.packageList.setColumnWidth(0, 46)
        #self.packageList.setColumnWidth(1, 300)
        #self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnHidden(3, False)
        self.packageList.setColumnWidth(4, 120)
        self.packageList.setSortingEnabled(True)
        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Fixed)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(4, QHeaderView.Fixed)
        self.packageList.sortByColumn(1, Qt.AscendingOrder)
        
        def toggleItemState():
            item = self.packageList.currentItem()
            checkbox = self.packageList.itemWidget(item, 0)
            checkbox.setChecked(not checkbox.isChecked())

        sct = QShortcut(QKeySequence(Qt.Key_Space), self.packageList)
        sct.activated.connect(toggleItemState)

        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.uninstall(item.text(1), item.text(2), item.text(4), packageItem=item))
        
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
            ins1.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem()))
            ins2 = QAction("Run as administrator")
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), admin=True))
            ins3 = QAction("Remove permanent data")
            ins3.setIcon(QIcon(getMedia("menu_close")))
            ins3.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), removeData=True))
            ins5 = QAction("Interactive uninstall")
            ins5.setIcon(QIcon(getMedia("interactive")))
            ins5.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), interactive=True))
            ins4 = QAction("Show package info")
            ins4.setIcon(QIcon(getMedia("info")))
            ins4.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), self.packageList.currentItem()))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if "scoop" in self.packageList.currentItem().text(4).lower():
                contextMenu.addAction(ins3)
                contextMenu.addSeparator()
            else:
                contextMenu.addAction(ins5)
            if self.packageList.currentItem().text(4).lower() != "local pc":
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

        #self.selectAllPkgsCheckBox = QCheckBox("Select all")
        #self.selectAllPkgsCheckBox.setFixedHeight(30)
        #self.selectAllPkgsCheckBox.setLayoutDirection(Qt.RightToLeft)
        ##self.selectAllPkgsCheckBox.setFixedWidth(140)
        #self.selectAllPkgsCheckBox.setStyleSheet("margin-top: 0px;")
        #self.selectAllPkgsCheckBox.setChecked(self.allPkgSelected)
        #self.selectAllPkgsCheckBox.clicked.connect(lambda v: self.selectAllInstalled())

        #self.exportSelectionButton = QPushButton("Export selection (beta)")
        #self.exportSelectionButton.setFixedWidth(300)
        #self.exportSelectionButton.setStyleSheet("margin-top: 0px;")
        #self.exportSelectionButton.clicked.connect(self.exportSelection)

        self.toolbar = QToolBar(self)
        self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        self.toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("menu_uninstall")), "", self.toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4).lower(), packageItem=self.packageList.currentItem()))
        self.toolbar.addAction(self.upgradeSelected)
        self.toolbar.widgetForAction(self.upgradeSelected).setFixedSize(40, 45)

        ins2 = QAction("", self.toolbar)# ("Run as administrator")
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), admin=True))
        ins5 = QAction("", self.toolbar)# ("Interactive uninstall")
        ins5.setIcon(QIcon(getMedia("interactive")))
        ins5.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), interactive=True))
        
        for action in [self.upgradeSelected, ins2, ins5]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)


        self.toolbar.addSeparator()

        self.upgradeSelectedAction = QAction(QIcon(getMedia("list")), "Uninstall selected packages", self.toolbar)
        self.upgradeSelectedAction.triggered.connect(lambda: self.uninstallSelected())
        self.toolbar.addAction(self.upgradeSelectedAction)

        self.toolbar.addSeparator()

        def setAllSelected(checked: bool) -> None:
            for i in range(self.packageList.topLevelItemCount()):
                program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
                self.packageList.itemWidget(program, 0).setChecked(checked)

        self.selectAllAction = QAction(QIcon(getMedia("selectall")), "", self.toolbar)
        self.selectAllAction.triggered.connect(lambda: setAllSelected(True))
        self.toolbar.addAction(self.selectAllAction)
        self.toolbar.widgetForAction(self.selectAllAction).setFixedSize(40, 45)
        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", self.toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        self.toolbar.addAction(self.selectNoneAction)
        self.toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)

        self.toolbar.addSeparator()

        self.exportAction = QAction(QIcon(getMedia("export")), "Export selected packages to a file", self.toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection())
        self.toolbar.addAction(self.exportAction)

        self.exportAction = QAction(QIcon(getMedia("export")), "Export all", self.toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection(all=True))
        self.toolbar.addAction(self.exportAction)

        w = QWidget()
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.toolbar.addWidget(w)
        self.toolbar.addWidget(TenPxSpacer())
        self.toolbar.addWidget(TenPxSpacer())



        self.countLabel = QLabel("Searching for installed packages...")
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
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
        print("🟢 Discover tab loaded")

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

    def uninstallSelected(self) -> None:
        toUninstall = []
        for i in range(self.packageList.topLevelItemCount()):
            program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)
            if not program.isHidden():
                try:
                    if self.packageList.itemWidget(program, 0).isChecked():
                        toUninstall.append(program)
                except AttributeError:
                    pass
        conf = False
        if len(toUninstall) == 1:
            conf = MessageBox.question(self, "Are you sure?", f"Do you really want to uninstall {toUninstall[0].text(1)}?", MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        elif len(toUninstall) > 1:
            conf = MessageBox.question(self, "Are you sure?", f"Do you really want to uninstall {len(toUninstall)} packages?", MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        if conf:
            for program in toUninstall:
                self.uninstall(program.text(1), program.text(2), program.text(4), packageItem=program, avoidConfirm=True)

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction) -> None:
        self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), useId=not("…" in id), store=store, packageItem=packageItem)
        self.infobox.show()
        ApplyMenuBlur(self.infobox.winId(), self.infobox, avoidOverrideStyleSheet=True, shadow=False)


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
            print("🟢 Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event: QResizeEvent):
        self.toolbar.setToolButtonStyle(Qt.ToolButtonIconOnly if self.width()<820 else Qt.ToolButtonTextBesideIcon)
        return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store: str) -> None:
        if not "---" in name:
            item = TreeWidgetItemWithQAction()
            if store.lower() == "winget":
                for illegal_char in ("{", "}", "_", " "):
                    if illegal_char in id:
                        store = "Local PC"
                if id.count(".") != 1:
                    store = "Local PC"
            item.setText(1, name)
            item.setText(2, id)
            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setText(3, version)
            item.setIcon(4, self.providerIcon)
            item.setText(4, store)
            c = QCheckBox()
            c.setChecked(False)
            c.setStyleSheet("margin-top: 1px; margin-left: 8px;")
            self.packageList.addTopLevelItem(item)
            self.packageList.setItemWidget(item, 0, c)
            action = QAction(name+" \t"+version, globals.trayMenuInstalledList)
            action.triggered.connect(lambda: (self.uninstall(name, id, store, packageItem=item), print(name, id, store, item)))
            action.setShortcut(version)
            item.setAction(action)
            globals.trayMenuInstalledList.addAction(action)
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 2)
        print(f"🟢 Searching for string \"{self.query.text()}\"")
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def uninstall(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        if avoidConfirm:
            answer = True
        else:
            answer = MessageBox.question(self, "Are you sure?", f"Do you really want to uninstall {title}?", MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        if answer:
            print("🔵 Uninstalling", id)
            if not "scoop" in store:
                    self.addInstallation(PackageUninstallerWidget(title, "winget", useId=not("…" in id), packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, removeData=removeData, args=["--interactive" if interactive else "--silent"]))
            else:
                    self.addInstallation(PackageUninstallerWidget(title, "scoop" , useId=not("…" in id), packageId=id.replace("…", ""), packageItem=packageItem, admin=admin, removeData=removeData))

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

    def selectAllInstalled(self) -> None:
        self.allPkgSelected = not self.allPkgSelected
        for item in [self.packageList.topLevelItem(i) for i in range(self.packageList.topLevelItemCount())]:
            self.packageList.itemWidget(item, 0).setChecked(self.allPkgSelected)
    
    def exportSelection(self, all: bool = False) -> None:
        """
        Export all selected packages into a file.

        Target format: {"winget": wingetschema, "scoop": scoopschema}

        Winget implementation: In progress
        Scoop implementation: To be done
        
        Winget docs
        ---
        JSON schema for export file: https://raw.githubusercontent.com/microsoft/winget-cli/master/schemas/JSON/packages/packages.schema.1.0.json

        """
        wingetPackagesList = []
        scoopPackageList = []

        try:
            for i in range(self.packageList.topLevelItemCount()):
                item = self.packageList.topLevelItem(i)
                if ((self.packageList.itemWidget(item, 0).isChecked() or all) and item.text(4).lower() == "winget"):
                    id = item.text(2).strip()
                    wingetPackage = {"PackageIdentifier": id}
                    wingetPackagesList.append(wingetPackage)
                elif ((self.packageList.itemWidget(item, 0).isChecked() or all) and "scoop" in item.text(4).lower()):
                    scoopPackage = {"Name": item.text(2)}
                    scoopPackageList.append(scoopPackage)

            wingetDetails = {
                "Argument": "https://cdn.winget.microsoft.com/cache",
                "Identifier" : "Microsoft.Winget.Source_8wekyb3d8bbwe",
                "Name": "winget",
                "Type" : "Microsoft.PreIndexed.Package"
            }
            wingetExportSchema = {
                "$schema" : "https://aka.ms/winget-packages.schema.2.0.json",
                "CreationDate" : "2022-08-16T20:55:44.415-00:00", # TODO: get data automatically
                "Sources": [{
                    "Packages": wingetPackagesList,
                    "SourceDetails": wingetDetails}],
                "WinGetVersion" : "1.4.2161-preview" # TODO: get installed winget version
            }
            scoopExportSchema = {
                "apps": scoopPackageList,
            }
            overAllSchema = {
                "winget": wingetExportSchema,
                "scoop": scoopExportSchema
            }

            filename = QFileDialog.getSaveFileName(self, "Save File", "wingetui exported packages", filter='JSON (*.json)')
            if filename[0] != "":
                with open(filename[0], 'w') as f:
                    f.write(json.dumps(overAllSchema, indent=4))

        except Exception as e:
            report(e)


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
        title = QLabel("Component information")
        title.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(title)

        self.layout.addWidget(QLabel())
        
        table = QTableWidget()
        table.setAutoFillBackground(True)
        table.setStyleSheet("*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
        table.setColumnCount(2)
        table.setRowCount(3)
        table.setEnabled(False)
        table.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
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
        table.setCornerWidget(QLabel(""))
        table.setCornerButtonEnabled(True)
        table.cornerWidget().setStyleSheet("background: transparent;")
        self.layout.addWidget(table)
        title = QLabel("About WingetUI "+str(versionName)+"")
        title.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")

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
        self.layout.addStretch()
    
        print("🟢 About tab loaded!")
        
    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements, daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)

class SettingsSection(QScrollArea):
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
        self.announcements.setMinimumWidth(800)
        self.layout.addWidget(self.announcements)
        title = QLabel("General Settings")
        title.setStyleSheet(f"font-size: 40px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(title)

        subtitle = QLabel("General preferences")
        subtitle.setStyleSheet(f"font-size: 25px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(subtitle)
        self.layout.addWidget(QLabel())

        themeTextLabel = QLabel("Application theme:")
        
        themes = {
            "Light": "light",
            "Dark": "dark",
            "Follow system color scheme": "auto"
        }
        invertedThemes = {
            "light" : "Light",
            "dark" : "Dark",
            "auto" : "Follow system color scheme"
        }

        themeText = QComboBox()
        themeText.setFixedWidth(250)
        themeText.insertItems(0, list(themes.keys()))
        currentValue = getSettingsValue("PreferredTheme")
        try:
            themeText.setCurrentText(invertedThemes[currentValue])
        except KeyError:
            themeText.setCurrentText("1 hour")
        except Exception as e:
            report(e)
        
        themeText.currentTextChanged.connect(lambda v: setSettingsValue("PreferredTheme", themes[v]))

        hl = QHBoxLayout()
        hl.setContentsMargins(0, 0, 0, 0)
        hl.addWidget(themeTextLabel)
        hl.addSpacing(20)
        hl.addWidget(themeText)
        hl.addStretch()


        self.layout.addLayout(hl)

        updateCheckBox = QCheckBox("Update WingetUI automatically")
        updateCheckBox.setChecked(not getSettings("DisableAutoUpdateWingetUI"))
        updateCheckBox.clicked.connect(lambda v: setSettings("DisableAutoUpdateWingetUI", not bool(v)))
        self.layout.addWidget(updateCheckBox)
        changeDefaultInstallAction = QCheckBox("Directly install when double-clicking an item on the Discover Software tab (instead of showing the package info)")
        changeDefaultInstallAction.setChecked(getSettings("InstallOnDoubleClick"))
        changeDefaultInstallAction.clicked.connect(lambda v: setSettings("InstallOnDoubleClick", bool(v)))
        self.layout.addWidget(changeDefaultInstallAction)
        changeDefaultUpdateAction = QCheckBox("Show info about the package on the Updates tab")
        changeDefaultUpdateAction.setChecked(not getSettings("DoNotUpdateOnDoubleClick"))
        changeDefaultUpdateAction.clicked.connect(lambda v: setSettings("DoNotUpdateOnDoubleClick", bool(not v)))
        self.layout.addWidget(changeDefaultUpdateAction)
        dontUseBuiltInGsudo = QCheckBox("Use installed GSudo instead of the bundled one (requires app restart)")
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.clicked.connect(lambda v: setSettings("UseUserGSudo", bool(v)))
        self.layout.addWidget(dontUseBuiltInGsudo)
        self.layout.addWidget(QLabel())
    
        subtitle = QLabel("Startup options")
        subtitle.setStyleSheet(f"font-size: 25px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(subtitle)

        doCloseWingetUI = QCheckBox("Close WingetUI to the notification area")
        doCloseWingetUI.setChecked(not getSettings("DisablesystemTray"))
        doCloseWingetUI.clicked.connect(lambda v: setSettings("DisablesystemTray", not bool(v)))
        self.layout.addWidget(doCloseWingetUI)
        doCloseWingetUI = QCheckBox("Autostart wingetUI in the notifications area")
        doCloseWingetUI.setChecked(not getSettings("DisableAutostart"))
        doCloseWingetUI.clicked.connect(lambda v: setSettings("DisableAutostart", not bool(v)))
        self.layout.addWidget(doCloseWingetUI)
        disableUpdateIndexes = QCheckBox("Do not update package indexes on launch")
        disableUpdateIndexes.setChecked(getSettings("DisableUpdateIndexes"))
        disableUpdateIndexes.clicked.connect(lambda v: setSettings("DisableUpdateIndexes", bool(v)))
        self.layout.addWidget(disableUpdateIndexes)
        enableScoopCleanup = QCheckBox("Enable scoop cleanup on launch")
        enableScoopCleanup.setChecked(getSettings("EnableScoopCleanup"))
        enableScoopCleanup.clicked.connect(lambda v: setSettings("EnableScoopCleanup", bool(v)))
        self.layout.addWidget(enableScoopCleanup)


        self.layout.addWidget(QLabel())
        subtitle = QLabel("Notification tray options")
        subtitle.setStyleSheet(f"font-size: 25px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(subtitle)
        checkForUpdates = QCheckBox("Check for updates periodically")
        checkForUpdates.setChecked(not getSettings("DisableAutoCheckforUpdates"))
        checkForUpdates.clicked.connect(lambda v: setSettings("DisableAutoCheckforUpdates", not bool(v)))
        self.layout.addWidget(checkForUpdates)

        updatesFrequencyText = QLabel("Check for updates every:")
        
        times = {
            "30 minutes": "1800",
            "1 hour": "3600",
            "2 hours": "7200",
            "4 hours": "14400",
            "8 hours": "28800",
        }
        invertedTimes = {
            "1800" : "30 minutes",
            "3600" : "1 hour",
            "7200" : "2 hours",
            "14400": "4 hours",
            "28800": "8 hours",
        }

        updatesFrequency = QComboBox()
        updatesFrequency.insertItems(0, list(times.keys()))
        currentValue = getSettingsValue("UpdatesCheckInterval")
        try:
            updatesFrequency.setCurrentText(invertedTimes[currentValue])
        except KeyError:
            updatesFrequency.setCurrentText("1 hour")
        except Exception as e:
            report(e)
        
        updatesFrequency.currentTextChanged.connect(lambda v: setSettingsValue("UpdatesCheckInterval", times[v]))

        hl = QHBoxLayout()
        hl.setContentsMargins(0, 0, 0, 0)
        hl.addWidget(updatesFrequencyText)
        hl.addSpacing(20)
        hl.addWidget(updatesFrequency)
        hl.addStretch()


        self.layout.addLayout(hl)
        notifyAboutUpdates = QCheckBox("Show a notification when there are available updates")
        notifyAboutUpdates.setChecked(not getSettings("DisableUpdatesNotifications"))
        notifyAboutUpdates.clicked.connect(lambda v: setSettings("DisableUpdatesNotifications", not bool(v)))
        self.layout.addWidget(notifyAboutUpdates)
        self.layout.addWidget(QLabel())


        subtitle = QLabel("Package manager preferences")
        subtitle.setStyleSheet(f"font-size: 25px;font-family: \"Segoe UI Variable Display {'semib' if isDark() else ''}\"")
        self.layout.addWidget(subtitle)

        parallelInstalls = QCheckBox("Allow parallel installs (NOT RECOMMENDED)")
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.clicked.connect(lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.layout.addWidget(parallelInstalls)
        disableWinget = QCheckBox("Disable Winget")
        disableWinget.setChecked(getSettings("DisableWinget"))
        disableWinget.clicked.connect(lambda v: setSettings("DisableWinget", bool(v)))
        self.layout.addWidget(disableWinget)
        disableScoop = QCheckBox("Disable Scoop")
        disableScoop.setChecked(getSettings("DisableScoop"))
        disableScoop.clicked.connect(lambda v: setSettings("DisableScoop", bool(v)))
        self.layout.addWidget(disableScoop)
        scoopPreventCaps = QCheckBox("Show scoop apps as lowercase")
        scoopPreventCaps.setChecked(getSettings("LowercaseScoopApps"))
        scoopPreventCaps.clicked.connect(lambda v: setSettings("LowercaseScoopApps", bool(v)))
        self.layout.addWidget(scoopPreventCaps)
        

        self.layout.addWidget(QLabel())
        
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
        button = QPushButton("Ensure scoop is properly installed")
        #button.setFixedWidth(350)
        button.setFixedHeight(30)
        button.clicked.connect(lambda: os.startfile(os.path.join(realpath, "resources/install_scoop.cmd")))
        l.addWidget(button)
        l.setContentsMargins(0, 0, 0, 0)
        self.layout.addLayout(l)
        self.layout.addStretch()
        
        print("🟢 Settings tab loaded!")
        
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

class DebuggingSection(QWidget):
    def __init__(self):
        super().__init__()
        class QPlainTextEditWithFluentMenu(QPlainTextEdit):
            def __init__(self):
                super().__init__()

            def contextMenuEvent(self, e: QContextMenuEvent) -> None:
                menu = self.createStandardContextMenu()
                menu.addSeparator()

                a = QAction()
                a.setText(("Reload log"))
                a.triggered.connect(lambda: self.textEdit.setPlainText(buffer.getvalue()))
                menu.addAction(a)

                a2 = QAction()
                a2.setText(("Export log as a file"))
                a2.triggered.connect(lambda: saveLog())
                menu.addAction(a2)

                a3 = QAction()
                a3.setText(("Copy log to clipboard"))
                a3.triggered.connect(lambda: copyLog())
                menu.addAction(a3)

                ApplyMenuBlur(menu.winId().__int__(), menu)
                menu.exec(e.globalPos())

        self.setObjectName("background")

        self.setLayout(QVBoxLayout())
        self.setContentsMargins(0, 0, 0, 0)

        self.textEdit = QPlainTextEditWithFluentMenu()
        self.textEdit.setReadOnly(True)
        if isDark():
            self.textEdit.setStyleSheet(f"QPlainTextEdit{{margin: 10px;border-radius: 6px;border: 1px solid #161616;}}")
        else:
            self.textEdit.setStyleSheet(f"QPlainTextEdit{{margin: 10px;border-radius: 6px;border: 1px solid #dddddd;}}")

        self.textEdit.setPlainText(buffer.getvalue())

        reloadButton = QPushButton(("Reload log"))
        reloadButton.setFixedWidth(200)
        reloadButton.clicked.connect(lambda: self.textEdit.setPlainText(buffer.getvalue()))

        def saveLog():
            try:
                print("🔵 Saving log...")
                f = QFileDialog.getSaveFileName(self, "Save log", os.path.expanduser("~"), "Text file (.txt)")
                if f[0]:
                    fpath = f[0]
                    if not ".txt" in fpath.lower():
                        fpath += ".txt"
                    with open(fpath, "wb") as fobj:
                        fobj.write(buffer.getvalue().encode("utf-8"))
                        fobj.close()
                    os.startfile(fpath)
                    print("🟢 log saved successfully")
                    self.textEdit.setPlainText(buffer.getvalue())
                else:
                    print("🟡 log save cancelled!")
                    self.textEdit.setPlainText(buffer.getvalue())
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(buffer.getvalue())

        exportButtom = QPushButton(("Export log as a file"))
        exportButtom.setFixedWidth(200)
        exportButtom.clicked.connect(lambda: saveLog())

        def copyLog():
            try:
                print("🔵 Copying log to the clipboard...")
                globals.app.clipboard().setText(buffer.getvalue())
                print("🟢 Log copied to the clipboard successfully!")
                self.textEdit.setPlainText(buffer.getvalue())
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(buffer.getvalue())

        copyButton = QPushButton(("Copy log to clipboard"))
        copyButton.setFixedWidth(200)
        copyButton.clicked.connect(lambda: copyLog())

        hl = QHBoxLayout()
        hl.setSpacing(5)
        hl.setContentsMargins(10, 10, 10, 0)
        hl.addWidget(exportButtom)
        hl.addWidget(copyButton)
        hl.addStretch()
        hl.addWidget(reloadButton)

        self.layout().setSpacing(0)
        self.layout().setContentsMargins(5, 5, 5, 5)
        self.layout().addLayout(hl, stretch=0)
        self.layout().addWidget(self.textEdit, stretch=1)

        self.setAutoFillBackground(True)

    def showEvent(self, event: QShowEvent) -> None:
        self.textEdit.setPlainText(buffer.getvalue())
        return super().showEvent(event)


if __name__ == "__main__":
    import __init__