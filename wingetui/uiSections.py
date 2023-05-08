from __future__ import annotations
import glob # to fix NameError: name 'TreeWidgetItemWithQAction' is not defined
import sys, subprocess, time, os, json
from threading import Thread
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from storeEngine import *
from data.translations import untranslatedPercentage, languageCredits
from data.contributors import contributorsInfo

import globals
from customWidgets import *
from tools import _
from PackageManagers import PackageClasses

class DiscoverSoftwareSection(SoftwareSection):

    PackageManagers: list[PackageClasses.PackageManagerModule] = [
        Winget,
        Scoop,
        Choco
    ]
    
    PackagesLoaded: dict[PackageClasses.PackageManagerModule:bool] = {
        Winget: False,
        Scoop: False,
        Choco: False,
    }

    def __init__(self, parent = None):
        super().__init__(parent = parent)
        
        self.query.setPlaceholderText(" "+_("Search for packages"))
        self.discoverLabel.setText(_("Discover Packages"))
        self.SectionImage.setPixmap(QIcon(getMedia("desktop_download")).pixmap(QSize(64, 64)))

        self.packageList.setHeaderLabels(["", _("Package Name"), _("Package ID"), _("Version"), _("Source")])
        self.packageList.setColumnCount(5)
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
        self.informationBanner.setText(_("Chocolatey packages are being loaded. Since this is the first time, it might take a while, and they will show here once loaded."))
        
        self.installIcon = QIcon(getMedia("install"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("newversion"))
        
    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        contextMenu = QMenu(self)
        contextMenu.setParent(self)
        contextMenu.setStyleSheet("* {background: red;color: black}")
        ApplyMenuBlur(contextMenu.winId().__int__(), contextMenu)
        inf = QAction(_("Package details"))
        inf.triggered.connect(lambda: (contextMenu.close(), self.openInfo(self.packageList.currentItem())))
        inf.setIcon(QIcon(getMedia("info")))
        ins1 = QAction(_("Install"))
        ins1.setIcon(QIcon(getMedia("newversion")))
        ins1.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem()))
        ins2 = QAction(_("Install as administrator"))
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), admin=True))
        ins3 = QAction(_("Skip hash check"))
        ins3.setIcon(QIcon(getMedia("checksum")))
        ins3.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), skiphash=True))
        ins4 = QAction(_("Interactive installation"))
        ins4.setIcon(QIcon(getMedia("interactive")))
        ins4.triggered.connect(lambda: self.installPackageItem(self.packageList.currentItem(), interactive=True,))
        ins5 = QAction(_("Share this package"))
        ins5.setIcon(QIcon(getMedia("share")))
        ins5.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        contextMenu.addAction(ins1)
        contextMenu.addSeparator()
        contextMenu.addAction(ins2)
        if not "scoop" in self.packageList.currentItem().text(4).lower():
            contextMenu.addAction(ins4)
        contextMenu.addAction(ins3)
        contextMenu.addSeparator()
        contextMenu.addAction(ins5)
        contextMenu.addAction(inf)
        contextMenu.addSeparator()
        contextMenu.exec(QCursor.pos())
        
    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.installPackages = QAction(QIcon(getMedia("newversion")), _("Install selected packages"), toolbar)
        self.installPackages.triggered.connect(lambda: self.installSelectedPackageItems())
        toolbar.addAction(self.installPackages)
        
        showInfo = QAction("", toolbar)# ("Show info")
        showInfo.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        showInfo.setIcon(QIcon(getMedia("info")))
        runAsAdmin = QAction("", toolbar)# ("Run as administrator")
        runAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        runAsAdmin.triggered.connect(lambda: self.installSelectedPackageItems(admin=True))
        checksum = QAction("", toolbar)# ("Skip hash check")
        checksum.setIcon(QIcon(getMedia("checksum")))
        checksum.triggered.connect(lambda: self.installSelectedPackageItems(skiphash=True))
        interactive = QAction("", toolbar)# ("Interactive update")
        interactive.setIcon(QIcon(getMedia("interactive")))
        interactive.triggered.connect(lambda: self.installSelectedPackageItems(interactive=True))
        share = QAction("", toolbar)
        share.setIcon(QIcon(getMedia("share")))
        share.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        
        for action in [runAsAdmin, checksum, interactive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)
        
        toolbar.addSeparator()

        for action in [showInfo, share]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()
                
        def setAllSelected(checked: bool) -> None:
            itemList = []
            self.packageList.setSortingEnabled(False)
            for i in range(self.packageList.topLevelItemCount()):
                itemList.append(self.packageList.topLevelItem(i))
            for program in itemList:
                if not program.isHidden():
                    program.setCheckState(0, Qt.CheckState.Checked if checked else Qt.CheckState.Unchecked)
            self.packageList.setSortingEnabled(True)

        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        toolbar.addAction(self.selectNoneAction)
        toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)
        
        toolbar.addSeparator()

        self.importAction = QAction(_("Import packages from a file"), toolbar)
        self.importAction.setIcon(QIcon(getMedia("import")))
        self.importAction.triggered.connect(lambda: self.importPackages())
        toolbar.addAction(self.importAction)

        self.exportAction = QAction(QIcon(getMedia("export")), _("Export selected packages to a file"), toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection())
        toolbar.addAction(self.exportAction)


        tooltips = {
            self.installPackages: _("Install selected packages"),
            showInfo: _("Show package details"),
            runAsAdmin: _("Install selected packages with administrator privileges"),
            checksum: _("Skip the hash check when installing the selected packages"),
            interactive: _("Do an interactive install for the selected packages"),
            share: _("Share this package"),
            self.selectNoneAction: _("Clear selection"),
            self.importAction: _("Install packages from a file"),
            self.exportAction: _("Export selected packages to a file")
        }

        for action in tooltips.keys():
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            
        toolbar.addWidget(TenPxSpacer())
        toolbar.addWidget(TenPxSpacer())
        
        return toolbar
    
    def loadShared(self, id):
        if id in self.packages:
            package = self.packages[id]
            self.infobox.loadProgram(package["name"], id, useId=not("…" in id), store=package["store"], packageItem=package["item"], version=package["store"])
            self.infobox.show()
            cprint("shown")
        else:
            self.err = CustomMessageBox(self.window())
            errorData = {
                    "titlebarTitle": _("Unable to find package"),
                    "mainTitle": _("Unable to find package"),
                    "mainText": _("We could not load detailed information about this package, because it was not found in any of your package sources"),
                    "buttonTitle": _("Ok"),
                    "errorDetails": _("This is probably due to the fact that the package you were sent was removed, or published on a package manager that you don't have enabled. The received ID is {0}").format(id),
                    "icon": QIcon(getMedia("notif_warn")),
                }
            self.err.showErrorMessage(errorData, showNotification=False)

    def exportSelection(self) -> None:
        """
        Export all selected packages into a file.

        """
        wingetPackagesList = []
        scoopPackageList = []
        chocoPackageList = []

        try:
            for item in self.packageItems:
                if ((item.checkState(0) ==  Qt.CheckState.Checked) and item.text(4).lower() == "winget"):
                    id = item.text(2).strip()
                    wingetPackage = {"PackageIdentifier": id}
                    wingetPackagesList.append(wingetPackage)
                elif ((item.checkState(0) ==  Qt.CheckState.Checked) and "scoop" in item.text(4).lower()):
                    scoopPackage = {"Name": item.text(2)}
                    scoopPackageList.append(scoopPackage)
                elif ((item.checkState(0) ==  Qt.CheckState.Checked) and item.text(4).lower() == "chocolatey"):
                    chocoPackage = {"Name": item.text(2)}
                    chocoPackageList.append(chocoPackage)

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
            chocoExportSchema = {
                "apps": chocoPackageList,
            }
            overAllSchema = {
                "winget": wingetExportSchema,
                "scoop": scoopExportSchema,
                "chocolatey": chocoExportSchema
            }

            filename = QFileDialog.getSaveFileName(None, _("Save File"), _("wingetui exported packages"), filter='JSON (*.json)')
            if filename[0] != "":
                with open(filename[0], 'w') as f:
                    f.write(json.dumps(overAllSchema, indent=4))

        except Exception as e:
            report(e)

    def installSelectedPackageItems(self, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        for package in self.packageItems:
            try:
                if package.checkState(0) ==  Qt.CheckState.Checked:
                    self.installPackageItem(package, admin, interactive, skiphash)
            except AttributeError:
                pass

    def importPackages(self):
        try:
            packageList = []
            file = QFileDialog.getOpenFileName(None, _("Select package file"), filter="JSON (*.json)")[0]
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
                try:
                    packages = contents["chocolatey"]["apps"]
                    for pkg in packages:
                        packageList.append(pkg["Name"])
                except KeyError as e:
                    print("🟠 Invalid chocolatey section")
                for packageId in packageList:
                    try:
                        item = self.packages[packageId]["item"]
                        self.installPackageItem(item)
                    except KeyError:
                        print(f"🟠 Can't find package {packageId} in the package reference")
        except Exception as e:
            report(e)
        
    def finishLoadingIfNeeded(self) -> None:
        itemCount = len(self.packageItems)
        self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(itemCount)))
        if itemCount == 0:
            self.packageList.label.setText(self.countLabel.text())
        else:
            self.packageList.label.setText("")
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.query.setEnabled(True)
        self.finishFiltering(self.query.text())
        
        for manager in self.PackageManagers: # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return
                
        self.reloadButton.setEnabled(True)
        self.loadingProgressBar.hide()
        self.countLabel.setText(_("Found packages: {0}").format(str(itemCount)))
        self.packageList.label.setText("")
        print("🟢 Total packages: "+str(itemCount))

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
            self.packages[package.Id] = {
                "name": package.Name,
                "version": package.Version,
                "store": package.Source,
                "item": item,
                "package": package,
            }
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)
                
    def installPackageItem(self, item: QTreeWidgetItem, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        """
        Initialize the install procedure for the given package, passed as a QTreeWidgetItem. Switches: admin, interactive, skiphash
        """
        try:
            package: Package = self.packages[item.text(2)]["package"]
            if package.isWinget():
                self.addInstallation(PackageInstallerWidget(package.Name, package.Source, useId=not("…" in package.Id), packageId=package.Id, admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--ignore-security-hash" if skiphash else "", "--force"])), packageItem=package.PackageItem))
            elif package.isWinget():
                self.addInstallation(PackageInstallerWidget(package.Name, package.Source, useId=True, packageId=package.Id, admin=admin, args=list(filter(None, ["--force" if skiphash else "", "--ignore-checksums" if skiphash else "", "--notsilent" if interactive else ""])), packageItem=package.PackageItem))
            else:
                self.addInstallation(PackageInstallerWidget(package.Name, package.Source, useId=not("…" in package.Id), packageId=package.Id, admin=admin, args=["--skip" if skiphash else ""], packageItem=package.PackageItem))
        except Exception as e:
            report(e)
        
    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        packages = manager.getAvailablePackages_v2()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()
    
    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for packages..."))
        return super().startLoadingPackages(force)
    
class UpdateSoftwareSection(SoftwareSection):

    addProgram = Signal(object)
    availableUpdates: int = 0
        
    PackageManagers: list[PackageClasses.PackageManagerModule] = [
        Winget,
        Scoop,
        Choco
    ]
    
    PackagesLoaded: dict[PackageClasses.PackageManagerModule:bool] = {
        Winget: False,
        Scoop: False,
        Choco: False,
    }


    def __init__(self, parent = None):
        super().__init__(parent = parent)
        
        
        self.blacklistManager = IgnoredUpdatesManager(self.window())
        self.LegacyBlacklist = getSettingsValue("BlacklistedUpdates")

        self.query.setPlaceholderText(" "+_("Search on available updates"))
        self.SectionImage.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
        self.discoverLabel.setText(_("Software Updates"))

        self.packageList.setColumnCount(6)
        self.packageList.setHeaderLabels(["", _("Package Name"), _("Package ID"), _("Installed Version"), _("New Version"), _("Source")])
        self.packageList.setSortingEnabled(True)
        self.packageList.sortByColumn(1, Qt.SortOrder.AscendingOrder)
        
        self.packageList.itemDoubleClicked.connect(lambda item, column: (self.updatePackageItem(item) if not getSettings("DoNotUpdateOnDoubleClick") else self.openInfo(item)))            

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.setStretchLastSection(False)
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
        
        self.installIcon = QIcon(getMedia("install"))
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.newVersionIcon = QIcon(getMedia("newversion"))
        
    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        contextMenu = QMenu(self)
        contextMenu.setParent(self)
        contextMenu.setStyleSheet("* {background: red;color: black}")
        ApplyMenuBlur(contextMenu.winId().__int__(), contextMenu)
        inf = QAction(_("Package details"))
        inf.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        inf.setIcon(QIcon(getMedia("info")))
        ins1 = QAction(_("Update"))
        ins1.setIcon(QIcon(getMedia("menu_updates")))
        ins1.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem()))
        ins2 = QAction(_("Update as administrator"))
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), admin=True))
        ins3 = QAction(_("Skip hash check"))
        ins3.setIcon(QIcon(getMedia("checksum")))
        ins3.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), skiphash=True))
        ins4 = QAction(_("Interactive update"))
        ins4.setIcon(QIcon(getMedia("interactive")))
        ins4.triggered.connect(lambda: self.updatePackageItem(self.packageList.currentItem(), interactive=True))
        ins5 = QAction(_("Uninstall package"))
        ins5.setIcon(QIcon(getMedia("menu_uninstall")))
        def raiseexp():
            raise NotImplementedError("This function is not ready yet")
        ins5.triggered.connect(lambda: raiseexp)
        ins6 = QAction(_("Ignore updates for this package"))
        ins6.setIcon(QIcon(getMedia("pin")))
        ins6.triggered.connect(lambda: (IgnorePackageUpdates_Permanent(self.packageList.currentItem().text(2), self.packageList.currentItem().text(5)), self.packageList.currentItem().setHidden(True), self.packageItems.remove(self.packageList.currentItem()), self.showableItems.remove(self.packageList.currentItem()), self.packageList.takeTopLevelItem(self.packageList.indexOfTopLevelItem(self.packageList.currentItem())), self.updatePackageNumber()))
        ins8 = QAction(_("Skip this version"))
        ins8.setIcon(QIcon(getMedia("skip")))
        ins8.triggered.connect(lambda: (IgnorePackageUpdates_SpecificVersion(self.packageList.currentItem().text(2), self.packageList.currentItem().text(4), self.packageList.currentItem().text(5)), self.packageList.currentItem().setHidden(True), self.packageItems.remove(self.packageList.currentItem()), self.showableItems.remove(self.packageList.currentItem()), self.packageList.takeTopLevelItem(self.packageList.indexOfTopLevelItem(self.packageList.currentItem())), self.updatePackageNumber()))

        ins7 = QAction(_("Share this package"))
        ins7.setIcon(QIcon(getMedia("share")))
        ins7.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        contextMenu.addAction(ins1)
        contextMenu.addSeparator()
        contextMenu.addAction(ins2)
        if not "scoop" in self.packageList.currentItem().text(5).lower():
            contextMenu.addAction(ins4)
        contextMenu.addAction(ins3)
        contextMenu.addAction(ins5)
        contextMenu.addSeparator()
        contextMenu.addAction(ins6)
        contextMenu.addAction(ins8)
        contextMenu.addSeparator()
        contextMenu.addAction(ins7)
        contextMenu.addAction(inf)
        contextMenu.exec(QCursor.pos())

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
                    except AttributeError:
                        pass
            self.updatePackageNumber()

        def setAllSelected(checked: bool) -> None:
            itemList: list[TreeWidgetItemWithQAction] = []
            self.packageList.setSortingEnabled(False)
            for i in range(self.packageList.topLevelItemCount()):
                itemList.append(self.packageList.topLevelItem(i))
            for program in itemList:
                if not program.isHidden():
                    program.setCheckState(0, Qt.CheckState.Checked if checked else Qt.CheckState.Unchecked)
            self.packageList.setSortingEnabled(True)


        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("menu_updates")), _("Update selected packages"), toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.updateSelectedPackageItems())
        toolbar.addAction(self.upgradeSelected)
        
        showInfo = QAction("", toolbar)
        showInfo.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        showInfo.setIcon(QIcon(getMedia("info")))
        runAsAdmin = QAction("", toolbar)
        runAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        runAsAdmin.triggered.connect(lambda: self.updateSelectedPackageItems(admin=True))
        checksum = QAction("", toolbar)
        checksum.setIcon(QIcon(getMedia("checksum")))
        checksum.triggered.connect(lambda: self.updateSelectedPackageItems(skiphash=True))
        interactive = QAction("", toolbar)
        interactive.setIcon(QIcon(getMedia("interactive")))
        interactive.triggered.connect(lambda: self.updateSelectedPackageItems(interactive=True))
        share = QAction("", toolbar)
        share.setIcon(QIcon(getMedia("share")))
        share.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        for action in [runAsAdmin, checksum, interactive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        for action in [showInfo, share]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)
            
        toolbar.addSeparator()

        self.upgradeAllAction = QAction(QIcon(getMedia("installall")), "", toolbar)
        self.upgradeAllAction.triggered.connect(lambda: self.updateAllPackageItems()) 
        # self.upgradeAllAction is Required for the systray context menu

        self.selectAllAction = QAction(QIcon(getMedia("selectall")), "", toolbar)
        self.selectAllAction.triggered.connect(lambda: setAllSelected(True))
        toolbar.addAction(self.selectAllAction)
        toolbar.widgetForAction(self.selectAllAction).setFixedSize(40, 45)
        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        toolbar.addAction(self.selectNoneAction)
        toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)
        toolbar.widgetForAction(self.selectNoneAction).setToolTip(_("Clear selection"))
        toolbar.widgetForAction(self.selectAllAction).setToolTip(_("Select all"))

        toolbar.addSeparator()

        self.blacklistAction = QAction(QIcon(getMedia("pin")), _("Ignore selected packages"), toolbar)
        self.blacklistAction.triggered.connect(lambda: blacklistSelectedPackages())
        toolbar.addAction(self.blacklistAction)
        self.resetBlackList = QAction(QIcon(getMedia("blacklist")), _("Manage ignored updates"), toolbar)
        self.resetBlackList.triggered.connect(lambda: (self.blacklistManager.show()))
        toolbar.addAction(self.resetBlackList)


        tooltips = {
            self.upgradeSelected: _("Update selected packages"),
            showInfo: _("Show package details"),
            runAsAdmin: _("Update selected packages with administrator privileges"),
            checksum: _("Skip the hash check when updating the selected packages"),
            interactive: _("Do an interactive update for the selected packages"),
            share: _("Share this package"),
            self.selectAllAction: _("Select all packages"),
            self.selectNoneAction: _("Clear selection"),
            self.resetBlackList: _("Manage ignored packages"),
            self.blacklistAction: _("Ignore updates for the selected packages")
        }
            
        for action in tooltips.keys():
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            
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
            if getSettings("AutomaticallyUpdatePackages") or "--updateapps" in sys.argv:
                self.updateAllPackageItems()
                t = ToastNotification(self, self.callInMain.emit)
                if count > 1:
                    t.setTitle(_("Updates found!"))
                    t.setDescription(_("{0} packages are being updated").format(count))
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1)+", "
                    t.setSmallText(packageList[:-2])
                elif count == 1:
                    t.setTitle(_("Update found!"))
                    t.setDescription(_("{0} is being updated").format(lastVisibleItem.text(1)))
                t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(1)))
                if ENABLE_UPDATES_NOTIFICATIONS:
                    t.show() 
                    
            else:            
                t = ToastNotification(self, self.callInMain.emit)
                if count > 1:
                    t.setTitle(_("Updates found!"))
                    t.setDescription(_("{0} packages can be updated").format(count)+":")
                    t.addAction(_("Update all"), self.updateAllPackageItems)
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1)+", "
                    t.setSmallText(packageList[:-2])
                elif count == 1:
                    t.setTitle(_("Update found!"))
                    t.setDescription(_("{0} can be updated").format(lastVisibleItem.text(1)))
                    t.addAction(_("Update"), self.updateAllPackageItems)
                t.addAction(_("Show WingetUI"), lambda: (globals.mainWindow.showWindow(1)))
                t.addOnClickCallback(lambda: (globals.mainWindow.showWindow(1)))
                if ENABLE_UPDATES_NOTIFICATIONS:
                    t.show()
                    
            globals.trayIcon.setIcon(QIcon(getMedia("greenicon")))
            self.packageList.label.setText("")
        else:
            globals.trayIcon.setIcon(QIcon(getMedia("greyicon")))
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
            package.Source = globals.uninstall.packages[package.Id]["store"]
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
            package.PackageItem = item
            if package.isScoop():
                try:
                    if version == globals.uninstall.packages[package.Id]["version"]:
                        package.Source = globals.uninstall.packages[package.Id]["store"]
                    item.setText(5, package.Source)
                except KeyError as e:
                    item.setText(5, _("Loading..."))
                    print(f"🟡 Package {package.Id} found in the updates section but not in the installed one, might be a temporal issue, retrying in 3 seconds...")
                    Thread(target=self.changeStore, args=(package)).start()
            else:
                item.setText(5, package.Source)
            item.setIcon(5, package.getSourceIcon())

            self.packages[package.Id] = {
                "name": package.Name,
                "version": package.Version,
                "newVersion": package.NewVersion,
                "store": package.Source,
                "item": item,
                "package": package,
            }
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)
            action = QAction(package.Name+"  \t"+package.Version+"\t → \t"+package.NewVersion, globals.trayMenuUpdatesList)
            action.triggered.connect(lambda : self.updatePackageItem(item))
            action.setShortcut(package.Version)
            item.setAction(action)
            globals.trayMenuUpdatesList.addAction(action)

    def finishFiltering(self, text: str):
        def getChecked(item: QTreeWidgetItem) -> str:
            return "" if item.checkState(0) == Qt.CheckState.Checked else " "
        def getTitle(item: QTreeWidgetItem) -> str:
            return item.text(1)
        def getID(item: QTreeWidgetItem) -> str:
            return item.text(2)
        def getVersion(item: QTreeWidgetItem) -> str:
            return item.text(3)
        def getNewVersion(item: QTreeWidgetItem) -> str:
            return item.text(4)
        def getSource(item: QTreeWidgetItem) -> str:
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
        trayIconToolTip = ""
        trayMenuText = ""
        if self.availableUpdates > 0:
            if self.availableUpdates == 1:
                trayIconToolTip = _("WingetUI - 1 update is available")
            else:
                trayIconToolTip = _("WingetUI - {0} updates are available").format(self.availableUpdates)
            trayMenuText = _("Available updates: {0}").format(self.availableUpdates)
            self.packageList.label.hide()
            self.packageList.label.setText("")
            self.SectionImage.setPixmap(QIcon(getMedia("alert_laptop")).pixmap(QSize(64, 64)))
            globals.updatesAction.setIcon(QIcon(getMedia("alert_laptop")))
            globals.app.uaAction.setEnabled(True)
            globals.trayMenuUpdatesList.menuAction().setEnabled(True)
            globals.trayIcon.setIcon(QIcon(getMedia("greenicon")))
        else:
            trayIconToolTip = _("WingetUI - Everything is up to date")
            trayMenuText = _("No updates are available")
            self.packageList.label.setText(_("Hooray! No updates were found!"))
            self.packageList.label.show()
            globals.app.uaAction.setEnabled(False)
            globals.trayMenuUpdatesList.menuAction().setEnabled(False)
            globals.updatesAction.setIcon(QIcon(getMedia("checked_laptop")))
            globals.trayIcon.setIcon(QIcon(getMedia("greyicon")))
            self.SectionImage.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
        globals.trayIcon.setToolTip(trayIconToolTip)
        globals.trayMenuUpdatesList.menuAction().setText(trayMenuText)
    
    def updateAllPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden():
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updateSelectedPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden() and item.checkState(0) ==  Qt.CheckState.Checked:
                self.updatePackageItem(item, admin, skiphash, interactive)
                
    def updatePackageItem(self, packageItem: TreeWidgetItemWithQAction, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        try:
            package: UpgradablePackage = self.packages[packageItem.text(2)]["package"]
            if package.isWinget():
                    self.addInstallation(PackageUpdaterWidget(package.Name, "winget", useId=not("…" in package.Id), packageId=package.Id, packageItem=packageItem, admin=admin, args=list(filter(None, ["--interactive" if interactive else "--silent", "--ignore-security-hash" if skiphash else "", "--force"])), currentVersion=package.Version, newVersion=package.NewVersion))
            elif package.isChocolatey():
                self.addInstallation(PackageUpdaterWidget(package.Name, "chocolatey", useId=True, packageId=package.Id, admin=admin, args=list(filter(None, ["--force" if skiphash else "", "--ignore-checksums" if skiphash else "", "--notsilent" if interactive else ""])), packageItem=packageItem, currentVersion=package.Version, newVersion=package.NewVersion))
            else:
                self.addInstallation(PackageUpdaterWidget(package.Name, package.Source,  useId=not("…" in package.Id), packageId=package.Id, packageItem=packageItem, admin=admin, args=["--skip" if skiphash else ""], currentVersion=package.Version, newVersion=package.NewVersion))
        except Exception as e:
            report(e)
     
    def reloadSources(self):
        print("Reloading sources...")
        try:
            o1 = subprocess.run(f"powershell -Command scoop update", shell=True, stdout=subprocess.PIPE)
            print("Updated scoop packages with result", o1.returncode)
            o2 = subprocess.run(f"{Winget.EXECUTABLE} source update --name winget", shell=True, stdout=subprocess.PIPE)
            print("Updated Winget packages with result", o2.returncode)
            o2 = subprocess.run(f"{Choco.EXECUTABLE} source update --name winget", shell=True, stdout=subprocess.PIPE)
        except Exception as e:
            report(e)
        self.callInMain.emit(self.startLoadingPackages)
    
    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        packages = manager.getAvailableUpdates_v2()
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

class UninstallSoftwareSection(SoftwareSection):
    
    allPkgSelected: bool = False
    
    PackageManagers: list[PackageClasses.PackageManagerModule] = [
        Winget,
        Scoop,
        Choco
    ]
    
    PackagesLoaded: dict[PackageClasses.PackageManagerModule:bool] = {
        Winget: False,
        Scoop: False,
        Choco: False,
    }

    
    def __init__(self, parent = None):
        super().__init__(parent = parent)
        self.query.setPlaceholderText(" "+_("Search on your software"))
        self.SectionImage.setPixmap(QIcon(getMedia("workstation")).pixmap(QSize(64, 64)))
        self.discoverLabel.setText(_("Installed Packages"))

        self.headers = ["", _("Package Name"), _("Package ID"), _("Installed Version"), _("Source")] # empty header added for checkbox
        self.packageList.setHeaderLabels(self.headers)
        self.packageList.setColumnCount(5)
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
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))

    def showContextMenu(self, pos: QPoint) -> None:
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
        ins1.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem()))
        ins2 = QAction(_("Uninstall as administrator"))
        ins2.setIcon(QIcon(getMedia("runasadmin")))
        ins2.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), admin=True))
        ins3 = QAction(_("Remove permanent data"))
        ins3.setIcon(QIcon(getMedia("menu_close")))
        ins3.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), removeData=True))
        ins5 = QAction(_("Interactive uninstall"))
        ins5.setIcon(QIcon(getMedia("interactive")))
        ins5.triggered.connect(lambda: self.uninstallPackageItem(self.packageList.currentItem(), interactive=True))
        ins7 = QAction(_("Ignore updates for this package"))
        ins7.setIcon(QIcon(getMedia("pin")))
        ins7.triggered.connect(lambda: (IgnorePackageUpdates_Permanent(self.packageList.currentItem().text(2), self.packageList.currentItem().text(4))))
        ins4 = QAction(_("Package details"))
        ins4.setIcon(QIcon(getMedia("info")))
        ins4.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        ins6 = QAction(_("Share this package"))
        ins6.setIcon(QIcon(getMedia("share")))
        ins6.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))
        contextMenu.addAction(ins1)
        contextMenu.addSeparator()
        contextMenu.addAction(ins2)
        if "scoop" in self.packageList.currentItem().text(4).lower():
            contextMenu.addAction(ins3)
            contextMenu.addSeparator()
        else:
            contextMenu.addAction(ins5)
        if self.packageList.currentItem().text(4) not in ((_("Local PC"), "Microsoft Store", "Steam", "GOG", "Ubisoft Connect")):
            contextMenu.addSeparator()
            contextMenu.addAction(ins7)
            contextMenu.addSeparator()
            contextMenu.addAction(ins6)
            contextMenu.addAction(ins4)

        contextMenu.exec(QCursor.pos())

    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.upgradeSelected = QAction(QIcon(getMedia("menu_uninstall")), _("Uninstall selected packages"), toolbar)
        self.upgradeSelected.triggered.connect(lambda: self.uninstallSelected())
        toolbar.addAction(self.upgradeSelected)

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
                self.openInfo(item)

        showInfoAction = QAction("", toolbar)# ("Show info")
        showInfoAction.triggered.connect(showInfo)
        showInfoAction.setIcon(QIcon(getMedia("info")))
        runAsAdmin = QAction("", toolbar)# ("Run as administrator")
        runAsAdmin.setIcon(QIcon(getMedia("runasadmin")))
        runAsAdmin.triggered.connect(lambda: self.uninstallSelected(admin=True))
        interactive = QAction("", toolbar)# ("Interactive uninstall")
        interactive.setIcon(QIcon(getMedia("interactive")))
        interactive.triggered.connect(lambda: self.uninstallSelected(interactive=True))
        share = QAction("", toolbar)
        share.setIcon(QIcon(getMedia("share")))
        share.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        for action in [runAsAdmin, interactive]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        for action in [showInfoAction, share]:
            toolbar.addAction(action)
            toolbar.widgetForAction(action).setFixedSize(40, 45)

        toolbar.addSeparator()

        def setAllSelected(checked: bool) -> None:
            itemList = []
            self.packageList.setSortingEnabled(False)
            for i in range(self.packageList.topLevelItemCount()):
                itemList.append(self.packageList.topLevelItem(i))
            for program in itemList:
                if not program.isHidden():
                    program.setCheckState(0, Qt.CheckState.Checked if checked else Qt.CheckState.Unchecked)
            self.packageList.setSortingEnabled(True)

        self.selectAllAction = QAction(QIcon(getMedia("selectall")), "", toolbar)
        self.selectAllAction.triggered.connect(lambda: setAllSelected(True))
        toolbar.addAction(self.selectAllAction)
        toolbar.widgetForAction(self.selectAllAction).setFixedSize(40, 45)
        self.selectNoneAction = QAction(QIcon(getMedia("selectnone")), "", toolbar)
        self.selectNoneAction.triggered.connect(lambda: setAllSelected(False))
        toolbar.addAction(self.selectNoneAction)
        toolbar.widgetForAction(self.selectNoneAction).setFixedSize(40, 45)
        toolbar.widgetForAction(self.selectNoneAction).setToolTip(_("Clear selection"))
        toolbar.widgetForAction(self.selectAllAction).setToolTip(_("Select all"))

        toolbar.addSeparator()

        self.exportSelectedAction = QAction(QIcon(getMedia("export")), _("Export selected packages to a file"), toolbar)
        self.exportSelectedAction.triggered.connect(lambda: self.exportSelection())
        toolbar.addAction(self.exportSelectedAction)

        self.exportAction = QAction(QIcon(getMedia("export")), _("Export all"), toolbar)
        self.exportAction.triggered.connect(lambda: self.exportSelection(all=True))
        #toolbar.addAction(self.exportAction)

        w = QWidget()
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        toolbar.addWidget(w)
        toolbar.addWidget(TenPxSpacer())
        toolbar.addWidget(TenPxSpacer())
         
        tooltips = {
            self.upgradeSelected: _("Uninstall selected packages"),
            showInfoAction: _("Show package details"),
            runAsAdmin: _("Uninstall the selected packages with administrator privileges"),
            interactive: _("Do an interactive uninstall for the selected packages"),
            share: _("Share this package"),
            self.selectNoneAction: _("Clear package selection"),
            self.selectAllAction: _("Select all packages"),
            self.exportSelectedAction: _("Export selected packages to a file")
        }

        for action in tooltips.keys():
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])

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
        
    def confirmUninstallSelected(self, toUninstall: list[TreeWidgetItemWithQAction], a: CustomMessageBox):
        questionData = {
            "titlebarTitle": "Wait!",
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
                self.callInMain.emit(partial(self.uninstallPackageItem, program, avoidConfirm=True))

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
            if package.isWinget():
                for illegal_char in ("{", "}", " "):
                    if illegal_char in package.Id:
                        package.Source = _("Local PC")
                        break
                
                if package.Source.lower() == "winget":
                    if package.Id.count(".") != 1:
                        package.Source = (_("Local PC"))
                        if package.Id.count(".") > 1:
                            for letter in package.Id:
                                if letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ":
                                    package.Source = "Winget"
                                    break
                
                if package.Source == (_("Local PC")):
                    if package.Id == "Steam":
                        package.Source = "Steam"
                    if package.Id == "Uplay":
                        package.Source = "Ubisoft Connect"
                    if package.Id.count("_is1") == 1:
                        package.Source = "GOG"
                        for number in package.Id.split("_is1")[0]:
                            if number not in "0123456789":
                                package.Source = (_("Local PC"))
                                break
                        if len(package.Id) != 14:
                            package.Source = (_("Local PC"))
                        if package.Id.count("GOG") == 1:
                            package.Source = "GOG"
                
                if package.Source.lower() == "winget":
                    if len(package.Id.split("_")[-1]) == 13 and len(package.Id.split("_"))==2:
                        package.Source = "Microsoft Store"
                    elif len(package.Id.split("_")[-1]) <= 13 and len(package.Id.split("_"))==2 and "…" == package.Id.split("_")[-1][-1]: # Delect microsoft store ellipsed packages 
                        package.Source = "Microsoft Store"

            item.setCheckState(0, Qt.CheckState.Unchecked)
            item.setText(1, package.Name)
            item.setIcon(1, self.installIcon)
            item.setText(2, package.Id)
            item.setIcon(2, self.IDIcon)
            item.setText(3, package.Version)
            item.setIcon(3, self.versionIcon)
            item.setText(4, package.Source)
            item.setIcon(4, package.getSourceIcon())
            self.packages[package.Id] = {
                "name": package.Name,
                "version": package.Version,
                "store": package.Source,
                "item": item,
                "package": package
            }
            
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
            "titlebarTitle": "Wait!",
            "mainTitle": _("Are you sure?"),
            "mainText": _("Do you really want to uninstall {0}?").format(toUninstall[0].text(1)) if len(toUninstall) == 1 else  _("Do you really want to uninstall {0} packages?").format(len(toUninstall)),
            "acceptButtonTitle": _("Yes"),
            "cancelButtonTitle": _("No"),
            "icon": QIcon(),
        }
        if a.askQuestion(questionData):
            for program in toUninstall:
                self.callInMain.emit(partial(self.uninstallPackageItem, program, admin, interactive, removeData, avoidConfirm=True))

    def uninstall(self, title: str, id: str, store: str, packageItem: TreeWidgetItemWithQAction = None, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        self.uninstallPackageItem(self.packages[id]["item"], admin, removeData, interactive, avoidConfirm)

    def uninstallPackageItem(self, packageItem: TreeWidgetItemWithQAction, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        package: Package = self.packages[packageItem.text(2)]["package"]
        if not avoidConfirm:
            a = CustomMessageBox(self)
            Thread(target=self.confirmUninstallSelected, args=([packageItem], a, admin, interactive, removeData)).start()
        else:
            print("🔵 Uninstalling", id)
            if package.isWinget():
                self.addInstallation(PackageUninstallerWidget(package.Name, "winget", useId=not("…" in package.Id), packageId=package.Id, packageItem=packageItem, admin=admin, removeData=removeData, args=["--interactive" if interactive else "--silent", "--force"]))
            elif package.isChocolatey():
                self.addInstallation(PackageUninstallerWidget(package.Name, "chocolatey", useId=True, packageId=package.Id, admin=admin, packageItem=packageItem, args=list(filter(None, ["--notsilent" if interactive else ""]))))
            else: # Scoop
                self.addInstallation(PackageUninstallerWidget(package.Name, package.Source, useId=not("…" in package.Id), packageId=package.Id, packageItem=packageItem, admin=admin, removeData=removeData))

    def loadPackages(self, manager: PackageClasses.PackageManagerModule) -> None:
        packages = manager.getInstalledPackages_v2()
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
    
    def exportSelection(self, all: bool = False) -> None:
        """
        Export all selected packages into a file.

        """
        wingetPackagesList = []
        scoopPackageList = []
        chocoPackageList = []

        try:
            for item in self.packageItems:
                if ((item.checkState(0) ==  Qt.CheckState.Checked or all) and item.text(4).lower() == "winget"):
                    id = item.text(2).strip()
                    wingetPackage = {"PackageIdentifier": id}
                    wingetPackagesList.append(wingetPackage)
                elif ((item.checkState(0) ==  Qt.CheckState.Checked or all) and "scoop" in item.text(4).lower()):
                    scoopPackage = {"Name": item.text(2)}
                    scoopPackageList.append(scoopPackage)
                elif ((item.checkState(0) ==  Qt.CheckState.Checked or all) and item.text(4).lower() == "chocolatey"):
                    chocoPackage = {"Name": item.text(2)}
                    chocoPackageList.append(chocoPackage)

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
            chocoExportSchema = {
                "apps": chocoPackageList,
            }
            overAllSchema = {
                "winget": wingetExportSchema,
                "scoop": scoopExportSchema,
                "chocolatey": chocoExportSchema
            }

            filename = QFileDialog.getSaveFileName(None, _("Save File"), _("wingetui exported packages"), filter='JSON (*.json)')
            if filename[0] != "":
                with open(filename[0], 'w') as f:
                    f.write(json.dumps(overAllSchema, indent=4))

        except Exception as e:
            report(e)

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
            table.setRowCount(4)
            table.setEnabled(False)
            table.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
            table.setShowGrid(False)
            table.setHorizontalHeaderLabels([("" if isDark() else "   ")+_("Status"), _("Version")])
            table.setColumnWidth(1, 500)
            table.setColumnWidth(0, 150)
            table.verticalHeader().setFixedWidth(100)
            table.setVerticalHeaderLabels(["Winget ", "Scoop ", "Chocolatey ", " GSudo "])
            table.setItem(0, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["wingetFound"] else _("Not found")))
            table.setItem(0, 1, QTableWidgetItem(" "+str(globals.componentStatus["wingetVersion"])))
            table.setItem(1, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["scoopFound"] else _("Not found")))
            table.setItem(1, 1, QTableWidgetItem(" "+str(globals.componentStatus["scoopVersion"])))
            table.setItem(2, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["chocoFound"] else _("Not found")))
            table.setItem(2, 1, QTableWidgetItem(" "+str(globals.componentStatus["chocoVersion"])))
            table.setItem(3, 0, QTableWidgetItem("  "+_("Found") if globals.componentStatus["sudoFound"] else _("Not found")))
            table.setItem(3, 1, QTableWidgetItem(" "+str(globals.componentStatus["sudoVersion"])))
            table.horizontalHeaderItem(0).setTextAlignment(Qt.AlignLeft)
            table.setRowHeight(0, 35)
            table.setRowHeight(1, 35)
            table.setRowHeight(2, 35)
            table.setRowHeight(3, 35)
            table.horizontalHeaderItem(1).setTextAlignment(Qt.AlignLeft)
            table.verticalHeaderItem(0).setTextAlignment(Qt.AlignRight)
            table.verticalHeaderItem(1).setTextAlignment(Qt.AlignRight)
            table.verticalHeaderItem(2).setTextAlignment(Qt.AlignRight)
            table.verticalHeaderItem(3).setTextAlignment(Qt.AlignRight)
            table.setCornerWidget(QLabel(""))
            table.setCornerButtonEnabled(False)
            table.setFixedHeight(190)
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
            self.layout.addWidget(QLinkLabel(f"Win23mica:&thinsp;{_('MIT License')}:&thinsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a style=\"color: {blueColor};\" href=\"https://github.com/marticliment/win32mica/blob/main/LICENSE\">https://github.com/marticliment/win32mica/blob/main/LICENSE</a>"))
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

        def restartElevenClockByLangChange():
            subprocess.run(str("start /B \"\" \""+sys.executable)+"\"", shell=True)
            globals.app.quit()

        self.language.restartButton.clicked.connect(restartElevenClockByLangChange)
        self.language.combobox.currentTextChanged.connect(changeLang)
        
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
        changeDefaultInstallAction = SectionCheckBox(_("Directly install when double-clicking an item on the Discover Software tab (instead of showing the package info)"))
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
        
        def resetAdminRightsCache():
            resetsudo = subprocess.Popen([GSUDO_EXE_PATH, "-k"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True, cwd=GSUDO_EXE_LOCATION, env=os.environ)
            resetsudo.wait()
            globals.adminRightsGranted = False
        
        doCacheAdminPrivileges.stateChanged.connect(lambda v: (setSettings("DoCacheAdminRights", bool(v)), resetAdminRightsCache()))
        self.advancedOptions.addWidget(doCacheAdminPrivileges)
        alwaysRunWingetAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm="Winget"))
        alwaysRunWingetAsAdmin.setChecked(getSettings("AlwaysElevateWinget"))
        alwaysRunWingetAsAdmin.stateChanged.connect(lambda v: setSettings("AlwaysElevateWinget", bool(v)))
        self.advancedOptions.addWidget(alwaysRunWingetAsAdmin)
        alwaysRunScoopAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm="Scoop"))
        alwaysRunScoopAsAdmin.setChecked(getSettings("AlwaysElevateScoop"))
        alwaysRunScoopAsAdmin.stateChanged.connect(lambda v: setSettings("AlwaysElevateScoop", bool(v)))
        self.advancedOptions.addWidget(alwaysRunScoopAsAdmin)
        alwaysRunChocolateyAsAdmin = SectionCheckBox(_("Always elevate {pm} installations by default").format(pm="Chocolatey"))
        alwaysRunChocolateyAsAdmin.setChecked(getSettings("AlwaysElevateChocolatey"))
        alwaysRunChocolateyAsAdmin.stateChanged.connect(lambda v: setSettings("AlwaysElevateChocolatey", bool(v)))
        alwaysRunChocolateyAsAdmin.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        self.advancedOptions.addWidget(alwaysRunChocolateyAsAdmin)
        dontUseBuiltInGsudo = SectionCheckBox(_("Use installed GSudo instead of the bundled one (requires app restart)"))
        dontUseBuiltInGsudo.setChecked(getSettings("UseUserGSudo"))
        dontUseBuiltInGsudo.stateChanged.connect(lambda v: setSettings("UseUserGSudo", bool(v)))
        self.advancedOptions.addWidget(dontUseBuiltInGsudo)

        self.advancedOptions = CollapsableSection(_("Experimental settings and developer options"), getMedia("testing"), _("Beta features and other options that shouldn't be touched"))
        self.layout.addWidget(self.advancedOptions)
        disableShareApi = SectionCheckBox(_("Disable new share API (port 7058)"))
        disableShareApi.setChecked(getSettings("DisableApi"))
        disableShareApi.stateChanged.connect(lambda v: setSettings("DisableApi", bool(v)))
        self.advancedOptions.addWidget(disableShareApi)

        enableSystemWinget = SectionCheckBox(_("Use system Winget (Needs a restart)"))
        enableSystemWinget.setChecked(getSettings("UseSystemWinget"))
        enableSystemWinget.stateChanged.connect(lambda v: setSettings("UseSystemWinget", bool(v)))
        self.advancedOptions.addWidget(enableSystemWinget)
        disableLangUpdates = SectionCheckBox(_("Do not download new app translations from GitHub automatically"))
        disableLangUpdates.setChecked(getSettings("DisableLangAutoUpdater"))
        disableLangUpdates.stateChanged.connect(lambda v: setSettings("DisableLangAutoUpdater", bool(v)))
        self.advancedOptions.addWidget(disableLangUpdates)
        resetyWingetUICache = SectionButton(_("Reset WingetUI icon and screenshot cache"), _("Reset"))
        resetyWingetUICache.clicked.connect(lambda: (shutil.rmtree(os.path.join(os.path.expanduser("~"), ".wingetui/cachedmeta/")), notify("WingetUI", _("Cache was reset successfully!"))))
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
            restartElevenClockByLangChange()
        
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
        disableWinget = SectionCheckBox(_("Enable {pm}").format(pm = "Winget"))
        disableWinget.setChecked(not getSettings("DisableWinget"))
        disableWinget.stateChanged.connect(lambda v: (setSettings("DisableWinget", not bool(v)), parallelInstalls.setEnabled(v), button.setEnabled(v), enableSystemWinget.setEnabled(v)))
        self.wingetPreferences.addWidget(disableWinget)

        parallelInstalls = SectionCheckBox(_("Allow parallel installs (NOT RECOMMENDED)"))
        parallelInstalls.setChecked(getSettings("AllowParallelInstalls"))
        parallelInstalls.stateChanged.connect(lambda v: setSettings("AllowParallelInstalls", bool(v)))
        self.wingetPreferences.addWidget(parallelInstalls)
        button = SectionButton(_("Reset Winget sources (might help if no packages are listed)"), _("Reset"))
        button.clicked.connect(lambda: (os.startfile(os.path.join(realpath, "resources/reset_winget_sources.cmd"))))
        self.wingetPreferences.addWidget(button)
        button.setStyleSheet("QWidget#stChkBg{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;border-bottom: 1px;}")
        
        parallelInstalls.setEnabled(disableWinget.isChecked())
        button.setEnabled(disableWinget.isChecked())
        enableSystemWinget.setEnabled(disableWinget.isChecked())
        
        resetCache = SectionButton(_("Reset {pm} cache").format(pm=Winget.NAME), _("Reset"))
        resetCache.clicked.connect(lambda: (os.remove(Winget.CAHCE_FILE), notify("WingetUI", _("Cache was reset successfully!"))))
        self.wingetPreferences.addWidget(resetCache)

        self.scoopPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Scoop"), getMedia("scoop"), _("{pm} package manager specific preferences").format(pm = "Scoop"))
        self.layout.addWidget(self.scoopPreferences)

        disableScoop = SectionCheckBox(_("Enable {pm}").format(pm = "Scoop"))
        disableScoop.setChecked(not getSettings("DisableScoop"))
        disableScoop.stateChanged.connect(lambda v: (setSettings("DisableScoop", not bool(v)), scoopPreventCaps.setEnabled(v), bucketManager.setEnabled(v), uninstallScoop.setEnabled(v), enableScoopCleanup.setEnabled(v)))
        self.scoopPreferences.addWidget(disableScoop)
        scoopPreventCaps = SectionCheckBox(_("Show Scoop packages in lowercase"))
        scoopPreventCaps.setChecked(getSettings("LowercaseScoopApps"))
        scoopPreventCaps.stateChanged.connect(lambda v: setSettings("LowercaseScoopApps", bool(v)))
        self.scoopPreferences.addWidget(scoopPreventCaps)
        bucketManager = ScoopBucketManager()
        bucketManager.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        self.scoopPreferences.addWidget(bucketManager)
        installScoop = SectionButton(_("Install Scoop"), _("Install"))
        installScoop.setStyleSheet("QWidget#stBtn{border-bottom-left-radius: 0;border-bottom-right-radius: 0;border-bottom: 0;}")
        installScoop.clicked.connect(lambda: (setSettings("DisableScoop", False), disableScoop.setChecked(False), os.startfile(os.path.join(realpath, "resources/install_scoop.cmd"))))
        self.scoopPreferences.addWidget(installScoop)
        uninstallScoop = SectionButton(_("Uninstall Scoop (and its packages)"), _("Uninstall"))
        uninstallScoop.clicked.connect(lambda: (setSettings("DisableScoop", True), disableScoop.setChecked(True), os.startfile(os.path.join(realpath, "resources/uninstall_scoop.cmd"))))
        self.scoopPreferences.addWidget(uninstallScoop)
        
        scoopPreventCaps.setEnabled(disableScoop.isChecked())
        bucketManager.setEnabled(disableScoop.isChecked())
        uninstallScoop.setEnabled(disableScoop.isChecked())
        enableScoopCleanup.setEnabled(disableScoop.isChecked())
        resetCache = SectionButton(_("Reset {pm} cache").format(pm=Scoop.NAME), _("Reset"))
        resetCache.clicked.connect(lambda: (os.remove(Scoop.CAHCE_FILE), notify("WingetUI", _("Cache was reset successfully!"))))
        self.scoopPreferences.addWidget(resetCache)
        
        self.chocoPreferences = CollapsableSection(_("{pm} preferences").format(pm = "Chocolatey"), getMedia("choco"), _("{pm} package manager specific preferences").format(pm = "Chocolatey"))
        self.layout.addWidget(self.chocoPreferences)
        disableChocolatey = SectionCheckBox(_("Enable {pm}").format(pm = "Chocolatey"))
        disableChocolatey.setChecked(not getSettings("DisableChocolatey"))
        disableChocolatey.stateChanged.connect(lambda v: (setSettings("DisableChocolatey", not bool(v))))
        self.chocoPreferences.addWidget(disableChocolatey)
        enableSystemChocolatey = SectionCheckBox(_("Use system Chocolatey (Needs a restart)"))
        enableSystemChocolatey.setChecked(getSettings("UseSystemChocolatey"))
        enableSystemChocolatey.stateChanged.connect(lambda v: setSettings("UseSystemChocolatey", bool(v)))
        self.chocoPreferences.addWidget(enableSystemChocolatey)
        resetCache = SectionButton(_("Reset {pm} cache").format(pm=Choco.NAME), _("Reset"))
        resetCache.clicked.connect(lambda: (os.remove(Choco.CAHCE_FILE), notify("WingetUI", _("Cache was reset successfully!"))))
        self.chocoPreferences.addWidget(resetCache)

        self.layout.addStretch()

        print("🟢 Settings tab loaded!")
        
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
                a.triggered.connect(lambda: (print("🔵 Reloading log..."), self.setPlainText(stdout_buffer.getvalue()), self.verticalScrollBar().setValue(self.verticalScrollBar().maximum())))
                menu.addAction(a)

                
                a4 = QAction()
                a4.setText(_("Show missing translation strings"))
                a4.triggered.connect(lambda: self.setPlainText('\n'.join(MissingTranslationList)))#buffer.getvalue()))
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

        self.textEdit.setPlainText(stdout_buffer.getvalue())

        reloadButton = QPushButton(_("Reload log"))
        reloadButton.setFixedWidth(200)        
        reloadButton.clicked.connect(lambda: (print("🔵 Reloading log..."), self.textEdit.setPlainText(stdout_buffer.getvalue()), self.textEdit.verticalScrollBar().setValue(self.textEdit.verticalScrollBar().maximum())))

        def saveLog():
            try:
                print("🔵 Saving log...")
                f = QFileDialog.getSaveFileName(None, _("Export log"), os.path.expanduser("~"), f"{_('Text file')} (*.txt)")
                if f[0]:
                    fpath = f[0]
                    if not ".txt" in fpath.lower():
                        fpath += ".txt"
                    with open(fpath, "wb") as fobj:
                        fobj.write(stdout_buffer.getvalue().encode("utf-8"))
                        fobj.close()
                    os.startfile(fpath)
                    print("🟢 log saved successfully")
                    self.textEdit.setPlainText(stdout_buffer.getvalue())
                else:
                    print("🟡 log save cancelled!")
                    self.textEdit.setPlainText(stdout_buffer.getvalue())
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(stdout_buffer.getvalue())

        exportButtom = QPushButton(_("Export log as a file"))
        exportButtom.setFixedWidth(200)
        exportButtom.clicked.connect(saveLog)

        def copyLog():
            try:
                print("🔵 Copying log to the clipboard...")
                globals.app.clipboard().setText(stdout_buffer.getvalue())
                print("🟢 Log copied to the clipboard successfully!")
                self.textEdit.setPlainText(stdout_buffer.getvalue())
            except Exception as e:
                report(e)
                self.textEdit.setPlainText(stdout_buffer.getvalue())

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
        self.textEdit.setPlainText(stdout_buffer.getvalue())
        return super().showEvent(event)

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


if __name__ == "__main__":
    import __init__
