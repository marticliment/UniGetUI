from __future__ import annotations # to fix NameError: name 'TreeWidgetItemWithQAction' is not defined
import wingetHelpers, scoopHelpers, sys, subprocess, time, os, json
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from storeEngine import *
from lang.translated_percentage import untranslatedPercentage

import globals
from customWidgets import *
from tools import _


class DiscoverSoftwareSection(QWidget):

    addProgram = Signal(str, str, str, str)
    finishLoading = Signal(str)
    clearList = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    callInMain = Signal(object)
    discoverLabelDefaultWidth: int = 0
    discoverLabelIsSmall: bool = False
    isToolbarSmall: bool = False
    toolbarDefaultWidth: int = 0

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = globals.infobox
        self.setStyleSheet("margin: 0px;")
        self.packageReference: dict[str, TreeWidgetItemWithQAction] = {}

        self.programbox = QWidget()
        self.callInMain.connect(lambda f: f())

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

        self.forceCheckBox = QCheckBox(_("Instant search"))
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnInstall"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnInstall", bool(not v)))
         
        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" "+_("Search for packages on Winget and Scoop"))
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
        self.query.setMinimumWidth(100)
        self.query.setMaximumWidth(250)
        self.query.setBaseSize(250, 30)
        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)
        
        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)

        img = QLabel()
        img.setFixedWidth(80)
        img.setPixmap(QIcon(getMedia("desktop_download")).pixmap(QSize(64, 64)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        v.setSpacing(0)
        v.setContentsMargins(0, 0, 0, 0)
        self.discoverLabel = QLabel(_("Discover Packages"))
        self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        v.addWidget(self.discoverLabel)

        self.titleWidget = QWidget()
        self.titleWidget.setContentsMargins(0, 0, 0, 0)
        self.titleWidget.setFixedHeight(70)
        self.titleWidget.setLayout(v)

        hLayout.addWidget(self.titleWidget, stretch=1)
        hLayout.addStretch()
        forceCheckBox = QVBoxLayout()
        forceCheckBox.addWidget(self.forceCheckBox)
        hLayout.addLayout(forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        self.packageListScrollBar = CustomScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList: TreeWidget = TreeWidget("a")
        self.packageList.setHeaderLabels([_("Package Name"), _("Package ID"), _("Version"), _("Source")])
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
            inf = QAction(_("Show info"))
            inf.triggered.connect(lambda: (contextMenu.close(), self.openInfo(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem())))
            inf.setIcon(QIcon(getMedia("info")))
            ins1 = QAction(_("Install"))
            ins1.setIcon(QIcon(getMedia("newversion")))
            ins1.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), packageItem=self.packageList.currentItem()))
            ins2 = QAction(_("Install as administrator"))
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), admin=True, packageItem=self.packageList.currentItem()))
            ins3 = QAction(_("Skip hash check"))
            ins3.setIcon(QIcon(getMedia("checksum")))
            ins3.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3), skiphash=True, packageItem=self.packageList.currentItem()))
            ins4 = QAction(_("Interactive installation"))
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
        self.loadingProgressBar.setStyleSheet("margin: 0px; margin-left: 15px;margin-right: 15px;")

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

        self.toolbar = QToolBar(self.window())
        self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        self.toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("newversion")), "", self.toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.fastinstall(self.packageList.currentItem().text(0), self.packageList.currentItem().text(1), self.packageList.currentItem().text(3).lower(), packageItem=self.packageList.currentItem()))
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

        
        tooltips = {
            self.upgradeSelected: _("Install package"),
            inf: _("Show package info"),
            ins2: _("Run the installer with administrator privileges"),
            ins3: _("Skip the hash check"),
            ins4: _("Interactive installation"),
        }

        for action in [self.upgradeSelected, inf, ins2, ins3, ins4]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)
            self.toolbar.widgetForAction(action).setToolTip(tooltips[action])

        self.toolbar.addSeparator()

        self.importAction = QAction(_("Import packages from a file"), self.toolbar)
        self.importAction.setIcon(QIcon(getMedia("import")))
        self.importAction.triggered.connect(lambda: self.importPackages())
        self.toolbar.addAction(self.importAction)


        self.toolbar.addWidget(TenPxSpacer())
        self.toolbar.addWidget(TenPxSpacer())

        self.countLabel = QLabel(_("Searching for packages..."))
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        v.addWidget(self.countLabel)
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
        layout.setContentsMargins(0, 0, 0, 0)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        hl2 = QHBoxLayout()
        hl2.addWidget(self.packageList)
        hl2.addWidget(self.packageListScrollBar)
        hl2.setSpacing(0)
        hl2.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(hl2)
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
        self.wingetIcon = QIcon(getMedia("winget"))
        self.scoopIcon = QIcon(getMedia("scoop"))

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
            file = QFileDialog.getOpenFileName(self, _("Select package file"), filter="JSON (*.json)")[0]
            if file != "":
                f = open(file, "r")
                contents = json.load(f)
                f.close()
                try:
                    packages = contents["winget"]["Sources"][0]["Packages"]
                    for pkg in packages:
                        packageList.append(pkg["PackageIdentifier"])
                except KeyError as e:
                    print("🟠 Invalid winget section")
                try:
                    packages = contents["scoop"]["apps"]
                    for pkg in packages:
                        packageList.append(pkg["Name"])
                except KeyError as e:
                    print("🟠 Invalid scoop section")
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
            self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.filter()
            self.loadingProgressBar.hide()
            self.countLabel.setText(_("Found packages: {0}").format(str(self.packageList.topLevelItemCount())))
            self.packageList.label.setText("")
            print("🟢 Total packages: "+str(self.packageList.topLevelItemCount()))

    def resizeEvent(self, event: QResizeEvent):
        self.adjustWidgetsSize()
        return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store) -> None:
        if not "---" in name:
            item = TreeWidgetItemWithQAction()
            if "scoop" in store.lower():
                item.setText(0, name.replace("-", " ").capitalize())
            else:
                item.setText(0, name)
            item.setText(1, id)
            item.setIcon(0, self.installIcon)
            item.setIcon(1, self.IDIcon)
            item.setIcon(2, self.versionIcon)
            if "scoop" in store.lower():
                item.setIcon(3, self.scoopIcon)
            else:
                item.setIcon(3, self.wingetIcon)
            item.setText(3, store)
            item.setText(2, version)
            self.packageList.addTopLevelItem(item)
            self.packageReference[id.lower()] = item
    
    def filter(self) -> None:
        print(f"🟢 Searching for string \"{self.query.text()}\"")
        Thread(target=lambda: (time.sleep(0.25), self.callInMain.emit(partial(self.finishFiltering, self.query.text())))).start()
    
    def finishFiltering(self, text: str):
        if self.query.text() != text:
            return
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 0)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        found = 0
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
                found += 1
        if found == 0:
            if self.packageList.label.text() == "":
                self.packageList.label.show()
                self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            if self.packageList.label.text() == _("No packages found matching the input criteria"):
                self.packageList.label.hide()
                self.packageList.label.setText("")
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction) -> None:
        self.infobox.loadProgram(title, id, useId=not("…" in id), store=store, packageItem=packageItem)
        self.infobox.show()

    def fastinstall(self, title: str, id: str, store: str, admin: bool = False, interactive: bool = False, skiphash: bool = False, packageItem: TreeWidgetItemWithQAction = None) -> None:
        if not "scoop" in store.lower():
                self.addInstallation(PackageInstallerWidget(title, "winget", useId=not("…" in id), packageId=id, admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--force" if skiphash else ""])), packageItem=packageItem))
        else:
                self.addInstallation(PackageInstallerWidget(title, "scoop", useId=not("…" in id), packageId=id, admin=admin, args=["--skip" if skiphash else ""], packageItem=packageItem))
    
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
        self.countLabel.setText(_("Searching for packages..."))
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
    
    def destroyAnims(self) -> None:
        for anim in (self.leftSlow, self.leftFast, self.rightFast, self.rightSlow):
            anim: QVariantAnimation
            anim.pause()
            anim.stop()
            anim.valueChanged.disconnect()
            anim.finished.disconnect()
            anim.deleteLater()

    def showEvent(self, event: QShowEvent) -> None:
        self.adjustWidgetsSize()
        return super().showEvent(event)

    def adjustWidgetsSize(self) -> None:
        if self.discoverLabelDefaultWidth == 0:
            self.discoverLabelDefaultWidth = self.discoverLabel.sizeHint().width()
        if self.discoverLabelDefaultWidth > self.titleWidget.width():
            if not self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = True
                self.discoverLabel.setStyleSheet(f"font-size: 15pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        else:
            if self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = False
                self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")

        self.forceCheckBox.setFixedWidth(self.forceCheckBox.sizeHint().width()+10)
        if self.toolbarDefaultWidth == 0:
            self.toolbarDefaultWidth = self.toolbar.sizeHint().width()+2
        if self.toolbarDefaultWidth != 0:
            if self.toolbarDefaultWidth > self.toolbar.width():
                if not self.isToolbarSmall:
                    self.isToolbarSmall = True
                    self.toolbar.setToolButtonStyle(Qt.ToolButtonIconOnly)
            else:
                if self.isToolbarSmall:
                    self.isToolbarSmall = False
                    self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)
        self.forceCheckBox.setFixedWidth(self.forceCheckBox.sizeHint().width()+10)

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
    discoverLabelDefaultWidth: int = 0
    discoverLabelIsSmall: bool = False
    isToolbarSmall: bool = False
    toolbarDefaultWidth: int = 0

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        
        self.callInMain.connect(lambda f: f())
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = globals.infobox
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
        self.query.setPlaceholderText(" "+_("Search on available updates"))
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
        self.query.setMinimumWidth(100)
        self.query.setMaximumWidth(250)
        self.query.setBaseSize(250, 30)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)
        
        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)

        self.forceCheckBox = QCheckBox(_("Instant search"))
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(98)
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnUpgrade"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnUpgrade", bool(not v)))

        self.img = QLabel()
        self.img.setFixedWidth(80)
        self.img.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
        hLayout.addWidget(self.img)

        v = QVBoxLayout()
        v.setSpacing(0)
        v.setContentsMargins(0, 0, 0, 0)
        self.discoverLabel = QLabel(_("Software Updates"))
        self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        v.addWidget(self.discoverLabel)

        self.titleWidget = QWidget()
        self.titleWidget.setContentsMargins(0, 0, 0, 0)
        self.titleWidget.setLayout(v)
        self.titleWidget.setFixedHeight(70)

        hLayout.addWidget(self.titleWidget, stretch=1)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)

        self.packageListScrollBar = CustomScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = TreeWidget("ª")
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.setColumnCount(6)
        self.packageList.setHeaderLabels(["", _("Package Name"), _("Package ID"), _("Installed Version"), _("New Version"), _("Source")])
        self.packageList.setColumnWidth(0, 50)
        self.packageList.setColumnWidth(1, 350)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnWidth(3, 175)
        self.packageList.setColumnWidth(4, 175)
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
            inf = QAction(_("Show info"))
            inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), self.packageList.currentItem()))
            inf.setIcon(QIcon(getMedia("info")))
            ins1 = QAction(_("Update"))
            ins1.setIcon(QIcon(getMedia("newversion")))
            ins1.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem()))
            ins2 = QAction(_("Update as administrator"))
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), admin=True))
            ins3 = QAction(_("Skip hash check"))
            ins3.setIcon(QIcon(getMedia("checksum")))
            ins3.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), skiphash=True))
            ins4 = QAction(_("Interactive update"))
            ins4.setIcon(QIcon(getMedia("interactive")))
            ins4.triggered.connect(lambda: self.update(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5).lower(), packageItem=self.packageList.currentItem(), interactive=True))
            ins5 = QAction(_("Uninstall package"))
            ins5.setIcon(QIcon(getMedia("menu_uninstall")))
            ins5.triggered.connect(lambda: globals.uninstall.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(5), packageItem=self.packageList.currentItem()))
            contextMenu.addAction(ins1)
            contextMenu.addSeparator()
            contextMenu.addAction(ins2)
            if self.packageList.currentItem().text(5).lower() == "winget":
                contextMenu.addAction(ins4)
            contextMenu.addAction(ins3)
            contextMenu.addSeparator()
            ins6 = QAction(_("Ignore updates for this package"))
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
        self.packageList.setColumnWidth(0, 50)
        self.packageList.setColumnWidth(3, 130)
        self.packageList.setColumnWidth(4, 130)
        self.packageList.setColumnWidth(5, 100)

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
        self.loadingProgressBar.setStyleSheet("margin: 0px; margin-left: 15px;margin-right: 15px;")

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
                if not program.isHidden():
                    self.packageList.itemWidget(program, 0).setChecked(checked)

        self.toolbar = QToolBar(self.window())
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

        
        tooltips = {
            self.upgradeSelected: _("Update package"),
            inf: _("Show package info"),
            ins2: _("Update with administrator privileges"),
            ins3: _("Skip the hash check"),
            ins4: _("Interactive update"),
        }

        for action in [self.upgradeSelected, inf, ins2, ins3, ins4]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)
            self.toolbar.widgetForAction(action).setToolTip(tooltips[action])


        self.toolbar.addSeparator()

        self.upgradeAllAction = QAction(QIcon(getMedia("installall")), _("Update all"), self.toolbar)
        self.upgradeAllAction.triggered.connect(lambda: self.updateAll())
        #self.toolbar.addAction(self.upgradeAllAction)
        self.upgradeSelectedAction = QAction(QIcon(getMedia("list")), _("Update selected"), self.toolbar)
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
        self.toolbar.widgetForAction(self.selectNoneAction).setToolTip(_("Select none"))
        self.toolbar.widgetForAction(self.selectAllAction).setToolTip(_("Select all"))

        self.toolbar.addSeparator()

        self.selectAllAction = QAction(QIcon(getMedia("blacklist")), _("Blacklist apps"), self.toolbar)
        self.selectAllAction.triggered.connect(lambda: blacklistSelectedPackages())
        self.toolbar.addAction(self.selectAllAction)
        self.selectAllAction = QAction(QIcon(getMedia("undelete")), _("Reset blacklist"), self.toolbar)
        self.selectAllAction.triggered.connect(lambda: (setSettingsValue("BlacklistedUpdates", ""), self.reload()))
        self.toolbar.addAction(self.selectAllAction)

        self.showUnknownSection = QCheckBox(_("Show unknown versions"))
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

        self.countLabel = QLabel(_("Checking for updates..."))
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        layout.setContentsMargins(0, 0, 0, 0)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        hl2 = QHBoxLayout()
        hl2.addWidget(self.packageList)
        hl2.addWidget(self.packageListScrollBar)
        hl2.setSpacing(0)
        hl2.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(hl2)
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
        self.wingetIcon = QIcon(getMedia("winget"))
        self.scoopIcon = QIcon(getMedia("scoop"))

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
            self.countLabel.setText(_("Available updates: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText(_("Available updates: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            self.scoopLoaded = True
            self.filter()
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.loadingProgressBar.hide()
            self.loadingProgressBar.hide()
            globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}").format(str(self.packageList.topLevelItemCount())))
            count = 0
            lastVisibleItem = None
            for i in range(self.packageList.topLevelItemCount()):
                if not self.packageList.topLevelItem(i).isHidden():
                    count += 1
                    lastVisibleItem = self.packageList.topLevelItem(i)
            self.packageList.label.setText(str(count))
            if not getSettings("DisableUpdatesNotifications"):
                if count > 1:
                    notify(_("Updates found!"), _("{0} apps can be updated").format(count))
                elif count == 1:
                    notify(_("Update found!"), _("{0} can be updated").format(lastVisibleItem.text(1)))
            if count > 0:
                globals.trayIcon.setIcon(QIcon(getMedia("greenicon")))
            else:
                globals.trayIcon.setIcon(QIcon(getMedia("greyicon")))
            self.updatePackageNumber()
            self.packageList.label.setText("")
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
        self.adjustWidgetsSize()
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
                if "scoop" in store.lower():
                    item.setIcon(5, self.scoopIcon)
                else:
                    item.setIcon(5, self.wingetIcon)
                self.packageList.addTopLevelItem(item)
                c = QCheckBox()
                c.setChecked(True)
                c.setStyleSheet("margin-top: 1px; margin-left: 8px;")
                c.stateChanged.connect(lambda: item.setText(0, str(" " if c.isChecked() else "")))
                self.packageList.setItemWidget(item, 0, c)
                action = QAction(name+"  \t"+version+"\t → \t"+newVersion, globals.trayMenuUpdatesList)
                action.triggered.connect(lambda : self.update(name, id, store, packageItem=item))
                action.setShortcut(version)
                item.setAction(action)
                globals.trayMenuUpdatesList.addAction(action)
            else:
                print(id,"was blackisted")
    
    def filter(self) -> None:
        resultsFound = self.packageList.findItems(self.query.text(), Qt.MatchContains, 1)
        resultsFound += self.packageList.findItems(self.query.text(), Qt.MatchContains, 2)
        print(f"🟢 Searching for string \"{self.query.text()}\"")
        found = 0
        for item in self.packageList.findItems('', Qt.MatchContains, 1):
            if not(item in resultsFound):
                item.setHidden(True)
                item.treeWidget().itemWidget(item, 0).hide()
            else:
                item.setHidden(False)
                if item.text(3) == "Unknown":
                    item.setHidden(not self.showUnknownSection.isChecked())
                    if self.showUnknownSection.isChecked():
                        found += 1
                else:
                    found += 1
        if found == 0:
            if self.packageList.label.text() == "":
                self.packageList.label.show()
                self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            if self.packageList.label.text() == _("No packages found matching the input criteria"):
                self.packageList.label.hide()
                self.packageList.label.setText("")
        self.packageList.scrollToItem(self.packageList.currentItem())

    def updatePackageNumber(self, showQueried: bool = False, foundResults: int = 0):
        self.availableUpdates = 0
        for item in self.packageList.findItems('', Qt.MatchContains, 1):
            if not item.isHidden():
                self.availableUpdates += 1
        self.countLabel.setText(_("Available updates: {0}").format(self.availableUpdates))
        globals.trayIcon.setToolTip("WingetUI" if self.availableUpdates == 0 else (_("WingetUI - 1 update is available") if self.availableUpdates == 1 else _("WingetUI - {0} updates are available").format(self.availableUpdates)) )
        globals.trayMenuUpdatesList.menuAction().setText(_("{0} updates found").format(self.availableUpdates))
        if self.availableUpdates > 0:
            self.packageList.label.hide()
            self.packageList.label.setText("")
            self.img.setPixmap(QIcon(getMedia("alert_laptop")).pixmap(QSize(64, 64)))
            globals.app.uaAction.setEnabled(True)
            globals.trayIcon.setIcon(QIcon(getMedia("greenicon")))
        else:
            self.packageList.label.setText(_("Hooray! No updates were found!"))
            self.packageList.label.show()
            globals.app.uaAction.setEnabled(False)
            globals.trayIcon.setIcon(QIcon(getMedia("greyicon")))
            self.img.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
    
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
                    self.addInstallation(PackageUpdaterWidget(title, "winget", useId=not("…" in id), packageId=id, packageItem=packageItem, admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--force" if skiphash else ""]))))
            else:
                    self.addInstallation(PackageUpdaterWidget(title, "scoop",  useId=not("…" in id), packageId=id, packageItem=packageItem, admin=admin, args=["--skip" if skiphash else ""]))
     

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None) -> None:
        self.infobox.loadProgram(title, id, useId=not("…" in id), store=store, update=True, packageItem=packageItem)
        self.infobox.show()

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
        self.countLabel.setText(_("Checking for updates..."))
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

    def destroyAnims(self) -> None:
        for anim in (self.leftSlow, self.leftFast, self.rightFast, self.rightSlow):
            anim: QVariantAnimation
            anim.deleteLater()
            anim.stop()
            anim.start(anim.DeleteWhenStopped)
            anim.pause()
            anim.stop()
            anim.valueChanged.disconnect()
            anim.finished.disconnect()

    def adjustWidgetsSize(self) -> None:
        if self.discoverLabelDefaultWidth == 0:
            self.discoverLabelDefaultWidth = self.discoverLabel.sizeHint().width()
        if self.discoverLabelDefaultWidth > self.titleWidget.width():
            if not self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = True
                self.discoverLabel.setStyleSheet(f"font-size: 15pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        else:
            if self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = False
                self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")

        if self.toolbarDefaultWidth == 0:
            self.toolbarDefaultWidth = self.toolbar.sizeHint().width()+10
        if self.toolbarDefaultWidth > self.toolbar.width():
            if not self.isToolbarSmall:
                self.isToolbarSmall = True
                self.toolbar.setToolButtonStyle(Qt.ToolButtonIconOnly)
        else:
            if self.isToolbarSmall:
                self.isToolbarSmall = False
                self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)
        self.forceCheckBox.setFixedWidth(self.forceCheckBox.sizeHint().width()+10)
        self.showUnknownSection.setFixedWidth(self.showUnknownSection.sizeHint().width()+10)


    def showEvent(self, event: QShowEvent) -> None:
        self.adjustWidgetsSize()
        return super().showEvent(event)

class UninstallSoftwareSection(QWidget):

    addProgram = Signal(str, str, str, str)
    finishLoading = Signal(str)
    clearList = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    discoverLabelDefaultWidth: int = 0
    discoverLabelIsSmall: bool = False
    isToolbarSmall: bool = False
    toolbarDefaultWidth: int = 0

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.infobox = globals.infobox
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

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" "+_("Search on your software"))
        self.query.returnPressed.connect(self.filter)
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
        self.query.setMinimumWidth(100)
        self.query.setMaximumWidth(250)
        self.query.setBaseSize(250, 30)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.reload)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)


        self.forceCheckBox = QCheckBox(_("Instant search"))
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setFixedWidth(98)
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings("DisableInstantSearchOnUninstall"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings("DisableInstantSearchOnUninstall", bool(not v)))


        img = QLabel()
        img.setFixedWidth(80)
        img.setPixmap(QIcon(getMedia("workstation")).pixmap(QSize(64, 64)))
        hLayout.addWidget(img)

        v = QVBoxLayout()
        v.setSpacing(0)
        v.setContentsMargins(0, 0, 0, 0)
        self.discoverLabel = QLabel(_("Installed Packages"))
        self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        v.addWidget(self.discoverLabel)

        self.titleWidget = QWidget()
        self.titleWidget.setFixedHeight(70)
        self.titleWidget.setContentsMargins(0, 0, 0, 0)
        self.titleWidget.setLayout(v)

        hLayout.addWidget(self.titleWidget, stretch=1)
        hLayout.addWidget(self.forceCheckBox)
        hLayout.addWidget(self.query)
        hLayout.addWidget(self.searchButton)
        hLayout.addWidget(self.reloadButton)
        
        self.packageListScrollBar = CustomScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)

        self.packageList = TreeWidget(_("Found 0 Packages"))
        self.packageList.setIconSize(QSize(24, 24))
        self.headers = ["", _("Package Name"), _("Package ID"), _("Installed Version"), _("Source")] # empty header added for checkbox

        self.packageList.setColumnCount(len(self.headers))
        self.packageList.setHeaderLabels(self.headers)
        self.packageList.setColumnWidth(0, 50)
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
            ins1 = QAction(_("Uninstall"))
            ins1.setIcon(QIcon(getMedia("menu_uninstall")))
            ins1.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem()))
            ins2 = QAction(_("Uninstall as administrator"))
            ins2.setIcon(QIcon(getMedia("runasadmin")))
            ins2.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), admin=True))
            ins3 = QAction(_("Remove permanent data"))
            ins3.setIcon(QIcon(getMedia("menu_close")))
            ins3.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), removeData=True))
            ins5 = QAction(_("Interactive uninstall"))
            ins5.setIcon(QIcon(getMedia("interactive")))
            ins5.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), interactive=True))
            ins4 = QAction(_("Show info"))
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
        self.loadingProgressBar.setStyleSheet("margin: 0px; margin-left: 15px;margin-right: 15px;")
        
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
        self.bodyWidget.setLayout(l)

        self.toolbar = QToolBar(self.window())
        self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        self.toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("menu_uninstall")), "", self.toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4).lower(), packageItem=self.packageList.currentItem()))
        self.toolbar.addAction(self.upgradeSelected)
        self.toolbar.widgetForAction(self.upgradeSelected).setFixedSize(40, 45)

        def showInfo():
            item = self.packageList.currentItem()
            if item.text(4).lower() == "local pc":
                self.err = ErrorMessage(self.window())
                errorData = {
                        "titlebarTitle": _("Unable to load informarion"),
                        "mainTitle": _("Unable to load informarion"),
                        "mainText": _("We could not load detailed information about this package, because it was not installed neither from Winget nor Scoop."),
                        "buttonTitle": _("Ok"),
                        "errorDetails": _("Uninstallable packages with the origin listed as \"Local PC\" are not published on any package manager, so there's no information available to show about them."),
                        "icon": QIcon(getMedia("notif_warn")),
                    }
                self.err.showErrorMessage(errorData, showNotification=False)
            else:
                self.openInfo(item.text(1), item.text(2), item.text(5).lower(), item)

        inf = QAction("", self.toolbar)# ("Show info")
        inf.triggered.connect(showInfo)
        inf.setIcon(QIcon(getMedia("info")))
        ins2 = QAction("", self.toolbar)# ("Run as administrator")
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), packageItem=self.packageList.currentItem(), admin=True))
        ins5 = QAction("", self.toolbar)# ("Interactive uninstall")
        ins5.setIcon(QIcon(getMedia("interactive")))
        ins5.triggered.connect(lambda: self.uninstall(self.packageList.currentItem().text(1), self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), interactive=True))
        
        
        tooltips = {
            self.upgradeSelected: _("Uninstall package"),
            inf: _("Show package info"),
            ins2: _("Uninstall with administrator privileges"),
            ins5: _("Interactive uninstall"),
        }

        for action in [self.upgradeSelected, inf, ins2, ins5]:
            self.toolbar.addAction(action)
            self.toolbar.widgetForAction(action).setFixedSize(40, 45)
            self.toolbar.widgetForAction(action).setToolTip(tooltips[action])


        self.toolbar.addSeparator()

        self.upgradeSelectedAction = QAction(QIcon(getMedia("list")), _("Uninstall selected packages"), self.toolbar)
        self.upgradeSelectedAction.triggered.connect(lambda: self.uninstallSelected())
        self.toolbar.addAction(self.upgradeSelectedAction)

        self.toolbar.addSeparator()

        def setAllSelected(checked: bool) -> None:
            for i in range(self.packageList.topLevelItemCount()):
                program: TreeWidgetItemWithQAction = self.packageList.topLevelItem(i)                
                if not program.isHidden():
                    self.packageList.itemWidget(program, 0).setChecked(checked)

        self.selectAllAction = QAction(QIcon(getMedia("selectall")), "", self.toolbar)
        self.selectAllAction.triggered.connect(lambda: setAllSelected(True))
        self.toolbar.addAction(self.selectAllAction)
        self.toolbar.widgetForAction(self.selectAllAction).setFixedSize(40, 45)
        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", self.toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        self.toolbar.addAction(self.selectNoneAction)
        self.toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)
        self.toolbar.widgetForAction(self.selectNoneAction).setToolTip(_("Select none"))
        self.toolbar.widgetForAction(self.selectAllAction).setToolTip(_("Select all"))

        self.toolbar.addSeparator()

        self.exportAction = QAction(QIcon(getMedia("export")), _("Export selected packages to a file"), self.toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection())
        self.toolbar.addAction(self.exportAction)

        self.exportAction = QAction(QIcon(getMedia("export")), _("Export all"), self.toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection(all=True))
        #self.toolbar.addAction(self.exportAction)

        w = QWidget()
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.toolbar.addWidget(w)
        self.toolbar.addWidget(TenPxSpacer())
        self.toolbar.addWidget(TenPxSpacer())



        self.countLabel = QLabel(_("Searching for installed packages..."))
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")
        layout.addLayout(hLayout)
        layout.addWidget(self.toolbar)
        layout.setContentsMargins(0, 0, 0, 0)
        v.addWidget(self.countLabel)
        layout.addWidget(self.loadingProgressBar)
        hl2 = QHBoxLayout()
        hl2.addWidget(self.packageList)
        hl2.addWidget(self.packageListScrollBar)
        hl2.setSpacing(0)
        hl2.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(hl2)
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
        self.wingetIcon = QIcon(getMedia("winget"))
        self.scoopIcon = QIcon(getMedia("scoop"))
        self.localIcon = QIcon(getMedia("localpc"))
        
    
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
            conf = MessageBox.question(self, _("Are you sure?"), _("Do you really want to uninstall {0}?").format(toUninstall[0].text(1)), MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        elif len(toUninstall) > 1:
            conf = MessageBox.question(self, _("Are you sure?"), _("Do you really want to uninstall {0} packages?").format(len(toUninstall)), MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        if conf:
            for program in toUninstall:
                self.uninstall(program.text(1), program.text(2), program.text(4), packageItem=program, avoidConfirm=True)

    def openInfo(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction) -> None:
        self.infobox.loadProgram(title, id, useId=not("…" in id), store=store, packageItem=packageItem)
        self.infobox.show()
        #ApplyMenuBlur(self.infobox.winId(), self.infobox, avoidOverrideStyleSheet=True, shadow=False)


    def finishLoadingIfNeeded(self, store: str) -> None:
        if(store == "winget"):
            self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(str(self.packageList.topLevelItemCount())))
            self.wingetLoaded = True
            self.reloadButton.setEnabled(True)
            self.searchButton.setEnabled(True)
            self.filter()
            self.query.setEnabled(True)
        elif(store == "scoop"):
            self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(self.packageList.topLevelItemCount())))
            if self.packageList.topLevelItemCount() == 0:
                self.packageList.label.setText(self.countLabel.text())
            else:
                self.packageList.label.setText("")
            globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(str(self.packageList.topLevelItemCount())))
            self.scoopLoaded = True
            self.reloadButton.setEnabled(True)
            self.filter()
            self.searchButton.setEnabled(True)
            self.query.setEnabled(True)
        if(self.wingetLoaded and self.scoopLoaded):
            self.filter()
            self.loadingProgressBar.hide()
            globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(str(self.packageList.topLevelItemCount())))
            self.countLabel.setText(_("Found packages: {0}").format(str(self.packageList.topLevelItemCount())))
            self.packageList.label.setText("")
            print("🟢 Total packages: "+str(self.packageList.topLevelItemCount()))

    def adjustWidgetsSize(self) -> None:
        if self.discoverLabelDefaultWidth == 0:
            self.discoverLabelDefaultWidth = self.discoverLabel.sizeHint().width()
        if self.discoverLabelDefaultWidth > self.titleWidget.width():
            if not self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = True
                self.discoverLabel.setStyleSheet(f"font-size: 15pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        else:
            if self.discoverLabelIsSmall:
                self.discoverLabelIsSmall = False
                self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")

        if self.toolbarDefaultWidth == 0:
            self.toolbarDefaultWidth = self.toolbar.sizeHint().width()+2
        if self.toolbarDefaultWidth != 0:
            if self.toolbarDefaultWidth > self.toolbar.width():
                if not self.isToolbarSmall:
                    self.isToolbarSmall = True
                    self.toolbar.setToolButtonStyle(Qt.ToolButtonIconOnly)
            else:
                if self.isToolbarSmall:
                    self.isToolbarSmall = False
                    self.toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)
        self.forceCheckBox.setFixedWidth(self.forceCheckBox.sizeHint().width()+10)

    def resizeEvent(self, event: QResizeEvent):
        self.adjustWidgetsSize()
        return super().resizeEvent(event)
        
    def showEvent(self, event: QShowEvent) -> None:
        self.adjustWidgetsSize()
        return super().showEvent(event)


    def addItem(self, name: str, id: str, version: str, store: str) -> None:
        if not "---" in name:
            item = TreeWidgetItemWithQAction()
            if store.lower() == "winget":
                for illegal_char in ("{", "}", "_", " "):
                    if illegal_char in id:
                        store = "Local PC"
                if store.lower() == "winget":
                    if id.count(".") != 1:
                        store = "Local PC"
                        if id.count(".") > 1:
                            for letter in id:
                                if letter in "AABCDEFGHIJKLMNOPQRSTUVWXYZ":
                                    store = "Winget"
                                    break
            item.setText(1, name)
            item.setText(2, id)
            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setText(3, version)
            if "scoop" in store.lower():
                item.setIcon(4, self.scoopIcon)
            elif "winget" in store.lower():
                item.setIcon(4, self.wingetIcon)
            else:
                item.setIcon(4, self.localIcon)
            item.setText(4, store)
            c = QCheckBox()
            c.setChecked(False)
            c.setStyleSheet("margin-top: 1px; margin-left: 8px;")
            c.stateChanged.connect(lambda: item.setText(0, str(" " if c.isChecked() else "")))

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
        found = 0
        for item in self.packageList.findItems('', Qt.MatchContains, 0):
            if not(item in resultsFound):
                item.setHidden(True)
            else:
                item.setHidden(False)
                found += 1
        if found == 0:
            if self.packageList.label.text() == "":
                self.packageList.label.show()
                self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            if self.packageList.label.text() == _("No packages found matching the input criteria"):
                self.packageList.label.hide()
                self.packageList.label.setText("")
        self.packageList.scrollToItem(self.packageList.currentItem())
    
    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def uninstall(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        if avoidConfirm:
            answer = True
        else:
            answer = MessageBox.question(self, _("Are you sure?"), _("Do you really want to uninstall {0}?").format(title), MessageBox.No | MessageBox.Yes, MessageBox.Yes) == MessageBox.Yes
        if answer:
            print("🔵 Uninstalling", id)
            if not "scoop" in store.lower():
                    self.addInstallation(PackageUninstallerWidget(title, "winget", useId=not("…" in id), packageId=id, packageItem=packageItem, admin=admin, removeData=removeData, args=["--interactive" if interactive else "--silent"]))
            else:
                    self.addInstallation(PackageUninstallerWidget(title, "scoop" , useId=not("…" in id), packageId=id, packageItem=packageItem, admin=admin, removeData=removeData))

    def reload(self) -> None:
        self.scoopLoaded = False
        self.wingetLoaded = False
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        self.countLabel.setText(_("Searching for installed packages..."))
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

            filename = QFileDialog.getSaveFileName(self, _("Save File"), _("wingetui exported packages"), filter='JSON (*.json)')
            if filename[0] != "":
                with open(filename[0], 'w') as f:
                    f.write(json.dumps(overAllSchema, indent=4))

        except Exception as e:
            report(e)

    def destroyAnims(self) -> None:
        for anim in (self.leftSlow, self.leftFast, self.rightFast, self.rightSlow):
            anim: QVariantAnimation
            anim.pause()
            anim.stop()
            anim.valueChanged.disconnect()
            anim.finished.disconnect()
            anim.deleteLater()

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
        title = QLabel(_("Component Information"))
        title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.layout.addWidget(title)

        self.layout.addSpacing(15)
        
        table = QTableWidget()
        table.setAutoFillBackground(True)
        table.setStyleSheet("*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;padding: 0px;margin: 0px;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
        table.setColumnCount(2)
        table.setRowCount(3)
        table.setEnabled(False)
        table.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        table.setShowGrid(False)
        table.setHorizontalHeaderLabels([_("Status"), _("Version")])
        table.setColumnWidth(1, 500)
        table.verticalHeader().setFixedWidth(100)
        table.setVerticalHeaderLabels(["Winget ", "Scoop ", "GSudo "])
        table.setItem(0, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["wingetFound"] else _("Not found")))
        table.setItem(0, 1, QTableWidgetItem(" "+str(globals.componentStatus["wingetVersion"])))
        table.setItem(1, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["scoopFound"] else _("Not found")))
        table.setItem(1, 1, QTableWidgetItem(" "+str(globals.componentStatus["scoopVersion"])))
        table.setItem(2, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["sudoFound"] else _("Not found")))
        table.setItem(2, 1, QTableWidgetItem(" "+str(globals.componentStatus["sudoVersion"])))
        table.horizontalHeaderItem(0).setTextAlignment(Qt.AlignLeft)
        table.setRowHeight(0, 35)
        table.setRowHeight(1, 35)
        table.setRowHeight(2, 35)
        table.horizontalHeaderItem(1).setTextAlignment(Qt.AlignLeft)
        table.verticalHeaderItem(0).setTextAlignment(Qt.AlignRight)
        table.verticalHeaderItem(1).setTextAlignment(Qt.AlignRight)
        table.verticalHeaderItem(2).setTextAlignment(Qt.AlignRight)
        table.setCornerWidget(QLabel(""))
        table.setCornerButtonEnabled(False)
        table.setFixedHeight(150)
        table.cornerWidget().setStyleSheet("background: transparent;")
        self.layout.addWidget(table)
        title = QLabel(_("About WingetUI version {0}").format(versionName))
        title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.layout.addWidget(title)
        self.layout.addSpacing(5)
        description = QLinkLabel(_("The main goal of this project is to create an intuitive UI to manage the most common CLI package managers for Windows, such as Winget and Scoop.")+"\n"+_("This project has no connection with the official {0} project — it's completely unofficial.").format(f"<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli\">Winget</a>"))
        self.layout.addWidget(description)
        self.layout.addSpacing(5)
        self.layout.addWidget(QLinkLabel(f"{_('Homepage')}:   <a style=\"color: {blueColor};\" href=\"https://github.com/martinet101/WingetUI\">https://github.com/martinet101/WingetUI</a>"))
        self.layout.addSpacing(30)

        self.layout.addWidget(QLinkLabel(f"{_('Contributors')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))        
        self.layout.addWidget(QLinkLabel(_("WingetUI wouldn't have been possible with the help of our dear contributors. Check out their GitHub profile, WingetUI wouldn't be possible without them!")))
        GHcontributors = "<ul>"
        for user in ("ppvnf", "RavenMacDaddy", "MisterEvans78", "vedantmgoyal2009", "sklart", "Satanarious", "sitiom", "harleylara", "victorelec14", "neoOpus"):
            GHcontributors += f"<li><a style=\"color:{blueColor}\" href=\"https://github.com/{user}\">{user}</a></li>"
        GHcontributors += "</ul>"
        self.layout.addWidget(QLinkLabel(GHcontributors))
        self.layout.addSpacing(15)

        self.layout.addWidget(QLinkLabel(f"{_('Translators')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))        
        self.layout.addWidget(QLinkLabel(_("WingetUI has not been machine translated. The following users have been in charge of the translations:")))
        translators = "<ul>"
        for user in ("Aaron liu: Chinese", "Ahmet Özmetin: Turkish", "Artem Moldovanenko: Ukranian", "BUGP Association: Chinese", "Cololi: Chinese", "Datacra5H: German", "Evans: French", "GiacoBot: Italian", "gidano: Hungarian", "Maicol Battistini: Italian", "Nemanja Djurcic: Serbian, Croatian", "Operator404: Ukranian", "ppvnf: Portuguese (Portugal and Brazil)", "Rosario Di Mauro: Italian", "Satyam Singh Niranjan: Hindi", "Sergey: Russian", "sho9029: Japanese", "TAKASE, Yuki: Japanese", "yrctw: Chinese", "Артем: Russian", "Martí Climent: Catalan"):
            translators += f"<li>{user}</li>"
        translators += "</ul><br>"
        translators += _("Do you want to translate WingetUI to your language? See how to contribute <a style=\"color:{0}\" href=\"{1}\"a>HERE!</a>").format(blueColor, "https://github.com/martinet101/WingetUI/wiki#translating-wingetui")
        self.layout.addWidget(QLinkLabel(translators))
        self.layout.addSpacing(15)
        
        self.layout.addWidget(QLinkLabel(f"{_('About the dev')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))        
        self.layout.addWidget(QLinkLabel(_("Hi, my name is Martí, and i am the <i>developer</i> of WingetUI. WingetUI has been entirely made on my free time!")))
        self.layout.addWidget(QLinkLabel(_("Check out my {0} and my {1}!").format(f"<a style=\"color:{blueColor}\" href=\"https://github.com/martinet101\">{_('GitHub profile')}</a>", f"<a style=\"color:{blueColor}\" href=\"http://www.somepythonthings.tk\">{_('homepage')}</a>")))
        self.layout.addWidget(QLinkLabel(_("Do you find WingetUI useful? You'd like to support the developer? If so, you can {0}, it helps a lot!").format(f"<a style=\"color:{blueColor}\" href=\"https://ko-fi.com/martinet101\">{_('buy me a coffee')}</a>")))

        self.layout.addSpacing(15)
        self.layout.addWidget(QLinkLabel(f"{_('Licenses')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
        self.layout.addWidget(QLabel())
        self.layout.addWidget(QLinkLabel(f"WingetUI:&nbsp;&nbsp;&nbsp;&nbsp;LGPL v2.1:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/martinet101/WinGetUI/blob/main/LICENSE\">https://github.com/martinet101/WinGetUI/blob/main/LICENSE</a>"))
        self.layout.addWidget(QLabel())
        self.layout.addWidget(QLinkLabel(f"PySide6:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;LGPLv3:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">https://www.gnu.org/licenses/lgpl-3.0.html</a>"))
        self.layout.addWidget(QLinkLabel(f"Python3:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{('PSF License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://docs.python.org/3/license.html#psf-license\">https://docs.python.org/3/license.html#psf-license</a>"))
        self.layout.addWidget(QLinkLabel(f"Pywin32:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{('PSF License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://spdx.org/licenses/PSF-2.0.html\">https://spdx.org/licenses/PSF-2.0.html</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"Winget:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli/blob/master/LICENSE\">https://github.com/microsoft/winget-cli/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"Scoop:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;Unlicense:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/lukesampson/scoop/blob/master/LICENSE\">https://github.com/lukesampson/scoop/blob/master/LICENSE</a>"))
        self.layout.addWidget(QLinkLabel(f"GSudo:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;{('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/gerardog/gsudo/blob/master/LICENSE.txt\">https://github.com/gerardog/gsudo/blob/master/LICENSE.txt</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel(f"{('Icons')}:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;{('By Icons8')}:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://icons8.com\">https://icons8.com</a>"))
        self.layout.addWidget(QLinkLabel())
        self.layout.addWidget(QLinkLabel())
        button = QPushButton(_("About Qt6"))
        button.setFixedWidth(710)
        button.setFixedHeight(25)
        button.clicked.connect(lambda: MessageBox.aboutQt(self, _("WingetUI - About Qt6")))
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
        title = QLabel(_("WingetUI Settings"))

        title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.layout.addWidget(title)
        self.layout.addSpacing(20)

        self.generalTitle = QSettingsTitle(_("General preferences"), getMedia("settings"), _("Language, theme and other miscellaneous preferences"))
        self.layout.addWidget(self.generalTitle)

        self.language = QSettingsComboBox(_("WingetUI display language:")+" (Language)")
        self.generalTitle.addWidget(self.language)
        self.language.restartButton.setText(_("Restart WingetUI")+" (Restart)")
        self.language.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")

        langListWithPercentage = []
        langDictWithPercentage = {}
        invertedLangDict = {}
        for key, value in languageReference.items():
            if (key in untranslatedPercentage):
                perc = untranslatedPercentage[key]
                if (perc == "0%"): continue
                if not key in lang["locale"]:
                    langListWithPercentage.append(f"{value} ({perc})")
                    langDictWithPercentage[key] = f"{value} ({perc})"
                    invertedLangDict[f"{value} ({perc})"] = key
                else:
                    k = len(lang.keys())
                    v = len([val for val in lang.values() if val != None])
                    perc = f"{int(v/k*100)}%"
                    langListWithPercentage.append(f"{value} ({perc})")
                    langDictWithPercentage[key] = f"{value} ({perc})"
                    invertedLangDict[f"{value} ({perc})"] = key
            else:
                invertedLangDict[value] = key
                langDictWithPercentage[key] = value
                langListWithPercentage.append(value)
        try:
            cprint(invertedLangDict)
            self.language.combobox.insertItems(0, langListWithPercentage)
            self.language.combobox.setCurrentIndex(langListWithPercentage.index(langDictWithPercentage[langName]))
        except Exception as e:
            report(e)
            self.language.combobox.insertItems(0, langListWithPercentage)

        def changeLang():
            self.language.restartButton.setVisible(True)
            i = self.language.combobox.currentIndex()
            selectedLang = invertedLangDict[self.language.combobox.currentText()] # list(languageReference.keys())[i]
            cprint(invertedLangDict[self.language.combobox.currentText()])
            setSettingsValue("PreferredLanguage", selectedLang)

        def restartElevenClockByLangChange():
            subprocess.run(str("start /B \"\" \""+sys.executable)+"\"", shell=True)
            globals.app.quit()

        self.language.restartButton.clicked.connect(restartElevenClockByLangChange)
        self.language.combobox.currentTextChanged.connect(changeLang)
        
        updateCheckBox = QSettingsCheckBox(_("Update WingetUI automatically"))
        updateCheckBox.setChecked(not getSettings("DisableAutoUpdateWingetUI"))
        updateCheckBox.stateChanged.connect(lambda v: setSettings("DisableAutoUpdateWingetUI", not bool(v)))
        self.generalTitle.addWidget(updateCheckBox)
        dontUseBuiltInGsudo = QSettingsCheckBox(_("Use installed GSudo instead of the bundled one (requires app restart)"))
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: setSettings("UseUserGSudo", bool(v)))
        self.generalTitle.addWidget(dontUseBuiltInGsudo)
        

        self.theme = QSettingsComboBox(_("Application theme:"))
        self.generalTitle.addWidget(self.theme)
        self.theme.restartButton.setText(_("Restart WingetUI"))
        
        themes = {
            _("Light"): "light",
            _("Dark"): "dark",
            _("Follow system color scheme"): "auto"
        }
        invertedThemes = {
            "light" : _("Light"),
            "dark" : _("Dark"),
            "auto" : _("Follow system color scheme")
        }

        self.theme.combobox.insertItems(0, list(themes.keys()))
        currentValue = getSettingsValue("PreferredTheme")
        try:
            self.theme.combobox.setCurrentText(invertedThemes[currentValue])
        except KeyError:
            self.theme.combobox.setCurrentText(_("Follow system color scheme"))
        except Exception as e:
            report(e)
        
        self.theme.combobox.currentTextChanged.connect(lambda v: (setSettingsValue("PreferredTheme", themes[v]), self.theme.restartButton.setVisible(True)))
        self.theme.restartButton.clicked.connect(restartElevenClockByLangChange)

        self.startup = QSettingsTitle(_("Startup options"), getMedia("launch"), _("WingetUI autostart behaviour, application launch settings"))    
        self.layout.addWidget(self.startup)
        doCloseWingetUI = QSettingsCheckBox(_("Autostart WingetUI in the notifications area"))
        doCloseWingetUI.setChecked(not getSettings("DisableAutostart"))
        doCloseWingetUI.stateChanged.connect(lambda v: setSettings("DisableAutostart", not bool(v)))
        self.startup.addWidget(doCloseWingetUI)
        disableUpdateIndexes = QSettingsCheckBox(_("Do not update package indexes on launch"))
        disableUpdateIndexes.setChecked(getSettings("DisableUpdateIndexes"))
        self.startup.addWidget(disableUpdateIndexes)
        enableScoopCleanup = QSettingsCheckBox(_("Enable Scoop cleanup on launch"))
        disableUpdateIndexes.stateChanged.connect(lambda v: setSettings("DisableUpdateIndexes", bool(v)))
        enableScoopCleanup.setChecked(getSettings("EnableScoopCleanup"))
        enableScoopCleanup.stateChanged.connect(lambda v: setSettings("EnableScoopCleanup", bool(v)))
        enableScoopCleanup.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.startup.addWidget(enableScoopCleanup)
        
        self.UITitle = QSettingsTitle(_("User interface preferences"), getMedia("interactive"), _("Action when double-clicking packages, hide successful installations"))
        self.layout.addWidget(self.UITitle)
        changeDefaultInstallAction = QSettingsCheckBox(_("Directly install when double-clicking an item on the Discover Software tab (instead of showing the package info)"))
        changeDefaultInstallAction.setChecked(getSettings("InstallOnDoubleClick"))
        changeDefaultInstallAction.stateChanged.connect(lambda v: setSettings("InstallOnDoubleClick", bool(v)))
        self.UITitle.addWidget(changeDefaultInstallAction)
        changeDefaultUpdateAction = QSettingsCheckBox(_("Show info about the package on the Updates tab"))
        changeDefaultUpdateAction.setChecked(not getSettings("DoNotUpdateOnDoubleClick"))
        changeDefaultUpdateAction.stateChanged.connect(lambda v: setSettings("DoNotUpdateOnDoubleClick", bool(not v)))
        self.UITitle.addWidget(changeDefaultUpdateAction)
        dontUseBuiltInGsudo = QSettingsCheckBox(_("Remove successful installs/uninstalls/updates from the installation list"))
        dontUseBuiltInGsudo.setChecked(not getSettings("MaintainSuccessfulInstalls"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: setSettings("MaintainSuccessfulInstalls", not bool(v)))
        self.UITitle.addWidget(dontUseBuiltInGsudo)
        


        self.trayIcon = QSettingsTitle(_("Notification tray options"), getMedia("systemtray"), _("WingetUI tray application preferences"))
        self.layout.addWidget(self.trayIcon)

        doCloseWingetUI = QSettingsCheckBox(_("Close WingetUI to the notification area"))
        doCloseWingetUI.setChecked(not getSettings("DisablesystemTray"))
        doCloseWingetUI.stateChanged.connect(lambda v: setSettings("DisablesystemTray", not bool(v)))
        self.trayIcon.addWidget(doCloseWingetUI)
        checkForUpdates = QSettingsCheckBox(_("Check for package updates periodically"))
        checkForUpdates.setChecked(not getSettings("DisableAutoCheckforUpdates"))
        self.trayIcon.addWidget(checkForUpdates)

        frequencyCombo = QSettingsComboBox(_("Check for updates every:"), buttonEnabled=False)
        
        times = {
            _("30 minutes"): "1800",
            _("1 hour"): "3600",
            _("2 hours"): "7200",
            _("4 hours"): "14400",
            _("8 hours"): "28800",
        }
        invertedTimes = {
            "1800" : _("30 minutes"),
            "3600" : _("1 hour"),
            "7200" : _("2 hours"),
            "14400": _("4 hours"),
            "28800": _("8 hours"),
        }

        frequencyCombo.setEnabled(checkForUpdates.isChecked())
        checkForUpdates.stateChanged.connect(lambda v: (setSettings("DisableAutoCheckforUpdates", not bool(v)), frequencyCombo.setEnabled(bool(v))))
        frequencyCombo.combobox.insertItems(0, list(times.keys()))
        currentValue = getSettingsValue("UpdatesCheckInterval")
        try:
            frequencyCombo.combobox.setCurrentText(invertedTimes[currentValue])
        except KeyError:
            frequencyCombo.combobox.setCurrentText(_("1 hour"))
        except Exception as e:
            report(e)
        
        frequencyCombo.combobox.currentTextChanged.connect(lambda v: setSettingsValue("UpdatesCheckInterval", times[v]))

        self.trayIcon.addWidget(frequencyCombo)
        frequencyCombo.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius:0 ;border-bottom: 0px;}")


        notifyAboutUpdates = QSettingsCheckBox(_("Show a notification when there are available updates"))
        notifyAboutUpdates.setChecked(not getSettings("DisableUpdatesNotifications"))
        notifyAboutUpdates.stateChanged.connect(lambda v: setSettings("DisableUpdatesNotifications", not bool(v)))
        notifyAboutUpdates.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.trayIcon.addWidget(notifyAboutUpdates)



        self.pacmanPrefs = QSettingsTitle(_("Winget preferences"), getMedia("winget"), _("Winget package manager specific preferences"))
        self.layout.addWidget(self.pacmanPrefs)
        
        parallelInstalls = QSettingsCheckBox(_("Allow parallel installs (NOT RECOMMENDED)"))
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.stateChanged.connect(lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.pacmanPrefs.addWidget(parallelInstalls)
        disableWinget = QSettingsCheckBox(_("Disable Winget"))
        disableWinget.setChecked(getSettings("DisableWinget"))
        disableWinget.stateChanged.connect(lambda v: setSettings("DisableWinget", bool(v)))
        self.pacmanPrefs.addWidget(disableWinget)
        button = QSettingsButton(_("Reset Winget sources (might help if no packages are listed)"), _("Reset"))
        button.clicked.connect(lambda: (os.startfile(os.path.join(realpath, "resources/reset_winget_sources.cmd"))))
        self.pacmanPrefs.addWidget(button)
        
        self.pacmanPrefs = QSettingsTitle(_("Scoop preferences"), getMedia("scoop"), _("Scoop package manager specific preferences"))
        self.layout.addWidget(self.pacmanPrefs)

        scoopPreventCaps = QSettingsCheckBox(_("Show Scoop apps as lowercase"))
        scoopPreventCaps.setChecked(getSettings("LowercaseScoopApps"))
        scoopPreventCaps.stateChanged.connect(lambda v: setSettings("LowercaseScoopApps", bool(v)))
        self.pacmanPrefs.addWidget(scoopPreventCaps)
        disableScoop = QSettingsCheckBox(_("Disable Scoop"))
        disableScoop.setChecked(getSettings("DisableScoop"))
        disableScoop.stateChanged.connect(lambda v: setSettings("DisableScoop", bool(v)))
        self.pacmanPrefs.addWidget(disableScoop)
        button = QSettingsButton(_("Add a bucket to Scoop"), _("Add"))
        button.clicked.connect(lambda: self.scoopAddExtraBucket())
        button.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.pacmanPrefs.addWidget(button)
        button = QSettingsButton(_("Remove a bucket from Scoop"), _("Remove"))
        button.clicked.connect(lambda: self.scoopRemoveExtraBucket())
        self.pacmanPrefs.addWidget(button)
        button.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        button = QSettingsButton(_("Install Scoop"), _("Install"))
        button.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        button.clicked.connect(lambda: (setSettings("DisableScoop", False), disableScoop.setChecked(False), os.startfile(os.path.join(realpath, "resources/install_scoop.cmd"))))
        self.pacmanPrefs.addWidget(button)
        button = QSettingsButton(_("Uninstall Scoop (and its packages)"), _("Uninstall"))
        button.clicked.connect(lambda: (setSettings("DisableScoop", True), disableScoop.setChecked(True), os.startfile(os.path.join(realpath, "resources/uninstall_scoop.cmd"))))
        self.pacmanPrefs.addWidget(button)
        self.layout.addStretch()
        
        print("🟢 Settings tab loaded!")
        
    def scoopAddExtraBucket(self) -> None:
        r = QInputDialog.getItem(self, _("Scoop bucket manager"), _("Which bucket do you want to add?"), ["main", "extras", "versions", "nirsoft", "php", "nerd-fonts", "nonportable", "java", "games"], 1, editable=False)
        if r[1]:
            print(r[0])
            globals.installersWidget.addItem(PackageInstallerWidget(f"{r[0]} Scoop bucket", "custom", customCommand=f"scoop bucket add {r[0]}"))
    
    def scoopRemoveExtraBucket(self) -> None:
        r = QInputDialog.getItem(self, _("Scoop bucket manager"), _("Which bucket do you want to remove?"), ["main", "extras", "versions", "nirsoft", "php", "nerd-fonts", "nonportable", "java", "games"], 1, editable=False)
        if r[1]:
            print(r[0])
            globals.installersWidget.addItem(PackageUninstallerWidget(f"{r[0]} Scoop bucket", "custom", customCommand=f"scoop bucket rm {r[0]}"))

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
                a.setText(_("Reload log"))
                a.triggered.connect(lambda: self.setPlainText(buffer.getvalue()))
                menu.addAction(a)

                
                a4 = QAction()
                a4.setText(_("Show missing translation strings"))
                a4.triggered.connect(lambda: self.setPlainText('\n'.join(missingTranslationList)))#buffer.getvalue()))
                menu.addAction(a4)


                a2 = QAction()
                a2.setText(_("Export log as a file"))
                a2.triggered.connect(lambda: saveLog())
                menu.addAction(a2)

                a3 = QAction()
                a3.setText(_("Copy log to clipboard"))
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

        reloadButton = QPushButton(_("Reload log"))
        reloadButton.setFixedWidth(200)
        reloadButton.clicked.connect(lambda: self.textEdit.setPlainText(buffer.getvalue()))

        def saveLog():
            try:
                print("🔵 Saving log...")
                f = QFileDialog.getSaveFileName(self, _("Export log"), os.path.expanduser("~"), f"{_('Text file')} (.txt)")
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

        exportButtom = QPushButton(_("Export log as a file"))
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

        copyButton = QPushButton(_("Copy log to clipboard"))
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
