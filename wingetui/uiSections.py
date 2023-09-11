from __future__ import annotations

import glob  # to fix NameError: name 'TreeWidgetItemWithQAction' is not defined
import json
import os
import subprocess
import sys
import time
from threading import Thread
import win32mica

import globals
from customWidgets import *
from data.contributors import contributorsInfo
from data.translations import languageCredits, untranslatedPercentage
from PackageManagers import PackageClasses
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from storeEngine import *
from tools import *
from tools import _


class DiscoverSoftwareSection(SoftwareSection):
    PackageManagers = StaticPackageManagersList.copy()
    PackagesLoaded = StaticPackagesLoadedDict.copy()

    DynaimcPackageManagers = DynaimcPackageManagersList.copy()
    DynamicPackagesLoaded = DynamicPackagesLoadedDict.copy()

    LastQueryDynamicallyLoaded: str = ""

    finishDynamicLoading = Signal()
    
    ShouldHideGuideArrow: bool = False

    runningThreads = 0

    def __init__(self, parent = None):
        super().__init__(parent = parent)

        self.finishDynamicLoading.connect(self.finishDynamicLoadingIfNeeded)

        self.query.setPlaceholderText(" "+_("Search for packages"))
        self.discoverLabel.setText(_("Discover Packages"))
        self.SectionImage.setPixmap(QIcon(getMedia("desktop_download")).pixmap(QSize(64, 64)))

        self.packageList.setHeaderLabels(["", _("Package Name"), _("Package ID"), _("Version"), _("Source")])
        self.packageList.setColumnCount(7)
        self.packageList.setColumnHidden(5, True)
        self.packageList.setColumnHidden(6, True)
        self.packageList.setSortingEnabled(True)
        self.packageList.sortByColumn(1, Qt.SortOrder.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item) if not getSettings("InstallOnDoubleClick") else self.installPackageItem(item))

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Fixed)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        header.setSectionResizeMode(4, QHeaderView.Fixed)
        self.packageList.setColumnWidth(0, 10)
        self.packageList.setColumnWidth(3, 150)
        self.packageList.setColumnWidth(4, 150)
        self.countLabel.setText(_("Searching for packages..."))
        self.packageList.label.setText(self.countLabel.text())
        #self.informationBanner.setText(_("The packages are being loaded for the first time. This process will take longer than usual, since package caches are being rebuilt."))
        #if not getSettings("WarnedAboutPackages_v2"):
            #setSettings("WarnedAboutPackages_v2", True)
            #self.informationBanner.show()

        self.contextMenu = QMenu(self)
        self.contextMenu.setParent(self)
        self.MenuDetailsAction = QAction(_("Package details"))
        self.MenuDetailsAction.triggered.connect(lambda: (self.contextMenu.close(), self.openInfo(self.packageList.currentItem())))
        self.MenuInstall = QAction(_("Install"))
        self.MenuInstall.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem()))
        self.MenuAdministrator = QAction(_("Install as administrator"))
        self.MenuAdministrator.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), admin=True))
        self.MenuSkipHash = QAction(_("Skip hash check"))
        self.MenuSkipHash.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), skiphash=True))
        self.MenuInteractive = QAction(_("Interactive installation"))
        self.MenuInteractive.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), interactive=True,))
        self.MenuShare = QAction(_("Share this package"))
        self.MenuShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))
        
        self.installIcon = QIcon(getMedia("install"))
        self.installedIcon = QIcon(getMedia("installed"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("newversion"))

        self.contextMenu.addAction(self.MenuInstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuAdministrator)
        self.contextMenu.addAction(self.MenuInteractive)
        self.contextMenu.addAction(self.MenuSkipHash)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuShare)
        self.contextMenu.addAction(self.MenuDetailsAction)
        self.contextMenu.addSeparator()
        
        self.ArrowLabel = QLabel(self.query.parent())
        self.ArrowLabelInAnimation = QVariantAnimation()
        self.ArrowLabelOutAnimation = QVariantAnimation()
        self.ArrowLabelOpacity = QGraphicsOpacityEffect()
        
        def hideArrow():
            if self.ShouldHideGuideArrow:
                self.ShouldHideGuideArrow = False
                self.packageList.label.setStyleSheet("")
                self.ArrowLabel.hide()
                self.ArrowLabelInAnimation.stop()
                self.ArrowLabelOutAnimation.stop()

        if not getSettings("ShownSearchGuideArrow"):
            
            setSettings("ShownSearchGuideArrow", True)
            self.packageList.label.setStyleSheet("color: red")
            self.ShouldHideGuideArrow = True
            self.query.textChanged.connect(hideArrow)
            
            self.ArrowLabel.show()
            self.ArrowLabel.setAttribute(Qt.WidgetAttribute.WA_TransparentForMouseEvents)
            self.ArrowLabelPixmap = QPixmap(getMedia("red_arrow"))
            self.ArrowLabel.resize(self.ArrowLabelPixmap.size())
            self.ArrowLabel.move(self.query.x() + self.query.width()//2 - self.ArrowLabel.width() + 80, self.query.y()+self.query.height())
            self.ArrowLabel.setPixmap(self.ArrowLabelPixmap)
            self.ArrowLabel.setGraphicsEffect(self.ArrowLabelOpacity)
            
            self.ArrowLabelInAnimation.setStartValue(250)
            self.ArrowLabelInAnimation.setEndValue(750)
            self.ArrowLabelInAnimation.setDuration(1000)
            self.ArrowLabelInAnimation.setEasingCurve(QEasingCurve.Type.InOutCubic)
            self.ArrowLabelInAnimation.valueChanged.connect(lambda v: (self.ArrowLabelOpacity.setOpacity(v/1000) if self.ArrowLabel.isVisible() else None, self.ArrowLabel.move(self.query.x() + self.query.width()//2 - self.ArrowLabel.width() + 80, self.query.y()+self.query.height())))
            self.ArrowLabelInAnimation.finished.connect(self.ArrowLabelOutAnimation.start)

            self.ArrowLabelOutAnimation.setStartValue(750)
            self.ArrowLabelOutAnimation.setEndValue(250)
            self.ArrowLabelOutAnimation.setDuration(1000)
            self.ArrowLabelOutAnimation.setEasingCurve(QEasingCurve.Type.InOutCubic)
            self.ArrowLabelOutAnimation.valueChanged.connect(lambda v: (self.ArrowLabelOpacity.setOpacity(v/1000) if self.ArrowLabel.isVisible() else None, self.ArrowLabel.move(self.query.x() + self.query.width()//2 - self.ArrowLabel.width() + 80, self.query.y()+self.query.height())))
            self.ArrowLabelOutAnimation.finished.connect(self.ArrowLabelInAnimation.start)
            
            self.ArrowLabelOutAnimation.start()
            
        self.query.setFocus()

        self.finishInitialisation()
        
    def ApplyIcons(self):
        super().ApplyIcons()
        self.installIcon = QIcon(getMedia("install"))
        self.installedIcon = QIcon(getMedia("installed"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("newversion"))
        
        self.MenuShare.setIcon(QIcon(getMedia("share")))
        self.MenuInteractive.setIcon(QIcon(getMedia("interactive")))
        self.MenuSkipHash.setIcon(QIcon(getMedia("checksum")))
        self.MenuAdministrator.setIcon(QIcon(getMedia("runasadmin")))
        self.MenuInstall.setIcon(QIcon(getMedia("newversion")))
        self.MenuDetailsAction.setIcon(QIcon(getMedia("info")))
        
        self.ToolbarInstall.setIcon(QIcon(getMedia("newversion")))
        self.ToolbarShowInfo.setIcon(QIcon(getMedia("info")))
        self.ToolbarRunAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        self.ToolbarSkipHash.setIcon(QIcon(getMedia("checksum")))
        self.ToolbarInteractive.setIcon(QIcon(getMedia("interactive")))
        self.ToolbarShare.setIcon(QIcon(getMedia("share")))
        self.ToolbarSelectNone.setIcon(QIcon(getMedia("selectnone")))
        self.ToolbarImportPackages.setIcon(QIcon(getMedia("import")))
        self.ToolbarExportPackages.setIcon(QIcon(getMedia("export")))
        self.ToolbarHelp.setIcon(QIcon(getMedia("help")))
        
        self.HelpMenuEntry1.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry2.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry3.setIcon(QIcon(getMedia("launch")))
        
        for item in self.packageItems:
            package: Package = self.ItemPackageReference[item]
            
            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, package.getSourceIcon())
                
            UNINSTALL: UninstallSoftwareSection = globals.uninstall
            if package.Id in UNINSTALL.IdPackageReference.keys():
                installedPackage: UpgradablePackage = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems:
                    item.setIcon(1, self.installedIcon)
            
        
    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)

        try:
            Capabilities: PackageManagerCapabilities =  self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuSkipHash.setVisible(Capabilities.CanSkipIntegrityChecks)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        pos.setY(pos.y()+35)
        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.ToolbarInstall = QAction(_("Install selected packages"), toolbar)
        self.ToolbarInstall.triggered.connect(lambda: self.installSelectedPackageItems())
        toolbar.addAction(self.ToolbarInstall)

        self.ToolbarShowInfo = QAction("", toolbar)# ("Show info")
        self.ToolbarShowInfo.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        self.ToolbarRunAsAdmin = QAction("", toolbar)# ("Run as administrator")
        self.ToolbarRunAsAdmin.triggered.connect(lambda: self.installSelectedPackageItems(admin=True))
        self.ToolbarSkipHash = QAction("", toolbar)# ("Skip hash check")
        self.ToolbarSkipHash.triggered.connect(lambda: self.installSelectedPackageItems(skiphash=True))
        self.ToolbarInteractive = QAction("", toolbar)# ("Interactive update")
        self.ToolbarInteractive.triggered.connect(lambda: self.installSelectedPackageItems(interactive=True))
        self.ToolbarShare = QAction("", toolbar)
        self.ToolbarShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))


        for action in [self.ToolbarRunAsAdmin, self.ToolbarSkipHash, self.ToolbarInteractive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        for action in [self.ToolbarShowInfo, self.ToolbarShare]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        self.ToolbarSelectNone = QAction("", toolbar)
        self.ToolbarSelectNone.triggered.connect(lambda: self.setAllPackagesSelected(False))
        toolbar.addAction(self.ToolbarSelectNone)
        toolbar.widgetForAction(self.ToolbarSelectNone).setFixedSize(40, 45)

        toolbar.addSeparator()

        self.ToolbarImportPackages = QAction(_("Import packages from a file"), toolbar)
        self.ToolbarImportPackages.triggered.connect(lambda: self.importPackages())
        toolbar.addAction(self.ToolbarImportPackages)

        self.ToolbarExportPackages = QAction(_("Export selected packages to a file"), toolbar)
        self.ToolbarExportPackages.triggered.connect(lambda: self.exportSelectedPackages())
        toolbar.addAction(self.ToolbarExportPackages)


        tooltips = {
            self.ToolbarInstall: _("Install selected packages"),
            self.ToolbarShowInfo: _("Show package details"),
            self.ToolbarRunAsAdmin: _("Install selected packages with administrator privileges"),
            self.ToolbarSkipHash: _("Skip the hash check when installing the selected packages"),
            self.ToolbarInteractive: _("Do an interactive install for the selected packages"),
            self.ToolbarShare: _("Share this package"),
            self.ToolbarSelectNone: _("Clear selection"),
            self.ToolbarImportPackages: _("Install packages from a file"),
            self.ToolbarExportPackages: _("Export selected packages to a file")
        }

        for action in tooltips.keys():
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])

        
        toolbar.addSeparator()
        
        self.HelpMenuEntry1 = QAction("Guide for beginners on how to install a package")
        self.HelpMenuEntry1.triggered.connect(lambda: os.startfile("https://marticliment.com/wingetui/help/install-a-program"))
        self.HelpMenuEntry2 = QAction("Discover Packages overview - every feature explained")
        self.HelpMenuEntry2.triggered.connect(lambda: os.startfile("https://marticliment.com/wingetui/help/discover-overview"))
        self.HelpMenuEntry3 = QAction("WingetUI Help and Documentation")
        self.HelpMenuEntry3.triggered.connect(lambda: os.startfile("https://marticliment.com/wingetui/help"))

        def showHelpMenu():
            helpMenu = QMenu(self)
            helpMenu.addAction(self.HelpMenuEntry1)
            helpMenu.addAction(self.HelpMenuEntry2)
            helpMenu.addSeparator()
            helpMenu.addAction(self.HelpMenuEntry3)
            ApplyMenuBlur(helpMenu.winId().__int__(), self.contextMenu)
            helpMenu.exec(QCursor.pos())
    
        self.ToolbarHelp = QAction(_("Help"), toolbar)
        self.ToolbarHelp.triggered.connect(showHelpMenu)
        toolbar.addAction(self.ToolbarHelp)

        toolbar.addWidget(TenPxSpacer())
        toolbar.addWidget(TenPxSpacer())
        

        return toolbar

    def loadShared(self, argument: str, second_round: bool = False):
        print(argument)
        if "#" in argument:
            id = argument.split("#")[0]
            store = argument.split("#")[1]
        else:
            id = argument
            store = "Unknown"
        packageFound = False
        for package in self.PackageItemReference.keys():
            package: Package
            if package.Id == id and (package.Source == store or store=="Unknown"):
                self.infobox: PackageInfoPopupWindow
                self.infobox.showPackageDetails(package)
                self.infobox.show()
                self.packageList.setEnabled(True)
                packageFound = True
                break
        if packageFound:
            pass
        elif not second_round:
            self.query.setText(id)
            self.finishFiltering(self.query.text())
            self.packageList.setEnabled(False)
            Thread(target=self.loadSharedId, args=(argument,), daemon=True).start()
        else:
            self.packageList.setEnabled(True)
            self.err = CustomMessageBox(self.window())
            errorData = {
                    "titlebarTitle": _("Unable to find package"),
                    "mainTitle": _("Unable to find package"),
                    "mainText": _("We could not load detailed information about this package, because it was not found in any of your package sources"),
                    "buttonTitle": _("Ok"),
                    "errorDetails": _("This is probably due to the fact that the package you were sent was removed, or published on a package manager that you don't have enabled. The received ID is {0}").format(argument),
                    "icon": QIcon(getMedia("notif_warn")),
                }
            self.err.showErrorMessage(errorData, showNotification=False)

    def loadSharedId(self, argument: str):
        while self.isLoadingDynamicPackages():
            time.sleep(0.1)
        self.callInMain.emit(lambda: self.loadShared(argument, second_round=True))

    def installSelectedPackageItems(self, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        for package in self.packageItems:
            try:
                if package.checkState(0) ==  Qt.CheckState.Checked:
                    self.installPackageItem(package, admin, interactive, skiphash)
            except AttributeError:
                pass

    def importPackages(self):
        self.importer = PackageImporter(self)

    def filter(self) -> None:
        print(f"🟢 Searching for string \"{self.query.text()}\"")

        def waitAndFilter(query: str):
            time.sleep(0.1)
            if query == self.query.text():
                self.callInMain.emit(partial(self.finishFiltering, query))

        Thread(target=lambda: waitAndFilter(self.query.text())).start()

    def finishFiltering(self, text: str) -> None:
        if len(text) >= 2 or getSettings("AlwaysListPackages"):
            if text != self.LastQueryDynamicallyLoaded:
                self.LastQueryDynamicallyLoaded = text
                self.startLoadingDyamicPackages(text)
            super().finishFiltering(text)
            if len(self.showableItems) == 0 and self.isLoadingDynamicPackages():
                self.packageList.label.setText(_("Looking for packages..."))
        elif len(text) == 0:
            self.showableItems = []
            for item in self.packageItems:
                try:
                    if item.checkState(0) == Qt.CheckState.Checked:
                        self.showableItems.append(item)
                except RuntimeError:
                    print("🟠 RuntimeError on DiscoverSoftwareSection.finishFiltering")
            self.addItemsToTreeWidget(reset = True)
            self.packageList.scrollToItem(self.packageList.currentItem())

            if len(self.showableItems) == 0:
                self.addItemsToTreeWidget(reset=True)
                self.loadingProgressBar.hide()
                self.packageList.label.show()
                self.packageList.label.setText(_("Search for packages to start"))
        else:
            self.showableItems = []
            self.addItemsToTreeWidget(reset=True)
            self.loadingProgressBar.hide()
            self.packageList.label.show()
            self.packageList.label.setText(_("Please type at least two characters"))

    def finishLoadingIfNeeded(self) -> None:
        itemCount = len(self.packageItems)
        self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(itemCount)))
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.query.setEnabled(True)
        self.finishFiltering(self.query.text())

        for manager in self.PackageManagers: # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager] and manager.isEnabled():
                return

        self.reloadButton.setEnabled(True)
        self.loadingProgressBar.hide()
        self.countLabel.setText(_("Found packages: {0}").format(str(itemCount)))
        print("🟢 Total packages: "+str(itemCount))

    def finishDynamicLoadingIfNeeded(self) -> None:
        self.finishFiltering(self.query.text())
        if len(self.showableItems) == 0 and len(self.query.text())>=3:
            self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            self.packageList.label.setText(_(""))

        if not self.isLoadingDynamicPackages():
            self.loadingProgressBar.hide()
        
    def isLoadingDynamicPackages(self) -> bool:
        return self.runningThreads > 0

    def addItem(self, package: Package) -> None:
        if not "---" in package.Name and not package.Name in ("+", "Scoop", "At", "The", "But", "Au") and not version in ("the", "is"):
            
            item = TreeWidgetItemWithQAction(self)
            item.setCheckState(0, Qt.CheckState.Unchecked)
            item.setText(1, package.Name)
            item.setIcon(1, self.installIcon)
            item.setText(2, package.Id)
            item.setIcon(2, self.IDIcon)
            item.setText(3, package.Version)
            item.setIcon(3, self.versionIcon)
            item.setText(4, package.Source)
            item.setIcon(4, package.getSourceIcon())
            item.setText(6, package.getFloatVersion())
            
            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)
                
            UNINSTALL: UninstallSoftwareSection = globals.uninstall
            if package.Id in UNINSTALL.IdPackageReference.keys():
                installedPackage: UpgradablePackage = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems:
                    item.setIcon(1, self.installedIcon)
                    item.setToolTip(1, _("This package is already installed")+" - "+package.Name)

    def installPackageItem(self, item: TreeWidgetItemWithQAction, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        """
        Initialize the install procedure for the given package item, passed as a TreeWidgetItemWithQAction. Switches: admin, interactive, skiphash
        """
        package: Package = self.ItemPackageReference[item]
        options = InstallationOptions()
        options.RunAsAdministrator = admin
        options.InteractiveInstallation = interactive
        options.SkipHashCheck = skiphash
        self.addInstallation(PackageInstallerWidget(package, options))
        
    def installPackage(self, package: Package, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        """
        Initialize the install procedure for the given package, passed as a TreeWidgetItemWithQAction. Switches: admin, interactive, skiphash
        """
        options = InstallationOptions()
        options.RunAsAdministrator = admin
        options.InteractiveInstallation = interactive
        options.SkipHashCheck = skiphash
        self.addInstallation(PackageInstallerWidget(package, options))

    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        packages = manager.getAvailablePackages()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def loadDynamicPackages(self, query: str, manager: PackageClasses.DynamicPackageManager) -> None:
        self.runningThreads += 1
        packages = manager.getPackagesForQuery(query)
        for package in packages:
            if package.Id not in self.IdPackageReference:
                self.addProgram.emit(package)
            elif package.Source != self.IdPackageReference[package.Id].Source:
                self.addProgram.emit(package)
        self.DynamicPackagesLoaded[manager] = True
        self.runningThreads -= 1
        self.finishDynamicLoading.emit()

    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for packages..."))
        text = self.query.text()
        super().startLoadingPackages(force)
        if text != "":
            self.query.setText(text)
            self.startLoadingDyamicPackages(text, force=True)

    def startLoadingDyamicPackages(self, query: str, force: bool = False) -> None:
        print(f"🔵 Loading dynamic packages for query {query}")
        for manager in self.DynaimcPackageManagers:
            self.DynamicPackagesLoaded[manager] = False
        self.loadingProgressBar.show()

        for manager in self.DynaimcPackageManagers:
            if manager.isEnabled():
                Thread(target=self.loadDynamicPackages, args=(query, manager), daemon=True, name=f"{manager.NAME} dyamic packages loader").start()
            else:
                self.PackagesLoaded[manager] = True

        self.finishDynamicLoadingIfNeeded()
        
    def resizeEvent(self, event: QResizeEvent) -> None:
        if self.ArrowLabel.isVisible():
            self.ArrowLabel.move(self.query.x() + self.query.width()//2 - self.ArrowLabel.width() + 80, self.query.y()+self.query.height())
        return super().resizeEvent(event)
    
    def moveEvent(self, event: QMoveEvent) -> None:
        if self.ArrowLabel.isVisible():
            self.ArrowLabel.move(self.query.x() + self.query.width()//2 - self.ArrowLabel.width() + 80, self.query.y()+self.query.height())
        return super().moveEvent(event)

class UpdateSoftwareSection(SoftwareSection):

    PackageManagers = PackageManagersList.copy()
    PackagesLoaded = PackagesLoadedDict.copy()
    addProgram = Signal(object)
    availableUpdates: int = 0
    PackageItemReference: dict[UpgradablePackage:TreeWidgetItemWithQAction] = {}
    ItemPackageReference: dict[TreeWidgetItemWithQAction:UpgradablePackage] = {}
    IdPackageReference: dict[str:UpgradablePackage] = {}
    UpdatesNotification: ToastNotification = None

    def __init__(self, parent = None):
        super().__init__(parent = parent)


        self.blacklistManager = IgnoredUpdatesManager(self.window())
        self.LegacyBlacklist = getSettingsValue("BlacklistedUpdates")

        self.query.setPlaceholderText(" "+_("Search on available updates"))
        self.SectionImage.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
        self.discoverLabel.setText(_("Software Updates"))

        self.packageList.setHeaderLabels(["", _("Package Name"), _("Package ID"), _("Installed Version"), _("New Version"), _("Source")])
        self.packageList.setSortingEnabled(True)
        self.packageList.sortByColumn(1, Qt.SortOrder.AscendingOrder)

        self.packageList.itemDoubleClicked.connect(lambda item, column: (self.updatePackageItem(item) if not getSettings("DoNotUpdateOnDoubleClick") else self.openInfo(item, update=True)))

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        self.packageList.setColumnCount(7)
        self.packageList.setColumnHidden(6, True)
        header.setSectionResizeMode(0, QHeaderView.Fixed)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        header.setSectionResizeMode(4, QHeaderView.Fixed)
        header.setSectionResizeMode(5, QHeaderView.Fixed)
        self.packageList.setColumnWidth(0, 10)
        self.packageList.setColumnWidth(3, 130)
        self.packageList.setColumnWidth(4, 130)
        self.packageList.setColumnWidth(5, 150)

        self.countLabel.setText(_("Checking for updates..."))
        self.packageList.label.setText(self.countLabel.text())


        self.contextMenu = QMenu(self)
        self.contextMenu.setParent(self)
        self.contextMenu.setStyleSheet("* {background: red;color: black}")
        self.MenuDetails = QAction(_("Package details"))
        self.MenuDetails.triggered.connect(lambda: self.openInfo(self.packageList.currentItem(), update=True))
        self.MenuInstall = QAction(_("Update"))
        self.MenuInstall.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem()))
        self.MenuAdministrator = QAction(_("Update as administrator"))
        self.MenuAdministrator.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), admin=True))
        self.MenuSkipHash = QAction(_("Skip hash check"))
        self.MenuSkipHash.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), skiphash=True))
        self.MenuInteractive = QAction(_("Interactive update"))
        self.MenuInteractive.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), interactive=True))
        
        def uninstallPackage():
            UNINSTALL_SECTION: UninstallSoftwareSection = globals.uninstall
            if self.packageList.currentItem():
                id = self.ItemPackageReference[self.packageList.currentItem()].Id
            UNINSTALL_SECTION.uninstallPackageItem(UNINSTALL_SECTION.IdPackageReference[id].PackageItem)
        
        self.MenuUninstall = QAction(_("Uninstall package"))
        self.MenuUninstall.triggered.connect(lambda: uninstallPackage())
        
        def ignoreUpdates(item: TreeWidgetItemWithQAction):
            IgnorePackageUpdates_Permanent(item.text(2), item.text(5))
            item.setHidden(True)
            self.packageItems.remove(item)
            self.showableItems.remove(item)
            self.packageList.takeTopLevelItem(self.packageList.indexOfTopLevelItem(item))
            self.updatePackageNumber()
            INSTALLED: UninstallSoftwareSection = globals.uninstall
            INSTALLED.showBlacklistedIcon(INSTALLED.IdPackageReference[item.text(2)].PackageItem)
        
        self.MenuIgnoreUpdates = QAction(_("Ignore updates for this package"))
        self.MenuIgnoreUpdates.triggered.connect(lambda: ignoreUpdates(self.packageList.currentItem()))
        
        self.MenuSkipVersion = QAction(_("Skip this version"))
        self.MenuSkipVersion.triggered.connect(lambda: (IgnorePackageUpdates_SpecificVersion(self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), self.packageList.currentItem().text(5)), self.packageList.currentItem().setHidden(True), self.packageItems.remove(self.packageList.currentItem()), self.showableItems.remove(self.packageList.currentItem()), self.packageList.takeTopLevelItem(self.packageList.indexOfTopLevelItem(self.packageList.currentItem())), self.updatePackageNumber()))

        self.MenuShare = QAction(_("Share this package"))
        self.MenuShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))
        
        self.installIcon = QIcon(getMedia("install"))
        self.updateIcon = QIcon(getMedia("update"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.newVersionIcon = QIcon(getMedia("newversion"))

        self.contextMenu.addAction(self.MenuInstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuAdministrator)
        self.contextMenu.addAction(self.MenuInteractive)
        self.contextMenu.addAction(self.MenuSkipHash)
        self.contextMenu.addAction(self.MenuUninstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuIgnoreUpdates)
        self.contextMenu.addAction(self.MenuSkipVersion)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuShare)
        self.contextMenu.addAction(self.MenuDetails)

        self.finishInitialisation()
        
    def ApplyIcons(self):
        super().ApplyIcons()
        self.installIcon = QIcon(getMedia("install"))
        self.updateIcon = QIcon(getMedia("update"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.newVersionIcon = QIcon(getMedia("newversion"))
        
        self.MenuInteractive.setIcon(QIcon(getMedia("interactive")))
        self.MenuSkipHash.setIcon(QIcon(getMedia("checksum")))
        self.MenuAdministrator.setIcon(QIcon(getMedia("runasadmin")))
        self.MenuInstall.setIcon(QIcon(getMedia("menu_updates")))
        self.MenuDetails.setIcon(QIcon(getMedia("info")))
        self.MenuShare.setIcon(QIcon(getMedia("share")))
        self.MenuSkipVersion.setIcon(QIcon(getMedia("skip")))
        self.MenuIgnoreUpdates.setIcon(QIcon(getMedia("pin")))
        self.MenuUninstall.setIcon(QIcon(getMedia("menu_uninstall")))
        
        self.ToolbarInstall.setIcon(QIcon(getMedia("menu_updates")))
        self.ToolbarShowInfo.setIcon(QIcon(getMedia("info")))
        self.ToolbarRunAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        self.ToolbarSkipHash.setIcon(QIcon(getMedia("checksum")))
        self.ToolbarInteractive.setIcon(QIcon(getMedia("interactive")))
        self.ToolbarShare.setIcon(QIcon(getMedia("share")))
        self.ToolbarSelectNone.setIcon(QIcon(getMedia("selectnone")))
        self.ToolbarHelp.setIcon(QIcon(getMedia("help")))
        self.ToolbarSelectAll.setIcon(QIcon(getMedia("selectall")))
        self.ToolbarIgnoreSelected.setIcon(QIcon(getMedia("pin")))
        self.ToolbarManageBlacklist.setIcon(QIcon(getMedia("blacklist")))
        
        self.HelpMenuEntry1.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry2.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry3.setIcon(QIcon(getMedia("launch")))
        
        for item in self.packageItems:
            package: UpgradablePackage = self.ItemPackageReference[item]
            
            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, self.newVersionIcon)
            item.setIcon(5, package.getSourceIcon())
            
            UNINSTALL: UninstallSoftwareSection = globals.uninstall
            if package.Id in UNINSTALL.IdPackageReference.keys():
                installedPackage: UpgradablePackage = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems:
                    installedItem.setIcon(1, self.updateIcon)


    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return

        try:
            Capabilities: PackageManagerCapabilities =  self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuSkipHash.setVisible(Capabilities.CanSkipIntegrityChecks)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        pos.setY(pos.y()+35)
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)

        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:

        def blacklistSelectedPackages():
            for program in self.packageItems:
                if not program.isHidden():
                    try:
                        if program.checkState(0) ==  Qt.CheckState.Checked:
                            IgnorePackageUpdates_Permanent(program.text(2), program.text(5))
                            program.setHidden(True)
                            self.packageItems.remove(program)
                            if program in self.showableItems:
                                self.showableItems.remove(program)
                            self.packageList.takeTopLevelItem(self.packageList.indexOfTopLevelItem(program))
                            INSTALLED: UninstallSoftwareSection = globals.uninstall
                            if program.text(2) in INSTALLED.IdPackageReference:
                                INSTALLED.showBlacklistedIcon(INSTALLED.IdPackageReference[program.text(2)].PackageItem)
                    except AttributeError:
                        pass
            self.updatePackageNumber()

        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.ToolbarInstall = QAction(_("Update selected packages"), toolbar)
        self.ToolbarInstall.triggered.connect(lambda: self.updateSelectedPackageItems())
        toolbar.addAction(self.ToolbarInstall)

        self.ToolbarShowInfo = QAction("", toolbar)
        self.ToolbarShowInfo.triggered.connect(lambda: self.openInfo(self.packageList.currentItem(), update=True))
        self.ToolbarRunAsAdmin = QAction("", toolbar)
        self.ToolbarRunAsAdmin.triggered.connect(lambda: self.updateSelectedPackageItems(admin=True))
        self.ToolbarSkipHash = QAction("", toolbar)
        self.ToolbarSkipHash.triggered.connect(lambda: self.updateSelectedPackageItems(skiphash=True))
        self.ToolbarInteractive = QAction("", toolbar)
        self.ToolbarInteractive.triggered.connect(lambda: self.updateSelectedPackageItems(interactive=True))
        self.ToolbarShare = QAction("", toolbar)
        self.ToolbarShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        for action in [self.ToolbarRunAsAdmin, self.ToolbarSkipHash, self.ToolbarInteractive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        for action in [self.ToolbarShowInfo, self.ToolbarShare]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        self.upgradeAllAction = QAction("", toolbar)
        self.upgradeAllAction.triggered.connect(lambda: self.updateAllPackageItems())
        # self.upgradeAllAction is Required for the systray context menu. DO NOT TOUCH!

        self.ToolbarSelectAll = QAction("", toolbar)
        self.ToolbarSelectAll.triggered.connect(lambda: self.setAllPackagesSelected(True))
        toolbar.addAction(self.ToolbarSelectAll)
        toolbar.widgetForAction(self.ToolbarSelectAll).setFixedSize(40, 45)
        self.ToolbarSelectNone = QAction("", toolbar)
        self.ToolbarSelectNone.triggered.connect(lambda: self.setAllPackagesSelected(False))
        toolbar.addAction(self.ToolbarSelectNone)
        toolbar.widgetForAction(self.ToolbarSelectNone).setFixedSize(40, 45)
        toolbar.widgetForAction(self.ToolbarSelectNone).setToolTip(_("Clear selection"))
        toolbar.widgetForAction(self.ToolbarSelectAll).setToolTip(_("Select all"))

        toolbar.addSeparator()

        self.ToolbarIgnoreSelected = QAction(_("Ignore selected packages"), toolbar)
        self.ToolbarIgnoreSelected.triggered.connect(lambda: blacklistSelectedPackages())
        toolbar.addAction(self.ToolbarIgnoreSelected)
        self.ToolbarManageBlacklist = QAction(_("Manage ignored updates"), toolbar)
        self.ToolbarManageBlacklist.triggered.connect(lambda: (self.blacklistManager.show()))
        toolbar.addAction(self.ToolbarManageBlacklist)


        tooltips = {
            self.ToolbarInstall: _("Update selected packages"),
            self.ToolbarShowInfo: _("Show package details"),
            self.ToolbarRunAsAdmin: _("Update selected packages with administrator privileges"),
            self.ToolbarSkipHash: _("Skip the hash check when updating the selected packages"),
            self.ToolbarInteractive: _("Do an interactive update for the selected packages"),
            self.ToolbarShare: _("Share this package"),
            self.ToolbarSelectAll: _("Select all packages"),
            self.ToolbarSelectNone: _("Clear selection"),
            self.ToolbarManageBlacklist: _("Manage ignored packages"),
            self.ToolbarIgnoreSelected: _("Ignore updates for the selected packages")
        }

        for action in tooltips.keys():
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            
        toolbar.addSeparator()
        
        self.HelpMenuEntry1 = QAction("")
        self.HelpMenuEntry1.triggered.connect(lambda: os.startfile(""))
        self.HelpMenuEntry2 = QAction("Software Updates overview - every feature explained")
        self.HelpMenuEntry2.triggered.connect(lambda: os.startfile("https://marticliment.com/wingetui/help/updates-overview"))
        self.HelpMenuEntry3 = QAction("WingetUI Help and Documentation")
        self.HelpMenuEntry3.triggered.connect(lambda: os.startfile("https://marticliment.com/wingetui/help"))
        
        def showHelpMenu():
            helpMenu = QMenu(self)
            #helpMenu.addAction(self.HelpMenuEntry1)
            helpMenu.addAction(self.HelpMenuEntry2)
            helpMenu.addSeparator()
            helpMenu.addAction(self.HelpMenuEntry3)
            ApplyMenuBlur(helpMenu.winId().__int__(), self.contextMenu)
            helpMenu.exec(QCursor.pos())
    
        self.ToolbarHelp = QAction(QIcon(getMedia("help")), _("Help"), toolbar)
        self.ToolbarHelp.triggered.connect(showHelpMenu)
        toolbar.addAction(self.ToolbarHelp)

        return toolbar

    def finishLoadingIfNeeded(self) -> None:
        self.countLabel.setText(_("Available updates: {0}, not finished yet...").format(str(len(self.packageItems))))
        globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}, not finished yet...").format(str(len(self.packageItems))))
        if len(self.packageItems) == 0:
            self.packageList.label.setText(self.countLabel.text())
        else:
            self.packageList.label.setText("")
        self.filter()
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.query.setEnabled(True)

        for manager in self.PackageManagers: # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return

        self.reloadButton.setEnabled(True)
        self.loadingProgressBar.hide()
        self.loadingProgressBar.hide()
        globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}").format(str(len(self.packageItems))))
        count = 0
        lastVisibleItem = None
        for item in self.packageItems:
            if not item.isHidden():
                count += 1
                lastVisibleItem = item
        if count > 0:
            globals.tray_is_available_updates = True
            update_tray_icon()
            try:
                self.UpdatesNotification.close()
            except AttributeError:
                pass
            except Exception as e:
                report(e)
            if getSettings("AutomaticallyUpdatePackages") or "--updateapps" in sys.argv:
                self.updateAllPackageItems()
                self.UpdatesNotification = ToastNotification(self, self.callInMain.emit)
                if count > 1:
                    self.UpdatesNotification.setTitle(_("Updates found!"))
                    self.UpdatesNotification.setDescription(_("{0} packages are being updated").format(count)+":")
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1)+", "
                    self.UpdatesNotification.setSmallText(packageList[:-2])
                elif count == 1:
                    self.UpdatesNotification.setTitle(_("Update found!"))
                    self.UpdatesNotification.setDescription(_("{0} is being updated").format(lastVisibleItem.text(1)))
                self.UpdatesNotification.addOnClickCallback(lambda: (globals.mainWindow.showWindow(1)))
                if globals.ENABLE_UPDATES_NOTIFICATIONS:
                    self.UpdatesNotification.show()

            else:
                self.UpdatesNotification = ToastNotification(self, self.callInMain.emit)
                if count > 1:
                    self.UpdatesNotification.setTitle(_("Updates found!"))
                    self.UpdatesNotification.setDescription(_("{0} packages can be updated").format(count)+":")
                    self.UpdatesNotification.addAction(_("Update all"), self.updateAllPackageItems)
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1)+", "
                    self.UpdatesNotification.setSmallText(packageList[:-2])
                elif count == 1:
                    self.UpdatesNotification.setTitle(_("Update found!"))
                    self.UpdatesNotification.setDescription(_("{0} can be updated").format(lastVisibleItem.text(1)))
                    self.UpdatesNotification.addAction(_("Update"), self.updateAllPackageItems)
                self.UpdatesNotification.addAction(_("Show WingetUI"), lambda: (globals.mainWindow.showWindow(1)))
                self.UpdatesNotification.addOnClickCallback(lambda: (globals.mainWindow.showWindow(1)))
                if globals.ENABLE_UPDATES_NOTIFICATIONS:
                    self.UpdatesNotification.show()

            self.packageList.label.setText("")
        else:
            globals.tray_is_available_updates = False
            update_tray_icon()
        self.updatePackageNumber()
        self.filter()
        if not getSettings("DisableAutoCheckforUpdates"):
            try:
                waitTime = int(getSettingsValue("UpdatesCheckInterval"))
            except ValueError:
                print(f"🟡 Can't get custom interval time! (got value was '{getSettingsValue('UpdatesCheckInterval')}')")
                waitTime = 3600
            Thread(target=lambda: (time.sleep(waitTime), self.reloadSources()), daemon=True, name="AutoCheckForUpdates Thread").start()
        print("🟢 Total packages: "+str(len(self.packageItems)))

    def changeStore(self, package: UpgradablePackage):
        time.sleep(3)
        try:
            UNINSTALL_SECTION: UninstallSoftwareSection = globals.uninstall
            package.Source = UNINSTALL_SECTION.IdPackageReference[package.Id].Source
        except KeyError as e:
            print(f"🟠 Package {package.Id} found in the updates section but not in the installed one, happened again")
        self.callInMain.emit(partial(package.PackageItem.setText, 5, package.Source))

    def addItem(self, package: UpgradablePackage) -> None:
        if not "---" in package.Name and not "The following packages" in package.Name and not "Name  " in package.Name and not package.Name in ("+", "Scoop", "At", "The", "But", "Au") and not package.Version.lower() in ("the", "is", "install") and not package.NewVersion in ("Manifest", package.Version):
            if [package.Id, package.Source.lower().split(":")[0]] in GetIgnoredPackageUpdates_Permanent():
                print(f"🟡 Package {package.Id} is ignored")
                return
            if [package.Id, package.NewVersion.lower().replace(",", "."), package.Source.lower().lower().split(":")[0]] in GetIgnoredPackageUpdates_SpecificVersion():
                print(f"🟡 Package {package.Id} version {package.Version} is ignored")
                return
            if package.Id in self.LegacyBlacklist:
                print(f"🟠 Package {package.Id} is legacy blacklisted")
                return
            item = TreeWidgetItemWithQAction()
            item.setCheckState(0, Qt.CheckState.Checked)
            item.setText(1, package.Name)
            item.setIcon(1, self.installIcon)
            item.setText(2, package.Id)
            item.setIcon(2, self.IDIcon)
            item.setText(3, package.Version if package.Version != "Unknown" else _("Unknown"))
            item.setIcon(3, self.versionIcon)
            item.setText(4, package.NewVersion)
            item.setIcon(4, self.newVersionIcon)
            item.setText(6, package.getFloatVersion())
            package.PackageItem = item
            if package.isManager(Scoop):
                try:
                    UNINSTALL_SECTION: UninstallSoftwareSection = globals.uninstall
                    if package.Version == UNINSTALL_SECTION.IdPackageReference[package.Id].Version:
                        package.Source = UNINSTALL_SECTION.IdPackageReference[package.Id].Source
                    item.setText(5, package.Source)
                except KeyError as e:
                    item.setText(5, _("Loading..."))
                    print(f"🟡 Package {package.Id} found in the updates section but not in the installed one, might be a temporal issue, retrying in 3 seconds...")
                    Thread(target=self.changeStore, args=(package,)).start()
            else:
                item.setText(5, package.Source)
            item.setIcon(5, package.getSourceIcon())
            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)
            action = QAction(package.Name+"  \t"+package.Version+"\t → \t"+package.NewVersion, globals.trayMenuUpdatesList)
            action.triggered.connect(lambda : self.updatePackageItem(item))
            action.setShortcut(package.Version)
            item.setAction(action)
            globals.trayMenuUpdatesList.addAction(action)
            
            UNINSTALL: UninstallSoftwareSection = globals.uninstall
            if package.Id in UNINSTALL.IdPackageReference.keys():
                installedPackage: UpgradablePackage = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems:
                    installedItem.setIcon(1, self.updateIcon)
                    installedItem.setToolTip(1, _("This package can be updated to version {0}").format(package.NewVersion)+" - "+package.Name)
            
    def finishFiltering(self, text: str):
        def getChecked(item: TreeWidgetItemWithQAction) -> str:
            return " " if item.checkState(0) == Qt.CheckState.Checked else ""
        def getTitle(item: TreeWidgetItemWithQAction) -> str:
            return item.text(1)
        def getID(item: TreeWidgetItemWithQAction) -> str:
            return item.text(2)
        def getVersion(item: TreeWidgetItemWithQAction) -> str:
            return item.text(6)
        def getNewVersion(item: TreeWidgetItemWithQAction) -> str:
            return item.text(4)
        def getSource(item: TreeWidgetItemWithQAction) -> str:
            return item.text(5)

        if self.query.text() != text:
            return
        self.showableItems = []
        found = 0

        sortColumn = self.packageList.sortColumn()
        descendingSort = self.packageList.header().sortIndicatorOrder() == Qt.SortOrder.DescendingOrder
        match sortColumn:
            case 0:
                self.packageItems.sort(key=getChecked, reverse=descendingSort)
            case 1:
                self.packageItems.sort(key=getTitle, reverse=descendingSort)
            case 2:
                self.packageItems.sort(key=getID, reverse=descendingSort)
            case 3:
                self.packageItems.sort(key=getVersion, reverse=descendingSort)
            case 4:
                self.packageItems.sort(key=getNewVersion, reverse=descendingSort)
            case 5:
                self.packageItems.sort(key=getSource, reverse=descendingSort)

        for item in self.packageItems:
            try:
                if self.containsQuery(item, text.replace("-", "").replace(" ", "").lower()):
                    self.showableItems.append(item)
                    found += 1
            except RuntimeError:
                print("nullitem")
        if found == 0:
            if self.packageList.label.text() == "":
                self.packageList.label.show()
                self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            if self.packageList.label.text() == _("No packages found matching the input criteria"):
                self.packageList.label.hide()
                self.packageList.label.setText("")
        self.addItemsToTreeWidget(reset = True)
        self.packageList.scrollToItem(self.packageList.currentItem())

    def updatePackageNumber(self, showQueried: bool = False, foundResults: int = 0):
        self.availableUpdates = 0
        for item in self.packageItems:
            if not item.isHidden():
                self.availableUpdates += 1
        self.countLabel.setText(_("Available updates: {0}").format(self.availableUpdates))
        trayMenuText = ""
        if self.availableUpdates > 0:
            trayMenuText = _("Available updates: {0}").format(self.availableUpdates)
            self.packageList.label.hide()
            self.packageList.label.setText("")
            self.SectionImage.setPixmap(QIcon(getMedia("alert_laptop")).pixmap(QSize(64, 64)))
            globals.updatesAction.setIcon(QIcon(getMedia("alert_laptop")))
            globals.app.uaAction.setEnabled(True)
            globals.trayMenuUpdatesList.menuAction().setEnabled(True)
            globals.tray_is_available_updates = True
        else:
            trayMenuText = _("No updates are available")
            self.packageList.label.setText(_("Hooray! No updates were found!"))
            self.packageList.label.show()
            globals.app.uaAction.setEnabled(False)
            globals.trayMenuUpdatesList.menuAction().setEnabled(False)
            globals.updatesAction.setIcon(QIcon(getMedia("checked_laptop")))
            self.SectionImage.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
            globals.tray_is_available_updates = False
        globals.trayMenuUpdatesList.menuAction().setText(trayMenuText)
        update_tray_icon()

    def updateAllPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden():
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updateSelectedPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden() and item.checkState(0) ==  Qt.CheckState.Checked:
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updatePackageItem(self, item: TreeWidgetItemWithQAction, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        package: Package = self.ItemPackageReference[item]
        options = InstallationOptions()
        options.RunAsAdministrator = admin
        options.InteractiveInstallation = interactive
        options.SkipHashCheck = skiphash
        self.addInstallation(PackageUpdaterWidget(package, options))

    def reloadSources(self, asyncroutine: bool = False):
        print("🔵 Reloading sources...")
        try:
            for manager in PackageManagersList:
                manager.updateSources()
        except Exception as e:
            report(e)
        if not asyncroutine:
            self.callInMain.emit(self.startLoadingPackages)

    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        t = Thread(target=lambda: self.reloadSources(asyncroutine = True), daemon=True)
        t.start()
        t0 = int(time.time())
        while t.is_alive() and (int(time.time())-t0 < 10): # Timeout of 10 seconds for the reloadSources function
            time.sleep(0.2)
        packages = manager.getAvailableUpdates()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for updates..."))
        self.packageList.label.setText(self.countLabel.text())
        for action in globals.trayMenuUpdatesList.actions():
            globals.trayMenuUpdatesList.removeAction(action)
        globals.trayMenuUpdatesList.addAction(globals.updatesHeader)
        return super().startLoadingPackages(force)
    
    def sharePackage(self, package: TreeWidgetItemWithQAction):
        url = f"https://marticliment.com/wingetui/share?pid={package.text(2)}^&pname={package.text(1)}^&psource={package.text(5)}"
        nativeWindowsShare(package.text(2), url, self.window())


class UninstallSoftwareSection(SoftwareSection):
    allPkgSelected: bool = False
    PackageManagers = PackageManagersList.copy()
    PackagesLoaded = PackagesLoadedDict.copy()
    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.query.setPlaceholderText(" "+_("Search on your software"))
        self.SectionImage.setPixmap(QIcon(getMedia("workstation")).pixmap(QSize(64, 64)))
        self.discoverLabel.setText(_("Installed Packages"))

        self.headers = ["", _("Package Name"), _("Package ID"), _("Installed Version"), _("Source"), "", ""] # empty header added for checkbox
        self.packageList.setHeaderLabels(self.headers)
        self.packageList.setColumnCount(7)
        self.packageList.setColumnHidden(5, True)
        self.packageList.setColumnHidden(6, True)
        self.packageList.setSortingEnabled(True)
        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
        header.setSectionResizeMode(0, QHeaderView.Fixed)
        header.setSectionResizeMode(1, QHeaderView.Stretch)
        header.setSectionResizeMode(2, QHeaderView.Stretch)
        header.setSectionResizeMode(3, QHeaderView.Fixed)
        header.setSectionResizeMode(4, QHeaderView.Fixed)
        self.packageList.setColumnWidth(3, 150)
        self.packageList.setColumnWidth(4, 150)
        self.packageList.sortByColumn(1, Qt.SortOrder.AscendingOrder)
        self.countLabel.setText(_("Searching for installed packages..."))
        self.packageList.label.setText(self.countLabel.text())
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.uninstallPackageItem(item))

        self.installIcon = QIcon(getMedia("install"))
        self.pinnedIcon = QIcon(getMedia("pin_yellow"))
        self.updateIcon = QIcon(getMedia("update"))
        self.installedIcon = QIcon(getMedia("installed"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))

        self.contextMenu = QMenu(self)
        self.contextMenu.setParent(self)
        self.contextMenu.setStyleSheet("* {background: red;color: black}")
        self.MenuInstall = QAction(_("Uninstall"))
        self.MenuInstall.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem()))
        self.MenuAdministrator = QAction(_("Uninstall as administrator"))
        self.MenuAdministrator.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), admin=True))
        self.MenuRemovePermaData = QAction(_("Remove permanent data"))
        self.MenuRemovePermaData.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), removeData=True))
        self.MenuInteractive = QAction(_("Interactive uninstall"))
        self.MenuInteractive.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), interactive=True))
        self.MenuIgnoreUpdates = QAction(_("Ignore updates for this package"))
        self.MenuIgnoreUpdates.triggered.connect(lambda: (IgnorePackageUpdates_Permanent(self.packageList.currentItem().text(2), self.packageList.currentItem().text(4)), self.showBlacklistedIcon(self.packageList.currentItem())))
        self.MenuDetails = QAction(_("Package details"))
        self.MenuDetails.triggered.connect(lambda: self.openInfo(self.packageList.currentItem(), uninstall=True))
        self.MenuShare = QAction(_("Share this package"))
        self.MenuShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))
        self.contextMenu.addAction(self.MenuInstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuAdministrator)
        self.contextMenu.addAction(self.MenuRemovePermaData)
        self.contextMenu.addAction(self.MenuInteractive)
        self.contextMenu.addSeparator()
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuIgnoreUpdates)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuShare)
        self.contextMenu.addAction(self.MenuDetails)

        self.finishInitialisation()
        
    def ApplyIcons(self):
        super().ApplyIcons()
        self.installIcon = QIcon(getMedia("install"))
        self.pinnedIcon = QIcon(getMedia("pin_yellow"))
        self.updateIcon = QIcon(getMedia("update"))
        self.installedIcon = QIcon(getMedia("installed"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        
        self.MenuInteractive.setIcon(QIcon(getMedia("interactive")))
        self.MenuRemovePermaData.setIcon(QIcon(getMedia("menu_close")))
        self.MenuAdministrator.setIcon(QIcon(getMedia("runasadmin")))
        self.MenuInstall.setIcon(QIcon(getMedia("menu_uninstall")))
        self.MenuDetails.setIcon(QIcon(getMedia("info")))
        self.MenuShare.setIcon(QIcon(getMedia("share")))
        self.MenuIgnoreUpdates.setIcon(QIcon(getMedia("pin")))
        
        self.ToolbarInstall.setIcon(QIcon(getMedia("menu_uninstall")))
        self.ToolbarShowInfo.setIcon(QIcon(getMedia("info")))
        self.ToolbarRunAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        self.ToolbarInteractive.setIcon(QIcon(getMedia("interactive")))
        self.ToolbarShare.setIcon(QIcon(getMedia("share")))
        self.ToolbarSelectNone.setIcon(QIcon(getMedia("selectnone")))
        self.ToolbarHelp.setIcon(QIcon(getMedia("help")))
        self.ToolbarSelectAll.setIcon(QIcon(getMedia("selectall")))
        self.ToolbarIgnoreSelected.setIcon(QIcon(getMedia("pin")))
        self.ToolbarExport.setIcon(QIcon(getMedia("export")))
        
        self.HelpMenuEntry1.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry2.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry3.setIcon(QIcon(getMedia("launch")))
        
        for item in self.packageItems:
            package: UpgradablePackage = self.ItemPackageReference[item]
            
            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, package.getSourceIcon())
            
            if package.hasUpdatesIgnoredPermanently():
                item.setIcon(1, self.pinnedIcon)

            UPDATES: UpdateSoftwareSection = globals.updates
            if package.Id in UPDATES.IdPackageReference.keys():
                updatePackage: UpgradablePackage = UPDATES.IdPackageReference[package.Id]
                updateItem = updatePackage.PackageItem
                if updateItem in UPDATES.packageItems:
                    item.setIcon(1, self.updateIcon)
            
            DISCOVER: UninstallSoftwareSection = globals.discover
            if package.Id in DISCOVER.IdPackageReference.keys():
                discoverablePackage: UpgradablePackage = DISCOVER.IdPackageReference[package.Id]
                discoverableItem = discoverablePackage.PackageItem
                if discoverableItem in DISCOVER.packageItems:
                    discoverableItem.setIcon(1, self.installedIcon)

    def showBlacklistedIcon(self, packageItem: QTreeWidgetItem):
        packageItem.setIcon(1, self.pinnedIcon)
        packageItem.setToolTip(1, _("Updates for this package are ignored")+" - "+packageItem.text(1))

    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)

        try:
            Capabilities: PackageManagerCapabilities =  self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuRemovePermaData.setVisible(Capabilities.CanRemoveDataOnUninstall)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        if self.ItemPackageReference[self.packageList.currentItem()].Source not in ((_("Local PC"), "Microsoft Store", "Steam", "GOG", "Ubisoft Connect", _("Android Subsystem"))):
            self.MenuIgnoreUpdates.setVisible(True)
            self.MenuShare.setVisible(True)
            self.MenuDetails.setVisible(True)
        else:
            self.MenuIgnoreUpdates.setVisible(False)
            self.MenuShare.setVisible(False)
            self.MenuDetails.setVisible(False)

        pos.setY(pos.y()+35)

        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.ToolbarInstall = QAction(_("Uninstall selected packages"), toolbar)
        self.ToolbarInstall.triggered.connect(lambda: self.uninstallSelected())
        toolbar.addAction(self.ToolbarInstall)

        def blacklistSelectedPackages():
            for program in self.packageItems:
                if not program.isHidden():
                    try:
                        if program.checkState(0) ==  Qt.CheckState.Checked:
                            IgnorePackageUpdates_Permanent(program.text(2), program.text(4))
                            self.showBlacklistedIcon(program)
                    except AttributeError:
                        pass
            self.notif = InWindowNotification(self, _("The selected packages have been blacklisted"))
            self.notif.show()
            self.updatePackageNumber()

        def showInfo():
            item = self.packageList.currentItem()
            if item.text(4) in ((_("Local PC"), "Microsoft Store", "Steam", "GOG", "Ubisoft Connect")):
                self.err = CustomMessageBox(self.window())
                errorData = {
                        "titlebarTitle": _("Unable to load informarion"),
                        "mainTitle": _("Unable to load informarion"),
                        "mainText": _("We could not load detailed information about this package, because it was not installed from an available package manager."),
                        "buttonTitle": _("Ok"),
                        "errorDetails": _("Uninstallable packages with the origin listed as \"{0}\" are not published on any package manager, so there's no information available to show about them.").format(item.text(4)),
                        "icon": QIcon(getMedia("notif_warn")),
                    }
                self.err.showErrorMessage(errorData, showNotification=False)
            else:
                self.openInfo(item, uninstall=True)

        self.ToolbarShowInfo = QAction("", toolbar)# ("Show info")
        self.ToolbarShowInfo.triggered.connect(showInfo)
        self.ToolbarRunAsAdmin = QAction("", toolbar)# ("Run as administrator")
        self.ToolbarRunAsAdmin.triggered.connect(lambda: self.uninstallSelected(admin=True))
        self.ToolbarInteractive = QAction("", toolbar)# ("Interactive uninstall")
        self.ToolbarInteractive.triggered.connect(lambda: self.uninstallSelected(interactive=True))
        self.ToolbarShare = QAction("", toolbar)
        self.ToolbarShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        for action in [self.ToolbarRunAsAdmin, self.ToolbarInteractive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        for action in [self.ToolbarShowInfo, self.ToolbarShare]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        self.ToolbarSelectAll = QAction("", toolbar)
        self.ToolbarSelectAll.triggered.connect(lambda: self.setAllPackagesSelected(True))
        toolbar.addAction(self.ToolbarSelectAll)
        toolbar.widgetForAction(self.ToolbarSelectAll).setFixedSize(40, 45)
        self.ToolbarSelectNone = QAction("", toolbar)
        self.ToolbarSelectNone.triggered.connect(lambda: self.setAllPackagesSelected(False))
        toolbar.addAction(self.ToolbarSelectNone)
        toolbar.widgetForAction(self.ToolbarSelectNone).setFixedSize(40, 45)
        toolbar.widgetForAction(self.ToolbarSelectNone).setToolTip(_("Clear selection"))
        toolbar.widgetForAction(self.ToolbarSelectAll).setToolTip(_("Select all"))

        toolbar.addSeparator()
        
        self.ToolbarIgnoreSelected = QAction(QIcon(getMedia("pin")), _("Ignore selected packages"), toolbar)
        self.ToolbarIgnoreSelected.triggered.connect(lambda: blacklistSelectedPackages())
        toolbar.addAction(self.ToolbarIgnoreSelected)
        
        toolbar.addSeparator()

        self.ToolbarExport = QAction(QIcon(getMedia("export")), _("Export selected packages to a file"), toolbar)
        self.ToolbarExport.triggered.connect(lambda: self.exportSelectedPackages())
        toolbar.addAction(self.ToolbarExport)


        tooltips = {
            self.ToolbarInstall: _("Uninstall selected packages"),
            self.ToolbarShowInfo: _("Show package details"),
            self.ToolbarRunAsAdmin: _("Uninstall the selected packages with administrator privileges"),
            self.ToolbarInteractive: _("Do an interactive uninstall for the selected packages"),
            self.ToolbarShare: _("Share this package"),
            self.ToolbarIgnoreSelected: _("Ignore updates for the selected packages"),
            self.ToolbarSelectNone: _("Clear selection"),
            self.ToolbarSelectAll: _("Select all packages"),
            self.ToolbarExport: _("Export selected packages to a file")
        }

        for action in tooltips.keys():
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
        
        toolbar.addSeparator()
        
        self.HelpMenuEntry1 = QAction("")
        self.HelpMenuEntry1.triggered.connect(lambda: os.startfile(""))
        self.HelpMenuEntry2 = QAction("")
        self.HelpMenuEntry2.triggered.connect(lambda: os.startfile(""))
        self.HelpMenuEntry3 = QAction("")
        self.HelpMenuEntry3.triggered.connect(lambda: os.startfile(""))
        
        def showHelpMenu():
            helpMenu = QMenu(self)
            #helpMenu.addAction(self.HelpMenuEntry1)
            #helpMenu.addAction(self.HelpMenuEntry2)
            helpMenu.addSeparator()
            #helpMenu.addAction(self.HelpMenuEntry3)
            ApplyMenuBlur(helpMenu.winId().__int__(), self.contextMenu)
            helpMenu.exec(QCursor.pos())
    
        self.ToolbarHelp = QAction(QIcon(getMedia("help")), _("Help"), toolbar)
        self.ToolbarHelp.triggered.connect(showHelpMenu)
        toolbar.addAction(self.ToolbarHelp)
        
        w = QWidget()
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        toolbar.addWidget(w)
        toolbar.addWidget(TenPxSpacer())
        toolbar.addWidget(TenPxSpacer())

        return toolbar

    def uninstallSelected(self, admin: bool = False, interactive: bool = False) -> None:
        toUninstall = []
        for program in self.packageItems:
            if not program.isHidden():
                try:
                    if program.checkState(0) ==  Qt.CheckState.Checked:
                        toUninstall.append(program)
                except AttributeError:
                    pass
        a = CustomMessageBox(self)
        Thread(target=self.confirmUninstallSelected, args=(toUninstall, a, admin, interactive)).start()

    def updatePackageNumber(self, showQueried: bool = False, foundResults: int = 0):
        self.foundPackages = len(self.packageItems)
        self.countLabel.setText(_("{0} packages found").format(self.foundPackages))
        if self.foundPackages == 1:
            trayMenuText = _("1 package was found").format(self.foundPackages)
        else:
            trayMenuText = _("{0} packages were found").format(self.foundPackages)
        globals.trayMenuInstalledList.menuAction().setText(trayMenuText)
        if self.foundPackages > 0:
            self.packageList.label.hide()
            self.packageList.label.setText("")
        else:
            self.packageList.label.setText(_("{0} packages were found").format(0))
            self.packageList.label.show()

    def finishLoadingIfNeeded(self) -> None:
        self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(len(self.packageItems)))
        if len(self.packageItems) == 0:
            self.packageList.label.setText(self.countLabel.text())
        else:
            self.packageList.label.setText("")
        globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(len(self.packageItems)))
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.filter()
        self.query.setEnabled(True)

        for manager in self.PackageManagers: # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return

        self.reloadButton.setEnabled(True)
        self.filter()
        self.loadingProgressBar.hide()
        globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(len(self.packageItems)))
        self.countLabel.setText(_("Found packages: {0}").format(len(self.packageItems)))
        self.packageList.label.setText("")
        print("🟢 Total packages: "+str(len(self.packageItems)))

    def addItem(self, package: Package) -> None:
        if not "---" in package.Name and not package.Name in ("+", "Scoop", "At", "The", "But", "Au") and not package.Version in ("the", "is"):
            item = TreeWidgetItemWithQAction()
            item.setCheckState(0, Qt.CheckState.Unchecked)
            item.setText(1, package.Name)
            item.setIcon(1, self.installIcon)
            item.setText(2, package.Id)
            item.setIcon(2, self.IDIcon)
            item.setText(3, package.Version)
            item.setIcon(3, self.versionIcon)
            item.setText(4, package.Source)
            item.setIcon(4, package.getSourceIcon())
            item.setText(6, package.getFloatVersion())
            
            UPDATES: UpdateSoftwareSection = globals.updates
            if package.hasUpdatesIgnoredPermanently():
                item.setIcon(1, self.pinnedIcon)
                item.setToolTip(1, _("Updates for this package are ignored")+" - "+package.Name)
            
            if package.Id in UPDATES.IdPackageReference.keys():
                updatePackage: UpgradablePackage = UPDATES.IdPackageReference[package.Id]
                updateItem = updatePackage.PackageItem
                if updateItem in UPDATES.packageItems:
                    item.setIcon(1, self.updateIcon)
                    item.setToolTip(1, _("This package can be updated to version {0}").format(updatePackage.NewVersion)+" - "+package.Name)
            
            DISCOVER: UninstallSoftwareSection = globals.discover
            if package.Id in DISCOVER.IdPackageReference.keys():
                discoverablePackage: UpgradablePackage = DISCOVER.IdPackageReference[package.Id]
                discoverableItem = discoverablePackage.PackageItem
                if discoverableItem in DISCOVER.packageItems:
                    discoverableItem.setIcon(1, self.installedIcon)
                    discoverableItem.setToolTip(1, _("This package is already installed")+" - "+package.Name)
            
            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)

            action = QAction(package.Name+" \t"+package.Version, globals.trayMenuInstalledList)
            action.triggered.connect(lambda: (self.uninstallPackageItem(item)))
            action.setShortcut(package.Version)
            item.setAction(action)
            globals.trayMenuInstalledList.addAction(action)

    def confirmUninstallSelected(self, toUninstall: list[TreeWidgetItemWithQAction], a: CustomMessageBox, admin: bool = False, interactive: bool = False, removeData: bool = False):
        questionData = {
            "titlebarTitle": _("Uninstall"),
            "mainTitle": _("Are you sure?"),
            "mainText": _("Do you really want to uninstall {0}?").format(toUninstall[0].text(1)) if len(toUninstall) == 1 else  _("Do you really want to uninstall {0} packages?").format(len(toUninstall)),
            "acceptButtonTitle": _("Yes"),
            "cancelButtonTitle": _("No"),
            "icon": QIcon(),
        }
        if len(toUninstall) == 0:
            return
        if a.askQuestion(questionData):
            for program in toUninstall:
                self.callInMain.emit(partial(self.uninstallPackageItem, program, admin, interactive, removeData, avoidConfirm=True))

    def uninstall(self, id: str, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        self.uninstallPackageItem(self.ItemPackageReference[self.IdPackageReference[id]], admin, removeData, interactive, avoidConfirm)

    def uninstallPackageItem(self, packageItem: TreeWidgetItemWithQAction, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        package: Package = self.ItemPackageReference[packageItem]
        if not avoidConfirm:
            a = CustomMessageBox(self)
            Thread(target=self.confirmUninstallSelected, args=([packageItem], a, admin, interactive, removeData)).start()
        else:
            options = InstallationOptions()
            options.RunAsAdministrator = admin
            options.InteractiveInstallation = interactive
            options.RemoveDataOnUninstall = removeData
            self.addInstallation(PackageUninstallerWidget(package, options))

    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        packages = manager.getInstalledPackages()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for packages..."))
        self.packageList.label.setText(self.countLabel.text())
        for action in globals.trayMenuInstalledList.actions():
            globals.trayMenuInstalledList.removeAction(action)
        globals.trayMenuInstalledList.addAction(globals.installedHeader)
        return super().startLoadingPackages(force)

    def selectAllInstalled(self) -> None:
        self.allPkgSelected = not self.allPkgSelected
        for item in self.packageItems:
            item.setCheckState(Qt.CheckState.Checked if self.allPkgSelected else Qt.CheckState.Unchecked)


class AboutSection(SmoothScrollArea):
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
        try:
            table = QTableWidget()
            table.setAutoFillBackground(True)
            table.setStyleSheet("*{border: 0px solid transparent; background-color: transparent;}QHeaderView{font-size: 13pt;padding: 0px;margin: 0px;}QTableCornerButton::section,QHeaderView,QHeaderView::section,QTableWidget,QWidget,QTableWidget::item{background-color: transparent;border: 0px solid transparent}")
            table.setColumnCount(2)
            table.setEnabled(False)
            table.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
            table.setShowGrid(False)
            table.setHorizontalHeaderLabels([_("Status"), _("Version")])
            table.setColumnWidth(1, 500)
            table.setColumnWidth(0, 150)
            table.verticalHeader().setFixedWidth(100)
            table.setRowCount(len(PackageManagersList)+1)
            table.setVerticalHeaderLabels(["Gsudo"] + [manager.NAME for manager in PackageManagersList])
            currentIndex: int = 0
            table.setItem(currentIndex, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["sudoFound"] else _("Not found")))
            table.setItem(currentIndex, 1, QTableWidgetItem(" "+str(globals.componentStatus["sudoVersion"])))

            for manager in PackageManagersList:
                try:
                    currentIndex += 1
                    table.setItem(currentIndex, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus[f"{manager.NAME}Found"] else _("Not found")))
                    table.setItem(currentIndex, 1, QTableWidgetItem(" "+str(globals.componentStatus[f"{manager.NAME}Version"])))
                    table.verticalHeaderItem(currentIndex).setTextAlignment(Qt.AlignRight)
                    table.setRowHeight(currentIndex, 35)
                except Exception as e:
                    report(e)
                    currentIndex += 1
                    table.setItem(currentIndex, 0, QTableWidgetItem("  "+_("Error")))
                    table.setItem(currentIndex, 1, QTableWidgetItem(" "+str(e)))
                    table.verticalHeaderItem(currentIndex).setTextAlignment(Qt.AlignRight)
                    table.setRowHeight(currentIndex, 35)

            table.horizontalHeaderItem(0).setTextAlignment(Qt.AlignLeft)
            table.setRowHeight(0, 35)
            table.horizontalHeaderItem(1).setTextAlignment(Qt.AlignLeft)
            table.verticalHeaderItem(0).setTextAlignment(Qt.AlignRight)
            table.setCornerWidget(QLabel(""))
            table.setCornerButtonEnabled(False)
            table.setFixedHeight(260)
            table.cornerWidget().setStyleSheet("background: transparent;")
            self.layout.addWidget(table)
            title = QLabel(_("About WingetUI version {0}").format(versionName))
            title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
            self.layout.addWidget(title)
            self.layout.addSpacing(5)
            description = QLinkLabel(_("The main goal of this project is to create an intuitive UI to manage the most common CLI package managers for Windows, such as Winget and Scoop.")+"\n"+_("This project has no connection with the official {0} project — it's completely unofficial.").format(f"<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli\">Winget</a>"))
            self.layout.addWidget(description)
            self.layout.addSpacing(5)
            self.layout.addWidget(QLinkLabel(f"{_('Homepage')}:   <a style=\"color: {blueColor};\" href=\"https://marticliment.com/wingetui\">https://marticliment.com/wingetui</a>"))
            self.layout.addWidget(QLinkLabel(f"{_('Repository')}:   <a style=\"color: {blueColor};\" href=\"https://github.com/marticliment/wingetui\">https://github.com/marticliment/wingetui</a>"))
            self.layout.addSpacing(30)

            self.layout.addWidget(QLinkLabel(f"{_('Contributors')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.layout.addWidget(QLinkLabel(_("WingetUI wouldn't have been possible with the help of our dear contributors. Check out their GitHub profile, WingetUI wouldn't be possible without them!")))
            contributorsHTMLList = "<ul>"
            for contributor in contributorsInfo:
                contributorsHTMLList += f"<li><a style=\"color:{blueColor}\" href=\"{contributor.get('link')}\">{contributor.get('name')}</a></li>"
            contributorsHTMLList += "</ul>"
            self.layout.addWidget(QLinkLabel(contributorsHTMLList))
            self.layout.addSpacing(15)

            self.layout.addWidget(QLinkLabel(f"{_('Translators')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.layout.addWidget(QLinkLabel(_("WingetUI has not been machine translated. The following users have been in charge of the translations:")))
            translatorsHTMLList = "<ul>"
            translatorList = []
            translatorData: dict[str, str] = {}
            for key, value in languageCredits.items():
                langName = languageReference[key] if (key in languageReference) else key
                for translator in value:
                    link = translator.get("link")
                    name = translator.get("name")
                    translatorLine = name
                    if (link):
                        translatorLine = f"<a style=\"color:{blueColor}\" href=\"{link}\">{name}</a>"
                    translatorKey = f"{name}{langName}" # for sort
                    translatorList.append(translatorKey)
                    translatorData[translatorKey] = f"{translatorLine} ({langName})"
            translatorList.sort(key=str.casefold)
            for translator in translatorList:
                translatorsHTMLList += f"<li>{translatorData[translator]}</li>"
            translatorsHTMLList += "</ul><br>"
            translatorsHTMLList += _("Do you want to translate WingetUI to your language? See how to contribute <a style=\"color:{0}\" href=\"{1}\"a>HERE!</a>").format(blueColor, "https://github.com/marticliment/WingetUI/wiki#translating-wingetui")
            self.layout.addWidget(QLinkLabel(translatorsHTMLList))
            self.layout.addSpacing(15)

            self.layout.addWidget(QLinkLabel(f"{_('About the dev')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.layout.addWidget(QLinkLabel(_("Hi, my name is Martí, and i am the <i>developer</i> of WingetUI. WingetUI has been entirely made on my free time!")))
            try:
                self.layout.addWidget(QLinkLabel(_("Check out my {0} and my {1}!").format(f"<a style=\"color:{blueColor}\" href=\"https://github.com/marticliment\">{_('GitHub profile')}</a>", f"<a style=\"color:{blueColor}\" href=\"http://www.marticliment.com\">{_('homepage')}</a>")))
            except Exception as e:
                print(e)
                print(blueColor)
                print(_('homepage'))
                print(_('GitHub profile'))
            self.layout.addWidget(QLinkLabel(_("Do you find WingetUI useful? You'd like to support the developer? If so, you can {0}, it helps a lot!").format(f"<a style=\"color:{blueColor}\" href=\"https://ko-fi.com/martinet101\">{_('buy me a coffee')}</a>")))

            self.layout.addSpacing(15)
            self.layout.addWidget(QLinkLabel(f"{_('Licenses')}:", f"font-size: 22pt;font-family: \"{globals.dispfont}\";font-weight: bold;"))
            self.layout.addWidget(QLabel())
            self.layout.addWidget(QLinkLabel(f"WingetUI:&nbsp;&nbsp;&nbsp;&nbsp;LGPL v2.1:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/marticliment/WinGetUI/blob/main/LICENSE\">https://github.com/marticliment/WinGetUI/blob/main/LICENSE</a>"))
            self.layout.addWidget(QLabel())
            self.layout.addWidget(QLinkLabel(f"PySide6:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;LGPLv3:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">https://www.gnu.org/licenses/lgpl-3.0.html</a>"))
            self.layout.addWidget(QLinkLabel(f"Python3:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{_('PSF License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://docs.python.org/3/license.html#psf-license\">https://docs.python.org/3/license.html#psf-license</a>"))
            self.layout.addWidget(QLinkLabel(f"Pywin32:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{_('PSF License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://spdx.org/licenses/PSF-2.0.html\">https://spdx.org/licenses/PSF-2.0.html</a>"))
            self.layout.addWidget(QLinkLabel(f"Win32mica:&thinsp;{_('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://github.com/marticliment/win32mica/blob/main/LICENSE\">https://github.com/marticliment/win32mica/blob/main/LICENSE</a>"))
            self.layout.addWidget(QLinkLabel())
            self.layout.addWidget(QLinkLabel(f"Winget:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{_('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://github.com/microsoft/winget-cli/blob/master/LICENSE\">https://github.com/microsoft/winget-cli/blob/master/LICENSE</a>"))
            self.layout.addWidget(QLinkLabel(f"Scoop:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;{_('Unlicense')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/lukesampson/scoop/blob/master/LICENSE\">https://github.com/lukesampson/scoop/blob/master/LICENSE</a>"))
            self.layout.addWidget(QLinkLabel(f"Chocolatey:&thinsp;Apache v2:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/chocolatey/choco/blob/master/LICENSE\">https://github.com/chocolatey/choco/blob/master/LICENSE</a>"))
            self.layout.addWidget(QLinkLabel(f"GSudo:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;{_('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://github.com/gerardog/gsudo/blob/master/LICENSE.txt\">https://github.com/gerardog/gsudo/blob/master/LICENSE.txt</a>"))
            self.layout.addWidget(QLinkLabel())
            self.layout.addWidget(QLinkLabel(f"{_('Icons')}:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;{_('By Icons8')}:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&thinsp;<a style=\"color: {blueColor};\" href=\"https://icons8.com\">https://icons8.com</a>"))
            self.layout.addWidget(QLinkLabel())
            self.layout.addWidget(QLinkLabel())
            button = QPushButton(_("About Qt6"))
            button.setFixedWidth(710)
            button.setFixedHeight(25)
            button.clicked.connect(lambda: MessageBox.aboutQt(self, f"WingetUI - {_('About Qt6')}"))
            self.layout.addWidget(button)
            self.layout.addWidget(QLinkLabel())
            self.layout.addStretch()
        except Exception as e:
            self.layout.addWidget(QLabel("An error occurred while loading the about section"))
            self.layout.addWidget(QLabel(str(e)))
            report(e)
        print("🟢 About tab loaded!")

    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements, daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)

class SettingsSection(SmoothScrollArea):
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

        self.generalTitle = CollapsableSection(_("General preferences"), getMedia("settings"), _("Language, theme and other miscellaneous preferences"))
        self.layout.addWidget(self.generalTitle)

        self.language = SectionComboBox(_("WingetUI display language:")+" (Language)")
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
            self.language.toggleRestartButton(selectedLang != langName)
            setSettingsValue("PreferredLanguage", selectedLang)

        def restartWingetUIByLangChange():
            subprocess.run(str("start /B \"\" \""+sys.executable)+"\"", shell=True)
            globals.app.quit()

        self.language.restartButton.clicked.connect(restartWingetUIByLangChange)
        self.language.combobox.currentTextChanged.connect(changeLang)

        self.wizardButton = SectionButton(_("Open the welcome wizard"), _("Open"))
        def ww():
            subprocess.run(str("start /B \"\" \""+sys.executable)+"\" --welcome", shell=True)
            globals.app.quit()

        self.wizardButton.clicked.connect(ww)
        self.wizardButton.button.setObjectName("AccentButton")
        self.wizardButton.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.wizardButton)

        updateCheckBox = SectionCheckBox(_("Update WingetUI automatically"))
        updateCheckBox.setChecked(not getSettings("DisableAutoUpdateWingetUI"))
        updateCheckBox.stateChanged.connect(lambda v: setSettings("DisableAutoUpdateWingetUI", not bool(v)))
        self.generalTitle.addWidget(updateCheckBox)


        checkForUpdates = SectionCheckBox(_("Check for package updates periodically"))
        checkForUpdates.setChecked(not getSettings("DisableAutoCheckforUpdates"))
        self.generalTitle.addWidget(checkForUpdates)
        frequencyCombo = SectionComboBox(_("Check for updates every:"), buttonEnabled=False)

        times = {
            _("{0} minutes").format(10):   "600",
            _("{0} minutes").format(30):  "1800",
            _("1 hour")                :  "3600",
            _("{0} hours").format(2)   :  "7200",
            _("{0} hours").format(4)   : "14400",
            _("{0} hours").format(8)   : "28800",
            _("{0} hours").format(12)  : "43200",
            _("1 day")                 : "86400",
            _("{0} days").format(2)    :"172800",
            _("{0} days").format(3)    :"259200",
            _("1 week")                :"604800",
        }
        invertedTimes = {
            "600"   : _("{0} minutes").format(10),
            "1800"  : _("{0} minutes").format(30),
            "3600"  : _("1 hour"),
            "7200"  : _("{0} hours").format(2),
            "14400" : _("{0} hours").format(4),
            "28800" : _("{0} hours").format(8),
            "43200" : _("{0} hours").format(12),
            "86400" : _("1 day"),
            "172800": _("{0} days").format(2),
            "259200": _("{0} days").format(3),
            "604800": _("1 week")
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

        self.generalTitle.addWidget(frequencyCombo)
        frequencyCombo.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius:0 ;border-bottom: 0px;}")


        automaticallyInstallUpdates = SectionCheckBox(_("Update packages automatically"))
        automaticallyInstallUpdates.setChecked(getSettings("AutomaticallyUpdatePackages"))
        automaticallyInstallUpdates.stateChanged.connect(lambda v: setSettings("AutomaticallyUpdatePackages", bool(v)))
        self.generalTitle.addWidget(automaticallyInstallUpdates)



        self.theme = SectionComboBox(_("Application theme:"))
        self.theme.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0px;}")

        self.generalTitle.addWidget(self.theme)
        self.theme.restartButton.setText(_("Apply"))

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

        def applyTheme():
            mode = win32mica.MicaTheme.AUTO
            theme = getSettingsValue("PreferredTheme")
            match theme:
                case "dark":
                    mode = win32mica.MicaTheme.DARK
                case "light":
                    mode = win32mica.MicaTheme.LIGHT
            win32mica.ApplyMica(globals.mainWindow.winId(), mode)
            globals.mainWindow.ApplyStyleSheetsAndIcons()
            self.theme.restartButton.hide()

        self.theme.combobox.currentTextChanged.connect(lambda v: (setSettingsValue("PreferredTheme", themes[v]), applyTheme()))
        self.theme.restartButton.clicked.connect(applyTheme)


        def exportSettings():
            nonlocal self
            try:
                rawstr = ""
                for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                    if not "Running" in file and not "png" in file and not "PreferredLanguage" in file and not "json" in file:
                        sName = file.replace("\\", "/").split("/")[-1]
                        rawstr += sName+"|@|"+getSettingsValue(sName)+"|~|"
                fileName = QFileDialog.getSaveFileName(self, _("Export settings to a local file"), os.path.expanduser("~"), f"{_('WingetUI Settings File')} (*.conf);;{_('All files')} (*.*)")
                if fileName[0] != "":
                    oFile =  open(fileName[0], "w")
                    oFile.write(rawstr)
                    oFile.close()
                    subprocess.run("explorer /select,\""+fileName[0].replace('/', '\\')+"\"", shell=True)
            except Exception as e:
                report(e)

        def importSettings():
            nonlocal self
            try:
                fileName = QFileDialog.getOpenFileName(self, _("Import settings from a local file"), os.path.expanduser("~"), f"{_('WingetUI Settings File')} (*.conf);;{_('All files')} (*.*)")
                if fileName:
                    iFile = open(fileName[0], "r")
                    rawstr = iFile.read()
                    iFile.close()
                    resetSettings()
                    for element in rawstr.split("|~|"):
                        pairValue = element.split("|@|")
                        if len(pairValue) == 2:
                            setSettings(pairValue[0], True)
                            if pairValue[1] != "":
                                setSettingsValue(pairValue[0], pairValue[1])
                    os.startfile(sys.executable)
                    globals.app.quit()
            except Exception as e:
                report(e)

        def resetSettings():
            for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                if not "Running" in file:
                    try:
                        os.remove(file)
                    except:
                        pass

        self.importSettings = SectionButton(_("Import settings from a local file"), _("Import"))
        self.importSettings.clicked.connect(lambda: importSettings())
        self.importSettings.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.importSettings)
        self.exportSettings = SectionButton(_("Export settings to a local file"), _("Export"))
        self.exportSettings.clicked.connect(lambda: exportSettings())
        self.exportSettings.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.generalTitle.addWidget(self.exportSettings)
        self.resetButton = SectionButton(_("Reset WingetUI"), _("Reset"))
        self.resetButton.clicked.connect(lambda: (resetSettings(), os.startfile(sys.executable), globals.app.quit()))
        self.generalTitle.addWidget(self.resetButton)

        self.startup = CollapsableSection(_("Startup options"), getMedia("launch"), _("WingetUI autostart behaviour, application launch settings"))
        self.layout.addWidget(self.startup)
        doCloseWingetUI = SectionCheckBox(_("Autostart WingetUI in the notifications area"))
        doCloseWingetUI.setChecked(not getSettings("DisableAutostart"))
        doCloseWingetUI.stateChanged.connect(lambda v: setSettings("DisableAutostart", not bool(v)))
        self.startup.addWidget(doCloseWingetUI)
        disableUpdateIndexes = SectionCheckBox(_("Do not update package indexes on launch"))
        disableUpdateIndexes.setChecked(getSettings("DisableUpdateIndexes"))
        self.startup.addWidget(disableUpdateIndexes)
        enableScoopCleanup = SectionCheckBox(_("Enable Scoop cleanup on launch"))
        disableUpdateIndexes.stateChanged.connect(lambda v: setSettings("DisableUpdateIndexes", bool(v)))
        enableScoopCleanup.setChecked(getSettings("EnableScoopCleanup"))
        enableScoopCleanup.stateChanged.connect(lambda v: setSettings("EnableScoopCleanup", bool(v)))
        enableScoopCleanup.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.startup.addWidget(enableScoopCleanup)

        self.UITitle = CollapsableSection(_("User interface preferences"), getMedia("interactive"), _("Action when double-clicking packages, hide successful installations"))
        self.layout.addWidget(self.UITitle)
        enableListingallPackages = SectionCheckBox(_("List packages if the query is empty on the \"{discoveryTab}\" tab").format(discoveryTab = _("Discover Packages")))
        enableListingallPackages.setChecked(getSettings("AlwaysListPackages"))
        enableListingallPackages.stateChanged.connect(lambda v: setSettings("AlwaysListPackages", bool(v)))
        self.UITitle.addWidget(enableListingallPackages)
        changeDefaultInstallAction = SectionCheckBox(_("Directly install when double-clicking an item on the \"{discoveryTab}\" tab (instead of showing the package info)").format(discoveryTab = _("Discover Packages")))
        changeDefaultInstallAction.setChecked(getSettings("InstallOnDoubleClick"))
        changeDefaultInstallAction.stateChanged.connect(lambda v: setSettings("InstallOnDoubleClick", bool(v)))
        self.UITitle.addWidget(changeDefaultInstallAction)
        changeDefaultUpdateAction = SectionCheckBox(_("Show info about the package on the Updates tab"))
        changeDefaultUpdateAction.setChecked(not getSettings("DoNotUpdateOnDoubleClick"))
        changeDefaultUpdateAction.stateChanged.connect(lambda v: setSettings("DoNotUpdateOnDoubleClick", bool(not v)))
        self.UITitle.addWidget(changeDefaultUpdateAction)
        dontUseBuiltInGsudo = SectionCheckBox(_("Remove successful installs/uninstalls/updates from the installation list"))
        dontUseBuiltInGsudo.setChecked(not getSettings("MaintainSuccessfulInstalls"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: setSettings("MaintainSuccessfulInstalls", not bool(v)))
        dontUseBuiltInGsudo.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.UITitle.addWidget(dontUseBuiltInGsudo)

        self.trayTitle = CollapsableSection(_("Notification tray options"), getMedia("systemtray"), _("WingetUI tray application preferences"))
        self.layout.addWidget(self.trayTitle)

        doCloseWingetUI = SectionCheckBox(_("Close WingetUI to the notification area"))
        doCloseWingetUI.setChecked(not getSettings("DisablesystemTray"))
        doCloseWingetUI.stateChanged.connect(lambda v: setSettings("DisablesystemTray", not bool(v)))
        self.trayTitle.addWidget(doCloseWingetUI)
        generalNotifications = SectionCheckBox(_("Enable WingetUI notifications"))
        generalNotifications.setChecked(not getSettings("DisableNotifications"))
        generalNotifications.stateChanged.connect(lambda v: setSettings("DisableNotifications", not bool(v)))
        self.trayTitle.addWidget(generalNotifications)
        updatesNotifications = SectionCheckBox(_("Show a notification when there are available updates"))
        updatesNotifications.setChecked(not getSettings("DisableUpdatesNotifications"))
        updatesNotifications.stateChanged.connect(lambda v: setSettings("DisableUpdatesNotifications", not bool(v)))
        self.trayTitle.addWidget(updatesNotifications)
        errorNotifications = SectionCheckBox(_("Show a notification when an installation fails"))
        errorNotifications.setChecked(not getSettings("DisableErrorNotifications"))
        errorNotifications.stateChanged.connect(lambda v: setSettings("DisableErrorNotifications", not bool(v)))
        self.trayTitle.addWidget(errorNotifications)
        successNotifications = SectionCheckBox(_("Show a notification when an installation finishes successfully"))
        successNotifications.setChecked(not getSettings("DisableSuccessNotifications"))
        successNotifications.stateChanged.connect(lambda v: setSettings("DisableSuccessNotifications", not bool(v)))
        self.trayTitle.addWidget(successNotifications)
        successNotifications.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")



        self.advancedOptions = CollapsableSection(_("Administrator privileges preferences"), getMedia("runasadmin"), _("Ask once or always for administrator rights, elevate installations by default"))
        self.layout.addWidget(self.advancedOptions)
        doCacheAdminPrivileges = SectionCheckBox(_("Ask only once for administrator privileges (not recommended)"))
        doCacheAdminPrivileges.setChecked(getSettings("DoCacheAdminRights"))
        doCacheAdminPrivileges.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0px;}")


        def resetAdminRightsCache():
            resetsudo = subprocess.Popen([GSUDO_EXECUTABLE, "-k"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            resetsudo.wait()
            globals.adminRightsGranted = False

        doCacheAdminPrivileges.stateChanged.connect(lambda v: (setSettings("DoCacheAdminRights", bool(v)), resetAdminRightsCache()))
        self.advancedOptions.addWidget(doCacheAdminPrivileges)

        # Due to lambda's nature, the following code can NOT be placed in a for loop
        
        WINGET = Winget.NAME
        alwaysRunAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm=WINGET))
        alwaysRunAsAdmin.setChecked(getSettings(f"AlwaysElevate"+WINGET))
        alwaysRunAsAdmin.stateChanged.connect(lambda v: setSettings(f"AlwaysElevate"+WINGET, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        SCOOP = Scoop.NAME
        alwaysRunAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm=SCOOP))
        alwaysRunAsAdmin.setChecked(getSettings(f"AlwaysElevate"+SCOOP))
        alwaysRunAsAdmin.stateChanged.connect(lambda v: setSettings(f"AlwaysElevate"+SCOOP, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        CHOCO = Choco.NAME
        alwaysRunAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm=CHOCO))
        alwaysRunAsAdmin.setChecked(getSettings(f"AlwaysElevate"+CHOCO))
        alwaysRunAsAdmin.stateChanged.connect(lambda v: setSettings(f"AlwaysElevate"+CHOCO, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        PIP = Pip.NAME
        alwaysRunAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm=PIP))
        alwaysRunAsAdmin.setChecked(getSettings(f"AlwaysElevate"+PIP))
        alwaysRunAsAdmin.stateChanged.connect(lambda v: setSettings(f"AlwaysElevate"+PIP, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        NPM = Npm.NAME
        alwaysRunAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm=NPM))
        alwaysRunAsAdmin.setChecked(getSettings(f"AlwaysElevate"+NPM))
        alwaysRunAsAdmin.stateChanged.connect(lambda v: setSettings(f"AlwaysElevate"+NPM, bool(v)))
        self.advancedOptions.addWidget(alwaysRunAsAdmin)

        dontUseBuiltInGsudo = SectionCheckBox(_("Use installed GSudo instead of the bundled one (requires app restart)"))
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: (setSettings("UseUserGSudo", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        dontUseBuiltInGsudo.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.advancedOptions.addWidget(dontUseBuiltInGsudo)

        self.advancedOptions = CollapsableSection(_("Experimental settings and developer options"), getMedia("testing"), _("Beta features and other options that shouldn't be touched"))
        self.layout.addWidget(self.advancedOptions)
        disableShareApi = SectionCheckBox(_("Disable new share API (port 7058)"))
        disableShareApi.setChecked(getSettings("DisableApi"))
        disableShareApi.stateChanged.connect(lambda v: (setSettings("DisableApi", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(disableShareApi)
        parallelInstalls = SectionCheckBox(_("Allow parallel installs (NOT RECOMMENDED)"))
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.stateChanged.connect(lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.advancedOptions.addWidget(parallelInstalls)

        enableSystemWinget = SectionCheckBox(_("Use system Winget (Needs a restart)"))
        enableSystemWinget.setChecked(getSettings("UseSystemWinget"))
        enableSystemWinget.stateChanged.connect(lambda v: (setSettings("UseSystemWinget", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(enableSystemWinget)
        enableSystemWinget = SectionCheckBox(_("Use ARM compiled winget version (ONLY FOR ARM64 SYSTEMS)"))
        enableSystemWinget.setChecked(getSettings("EnableArmWinget"))
        enableSystemWinget.stateChanged.connect(lambda v: (setSettings("EnableArmWinget", bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.advancedOptions.addWidget(enableSystemWinget)
        disableLangUpdates = SectionCheckBox(_("Do not download new app translations from GitHub automatically"))
        disableLangUpdates.setChecked(getSettings("DisableLangAutoUpdater"))
        disableLangUpdates.stateChanged.connect(lambda v: setSettings("DisableLangAutoUpdater", bool(v)))
        self.advancedOptions.addWidget(disableLangUpdates)
        
        useCustomIconProvider = SectionCheckBoxTextBox(_("Use a custom icon and screenshot database URL"), None, f"<a style='color:rgb({getColors()[2 if isDark() else 4]})' href=\"https://www.marticliment.com/wingetui/help/icons-and-screenshots#custom-source\">{_('More details')}</a>")
        useCustomIconProvider.setPlaceholderText(_("Paste a valid URL to the database"))
        useCustomIconProvider.setText(getSettingsValue("IconDataBaseURL"))
        useCustomIconProvider.setChecked(getSettings("IconDataBaseURL"))
        useCustomIconProvider.stateChanged.connect(lambda e: setSettings("IconDataBaseURL", e))
        useCustomIconProvider.valueChanged.connect(lambda v: setSettingsValue("IconDataBaseURL", v))
        self.advancedOptions.addWidget(useCustomIconProvider)
        
        resetyWingetUICache = SectionButton(_("Reset WingetUI icon and screenshot cache"), _("Reset"))
        resetyWingetUICache.clicked.connect(lambda: (shutil.rmtree(os.path.join(os.path.expanduser("~"), ".wingetui/cachedmeta/")), self.inform(_("Cache was reset successfully!"))))
        resetyWingetUICache.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        self.advancedOptions.addWidget(resetyWingetUICache)

        def resetWingetUIStore():
            sd = getSettings("DisableScoop")
            wd = getSettings("DisableWinget")
            for file in glob.glob(os.path.join(os.path.expanduser("~"), ".wingetui/*")):
                if not "Running" in file:
                    try:
                        os.remove(file)
                    except:
                        pass
            setSettings("DisableScoop", sd)
            setSettings("DisableWinget", wd)
            restartWingetUIByLangChange()

        resetWingetUI = SectionButton(_("Reset WingetUI and its preferences"), _("Reset"))
        resetWingetUI.clicked.connect(lambda: resetWingetUIStore())
        self.advancedOptions.addWidget(resetWingetUI)

        title = QLabel(_("Package manager preferences"))
        self.layout.addSpacing(40)
        title.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.layout.addWidget(title)
        self.layout.addSpacing(20)

        self.wingetPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Winget"), getMedia("winget"), _("{pm} package manager specific preferences").format(pm = "Winget"))
        self.layout.addWidget(self.wingetPreferences)
        path = SectionButton(Winget.EXECUTABLE, _("Copy"), h = 50)
        path.clicked.connect(lambda: globals.app.clipboard().setText(Winget.EXECUTABLE))
        self.wingetPreferences.addWidget(path)
        path.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        disableWinget = SectionCheckBox(_("Enable {pm}").format(pm = "Winget"))
        disableWinget.setChecked(not getSettings(f"Disable{Winget.NAME}"))
        disableWinget.stateChanged.connect(lambda v: (setSettings(f"Disable{Winget.NAME}", not bool(v)), parallelInstalls.setEnabled(v), button.setEnabled(v), enableSystemWinget.setEnabled(v), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.wingetPreferences.addWidget(disableWinget)
        #disableWinget = SectionCheckBox(_("Enable Microsoft Store package source"))
        #disableWinget.setChecked(not getSettings(f"DisableMicrosoftStore"))
        #disableWinget.stateChanged.connect(lambda v: (setSettings(f"DisableMicrosoftStore", not bool(v))))
        #self.wingetPreferences.addWidget(disableWinget)
        bucketManager = WingetBucketManager()
        bucketManager.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.wingetPreferences.addWidget(bucketManager)



        button = SectionButton(_("Reset Winget sources (might help if no packages are listed)"), _("Reset"))
        button.clicked.connect(lambda: (os.startfile(os.path.join(realpath, "resources/reset_winget_sources.cmd"))))
        button.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.wingetPreferences.addWidget(button)

        parallelInstalls.setEnabled(disableWinget.isChecked())
        button.setEnabled(disableWinget.isChecked())
        enableSystemWinget.setEnabled(disableWinget.isChecked())


        self.scoopPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Scoop"), getMedia("scoop"), _("{pm} package manager specific preferences").format(pm = "Scoop"))
        self.layout.addWidget(self.scoopPreferences)
        path = SectionButton(Scoop.EXECUTABLE, _("Copy"), h = 50)
        path.clicked.connect(lambda: globals.app.clipboard().setText(Scoop.EXECUTABLE))
        self.scoopPreferences.addWidget(path)
        path.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disableScoop = SectionCheckBox(_("Enable {pm}").format(pm = "Scoop"))
        disableScoop.setChecked(not getSettings(f"Disable{Scoop.NAME}"))
        disableScoop.stateChanged.connect(lambda v: (setSettings(f"Disable{Scoop.NAME}", not bool(v)), scoopPreventCaps.setEnabled(v), bucketManager.setEnabled(v), uninstallScoop.setEnabled(v), enableScoopCleanup.setEnabled(v), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.scoopPreferences.addWidget(disableScoop)
        scoopPreventCaps = SectionCheckBox(_("Show Scoop packages in lowercase"))
        scoopPreventCaps.setChecked(getSettings("LowercaseScoopApps"))
        scoopPreventCaps.stateChanged.connect(lambda v: setSettings("LowercaseScoopApps", bool(v)))
        self.scoopPreferences.addWidget(scoopPreventCaps)
        bucketManager = ScoopBucketManager()
        bucketManager.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.scoopPreferences.addWidget(bucketManager)
        installScoop = SectionButton(_("Reset Scoop's global app cache"), _("Reset"))
        installScoop.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        installScoop.clicked.connect(lambda: Thread(target=lambda: subprocess.Popen([GSUDO_EXECUTABLE, os.path.join(realpath, "resources", "scoop_cleanup.cmd")]), daemon=True).start())
        self.scoopPreferences.addWidget(installScoop)

        installScoop = SectionButton(_("Install Scoop"), _("Install"))
        installScoop.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        installScoop.clicked.connect(lambda: (setSettings("DisableScoop", False), disableScoop.setChecked(False), os.startfile(os.path.join(realpath, "resources/install_scoop.cmd"))))
        self.scoopPreferences.addWidget(installScoop)
        uninstallScoop = SectionButton(_("Uninstall Scoop (and its packages)"), _("Uninstall"))
        uninstallScoop.clicked.connect(lambda: (setSettings("DisableScoop", True), disableScoop.setChecked(True), os.startfile(os.path.join(realpath, "resources/uninstall_scoop.cmd"))))
        self.scoopPreferences.addWidget(uninstallScoop)
        uninstallScoop.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")

        scoopPreventCaps.setEnabled(disableScoop.isChecked())
        bucketManager.setEnabled(disableScoop.isChecked())
        uninstallScoop.setEnabled(disableScoop.isChecked())
        enableScoopCleanup.setEnabled(disableScoop.isChecked())

        self.chocoPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Chocolatey"), getMedia("choco"), _("{pm} package manager specific preferences").format(pm = "Chocolatey"))
        self.layout.addWidget(self.chocoPreferences)
        
        path = SectionButton(Choco.EXECUTABLE, _("Copy"), h = 50)
        path.clicked.connect(lambda: globals.app.clipboard().setText(Choco.EXECUTABLE))
        self.chocoPreferences.addWidget(path)
        path.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        disableChocolatey = SectionCheckBox(_("Enable {pm}").format(pm = "Chocolatey"))
        disableChocolatey.setChecked(not getSettings(f"Disable{Choco.NAME}"))
        disableChocolatey.stateChanged.connect(lambda v: (setSettings(f"Disable{Choco.NAME}", not bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.chocoPreferences.addWidget(disableChocolatey)
        enableSystemChocolatey = SectionCheckBox(_("Use system Chocolatey (Needs a restart)"))
        enableSystemChocolatey.setChecked(getSettings("UseSystemChocolatey"))
        enableSystemChocolatey.stateChanged.connect(lambda v: setSettings("UseSystemChocolatey", bool(v)))
        self.chocoPreferences.addWidget(enableSystemChocolatey)

        self.pipPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Pip"), getMedia("python"), _("{pm} package manager specific preferences").format(pm = "Pip"))
        self.layout.addWidget(self.pipPreferences)
        
        path = SectionButton(Pip.EXECUTABLE, _("Copy"), h = 50)
        path.clicked.connect(lambda: globals.app.clipboard().setText(Pip.EXECUTABLE))
        self.pipPreferences.addWidget(path)
        path.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")
        disablePip = SectionCheckBox(_("Enable {pm}").format(pm = "Pip"))
        disablePip.setChecked(not getSettings(f"Disable{Pip.NAME}"))
        disablePip.stateChanged.connect(lambda v: (setSettings(f"Disable{Pip.NAME}", not bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.pipPreferences.addWidget(disablePip)
        disablePip.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")

        self.npmPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Npm"), getMedia("node"), _("{pm} package manager specific preferences").format(pm = "Npm"))
        self.layout.addWidget(self.npmPreferences)

        path = SectionButton(Npm.EXECUTABLE, _("Copy"), h = 50)
        path.clicked.connect(lambda: globals.app.clipboard().setText(Npm.EXECUTABLE))
        path.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0px;border-bottom-right-radius: 0px;border-bottom: 0px;}")

        self.npmPreferences.addWidget(path)

        
        disableNpm = SectionCheckBox(_("Enable {pm}").format(pm = Npm.NAME))
        disableNpm.setChecked(not getSettings(f"Disable{Npm.NAME}"))
        disableNpm.stateChanged.connect(lambda v: (setSettings(f"Disable{Npm.NAME}", not bool(v)), self.inform(_("Restart WingetUI to fully apply changes"))))
        self.npmPreferences.addWidget(disableNpm)
        disableNpm.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")


        self.layout.addStretch()

        print("🟢 Settings tab loaded!")

    def showEvent(self, event: QShowEvent) -> None:
        Thread(target=self.announcements.loadAnnouncements, daemon=True, name="Settings: Announce loader").start()
        return super().showEvent(event)
    
    def inform(self, text: str) -> None:
        self.notif = InWindowNotification(self, text)
        self.notif.show()

class BaseLogSection(QWidget):
    def __init__(self):
        super().__init__()

        class QPlainTextEditWithFluentMenu(QPlainTextEdit):
            def __init__(selftext):
                super().__init__()

            def contextMenuEvent(selftext, e: QContextMenuEvent) -> None:
                menu = selftext.createStandardContextMenu()
                menu.addSeparator()

                a = QAction()
                a.setText(_("Reload"))
                a.triggered.connect(self.loadData)
                menu.addAction(a)


                a4 = QAction()
                a4.setText(_("Show missing translation strings"))
                a4.triggered.connect(lambda: selftext.setPlainText('\n'.join(MissingTranslationList)))#buffer.getvalue()))
                menu.addAction(a4)


                a2 = QAction()
                a2.setText(_("Export log as a file"))
                a2.triggered.connect(lambda: saveLog())
                menu.addAction(a2)

                a3 = QAction()
                a3.setText(_("Copy to clipboard"))
                a3.triggered.connect(lambda: copyLog())
                menu.addAction(a3)

                ApplyMenuBlur(menu.winId().__int__(), menu)
                menu.exec(e.globalPos())

        self.setObjectName("background")

        self.setLayout(QVBoxLayout())
        self.setContentsMargins(0, 0, 0, 0)

        self.textEdit = QPlainTextEditWithFluentMenu()
        self.textEdit.setReadOnly(True)

        self.textEdit.setPlainText(stdout_buffer.getvalue())

        reloadButton = QPushButton(_("Reload log"))
        reloadButton.setFixedWidth(200)
        reloadButton.clicked.connect(self.loadData)

        def saveLog():
            try:
                print("🔵 Saving log...")
                self.loadData()
                f = QFileDialog.getSaveFileName(None, _("Export"), os.path.expanduser("~"), f"{_('Text file')} (*.txt)")
                if f[0]:
                    fpath = f[0]
                    if not ".txt" in fpath.lower():
                        fpath += ".txt"
                    with open(fpath, "wb") as fobj:
                        fobj.write(self.textEdit.toPlainText().encode("utf-8", errors="ignore"))
                        fobj.close()
                    os.startfile(fpath)
                    print("🟢 log saved successfully")
                else:
                    print("🟡 log save cancelled!")
            except Exception as e:
                report(e)

        exportButtom = QPushButton(_("Export to a file"))
        exportButtom.setFixedWidth(200)
        exportButtom.clicked.connect(saveLog)

        def copyLog():
            try:
                print("🔵 Copying log to the clipboard...")
                self.loadData()
                globals.app.clipboard().setText(self.textEdit.toPlainText())
                print("🟢 Log copied to the clipboard successfully!")
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(stdout_buffer.getvalue())

        copyButton = QPushButton(_("Copy to clipboard"))
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
        
    
        self.ApplyIcons()
        self.registeredThemeEvent = False
        
    def ApplyIcons(self):
        if isDark():
            self.textEdit.setStyleSheet(f"QPlainTextEdit{{margin: 10px;border-radius: 6px;border: 1px solid #161616;}}")
        else:
            self.textEdit.setStyleSheet(f"QPlainTextEdit{{margin: 10px;border-radius: 6px;border: 1px solid #dddddd;}}")

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        self.loadData()
        return super().showEvent(event)


class OperationHistorySection(BaseLogSection):

    def loadData(self):
        print("🔵 Loading operation log...")
        self.textEdit.setPlainText(getSettingsValue("OperationHistory"))

class LogSection(BaseLogSection):

    def loadData(self):
        print("🔵 Loading log...")
        self.textEdit.setPlainText(stdout_buffer.getvalue())
        self.textEdit.verticalScrollBar().setValue(self.textEdit.verticalScrollBar().maximum())

class PackageManagerLogSection(BaseLogSection):

    def loadData(self):
        print("🔵 Reloading Package Manager logs...")
        self.textEdit.setPlainText(globals.PackageManagerOutput.replace("\n\n\n", ""))
        self.textEdit.verticalScrollBar().setValue(self.textEdit.verticalScrollBar().maximum())

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

        self.tagsWidget = QWidget()
        self.tagsWidget.setLayout(FlowLayout())

        self.hLayout = QHBoxLayout()
        self.oLayout = QHBoxLayout()
        self.description = QLinkLabel("<b>"+_('Description:')+"</b> "+_('Unknown'))
        self.description.setWordWrap(True)

        self.layout.addWidget(self.tagsWidget)
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
        self.screenshotsWidget.setFixedHeight(150)
        self.screenshotsWidget.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.screenshotsWidget.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.layout.addWidget(self.screenshotsWidget)
        self.centralwidget = QWidget(self)

        self.blackCover = QWidget(self.centralwidget)
        self.blackCover.setStyleSheet("border: none;border-radius: 16px; margin: 0px;background-color: rgba(0, 0, 0, 30%);")
        self.blackCover.hide()

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

        self.CustomCommandLabel = CommandLineEdit()
        self.CustomCommandLabel.setReadOnly(True)

        commandWidget = SectionHWidget(lastOne = True)
        commandWidget.addWidget(self.CustomCommandLabel)
        commandWidget.setFixedHeight(70)


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
        self.shareButton.setFixedWidth(200)
        self.shareButton.setStyleSheet("border-radius: 8px;")
        self.shareButton.setFixedHeight(35)
        self.shareButton.clicked.connect(lambda: nativeWindowsShare(self.title.text(), f"https://marticliment.com/wingetui/share?pid={self.currentPackage.Id}^&pname={self.currentPackage.Name}^&psource={self.currentPackage.Source}", self.window()))
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
        self.baseScrollArea.setWidget(self.centralwidget)

        l = QHBoxLayout()
        l.setContentsMargins(0,0, 0, 0)
        l.addWidget(self.baseScrollArea)
        self.setLayout(l)


        self.backButton = QPushButton("", self)
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(self.width()-40, 0)
        self.backButton.resize(40, 40)
        self.backButton.setFlat(True)
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
        
        self.ApplyIcons()
        self.registeredThemeEvent = False
        
    def ApplyIcons(self):
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
        self.appIcon.setStyleSheet(f"padding: 16px; border-radius: 16px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'rgba(255, 255, 255, 60%)'};")
        self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64))
        self.shareButton.setIcon(QIcon(getMedia("share")))
        self.backButton.setIcon(QIcon(getMedia("close")))
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.screenshotsWidget.setStyleSheet(f"QScrollArea{{padding: 8px; border-radius: 8px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'rgba(255, 255, 255, 60%)'};border: 0px solid black;}};")
        self.CustomCommandLabel.ApplyIcons()
        for widget in self.imagesCarrousel:
            widget.clickableButton.setStyleSheet(f"QPushButton{{background-color: rgba(127, 127, 127, 1%);border: 0px;border-radius: 0px;}}QPushButton:hover{{background-color: rgba({'255, 255, 255' if not isDark() else '0, 0, 0'}, 10%)}}")


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

    def getInstallationOptions(self) -> InstallationOptions:
        options = InstallationOptions()
        options.RunAsAdministrator = self.adminCheckbox.isChecked()
        options.InteractiveInstallation = self.interactiveCheckbox.isChecked()
        options.SkipHashCheck = self.hashCheckBox.isChecked()
        if self.versionCombo.currentText() not in (_("Latest"), "Latest", "Loading...", _("Loading..."), ""):
            options.Version = self.versionCombo.currentText()
        if self.architectureCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            options.Architecture = self.architectureCombo.currentText()
            if options.Architecture in (_("Global"), "Global"):
                options.RunAsAdministrator = True
        if self.scopeCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            options.InstallationScope = self.scopeCombo.currentText()
        options.CustomParameters = [c for c in self.customArgumentsLineEdit.text().split(" ") if c]
        return options

    def getCommandLineParameters(self) -> list[str]:
        return self.currentPackage.PackageManager.getParameters(self.getInstallationOptions())

    def loadPackageCommandLine(self):
        parameters = " ".join(self.getCommandLineParameters())
        if self.currentPackage.isManager(Winget):
            if not "…" in self.currentPackage.Id:
                self.CustomCommandLabel.setText(f"winget {'update' if self.isAnUpdate else ('uninstall' if self.isAnUninstall else 'install')} --id {self.currentPackage.Id} --exact {parameters} --accept-source-agreements --force ".strip().replace("  ", " ").replace("  ", " "))
            else:
                self.CustomCommandLabel.setText(_("Loading..."))
        elif self.currentPackage.isManager(Scoop):
            self.CustomCommandLabel.setText(f"scoop {'update' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.currentPackage.Id} {parameters}".strip().replace("  ", " ").replace("  ", " "))
        elif self.currentPackage.isManager(Choco):
            self.CustomCommandLabel.setText(f"choco {'upgrade' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.currentPackage.Id} -y {parameters}".strip().replace("  ", " ").replace("  ", " "))
        elif self.currentPackage.isManager(Pip):
            idtoInstall = self.currentPackage.Id
            if self.versionCombo.currentText() not in ("Latest", _("Latest"), "Loading...", _("Loading...")):
                idtoInstall += "=="+self.versionCombo.currentText()
            self.CustomCommandLabel.setText(f"pip {'install --upgrade' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {idtoInstall} {parameters}".strip().replace("  ", " ").replace("  ", " "))
        elif self.currentPackage.isManager(Npm):
            self.CustomCommandLabel.setText(f"npm {'update' if self.isAnUpdate else  ('uninstall' if self.isAnUninstall else 'install')} {self.currentPackage.Id} {parameters}".strip().replace("  ", " ").replace("  ", " "))
        else:
            print(f"🟠 Unknown source {self.currentPackage.Source}")
        self.CustomCommandLabel.setCursorPosition(0)

    def showPackageDetails(self, package: Package, update: bool = False, uninstall: bool = False, installedVersion: str = ""):
        self.isAnUpdate = update
        self.isAnUninstall = uninstall
        if self.currentPackage == package:
            return
        self.currentPackage = package

        self.ApplyIcons()

        self.iv.resetImages()
        if "…" in package.Id:
            self.installButton.setEnabled(False)
            self.installButton.setText(_("Please wait..."))
        else:
            if self.isAnUpdate:
                if type(package) == UpgradablePackage:
                    installedVersion = package.Version
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
            lastVerString = f"<b>{_('Installed Version')}:</b> {package.Version} ({_('Update to {0} available').format(package.NewVersion)})"
        elif self.isAnUninstall:
            lastVerString = f"<b>{_('Installed Version')}:</b> {package.Version}"
        else:
            if package.isManager(Scoop):
                lastVerString = f"<b>{_('Current Version')}:</b> {package.Version}"
            else:
                lastVerString = f"<b>{_('Latest Version')}:</b> {package.Version}"
        self.lastver.setText(lastVerString)

        self.sha.setText(f"<b>{_('Installer SHA512') if package.isManager(Choco) or package.isManager(Npm) else _('Installer SHA256')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {package.Id}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {_('Loading...')}")
        self.date.setText(f"<b>{_('Publication date:') if package.isManager(Npm) else _('Last updated:')}</b> {_('Loading...')}")
        self.notes.setText(f"<b>{_('Notes:') if package.isManager(Scoop) else _('Release notes:')}</b> {_('Loading...')}")
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

        Capabilities =  package.PackageManager.Capabilities
        self.adminCheckbox.setEnabled(Capabilities.CanRunAsAdmin)
        self.hashCheckBox.setEnabled(Capabilities.CanSkipIntegrityChecks)
        self.interactiveCheckbox.setEnabled(Capabilities.CanRunInteractively)
        self.versionCombo.setEnabled(Capabilities.SupportsCustomVersions)
        self.versionLabel.setEnabled(Capabilities.SupportsCustomVersions)
        self.architectureCombo.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.architectureLabel.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.scopeCombo.setEnabled(Capabilities.SupportsCustomScopes)
        self.scopeLabel.setEnabled(Capabilities.SupportsCustomScopes)

        self.callInMain.emit(lambda: resetLayoutWidget())
        self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64)))
        Thread(target=self.loadPackageIcon, args=(package,)).start()

        Thread(target=self.loadPackageDetails, args=(package,), daemon=True, name=f"Loading details for {package}").start()

        self.tagsWidget.layout().clear()

        self.finishedCount = 0

    def loadPackageDetails(self, package: Package):
        details = package.PackageManager.getPackageDetails(package)
        self.callInMain.emit(lambda: self.printData(details))

    def printData(self, details: PackageDetails) -> None:
        if details.PackageObject != self.currentPackage:
            return
        package = self.currentPackage

        self.loadingProgressBar.hide()
        self.installButton.setEnabled(True)
        self.adminCheckbox.setEnabled(True)
        if self.isAnUpdate:
            self.installButton.setText(_("Update"))
        elif self.isAnUninstall:
            self.installButton.setText(_("Uninstall"))
        else:
            self.installButton.setText(_("Install"))

        self.interactiveCheckbox.setEnabled(not package.isManager(Scoop))
        self.title.setText(details.Name)
        self.description.setText(details.Description)
        if package.isManager(Winget):
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
        self.sha.setText(f"<b>{_('Installer SHA512') if package.isManager(Choco) or package.isManager(Npm) else _('Installer SHA256')} ({_('Latest Version')}):</b> {details.InstallerHash}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {details.asUrl(details.InstallerURL)} {f'({details.InstallerSize} MB)' if details.InstallerSize > 0 else ''}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {details.InstallerType}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {details.Id}")
        self.date.setText(f"<b>{_('Publication date:') if package.isManager(Npm) else _('Last updated:')}</b> {details.UpdateDate}")
        self.notes.setText(f"<b>{_('Notes:') if package.isManager(Scoop) else _('Release notes:')}</b> {details.ReleaseNotes}")
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

        for tag in details.Tags:
            label = QLabel(tag)
            label.setStyleSheet(f"padding: 5px;padding-bottom: 2px;padding-top: 2px;background-color: {blueColor if isDark() else f'rgb({getColors()[0]})'}; color: black; border-radius: 10px;")
            label.setFixedHeight(20)
            self.tagsWidget.layout().addWidget(label)

        Capabilities =  package.PackageManager.Capabilities
        self.adminCheckbox.setEnabled(Capabilities.CanRunAsAdmin)
        self.hashCheckBox.setEnabled(Capabilities.CanSkipIntegrityChecks)
        self.interactiveCheckbox.setEnabled(Capabilities.CanRunInteractively)
        self.versionCombo.setEnabled(Capabilities.SupportsCustomVersions)
        self.versionLabel.setEnabled(Capabilities.SupportsCustomVersions)
        self.architectureCombo.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.architectureLabel.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.scopeCombo.setEnabled(Capabilities.SupportsCustomScopes)
        self.scopeLabel.setEnabled(Capabilities.SupportsCustomScopes)

        self.loadPackageCommandLine()

    def loadPackageIcon(self, package: Package) -> None:
        try:
            id = package.Id
            iconId = package.getIconId()
            iconpath = os.path.join(os.path.expanduser("~"), f".wingetui/cachedmeta/{iconId}.icon.png")
            if not os.path.exists(iconpath):
                if package.isManager(Choco):
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
        if self.ignoreFutureUpdates.isChecked():
            IgnorePackageUpdates_Permanent(self.currentPackage.Id, self.currentPackage.Source)
            print(f"🟡 Blacklising package {self.currentPackage.Id}")

        if self.isAnUpdate:
            p = PackageUpdaterWidget(self.currentPackage, self.getInstallationOptions())
        elif self.isAnUninstall:
            p = PackageUninstallerWidget(self.currentPackage, self.getInstallationOptions())
        else:
            p = PackageInstallerWidget(self.currentPackage, self.getInstallationOptions())
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

    def destroy(self, destroyWindow: bool = ..., destroySubWindows: bool = ...) -> None:
        for anim in (self.leftSlow, self.leftFast, self.rightFast, self.rightSlow):
            anim: QVariantAnimation
            anim.pause()
            anim.stop()
            anim.valueChanged.disconnect()
            anim.finished.disconnect()
            anim.deleteLater()
        return super().destroy(destroyWindow, destroySubWindows)
    
    def showEvent(self, event: QShowEvent):
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        return super().showEvent(event)




if __name__ == "__main__":
    import __init__
