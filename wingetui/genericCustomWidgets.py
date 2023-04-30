from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from win32mica import *
from tools import *
from tools import _
import windows_toasts

class MessageBox(QMessageBox):
    def __init__(self, parent: object = None) -> None:
        super().__init__(parent)
        ApplyMica(self.winId(), MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
        self.setStyleSheet("QMessageBox{background-color: transparent;}")
        
class SmoothScrollArea(QScrollArea):
    missingScroll = 0
    buttonVisible = False
    def __init__(self, parent = None):
        super().__init__(parent)
        self.setAutoFillBackground(True)
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(300)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutQuart)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        self.goTopButton = QPushButton(self)
        self.goTopButton.setIcon(QIcon(getMedia("gotop")))
        self.goTopButton.setToolTip(_("Return to top"))
        self.goTopButton.setAccessibleDescription(_("Return to top"))
        self.goTopButton.setFixedSize(24, 32)
        self.buttonOpacity = QGraphicsOpacityEffect()
        self.goTopButton.clicked.connect(lambda: (self.smoothScrollAnimation.setStartValue(self.verticalScrollBar().value()), self.smoothScrollAnimation.setEndValue(0), self.smoothScrollAnimation.start(), self.hideTopButton()))
        self.goTopButton.setGraphicsEffect(self.buttonOpacity)
        self.buttonOpacity.setOpacity(0)
        self.buttonAnimation = QVariantAnimation(self)
        self.buttonAnimation.setDuration(100)
        self.buttonAnimation.valueChanged.connect(lambda v: self.buttonOpacity.setOpacity(v/100))
        self.verticalScrollBar().setFixedWidth(15)
        
    def wheelEvent(self, e: QWheelEvent) -> None:
        currentPos = self.verticalScrollBar().value()
        finalPos = currentPos - e.angleDelta().y()
        self.doSmoothScroll(currentPos, finalPos)
        e.ignore()
        
    def doSmoothScroll(self, currentPos: int, finalPos: int):
        if self.smoothScrollAnimation.state() == QAbstractAnimation.Running:
            self.smoothScrollAnimation.stop()
            self.missingScroll = self.smoothScrollAnimation.endValue() - self.smoothScrollAnimation.currentValue()
        else:
            self.missingScroll = 0
        finalPos += self.missingScroll
        self.showTopButton() if finalPos>20 else self.hideTopButton()
        if finalPos < 0:
            finalPos = 0
        elif finalPos > self.verticalScrollBar().maximum():
            finalPos = self.verticalScrollBar().maximum()
        self.smoothScrollAnimation.setStartValue(currentPos)
        self.smoothScrollAnimation.setEndValue(finalPos)
        self.smoothScrollAnimation.start()
        
    def keyPressEvent(self, event: QKeyEvent) -> None:
        match event.key():
            case Qt.Key.Key_PageDown:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().value() + self.height()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_PageUp:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().value() - self.height()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_End:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().maximum()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_Home:
                currentPos = self.verticalScrollBar().value()
                finalPos = 0
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
        return super().keyPressEvent(event)
    
    def showTopButton(self):
        if not self.buttonVisible:
            self.buttonVisible = True
            self.goTopButton.raise_()
            self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity()*100))
            self.buttonAnimation.setEndValue(100)
            self.buttonAnimation.start()
        
    def hideTopButton(self):
        if self.buttonVisible:
            self.buttonVisible = False
            self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity()*100))
            self.buttonAnimation.setEndValue(0)
            self.buttonAnimation.start()
            
    def resizeEvent(self, event: QResizeEvent) -> None:
        self.goTopButton.move(self.width()-48, self.height()-48)
        return super().resizeEvent(event)

class TreeWidget(QTreeWidget):
    missingScroll: int = 0
    buttonVisible: bool = False
    def __init__(self, emptystr: str = "") -> None:
        super().__init__()
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(300)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutQuart)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        self.setIconSize(QSize(24, 24))
        self.setVerticalScrollMode(QTreeView.ScrollMode.ScrollPerPixel)
        self.setSortingEnabled(True)
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
        self.buttonOpacity = QGraphicsOpacityEffect()
        self.goTopButton = QPushButton(self)
        self.goTopButton.setIcon(QIcon(getMedia("gotop")))
        self.goTopButton.setToolTip(_("Return to top"))
        self.goTopButton.setAccessibleDescription(_("Return to top"))
        self.goTopButton.setFixedSize(24, 32)
        self.connectCustomScrollbar(self.verticalScrollBar())
        self.goTopButton.setGraphicsEffect(self.buttonOpacity)
        self.buttonOpacity.setOpacity(0)
        self.buttonAnimation = QVariantAnimation(self)
        self.buttonAnimation.setDuration(100)
        self.buttonAnimation.valueChanged.connect(lambda v: self.buttonOpacity.setOpacity(v/100))
        
    def connectCustomScrollbar(self, scrollbar: QScrollBar):
        try:
            self.goTopButton.clicked.disconnect()
        except RuntimeError:
            cprint("Can't disconnect")
        scrollbar.valueChanged.connect(lambda v: self.showTopButton() if v>20 else self.hideTopButton())
        self.goTopButton.clicked.connect(lambda: (self.smoothScrollAnimation.setStartValue(self.verticalScrollBar().value()), self.smoothScrollAnimation.setEndValue(0), self.smoothScrollAnimation.start()))
        
    def resizeEvent(self, event: QResizeEvent) -> None:
        self.label.move((self.width()-self.label.width())//2, (self.height()-self.label.height())//2,)
        self.goTopButton.move(self.width()-24, self.height()-48)
        return super().resizeEvent(event)

    def addTopLevelItem(self, item: QTreeWidgetItem) -> None:
        self.label.setText("")
        return super().addTopLevelItem(item)
    
    def showTopButton(self):
        if not self.buttonVisible:
            self.buttonVisible = True
            self.goTopButton.raise_()
            self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity()*100))
            self.buttonAnimation.setEndValue(100)
            self.buttonAnimation.start()
        
    def hideTopButton(self):
        if self.buttonVisible:
            self.buttonVisible = False
            self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity()*100))
            self.buttonAnimation.setEndValue(0)
            self.buttonAnimation.start()
        
    def clear(self) -> None:
        self.label.show()
        return super().clear()
    
    def wheelEvent(self, e: QWheelEvent) -> None:
        currentPos = self.verticalScrollBar().value()
        finalPos = currentPos - e.angleDelta().y()
        self.doSmoothScroll(currentPos, finalPos)
        e.ignore()
        
    def doSmoothScroll(self, currentPos: int, finalPos: int):
        if self.smoothScrollAnimation.state() == QAbstractAnimation.Running:
            self.smoothScrollAnimation.stop()
            self.missingScroll = self.smoothScrollAnimation.endValue() - self.smoothScrollAnimation.currentValue()
        else:
            self.missingScroll = 0
        finalPos += self.missingScroll
        if finalPos < 0:
            finalPos = 0
        elif finalPos > self.verticalScrollBar().maximum():
            finalPos = self.verticalScrollBar().maximum()
        self.smoothScrollAnimation.setStartValue(currentPos)
        self.smoothScrollAnimation.setEndValue(finalPos)
        self.smoothScrollAnimation.start()
        
    def keyPressEvent(self, event: QKeyEvent) -> None:
        match event.key():
            case Qt.Key.Key_PageDown:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().value() + self.height()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_PageUp:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().value() - self.height()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_End:
                currentPos = self.verticalScrollBar().value()
                finalPos = self.verticalScrollBar().maximum()
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
            case Qt.Key.Key_Home:
                currentPos = self.verticalScrollBar().value()
                finalPos = 0
                self.doSmoothScroll(currentPos, finalPos)
                event.ignore()
                return
        return super().keyPressEvent(event)

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

class CustomScrollBar(QScrollBar):
    def __init__(self):
        super().__init__()
        self.rangeChanged.connect(self.showHideIfNeeded)

    def showHideIfNeeded(self, min: int, max: int):
        self.setVisible(min != max)

class CustomPlainTextEdit(QPlainTextEdit):
    def contextMenuEvent(self, e: QContextMenuEvent) -> None:
        menu = self.createStandardContextMenu()
        ApplyMenuBlur(menu.winId(), menu)
        menu.exec(QCursor.pos())
       
class NotClosableWidget(QWidget):
    def closeEvent(self, event: QCloseEvent) -> None:
        if event.spontaneous():
            event.ignore()
            return
        return super().closeEvent(event)
   
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
      
class TenPxSpacer(QWidget):
    def __init__(self) -> None:
        super().__init__()
        self.setFixedWidth(10)

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

class SectionHWidget(QWidget):
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

class SectionButton(QWidget):
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

class SectionComboBox(QWidget):
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

class SectionCheckBox(QWidget):
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

class ToastNotification(QObject):
    toast: int = 0
    title: str = ""
    description: str = ""
    smallText: str = ""
    showTime: int = 3000
    callableActions: dict[int:object] = {}
    actionsReference: dict[object:str] = {}
    signalCaller: object = None
    onClickFun: object
    onDismissFun: object
    addedActions: list = []

    def __init__(self, parent = None, signalCaller: object = None):
        super().__init__(parent=parent)
        self.signalCaller = signalCaller
        self.onClickFun = self.nullFunction
        self.onDismissFun = self.nullFunctionWithParams
        self.addedActions = []
        self.actionsReference = {}
        self.callableActions = {}
        
    def nullFunction(self):
        """
        Internal private method, should never be called externally 
        """
        pass
    
    def nullFunctionWithParams(self, null1):
        """
        Internal private method, should never be called externally 
        """
        pass
        
    def setTitle(self, title: str):
        """
        Sets title of the notification
        """
        self.title = title
        
    def setDescription(self, description: str):
        """
        Sets description text of the notification
        """
        self.description = description
        
    def setDuration(self, msecs: int):
        """
        Sets the duration, in millseconds, of the notification
        """
        self.showTime = msecs

    def setSmallText(self, text: str):
        """
        Sets the smaller text shown on the bottom part of the notification
        """
        self.smallText = text

    def addAction(self, text: str, callback: object):
        self.showTime = 8000
        """
        Add a button to the notification, by giving the text and the callback function of the button
        """
        self.actionsReference[callback] = text
        
    def addOnClickCallback(self, function: object):
        """
        Set the function to be called when the notification is clicked
        """
        self.onClickFun = function
     
    def addOnDismissCallback(self, function: object):
        """
        Set the function to be called when the notification gets dismissed
        """
        self.onDismissFun = function
        
    def show(self):
        """
        Shows a toast notification with the given information
        """
        if self.description:
            template = windows_toasts.toast_types.ToastText4()
        else:
            template = windows_toasts.toast_types.ToastText2()
        template.SetFirstLine(self.title)
        if self.description:
            template.SetSecondLine(self.description)
        #template.SetDuration(self.showTime)
        for action in self.actionsReference.keys():
            actionText = self.actionsReference[action]
            if not actionText in self.addedActions:
                actionId = actionText.lower().replace(" ", "_")
                template.AddAction(windows_toasts.ToastButton(actionText, arguments=f"{actionId}"))
                self.callableActions[actionId] = action
                self.addedActions.append(actionText)
        template.on_activated=self.onAction
        template.on_dismissed=self.onDismissFun,
        template.on_failed=self.reportException
        self.toast = windows_toasts.InteractableWindowsToaster(self.smallText, notifierAUMID=f"MartiCliment.WingetUI.WingetUI.{versionName}")
        self.toast.show_toast(template)
        
    def reportException(self, id):
        """
        Internal private method, should never be called externally 
        """
        print(f"🔴 Notification {id} could not be shown")

    def hide(self) -> None:
        """
        Instantly closes the notification
        """
        self.close()
        
    def close(self) -> None:
        """
        Instantly closes the notification
        """
        self.toast.clear_toasts()
        
    def onAction(self, arguments: windows_toasts.ToastActivatedEventArgs = None, inputs: dict | None = None):
        """
        Internal private method, should never be called externally 
        """
        argument = arguments.arguments
        if argument in self.callableActions:
            if arguments:
                if self.signalCaller:
                    self.signalCaller(self.callableActions[str(argument)])
                else:
                    self.callableActions[str(argument)]()
        else:
            if self.signalCaller:
                self.signalCaller(self.onClickFun)
            else:
                self.onClickFun()
        


if __name__ == "__main__":
    import __init__