"""

wingetui/Interface/SoftwareSections.py

This file contains the code for the following classes:
 - DiscoverSoftwareSection
 - UpdateSoftwareSection
 - UninstallSoftwareSection
 - PackageInfoPopupWindow

Those classes are the classes that represent the three main tabs on WingetUI's interface.
The class PackageInfoPopupWindow contains the code for the Package Details window.

"""

if __name__ == "__main__":
    import subprocess
    import os
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "..")).returncode)


import os
import sys
import time
from threading import Thread

import globals
from Interface.CustomWidgets.SpecificWidgets import *
from PackageManagers.PackageClasses import PackageManagerModule, DynamicPackageManager
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from Interface.CustomWidgets.InstallerWidgets import *
from tools import *
from tools import _

from Interface.GenericSections import *


class DiscoverSoftwareSection(SoftwareSection):
    PackageManagers = StaticPackageManagersList.copy()
    PackagesLoaded = StaticPackagesLoadedDict.copy()

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
            Capabilities: PackageManagerCapabilities = self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
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
                self.loadingProgressBar.hide()
                self.packageList.label.show()
                self.packageList.label.setText(_("Search for packages to start"))
            self.updateFilterTable()
        else:
            self.showableItems = []
            self.addItemsToTreeWidget(reset=True)
            self.updateFilterTable()
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

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager] and manager.isEnabled():
                return

        self.reloadButton.setEnabled(True)
        self.loadingProgressBar.hide()
        self.countLabel.setText(_("Found packages: {0}").format(str(itemCount)))
        print("🟢 Total packages: " + str(itemCount))

    def finishDynamicLoadingIfNeeded(self) -> None:
        self.finishFiltering(self.query.text())
        if len(self.showableItems) == 0 and len(self.query.text()) >= 3:
            self.packageList.label.setText(_("No packages found matching the input criteria"))
        else:
            self.packageList.label.setText(_(""))

        if not self.isLoadingDynamicPackages():
            self.loadingProgressBar.hide()

    def isLoadingDynamicPackages(self) -> bool:
        return self.runningThreads > 0

    def addItem(self, package: Package) -> None:
        if "---" not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and version not in ("the", "is"):

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
                installedPackage: Package = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems and package.Source == installedPackage.Source:
                    item.setIcon(1, self.installedIcon)
                    item.setToolTip(1, _("This package is already installed") + " - " + package.Name)

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

    def loadPackages(self, manager: PackageManagerModule) -> None:
        packages = manager.getAvailablePackages()
        for package in packages:
            self.addProgram.emit(package)
        self.PackagesLoaded[manager] = True
        self.finishLoading.emit()

    def loadDynamicPackages(self, query: str, manager: DynamicPackageManager) -> None:
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
    PackageItemReference: dict[UpgradablePackage:TreeWidgetItemWithQAction] = {}
    ItemPackageReference: dict[TreeWidgetItemWithQAction:UpgradablePackage] = {}
    IdPackageReference: dict[str:UpgradablePackage] = {}
    UpdatesNotification: ToastNotification = None

    def __init__(self, parent=None):
        super().__init__(parent=parent, sectionName="Update")

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
        self.updateIcon = getMaskedIcon("update_masked")
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
            Capabilities: PackageManagerCapabilities = self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
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
            for program in self.packageItems:
                if not program.isHidden():
                    try:
                        if program.checkState(0) == Qt.CheckState.Checked:
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
            # helpMenu.addAction(self.HelpMenuEntry1)
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

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
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
                    self.UpdatesNotification.setDescription(_("{0} packages are being updated").format(count) + ":")
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1) + ", "
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
                    self.UpdatesNotification.setDescription(_("{0} packages can be updated").format(count) + ":")
                    self.UpdatesNotification.addAction(_("Update all"), self.updateAllPackageItems)
                    packageList = ""
                    for item in self.packageItems:
                        packageList += item.text(1) + ", "
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
        print("🟢 Total packages: " + str(len(self.packageItems)))

    def changeStore(self, package: UpgradablePackage):
        time.sleep(3)
        try:
            UNINSTALL_SECTION: UninstallSoftwareSection = globals.uninstall
            package.Source = UNINSTALL_SECTION.IdPackageReference[package.Id].Source
        except KeyError:
            print(f"🟠 Package {package.Id} found in the updates section but not in the installed one, happened again")
        self.callInMain.emit(partial(package.PackageItem.setText, 5, package.Source))

    def addItem(self, package: UpgradablePackage) -> None:
        if "---" not in package.Name and "The following packages" not in package.Name and "Name  " not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and package.Version.lower() not in ("the", "is", "install") and package.NewVersion not in ("Manifest", package.Version):
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
                except KeyError:
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
            action = QAction(package.Name + "  \t" + package.Version + "\t → \t" + package.NewVersion, globals.trayMenuUpdatesList)
            action.triggered.connect(lambda: self.updatePackageItem(item))
            action.setShortcut(package.Version)
            item.setAction(action)
            globals.trayMenuUpdatesList.addAction(action)

            UNINSTALL: UninstallSoftwareSection = globals.uninstall
            if package.Id in UNINSTALL.IdPackageReference.keys():
                installedPackage: UpgradablePackage = UNINSTALL.IdPackageReference[package.Id]
                installedItem = installedPackage.PackageItem
                if installedItem in UNINSTALL.packageItems:
                    installedItem.setIcon(1, self.updateIcon)
                    installedItem.setToolTip(1, _("This package can be updated to version {0}").format(package.NewVersion) + " - " + package.Name)

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
            if not item.isHidden() and item.checkState(0) == Qt.CheckState.Checked:
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
    FilterItemForManager = {}

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
        packageItem.setToolTip(1, _("Updates for this package are ignored") + " - " + packageItem.text(1))

    def showContextMenu(self, pos: QPoint) -> None:
        if not self.packageList.currentItem():
            return
        if self.packageList.currentItem().isHidden():
            return
        ApplyMenuBlur(self.contextMenu.winId().__int__(), self.contextMenu)

        try:
            Capabilities: PackageManagerCapabilities = self.ItemPackageReference[self.packageList.currentItem()].PackageManager.Capabilities
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
            for program in self.packageItems:
                if not program.isHidden():
                    try:
                        if program.checkState(0) == Qt.CheckState.Checked:
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
            # helpMenu.addAction(self.HelpMenuEntry1)
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

        for manager in self.PackageManagers:  # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager]:
                return

        self.reloadButton.setEnabled(True)
        self.filter()
        self.loadingProgressBar.hide()
        globals.trayMenuInstalledList.setTitle(_("{0} packages found").format(len(self.packageItems)))
        self.countLabel.setText(_("Found packages: {0}").format(len(self.packageItems)))
        self.packageList.label.setText("")
        print("🟢 Total packages: " + str(len(self.packageItems)))

    def addItem(self, package: Package) -> None:
        if "---" not in package.Name and package.Name not in ("+", "Scoop", "At", "The", "But", "Au") and package.Version not in ("the", "is"):
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
                item.setToolTip(1, _("Updates for this package are ignored") + " - " + package.Name)

            if package.Id in UPDATES.IdPackageReference.keys():
                updatePackage: UpgradablePackage = UPDATES.IdPackageReference[package.Id]
                updateItem = updatePackage.PackageItem
                if updateItem in UPDATES.packageItems:
                    item.setIcon(1, self.updateIcon)
                    item.setToolTip(1, _("This package can be updated to version {0}").format(updatePackage.NewVersion) + " - " + package.Name)

            DISCOVER: UninstallSoftwareSection = globals.discover
            if package.Id in DISCOVER.IdPackageReference.keys():
                discoverablePackage: UpgradablePackage = DISCOVER.IdPackageReference[package.Id]
                discoverableItem = discoverablePackage.PackageItem
                if discoverableItem in DISCOVER.packageItems:
                    discoverableItem.setIcon(1, self.installedIcon)
                    discoverableItem.setToolTip(1, _("This package is already installed") + " - " + package.Name)

            self.PackageItemReference[package] = item
            self.ItemPackageReference[item] = package
            self.IdPackageReference[package.Id] = package
            package.PackageItem = item
            self.packageItems.append(item)
            if self.containsQuery(item, self.query.text()):
                self.showableItems.append(item)

            action = QAction(package.Name + " \t" + package.Version, globals.trayMenuInstalledList)
            action.triggered.connect(lambda: (self.uninstallPackageItem(item)))
            action.setShortcut(package.Version)
            item.setAction(action)
            globals.trayMenuInstalledList.addAction(action)

    def confirmUninstallSelected(self, toUninstall: list[TreeWidgetItemWithQAction], a: CustomMessageBox, admin: bool = False, interactive: bool = False, removeData: bool = False):
        questionData = {
            "titlebarTitle": _("Uninstall"),
            "mainTitle": _("Are you sure?"),
            "mainText": _("Do you really want to uninstall {0}?").format(toUninstall[0].text(1)) if len(toUninstall) == 1 else _("Do you really want to uninstall {0} packages?").format(len(toUninstall)),
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

    def loadPackages(self, manager: PackageManagerModule) -> None:
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
        super().__init__(parent=parent)
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
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not self.loadingProgressBar.invertedAppearance()))

        self.vLayout = QVBoxLayout()
        self.layout = QVBoxLayout()
        self.title = CustomLabel()
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
        self.description = CustomLabel("<b>" + _('Description:') + "</b> " + _('Unknown'))
        self.description.setWordWrap(True)

        self.layout.addWidget(self.tagsWidget)
        self.layout.addWidget(self.description)

        self.homepage = CustomLabel("<b>" + _('Homepage') + ":</b> " + _('Unknown'))
        self.homepage.setWordWrap(True)

        self.layout.addWidget(self.homepage)

        self.publisher = CustomLabel("<b>" + _('Publisher') + ":</b> " + _('Unknown'))
        self.publisher.setOpenExternalLinks(False)
        self.publisher.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
        self.publisher.setWordWrap(True)

        self.layout.addWidget(self.publisher)

        self.author = CustomLabel("<b>" + _('Author') + ":</b> " + _('Unknown'))
        self.author.setOpenExternalLinks(False)
        self.author.linkActivated.connect(lambda t: (self.close(), globals.discover.query.setText(t), globals.discover.filter(), globals.mainWindow.buttonBox.buttons()[0].click()))
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

        commandWidget = SectionHWidget(lastOne=True)
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
        self.setStyleSheet("margin: 0px;")
        self.backButton.move(self.width() - 40, 0)
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

    def resizeEvent(self, event: QResizeEvent = None):
        self.centralwidget.setFixedWidth(self.width() - 18)
        self.loadingProgressBar.move(16, 0)
        self.loadingProgressBar.resize(self.width() - 32, 4)
        self.verticalScrollbar.move(self.width() - 16, 44)
        self.verticalScrollbar.resize(12, self.height() - 64)
        self.backButton.move(self.width() - 40, 0)
        self.imagesScrollbar.move(self.screenshotsWidget.x() + 22, self.screenshotsWidget.y() + self.screenshotsWidget.height() + 4)
        if event:
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
            if "…" not in self.currentPackage.Id:
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
                idtoInstall += "==" + self.versionCombo.currentText()
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
            for viewer in self.imagesCarrousel:
                viewer.setPixmap(p, index=0)
            Thread(target=self.loadPackageScreenshots, args=(package,)).start()

        Capabilities = package.PackageManager.Capabilities
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

    def reposition(self):
        self.setGeometry((self.parent().width() - self.width()) / 2,
                         (self.parent().height() - self.height()) / 2,
                         self.width(),
                         self.height())

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
        self.sha.setText(f"<b>{_('Installer SHA512') if package.isManager(Choco) or package.isManager(Npm) else _('Installer SHA256')} ({_('Latest Version')}):</b> {details.InstallerHash}")
        self.link.setText(f"<b>{_('Installer URL')} ({_('Latest Version')}):</b> {details.asUrl(details.InstallerURL)} {f'({details.InstallerSize} MB)' if details.InstallerSize > 0 else ''}")
        self.type.setText(f"<b>{_('Installer Type')} ({_('Latest Version')}):</b> {details.InstallerType}")
        self.packageId.setText(f"<b>{_('Package ID')}:</b> {details.Id}")
        self.date.setText(f"<b>{_('Publication date:') if package.isManager(Npm) else _('Last updated:')}</b> {details.UpdateDate}")
        self.notes.setText(f"<b>{_('Notes:') if package.isManager(Scoop) else _('Release notes:')}</b> {details.ReleaseNotes}")
        self.notesurl.setText(f"<b>{_('Release notes URL:')}</b> {details.asUrl(details.ReleaseNotesUrl)}")
        self.manifest.setText(f"<b>{_('Manifest')}:</b> {details.asUrl(details.ManifestUrl)}")
        while self.versionCombo.count() > 0:
            self.versionCombo.removeItem(0)
        self.versionCombo.addItems([_("Latest")] + details.Versions)
        while self.architectureCombo.count() > 0:
            self.architectureCombo.removeItem(0)
        self.architectureCombo.addItems([_("Default")] + details.Architectures)
        while self.scopeCombo.count() > 0:
            self.scopeCombo.removeItem(0)
        self.scopeCombo.addItems([_("Default")] + details.Scopes)

        for tag in details.Tags:
            label = QLabel(tag)
            label.setStyleSheet(f"padding: 5px;padding-bottom: 2px;padding-top: 2px;background-color: {blueColor if isDark() else f'rgb({getColors()[0]})'}; color: black; border-radius: 10px;")
            label.setFixedHeight(20)
            self.tagsWidget.layout().addWidget(label)

        Capabilities = package.PackageManager.Capabilities
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
            for i in range(count + 1, 20):
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
            for i in range(self.validImageCount + 1, 20):
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
        self.move(g.x() + g.width() // 2 - 700 // 2, g.y() + g.height() // 2 - 650 // 2)
        self.raise_()
        if not self.backgroundApplied:
            globals.centralWindowLayout.setGraphicsEffect(self.blurBackgroundEffect)
            self.backgroundApplied = True
        self.blurBackgroundEffect.setEnabled(True)
        self.blurBackgroundEffect.setBlurRadius(40)
        backgroundImage = globals.centralWindowLayout.grab(QRect(QPoint(0, 0), globals.centralWindowLayout.size()))
        self.blurBackgroundEffect.setEnabled(False)
        self.imagesScrollbar.move(self.screenshotsWidget.x() + 22, self.screenshotsWidget.y() + self.screenshotsWidget.height() + 4)
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
