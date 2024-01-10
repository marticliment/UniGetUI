if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import os
import socket
import sys
import time
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from threading import Thread

import wingetui.Core.Globals as Globals
import wingetui.Interface.BackendApi as BackendApi
from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from wingetui.Interface.CustomWidgets.SpecificWidgets import *
from wingetui.Interface.CustomWidgets.InstallerWidgets import *
from wingetui.Interface.GenericSections import *
from wingetui.PackageEngine.Classes import PackageManagerModule


class DiscoverSoftwareSection(SoftwareSection):
    PackageManagers = PackageManagersList.copy()
    PackagesLoaded = PackagesLoadedDict.copy()

    DynaimcPackageManagers = DynaimcPackageManagersList.copy()
    DynamicPackagesLoaded = DynamicPackagesLoadedDict.copy()
    FilterItemForManager = {}

    LastQueryDynamicallyLoaded: str = ""

    finishDynamicLoading = Signal()

    ShouldHideGuideArrow: bool = False

    runningThreads = 0

    def __init__(self, parent=None):
        super().__init__(parent=parent, sectionName="Discover")

        self.finishDynamicLoading.connect(self.finishDynamicLoadingIfNeeded)

        self.query.setPlaceholderText(" " + _("Search for packages"))
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
        self.installedIcon = getMaskedIcon("installed_masked")
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
            self.ArrowLabel.move(self.query.x() + self.query.width() // 2 - self.ArrowLabel.width() + 80, self.query.y() + self.query.height())
            self.ArrowLabel.setPixmap(self.ArrowLabelPixmap)
            self.ArrowLabel.setGraphicsEffect(self.ArrowLabelOpacity)

            self.ArrowLabelInAnimation.setStartValue(250)
            self.ArrowLabelInAnimation.setEndValue(750)
            self.ArrowLabelInAnimation.setDuration(1000)
            self.ArrowLabelInAnimation.setEasingCurve(QEasingCurve.Type.InOutCubic)
            self.ArrowLabelInAnimation.valueChanged.connect(lambda v: (self.ArrowLabelOpacity.setOpacity(v / 1000) if self.ArrowLabel.isVisible() else None, self.ArrowLabel.move(self.query.x() + self.query.width() // 2 - self.ArrowLabel.width() + 80, self.query.y() + self.query.height())))
            self.ArrowLabelInAnimation.finished.connect(self.ArrowLabelOutAnimation.start)

            self.ArrowLabelOutAnimation.setStartValue(750)
            self.ArrowLabelOutAnimation.setEndValue(250)
            self.ArrowLabelOutAnimation.setDuration(1000)
            self.ArrowLabelOutAnimation.setEasingCurve(QEasingCurve.Type.InOutCubic)
            self.ArrowLabelOutAnimation.valueChanged.connect(lambda v: (self.ArrowLabelOpacity.setOpacity(v / 1000) if self.ArrowLabel.isVisible() else None, self.ArrowLabel.move(self.query.x() + self.query.width() // 2 - self.ArrowLabel.width() + 80, self.query.y() + self.query.height())))
            self.ArrowLabelOutAnimation.finished.connect(self.ArrowLabelInAnimation.start)

            self.ArrowLabelOutAnimation.start()

        self.query.setFocus()

        self.finishInitialisation()

    def ApplyIcons(self):
        super().ApplyIcons()
        self.installIcon = QIcon(getMedia("install"))
        self.installedIcon = getMaskedIcon("installed_masked")
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
            package: Package = item.Package

            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, package.getSourceIcon())

            UNINSTALL: UninstallSoftwareSection = Globals.uninstall
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
            Capabilities: PackageManagerCapabilities = self.packageList.currentItem().Package.PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuSkipHash.setVisible(Capabilities.CanSkipIntegrityChecks)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        pos.setY(pos.y() + 35)
        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.ToolbarInstall = QAction(_("Install selected packages"), toolbar)
        self.ToolbarInstall.triggered.connect(lambda: self.installSelectedPackageItems())
        toolbar.addAction(self.ToolbarInstall)

        self.ToolbarShowInfo = QAction("", toolbar)  # ("Show info")
        self.ToolbarShowInfo.triggered.connect(lambda: self.openInfo(self.packageList.currentItem()))
        self.ToolbarRunAsAdmin = QAction("", toolbar)  # ("Run as administrator")
        self.ToolbarRunAsAdmin.triggered.connect(lambda: self.installSelectedPackageItems(admin=True))
        self.ToolbarSkipHash = QAction("", toolbar)  # ("Skip hash check")
        self.ToolbarSkipHash.triggered.connect(lambda: self.installSelectedPackageItems(skiphash=True))
        self.ToolbarInteractive = QAction("", toolbar)  # ("Interactive update")
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
            action.setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])

        toolbar.addSeparator()

        self.HelpMenuEntry1 = QAction("Guide for beginners on how to install a package")
        self.HelpMenuEntry1.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/install-a-program"))
        self.HelpMenuEntry2 = QAction("Discover Packages overview - every feature explained")
        self.HelpMenuEntry2.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/discover-overview"))
        self.HelpMenuEntry3 = QAction("WingetUI Help and Documentation")
        self.HelpMenuEntry3.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help"))

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
            if package.Id == id and (package.Source == store or store == "Unknown"):
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
                if package.checkState(0) == Qt.CheckState.Checked:
                    self.installPackageItem(package, admin, interactive, skiphash)
            except AttributeError:
                pass

    def importPackages(self):
        self.importer = PackageImporter(self)

    def filter(self) -> None:
        print(f"🟢 Searching for string \"{self.query.text()}\"")

        def waitAndFilter(query: str):
            time.sleep(0.5)
            if query == self.query.text():
                self.callInMain.emit(partial(self.finishFiltering, query))

        Thread(target=lambda: waitAndFilter(self.query.text())).start()

    def finishFiltering(self, text: str) -> None:
        if len(text) >= 2:
            if text != self.LastQueryDynamicallyLoaded:
                self.LastQueryDynamicallyLoaded = text
                self.startLoadingDyamicPackages(text)
            super().finishFiltering(text)
            if len(self.showableItems) == 0 and self.isLoadingDynamicPackages():
                self.packageList.label.setText(_("Looking for packages..."))
            else:
                self.updateFilterTable()
        elif len(text) == 0:
            self.showableItems = []
            for item in self.packageItems:
                try:
                    if item.checkState(0) == Qt.CheckState.Checked:
                        self.showableItems.append(item)
                except RuntimeError:
                    print("🟠 RuntimeError on DiscoverSoftwareSection.finishFiltering")
            self.addItemsToTreeWidget(reset=True)
            self.packageList.scrollToItem(self.packageList.currentItem())
            if len(self.showableItems) == 0:
                self.addItemsToTreeWidget(reset=True)
                self.LoadingIndicator.hide()
                self.packageList.label.show()
                self.packageList.label.setText(_("Search for packages to start"))
            self.updateFilterTable()
        else:
            self.showableItems = []
            self.addItemsToTreeWidget(reset=True)
            self.updateFilterTable()
            self.LoadingIndicator.hide()
            self.packageList.label.show()
            self.packageList.label.setText(_("Please type at least two characters"))

    def finishLoadingIfNeeded(self) -> None:
        itemCount = len(self.packageItems)
        self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(str(itemCount)))
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.query.setEnabled(True)
        self.finishFiltering(self.query.text())

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager] and manager.isEnabled():
                return

        self.reloadButton.setEnabled(True)
        self.LoadingIndicator.hide()
        self.countLabel.setText(_("Found packages: {0}").format(str(itemCount)))
        print("🟢 Total packages: " + str(itemCount))

    def finishDynamicLoadingIfNeeded(self) -> None:
        self.finishFiltering(self.query.text())

        if not self.isLoadingDynamicPackages():
            self.LoadingIndicator.hide()
            if len(self.showableItems) == 0 and len(self.query.text()) >= 3:
                self.packageList.label.setText(_("No packages found matching the input criteria"))
            else:
                self.packageList.label.setText(_(""))

    def isLoadingDynamicPackages(self) -> bool:
        return self.runningThreads > 0

    def addItem(self, package: Package) -> None:
        if "---" not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and version not in ("the", "is"):

            item = PackageItem(package)

            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            self.UniqueIdPackageReference[package.UniqueId] = package
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)

    def installPackageItem(self, item: PackageItem, admin: bool = False, interactive: bool = False, skiphash: bool = False) -> None:
        """
        Initialize the install procedure for the given package item, passed as a PackageItem. Switches: admin, interactive, skiphash
        """
        package: Package = item.Package
        options = InstallationOptions(package)
        if admin:
            options.RunAsAdministrator = True
        if interactive:
            options.InteractiveInstallation = True
        if skiphash:
            options.SkipHashCheck = True
        self.addInstallation(PackageInstallerWidget(package, options))

    def installPackage(self, package: Package, options: InstallationOptions = None) -> None:
        """
        Initialize the install procedure for the given package, passed as a Package. Switches: admin, interactive, skiphash
        """
        if not options:
            options = InstallationOptions(package)
        self.addInstallation(PackageInstallerWidget(package, options))

    def loadPackages(self, manager: PackageManagerModule) -> None:
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def loadDynamicPackages(self, query: str, manager: PackageManagerModule) -> None:
        self.runningThreads += 1
        packages = manager.getPackagesForQuery(query)
        for package in packages:
            if package.UniqueId in self.UniqueIdPackageReference and package.Source == self.UniqueIdPackageReference[package.UniqueId].Source and package.Version == self.UniqueIdPackageReference[package.UniqueId].Version:
                print(f"🟡 Not showing found result {package} because it is already present")
            elif query != self.query.text():
                print(f"🟡 Not showing found result {package} because the query changed")  # thanks copilot :)
            else:
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
        self.LoadingIndicator.show()

        for manager in self.DynaimcPackageManagers:
            if manager.isEnabled():
                Thread(target=self.loadDynamicPackages, args=(query, manager), daemon=True, name=f"{manager.NAME} dyamic packages loader").start()
            else:
                self.PackagesLoaded[manager] = True

        self.finishDynamicLoadingIfNeeded()

    def resizeEvent(self, event: QResizeEvent) -> None:
        if self.ArrowLabel.isVisible():
            self.ArrowLabel.move(self.query.x() + self.query.width() // 2 - self.ArrowLabel.width() + 80, self.query.y() + self.query.height())
        return super().resizeEvent(event)

    def moveEvent(self, event: QMoveEvent) -> None:
        if self.ArrowLabel.isVisible():
            self.ArrowLabel.move(self.query.x() + self.query.width() // 2 - self.ArrowLabel.width() + 80, self.query.y() + self.query.height())
        return super().moveEvent(event)


class UpdateSoftwareSection(SoftwareSection):

    PackageManagers = PackageManagersList.copy()
    PackagesLoaded = PackagesLoadedDict.copy()
    FilterItemForManager = {}

    addProgram = Signal(object)
    availableUpdates: int = 0
    PackageItemReference: dict[UpgradablePackage:UpgradablePackageItem] = {}
    ItemPackageReference: dict[UpgradablePackageItem:UpgradablePackage] = {}
    IdPackageReference: dict[str:UpgradablePackage] = {}
    UpdatesNotification: ToastNotification = None
    AllItemsSelected = True

    def __init__(self, parent=None):
        super().__init__(parent=parent, sectionName="Update")
        BackendApi.availableUpdates = self.packageItems

        self.blacklistManager = IgnoredUpdatesManager(self.window())
        self.LegacyBlacklist = getSettingsValue("BlacklistedUpdates")

        self.query.setPlaceholderText(" " + _("Search on available updates"))
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
            UNINSTALL_SECTION: UninstallSoftwareSection = Globals.uninstall
            if self.packageList.currentItem():
                installedItem = self.packageList.currentItem().getInstalledPackageItem()
                if installedItem:
                    UNINSTALL_SECTION.uninstallPackageItem(installedItem)

        def uninstallThenUpdate():
            UNINSTALL_SECTION: UninstallSoftwareSection = Globals.uninstall
            INSTALL_SECTION: DiscoverSoftwareSection = Globals.discover
            packageItem = self.packageList.currentItem()
            if packageItem:
                installedItem = self.packageList.currentItem().getInstalledPackageItem()
                if installedItem:
                    UNINSTALL_SECTION.uninstallPackageItem(installedItem, avoidConfirm=True)
                else:
                    UNINSTALL_SECTION.uninstallPackageItem(packageItem, avoidConfirm=True)
                INSTALL_SECTION.installPackageItem(packageItem)

        self.MenuUninstall = QAction(_("Uninstall package"))
        self.MenuUninstall.triggered.connect(lambda: uninstallPackage())
        self.MenuUninstallThenUpdate = QAction(_("Uninstall package, then update it"))
        self.MenuUninstallThenUpdate.triggered.connect(lambda: uninstallThenUpdate())

        self.MenuIgnoreUpdates = QAction(_("Ignore updates for this package"))
        self.MenuIgnoreUpdates.triggered.connect(lambda: self.packageList.currentItem().Package.AddToIgnoredUpdates())

        self.MenuSkipVersion = QAction(_("Skip this version"))
        self.MenuSkipVersion.triggered.connect(lambda: self.packageList.currentItem().Package.AddToIgnoredUpdates(self.packageList.currentItem().Package.NewVersion))

        self.MenuShare = QAction(_("Share this package"))
        self.MenuShare.triggered.connect(lambda: self.sharePackage(self.packageList.currentItem()))

        self.installIcon = QIcon(getMedia("install"))
        self.updateIcon = getMaskedIcon("update_masked")
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))
        self.newVersionIcon = QIcon(getMedia("newversion"))

        self.contextMenu.addAction(self.MenuInstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuAdministrator)
        self.contextMenu.addAction(self.MenuInteractive)
        self.contextMenu.addAction(self.MenuSkipHash)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuUninstallThenUpdate)
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
        self.updateIcon = getMaskedIcon("update_masked")
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
        self.MenuUninstallThenUpdate.setIcon(QIcon(getMedia("undelete")))

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

        self.HelpMenuEntry10.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry11.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry12.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry13.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry2.setIcon(QIcon(getMedia("launch")))
        self.HelpMenuEntry3.setIcon(QIcon(getMedia("launch")))

        for item in self.packageItems:
            package: UpgradablePackage = item.Package

            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, self.newVersionIcon)
            item.setIcon(5, package.getSourceIcon())

            UNINSTALL: UninstallSoftwareSection = Globals.uninstall
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
            Capabilities: PackageManagerCapabilities = self.packageList.currentItem().Package.PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuSkipHash.setVisible(Capabilities.CanSkipIntegrityChecks)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        pos.setY(pos.y() + 35)
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)
        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:

        def blacklistSelectedPackages():
            for packageItem in self.showableItems:
                if packageItem.checkState(0) == Qt.CheckState.Checked:
                    packageItem.Package.AddToIgnoredUpdates()

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
            action.setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])

        toolbar.addSeparator()

        self.HelpMenuEntry10 = QAction("Upgrading a package")
        self.HelpMenuEntry10.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/update-software/#upgrade-package"))
        self.HelpMenuEntry11 = QAction("Enabling automatic updates")
        self.HelpMenuEntry11.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/update-software/#enable-updates"))
        self.HelpMenuEntry12 = QAction("Ignoring updates for a package")
        self.HelpMenuEntry12.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/update-software/#ignore"))
        self.HelpMenuEntry13 = QAction("Managing ignored updates")
        self.HelpMenuEntry13.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/update-software/#manage-ignored"))

        self.HelpMenuEntry2 = QAction("Software Updates overview - every feature explained")
        self.HelpMenuEntry2.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help/updates-overview"))
        self.HelpMenuEntry3 = QAction("WingetUI Help and Documentation")
        self.HelpMenuEntry3.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help"))

        def showHelpMenu():
            helpMenu = QMenu(self)
            helpMenu.addAction(self.HelpMenuEntry10)
            helpMenu.addAction(self.HelpMenuEntry11)
            helpMenu.addAction(self.HelpMenuEntry12)
            helpMenu.addAction(self.HelpMenuEntry13)
            helpMenu.addSeparator()
            helpMenu.addAction(self.HelpMenuEntry2)
            helpMenu.addAction(self.HelpMenuEntry3)
            # helpMenu.addAction(self.HelpMenuEntry4)
            # helpMenu.addSeparator()
            # helpMenu.addAction(self.HelpMenuEntry5)
            ApplyMenuBlur(helpMenu.winId().__int__(), self.contextMenu)
            helpMenu.exec(QCursor.pos())

        self.ToolbarHelp = QAction(QIcon(getMedia("help")), _("Help"), toolbar)
        self.ToolbarHelp.triggered.connect(showHelpMenu)
        toolbar.addAction(self.ToolbarHelp)

        return toolbar

    def finishLoadingIfNeeded(self) -> None:
        self.countLabel.setText(_("Available updates: {0}, not finished yet...").format(str(len(self.packageItems))))
        BackendApi.availableUpdates = self.packageItems
        Globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}, not finished yet...").format(str(len(self.packageItems))))
        if len(self.packageItems) == 0:
            self.packageList.label.setText(self.countLabel.text())
        else:
            self.packageList.label.setText("")
        self.filter()
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.query.setEnabled(True)

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return

        self.reloadButton.setEnabled(True)
        self.LoadingIndicator.hide()
        Globals.trayMenuUpdatesList.menuAction().setText(_("Available updates: {0}").format(str(len(self.packageItems))))
        count = 0
        lastVisibleItem = None
        for item in self.packageItems:
            if not item.isHidden():
                count += 1
                lastVisibleItem = item
        if count > 0:
            Globals.tray_is_available_updates = True
            update_tray_icon()
            try:
                self.UpdatesNotification.close()
            except AttributeError:
                pass
            except Exception as e:
                report(e)
            if getSettings("AutomaticallyUpdatePackages") or "--updateapps" in sys.argv:
                if not Globals.tray_is_installing:
                    self.updateAllPackageItems()
                    self.UpdatesNotification = ToastNotification(self, self.callInMain.emit)
                    if count > 1:
                        self.UpdatesNotification.setTitle(_("Updates found!"))
                        self.UpdatesNotification.setDescription(_("{0} packages are being updated").format(count) + ":")
                        packageList = ""
                        for item in self.packageItems:
                            packageList += item.Package.Name + ", "
                        self.UpdatesNotification.setSmallText(packageList[:-2])
                    elif count == 1:
                        self.UpdatesNotification.setTitle(_("Update found!"))
                        self.UpdatesNotification.setDescription(_("{0} is being updated").format(lastVisibleItem.Package.Name))
                    self.UpdatesNotification.addOnClickCallback(lambda: (Globals.mainWindow.showWindow(1)))
                    if Globals.ENABLE_UPDATES_NOTIFICATIONS:
                        self.UpdatesNotification.show()

            else:
                self.UpdatesNotification = ToastNotification(self, self.callInMain.emit)
                if count > 1:
                    self.UpdatesNotification.setTitle(_("Updates found!"))
                    self.UpdatesNotification.setDescription(_("{0} packages can be updated").format(count) + ":")
                    self.UpdatesNotification.addAction(_("Update all"), self.updateAllPackageItems)
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.Package.Name + ", "
                    self.UpdatesNotification.setSmallText(packageList[:-2])
                elif count == 1:
                    self.UpdatesNotification.setTitle(_("Update found!"))
                    self.UpdatesNotification.setDescription(_("{0} can be updated").format(lastVisibleItem.Package.Name))
                    self.UpdatesNotification.addAction(_("Update"), self.updateAllPackageItems)
                self.UpdatesNotification.addAction(_("Show WingetUI"), lambda: (Globals.mainWindow.showWindow(1)))
                self.UpdatesNotification.addOnClickCallback(lambda: (Globals.mainWindow.showWindow(1)))
                if Globals.ENABLE_UPDATES_NOTIFICATIONS:
                    self.UpdatesNotification.show()

            self.packageList.label.setText("")
        else:
            Globals.tray_is_available_updates = False
            update_tray_icon()

        self.updatePackageNumber()
        self.filter()
        self.addItemsToTreeWidget(reset=True)

        if not getSettings("DisableAutoCheckforUpdates"):
            try:
                waitTime = int(getSettingsValue("UpdatesCheckInterval"))
            except ValueError:
                print(f"🟡 Can't get custom interval time! (got value was '{getSettingsValue('UpdatesCheckInterval')}')")
                waitTime = 3600
            Thread(target=lambda: (time.sleep(waitTime), self.reloadSources()), daemon=True, name="AutoCheckForUpdates Thread").start()
        print("🟢 Total packages: " + str(len(self.packageItems)))

    def changeStore(self, package: UpgradablePackage):
        time.sleep(3)
        try:
            UNINSTALL_SECTION: UninstallSoftwareSection = Globals.uninstall
            package.Source = UNINSTALL_SECTION.IdPackageReference[package.Id].Source
        except KeyError:
            print(f"🟠 Package {package.Id} found in the updates section but not in the installed one, happened again")
        self.callInMain.emit(partial(package.PackageItem.setText, 5, package.Source))

    def addItem(self, package: UpgradablePackage) -> None:
        if "---" not in package.Name and "The following packages" not in package.Name and "Name  " not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and package.Version.lower() not in ("the", "is", "install") and package.NewVersion not in ("Manifest", package.Version):

            if package.HasUpdatesIgnored(package.NewVersion):
                print(f"🟡 Package {package.Id} has version {package.GetIgnoredUpatesVersion()} ignored")
                return

            elif package.HasUpdatesIgnored():
                print(package.GetIgnoredUpatesVersion())
                return

            for match in package.getAllCorrespondingInstalledPackages():
                if match.Version == package.NewVersion:
                    print(f"🟡 Multiple versions of {package.Id} are installed, latest version is installed. Not showing the update")
                    return

            item = UpgradablePackageItem(package)

            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)
            action = QAction(package.Name + "  \t" + package.Version + "\t → \t" + package.NewVersion, Globals.trayMenuUpdatesList)
            action.triggered.connect(lambda: self.updatePackageItem(item))
            action.setShortcut(package.Version)
            item.setAction(action)
            Globals.trayMenuUpdatesList.addAction(action)

    def finishFiltering(self, text: str):
        def getChecked(item: UpgradablePackageItem) -> str:
            return " " if item.checkState(0) == Qt.CheckState.Checked else ""

        def getTitle(item: UpgradablePackageItem) -> str:
            return item.Package.Name

        def getID(item: UpgradablePackageItem) -> str:
            return item.Package.Id

        def getVersion(item: UpgradablePackageItem) -> str:
            return item.text(6)

        def getNewVersion(item: UpgradablePackageItem) -> str:
            return item.Package.NewVersion

        def getSource(item: UpgradablePackageItem) -> str:
            return item.Package.Source

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
                if self.containsQuery(item, text):
                    self.showableItems.append(item)
                    found += 1
            except RuntimeError:
                print("nullitem")

        self.updateFilterTable()

        if found == 0:
            if self.packageList.label.text() == "":
                self.packageList.label.show()
                self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            if self.packageList.label.text() == _("No packages found matching the input criteria"):
                self.packageList.label.hide()
                self.packageList.label.setText("")
        self.addItemsToTreeWidget(reset=True)
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
            Globals.updatesAction.setIcon(QIcon(getMedia("alert_laptop")))
            Globals.app.uaAction.setEnabled(True)
            Globals.trayMenuUpdatesList.menuAction().setEnabled(True)
            Globals.tray_is_available_updates = True
        else:
            trayMenuText = _("No updates are available")
            self.packageList.label.setText(_("Hooray! No updates were found!"))
            self.packageList.label.show()
            Globals.app.uaAction.setEnabled(False)
            Globals.trayMenuUpdatesList.menuAction().setEnabled(False)
            Globals.updatesAction.setIcon(QIcon(getMedia("checked_laptop")))
            self.SectionImage.setPixmap(QIcon(getMedia("checked_laptop")).pixmap(QSize(64, 64)))
            Globals.tray_is_available_updates = False
        Globals.trayMenuUpdatesList.menuAction().setText(trayMenuText)
        update_tray_icon()
        self.updateFilterTable()

    def updateAllPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden():
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updateAllPackageItemsForSource(self, source: str, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        print(source)
        for item in self.packageItems:
            if not item.isHidden() and item.Package.PackageManager.NAME == source:
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updateSelectedPackageItems(self, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        for item in self.packageItems:
            if not item.isHidden() and item.checkState(0) == Qt.CheckState.Checked and self.FilterItemForManager[item.Package.PackageManager].checkState(0) == Qt.CheckState.Checked:
                self.updatePackageItem(item, admin, skiphash, interactive)

    def updatePackageItem(self, item: UpgradablePackageItem, admin: bool = False, skiphash: bool = False, interactive: bool = False) -> None:
        package: Package = item.Package
        options = InstallationOptions(item.Package)
        if admin:
            options.RunAsAdministrator = True
        if interactive:
            options.InteractiveInstallation = True
        if skiphash:
            options.SkipHashCheck = True
        self.addInstallation(PackageUpdaterWidget(package, options))

    def updatePackageForGivenId(self, id: str):
        package: Package = self.IdPackageReference[id]
        self.updatePackageItem(self.PackageItemReference[package])

    def reloadSources(self, asyncroutine: bool = False):
        print("🔵 Reloading sources...")
        try:
            for manager in PackageManagersList:
                manager.updateSources()
        except Exception as e:
            report(e)
        if not asyncroutine:
            self.callInMain.emit(self.startLoadingPackages)

    def loadPackages(self, manager: PackageManagerModule) -> None:
        t = Thread(target=lambda: self.reloadSources(asyncroutine=True), daemon=True)
        t.start()
        t0 = int(time.time())
        while t.is_alive() and (int(time.time()) - t0 < 10):  # Timeout of 10 seconds for the reloadSources function
            time.sleep(0.2)
        packages = manager.getAvailableUpdates()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for updates..."))
        self.packageList.label.setText(self.countLabel.text())
        for action in Globals.trayMenuUpdatesList.actions():
            Globals.trayMenuUpdatesList.removeAction(action)
        Globals.trayMenuUpdatesList.addAction(Globals.updatesHeader)
        return super().startLoadingPackages(force)

    def sharePackage(self, packageItem: UpgradablePackageItem):
        url = f"https://marticliment.com/wingetui/share?pid={packageItem.Package.Id}^&pname={packageItem.Package.Name}^&psource={packageItem.Package.Source}"
        nativeWindowsShare(packageItem.Package.Id, url, self.window())


class UninstallSoftwareSection(SoftwareSection):
    allPkgSelected: bool = False
    PackageManagers = PackageManagersList.copy()
    PackagesLoaded = PackagesLoadedDict.copy()
    FilterItemForManager = {}
    IsFirstPackageLoad: bool = True

    def __init__(self, parent=None):
        super().__init__(parent=parent, sectionName="Uninstall")
        self.query.setPlaceholderText(" " + _("Search on your software"))
        self.SectionImage.setPixmap(QIcon(getMedia("workstation")).pixmap(QSize(64, 64)))
        self.discoverLabel.setText(_("Installed Packages"))

        self.headers = ["", _("Package Name"), _("Package ID"), _("Installed Version"), _("Source"), "", ""]  # empty header added for checkbox
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

        self.pinnedIcon = getMaskedIcon("pin_masked")
        self.updateIcon = getMaskedIcon("update_masked")

        self.installedIcon = getMaskedIcon("installed_masked")
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

        def reinstall():
            INSTALL_SECTION: DiscoverSoftwareSection = Globals.discover
            packageItem = self.packageList.currentItem()
            if packageItem:
                INSTALL_SECTION.installPackageItem(packageItem)

        def uninstallThenReinstall():
            INSTALL_SECTION: DiscoverSoftwareSection = Globals.discover
            packageItem = self.packageList.currentItem()
            if packageItem:
                self.uninstallPackageItem(packageItem, avoidConfirm=True)
                INSTALL_SECTION.installPackageItem(packageItem)

        self.Reinstall = QAction(_("Reinstall package"))
        self.Reinstall.triggered.connect(lambda: reinstall())
        self.UninstallThenReinstall = QAction(_("Uninstall package, then reinstall it"))
        self.UninstallThenReinstall.triggered.connect(lambda: uninstallThenReinstall())
        self.MenuIgnoreUpdates = QAction(_("Ignore updates for this package"))
        self.MenuIgnoreUpdates.triggered.connect(lambda: self.packageList.currentItem().Package.AddToIgnoredUpdates())
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
        self.contextMenu.addAction(self.Reinstall)
        self.contextMenu.addAction(self.UninstallThenReinstall)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuIgnoreUpdates)
        self.contextMenu.addSeparator()
        self.contextMenu.addAction(self.MenuShare)
        self.contextMenu.addAction(self.MenuDetails)

        self.finishInitialisation()

    def ApplyIcons(self):
        super().ApplyIcons()
        self.installIcon = QIcon(getMedia("install"))

        self.pinnedIcon = getMaskedIcon("pin_masked")

        self.updateIcon = getMaskedIcon("update_masked")
        self.installedIcon = getMaskedIcon("installed_masked")
        self.IDIcon = QIcon(getMedia("ID"))
        self.versionIcon = QIcon(getMedia("version"))

        self.MenuInteractive.setIcon(QIcon(getMedia("interactive")))
        self.MenuRemovePermaData.setIcon(QIcon(getMedia("menu_close")))
        self.MenuAdministrator.setIcon(QIcon(getMedia("runasadmin")))
        self.MenuInstall.setIcon(QIcon(getMedia("menu_uninstall")))
        self.MenuDetails.setIcon(QIcon(getMedia("info")))
        self.MenuShare.setIcon(QIcon(getMedia("share")))
        self.MenuIgnoreUpdates.setIcon(QIcon(getMedia("pin")))
        self.Reinstall.setIcon(QIcon(getMedia("newversion")))
        self.UninstallThenReinstall.setIcon(QIcon(getMedia("undelete")))

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
            package: UpgradablePackage = item.Package

            item.setIcon(1, self.installIcon)
            item.setIcon(2, self.IDIcon)
            item.setIcon(3, self.versionIcon)
            item.setIcon(4, package.getSourceIcon())

            if package.HasUpdatesIgnored():
                item.setIcon(1, self.pinnedIcon)

            UPDATES: UpdateSoftwareSection = Globals.updates
            if package.Id in UPDATES.IdPackageReference.keys():
                updatePackage: UpgradablePackage = UPDATES.IdPackageReference[package.Id]
                updateItem = updatePackage.PackageItem
                if updateItem in UPDATES.packageItems:
                    item.setIcon(1, self.updateIcon)

            DISCOVER: UninstallSoftwareSection = Globals.discover
            if package.Id in DISCOVER.IdPackageReference.keys():
                discoverablePackage: UpgradablePackage = DISCOVER.IdPackageReference[package.Id]
                discoverableItem = discoverablePackage.PackageItem
                if discoverableItem in DISCOVER.packageItems:
                    discoverableItem.setIcon(1, self.installedIcon)

    def showBlacklistedIcon(self, packageItem: PackageItem):
        try:
            raise DeprecationWarning("DEPRECATED!")
        except Exception as e:
            report(e)
        packageItem.setIcon(1, self.pinnedIcon)
        packageItem.setToolTip(1, _("Updates for this package are ignored") + " - " + packageItem.Package.Name)

    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)

        try:
            Capabilities: PackageManagerCapabilities = self.packageList.currentItem().Package.PackageManager.Capabilities
            self.MenuAdministrator.setVisible(Capabilities.CanRunAsAdmin)
            self.MenuRemovePermaData.setVisible(Capabilities.CanRemoveDataOnUninstall)
            self.MenuInteractive.setVisible(Capabilities.CanRunInteractively)
        except Exception as e:
            report(e)

        if self.packageList.currentItem().Package.Source not in ((_("Local PC"), "Microsoft Store", "Steam", "GOG", "Ubisoft Connect", _("Android Subsystem"))):
            self.MenuIgnoreUpdates.setVisible(True)
            self.MenuShare.setVisible(True)
            self.MenuDetails.setVisible(True)
            self.Reinstall.setVisible(True)
            self.UninstallThenReinstall.setVisible(True)

        else:
            self.MenuIgnoreUpdates.setVisible(False)
            self.MenuShare.setVisible(False)
            self.MenuDetails.setVisible(False)
            self.Reinstall.setVisible(False)
            self.UninstallThenReinstall.setVisible(False)

        pos.setY(pos.y() + 35)

        self.contextMenu.exec(self.packageList.mapToGlobal(pos))

    def getToolbar(self) -> QToolBar:
        toolbar = QToolBar(self.window())
        toolbar.setToolButtonStyle(Qt.ToolButtonTextBesideIcon)

        toolbar.addWidget(TenPxSpacer())
        self.ToolbarInstall = QAction(_("Uninstall selected packages"), toolbar)
        self.ToolbarInstall.triggered.connect(lambda: self.uninstallSelected())
        toolbar.addAction(self.ToolbarInstall)

        def blacklistSelectedPackages():
            for packageItem in self.packageItems:
                if not packageItem.isHidden():
                    try:
                        if packageItem.checkState(0) == Qt.CheckState.Checked:
                            packageItem.Package.AddToIgnoredUpdates()
                    except AttributeError:
                        pass
            self.notif = InWindowNotification(self, _("The selected packages have been blacklisted"))
            self.notif.show()
            self.updatePackageNumber()

        def showInfo():
            item = self.packageList.currentItem()
            if item.Package.Source in ((_("Local PC"), "Microsoft Store", "Steam", "GOG", "Ubisoft Connect")):
                self.err = CustomMessageBox(self.window())
                errorData = {
                    "titlebarTitle": _("Unable to load informarion"),
                    "mainTitle": _("Unable to load informarion"),
                    "mainText": _("We could not load detailed information about this package, because it was not installed from an available package manager."),
                    "buttonTitle": _("Ok"),
                    "errorDetails": _("Uninstallable packages with the origin listed as \"{0}\" are not published on any package manager, so there's no information available to show about them.").format(item.Package.Source),
                    "icon": QIcon(getMedia("notif_warn")),
                }
                self.err.showErrorMessage(errorData, showNotification=False)
            else:
                self.openInfo(item, uninstall=True)

        self.ToolbarShowInfo = QAction("", toolbar)  # ("Show info")
        self.ToolbarShowInfo.triggered.connect(showInfo)
        self.ToolbarRunAsAdmin = QAction("", toolbar)  # ("Run as administrator")
        self.ToolbarRunAsAdmin.triggered.connect(lambda: self.uninstallSelected(admin=True))
        self.ToolbarInteractive = QAction("", toolbar)  # ("Interactive uninstall")
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
            action.setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setToolTip(tooltips[action])
            toolbar.widgetForAction(action).setAccessibleName(tooltips[action])

        toolbar.addSeparator()

        self.HelpMenuEntry1 = QAction("WingetUI Help and Documentation")
        self.HelpMenuEntry1.triggered.connect(lambda: Globals.mainWindow.showHelpUrl("https://marticliment.com/wingetui/help"))
        self.HelpMenuEntry2 = QAction("")
        self.HelpMenuEntry2.triggered.connect(lambda: os.startfile(""))
        self.HelpMenuEntry3 = QAction("")
        self.HelpMenuEntry3.triggered.connect(lambda: os.startfile(""))

        def showHelpMenu():
            helpMenu = QMenu(self)
            helpMenu.addAction(self.HelpMenuEntry1)
            # helpMenu.addAction(self.HelpMenuEntry2)
            helpMenu.addSeparator()
            # helpMenu.addAction(self.HelpMenuEntry3)
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
                    if program.checkState(0) == Qt.CheckState.Checked:
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
        Globals.trayMenuInstalledList.menuAction().setText(trayMenuText)
        if self.foundPackages > 0:
            self.packageList.label.hide()
            self.packageList.label.setText("")
        else:
            self.packageList.label.setText(_("{0} packages were found").format(0))
            self.packageList.label.show()
        self.updateFilterTable()

    def finishLoadingIfNeeded(self) -> None:
        self.countLabel.setText(_("Found packages: {0}, not finished yet...").format(len(self.packageItems)))
        if len(self.packageItems) == 0:
            self.packageList.label.setText(self.countLabel.text())
        else:
            self.packageList.label.setText("")
        Globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(len(self.packageItems)))
        self.reloadButton.setEnabled(True)
        self.searchButton.setEnabled(True)
        self.filter()
        self.query.setEnabled(True)

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return

        self.reloadButton.setEnabled(True)
        self.filter()
        self.LoadingIndicator.hide()
        Globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(len(self.packageItems)))
        self.countLabel.setText(_("Found packages: {0}").format(len(self.packageItems)))
        self.packageList.label.setText("")
        print("🟢 Total packages: " + str(len(self.packageItems)))

        if (self.IsFirstPackageLoad and getSettings("EnablePackageBackup")):
            self.IsFirstPackageLoad = False
            try:
                print("🟢 Starting package backup...")

                dirName = getSettingsValue("ChangeBackupOutputDirectory")
                if not dirName:
                    dirName = Globals.DEFAULT_PACKAGE_BACKUP_DIR
                if not os.path.exists(dirName):
                    os.makedirs(dirName)

                fileName = getSettingsValue("ChangeBackupFileName")
                if not fileName:
                    fileName = f"{socket.gethostname()} installed packages"

                if getSettings("EnableBackupTimestamping"):
                    fileName += f" {datetime.now().strftime('%d-%m-%Y %H.%M')}"
                fileName += ".json"

                backupPath = os.path.join(dirName, fileName)
                print("🔵 Backup path set to", backupPath)
                data = self.packageExporter.generateExportJson(list(self.PackageItemReference.keys()))
                with open(backupPath, "w", encoding="utf-8", errors="ignore") as f:
                    f.write(json.dumps(data, indent=4))
                print("🟢 Package backup succeeded!")
            except Exception as e:
                report(e)

    def addItem(self, package: Package) -> None:
        if "---" not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and package.Version not in ("the", "is"):

            item = InstalledPackageItem(package)

            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)

            action = QAction(package.Name + " \t" + package.Version, Globals.trayMenuInstalledList)
            action.triggered.connect(lambda: (self.uninstallPackageItem(item)))
            action.setShortcut(package.Version)
            item.setAction(action)
            Globals.trayMenuInstalledList.addAction(action)

    def confirmUninstallSelected(self, toUninstall: list[InstalledPackageItem], a: CustomMessageBox, admin: bool = False, interactive: bool = False, removeData: bool = False):
        questionData = {
            "titlebarTitle": _("Uninstall"),
            "mainTitle": _("Are you sure?"),
            "mainText": _("Do you really want to uninstall {0}?").format(toUninstall[0].Package.Name) if len(toUninstall) == 1 else _("Do you really want to uninstall {0} packages?").format(len(toUninstall)),
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

    def uninstallPackageItem(self, packageItem: InstalledPackageItem, admin: bool = False, removeData: bool = False, interactive: bool = False, avoidConfirm: bool = False) -> None:
        if not avoidConfirm:
            a = CustomMessageBox(self)
            Thread(target=self.confirmUninstallSelected, args=([packageItem], a, admin, interactive, removeData)).start()
        else:
            options = InstallationOptions(packageItem.Package)
            if admin:
                options.RunAsAdministrator = True
            if interactive:
                options.InteractiveInstallation = True
            if removeData:
                options.RemoveDataOnUninstall = True
            self.addInstallation(PackageUninstallerWidget(packageItem.Package, options))

    def loadPackages(self, manager: PackageManagerModule) -> None:
        packages = manager.getInstalledPackages()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def startLoadingPackages(self, force: bool = False) -> None:
        self.countLabel.setText(_("Searching for packages..."))
        self.packageList.label.setText(self.countLabel.text())
        for action in Globals.trayMenuInstalledList.actions():
            Globals.trayMenuInstalledList.removeAction(action)
        Globals.trayMenuInstalledList.addAction(Globals.installedHeader)
        return super().startLoadingPackages(force)

    def selectAllInstalled(self) -> None:
        self.allPkgSelected = not self.allPkgSelected
        for item in self.packageItems:
            item.setCheckState(Qt.CheckState.Checked if self.allPkgSelected else Qt.CheckState.Unchecked)


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
    isLoadingPackageDetails: bool = False

    pressed = False
    oldPos = QPoint(0, 0)

    def __init__(self, parent):
        super().__init__(parent=parent)
        self.iv = ImageViewer(self.window())
        self.callInMain.connect(lambda f: f())
        self.baseScrollArea = SmoothScrollArea()
        self.blurBackgroundEffect = QGraphicsBlurEffect()
        self.setObjectName("bg")
        self.sct = QShortcut(QKeySequence("Esc"), self.baseScrollArea)
        self.sct.activated.connect(lambda: self.close())
        self.baseScrollArea.setWidgetResizable(True)

        self.LoadingIndicator = IndefiniteProgressBar(self)

        self.vLayout = QVBoxLayout()
        self.layout = QVBoxLayout()
        self.title = CustomLabel()
        self.title.setStyleSheet(f"font-size: 30pt;font-family: \"{Globals.dispfont}\";font-weight: bold;")
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
        self.description = CustomLabel("<b>" + _('Description:') + "</b> " + _('Unknown'))
        self.description.setWordWrap(True)

        self.layout.addWidget(self.tagsWidget)
        self.layout.addWidget(self.description)

        self.homepage = CustomLabel("<b>" + _('Homepage') + ":</b> " + _('Unknown'))
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = CustomLabel("<b>" + _('Publisher') + ":</b> " + _('Unknown'))
        self.publisher.setOpenExternalLinks(False)
        self.publisher.linkActivated.connect(lambda t: (self.close(), Globals.discover.query.setText(t), Globals.discover.filter(), Globals.mainWindow.buttonBox.buttons()[0].click()))
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = CustomLabel("<b>" + _('Author') + ":</b> " + _('Unknown'))
        self.author.setOpenExternalLinks(False)
        self.author.linkActivated.connect(lambda t: (self.close(), Globals.discover.query.setText(t), Globals.discover.filter(), Globals.mainWindow.buttonBox.buttons()[0].click()))
        self.author.setWordWrap(True)

        self.layout.addWidget(self.author)
        self.layout.addSpacing(10)

        self.license = CustomLabel("<b>" + _('License') + ":</b> " + _('Unknown'))
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

            def setPixmap(self, arg__1: QPixmap, index=0) -> None:
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
            viewer = LabelWithImageViewer(self)
            viewer.setStyleSheet("border-radius: 4px;margin: 0px;margin-right: 4px;")
            self.imagesCarrousel.append(viewer)
            self.imagesLayout.addWidget(viewer)

        self.contributeLabel = QLabel()
        self.contributeLabel.setText(f"""{_('Is this package missing the icon?')}<br>{_('Are these screenshots wron or blurry?')}<br>{_('The icons and screenshots are maintained by users like you!')}<br><a  style=\"color: {blueColor};\" href=\"https://github.com/marticliment/WingetUI/wiki/Home#the-icon-and-screenshots-database\">{_('Contribute to the icon and screenshot repository')}</a>""")
        self.contributeLabel.setAlignment(Qt.AlignCenter | Qt.AlignVCenter)
        self.contributeLabel.setOpenExternalLinks(True)
        self.imagesLayout.addWidget(self.contributeLabel)
        self.imagesLayout.addStretch()

        self.imagesScrollbar = CustomScrollBar()
        self.imagesScrollbar.setOrientation(Qt.Horizontal)
        self.screenshotsWidget.setHorizontalScrollBar(self.imagesScrollbar)
        self.imagesScrollbar.move(self.screenshotsWidget.x(), self.screenshotsWidget.y() + self.screenshotsWidget.width() - 16)
        self.imagesScrollbar.show()
        self.imagesScrollbar.setFixedHeight(12)

        self.layout.addWidget(self.imagesScrollbar)

        hLayout = QHBoxLayout()

        downloadGroupBox = QGroupBox()
        downloadGroupBox.setFlat(True)

        optionsSection = SmallCollapsableSection(_("Installation options"), getMedia("options"))

        optionsheader = SectionHWidget()
        infolabel = QLabel(_("The following settings will be applied each time this package is installed, updated or removed. They will be saved automatically."))
        infolabel.setWordWrap(True)
        optionsheader.addWidget(infolabel)
        saveButton = QPushButton(_("Save now"))
        saveButton.setFixedSize(150, 30)
        saveButton.clicked.connect(lambda: (self.getInstallationOptions().SaveOptionsToDisk(), InWindowNotification(self, _("Options saved")).show()))
        optionsheader.addWidget(saveButton)
        optionsSection.addWidget(optionsheader)

        self.HashCheckBox = QCheckBox()
        self.HashCheckBox.setText(_("Skip hash check"))
        self.HashCheckBox.setChecked(False)

        self.InteractiveCheckbox = QCheckBox()
        self.InteractiveCheckbox.setText(_("Interactive installation"))
        self.InteractiveCheckbox.setChecked(False)

        self.adminCheckbox = QCheckBox()
        self.adminCheckbox.setText(_("Run as admin"))
        self.adminCheckbox.setChecked(False)

        firstRow = SectionHWidget()
        firstRow.addWidget(self.HashCheckBox)
        firstRow.addWidget(self.InteractiveCheckbox)
        firstRow.addWidget(self.adminCheckbox)

        optionsSection.addWidget(firstRow)

        self.CustomCommandLabel = CommandLineEdit()
        self.CustomCommandLabel.setReadOnly(True)

        commandWidget = SectionHWidget(lastOne=True)
        commandWidget.addWidget(self.CustomCommandLabel)
        commandWidget.setFixedHeight(70)

        self.VersionLabel = QLabel(_("Version to install:"))
        self.VersionCombo = CustomComboBox(self)
        self.VersionCombo.setFixedWidth(150)
        self.VersionCombo.setIconSize(QSize(24, 24))
        self.VersionCombo.setFixedHeight(30)
        self.VersionSection = SectionHWidget()
        self.VersionSection.addWidget(self.VersionLabel)
        self.VersionSection.addWidget(self.VersionCombo)
        self.VersionSection.setFixedHeight(50)

        self.IgnoreFutureUpdates = QCheckBox()
        self.IgnoreFutureUpdates.setText(_("Ignore future updates for this package"))
        self.IgnoreFutureUpdates.setChecked(False)

        ignoreUpdatesSection = SectionHWidget()
        ignoreUpdatesSection.addWidget(self.IgnoreFutureUpdates)

        self.InstallPreRelease = QCheckBox()
        self.InstallPreRelease.setText(_("Install the latest prerelease version"))
        self.InstallPreRelease.setChecked(False)

        prereleaseSection = SectionHWidget()
        prereleaseSection.addWidget(self.InstallPreRelease)

        self.ArchLabel = QLabel(_("Architecture to install:"))
        self.ArchCombo = CustomComboBox(self)
        self.ArchCombo.setFixedWidth(150)
        self.ArchCombo.setIconSize(QSize(24, 24))
        self.ArchCombo.setFixedHeight(30)
        self.ArchSection = SectionHWidget()
        self.ArchSection.addWidget(self.ArchLabel)
        self.ArchSection.addWidget(self.ArchCombo)
        self.ArchSection.setFixedHeight(50)

        self.scopeLabel = QLabel(_("Installation scope:"))
        self.ScopeCombo = CustomComboBox(self)
        self.ScopeCombo.setFixedWidth(150)
        self.ScopeCombo.setIconSize(QSize(24, 24))
        self.ScopeCombo.setFixedHeight(30)
        self.ScopeSection = SectionHWidget()
        self.ScopeSection.addWidget(self.scopeLabel)
        self.ScopeSection.addWidget(self.ScopeCombo)
        self.ScopeSection.setFixedHeight(50)

        self.LocationSection = SectionCheckBoxDirPicker(_("Change install location"), smallerMargins=True)
        self.LocationSection.setDefaultText(_("Select"))

        CustomArgsSection = SectionHWidget()
        customArgumentsLabel = QLabel(_("Custom command-line arguments:"))
        self.CustomArgsLineEdit = CustomLineEdit()
        self.CustomArgsLineEdit.setFixedHeight(30)
        CustomArgsSection.addWidget(customArgumentsLabel)
        CustomArgsSection.addWidget(self.CustomArgsLineEdit)
        CustomArgsSection.setFixedHeight(50)

        optionsSection.addWidget(self.VersionSection)
        optionsSection.addWidget(prereleaseSection)
        optionsSection.addWidget(ignoreUpdatesSection)
        optionsSection.addWidget(self.ArchSection)
        optionsSection.addWidget(self.ScopeSection)
        optionsSection.addWidget(self.LocationSection)
        optionsSection.addWidget(CustomArgsSection)
        optionsSection.addWidget(commandWidget)

        self.ShareButton = QPushButton(_("Share this package"))
        self.ShareButton.setFixedWidth(200)
        self.ShareButton.setStyleSheet("border-radius: 8px;")
        self.ShareButton.setFixedHeight(35)
        self.ShareButton.clicked.connect(lambda: nativeWindowsShare(self.title.text(), f"https://marticliment.com/wingetui/share?pid={self.currentPackage.Id}^&pname={self.currentPackage.Name}^&psource={self.currentPackage.Source}", self.window()))
        self.InstallButton = QPushButton()
        self.InstallButton.setText(_("Install"))
        self.InstallButton.setObjectName("AccentButton")
        self.InstallButton.setStyleSheet("border-radius: 8px;")
        self.InstallButton.setIconSize(QSize(24, 24))
        self.InstallButton.clicked.connect(self.install)
        self.InstallButton.setFixedWidth(200)
        self.InstallButton.setFixedHeight(35)

        hLayout.addWidget(self.ShareButton)
        hLayout.addStretch()
        hLayout.addWidget(self.InstallButton)

        vl = QVBoxLayout()
        vl.addStretch()
        vl.addLayout(hLayout)

        vl.addStretch()

        downloadGroupBox.setLayout(vl)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addWidget(optionsSection)

        self.setStyleSheet("margin: 0px;")
        self.setAttribute(Qt.WA_StyledBackground)

        self.layout.addSpacing(10)

        self.packageId = CustomLabel("<b>" + _('Package ID') + "</b> " + _('Unknown'))
        self.packageId.setWordWrap(True)
        self.layout.addWidget(self.packageId)
        self.manifest = CustomLabel("<b>" + _('Manifest') + "</b> " + _('Unknown'))
        self.manifest.setWordWrap(True)
        self.layout.addWidget(self.manifest)
        self.lastver = CustomLabel("<b>" + _('Latest Version:') + "</b> " + _('Unknown'))
        self.lastver.setWordWrap(True)
        self.layout.addWidget(self.lastver)
        self.sha = CustomLabel(f"<b>{_('Installer SHA256')} ({_('Latest Version')}):</b> " + _('Unknown'))
        self.sha.setWordWrap(True)
        self.layout.addWidget(self.sha)
        self.link = CustomLabel(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> " + _('Unknown'))
        self.link.setWordWrap(True)
        self.layout.addWidget(self.link)
        self.type = CustomLabel(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> " + _('Unknown'))
        self.type.setWordWrap(True)
        self.layout.addWidget(self.type)
        self.date = CustomLabel("<b>" + _('Last updated:') + "</b> " + _('Unknown'))
        self.date.setWordWrap(True)
        self.layout.addWidget(self.date)
        self.notes = CustomLabel("<b>" + _('Release notes:') + "</b> " + _('Unknown'))
        self.notes.setWordWrap(True)
        self.layout.addWidget(self.notes)
        self.notesurl = CustomLabel("<b>" + _('Release notes URL:') + "</b> " + _('Unknown'))
        self.notesurl.setWordWrap(True)
        self.layout.addWidget(self.notesurl)

        self.storeLabel = CustomLabel("<b>" + _("Source:") + "</b> ")
        self.storeLabel.setWordWrap(True)
        self.layout.addWidget(self.storeLabel)

        self.layout.addSpacing(10)
        self.layout.addStretch()
        self.advert = CustomLabel("<b>" + _("DISCLAIMER: WE ARE NOT RESPONSIBLE FOR THE DOWNLOADED PACKAGES. PLEASE MAKE SURE TO INSTALL ONLY TRUSTED SOFTWARE."))
        self.advert.setWordWrap(True)
        self.layout.addWidget(self.advert)

        self.mainGroupBox.setLayout(self.layout)
        self.mainGroupBox.setMinimumHeight(480)
        self.vLayout.addWidget(self.mainGroupBox)
        self.hLayout.addLayout(self.vLayout, stretch=0)

        self.centralwidget.setLayout(self.hLayout)
        self.baseScrollArea.setWidget(self.centralwidget)

        tempHLayout = QHBoxLayout()
        tempHLayout.setContentsMargins(0, 0, 0, 0)
        tempHLayout.addWidget(self.baseScrollArea)
        self.setLayout(tempHLayout)

        self.backButton = QPushButton("", self)
        self.backButton.move(self.width() - 40, 0)
        self.backButton.resize(40, 40)
        self.backButton.setFlat(True)
        self.backButton.clicked.connect(lambda: (self.onClose.emit(), self.close()))
        self.backButton.show()

        self.hide()
        self.loadInfo.connect(self.printData)

        self.baseScrollArea.horizontalScrollBar().setEnabled(False)
        self.baseScrollArea.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.verticalScrollbar = CustomScrollBar()
        self.baseScrollArea.setVerticalScrollBar(self.verticalScrollbar)
        self.verticalScrollbar.setParent(self)
        self.verticalScrollbar.show()
        self.verticalScrollbar.setFixedWidth(12)

        self.CustomArgsLineEdit.textChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.adminCheckbox.clicked.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.InteractiveCheckbox.clicked.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.HashCheckBox.clicked.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.VersionCombo.currentIndexChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.ArchCombo.currentIndexChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.ScopeCombo.currentIndexChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.InstallPreRelease.stateChanged.connect(lambda enabled: (self.loadPackageCommandLine(saveOptionsToDisk=True), self.VersionCombo.setEnabled(not enabled)))
        self.LocationSection.valueChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))
        self.LocationSection.stateChanged.connect(lambda: self.loadPackageCommandLine(saveOptionsToDisk=True))

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
        self.ShareButton.setIcon(QIcon(getMedia("share")))
        self.backButton.setIcon(QIcon(getMedia("close")))
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:red;}")
        self.screenshotsWidget.setStyleSheet(f"QScrollArea{{padding: 8px; border-radius: 8px; background-color: {'rgba(255, 255, 255, 5%)' if isDark() else 'rgba(255, 255, 255, 60%)'};border: 0px solid black;}};")
        self.CustomCommandLabel.ApplyIcons()
        for widget in self.imagesCarrousel:
            widget.clickableButton.setStyleSheet(f"QPushButton{{background-color: rgba(127, 127, 127, 1%);border: 0px;border-radius: 0px;}}QPushButton:hover{{background-color: rgba({'255, 255, 255' if not isDark() else '0, 0, 0'}, 10%)}}")

    def resizeEvent(self, event: QResizeEvent = None):
        self.centralwidget.setFixedWidth(self.width() - 18)
        self.LoadingIndicator.move(16, 0)
        self.LoadingIndicator.resize(self.width() - 32, 4)
        self.verticalScrollbar.move(self.width() - 16, 44)
        self.verticalScrollbar.resize(12, self.height() - 64)
        self.backButton.move(self.width() - 40, 0)
        self.imagesScrollbar.move(self.screenshotsWidget.x() + 22, self.screenshotsWidget.y() + self.screenshotsWidget.height() + 4)
        if event:
            return super().resizeEvent(event)

    def getInstallationOptions(self) -> InstallationOptions:
        options = InstallationOptions(self.currentPackage)
        options.RunAsAdministrator = self.adminCheckbox.isChecked()
        options.InteractiveInstallation = self.InteractiveCheckbox.isChecked()
        options.SkipHashCheck = self.HashCheckBox.isChecked()
        options.PreRelease = self.InstallPreRelease.isChecked()

        if self.LocationSection.isChecked() and self.LocationSection.currentValue() != "":
            options.CustomInstallLocation = self.LocationSection.currentValue()
        else:
            options.CustomInstallLocation = ""

        if self.VersionCombo.currentText() not in (_("Latest"), "Latest", "Loading...", _("Loading..."), ""):
            options.Version = self.VersionCombo.currentText()
        else:
            options.Version = ""
        if self.ArchCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            options.Architecture = self.ArchCombo.currentText()
            if options.Architecture in (_("Global"), "Global"):
                options.RunAsAdministrator = True
        else:
            options.Architecture = ""
        if self.ScopeCombo.currentText() not in (_("Default"), "Default", "Loading...", _("Loading..."), ""):
            options.InstallationScope = self.ScopeCombo.currentText()
        else:
            options.InstallationScope = ""
        options.CustomParameters = [c for c in self.CustomArgsLineEdit.text().split(" ") if c]
        return options

    def loadPackageCommandLine(self, saveOptionsToDisk: bool = False):
        options = self.getInstallationOptions()
        if saveOptionsToDisk and not self.isLoadingPackageDetails:
            options.SaveOptionsToDisk()

        Manager = self.currentPackage.PackageManager

        versionedId = self.currentPackage.Id
        if Manager is Pip and options.Version:
            versionedId += "==" + options.Version

        baseVerb = Manager.Properties.ExecutableName + " "
        baseVerb += Manager.Properties.UpdateVerb if self.isAnUpdate else (Manager.Properties.UninstallVerb if self.isAnUninstall else Manager.Properties.InstallVerb) + " "
        baseVerb += versionedId + " "

        self.CustomCommandLabel.setText(baseVerb + " ".join(Manager.getParameters(options, self.isAnUninstall)))
        self.CustomCommandLabel.setCursorPosition(0)

    def showPackageDetails(self, package: Package, update: bool = False, uninstall: bool = False, installedVersion: str = ""):
        self.isLoadingPackageDetails = True
        self.isAnUpdate = update
        self.isAnUninstall = uninstall
        if self.currentPackage == package:
            return
        self.currentPackage = package

        self.ApplyIcons()

        self.iv.resetImages()
        if "…" in package.Id:
            self.InstallButton.setEnabled(False)
            self.InstallButton.setText(_("Please wait..."))
        else:
            if self.isAnUpdate:
                self.InstallButton.setText(_("Update"))
            elif self.isAnUninstall:
                self.InstallButton.setText(_("Uninstall"))
            else:
                self.InstallButton.setText(_("Install"))

        self.title.setText(package.Name)

        self.loadPackageCommandLine()

        self.LoadingIndicator.show()

        self.description.setText(_("Loading..."))
        self.author.setText("<b>" + _("Author") + ":</b> " + _("Loading..."))
        self.publisher.setText(f"<b>{_('Publisher')}:</b> " + _("Loading..."))
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

        self.sha.setText(f"<b>{_('Installer SHA512') if package.isManager(Choco) or package.isManager(Npm) or package.isManager(Dotnet) else _('Installer SHA256')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {_('Loading...')}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {package.Id}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {_('Loading...')}")
        self.date.setText(f"<b>{_('Publication date:') if package.isManager(Npm) else _('Last updated:')}</b> {_('Loading...')}")
        self.notes.setText(f"<b>{_('Notes:') if package.isManager(Scoop) else _('Release notes:')}</b> {_('Loading...')}")
        self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {_('Loading...')}")
        self.storeLabel.setText(f"<b>{_('Source')}:</b> {package.Source}")
        self.VersionCombo.addItems([_("Loading...")])
        self.ArchCombo.addItems([_("Loading...")])
        self.ScopeCombo.addItems([_("Loading...")])

        def resetLayoutWidget():
            p = QPixmap()
            for viewer in self.imagesCarrousel:
                viewer.setPixmap(p, index=0)
            Thread(target=self.loadPackageScreenshots, args=(package,)).start()

        Capabilities = package.PackageManager.Capabilities
        self.adminCheckbox.setEnabled(Capabilities.CanRunAsAdmin)
        self.HashCheckBox.setEnabled(Capabilities.CanSkipIntegrityChecks)
        self.InteractiveCheckbox.setEnabled(Capabilities.CanRunInteractively)
        self.VersionSection.setEnabled(Capabilities.SupportsCustomVersions)
        self.ArchSection.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.ScopeSection.setEnabled(Capabilities.SupportsCustomScopes)
        self.LocationSection.setEnabled(Capabilities.SupportsCustomLocations)
        self.loadCachedInstallationOptions()

        self.callInMain.emit(lambda: resetLayoutWidget())
        self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(getMedia("install")).pixmap(64, 64)))
        Thread(target=self.loadPackageIcon, args=(package,)).start()

        Thread(target=self.loadPackageDetails, args=(package,), daemon=True, name=f"Loading details for {package}").start()

        self.tagsWidget.layout().clear()

        self.finishedCount = 0
        self.isLoadingPackageDetails = False

    def reposition(self):
        self.setGeometry((self.parent().width() - self.width()) / 2,
                         (self.parent().height() - self.height()) / 2,
                         self.width(),
                         self.height())

    def loadPackageDetails(self, package: Package):
        details = package.PackageManager.getPackageDetails(package)
        self.callInMain.emit(lambda: self.printData(details))

    def printData(self, details: PackageDetails) -> None:
        self.isLoadingPackageDetails = True
        if details.PackageObject != self.currentPackage:
            return
        package = self.currentPackage

        self.LoadingIndicator.hide()
        self.InstallButton.setEnabled(True)
        self.adminCheckbox.setEnabled(True)
        if self.isAnUpdate:
            self.InstallButton.setText(_("Update"))
        elif self.isAnUninstall:
            self.InstallButton.setText(_("Uninstall"))
        else:
            self.InstallButton.setText(_("Install"))

        self.InteractiveCheckbox.setEnabled(not package.isManager(Scoop))
        self.title.setText(details.Name)
        self.description.setText(details.Description)
        if package.isManager(Winget):
            self.author.setText(f"<b>{_('Author')}:</b> <a style=\"color: {blueColor};\" href='{details.Id.split('.')[0]}'>{details.Author}</a>")
            self.publisher.setText(f"<b>{_('Publisher')}:</b> <a style=\"color: {blueColor};\" href='{details.Id.split('.')[0]}'>{details.Publisher}</a>")
        else:
            self.author.setText(f"<b>{_('Author')}:</b> " + details.Author)
            self.publisher.setText(f"<b>{_('Publisher')}:</b> " + details.Publisher)
        self.homepage.setText(f"<b>{_('Homepage')}:</b> {details.asUrl(details.HomepageURL)}")
        if details.License != "" and details.LicenseURL != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.License} ({details.asUrl(details.LicenseURL)})")
        elif details.License != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.License}")
        elif details.LicenseURL != "":
            self.license.setText(f"<b>{_('License')}:</b> {details.asUrl(details.License)}")
        else:
            self.license.setText(f"<b>{_('License')}:</b> {_('Not available')}")
        self.sha.setText(f"<b>{_('Installer SHA512') if package.isManager(Choco) or package.isManager(Npm) or package.isManager(Dotnet) else _('Installer SHA256')} ({_('Latest Version')}):</b> {details.InstallerHash}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {details.asUrl(details.InstallerURL)} {f'({details.InstallerSize} MB)' if details.InstallerSize > 0 else ''}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {details.InstallerType}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {details.Id}")
        self.date.setText(f"<b>{_('Publication date:') if package.isManager(Npm) else _('Last updated:')}</b> {details.UpdateDate}")
        self.notes.setText(f"<b>{_('Notes:') if package.isManager(Scoop) else _('Release notes:')}</b> {details.ReleaseNotes}")
        self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {details.asUrl(details.ReleaseNotesUrl)}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {details.asUrl(details.ManifestUrl)}")
        while self.VersionCombo.count() > 0:
            self.VersionCombo.removeItem(0)
        self.VersionCombo.addItems([_("Latest")] + details.Versions)
        while self.ArchCombo.count() > 0:
            self.ArchCombo.removeItem(0)
        self.ArchCombo.addItems([_("Default")] + details.Architectures)
        while self.ScopeCombo.count() > 0:
            self.ScopeCombo.removeItem(0)
        self.ScopeCombo.addItems([_("Default")] + details.Scopes)

        for tag in details.Tags:
            label = QLabel(tag)
            label.setStyleSheet(f"padding: 5px;padding-bottom: 2px;padding-top: 2px;background-color: {blueColor if isDark() else f'rgb({getColors()[0]})'}; color: black; border-radius: 10px;")
            label.setFixedHeight(20)
            self.tagsWidget.layout().addWidget(label)

        Capabilities = package.PackageManager.Capabilities
        self.adminCheckbox.setEnabled(Capabilities.CanRunAsAdmin)
        self.HashCheckBox.setEnabled(Capabilities.CanSkipIntegrityChecks)
        self.InteractiveCheckbox.setEnabled(Capabilities.CanRunInteractively)
        self.VersionSection.setEnabled(Capabilities.SupportsCustomVersions)
        self.InstallPreRelease.setEnabled(Capabilities.SupportsPreRelease)
        self.ArchSection.setEnabled(Capabilities.SupportsCustomArchitectures)
        self.ScopeSection.setEnabled(Capabilities.SupportsCustomScopes)
        self.LocationSection.setEnabled(Capabilities.SupportsCustomLocations)
        self.isLoadingPackageDetails = False

    def loadCachedInstallationOptions(self):
        self.isLoadingPackageDetails = True
        options = InstallationOptions(self.currentPackage)
        self.adminCheckbox.setChecked(options.RunAsAdministrator)
        self.InteractiveCheckbox.setChecked(options.InteractiveInstallation)
        self.HashCheckBox.setChecked(options.SkipHashCheck)
        self.InstallPreRelease.setChecked(options.PreRelease)
        self.LocationSection.setChecked(options.CustomInstallLocation != "")
        self.LocationSection.setValue(options.CustomInstallLocation)
        try:
            self.ArchCombo.setCurrentText(options.Architecture)
        except Exception as e:
            report(e)
        try:
            self.ScopeCombo.setCurrentText(options.InstallationScope)
        except Exception as e:
            report(e)
        self.CustomArgsLineEdit.setText(" ".join([c for c in options.CustomParameters if c]))
        self.isLoadingPackageDetails = False
        self.loadPackageCommandLine()

    def loadPackageIcon(self, package: Package) -> None:
        try:
            id = package.Id
            iconpath = package.getPackageIcon()
            if iconpath:
                if self.currentPackage.Id == id:
                    self.callInMain.emit(lambda: self.appIcon.setPixmap(QIcon(iconpath).pixmap(64, 64)))
                else:
                    print("🟡 Icon arrived too late!")
            else:
                print("🟡 An empty icon path was received")
        except Exception as e:
            report(e)

    def loadPackageScreenshots(self, package: Package) -> None:
        try:
            id = package.Id
            self.validImageCount = 0
            self.canContinueWithImageLoading = 0
            iconId = package.getIconId()
            count = 0
            for i in range(len(Globals.packageMeta["icons_and_screenshots"][iconId]["images"])):
                try:
                    p = QPixmap(getMedia("placeholder_image")).scaledToHeight(128, Qt.SmoothTransformation)
                    if not p.isNull():
                        self.callInMain.emit(self.imagesCarrousel[i].show)
                        self.callInMain.emit(partial(self.imagesCarrousel[i].setPixmap, p, count))
                        count += 1
                except Exception as e:
                    report(e)
            for i in range(count + 1, 20):
                self.callInMain.emit(self.imagesCarrousel[i].hide)
            for i in range(len(Globals.packageMeta["icons_and_screenshots"][iconId]["images"])):
                try:
                    imagepath = os.path.join(ICON_DIR, f"{iconId}.screenshot.{i}.png")
                    if not os.path.exists(ICON_DIR):
                        os.makedirs(ICON_DIR)
                    if not os.path.exists(imagepath):
                        iconurl = Globals.packageMeta["icons_and_screenshots"][iconId]["images"][i]
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
            for i in range(self.validImageCount + 1, 20):
                self.callInMain.emit(self.imagesCarrousel[i].hide)

        except Exception as e:
            try:
                if type(e) is not KeyError:
                    report(e)
                else:
                    print(f"🟡 Image {iconId} not found in json")
            except Exception as e:
                report(e)

    def install(self):
        print(f"🟢 Starting installation of package {self.currentPackage.Name} with id {self.currentPackage.Id}")
        if self.IgnoreFutureUpdates.isChecked():
            self.currentPackage.AddToIgnoredUpdates()
            print(f"🟡 Blacklising package {self.currentPackage.Id}")

        options = self.getInstallationOptions()
        options.SaveOptionsToDisk()

        if self.isAnUpdate:
            p = PackageUpdaterWidget(self.currentPackage, options)
        elif self.isAnUninstall:
            p = PackageUninstallerWidget(self.currentPackage, options)
        else:
            p = PackageInstallerWidget(self.currentPackage, options)
        self.addProgram.emit(p)
        self.close()

    def show(self) -> None:
        self.blackCover.hide()
        g = QRect(0, 0, self.parent().window().geometry().width(), self.parent().window().geometry().height())
        self.resize(700, 650)
        self.parent().window().blackmatt.show()
        self.move(g.x() + g.width() // 2 - 700 // 2, g.y() + g.height() // 2 - 650 // 2)
        self.raise_()
        if not self.backgroundApplied:
            Globals.centralWindowLayout.setGraphicsEffect(self.blurBackgroundEffect)
            self.backgroundApplied = True
        self.blurBackgroundEffect.setEnabled(True)
        self.blurBackgroundEffect.setBlurRadius(40)
        backgroundImage = Globals.centralWindowLayout.grab(QRect(QPoint(0, 0), Globals.centralWindowLayout.size()))
        self.blurBackgroundEffect.setEnabled(False)
        self.imagesScrollbar.move(self.screenshotsWidget.x() + 22, self.screenshotsWidget.y() + self.screenshotsWidget.height() + 4)
        self.blackCover.resize(self.width(), self.centralwidget.height())
        if Globals.centralWindowLayout:
            Globals.centralTextureImage.setPixmap(backgroundImage)
            Globals.centralTextureImage.show()
            Globals.centralWindowLayout.hide()
        _ = super().show()
        return _

    def close(self) -> bool:
        self.blackCover.hide()
        self.iv.close()
        self.parent().window().blackmatt.hide()
        self.blurBackgroundEffect.setEnabled(False)
        if Globals.centralWindowLayout:
            Globals.centralTextureImage.hide()
            Globals.centralWindowLayout.show()
        return super().close()

    def hide(self) -> None:
        self.blackCover.hide()
        try:
            self.parent().window().blackmatt.hide()
        except AttributeError:
            pass
        self.blurBackgroundEffect.setEnabled(False)
        self.iv.close()
        if Globals.centralWindowLayout:
            Globals.centralTextureImage.hide()
            Globals.centralWindowLayout.show()
        return super().hide()

    def showEvent(self, event: QShowEvent):
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            self.window().OnThemeChange.connect(self.ApplyIcons)
        return super().showEvent(event)
