from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from win32mica import *
from tools import *
from tools import _
from genericCustomWidgets import *

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
        ApplyMica(self.winId().__int__(), MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
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
                background-color: #ffffff;
                color: black;
                }}
                #btnBackground {{
                    border-top: 1px solid #dddddd;
                    background-color: #eeeeee;
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
                self.setMaximumHeight(2048)
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
        self.bgw1.setFixedHeight(self.bgw1.sizeHint().height())
        self.setFixedHeight(self.bgw1.sizeHint().height() + 70 + ((10+self.moreInfoTextArea.height()) if self.moreInfoTextArea.isVisible() else 0))
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
        self.checkbox = SectionCheckBox(text, mainw, margin=0, bigfont=True)
        self.checkbox.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, False)
        self.checkbox.stateChanged.connect(lambda v: (self.description.setEnabled(v), self.image.setEnabled(v)))
        self.checkbox.setFixedHeight(30)
        self.description = QLabel(description)
        self.description.setWordWrap(True)
        self.description.setEnabled(False)
        self.image = QLabel()
        self.image.setPixmap(QPixmap(image).scaledToHeight(64, Qt.TransformationMode.SmoothTransformation))
        h = QHBoxLayout()
        v = QVBoxLayout()
        v.addWidget(self.checkbox)
        v.addWidget(self.description, stretch=1)
        h.addLayout(v, stretch=1)
        h.addWidget(self.image)
        h.setContentsMargins(16, 16, 16, 16)
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
            self.setStyleSheet("""#bgwidget{background-color: rgba(255, 255, 255, 5%); border: 1px solid #101010; padding: 16px; border-radius: 16px;}""")
        else:
            self.setStyleSheet("""#bgwidget{background-color: rgba(255, 255, 255, 50%); border: 1px solid #eeeeee; padding: 16px; border-radius: 16px;}""")
        
    def setChecked(self, v: bool) -> None:
        self.checkbox.setChecked(v)
        
    def isChecked(self) -> bool:
        return self.checkbox.isChecked()

class IgnoredUpdatesManager(QWidget):
    def __init__(self, parent: QWidget | None = ...) -> None:
        super().__init__(parent)
        self.setLayout(QVBoxLayout())
        title = QLabel(_("Ignored updates"))
        title.setContentsMargins(10, 0, 0, 0)
        title.setStyleSheet(f"font-size: 20pt; font-family: \"{globals.dispfont}\";font-weight: bold;")
        self.layout().addWidget(title)
        desc = QLabel(_("The packages listed here won't be taken in account when checking for updates. Double-click them or click the button on their right to stop ignoring their updates."))
        desc.setWordWrap(True)
        self.layout().addWidget(desc)
        desc.setContentsMargins(10, 0, 0, 0)
        self.setWindowTitle("\x20")
        self.setWindowFlag(Qt.WindowType.Window, True)
        self.setWindowFlag(Qt.WindowType.WindowMaximizeButtonHint, False)
        self.setWindowFlag(Qt.WindowType.WindowMinimizeButtonHint, False)
        self.setMinimumSize(QSize(650, 400))
        self.treewidget = TreeWidget(_("No packages found"))
        self.layout().addWidget(self.treewidget)
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
        self.wingetIcon = QIcon(getMedia("winget"))
        self.scoopIcon = QIcon(getMedia("scoop"))
        self.chocolateyIcon = QIcon(getMedia("choco"))
        self.localIcon = QIcon(getMedia("localpc"))
        self.removeIcon = QIcon(getMedia("menu_uninstall"))

        
    def loadItems(self):
        for id in getSettingsValue("BlacklistedUpdates").split(","):
            if id:
                self.addItem(id, _("All versions"), _("Unknown"), BlacklistMethod.Legacy)
        
    def addItem(self, id: str, version: str, store: str, blacklistMethod: BlacklistMethod):
        item = QTreeWidgetItem()
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
                removeButton.clicked.connect(lambda: print(id, version, store, item))
                
            case BlacklistMethod.AllVersions:
                removeButton.clicked.connect(lambda: print(id, store, item))
                
        self.treewidget.setItemWidget(item, 3, removeButton)
        
    def unBlackistLegacy(self, id: str, item: QTreeWidgetItem):
        setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates").replace(id, "").replace(",,", ","))
        i = self.treewidget.takeTopLevelItem(self.treewidget.indexOfTopLevelItem(item))
        del i
        
    def showEvent(self, event: QShowEvent) -> None:
        ApplyMica(self.winId(), ColorMode=MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
        self.loadItems()
        return super().showEvent(event)
    
if __name__ == "__main__":
    import __init__