from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from win32mica import *
from tools import *
from tools import _


class MessageBox(QMessageBox):
    def __init__(self, parent: object = None) -> None:
        super().__init__(parent)
        ApplyMica(self.winId(), MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
        self.setStyleSheet("QMessageBox{background-color: transparent;}")
        
class SmoothScrollArea(QScrollArea):
    missingScroll = 0
    def __init__(self, parent = None):
        super().__init__(parent)
        self.setAutoFillBackground(True)
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(200)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutCubic)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        
    def wheelEvent(self, e: QWheelEvent) -> None:
        if self.smoothScrollAnimation.state() == QAbstractAnimation.Running:
            self.smoothScrollAnimation.stop()
            self.missingScroll = self.smoothScrollAnimation.endValue() - self.smoothScrollAnimation.currentValue()
        else:
            self.missingScroll = 0
        currentPos = self.verticalScrollBar().value()
        finalPos = currentPos - e.angleDelta().y() + self.missingScroll
        if finalPos < 0:
            finalPos = 0
        elif finalPos > self.verticalScrollBar().maximum():
            finalPos = self.verticalScrollBar().maximum()
        self.smoothScrollAnimation.setStartValue(currentPos)
        self.smoothScrollAnimation.setEndValue(finalPos)
        self.smoothScrollAnimation.setCurrentTime(0)
        self.smoothScrollAnimation.start()
        e.ignore()
        #super().wheelEvent(e)


class TreeWidget(QTreeWidget):
    def __init__(self, emptystr: str = "") -> None:
        super().__init__()
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(200)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutCubic)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        self.label = QLabel(emptystr, self)
        self.label.setAlignment(Qt.AlignVCenter | Qt.AlignHCenter)
        op=QGraphicsOpacityEffect(self.label)
        op.setOpacity(0.5)
        self.setRootIsDecorated(False)
        self.label.setGraphicsEffect(op)
        self.label.setAttribute(Qt.WA_TransparentForMouseEvents)
        self.label.setAutoFillBackground(True)
        font = self.label.font()
        font.setBold(True)
        font.setPointSize(20)
        self.label.setFont(font)
        self.label.setFixedWidth(2050)
        self.label.setFixedHeight(50)
        self.goTopButton = QPushButton(self)
        self.goTopButton.setIcon(QIcon(getMedia("gotop")))
        self.goTopButton.setToolTip(_("Return to top"))
        self.goTopButton.setAccessibleDescription(_("Return to top"))
        self.goTopButton.setFixedSize(24, 32)
        self.connectCustomScrollbar(self.verticalScrollBar())
        self.goTopButton.hide()
        
    def wheelEvent(self, e: QWheelEvent) -> None:
        if self.smoothScrollAnimation.state() == QAbstractAnimation.Running:
            self.smoothScrollAnimation.stop()
            self.verticalScrollBar().setValue(self.smoothScrollAnimation.endValue())
        currentPos = self.verticalScrollBar().value()
        finalPos = currentPos - e.angleDelta().y()
        self.smoothScrollAnimation.setStartValue(currentPos)
        self.smoothScrollAnimation.setEndValue(finalPos)
        self.smoothScrollAnimation.start()
        e.ignore()
        
    def connectCustomScrollbar(self, scrollbar: QScrollBar):
        try:
            self.goTopButton.clicked.disconnect()
        except RuntimeError:
            cprint("Can't disconnect")
        scrollbar.valueChanged.connect(lambda v: self.goTopButton.setVisible(v>100))
        self.goTopButton.clicked.connect(lambda: scrollbar.setValue(0))
        
    def resizeEvent(self, event: QResizeEvent) -> None:
        self.label.move((self.width()-self.label.width())//2, (self.height()-self.label.height())//2,)
        self.goTopButton.move(self.width()-24, self.height()-48)
        return super().resizeEvent(event)

    def addTopLevelItem(self, item: QTreeWidgetItem) -> None:
        self.label.setText("")
        return super().addTopLevelItem(item)

    def clear(self) -> None:
        self.label.show()
        return super().clear()

class ScrollWidget(QWidget):
    def __init__(self, scroller: QWidget) -> None:
        self.scroller = scroller
        super().__init__()

    def wheelEvent(self, event: QWheelEvent) -> None:
        self.scroller.wheelEvent(event)
        return super().wheelEvent(event)

class CustomLineEdit(QLineEdit):
    def __init__(self, parent = None):
        super().__init__(parent=parent)
        self.textChanged.connect(self.updateTextColor)
        self.updateTextColor(self.text())
        self.setClearButtonEnabled(True)

    def contextMenuEvent(self, arg__1: QContextMenuEvent) -> None:
        m = self.createStandardContextMenu()
        m.setContentsMargins(0, 0, 0, 0)
        ApplyMenuBlur(m.winId(), m)
        m.exec(arg__1.globalPos())

    def updateTextColor(self, text: str) -> None:
        if text == "":
            self.startStyleSheet = super().styleSheet()
            super().setStyleSheet(self.startStyleSheet+"color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)

    def setStyleSheet(self, styleSheet: str) -> None:
        if self.text() == "":
            self.startStyleSheet = styleSheet
            super().setStyleSheet(self.startStyleSheet+"color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)

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

class ResizableWidget(QWidget):
    resized = Signal(QResizeEvent)
    def __init__(self, parent = None) -> None:
        super().__init__(parent)
        
    def resizeEvent(self, event: QResizeEvent) -> None:
        self.resized.emit(event)
        return super().resizeEvent(event)


class DynamicScrollArea(QWidget):
    maxHeight = 200
    def __init__(self, showHideArrow: QWidget = None, parent = None) -> None:
        super().__init__(parent)
        l = QVBoxLayout()
        self.showHideArrow = showHideArrow
        l.setContentsMargins(5, 5, 5, 5)
        self.scrollArea = SmoothScrollArea()
        self.coushinWidget = QWidget()
        l.addWidget(self.coushinWidget)
        l.addWidget(self.scrollArea)
        self.w = ResizableWidget()
        self.w.resized.connect(self.rss)
        self.vlayout = QVBoxLayout()
        self.vlayout.setContentsMargins(0, 0, 0, 0)
        self.w.setLayout(self.vlayout)
        self.scrollArea.setWidget(self.w)
        self.scrollArea.setFrameShape(QFrame.NoFrame)
        self.scrollArea.setWidgetResizable(True)
        self.setLayout(l)
        self.itemCount = 0
        self.rss()

    def rss(self):
        if self.w.sizeHint().height() >= self.maxHeight:
            self.setFixedHeight(self.maxHeight)
        else:
            self.setFixedHeight(self.w.sizeHint().height()+20 if self.w.sizeHint().height() > 0 else 4)

    def removeItem(self, item: QWidget):
        self.vlayout.removeWidget(item)
        self.rss()
        self.itemCount = self.vlayout.count()
        if self.itemCount <= 0:
            globals.trayIcon.setIcon(QIcon(getMedia("greyicon"))) 
            self.showHideArrow.hide()

    def addItem(self, item: QWidget):
        self.vlayout.addWidget(item)
        self.itemCount = self.vlayout.count()
        self.showHideArrow.show()
        globals.trayIcon.setIcon(QIcon(getMedia("icon")))

class TreeWidgetItemWithQAction(QTreeWidgetItem):
    itemAction: QAction = QAction
    def __init__(self, parent = None):
        super().__init__()
        

    def setAction(self, action: QAction):
        self.itemAction = action

    def action(self) -> QAction:
        return self.itemAction

    def setHidden(self, hide: bool) -> None:
        if self.itemAction != QAction:
            self.itemAction.setVisible(not hide)
        return super().setHidden(hide)
    
    def setText(self, column: int, text: str) -> None:
        self.setToolTip(column, text)
        return super().setText(column, text)

    def treeWidget(self) -> TreeWidget:
        return super().treeWidget()

class ErrorMessage(QMainWindow):
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

class PushButtonWithAction(QPushButton):
    action: QAction = None
    def __init__(self, text: str = ""):
        super().__init__(text)
        self.action = QAction(text, self)
        self.action.triggered.connect(self.click)

class CustomComboBox(QComboBox):
    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self.setItemDelegate(QStyledItemDelegate(self))

    def showEvent(self, event: QShowEvent) -> None:
        v = self.view().window()
        ApplyMenuBlur(v.winId(), v)
        return super().showEvent(event)

    def dg(self):
        pass

class TenPxSpacer(QWidget):
    def __init__(self) -> None:
        super().__init__()
        self.setFixedWidth(10)

class CustomScrollBar(QScrollBar):
    def __init__(self):
        super().__init__()
        self.rangeChanged.connect(self.showHideIfNeeded)

    def showHideIfNeeded(self, min: int, max: int):
        self.setVisible(min != max)

class CollapsableSection(QWidget):
    oldScrollValue = 0
    showing = False
    searchMode = False
    childrenw = []
    callInMain = Signal(object)
    baseHeight: int = 70
    def __init__(self, text: str, icon: str, descText: str = "No description provided"):
        if isDark():
            self.iconMode = "white"
            semib = "Semib"
        else:
            self.iconMode = "black"
            semib = ""
        super().__init__()
        self.callInMain.connect(lambda f: f())
        self.icon = icon
        self.setObjectName("subtitleLabel")
        self.label = QLabel(text, self)
        self.setMaximumWidth(1000)
        self.descLabel = QLabel(descText, self)
        self.bg70 = QWidget(self)
        self.bg70.setObjectName("micaRegularBackground")
        self.descLabel.setObjectName("greyishLabel")
        if lang["locale"] == "zh_TW":
            self.label.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft JhengHei UI\";")
            self.descLabel.setStyleSheet("font-size: 8pt;background: none;font-family: \"Microsoft JhengHei UI\";")
        elif lang["locale"] == "zh_CN":
            self.label.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft YaHei UI\";")
            self.descLabel.setStyleSheet("font-size: 8pt;background: none;font-family: \"Microsoft YaHei UI\";")
        else:
            self.label.setStyleSheet(f"font-size: 10pt;background: none;font-family: \"Segoe UI Variable Text\";")
            self.descLabel.setStyleSheet(f"font-size: 8pt;background: none;font-family: \"Segoe UI Variable Text\";")

        self.image = QLabel(self)
        self.image.setStyleSheet(f"padding: 1px;background: transparent;")
        self.setAttribute(Qt.WA_StyledBackground)
        self.compressibleWidget = QWidget(self)
        self.compressibleWidget.show()
        #self.compressibleWidget.setAutoFillBackground(True)
        self.compressibleWidget.setObjectName("compressibleWidget")
        self.compressibleWidget.setStyleSheet("#compressibleWidget{background-color: transparent;}")

        self.showHideButton = QPushButton("", self)
        self.showHideButton.setIcon(QIcon(getMedia("collapse")))
        self.showHideButton.setStyleSheet("border: none; background-color:none;")
        self.showHideButton.clicked.connect(self.toggleChilds)
        l = QVBoxLayout()
        l.setSpacing(0)
        l.setContentsMargins(0, 0, 0, 0)
        self.childsVisible = False
        self.compressibleWidget.setLayout(l)

        self.setStyleSheet(f"QWidget#subtitleLabel{{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;}}")
        self.bg70.setStyleSheet(f"QWidget#subtitleLabel{{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;}}")


        self.showAnim = QVariantAnimation(self.compressibleWidget)
        self.showAnim.setEasingCurve(QEasingCurve.InOutCubic)
        self.showAnim.setStartValue(0)
        self.showAnim.setEndValue(1000)
        self.showAnim.valueChanged.connect(lambda v: self.setChildFixedHeight(v))
        self.showAnim.setDuration(300)
        self.showAnim.finished.connect(self.invertNotAnimated)
        self.hideAnim = QVariantAnimation(self.compressibleWidget)
        self.hideAnim.setEndValue(0)
        self.hideAnim.setEasingCurve(QEasingCurve.InOutCubic)
        self.hideAnim.valueChanged.connect(lambda v: self.setChildFixedHeight(v))
        self.hideAnim.setDuration(300)
        self.hideAnim.finished.connect(self.invertNotAnimated)
        self.scrollAnim = QVariantAnimation(self)
        self.scrollAnim.setEasingCurve(QEasingCurve.InOutCubic)
        self.scrollAnim.valueChanged.connect(lambda i: self.window().scrollArea.verticalScrollBar().setValue(i))
        self.scrollAnim.setDuration(200)
        self.NotAnimated = True

        self.button = QPushButton("", self)
        self.button.setObjectName("subtitleLabelHover")
        self.button.clicked.connect(self.toggleChilds)
        self.button.setStyleSheet(f"border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
        self.button.setStyleSheet(f"border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")
        self.setChildFixedHeight(0)

        self.newShowAnim = QVariantAnimation(self)
        self.newShowAnim.setEasingCurve(QEasingCurve.OutQuart)
        self.newShowAnim.setStartValue(self.baseHeight-20)
        self.newShowAnim.setEndValue(self.baseHeight)
        self.newShowAnim.setDuration(200)
        self.newShowAnim.valueChanged.connect(lambda i: (self.compressibleWidget.move(0, i),self.childrenOpacity.setOpacity((i-(self.baseHeight-20))/20)))

        self.newHideAnim = QVariantAnimation(self)
        self.newHideAnim.setEasingCurve(QEasingCurve.InQuart)
        self.newHideAnim.setStartValue(self.baseHeight)
        self.newHideAnim.setEndValue(self.baseHeight-20)
        self.newHideAnim.setDuration(200)
        self.newHideAnim.valueChanged.connect(lambda i: (self.compressibleWidget.move(0, i),self.childrenOpacity.setOpacity((i-(self.baseHeight-20))/20)))
        self.newHideAnim.finished.connect(lambda: (self.compressibleWidget.hide(),self.setChildFixedHeight(self.baseHeight)))

        self.childrenOpacity = QGraphicsOpacityEffect(self.compressibleWidget)
        self.childrenOpacity.setOpacity(0)
        self.compressibleWidget.setGraphicsEffect(self.childrenOpacity)

        self.compressibleWidget.move((-1500),(-1500))

    def showHideChildren(self):
        self.hideChildren()
        self.showChildren()

    def hideChildren(self) -> None:
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(self.newHideAnim.start)
        time.sleep(0.2)
        self.callInMain.emit(lambda: self.compressibleWidget.move((-1500), (-1500)))
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.baseHeight))

    def showChildren(self) -> None:
        self.callInMain.emit(lambda: self.compressibleWidget.move(0, (self.baseHeight-20)))
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(self.newShowAnim.start)

    def setChildFixedHeight(self, h: int) -> None:
        self.compressibleWidget.setFixedHeight(h)
        self.setFixedHeight(h+self.baseHeight)

    def invertNotAnimated(self):
        self.NotAnimated = not self.NotAnimated

    def toggleChilds(self):
        if self.childsVisible:
            self.childsVisible = False
            self.invertNotAnimated()
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
            Thread(target=lambda: (time.sleep(0.2),self.button.setStyleSheet(f"border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;"),self.bg70.setStyleSheet(f"border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")), daemon=True).start()
            Thread(target=self.hideChildren).start()
        else:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
            self.button.setStyleSheet(f"border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.bg70.setStyleSheet(f"border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.invertNotAnimated()
            self.childsVisible = True
            Thread(target=self.showChildren).start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def setIcon(self, icon: str) -> None:
        self.image.setPixmap(QIcon(icon).pixmap(QSize((24), (24))))

    def resizeEvent(self, event: QResizeEvent = None) -> None:
        if not self.searchMode:
            self.image.show()
            self.showHideButton.show()
            self.button.show()
            self.label.show()
            self.descLabel.show()
            self.button.move(0, 0)
            self.button.resize(self.width(), (70))
            self.showHideButton.setIconSize(QSize((12), (12)))
            self.showHideButton.setFixedSize(30, 30)
            self.showHideButton.move(self.width()-(55), (20))

            self.label.move((60), (17))
            self.label.setFixedHeight(20)
            self.descLabel.move((60), (37))
            self.descLabel.setFixedHeight(20)
            self.descLabel.setFixedWidth(self.width()-(70)-(70))

            self.image.move((17), (20))
            self.image.setFixedHeight(30)
            if self.childsVisible and self.NotAnimated:
                self.setFixedHeight(self.compressibleWidget.sizeHint().height()+(70))
                self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
            elif self.NotAnimated:
                self.setFixedHeight(70)
            self.compressibleWidget.move(0, (70))
            self.compressibleWidget.setFixedWidth(self.width())
            self.image.setFixedHeight(30)
            self.label.setFixedWidth(self.width()-(140))
            self.image.setFixedWidth(30)
            self.bg70.show()
            self.bg70.move(0, 0)
            self.bg70.resize(self.width()-(10), (70))
        else:
            self.bg70.hide()
            self.image.hide()
            self.showHideButton.hide()
            self.button.hide()
            self.image.hide()
            self.label.hide()
            self.descLabel.hide()

            self.setFixedHeight(self.compressibleWidget.sizeHint().height())
            self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
            self.compressibleWidget.move(0, 0)
            self.compressibleWidget.setFixedWidth(self.width())
        if event:
            return super().resizeEvent(event)

    def addWidget(self, widget: QWidget) -> None:
        self.compressibleWidget.layout().addWidget(widget)
        self.childrenw.append(widget)

    def getChildren(self) -> list:
        return self.childrenw

    def showEvent(self, event) -> None:
        if isDark():
            self.setIcon(self.icon.replace("black", "white"))
        else:
            self.setIcon(self.icon.replace("white", "black"))
        if self.childsVisible:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
        else:
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
        return super().showEvent(event)


class SmallCollapsableSection(CollapsableSection):
    oldScrollValue = 0
    showing = False
    childrenw = []
    callInMain = Signal(object)
    def __init__(self, text: str, icon: str):
        self.baseHeight = 40
        super().__init__(text, icon, descText="")
        self.setFixedHeight(40)
        
    def showHideChildren(self):
        self.hideChildren()
        self.showChildren()

    def hideChildren(self) -> None:
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(self.newHideAnim.start)
        time.sleep(0.2)
        self.callInMain.emit(lambda: self.compressibleWidget.move((-1500), (-1500)))
        self.callInMain.emit(lambda: self.setChildFixedHeight(40))

    def showChildren(self) -> None:
        self.callInMain.emit(lambda: self.compressibleWidget.move(0, (20)))
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(self.newShowAnim.start)

    def setChildFixedHeight(self, h: int) -> None:
        self.compressibleWidget.setFixedHeight(h)
        self.setFixedHeight(h+(40))

    def invertNotAnimated(self):
        self.NotAnimated = not self.NotAnimated

    def toggleChilds(self):
        if self.childsVisible:
            self.childsVisible = False
            self.invertNotAnimated()
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
            Thread(target=lambda: (time.sleep(0.2),self.button.setStyleSheet(f"border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;"),self.bg70.setStyleSheet(f"border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")), daemon=True).start()
            Thread(target=self.hideChildren).start()
        else:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
            self.button.setStyleSheet(f"border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.bg70.setStyleSheet(f"border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.invertNotAnimated()
            self.childsVisible = True
            Thread(target=self.showChildren).start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def setIcon(self, icon: str) -> None:
        self.image.setPixmap(QIcon(icon).pixmap(QSize((24), (24))))

    def resizeEvent(self, event: QResizeEvent = None) -> None:
            self.image.show()
            self.showHideButton.show()
            self.button.show()
            self.label.show()
            self.button.move(0, 0)
            self.button.resize(self.width(), (40))
            self.showHideButton.setIconSize(QSize((12), (12)))
            self.showHideButton.setFixedSize(30, 30)
            self.showHideButton.move(self.width()-(45), (5))

            self.label.move((45), (10))
            self.label.setFixedHeight(20)

            self.image.move((10), (8))
            self.image.setFixedHeight(24)
            if self.childsVisible and self.NotAnimated:
                self.setFixedHeight(self.compressibleWidget.sizeHint().height()+(40))
                self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
            elif self.NotAnimated:
                self.setFixedHeight(40)
            self.compressibleWidget.move(0, (40))
            self.compressibleWidget.setFixedWidth(self.width())
            self.label.setFixedWidth(self.width()-(140))
            self.image.setFixedWidth(30)
            self.bg70.show()
            self.bg70.move(0, 0)
            self.bg70.resize(self.width()-(0), (40))

    def addWidget(self, widget: QWidget) -> None:
        self.compressibleWidget.layout().addWidget(widget)
        self.childrenw.append(widget)

    def getChildren(self) -> list:
        return self.childrenw

    def showEvent(self, event) -> None:
        if isDark():
            self.setIcon(self.icon.replace("black", "white"))
        else:
            self.setIcon(self.icon.replace("white", "black"))
        if self.childsVisible:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
        else:
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
        return super().showEvent(event)

class HorizontalWidgetForSection(QWidget):
    def __init__(self, lastOne: bool = False):
        super().__init__()
        if not lastOne:
            self.setStyleSheet("#stBtn{border-radius: 0px;border-bottom: 0px}")
        self.setAttribute(Qt.WA_StyledBackground)
        self.setAutoFillBackground(True)
        self.setLayout(QHBoxLayout())
        self.setObjectName("stBtn")
        self.setFixedHeight(40)
        self.setContentsMargins(40, 0, 0, 0)
        
    def addWidget(self, w: QWidget):
        self.layout().addWidget(w)
        if w.sizeHint().height()+20 > self.height():
            self.setFixedHeight(w.sizeHint().height()+20)
            
    def addStretch(self):
        self.layout().addStretch()

class QSettingsButton(QWidget):
    clicked = Signal()
    def __init__(self, text="", btntext="", parent=None, h = 30):
        super().__init__(parent)
        self.fh = h
        self.setAttribute(Qt.WA_StyledBackground)
        self.button = QPushButton(btntext+" ", self)
        self.button.setLayoutDirection(Qt.RightToLeft)
        self.setObjectName("stBtn")
        self.label = QLabel("\u200e"+text, self)
        if lang["locale"] == "zh_TW":
            self.label.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft JhengHei UI\";font-weight: 450;")
            self.button.setStyleSheet("font-size: 10pt;font-family: \"Microsoft JhengHei UI\";font-weight: 450;")
        elif lang["locale"] == "zh_CN":
            self.label.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft YaHei UI\";font-weight: 450;")
            self.button.setStyleSheet("font-size: 10pt;font-family: \"Microsoft YaHei UI\";font-weight: 450;")
        else:
            self.label.setStyleSheet("font-size: 9pt;background: none;font-family: \"Segoe UI Variable Text\";font-weight: 450;")
            self.button.setStyleSheet("font-size: 9pt;font-family: \"Segoe UI Variable Text\";font-weight: 450;")
        self.label.setObjectName("StLbl")
        self.button.clicked.connect(self.clicked.emit)

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.button.move(self.width()-(170), 10)
        self.label.move((70), 10)
        self.label.setFixedWidth(self.width()-(250))
        self.label.setFixedHeight((self.fh))
        self.setFixedHeight((50+(self.fh-30)))
        self.button.setFixedHeight((self.fh))
        self.button.setFixedWidth(150)
        return super().resizeEvent(event)

    def setIcon(self, icon: QIcon) -> None:
        self.button.setIcon(icon)

    def text(self) -> str:
        return self.label.text() + " " + self.button.text()

class QSettingsComboBox(QWidget):
    textChanged = Signal(str)
    valueChanged = Signal(str)
    def __init__(self, text="", parent=None, buttonEnabled: bool = True):
        super().__init__(parent)
        self.buttonOn = buttonEnabled
        self.setAttribute(Qt.WA_StyledBackground)
        self.combobox = CustomComboBox(self)
        self.setObjectName("stBtn")
        self.restartButton = QPushButton("Restart ElevenClock", self)
        self.restartButton.hide()
        self.restartButton.setObjectName("AccentButton")
        self.label = QLabel("\u200e"+text, self)

        if lang["locale"] == "zh_TW":
            self.label.setStyleSheet("font-size: 11pt;background: none;font-family: \"Microsoft JhengHei UI\";font-weight: 450;")
            self.combobox.setStyleSheet("font-size: 11pt;font-family: \"Microsoft JhengHei UI\";font-weight: 450;")
            self.restartButton.setStyleSheet("font-size: 11pt;font-family: \"Microsoft JhengHei UI\";font-weight: 450;")
        elif lang["locale"] == "zh_CN":
            self.label.setStyleSheet("font-size: 11pt;background: none;font-family: \"Microsoft YaHei UI\";font-weight: 450;")
            self.combobox.setStyleSheet("font-size: 11pt;font-family: \"Microsoft YaHei UI\";font-weight: 450;")
            self.restartButton.setStyleSheet("font-size: 11pt;font-family: \"Microsoft YaHei UI\";font-weight: 450;")
        else:
            self.label.setStyleSheet("font-size: 9pt;background: none;font-family: \"Segoe UI Variable Text\";font-weight: 450;")
            self.combobox.setStyleSheet("font-size: 9pt;font-family: \"Segoe UI Variable Text\";font-weight: 450;")
            self.restartButton.setStyleSheet("font-size: 9pt;font-family: \"Segoe UI Variable Text\";font-weight: 450;")
        self.label.setObjectName("StLbl")

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def loadItems(self, items: list, index: int = -1) -> None:
        return self.setItems(items, index)

    def setItems(self, items: list, index: int = -1) -> None:
        self.combobox.addItems(items)
        try:
            if index >= 0:
                self.combobox.setCurrentIndex(index)
        except Exception as e:
            report(e)
            self.combobox.setCurrentIndex(0)
        self.combobox.currentTextChanged.connect(self.textChanged.emit)
        self.combobox.currentTextChanged.connect(self.valueChanged.emit)

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.combobox.move(self.width()-(270), 10)
        self.label.move((70), 10)
        self.label.setFixedWidth(self.width()-(530))
        self.label.setFixedHeight(30)
        if self.buttonOn:
            self.restartButton.move(self.width()-(480), 10)
            self.restartButton.setFixedWidth(200)
            self.restartButton.setFixedHeight(30)
        self.setFixedHeight(50)
        self.combobox.setFixedHeight(30)
        self.combobox.setFixedWidth(250)
        return super().resizeEvent(event)

    def setIcon(self, icon: QIcon) -> None:
        pass
        #self.button.setIcon(icon)

    def toggleRestartButton(self, force = None) -> None:
        if self.buttonOn:
            if (force == None):
                force = self.restartButton.isHidden
            if (force == True):
                self.restartButton.show()
            else:
                self.restartButton.hide()

    def text(self) -> str:
        return self.label.text() + " " + self.combobox.currentText()

class QSettingsCheckBox(QWidget):
    stateChanged = Signal(bool)
    def __init__(self, text="", parent=None, margin=70, bigfont = False):
        self.margin = margin
        super().__init__(parent)
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("stChkBg")
        self.checkbox = QCheckBox(text, self)
        if lang["locale"] == "zh_TW":
            self.checkbox.setStyleSheet(f"font-size: {14 if bigfont else 11}pt;background: none;font-family: \"Microsoft JhengHei UI\";font-weight: {700 if bigfont else 450};")
        elif lang["locale"] == "zh_CN":
            self.checkbox.setStyleSheet(f"font-size: {14 if bigfont else 11}pt;background: none;font-family: \"Microsoft YaHei UI\";font-weight: {700 if bigfont else 450};")
        else:
            self.checkbox.setStyleSheet(f"font-size: {14 if bigfont else 9}pt;background: none;font-family: \"Segoe UI Variable {'Display' if bigfont else 'Text'}\";font-weight: {700 if bigfont else 450};")
        self.checkbox.setObjectName("stChk")
        self.checkbox.stateChanged.connect(self.stateChanged.emit)
        self.setFixedHeight(50)

    def setChecked(self, checked: bool) -> None:
        self.checkbox.setChecked(checked)

    def isChecked(self) -> bool:
        return self.checkbox.isChecked()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def resizeEvent(self, event: QResizeEvent) -> None:
        if self.height() != 30:
            self.checkbox.move((self.margin), 10)
        else:
            self.checkbox.move((self.margin), 0)
        self.checkbox.setFixedHeight(30)
        self.checkbox.setFixedWidth(self.width()-(self.margin))
        return super().resizeEvent(event)

    def text(self) -> str:
        return self.checkbox.text()

class CustomPlainTextEdit(QPlainTextEdit):
    
    def contextMenuEvent(self, e: QContextMenuEvent) -> None:
        menu = self.createStandardContextMenu()
        ApplyMenuBlur(menu.winId(), menu)
        menu.exec(QCursor.pos())

class ClosableOpaqueMessage(QWidget):
    def __init__(self, text: str = None) -> None:
        super().__init__()
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, True)
        layout = QHBoxLayout()
        self.image = QLabel()
        self.image.setFixedSize(QSize(32, 32))
        self.setFixedHeight(30)
        self.text = QLabel(text)
        self.closeButton = QPushButton(self)
        self.closeButton.clicked.connect(self.hide)
        self.closeButton.setIcon(QIcon(getMedia("close")))
        self.closeButton.setIconSize(QSize(16, 16))
        self.closeButton.setFixedSize(QSize(22, 22))
        layout.addWidget(self.image)
        layout.addWidget(self.text, stretch=1)
        layout.addWidget(self.closeButton)
        self.setObjectName("bg")
        layout.setContentsMargins(12, 0, 4, 0)
        self.setLayout(layout)
        if isDark():
            self.setStyleSheet(f"""
                #bg {{
                    border: 1px solid #202020;
                    background-color: #88303030;
                    font-family: "Consolas";
                    padding: 4px;
                    border-radius: 8px;
                }}
                QLabel {{
                    color: rgb({getColors()[2]});
                }}
                QPushButton {{
                    border-radius: 6px;
                    background-color: rgba(0, 0, 0, 1%);
                    border: 0px;
                }}
                QPushButton:hover {{
                    background-color: rgba(255, 255, 255, 5%);
                }}
                QPushButton:pressed {{
                    background-color: rgba(255, 255, 255, 10%);
                }}
                """)
        else:
            self.setStyleSheet(f"""
                #bg {{
                    border: 1px solid rgb({getColors()[4]});
                    background-color: #ffffff;
                    font-family: "Consolas";
                    padding: 4px;
                    border-radius: 8px;
                }}
                QPushButton {{
                    border-radius: 6px;
                    background-color: rgba(255, 255, 255, 100%);
                    border: 0px;
                }}
                QPushButton:hover {{
                    background-color: rgba(240, 240, 240, 100%);
                }}
                QPushButton:pressed {{
                    background-color: rgba(225, 225, 225, 100%);
                }}
                """)
        
    def setText(self, text: str) -> None:
        self.text.setText(text)
        
    def setIcon(self, icon: QIcon) -> None:
        self.image.setPixmap(icon.pixmap(QSize(self.image.size())))
        
class NotClosableWidget(QWidget):
    def closeEvent(self, event: QCloseEvent) -> None:
        if event.spontaneous():
            event.ignore()
            return
        return super().closeEvent(event)
        
class PackageManager(QWidget):
    def __init__(self, text, description, image) -> None:
        super().__init__()
        mainw = QWidget(self)
        mainw.setContentsMargins(0, 0, 0, 0)
        mainw.setObjectName("bgwidget")
        mainw.setAttribute(Qt.WidgetAttribute.WA_StyledBackground, True)
        self.checkbox = QSettingsCheckBox(text, mainw, margin=0, bigfont=True)
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
if __name__ == "__main__":
    import __init__