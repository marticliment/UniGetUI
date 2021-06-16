from PySide2 import QtWidgets, QtCore, QtGui
import WingetTools
from threading import Thread

class Discover(QtWidgets.QWidget):

    addProgram = QtCore.Signal(str, str, str)
    clearList = QtCore.Signal()

    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.infobox = Program()
        self.infobox.onClose.connect(self.showQuery)

        self.layout = QtWidgets.QVBoxLayout()
        self.setLayout(self.layout)

        self.reloadButton = QtWidgets.QPushButton()
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.clicked.connect(self.reload)
        self.reloadButton.setIcon(QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/reload.png"))

        hLayout = QtWidgets.QHBoxLayout()

        self.query = QtWidgets.QLineEdit()
        self.query.setPlaceholderText(" Search something on Winget")
        self.query.textChanged.connect(self.filter)
        self.query.setFixedHeight(30)

        hLayout.addWidget(self.query)
        hLayout.addWidget(self.reloadButton)

        self.packageList = QtWidgets.QTreeWidget()
        self.packageList.setIconSize(QtCore.QSize(24, 24))
        self.packageList.setColumnCount(3)
        self.packageList.setHeaderLabels(["Package name", "Package ID", "Version"])
        self.packageList.setColumnWidth(0, 300)
        self.packageList.setColumnWidth(1, 300)
        self.packageList.setColumnWidth(2, 200)
        self.packageList.setColumnWidth(3, 100)
        self.packageList.setSortingEnabled(True)
        self.packageList.sortByColumn(0, QtCore.Qt.AscendingOrder)
        self.packageList.itemDoubleClicked.connect(lambda item, column: self.openInfo(item.text(0), item.text(1)))

        self.layout.addLayout(hLayout)
        self.layout.addWidget(self.packageList)
        self.layout.addWidget(self.infobox)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)
        self.clearList.connect(self.packageList.clear)
    

        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, ""), daemon=True).start()
        print("[   OK   ] Discover tab loaded")

    def addItem(self, name: str, id: str, version: str) -> None:
        item = QtWidgets.QTreeWidgetItem()
        item.setText(0, name)
        item.setText(1, id)
        item.setIcon(0, QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/install.png"))
        item.setIcon(1, QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/ID.png"))
        item.setIcon(2, QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/version.png"))
        self.reloadButton.setEnabled(True)
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
        self.packageList.show()
        self.query.show()
        self.reloadButton.show()
        self.infobox.hide()

    def openInfo(self, title, id) -> None:
        if("…" in title):
            self.infobox.loadProgram(id.replace("…", ""), id.replace("…", ""), goodTitle=False)
        else:
            self.infobox.loadProgram(title.replace("…", ""), id.replace("…", ""), goodTitle=True)
        self.packageList.hide()
        self.query.hide()
        self.reloadButton.hide()
        self.infobox.show()
    
    def reload(self) -> None:
        self.reloadButton.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        Thread(target=WingetTools.searchForPackage, args=(self.addProgram, ""), daemon=True).start()
    

class Installed(QtWidgets.QWidget):
    def __init__(self, parent=None):
        super().__init__(parent=parent)

class Update(QtWidgets.QWidget):
    def __init__(self, parent=None):
        super().__init__(parent=parent)

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


class Program(QtWidgets.QWidget):
    onClose = QtCore.Signal()
    loadInfo = QtCore.Signal(dict)
    closeDialog = QtCore.Signal()
    addInfoLine = QtCore.Signal(str)
    finishInstallation = QtCore.Signal(int)
    def __init__(self):
        super().__init__()
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
        fortyTopWidget.setMinimumHeight(60)

        self.closeDialog.connect(self.progressDialog.close)
        self.addInfoLine.connect(self.progressDialog.addTextLine)
        self.finishInstallation.connect(lambda code: self.closeAndInform(code))

        mainGroupBox = QtWidgets.QGroupBox()

        self.vLayout.addWidget(fortyTopWidget)
        self.layout.addWidget(self.title)
        self.layout.addWidget(QtWidgets.QLabel())

        self.hLayout = QtWidgets.QHBoxLayout()
        self.hLayout.addWidget(fortyWidget, stretch=1)

        self.description = QtWidgets.QLabel("Description: Unknown")

        self.layout.addWidget(self.description)

        self.homepage = QLinkLabel("Homepage URL: Unknown")

        self.layout.addWidget(self.homepage)

        self.publisher = QtWidgets.QLabel("Publisher: Unknown")

        self.layout.addWidget(self.publisher)

        self.author = QtWidgets.QLabel("Author: Unknown")

        self.layout.addWidget(self.author)
        self.layout.addWidget(QtWidgets.QLabel())

        self.license = QLinkLabel("License: Unknown")

        self.layout.addWidget(self.license)
        self.layout.addWidget(QtWidgets.QLabel())
        
        hLayout = QtWidgets.QHBoxLayout()
        self.versionLabel = QtWidgets.QLabel("Version: ")
        self.versionCombo = QtWidgets.QComboBox()
        self.versionCombo.setFixedWidth(150)
        self.installButton = QtWidgets.QPushButton()
        self.installButton.setText("Install")
        self.installButton.setIcon(QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/install.png"))
        self.installButton.clicked.connect(self.install)
        self.installButton.setFixedWidth(150)
        self.installButton.setFixedHeight(30)

        downloadGroupBox = QtWidgets.QGroupBox()

        hLayout.addWidget(self.versionLabel)
        hLayout.addWidget(self.versionCombo)
        hLayout.addWidget(QtWidgets.QLabel(), stretch=1)
        hLayout.addWidget(self.installButton)
        downloadGroupBox.setLayout(hLayout)
        self.layout.addWidget(downloadGroupBox)
        self.layout.addWidget(QtWidgets.QLabel())

        self.id = QLinkLabel("Program ID: Unknown")
        self.layout.addWidget(self.id)
        self.sha = QLinkLabel("Installer SHA256 (Lastest version): Unknown")
        self.layout.addWidget(self.sha)
        self.link = QLinkLabel("Installer URL (Lastest version): Unknown")
        self.layout.addWidget(self.link)
        self.type = QLinkLabel("Installer type (Lastest version): Unknown")
        self.layout.addWidget(self.type)
        self.layout.addWidget(QtWidgets.QLabel())
        self.advert = QLinkLabel("ALERT: NEITHER MICROSOFT NOR THE CREATORS OF WINGET UI STORE ARE RESPONSIBLE FOR THE DOWNLOADED SOFTWARE.<br> PROCEED WITH CAUTION")
        self.layout.addWidget(self.advert)

        mainGroupBox.setLayout(self.layout)
        self.vLayout.addWidget(mainGroupBox)
        self.vLayout.addWidget(fortyWidget, stretch=1)
        self.hLayout.addLayout(self.vLayout, stretch=0)
        self.hLayout.addWidget(fortyWidget, stretch=1)
        self.setLayout(self.hLayout)


        self.backButton = QtWidgets.QPushButton(QtGui.QIcon("C:/Users/marti/SPTPrograms/WinGetUI/wingetui/back.png"), "", self)
        self.backButton.setStyleSheet("font-size: 22px;")
        self.backButton.move(0, 0)
        self.backButton.resize(30, 30)
        self.backButton.clicked.connect(self.onClose.emit)
        self.backButton.show()

        self.loadInfo.connect(self.printData)
    
    def closeAndInform(self, returncode: int) -> None:
        print(returncode)
        self.progressDialog.close()
        if(returncode==0):
            QtWidgets.QMessageBox.information(self, "Winget UI Store", f"The package {self.title.text()} was installed successfully. (Winget output code is 0)")
        else:
            QtWidgets.QMessageBox.warning(self, "Winget UI Store", f"An error occurred while installing the package {self.title.text()}. (Winget output code is {returncode})")
    
    def loadProgram(self, title: str, id: str, goodTitle: bool) -> None:
        if(goodTitle):
            self.title.setText(title)
        else:
            self.title.setText(id)
        self.description.setText("Loading...")
        self.author.setText("Author: "+"Loading...")
        self.publisher.setText("Publisher: "+"Loading...")
        self.homepage.setText(f"Homepage: <a href=\"\">{'Loading...'}</a>")
        self.license.setText(f"License: {'Loading...'} (<a href=\"\">{'Loading...'}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {'Loading...'}")
        self.link.setText(f"Installer URL (Lastest version): <a href=\"\">{'Loading...'}</a>")
        self.type.setText(f"Installer type (Lastest version): {'Loading...'}")
        self.id.setText(f"Package ID: {'Loading...'}")
        self.versionCombo.addItems(["Loading..."])
        self.progressDialog.show()
        
        Thread(target=WingetTools.getInfo, args=(self.loadInfo, title, id, goodTitle), daemon=True).start()

    def printData(self, appInfo: dict) -> None:
        self.progressDialog.hide()
        self.title.setText(appInfo["title"])
        self.description.setText(appInfo["description"])
        self.author.setText("Author: "+appInfo["author"])
        self.publisher.setText("Publisher: "+appInfo["publisher"])
        self.homepage.setText(f"Homepage: <a href=\"{appInfo['homepage']}\">{appInfo['homepage']}</a>")
        self.license.setText(f"License: {appInfo['license']} (<a href=\"{appInfo['license-url']}\">{appInfo['license-url']}</a>)")
        self.sha.setText(f"Installer SHA256 (Lastest version): {appInfo['installer-sha256']}")
        self.link.setText(f"Installer URL (Lastest version): <a href=\"{appInfo['installer-url']}\">{appInfo['installer-url']}</a>")
        self.type.setText(f"Installer type (Lastest version): {appInfo['installer-type']}")
        self.id.setText(f"Package ID: {appInfo['id']}")
        while self.versionCombo.count()>0:
            self.versionCombo.removeItem(0)
        try:
            self.versionCombo.addItems(["Lastest"] + appInfo["versions"])
        except KeyError:
            pass

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
        self.progressDialog.show()
        Thread(target=WingetTools.install, args=(self.finishInstallation, self.addInfoLine, title, version), daemon=True).start()
        



if(__name__=="__main__"):
    import __init__