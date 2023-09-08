from datetime import datetime
from functools import partial
from typing import Optional

import PySide6.QtCore
import PySide6.QtWidgets
from genericCustomWidgets import *
from PackageManagers import PackageClasses
from PackageManagers.choco import Choco
from PackageManagers.npm import Npm
from PackageManagers.PackageClasses import *
from PackageManagers.pip import Pip
from PackageManagers.scoop import Scoop
from PackageManagers.winget import Winget
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from tools import _
from win32mica import *

PackageManagersList: list[PackageClasses.PackageManagerModule] = [
    Winget,
    Scoop,
    Choco,
    Pip,
    Npm,
]

PackagesLoadedDict: dict[PackageClasses.PackageManagerModule:bool] = {
    Winget: False,
    Scoop: False,
    Choco: False,
    Pip: False,
    Npm: False,
}

StaticPackageManagersList: list[PackageClasses.PackageManagerModule] = [
]

StaticPackagesLoadedDict: dict[PackageClasses.PackageManagerModule:bool] = {
}

DynaimcPackageManagersList: list[PackageClasses.DynamicPackageManager] = [
    Pip,
    Npm,
    Choco,
    Winget,
    Scoop,
]

DynamicPackagesLoadedDict: dict[PackageClasses.PackageManagerModule:bool] = {
    Pip: False,
    Npm: False,
    Winget: False,
    Choco: False,
    Scoop: False
}

class QLinkLabel(QLabel):
    def __init__(self, text: str = "", stylesheet: str = ""):
        super().__init__(text)
        self.setStyleSheet(stylesheet)
        self.setTextFormat(Qt.RichText)
        self.setTextInteractionFlags(Qt.TextBrowserInteraction)
        self.setWordWrap(True)
        self.setOpenExternalLinks(True)
        self.setContextMenuPolicy(Qt.CustomContextMenu)
        self.customContextMenuRequested.connect(self.showmenu)
        self.lineedit = QLineEdit(self)
        self.lineedit.hide()
        self.lineedit.setReadOnly(True)

    def setText(self, text: str) -> None:
        super().setText(text)

    def showmenu(self, pos: QPoint) -> None:
        self.lineedit.setText(self.selectedText())
        self.lineedit.selectAll()
        c = QLineEdit.createStandardContextMenu(self.lineedit)
        selAction = c.actions()[-1]
        selAction.setEnabled(True)
        selAction.triggered.connect(lambda: self.setSelection(0, len(self.text())))
        ApplyMenuBlur(c.winId().__int__(), c)
        c.exec(QCursor.pos())

class CommandLineEdit(CustomLineEdit):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setReadOnly(True)
        self.setClearButtonEnabled(False)
        self.copyButton = QPushButton(self)
        self.copyButton.setIcon(QIcon(getMedia("copy")))
        self.copyButton.setIconSize(QSize(24, 24))
        self.setFixedHeight(50)
        self.copyButton.setFixedSize(42, 42)
        self.copyButton.clicked.connect(lambda: globals.app.clipboard().setText(self.text()))
        if isDark():
            self.setStyleSheet("""
                QLineEdit {
                    border: 1px solid #282828;
                    background-color: #191919;
                    font-family: "Consolas";
                    padding: 15px;
                    border-radius: 8px;
                    padding-right: 50px;
                }
                QPushButton {
                    border-radius: 6px;
                    background-color: rgba(0, 0, 0, 1%);
                    border: 0px;
                }
                QPushButton:hover {
                    background-color: rgba(255, 255, 255, 5%);
                }
                QPushButton:pressed {
                    background-color: rgba(255, 255, 255, 10%);
                }
                """)
        else:
            self.setStyleSheet("""
                QLineEdit {
                    border: 1px solid #f5f5f5;
                    background-color: #ffffff;
                    font-family: "Consolas";
                    padding: 15px;
                    border-radius: 8px;
                    padding-right: 50px;
                }
                QPushButton {
                    border-radius: 6px;
                    background-color: rgba(255, 255, 255, 100%);
                    border: 0px;
                }
                QPushButton:hover {
                    background-color: rgba(240, 240, 240, 100%);
                }
                QPushButton:pressed {
                    background-color: rgba(225, 225, 225, 100%);
                }
                """)

    def contextMenuEvent(self, arg__1: QContextMenuEvent) -> None:
        arg__1.ignore()
        return False

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.copyButton.move(self.width()-46, 4)
        return super().resizeEvent(event)

class CustomMessageBox(QMainWindow):
    showerr = Signal(dict, bool)
    fHeight = 100
    oldpos = QPoint()
    mousePressed = False
    callInMain = Signal(object)
    qanswer = -1
    isQuestion = False
    def __init__(self, parent):
        super().__init__(parent)
        self.showerr.connect(self.em)
        self.callInMain.connect(lambda f: f())
        self.setWindowFlags(Qt.Dialog | Qt.CustomizeWindowHint)
        self.setObjectName("micawin")
        ApplyMica(self.winId().__int__(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.hide()
        if isDark():
            self.setStyleSheet(f"""#micawin {{
                background-color: #222222;
                color: white;
                }}
                #btnBackground {{
                    border-top: 1px solid #1b1b1b;
                    background-color: #181818;
                }}
                               """)
        else:
            self.setStyleSheet(f"""#micawin {{
                background-color: #f6f6f6;
                color: black;
                }}
                #btnBackground {{
                    border-top: 1px solid #d5d5d5;
                    background-color: #e5e5e5;
                }}
                               """)
        l = QVBoxLayout()
        self.titleLabel = QLabel()
        self.titleLabel.setStyleSheet("font-size: 16pt;font-family: \"Segoe UI Variable Text\";font-weight: bold;")
        l.addSpacing(10)
        l.addWidget(self.titleLabel)
        l.addSpacing(2)
        self.textLabel = QLabel()
        self.textLabel.setWordWrap(True)
        l.addWidget(self.textLabel)
        l.addSpacing(10)
        l.addStretch()
        self.iconLabel = QLabel()
        self.iconLabel.setFixedSize(64, 64)
        layout = QVBoxLayout()
        hl = QHBoxLayout()
        hl.setContentsMargins(20, 20, 20, 10)
        hl.addWidget(self.iconLabel)
        hl.addLayout(l)
        hl.addSpacing(16)
        self.bgw1 = QWidget()
        self.bgw1.setLayout(hl)
        layout.addWidget(self.bgw1)
        self.buttonLayout = QHBoxLayout()
        self.okButton = QPushButton(self)
        self.okButton.setFixedHeight(30)

        def returnTrue():
            if self.isQuestion:
                self.qanswer = 1
                self.close()
            globals.tray_is_error = False
            update_tray_icon()

        def returnFalse():
            if self.isQuestion:
                self.close()
                self.qanswer = 0

        self.okButton.clicked.connect(returnTrue)
        self.okButton.clicked.connect(self.delete)
        try:
            self.moreInfoButton = QPushButton(_("Show details"))
        except NameError:
            self.moreInfoButton = QPushButton("Show details")
        self.moreInfoButton.setFixedHeight(30)
        self.moreInfoButton.setObjectName("AccentButton")
        self.moreInfoButton.clicked.connect(self.moreInfo)
        self.moreInfoButton.clicked.connect(returnFalse)
        self.buttonLayout.addSpacing(10)
        self.buttonLayout.addWidget(self.moreInfoButton)
        self.buttonLayout.addWidget(self.okButton)
        self.buttonLayout.addSpacing(10)
        bglayout = QVBoxLayout()
        bglayout.addLayout(self.buttonLayout)
        l = QHBoxLayout()
        self.moreInfoTextArea = CustomPlainTextEdit()
        self.moreInfoTextArea.setReadOnly(True)
        self.moreInfoTextArea.setVisible(False)
        self.moreInfoTextArea.setMinimumHeight(120)
        l.addWidget(self.moreInfoTextArea)
        l.setContentsMargins(10, 0, 10, 0)
        bglayout.addLayout(l, stretch=1)
        bglayout.addSpacing(10)

        self.bgw2 = QWidget()
        self.bgw2.setObjectName("btnBackground")
        self.bgw2.setMinimumHeight(70)
        self.bgw2.setLayout(bglayout)
        layout.addWidget(self.bgw2)
        bglayout.setContentsMargins(20, 20, 20, 20)

        layout.setContentsMargins(0, 0, 0, 0)
        w = QWidget()
        w.setLayout(layout)
        self.setCentralWidget(w)
        w.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.setMinimumWidth(320)

    def delete(self):
        self.hide()

    def moreInfo(self):
        if not self.isQuestion:
            spacingAdded = False
            self.moreInfoTextArea.setVisible(not self.moreInfoTextArea.isVisible())
            self.moreInfoButton.setText(_("Hide details") if self.moreInfoTextArea.isVisible() else _("Show details"))
            if self.moreInfoTextArea.isVisible():
                # show textedit
                s = self.size()
                spacingAdded = True
                self.resize(s)
                self.setMinimumWidth(450)
                self.setMinimumHeight(self.bgw1.sizeHint().height())
                self.setMaximumHeight(-1)
            else:
                # Hide textedit
                s = self.size()
                s.setHeight(s.height() - self.moreInfoTextArea.height() - self.layout().spacing())
                self.setMaximumSize(s)
                self.resize(s)
                self.setMaximumSize(2048, 2048)
                self.setMinimumWidth(450)
                self.setFixedHeight(self.fHeight)
                self.setMinimumHeight(self.fHeight)
                self.setMaximumHeight(self.fHeight+1)

    def paintEvent(self, event: QPaintEvent) -> None:
        if not self.moreInfoTextArea.isVisible():
            self.bgw1.setFixedHeight(self.bgw1.sizeHint().height())
            self.setFixedHeight(self.bgw1.sizeHint().height() + 80)
        else:
            self.setMinimumHeight(self.bgw1.sizeHint().height() + 70 + self.moreInfoTextArea.height() + 10)
        return super().paintEvent(event)

    def showErrorMessage(self, data: dict, showNotification = True):
        self.isQuestion = False
        self.showerr.emit(data, showNotification)

    def em(self, data: dict, showNotification = True):
        self.buttonLayout.setDirection(QBoxLayout.Direction.LeftToRight)
        self.okButton.setObjectName("")
        self.moreInfoButton.setObjectName("")
        errorData = {
            "titlebarTitle": "Window title",
            "mainTitle": "Error message",
            "mainText": "An error occurred",
            "buttonTitle": "Ok",
            "errorDetails": "The details say that there were no details to detail the detailed error",
            "icon": QIcon(getMedia("notif_error")),
            "notifTitle": "Error notification",
            "notifText": "An error occurred",
            "notifIcon": QIcon(getMedia("notif_error")),
        } | data
        self.setWindowTitle(errorData["titlebarTitle"])
        self.titleLabel.setText(errorData["mainTitle"])
        self.textLabel.setText(errorData["mainText"])
        self.okButton.setText(errorData["buttonTitle"])
        self.iconLabel.setPixmap(QIcon(errorData["icon"]).pixmap(64, 64))
        self.moreInfoTextArea.setPlainText(errorData["errorDetails"])
        self.setMinimumWidth(450)
        self.resize(self.minimumSizeHint())
        wVisible = False
        wExists = False
        if self.parent():
            try:
                if self.parent().window():
                    wExists = True
                    if self.parent().window().isVisible():
                        wVisible = True
                        g: QRect = self.parent().window().geometry()
                        self.move(g.x()+g.width()//2-self.width()//2, g.y()+g.height()//2-self.height()//2)
            except AttributeError:
                print("Parent has no window!")
        if showNotification:
            if not wVisible:
                globals.trayIcon.showMessage(errorData["notifTitle"], errorData["notifText"], errorData["notifIcon"])
        if wExists:
            if wVisible:
                self.show()
                globals.app.beep()
            else:
                def waitNShow():
                    while not self.parent().window().isVisible():
                        time.sleep(0.5)
                    self.callInMain.emit(lambda: (self.show(), globals.app.beep()))
                Thread(target=waitNShow, daemon=True, name="Error message waiting to be shown").start()
        else:
            self.show()
            globals.app.beep()

    def askQuestion(self, data: dict):
        self.buttonLayout.setDirection(QBoxLayout.Direction.RightToLeft)
        self.isQuestion = True
        try:
            questionData = {
                "titlebarTitle": "Window title",
                "mainTitle": "Error message",
                "mainText": "An error occurred",
                "acceptButtonTitle": _("Yes"),
                "cancelButtonTitle": _("No"),
                "icon": QIcon(getMedia("question")),
            } | data
        except Exception as e:
            questionData = {
                "titlebarTitle": "Window title",
                "mainTitle": "Error message",
                "mainText": "An error occurred",
                "acceptButtonTitle": _("Yes"),
                "cancelButtonTitle": _("No"),
                "icon": QIcon(getMedia("question")),
            } | data
            report(e)
        self.callInMain.emit(lambda: self.aq(questionData))
        self.qanswer = -1
        while self.qanswer == -1:
            time.sleep(0.05)
        return True if self.qanswer == 1 else False

    def aq(self, questionData: dict):
        self.setWindowTitle(questionData["titlebarTitle"])
        self.titleLabel.setText(questionData["mainTitle"])
        self.textLabel.setText(questionData["mainText"])
        self.okButton.setText(questionData["acceptButtonTitle"])
        self.moreInfoButton.setText(questionData["cancelButtonTitle"])
        if QIcon(questionData["icon"]).isNull():
            self.iconLabel.setFixedWidth(10)
        else:
            self.iconLabel.setPixmap(QIcon(questionData["icon"]).pixmap(64, 64))
        wVisible = False
        wExists = False
        if self.parent():
            try:
                if self.parent().window():
                    wExists = True
                    if self.parent().window().isVisible():
                        wVisible = True
                        g: QRect = self.parent().window().geometry()
                        self.show()
                        self.setMinimumWidth(320)
                        self.resize(self.minimumSizeHint())
                        self.move(g.x()+g.width()//2-self.width()//2, g.y()+g.height()//2-self.height()//2)
            except AttributeError:
                print("Parent has no window!")
        if wExists:
            if wVisible:
                self.show()
                globals.app.beep()
            else:
                globals.mainWindow.showWindow()
                self.show()
                globals.app.beep()
        else:
            self.show()
            globals.app.beep()


    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.mousePressed = True
        self.oldpos = QCursor.pos()-self.window().pos()
        return super().mousePressEvent(event)

    def mouseMoveEvent(self, event: QMouseEvent) -> None:
        if self.mousePressed:
            self.move(QCursor.pos()-self.oldpos)#(self.window().pos()+(QCursor.pos()-self.oldpos))
            self.oldpos = self.oldpos = QCursor.pos()-self.window().pos()
        return super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        self.mousePressed = False
        return super().mouseReleaseEvent(event)

class QAnnouncements(QLabel):
    callInMain = Signal(object)

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
        l = QVBoxLayout()
        l.setSpacing(0)
        l.setContentsMargins(0, self.getPx(5), 0, self.getPx(5))
        l.addWidget(self.area, stretch=1)
        self.area.setWidgetResizable(True)
        self.area.setContentsMargins(0, 0, 0, 0)
        self.area.setObjectName("backgroundWindow")
        self.area.setStyleSheet("border: 0px solid black; padding: 0px; margin: 0px;")
        self.area.setFrameShape(QFrame.NoFrame)
        self.area.setHorizontalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.area.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.pictureLabel.setFixedHeight(self.area.height())
        self.textLabel.setFixedHeight(self.area.height())
        self.setLayout(l)



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
                self.file =  open(os.path.join(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui")), "announcement.png"), "wb")
                self.file.write(response)
                self.callInMain.emit(lambda: self.pictureLabel.setText(""))
                self.file.close()
                h = self.area.height()
                self.callInMain.emit(lambda: self.pictureLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.textLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.pictureLabel.setPixmap(QPixmap(self.file.name).scaledToHeight(h-self.getPx(8), Qt.SmoothTransformation)))
            except Exception as ex:
                s = "Couldn't load the announcement image"+"\n\n"+str(ex)
                self.callInMain.emit(lambda: self.pictureLabel.setText(s))
                print("🟠 Unable to retrieve announcement image")
                print(ex)
        except Exception as e:
            if useHttps:
                self.loadAnnouncements(useHttps=False)
            else:
                s = "Couldn't load the announcements. Please try again later"+"\n\n"+str(e)
                self.callInMain.emit(lambda: self.setTtext(s))
                print("🟠 Unable to retrieve latest announcement")
                print(e)

    def showEvent(self, a0: QShowEvent) -> None:
        return super().showEvent(a0)

    def getPx(self, i: int) -> int:
        return i

    def setTtext(self, a0: str) -> None:
        return super().setText(a0)

    def setText(self, a: str) -> None:
        raise Exception("This member should not be used under any circumstances")

class WelcomeWizardPackageManager(QWidget):
    def __init__(self, text, description, image) -> None:
        super().__init__()
        mainw = QWidget(self)
        mainw.setContentsMargins(0, 0, 0, 0)
        mainw.setObjectName("bgwidget")
        mainw.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, True)
        self.checkbox = QCheckBox(text)
        self.checkbox.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, False)
        self.checkbox.stateChanged.connect(lambda v: (self.image.setEnabled(v)))
        self.checkbox.setSizePolicy(QSizePolicy.Policy.Maximum, QSizePolicy.Policy.Maximum)
        self.description = ClickableLabel(description)
        self.description.clicked.connect(self.checkbox.click)
        self.description.setWordWrap(True)
        self.image = QLabel()
        self.image.setPixmap(QPixmap(image).scaledToHeight(48, Qt.TransformationMode.SmoothTransformation))
        h = QHBoxLayout()
        v = QVBoxLayout()
        v.addWidget(self.checkbox)
        v.addWidget(self.description, stretch=1)
        h.addLayout(v, stretch=1)
        h.addWidget(self.image)
        h.setContentsMargins(12, 8, 16, 8)
        h2 = QHBoxLayout()
        h.addStretch()
        mainw.setLayout(h)
        h2.addStretch()
        h2.addWidget(mainw)
        h2.setContentsMargins(0, 0, 0, 0)
        h2.addStretch()
        mainw.setFixedWidth(600)
        self.setLayout(h2)
        if isDark():
            self.setStyleSheet("""#bgwidget{background-color: rgba(255, 255, 255, 5%); border: 1px solid #101010; padding: 8px; border-radius: 8px;}""")
        else:
            self.setStyleSheet("""#bgwidget{background-color: rgba(255, 255, 255, 50%); border: 1px solid #eeeeee; padding: 8px; border-radius: 8px;}""")

    def setChecked(self, v: bool) -> None:
        self.checkbox.setChecked(v)

    def isChecked(self) -> bool:
        return self.checkbox.isChecked()

class IgnoredUpdatesManager(MovableFramelessWindow):
    def __init__(self, parent: QWidget | None = ...) -> None:
        super().__init__(parent)
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)
        self.setLayout(QVBoxLayout())
        self.setObjectName("background")
        title = QLabel(_("Ignored updates"))
        title.setContentsMargins(10, 0, 0, 0)
        title.setStyleSheet(f"font-size: 20pt; font-family: \"{globals.dispfont}\";font-weight: bold;")
        image = QLabel()
        image.setPixmap(QPixmap(getMedia("pin_color")).scaledToHeight(32, Qt.TransformationMode.SmoothTransformation))
        image.setFixedWidth(32)
        h = QHBoxLayout()
        h.setContentsMargins(10, 0, 0, 0)
        h.addWidget(image)
        h.addWidget(title)
        h.addStretch()
        self.layout().addLayout(h)
        desc = QLabel(_("The packages listed here won't be taken in account when checking for updates. Double-click them or click the button on their right to stop ignoring their updates."))
        desc.setWordWrap(True)
        self.layout().addWidget(desc)
        self.layout().setContentsMargins(5, 5, 5, 5)
        desc.setContentsMargins(10, 0, 0, 0)
        self.setWindowTitle("\x20")
        self.setMinimumSize(QSize(650, 400))
        self.treewidget = TreeWidget(_("No packages found"))
        self.layout().addWidget(self.treewidget)
        hl = QHBoxLayout()
        hl.addStretch()
        resetButton = QPushButton(_("Reset"))
        resetButton.clicked.connect(self.resetAll)
        hl.addWidget(resetButton)
        self.layout().addLayout(hl)
        self.treewidget.setColumnCount(4)
        self.treewidget.header().setStretchLastSection(False)
        self.treewidget.header().setSectionResizeMode(0, QHeaderView.Stretch)
        self.treewidget.header().setSectionResizeMode(1, QHeaderView.Fixed)
        self.treewidget.header().setSectionResizeMode(2, QHeaderView.Fixed)
        self.treewidget.header().setSectionResizeMode(3, QHeaderView.Fixed)
        self.treewidget.setColumnWidth(1, 150)
        self.treewidget.setColumnWidth(2, 150)
        self.treewidget.setColumnWidth(3, 0)
        self.treewidget.setHeaderLabels([_("Package ID"), _("Ignored version"), _("Source"), ""])
        self.treewidget.itemDoubleClicked.connect(lambda: self.treewidget.itemWidget(self.treewidget.currentItem(), 3).click())

        self.installIcon = QIcon(getMedia("install"))
        self.versionIcon = QIcon(getMedia("newversion"))
        self.wingetIcon = Winget.getIcon("Winget")
        self.scoopIcon = Scoop.getIcon("Scoop")
        self.chocolateyIcon = Choco.getIcon("Chocolatey")
        self.pipIcon = Pip.getIcon("Pip")
        self.npmIcon = Npm.getIcon("Npm")
        self.localIcon = QIcon(getMedia("localpc"))
        self.removeIcon = QIcon(getMedia("menu_uninstall"))


    def loadItems(self):
        self.treewidget.clear()
        for id in getSettingsValue("BlacklistedUpdates").split(","):
            if id:
                self.addItem(id, _("All versions"), _("Unknown"), BlacklistMethod.Legacy)
        for id, store in GetIgnoredPackageUpdates_Permanent():
            if id and store:
                self.addItem(id, _("All versions"), store.capitalize(), BlacklistMethod.AllVersions)
        for id, version, store in GetIgnoredPackageUpdates_SpecificVersion():
            if id and store:
                self.addItem(id, version, store.capitalize(), BlacklistMethod.SpecificVersion)

    def addItem(self, id: str, version: str, store: str, blacklistMethod: BlacklistMethod):
        item = TreeWidgetItemWithQAction()
        item.setText(0, id)
        item.setText(1, version)
        item.setText(2, store)
        item.setIcon(0, self.installIcon)
        item.setIcon(1, self.versionIcon)
        if "scoop" in store.lower():
            item.setIcon(2, self.scoopIcon)
        elif "winget" in store.lower():
            item.setIcon(2, self.wingetIcon)
        elif "choco" in store.lower():
            item.setIcon(2, self.chocolateyIcon)
        elif "pip" in store.lower():
            item.setIcon(2, self.pipIcon)
        elif "npm" in store.lower():
            item.setIcon(2, self.npmIcon)
        else:
            item.setIcon(2, self.localIcon)
        self.treewidget.addTopLevelItem(item)
        removeButton = QPushButton()
        removeButton.setIcon(self.removeIcon)
        removeButton.setFixedSize(QSize(24, 24))
        match blacklistMethod:
            case BlacklistMethod.Legacy:
                removeButton.clicked.connect(lambda: self.unBlackistLegacy(id, item))

            case BlacklistMethod.SpecificVersion:
                removeButton.clicked.connect(lambda: self.unBlackistSingleVersion(id, version, store, item))

            case BlacklistMethod.AllVersions:
                removeButton.clicked.connect(lambda: self.unBlackistAllVersions(id, store, item))

        self.treewidget.setItemWidget(item, 3, removeButton)

    def resetAll(self):
        for i in range(self.treewidget.topLevelItemCount()):
            self.treewidget.itemWidget(self.treewidget.topLevelItem(0), 3).click()
        self.close()
        globals.updates.startLoadingPackages(force=True)

    def unBlackistLegacy(self, id: str, item: TreeWidgetItemWithQAction):
        setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates").replace(id, "").replace(",,", ","))
        i = self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(item))
        del i

    def unBlackistAllVersions(self, id: str, store: str, item: TreeWidgetItemWithQAction):
        originalList: list[list[str]] = GetIgnoredPackageUpdates_Permanent()
        setSettingsValue("PermanentlyIgnoredPackageUpdates", "")
        for ignoredPackage in originalList:
            if [id, store.lower()] == ignoredPackage:
                continue
            IgnorePackageUpdates_Permanent(ignoredPackage[0], ignoredPackage[1])
        i = self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(item))
        
        INSTALLED: SoftwareSection = globals.uninstall
        if id in INSTALLED.IdPackageReference:
            package: Package = INSTALLED.IdPackageReference[id]
            package.PackageItem.setIcon(1, INSTALLED.installIcon)
            package.PackageItem.setToolTip(1, package.Name)
        
        del i

    def unBlackistSingleVersion(self, id: str, version: str, store: str, item: TreeWidgetItemWithQAction):
        originalList: list[list[str]] = GetIgnoredPackageUpdates_SpecificVersion()
        setSettingsValue("SingleVersionIgnoredPackageUpdates", "")
        for ignoredPackage in originalList:
            if [id, version, store.lower()] == ignoredPackage:
                continue
            IgnorePackageUpdates_SpecificVersion(ignoredPackage[0], ignoredPackage[1], ignoredPackage[2])
        i = self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(item))
        del i

    def resizeEvent(self, event: QResizeEvent) -> None:
        return super().resizeEvent(event)

    def showEvent(self, event: QShowEvent) -> None:
        r = ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.setStyleSheet("#background{background-color:"+("transparent" if r == 0x0 else ("#202020" if isDark() else "white"))+";}")
        self.loadItems()
        return super().showEvent(event)

class SoftwareSection(QWidget):

    addProgram = Signal(object)
    finishLoading = Signal()
    askForScoopInstall = Signal(str)
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()
    callInMain = Signal(object)
    discoverLabelDefaultWidth: int = 0
    discoverLabelIsSmall: bool = False
    isToolbarSmall: bool = False
    toolbarDefaultWidth: int = 0
    PackageItemReference: dict[Package:TreeWidgetItemWithQAction] = {}
    ItemPackageReference: dict[TreeWidgetItemWithQAction:Package] = {}
    IdPackageReference: dict[str:Package] = {}
    sectionName: str = ""
    packageItems: list[TreeWidgetItemWithQAction] = []
    showableItems: list[TreeWidgetItemWithQAction] = []
    addedItems: list[TreeWidgetItemWithQAction] = []
    shownItems: list[TreeWidgetItemWithQAction] = []
    nextItemToShow: int = 0
    OnThemeChange = Signal()

    PackageManagers: list[PackageManagerModule] = PackageManagersList
    PackagesLoaded: dict[PackageManagerModule:bool] = {}
    for manager in PackageManagers:
        PackagesLoaded[manager] = False

    def __init__(self, parent = None, sectionName: str = "Install"):
        super().__init__(parent = parent)
        self.sectionName = sectionName
        self.infobox = globals.infobox
        self.packageExporter = PackageExporter(self)
        self.setStyleSheet("margin: 0px;")

        self.programbox = QWidget()
        self.callInMain.connect(lambda f: f())

        self.mainLayout = QVBoxLayout()
        self.mainLayout.setContentsMargins(0, 0, 0, 0)
        self.setLayout(self.mainLayout)

        self.reloadButton = QPushButton()
        self.reloadButton.setFixedSize(30, 30)
        self.reloadButton.setStyleSheet("margin-top: 0px;")
        self.reloadButton.clicked.connect(self.startLoadingPackages)
        self.reloadButton.setAccessibleName(_("Reload"))

        self.searchButton = QPushButton()
        self.searchButton.setFixedSize(30, 30)
        self.searchButton.setStyleSheet("margin-top: 0px;")
        self.searchButton.clicked.connect(self.filter)
        self.searchButton.setAccessibleName(_("Search"))

        headerLayout = QHBoxLayout()
        headerLayout.setContentsMargins(0, 0, 0, 0)

        self.forceCheckBox = QCheckBox(_("Instant search"))
        self.forceCheckBox.setFixedHeight(30)
        self.forceCheckBox.setLayoutDirection(Qt.RightToLeft)
        self.forceCheckBox.setStyleSheet("margin-top: 0px;")
        self.forceCheckBox.setChecked(True)
        self.forceCheckBox.setChecked(not getSettings(f"DisableInstantSearchOn{sectionName}"))
        self.forceCheckBox.clicked.connect(lambda v: setSettings(f"DisableInstantSearchOn{sectionName}", bool(not v)))

        self.query = CustomLineEdit()
        self.query.setPlaceholderText(" PlaceholderText")
        self.query.returnPressed.connect(lambda: (self.filter()))
        self.query.editingFinished.connect(lambda: (self.filter()))
        self.query.textChanged.connect(lambda: self.filter() if self.forceCheckBox.isChecked() else print())
        self.query.setFixedHeight(30)
        self.query.setStyleSheet("margin-top: 0px;")
        self.query.setMinimumWidth(100)
        self.query.setMaximumWidth(250)
        self.query.setBaseSize(250, 30)

        sct = QShortcut(QKeySequence("Ctrl+F"), self)
        sct.activated.connect(lambda: (self.query.setFocus(), self.query.setSelection(0, len(self.query.text()))))

        sct = QShortcut(QKeySequence("Ctrl+R"), self)
        sct.activated.connect(self.startLoadingPackages)

        sct = QShortcut(QKeySequence("F5"), self)
        sct.activated.connect(self.startLoadingPackages)

        sct = QShortcut(QKeySequence("Esc"), self)
        sct.activated.connect(self.query.clear)


        self.SectionImage = QLabel()
        self.SectionImage.setFixedWidth(80)
        headerLayout.addWidget(self.SectionImage)

        v = QVBoxLayout()
        v.setSpacing(0)
        v.setContentsMargins(0, 0, 0, 0)
        self.discoverLabel = QLabel("SectionTitle")
        self.discoverLabel.setStyleSheet(f"font-size: 30pt;font-family: \"{globals.dispfont}\";font-weight: bold;")
        v.addWidget(self.discoverLabel)

        self.titleWidget = QWidget()
        self.titleWidget.setContentsMargins(0, 0, 0, 0)
        self.titleWidget.setFixedHeight(70)
        self.titleWidget.setLayout(v)

        headerLayout.addWidget(self.titleWidget, stretch=1)
        headerLayout.addStretch()
        headerLayout.setContentsMargins(5, 0, 5, 0)
        forceCheckBox = QVBoxLayout()
        forceCheckBox.addWidget(self.forceCheckBox)
        headerLayout.addLayout(forceCheckBox)
        headerLayout.addWidget(self.query)
        headerLayout.addWidget(self.searchButton)
        headerLayout.addWidget(self.reloadButton)

        self.packageListScrollBar = CustomScrollBar()
        self.packageListScrollBar.setOrientation(Qt.Vertical)
        self.packageListScrollBar.valueChanged.connect(lambda v: self.addItemsToTreeWidget() if v>=(self.packageListScrollBar.maximum()-20) else None)

        class HeaderView(QHeaderView):
            sortOrder = Qt.SortOrder.DescendingOrder
            def __init__(self, orientation: Qt.Orientation, parent: TreeWidget) -> None:
                super().__init__(orientation, parent)
                self.treewidget = parent
                self.sectionClicked.connect(self.clickNewSection)

            def clickNewSection(self, s: int):
                if s==3:
                    self.sortOrder = Qt.SortOrder.AscendingOrder if self.sortOrder == Qt.SortOrder.DescendingOrder else Qt.SortOrder.DescendingOrder
                    self.treewidget.sortByColumn(6, self.sortOrder)

        self.packageList = TreeWidget("")
        self.packageList.setUniformRowHeights(True)
        self.packageList.setHeader(HeaderView(Qt.Orientation.Horizontal, self.packageList))
        self.packageList.setSortingEnabled(True)
        self.packageList.setUniformRowHeights(True)
        self.packageList.sortByColumn(1, Qt.SortOrder.AscendingOrder)
        self.packageList.setVerticalScrollBar(self.packageListScrollBar)
        self.packageList.connectCustomScrollbar(self.packageListScrollBar)
        self.packageList.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.packageList.setVerticalScrollMode(QTreeWidget.ScrollPerPixel)
        self.packageList.setIconSize(QSize(24, 24))
        self.packageList.header().sectionClicked.connect(lambda: self.finishFiltering(self.query.text()))
        self.packageList.currentItemChanged.connect(lambda: self.addItemsToTreeWidget() if self.packageList.indexOfTopLevelItem(self.packageList.currentItem())+20 > self.packageList.topLevelItemCount() else None)



        def updateItemState(item: TreeWidgetItemWithQAction, column: int):
            if column == 0:
                item.setText(0, " " if item.checkState(0) == Qt.CheckState.Checked else "")
                if item.checkState(0) == Qt.CheckState.Checked:
                    self.packageList.setCurrentItem(item)

        self.packageList.itemChanged.connect(lambda i, c: updateItemState(i, c))

        sct = QShortcut(Qt.Key.Key_Return, self.packageList)
        sct.activated.connect(lambda: self.filter() if self.query.hasFocus() else self.packageList.itemDoubleClicked.emit(self.packageList.currentItem(), 0))

        def toggleItemState():
            item = self.packageList.currentItem()
            checked = item.checkState(0) == Qt.CheckState.Checked
            item.setCheckState(0, Qt.CheckState.Unchecked if checked else Qt.CheckState.Checked)

        sct = QShortcut(QKeySequence(Qt.Key_Space), self.packageList)
        sct.activated.connect(toggleItemState)

        self.packageList.setContextMenuPolicy(Qt.CustomContextMenu)
        self.packageList.customContextMenuRequested.connect(self.showContextMenu)

        header = self.packageList.header()
        header.setSectionResizeMode(QHeaderView.ResizeToContents)
        header.sectionClicked.connect(lambda: self.finishFiltering(self.query.text()))

        self.loadingProgressBar = QProgressBar()
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.loadingProgressBar.setStyleSheet("margin: 0px; margin-left: 2px;margin-right: 2px;")

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

        self.countLabel = QLabel(_("Searching for packages..."))
        self.packageList.label.setText(self.countLabel.text())
        self.countLabel.setObjectName("greyLabel")

        v.addWidget(self.countLabel)
        layout.addLayout(headerLayout)
        self.toolbar = self.getToolbar()
        layout.addWidget(self.toolbar)
        layout.setContentsMargins(0, 0, 0, 0)
        v.addWidget(self.countLabel)

        self.informationBanner = ClosableOpaqueMessage()
        self.informationBanner.image.hide()
        self.informationBanner.hide()

        layout.addWidget(self.loadingProgressBar)
        layout.addWidget(self.informationBanner)
        hl2 = QHBoxLayout()
        hl2.addWidget(self.packageList)
        hl2.addWidget(self.packageListScrollBar)
        hl2.setSpacing(0)
        hl2.setContentsMargins(0, 0, 0, 0)
        layout.addLayout(hl2)
        self.programbox.setLayout(l)
        self.mainLayout.addWidget(self.programbox, stretch=1)
        self.infobox.hide()

        self.addProgram.connect(self.addItem)

        self.finishLoading.connect(self.finishLoadingIfNeeded)
        self.infobox.addProgram.connect(self.addInstallation)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))

        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)

        g = self.packageList.geometry()

        self.leftSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))

        self.rightSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))

        self.leftFast = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))
        self.window().OnThemeChange.connect(self.ApplyIcons)
        
    def ApplyIcons(self):
        self.OnThemeChange.emit()
        self.reloadButton.setIcon(QIcon(getMedia("reload")))
        self.searchButton.setIcon(QIcon(getMedia("search")))
        self.reloadButton.click()

    def finishInitialisation(self):
        print(f"🟢 {self.sectionName} tab loaded successfully")
        toolbarWidgets = [self.toolbar.widgetForAction(action) for action in self.toolbar.actions() if self.toolbar.widgetForAction(action) != None and type(self.toolbar.widgetForAction(action)) != TenPxSpacer]
        taborder = [self.forceCheckBox, self.query, self.searchButton, self.reloadButton] + toolbarWidgets + [self.packageList]
        for i in range(len(taborder)-1):
            self.setTabOrder(taborder[i], taborder[i+1])
        self.leftSlow.start()
        self.startLoadingPackages(force = True)

    def showContextMenu(self, pos: QPoint):
        raise NotImplementedError("This function requires being reimplemented")

    def getToolbar(self) -> QToolBar:
        raise NotImplementedError("This function requires being reimplemented")

    def sharePackage(self, package: TreeWidgetItemWithQAction):
        url = f"https://marticliment.com/wingetui/share?pid={package.text(2)}^&pname={package.text(1)}^&psource={package.text(4)}"
        nativeWindowsShare(package.text(2), url, self.window())

    def finishLoadingIfNeeded(self, store: str) -> None:
        raise NotImplementedError("This function requires being reimplemented")

    def resizeEvent(self, event: QResizeEvent):
        self.adjustWidgetsSize()
        return super().resizeEvent(event)

    def addItem(self, name: str, id: str, version: str, store: str) -> None:
        raise NotImplementedError("This function requires being reimplemented")

    def addItemsToTreeWidget(self, reset: bool = False, itemsToAdd: int = 100):
        self.setUpdatesEnabled(False)
        if reset:
            for itemToHide in self.shownItems:
                itemToHide.setHidden(True, forceShowAction=True)
            self.nextItemToShow = 0
            self.shownItems = []
        addedItems = 0
        while addedItems < itemsToAdd:
            if self.nextItemToShow >= len(self.showableItems):
                break
            itemToAdd = self.showableItems[self.nextItemToShow]
            if itemToAdd not in self.addedItems:
                self.packageList.addTopLevelItem(itemToAdd)
                self.addedItems.append(itemToAdd)
            else:
                itemToAdd.setHidden(False)
            self.shownItems.append(itemToAdd)
            addedItems += 1
            self.nextItemToShow += 1
        self.setUpdatesEnabled(True)

    def filter(self) -> None:
        print(f"🟢 Searching for string \"{self.query.text()}\"")
        Thread(target=lambda: (time.sleep(0.1), self.callInMain.emit(partial(self.finishFiltering, self.query.text())))).start()

    def containsQuery(self, item: TreeWidgetItemWithQAction, querytext: str) -> bool:
        return querytext in item.text(1).lower().replace("-", "").replace(" ", "") or querytext in item.text(2).lower().replace("-", "").replace(" ", "")

    def finishFiltering(self, text: str):
        def getChecked(item: TreeWidgetItemWithQAction) -> str:
            return " " if item.checkState(0) == Qt.CheckState.Checked else ""
        def getTitle(item: TreeWidgetItemWithQAction) -> str:
            return item.text(1)
        def getID(item: TreeWidgetItemWithQAction) -> str:
            return item.text(2)
        def getVersion(item: TreeWidgetItemWithQAction) -> str:
            return item.text(6)
        def getSource(item: TreeWidgetItemWithQAction) -> str:
            return item.text(4)

        if self.query.text() != text:
            return
        self.showableItems = []

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
                self.packageItems.sort(key=getSource, reverse=descendingSort)

        for item in self.packageItems:
            if text == "":
                self.showableItems = self.packageItems.copy()
            else:
                try:
                    if self.containsQuery(item, text.replace("-", "").replace(" ", "").lower()):
                        self.showableItems.append(item)
                except RuntimeError:
                    print("🟠 RuntimeError on SoftwareSection.finishFiltering")
        found = len(self.showableItems)
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

    def showQuery(self) -> None:
        self.programbox.show()
        self.infobox.hide()

    def openInfo(self, item: TreeWidgetItemWithQAction, update: bool = False, uninstall: bool = False, installedVersion: str = "") -> None:
        self.infobox.showPackageDetails(self.ItemPackageReference[item], update, uninstall, installedVersion)
        self.infobox.show()

    def loadPackages(self, manager) -> None:
        raise NotImplementedError("This function requires being reimplemented")

    def exportSelectedPackages(self, all: bool = False) -> None:
        """
        Export all selected packages into a file.
        """
        packagesToExport: list[Package] = []
        for item in self.packageItems:
            if item.checkState(0) == Qt.CheckState.Checked or all:
                packagesToExport.append(self.ItemPackageReference[item])
        self.packageExporter.showExportUI(packagesToExport)

    def setAllPackagesSelected(self, checked: bool) -> None:
        self.packageList.setSortingEnabled(False)
        for item in self.packageItems:
            item.setCheckState(0, Qt.CheckState.Checked if checked else Qt.CheckState.Unchecked)
        self.packageList.setSortingEnabled(True)

    def startLoadingPackages(self, force: bool = False) -> None:
        for manager in self.PackageManagers: # Stop here if not all package managers loaded
            if not self.PackagesLoaded[manager] and not force:
                return
        for manager in self.PackageManagers:
            self.PackagesLoaded[manager] = False
        self.packageItems = []
        self.PackageItemReference = {}
        self.ItemPackageReference = {}
        self.IdPackageReference = {}
        self.shownItems = []
        self.addedItems = []
        self.loadingProgressBar.show()
        self.reloadButton.setEnabled(False)
        self.searchButton.setEnabled(False)
        self.query.setEnabled(False)
        self.packageList.clear()
        self.query.setText("")
        self.packageList.label.setText(self.countLabel.text())

        for manager in self.PackageManagers:
            if manager.isEnabled():
                Thread(target=self.loadPackages, args=(manager,), daemon=True, name=f"{manager.NAME} available packages loader").start()
            else:
                self.PackagesLoaded[manager] = True

        self.finishLoadingIfNeeded()

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

class ImageViewer(QWidget):
    callInMain = Signal(object)
    def __init__(self, parent=None):
        super().__init__(parent=parent)
        self.callInMain.connect(lambda f: f())
        layout = QHBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        self.images = {}

        try:
            self.sct = QShortcut(Qt.Key.Key_Escape, self)
            self.sct.activated.connect(lambda: self.close())
        except TypeError:
            pass
        self.setStyleSheet(f"""
        QGroupBox {{
            border: 0px;
        }}
        #backgroundWidget{{
            border-radius: 5px;
            padding: 5px;
            background-color: {'rgba(30, 30, 30, 50%)' if isDark() else 'rgba(255, 255, 255, 75%)'};
            border-radius: 16px;
            border: 1px solid #88888888;
        }}
        QPushButton {{
            background-color: {'rgba(20, 20, 20, 80%)' if isDark() else 'rgba(255, 255, 255, 80%)'};
        }}
        """)

        self.stackedWidget = QStackedWidget()
        self.stackedWidget.setObjectName("backgroundWidget")

        layout.addWidget(self.stackedWidget)
        self.setLayout(layout)

        self.closeButton = QPushButton(QIcon(getMedia("close")), "", self)
        self.closeButton.move(self.width()-40, 0)
        self.closeButton.resize(40, 40)
        self.closeButton.setFlat(True)
        self.closeButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:#c42b1c;}")
        self.closeButton.clicked.connect(lambda: (self.close()))
        self.closeButton.show()


        self.backButton = QPushButton(QIcon(getMedia("left")), "", self)
        try:
            self.bk = QShortcut(QKeySequence(Qt.Key.Key_Left), parent=self)
            self.bk.activated.connect(lambda: self.backButton.click())
        except TypeError:
            pass
        self.backButton.move(0, self.height()//2-24)
        self.backButton.resize(48, 48)
        self.backButton.setFlat(False)
        #self.backButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:#c42b1c;}")
        self.backButton.clicked.connect(lambda: (self.stackedWidget.setCurrentIndex(self.stackedWidget.currentIndex()-1 if self.stackedWidget.currentIndex()>0 else self.stackedWidget.count()-1)))
        self.backButton.show()

        self.nextButton = QPushButton(QIcon(getMedia("right")), "", self)
        try:
            self.nxt = QShortcut(Qt.Key.Key_Right, self)
            self.nxt.activated.connect(lambda: self.nextButton.click())
        except TypeError:
            pass
        self.nextButton.move(self.width()-48, self.height()//2-24)
        self.nextButton.resize(48, 48)
        self.nextButton.setFlat(False)
        #self.nextButton.setStyleSheet("QPushButton{border: none;border-radius:0px;background:transparent;border-top-right-radius: 16px;}QPushButton:hover{background-color:#c42b1c;}")
        self.nextButton.clicked.connect(lambda: (self.stackedWidget.setCurrentIndex(self.stackedWidget.currentIndex()+1 if self.stackedWidget.currentIndex()<(self.stackedWidget.count()-1) else 0)))
        self.nextButton.show()
        self.hide()


    def resizeEvent(self, event = None):
        self.closeButton.move(self.width()-40, 0)
        self.backButton.move(10, self.height()//2-24)
        self.nextButton.move(self.width()-58, self.height()//2-24)
        for i in range(self.stackedWidget.count()):
            l: QLabel = self.stackedWidget.widget(i)
            l.resize(self.stackedWidget.size())
            pixmap: QPixmap = self.images[l]
            l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        if(event):
            return super().resizeEvent(event)

    def show(self, index: int = 0) -> None:
        g = QRect(0, 0, self.window().geometry().width(), self.window().geometry().height())
        self.resize(g.width()-100, g.height()-100)
        self.move(50, 50)
        self.raise_()
        self.stackedWidget.setCurrentIndex(index)
        for i in range(self.stackedWidget.count()):
            l: QLabel = self.stackedWidget.widget(i)
            l.resize(self.stackedWidget.size())
            pixmap: QPixmap = self.images[l]
            l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        return super().show()

    def close(self) -> bool:
        return super().close()

    def hide(self) -> None:
        return super().hide()

    def resetImages(self) -> None:
        del self.images
        self.images = {}
        for i in range(self.stackedWidget.count()):
            widget = self.stackedWidget.widget(0)
            self.stackedWidget.removeWidget(widget)
            widget.close()
            widget.deleteLater()
            del widget

    def addImage(self, pixmap: QPixmap) -> None:
        l = QLabel()
        l.setAlignment(Qt.AlignmentFlag.AlignCenter | Qt.AlignmentFlag.AlignVCenter)
        self.stackedWidget.addWidget(l)
        l.resize(self.stackedWidget.size())
        l.setPixmap(pixmap.scaled(l.size(), Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        self.images[l] = pixmap

    def wheelEvent(self, event: QWheelEvent) -> None:
        if abs(event.angleDelta().x()) <= 30:
            if event.angleDelta().y() < -30:
                self.backButton.click()
            elif event.angleDelta().y() > 30:
                self.nextButton.click()
        else:
            if event.angleDelta().x() < -30:
                self.backButton.click()
            elif event.angleDelta().x() > 30:
                self.nextButton.click()
        return super().wheelEvent(event)

class PackageExporter(MovableFramelessWindow):
    def __init__(self, parent: QWidget | None = ...) -> None:
        super().__init__(parent)
        self.setLayout(QVBoxLayout())
        self.setObjectName("background")
        title = QLabel(_("Export packages"))
        title.setContentsMargins(10, 0, 0, 0)
        title.setStyleSheet(f"font-size: 20pt; font-family: \"{globals.dispfont}\";font-weight: bold;")
        image = QLabel()
        image.setPixmap(QPixmap(getMedia("save")).scaledToHeight(32, Qt.TransformationMode.SmoothTransformation))
        image.setFixedWidth(32)
        h = QHBoxLayout()
        h.setContentsMargins(10, 0, 0, 0)
        h.addWidget(image)
        h.addWidget(title)
        h.addStretch()
        self.layout().addLayout(h)
        desc = QLabel(_("The following packages are going to be exported to a JSON file. No user data or binaries are going to be saved.")+"\n"+_("Please note that packages from certain sources may be not exportable. They have been greyed out and won't be exported."))
        desc.setWordWrap(True)
        self.layout().addWidget(desc)
        desc.setContentsMargins(10, 0, 0, 0)
        self.layout().setContentsMargins(5, 5, 5, 5)
        self.setWindowTitle("\x20")
        self.setMinimumSize(QSize(650, 400))
        self.treewidget = TreeWidget(_("No packages selected"))
        self.layout().addWidget(self.treewidget)
        self.treewidget.setColumnCount(4)
        self.treewidget.header().setStretchLastSection(False)
        self.treewidget.header().setSectionResizeMode(0, QHeaderView.Stretch)
        self.treewidget.header().setSectionResizeMode(1, QHeaderView.Stretch)
        self.treewidget.header().setSectionResizeMode(2, QHeaderView.Fixed)
        self.treewidget.header().setSectionResizeMode(3, QHeaderView.Fixed)
        self.treewidget.setColumnWidth(2, 150)
        self.treewidget.setColumnWidth(3, 0)
        self.treewidget.setHeaderLabels([_("Package Name"), _("Package ID"), _("Source"), ""])

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 5, 5)
        hLayout.addStretch()
        cancelButton = QPushButton(_("Cancel"))
        cancelButton.setFixedHeight(30)
        cancelButton.clicked.connect(self.close)
        hLayout.addWidget(cancelButton)
        exportButton = QPushButton(_("Export"))
        exportButton.clicked.connect(self.exportPackages)
        exportButton.setFixedHeight(30)
        exportButton.setObjectName("AccentButton")
        hLayout.addWidget(exportButton)
        self.layout().addLayout(hLayout)

        self.installIcon = QIcon(getMedia("install"))
        self.idIcon = QIcon(getMedia("ID"))
        self.removeIcon = QIcon(getMedia("menu_uninstall"))

    def showExportUI(self, packageList: list[Package]):
        """
        Starts the process of exporting selected packages into a file.
        Receives a list composed of Package objects as the unique parameter
        """
        self.treewidget.clear()
        for package in packageList:
            item = TreeWidgetItemWithQAction()
            item.setText(0, package.Name)
            item.setText(1, package.Id)
            item.setText(2, package.Source)
            item.setIcon(0, self.installIcon)
            item.setIcon(1, self.idIcon)
            item.setIcon(2, package.getSourceIcon())
            if package.getSourceIcon() != Winget.wingetIcon and package.PackageManager == Winget:
                # If the package is not available from winget servers, being the case that the package manager is winget:
                item.setDisabled(True)
            removeButton = QPushButton()
            removeButton.setIcon(self.removeIcon)
            removeButton.setFixedSize(QSize(24, 24))
            removeButton.clicked.connect(lambda: self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(self.treewidget.currentItem())))
            self.treewidget.addTopLevelItem(item)
            self.treewidget.setItemWidget(item, 3, removeButton)
        self.treewidget.label.setVisible(self.treewidget.topLevelItemCount() == 0)
        self.show()

    def exportPackages(self) -> None:
        wingetPackagesList = []
        scoopPackageList = []
        chocoPackageList = []
        npmPackageList = []
        pipPackageList = []
        try:
            for i in range(self.treewidget.topLevelItemCount()):
                item = self.treewidget.topLevelItem(i)
                if "winget" in item.text(2).lower():
                    id = item.text(1).strip()
                    wingetPackage = {"PackageIdentifier": id}
                    wingetPackagesList.append(wingetPackage)
                elif "scoop" in item.text(2).lower():
                    scoopPackage = {"Name": item.text(1)}
                    scoopPackageList.append(scoopPackage)
                elif "chocolatey" in item.text(2).lower():
                    chocoPackage = {"Name": item.text(1)}
                    chocoPackageList.append(chocoPackage)
                elif "npm" in item.text(2).lower():
                    npmPackage = {"Name": item.text(1)}
                    npmPackageList.append(npmPackage)
                elif "pip" in item.text(2).lower():
                    pipPackage = {"Name": item.text(1)}
                    pipPackageList.append(pipPackage)
            wingetDetails = {
                "Argument": "https://cdn.winget.microsoft.com/cache",
                "Identifier" : "Microsoft.Winget.Source_8wekyb3d8bbwe",
                "Name": "winget",
                "Type" : "Microsoft.PreIndexed.Package"
            }
            wingetVersion = "1.4"
            try:
                wingetVersion = globals.componentStatus["WingetVersion"] if globals.componentStatus["WingetVersion"] else wingetVersion
            except Exception as e:
                report(e)
            wingetExportSchema = {
                "$schema" : "https://aka.ms/winget-packages.schema.2.0.json",
                "CreationDate" : str(datetime.now()),
                "Sources": [{
                    "Packages": wingetPackagesList,
                    "SourceDetails": wingetDetails}],
                "WinGetVersion" : wingetVersion
            }
            scoopExportSchema = {
                "apps": scoopPackageList,
            }
            chocoExportSchema = {
                "apps": chocoPackageList,
            }
            pipExportSchema = {
                "apps": pipPackageList,
            }
            npmExportSchema = {
                "apps": npmPackageList,
            }
            overallSchema = {
                "winget": wingetExportSchema,
                "scoop": scoopExportSchema,
                "chocolatey": chocoExportSchema,
                "pip": pipExportSchema,
                "npm": npmExportSchema
            }
            filename = QFileDialog.getSaveFileName(None, _("Save File"), _("Packages"), filter='JSON (*.json)')
            if filename[0] != "" and filename[1]:
                print(f"🔵 Saving JSON to {filename[0]}")
                with open(filename[0], 'w') as f:
                    f.write(json.dumps(overallSchema, indent=4))
                subprocess.run(['explorer.exe', '/select,', os.path.normpath(filename[0])], shell=True)
                self.hide()

        except Exception as e:
            report(e)

    def resizeEvent(self, event: QResizeEvent) -> None:
        return super().resizeEvent(event)

    def showEvent(self, event: QShowEvent) -> None:
        r = ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.setStyleSheet("#background{background-color:"+("transparent" if r == 0x0 else ("#202020" if isDark() else "white"))+";}")
        return super().showEvent(event)

class PackageImporter(MovableFramelessWindow):

    pendingPackages: dict[str:TreeWidgetItemWithQAction] = {}
    setLoadBarValue = Signal(str)
    startAnim = Signal(QVariantAnimation)
    changeBarOrientation = Signal()


    def __init__(self, parent: QWidget | None = ...) -> None:
        super().__init__(parent)
        self.setLayout(QVBoxLayout())
        self.setObjectName("background")
        title = QLabel(_("Import packages"))
        title.setContentsMargins(10, 0, 0, 0)
        title.setStyleSheet(f"font-size: 20pt; font-family: \"{globals.dispfont}\";font-weight: bold;")
        image = QLabel()
        image.setPixmap(QPixmap(getMedia("save")).scaledToHeight(32, Qt.TransformationMode.SmoothTransformation))
        image.setFixedWidth(32)
        h = QHBoxLayout()
        h.setContentsMargins(10, 0, 0, 0)
        h.addWidget(image)
        h.addWidget(title)
        h.addStretch()
        self.layout().addLayout(h)
        desc = QLabel(_("The following packages are going to be installed on your system.")+"\n"+_("Please note that certain packages might not be installable, due to the package managers that are enabled on this machine."))
        desc.setWordWrap(True)
        self.layout().addWidget(desc)
        desc.setContentsMargins(10, 0, 0, 0)
        self.layout().setContentsMargins(5, 5, 5, 5)
        self.setWindowTitle("\x20")
        self.setMinimumSize(QSize(650, 400))
        self.treewidget = TreeWidget(_("No packages found"))

        self.loadingProgressBar = QProgressBar(self)
        self.loadingProgressBar.setRange(0, 1000)
        self.loadingProgressBar.setValue(0)
        self.loadingProgressBar.setFixedHeight(4)
        self.loadingProgressBar.setTextVisible(False)
        self.setLoadBarValue.connect(self.loadingProgressBar.setValue)
        self.startAnim.connect(lambda anim: anim.start())
        self.changeBarOrientation.connect(lambda: self.loadingProgressBar.setInvertedAppearance(not(self.loadingProgressBar.invertedAppearance())))

        self.leftSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.leftSlow.setStartValue(0)
        self.leftSlow.setEndValue(1000)
        self.leftSlow.setDuration(700)
        self.leftSlow.finished.connect(lambda: (self.rightSlow.start(), self.changeBarOrientation.emit()))

        self.rightSlow = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.rightSlow.setStartValue(1000)
        self.rightSlow.setEndValue(0)
        self.rightSlow.setDuration(700)
        self.rightSlow.finished.connect(lambda: (self.leftFast.start(), self.changeBarOrientation.emit()))

        self.leftFast = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.leftFast.setStartValue(0)
        self.leftFast.setEndValue(1000)
        self.leftFast.setDuration(300)
        self.leftFast.finished.connect(lambda: (self.rightFast.start(), self.changeBarOrientation.emit()))

        self.rightFast = QPropertyAnimation(self.loadingProgressBar, b"value")
        self.rightFast.setStartValue(1000)
        self.rightFast.setEndValue(0)
        self.rightFast.setDuration(300)
        self.rightFast.finished.connect(lambda: (self.leftSlow.start(), self.changeBarOrientation.emit()))

        self.leftSlow.start()

        self.layout().addWidget(self.loadingProgressBar)
        self.layout().addWidget(self.treewidget)
        self.treewidget.setColumnCount(4)
        self.treewidget.header().setStretchLastSection(False)
        self.treewidget.header().setSectionResizeMode(0, QHeaderView.Stretch)
        self.treewidget.header().setSectionResizeMode(1, QHeaderView.Stretch)
        self.treewidget.header().setSectionResizeMode(2, QHeaderView.Fixed)
        self.treewidget.header().setSectionResizeMode(3, QHeaderView.Fixed)
        self.treewidget.header().setSectionResizeMode(4, QHeaderView.Fixed)
        self.treewidget.setColumnWidth(2, 150)
        self.treewidget.setColumnWidth(3, 100)
        self.treewidget.setColumnWidth(4, 0)
        self.treewidget.setHeaderLabels([_("Package Name"), _("Package ID"), _("Version"), _("Source"), ""])

        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 5, 5)
        hLayout.addStretch()
        cancelButton = QPushButton(_("Cancel"))
        cancelButton.setFixedHeight(30)
        cancelButton.clicked.connect(self.close)
        hLayout.addWidget(cancelButton)
        installButton = QPushButton(_("Install"))
        installButton.clicked.connect(self.installPackages)
        installButton.setFixedHeight(30)
        installButton.setObjectName("AccentButton")
        hLayout.addWidget(installButton)
        self.layout().addLayout(hLayout)

        self.installIcon = QIcon(getMedia("install"))
        self.idIcon = QIcon(getMedia("ID"))
        self.removeIcon = QIcon(getMedia("menu_uninstall"))
        self.versionIcon = QIcon(getMedia("version"))

        self.showImportUI()

    def showImportUI(self):
        """
        Starts the process of installinf selected packages from a file.
        """
        try:
            self.loadingProgressBar.show()
            self.pendingPackages = {}
            DISCOVER_SECTION: SoftwareSection = globals.discover
            self.treewidget.clear()
            packageList: list[str] = []
            self.show()
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
                for manager in ["chocolatey", "scoop", "pip", "npm"]:
                    try:
                        packages = contents[manager]["apps"]
                        for pkg in packages:
                            packageList.append(pkg["Name"])
                    except KeyError as e:
                        print(f"🟠 Invalid {manager} section")
                for packageId in packageList:
                    item = TreeWidgetItemWithQAction()
                    unknownIcon = QIcon(getMedia("question"))
                    self.treewidget.addTopLevelItem(item)
                    if packageId in DISCOVER_SECTION.IdPackageReference:
                        package = DISCOVER_SECTION.IdPackageReference[packageId]
                        self.addItemFromPackage(package, item)
                    else:
                        # If the package is not available from any package manager:
                        item.setText(0, packageId)
                        item.setText(1, packageId)
                        item.setText(2, _("Unknown"))
                        item.setText(3, _("Unknown"))
                        item.setIcon(0, self.installIcon)
                        item.setIcon(1, self.idIcon)
                        item.setIcon(2, unknownIcon)
                        item.setIcon(3, unknownIcon)
                        item.setDisabled(True)
                        self.pendingPackages[packageId] = item
                self.treewidget.label.setVisible(self.treewidget.topLevelItemCount() == 0)
                Thread(target=self.loadDynamicPackages).start()
                self.loadingProgressBar.hide()
            else:
                self.close()
                self.loadingProgressBar.hide()
        except Exception as e:
            report(e)

    def addItemFromPackage(self, package: Package, item: TreeWidgetItemWithQAction) -> None:
        item.setText(0, package.Name)
        item.setText(1, package.Id)
        item.setText(2, package.Version)
        item.setText(3, package.Source)
        item.setIcon(0, self.installIcon)
        item.setIcon(1, self.idIcon)
        item.setIcon(2, self.versionIcon)
        item.setDisabled(False)
        item.setIcon(3, package.getSourceIcon())
        removeButton = QPushButton()
        removeButton.setIcon(self.removeIcon)
        removeButton.setFixedSize(QSize(24, 24))
        removeButton.clicked.connect(lambda: self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(self.treewidget.currentItem())))
        self.treewidget.setItemWidget(item, 4, removeButton)

    def loadDynamicPackages(self):
        DISCOVER_SECTION: SoftwareSection = globals.discover
        DISCOVER_SECTION.callInMain.emit(lambda: DISCOVER_SECTION.packageList.setEnabled(False))
        DISCOVER_SECTION.callInMain.emit(lambda: self.loadingProgressBar.show())
        for packageId in self.pendingPackages.keys():
            print(packageId)
            DISCOVER_SECTION.callInMain.emit(lambda: DISCOVER_SECTION.query.setText(packageId))
            DISCOVER_SECTION.callInMain.emit(lambda: DISCOVER_SECTION.finishFiltering(DISCOVER_SECTION.query.text()))
            while not DISCOVER_SECTION.isLoadingDynamic: # Make time before packages actually start loading
                time.sleep(0.001)
            while DISCOVER_SECTION.isLoadingDynamic:
                time.sleep(0.01)
            if packageId in DISCOVER_SECTION.IdPackageReference:
                package = DISCOVER_SECTION.IdPackageReference[packageId]
                DISCOVER_SECTION.callInMain.emit(lambda: self.addItemFromPackage(package, self.pendingPackages[packageId]))
                DISCOVER_SECTION.callInMain.emit(lambda: self.treewidget.label.setVisible(self.treewidget.topLevelItemCount() == 0))
        DISCOVER_SECTION.callInMain.emit(lambda: self.loadingProgressBar.hide())
        DISCOVER_SECTION.callInMain.emit(lambda: DISCOVER_SECTION.packageList.setEnabled(True))

    def installPackages(self) -> None:
        DISCOVER_SECTION: SoftwareSection = globals.discover
        for i in range(self.treewidget.topLevelItemCount()):
            if not self.treewidget.topLevelItem(i).isDisabled():
                try:
                    id = self.treewidget.topLevelItem(i).text(1)
                    item = DISCOVER_SECTION.PackageItemReference[DISCOVER_SECTION.IdPackageReference[id]]
                    DISCOVER_SECTION.installPackageItem(item)
                except Exception as e:
                    report(e)
        self.close()

    def resizeEvent(self, event: QResizeEvent) -> None:
        return super().resizeEvent(event)

    def showEvent(self, event: QShowEvent) -> None:
        r = ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.setStyleSheet("#background{background-color:"+("transparent" if r == 0x0 else ("#202020" if isDark() else "white"))+";}")
        return super().showEvent(event)

class FlowLayout(QLayout):
    # partially Taken from https://github.com/ByteDream/PyQt5-expansion/blob/main/QCustomObjects.py
    def __init__(self, parent=None, margin=0, spacing=-1):
        super().__init__(parent)

        if parent is not None:
            self.setContentsMargins(margin, margin, margin, margin)

        self.setSpacing(spacing)

        self._items = []
        self.__pending_positions = {}

    def __del__(self):
        item = self.takeAt(0)
        while item:
            item = self.takeAt(0)

    def addItem(self, a0: QLayoutItem) -> None:
        try:
            position = self.__pending_positions[a0.widget()]
            self._items.insert(position, a0)
            del self.__pending_positions[a0.widget()]
        except KeyError:
            self._items.append(a0)

    def addWidget(self, w: QWidget, position: int = None) -> None:
        if position:
            self.__pending_positions[w] = position
        super().addWidget(w)

    def count(self):
        return len(self._items)

    def expandingDirections(self):
        return Qt.Orientations(Qt.Orientation(0))

    def itemAt(self, index: int) -> QLayoutItem:
        if 0 <= index < len(self._items):
            return self._items[index]

        return None

    def hasHeightForWidth(self):
        return True

    def heightForWidth(self, width):
        height = self._doLayout(QRect(0, 0, width, 0), True)
        return height

    def minimumSize(self):
        size = QSize()

        for item in self._items:
            size = size.expandedTo(item.minimumSize())

        margin, _, _, _ = self.getContentsMargins()

        size += QSize(2 * margin, 2 * margin)
        return size

    def removeItem(self, a0: QLayoutItem) -> None:
        a0.widget().deleteLater()

    def removeWidget(self, w: QWidget) -> None:
        w.deleteLater()

    def setGeometry(self, rect):
        super().setGeometry(rect)
        self._doLayout(rect, False)

    def sizeHint(self):
        return self.minimumSize()

    def takeAt(self, index: int) -> QLayoutItem:
        if 0 <= index < len(self._items):
            return self._items.pop(index)

        return None

    def _doLayout(self, rect, testOnly):
        """This does the layout. Dont ask me how. Source: https://github.com/baoboa/pyqt5/blob/master/examples/layouts/flowlayout.py"""
        x = rect.x()
        y = rect.y()
        line_height = 0

        for item in self._items:
            wid = item.widget()
            space_x = self.spacing() + wid.style().layoutSpacing(
                QSizePolicy.PushButton,
                QSizePolicy.PushButton,
                Qt.Horizontal)
            space_y = self.spacing() + wid.style().layoutSpacing(
                QSizePolicy.PushButton,
                QSizePolicy.PushButton,
                Qt.Vertical)
            next_x = x + item.sizeHint().width() + space_x
            if next_x - space_x > rect.right() and line_height > 0:
                x = rect.x()
                y = y + line_height + space_y
                next_x = x + item.sizeHint().width() + space_x
                line_height = 0

            if not testOnly:
                item.setGeometry(QRect(QPoint(x, y), item.sizeHint()))

            x = next_x
            line_height = max(line_height, item.sizeHint().height())

        return y + line_height - rect.y()

    def clear(self):
        items = self._items.copy()
        for item in items:
            item: QLayoutItem
            self.removeWidget(item.widget())
            self.removeItem(item)


if __name__ == "__main__":
    import __init__
