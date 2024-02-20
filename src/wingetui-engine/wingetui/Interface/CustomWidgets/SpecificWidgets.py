
class AnnouncementsPane(QLabel):

    def __init__(self):
        super().__init__()
        self.area = SmoothScrollArea()
        self.setMaximumWidth(self.getPx(1000))
        self.callInMain.connect(lambda f: f())
        self.setFixedHeight(self.getPx(110))
        self.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setStyleSheet(f"#subtitleLabel{{border-bottom-left-radius: {self.getPx(4)}px;border-bottom-right-radius: {self.getPx(4)}px;border-bottom: {self.getPx(1)}px;font-size: 12pt;}}*{{padding: 3px;}}")
        self.setTtext("Fetching latest announcement, please wait...")
        layout = QHBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        self.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        self.pictureLabel = QLabel()
        self.pictureLabel.setContentsMargins(0, 0, 0, 0)
        self.pictureLabel.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        self.textLabel = QLabel()
        self.textLabel.setOpenExternalLinks(True)
        self.textLabel.setContentsMargins(self.getPx(10), 0, self.getPx(10), 0)
        self.textLabel.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        layout.addStretch()
        layout.addWidget(self.textLabel, stretch=0)
        layout.addWidget(self.pictureLabel, stretch=0)
        layout.addStretch()
        self.w = QWidget()
        self.w.setObjectName("backgroundWindow")
        self.w.setLayout(layout)
        self.pictureLabel.setText("Loading media...")
        self.w.setContentsMargins(0, 0, 0, 0)
        self.area.setWidget(self.w)
        vLayout = QVBoxLayout()
        vLayout.setSpacing(0)
        vLayout.setContentsMargins(0, self.getPx(5), 0, self.getPx(5))
        vLayout.addWidget(self.area, stretch=1)
        self.area.setWidgetResizable(True)
        self.area.setContentsMargins(0, 0, 0, 0)
        self.area.setObjectName("backgroundWindow")
        self.area.setStyleSheet("border: 0px solid black; padding: 0px; margin: 0px;")
        self.area.setFrameShape(QFrame.NoFrame)
        self.area.setHorizontalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.area.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.pictureLabel.setFixedHeight(self.area.height())
        self.textLabel.setFixedHeight(self.area.height())
        self.setLayout(vLayout)

    def loadAnnouncements(self, useHttps: bool = True):
        try:
            response = urlopen(f"http{'s' if useHttps else ''}://www.marticliment.com/resources/wingetui.announcement")
            print("🔵 Announcement URL:", response.url)
            response = response.read().decode("utf8")
            self.callInMain.emit(lambda: self.setTtext(""))
            announcement_body = response.split("////")[0].strip().replace("http://", "ignore:").replace("https://", "ignoreSecure:").replace("linkId", "http://marticliment.com/redirect/").replace("linkColor", f"rgb({getColors()[2 if isDark() else 4]})")
            self.callInMain.emit(lambda: self.textLabel.setText(announcement_body))
            announcement_image_url = response.split("////")[1].strip()
            try:
                response = urlopen(announcement_image_url)
                print("🔵 Image URL:", response.url)
                response = response.read()
                self.file = open(os.path.join(ICON_DIR, "announcement.png"), "wb")
                self.file.write(response)
                self.callInMain.emit(lambda: self.pictureLabel.setText(""))
                self.file.close()
                h = self.area.height()
                self.callInMain.emit(lambda: self.pictureLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.textLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.pictureLabel.setPixmap(QPixmap(self.file.name).scaledToHeight(h - self.getPx(8), Qt.SmoothTransformation)))
            except Exception as e:
                s = "Couldn't load the announcement image" + "\n\n" + str(e)
                self.callInMain.emit(lambda: self.pictureLabel.setText(s))
                print("🟠 Unable to retrieve announcement image")
                report(e)
        except Exception as e:
            if useHttps:
                self.loadAnnouncements(useHttps=False)
            else:
                s = "Couldn't load the announcements. Please try again later" + "\n\n" + str(e)
                self.callInMain.emit(lambda: self.setTtext(s))
                print("🟠 Unable to retrieve latest announcement")
                report(e)

    def showEvent(self, a0: QShowEvent) -> None:
        return super().showEvent(a0)

    def getPx(self, i: int) -> int:
        return i

    def setTtext(self, a0: str) -> None:
        return super().setText(a0)

    def setText(self, a: str) -> None:
        raise Exception("This member should not be used under any circumstances")


class PackageItem(QTreeWidgetItem):
    class Tag():
        Default = 0
        Installed = 1
        Upgradable = 2
        Pinned = 3
        Pending = 4
        BeingProcessed = 5
        Failed = 6

    Package: 'Package' = None
    CurrentTag: 'Tag' = Tag.Default
    __item_action: QAction = None
    SoftwareSection: 'SoftwareSection' = None
    callInMain: Signal = None

    def __init__(self, package: 'Package'):
        if not self.SoftwareSection:
            self.SoftwareSection = Globals.discover
        self.Package = package
        self.Package.PackageItem = self
        self.callInMain: Signal = Globals.mainWindow.callInMain
        super().__init__()
        self.setChecked(False)
        self.setText(1, self.Package.Name)
        self.setTag(PackageItem.Tag.Default)
        self.setText(2, self.Package.Id)
        self.setIcon(2, getIcon("ID"))
        self.setText(3, self.Package.Version if self.Package.Version != "Unknown" else _("Unknown"))
        self.setIcon(3, getIcon("newversion"))
        self.setText(4, package.Source)
        self.setIcon(4, package.getSourceIcon())
        self.setText(6, self.Package.getFloatVersion())
        self.updateCorrespondingPackages()

    def updateCorrespondingPackages(self) -> None:
        UpgradableItem = self.getUpdatesPackageItem()
        InstalledItem = self.getInstalledPackageItem()
        if UpgradableItem:
            self.setTag(PackageItem.Tag.Upgradable)
        elif InstalledItem:
            self.setTag(PackageItem.Tag.Installed)

    def setTag(self, tag: Tag, newVersion: str = ""):
        self.CurrentTag = tag
        try:
            match self.CurrentTag:
                case PackageItem.Tag.Default:
                    self.setIcon(1, getIcon("install"))
                    self.setToolTip(1, self.Package.Name)

                case PackageItem.Tag.Installed:
                    self.setIcon(1, getMaskedIcon("installed_masked"))
                    self.setToolTip(1, _("This package is already installed") + " - " + self.Package.Name)

                case PackageItem.Tag.Upgradable:
                    self.setIcon(1, getMaskedIcon("update_masked"))
                    if newVersion:
                        self.setToolTip(1, _("This package can be updated to version {0}").format(newVersion) + " - " + self.Package.Name)
                    else:
                        self.setToolTip(1, _("This package can be updated") + " - " + self.Package.Name)

                case PackageItem.Tag.Pinned:
                    self.setIcon(1, getMaskedIcon("pin_masked"))
                    self.setToolTip(1, _("Updates for this package are ignored") + " - " + self.Package.Name)

                case PackageItem.Tag.Pending:
                    self.setIcon(1, getIcon("queued"))
                    self.setToolTip(1, _("This package is on the queue") + " - " + self.Package.Name)

                case PackageItem.Tag.BeingProcessed:
                    self.setIcon(1, getMaskedIcon("gears_masked"))
                    self.setToolTip(1, _("This package is being processed") + " - " + self.Package.Name)

                case PackageItem.Tag.Failed:
                    self.setIcon(1, getMaskedIcon("warning_masked"))
                    self.setToolTip(1, _("An error occurred while processing this package") + " - " + self.Package.Name)
        except RuntimeError:
            pass

    def removeFromList(self) -> None:
        try:
            self.setHidden(True)
            if self in self.SoftwareSection.packageItems:
                self.SoftwareSection.packageItems.remove(self)
            if self in self.SoftwareSection.showableItems:
                self.SoftwareSection.showableItems.remove(self)
            if self.treeWidget():
                self.treeWidget().takeTopLevelItem(self.treeWidget().indexOfTopLevelItem(self))
        except RuntimeError:
            pass
        self.SoftwareSection.updatePackageNumber()


class UpgradablePackageItem(PackageItem):
    Package: 'UpgradablePackage' = None

    def updateCorrespondingPackages(self) -> None:
        InstalledItem = self.getInstalledPackageItem()
        if InstalledItem:
            InstalledItem.setTag(PackageItem.Tag.Upgradable, self.Package.NewVersion)
        AvailableWidget = self.getDiscoverPackageItem()
        if AvailableWidget:
            AvailableWidget.setTag(PackageItem.Tag.Upgradable, self.Package.NewVersion)


class InstalledPackageItem(PackageItem):

    def updateCorrespondingPackages(self) -> None:
        if self.Package.HasUpdatesIgnored():
            if self.Package.GetIgnoredUpatesVersion() == "*":
                self.setTag(PackageItem.Tag.Pinned)

        AvailableItem = self.getDiscoverPackageItem()
        if AvailableItem:
            AvailableItem.setTag(PackageItem.Tag.Installed)

        UpgradableItem = self.getUpdatesPackageItem()
        if UpgradableItem:
            self.setTag(PackageItem.Tag.Upgradable, UpgradableItem.Package.NewVersion)


if __name__ == "__main__":
    import __init__
