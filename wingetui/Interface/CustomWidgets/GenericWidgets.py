if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module 
    print("redirecting...")
    import subprocess, os, sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import PySide6.QtCore
import PySide6.QtGui
import PySide6.QtWidgets
import windows_toasts
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from wingetui.Interface.Tools import *
from wingetui.Interface.Tools import _
from win32mica import *


class MessageBox(QMessageBox):
    def __init__(self, parent: object = None) -> None:
        super().__init__(parent)
        ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.setStyleSheet("QMessageBox{background-color: transparent;}")


class CustomLabel(QLabel):
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


class SmoothScrollArea(QScrollArea):
    missingScroll = 0
    buttonVisible = False
    registeredThemeEvent = False
    EnableTopButton: bool = True

    def __init__(self, parent: QWidget = None, EnableTopButton: bool = True):
        super().__init__(parent)
        self.EnableTopButton = EnableTopButton
        self.setAutoFillBackground(True)
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(300)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutQuart)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        if self.EnableTopButton:
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
            self.buttonAnimation.valueChanged.connect(lambda v: self.buttonOpacity.setOpacity(v / 100))

    def wheelEvent(self, e: QWheelEvent) -> None:
        currentPos = self.verticalScrollBar().value()
        maxPos = self.verticalScrollBar().maximum()
        finalPos = currentPos - e.angleDelta().y()
        if (finalPos <= 0 and currentPos == 0) or (finalPos > maxPos and currentPos == maxPos): # If there are no scrollable contents:
            e.ignore()
        else:
            e.angleDelta().setX(0)
            e.angleDelta().setY(0)
            e.accept()
            self.doSmoothScroll(currentPos, finalPos)

    def doSmoothScroll(self, currentPos: int, finalPos: int):
        if self.smoothScrollAnimation.state() == QAbstractAnimation.Running:
            self.smoothScrollAnimation.stop()
            self.missingScroll = self.smoothScrollAnimation.endValue() - self.smoothScrollAnimation.currentValue()
        else:
            self.missingScroll = 0
        finalPos += self.missingScroll
        self.showTopButton() if finalPos > 20 else self.hideTopButton()
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
            if self.EnableTopButton:
                self.goTopButton.raise_()
                self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity() * 100))
                self.buttonAnimation.setEndValue(100)
                self.buttonAnimation.start()

    def hideTopButton(self):
        if self.buttonVisible:
            self.buttonVisible = False
            if self.EnableTopButton:
                self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity() * 100))
                self.buttonAnimation.setEndValue(0)
                self.buttonAnimation.start()

    def resizeEvent(self, event: QResizeEvent) -> None:
        if self.EnableTopButton:
            self.goTopButton.move(self.width() - 48, self.height() - 48)
        return super().resizeEvent(event)

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            try:
                Globals.mainWindow.OnThemeChange.connect(self.ApplyIcons)
                self.registeredThemeEvent = False
            except AttributeError:
                pass
            self.ApplyIcons()
        return super().showEvent(event)

    def ApplyIcons(self):
        if self.EnableTopButton:
            self.goTopButton.setIcon(QIcon(getMedia("gotop")))


class TreeWidget(QTreeWidget):
    missingScroll: int = 0
    buttonVisible: bool = False
    registeredThemeEvent = False
    EnableTopButton: bool

    def __init__(self, emptystr: str = "", EnableTopButton: bool = True) -> None:
        super().__init__()
        self.EnableTopButton = EnableTopButton
        self.header().setDefaultAlignment(Qt.AlignmentFlag.AlignCenter)
        self.smoothScrollAnimation = QVariantAnimation(self)
        self.smoothScrollAnimation.setDuration(300)
        self.smoothScrollAnimation.setEasingCurve(QEasingCurve.OutQuart)
        self.smoothScrollAnimation.valueChanged.connect(lambda v: self.verticalScrollBar().setValue(v))
        self.setIconSize(QSize(24, 24))
        self.setVerticalScrollMode(QTreeView.ScrollMode.ScrollPerPixel)
        self.setSortingEnabled(True)
        self.label = QLabel(emptystr, self)
        self.label.setAlignment(Qt.AlignVCenter | Qt.AlignHCenter)
        self.setRootIsDecorated(False)
        self.label.setAttribute(Qt.WA_TransparentForMouseEvents)
        self.label.setStyleSheet("color: #777777")
        font = self.label.font()
        font.setBold(True)
        font.setPointSize(20)
        self.label.setFont(font)
        self.label.setFixedWidth(2050)
        self.label.setFixedHeight(50)
        if self.EnableTopButton:
            self.goTopButton = QPushButton(self)
            self.goTopButton.setToolTip(_("Return to top"))
            self.goTopButton.setAccessibleDescription(_("Return to top"))
            self.goTopButton.setFixedSize(24, 32)
            self.connectCustomScrollbar(self.verticalScrollBar())
            self.buttonOpacity = QGraphicsOpacityEffect(self.goTopButton)
            self.buttonOpacity.setOpacity(0)
            self.goTopButton.setGraphicsEffect(self.buttonOpacity)
            self.buttonAnimation = QVariantAnimation(self)
            self.buttonAnimation.setDuration(100)
            self.buttonAnimation.valueChanged.connect(lambda v: self.buttonOpacity.setOpacity(v / 100))
            self.goTopButton.setIcon(QIcon(getMedia("gotop")))

    def connectCustomScrollbar(self, scrollbar: QScrollBar):
        if self.EnableTopButton:
            try:
                self.goTopButton.clicked.disconnect()
            except RuntimeError:
                pass
            self.goTopButton.clicked.connect(lambda: (self.smoothScrollAnimation.setStartValue(self.verticalScrollBar().value()), self.smoothScrollAnimation.setEndValue(0), self.smoothScrollAnimation.start()))
        scrollbar.valueChanged.connect(lambda v: self.showTopButton() if v > 20 else self.hideTopButton())


    def resizeEvent(self, event: QResizeEvent) -> None:
        self.label.move((self.width() - self.label.width()) // 2, (self.height() - self.label.height()) // 2,)
        if self.EnableTopButton:
            self.goTopButton.move(self.width() - 24, self.height() - 48)
        return super().resizeEvent(event)

    def addTopLevelItem(self, item: QTreeWidgetItem) -> None:
        self.label.setText("")
        return super().addTopLevelItem(item)

    def showTopButton(self):
        if self.EnableTopButton:
            if not self.buttonVisible:
                self.buttonVisible = True
                self.goTopButton.raise_()
                self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity() * 100))
                self.buttonAnimation.setEndValue(100)
                self.buttonAnimation.start()

    def hideTopButton(self):
        if self.EnableTopButton:
            if self.buttonVisible:
                self.buttonVisible = False
                self.buttonAnimation.setStartValue(int(self.buttonOpacity.opacity() * 100))
                self.buttonAnimation.setEndValue(0)
                self.buttonAnimation.start()

    def clear(self) -> None:
        self.label.show()
        return super().clear()

    def wheelEvent(self, e: QWheelEvent) -> None:
        currentPos = self.verticalScrollBar().value()
        maxPos = self.verticalScrollBar().maximum()
        finalPos = currentPos - e.angleDelta().y()
        if (finalPos <= 0 and currentPos == 0) or (finalPos > maxPos and currentPos == maxPos): # If there are no scrollable contents:
            e.ignore()
        else:
            e.angleDelta().setX(0)
            e.angleDelta().setY(0)
            e.accept()
            self.doSmoothScroll(currentPos, finalPos)

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

    def showEvent(self, event: QShowEvent) -> None:
        self.setUpdatesEnabled(True)
        self.label.setUpdatesEnabled(True)
        if self.EnableTopButton:
            self.goTopButton.setUpdatesEnabled(True)
        if not self.registeredThemeEvent:
            try:
                Globals.mainWindow.OnThemeChange.connect(self.ApplyIcons)
                self.registeredThemeEvent = False
            except AttributeError:
                pass
            self.ApplyIcons()
        return super().showEvent(event)

    def ApplyIcons(self):
        if self.EnableTopButton:
            self.goTopButton.setIcon(QIcon(getMedia("gotop")))


class PackageListSortingModel(QAbstractItemModel):

    def sort(self, column: int, order: Qt.SortOrder = ...) -> None:
        if column == 2:
            column = 6
        return super().sort(column, order)


class ScrollWidget(QWidget):
    def __init__(self, scroller: QWidget) -> None:
        self.scroller = scroller
        super().__init__()

    def wheelEvent(self, event: QWheelEvent) -> None:
        self.scroller.wheelEvent(event)
        return super().wheelEvent(event)


class CustomLineEdit(QLineEdit):
    def __init__(self, parent: QWidget = None):
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
            super().setStyleSheet(self.startStyleSheet + "color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)

    def setStyleSheet(self, styleSheet: str) -> None:
        if self.text() == "":
            self.startStyleSheet = styleSheet
            super().setStyleSheet(self.startStyleSheet + "color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)


class ResizableWidget(QWidget):
    resized = Signal(QResizeEvent)

    def __init__(self, parent: QWidget = None) -> None:
        super().__init__(parent)

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.resized.emit(event)
        return super().resizeEvent(event)


class DynamicScrollArea(QWidget):
    maxHeight = 200
    items = []

    def __init__(self, resizeBar: QWidget = None, parent: QWidget = None, EnableTopButton: bool = True) -> None:
        super().__init__(parent)
        self.EnableTopButton = EnableTopButton
        vLayout = QVBoxLayout()
        self.resizeBar = resizeBar
        vLayout.setContentsMargins(5, 0, 5, 5)
        self.scrollArea = SmoothScrollArea(EnableTopButton=EnableTopButton)
        self.coushinWidget = QWidget()
        vLayout.addWidget(self.coushinWidget)
        vLayout.addWidget(self.scrollArea)
        self.w = ResizableWidget()
        self.w.resized.connect(self.rss)
        self.vlayout = QVBoxLayout()
        self.vlayout.setContentsMargins(0, 0, 0, 0)
        self.w.setLayout(self.vlayout)
        self.scrollArea.setWidget(self.w)
        self.scrollArea.setFrameShape(QFrame.NoFrame)
        self.scrollArea.setWidgetResizable(True)
        self.setLayout(vLayout)
        self.itemCount = 0
        self.rss()

    def rss(self):
        """
        Legacy code
        """
        self.calculateSize()

    def calculateSize(self) -> None:
        """
        Recalculates minimum height
        """
        if self.resizeBar:
            if self.getFullHeight() >= self.maxHeight:
                self.setFixedHeight(self.maxHeight)
            else:
                self.setFixedHeight(self.getFullHeight() if self.getFullHeight() > 15 else 4)

    def getFullHeight(self) -> int:
        """
        Returns the full height of the widget
        """
        return self.w.sizeHint().height() + 20

    def removeItem(self, item: QWidget):
        try:
            if item in self.items:
                self.items.remove(item)
        except ValueError as e:
            report(e)
        item.setVisible(False)
        self.vlayout.removeWidget(item)
        self.rss()
        self.itemCount = len(self.items)
        if self.itemCount <= 0 and self.resizeBar:
            self.resizeBar.hide()

    def addItem(self, item: QWidget):
        self.items.append(item)
        self.vlayout.addWidget(item)
        self.itemCount = len(self.items)
        self.setEnabled(True)
        self.w.setEnabled(True)
        self.scrollArea.setEnabled(True)
        self.coushinWidget.setEnabled(True)
        item.setEnabled(True)
        item.setUpdatesEnabled(True)
        self.calculateSize()
        if self.resizeBar:
            self.resizeBar.show()


class TreeWidgetItemWithQAction(QTreeWidgetItem):
    itemAction: QAction = QAction

    def __init__(self, parent: QWidget = None):
        super().__init__()

    def setAction(self, action: QAction):
        self.itemAction = action

    def action(self) -> QAction:
        return self.itemAction

    def setHidden(self, hide: bool, forceShowAction: bool = False) -> None:
        if not forceShowAction:
            if self.itemAction != QAction:
                self.itemAction.setVisible(not hide)
        try:
            return super().setHidden(hide)
        except RuntimeError:
            return False

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
    disableScrolling = False
    registeredThemeEvent = False

    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WA_StyledBackground)
        self.setItemDelegate(QStyledItemDelegate(self))
        self.setObjectName("transparent")
        self.view().window().setObjectName("transparent")
        self.ApplyBackdrop()

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            try:
                Globals.mainWindow.OnThemeChange.connect(self.ApplyBackdrop)
                self.registeredThemeEvent = False
            except AttributeError:
                pass
        return super().showEvent(event)

    def ApplyBackdrop(self):
        v = self.view().window()
        ApplyMenuBlur(v.winId().__int__(), v)

    def wheelEvent(self, e: QWheelEvent) -> None:
        if self.disableScrolling:
            e.ignore()
            return False
        else:
            return super().wheelEvent(e)

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

    def __init__(self, parent=None, signalCaller: object = None):
        super().__init__(parent=parent)
        self.signalCaller = signalCaller
        self.onClickFun = self.nullFunction
        self.onDismissFun = self.nullFunction
        self.addedActions = []
        self.actionsReference = {}
        self.callableActions = {}

    def nullFunction(self):
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
            template = windows_toasts.Toast()
        else:
            template = windows_toasts.Toast()
        template.text_fields = [self.title]
        if self.description:
            template.text_fields = [self.title, self.description]
        if self.smallText:
            template.text_fields = [self.title, self.description, self.smallText]
        for action in self.actionsReference.keys():
            actionText = self.actionsReference[action]
            if actionText not in self.addedActions:
                actionId = actionText.lower().replace(" ", "_")
                template.AddAction(windows_toasts.ToastButton(actionText, arguments=f"{actionId}"))
                self.callableActions[actionId] = action
                self.addedActions.append(actionText)
        template.on_activated = self.onAction
        template.on_dismissed = lambda _1: self.onDismissFun()
        template.on_failed = lambda _1: self.reportException()
        self.toast = windows_toasts.InteractableWindowsToaster(self.smallText, notifierAUMID=Globals.AUMID if Globals.AUMID != "" else None)
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


class DraggableWindow(QWidget):
    pressed = False
    oldPos = QPoint(0, 0)

    def __init__(self, parent: QWidget = None) -> None:
        self.FixLag = False
        super().__init__(parent)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.pressed = True
        self.oldPos = event.pos()
        return super().mousePressEvent(event)

    def mouseMoveEvent(self, event: QMouseEvent) -> None:
        if self.pressed:
            self.move(self.pos() + event.pos() - self.oldPos)
        return super().mouseMoveEvent(event)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        self.pressed = False
        self.oldPos = event.pos()
        return super().mouseReleaseEvent(event)

    def moveEvent(self, event) -> None:
        if self.FixLag:
            time.sleep(0.02)
        return super().moveEvent(event)


class MovableFramelessWindow(DraggableWindow):
    def __init__(self, parent: QWidget | None = ...) -> None:
        super().__init__(parent)
        self.setWindowFlags(Qt.Dialog | Qt.CustomizeWindowHint)
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WidgetAttribute.WA_StyledBackground)
        self.setStyleSheet("margin: 0px;")
        self.backButton = QPushButton("", self)
        self.backButton.move(self.width() - 40, 0)
        self.backButton.resize(30, 30)
        self.backButton.setFlat(True)
        self.backButton.clicked.connect(self.close)
        self.backButton.show()
        self.ApplyIcons()
        self.registeredThemeEvent = False

    def ApplyIcons(self):
        if self.isVisible():
            ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)
        self.setStyleSheet("#background{background-color:" + ("transparent" if isWin11 else ("#202020" if isDark() else "white")) + ";}")
        self.backButton.setStyleSheet("QPushButton{border: none;border-radius:6px;background:transparent;}QPushButton:hover{background-color:#c42b1c;}")
        self.backButton.setIcon(QIcon(getMedia("close")))

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            try:
                Globals.mainWindow.OnThemeChange.connect(self.ApplyIcons)
                self.registeredThemeEvent = False
            except AttributeError:
                pass
            self.ApplyIcons()
        ApplyMica(self.winId(), MicaTheme.DARK if isDark() else MicaTheme.LIGHT)

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.backButton.move(self.width() - 35, 0)
        return super().resizeEvent(event)


class ButtonWithResizeSignal(QPushButton):
    resized = Signal()

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.resized.emit()
        return super().resizeEvent(event)


class VerticallyDraggableWidget(QLabel):
    pressed = False
    oldPos = QPoint(0, 0)
    dragged = Signal(int)

    def __init__(self, parent: QWidget = None) -> None:
        super().__init__(parent)
        self.setMouseTracking(True)

    def mousePressEvent(self, event: QMouseEvent) -> None:
        self.pressed = True
        self.oldPos = QCursor.pos()
        return super().mousePressEvent(event)

    def enterEvent(self, event: QEnterEvent) -> None:
        Globals.app.setOverrideCursor(QCursor(Qt.CursorShape.SizeVerCursor))
        return super().enterEvent(event)

    def leaveEvent(self, event: QEvent) -> None:
        if not self.pressed:
            Globals.app.restoreOverrideCursor()
        return super().leaveEvent(event)

    def mouseMoveEvent(self, ev: QMouseEvent) -> None:
        if self.pressed:
            self.dragged.emit(self.mapToGlobal(self.oldPos).y() - (self.mapToGlobal(QCursor.pos()).y()))
            self.oldPos = QCursor.pos()
        return super().mouseMoveEvent(ev)

    def mouseReleaseEvent(self, event: QMouseEvent) -> None:
        self.pressed = False
        self.dragged.emit(self.mapToGlobal(self.oldPos).y() - (self.mapToGlobal(QCursor.pos()).y()))
        Globals.app.restoreOverrideCursor()
        self.oldPos = QCursor.pos()
        return super().mouseReleaseEvent(event)

    def hideEvent(self, event: QHideEvent) -> None:
        Globals.app.restoreOverrideCursor()
        return super().hideEvent(event)

    def closeEvent(self, event: QCloseEvent) -> None:
        Globals.app.restoreOverrideCursor()
        return super().closeEvent(event)


class ClickableLabel(QLabel):
    clicked = Signal()

    def __init__(self, text: str = "", parent: QWidget = None):
        super().__init__(text, parent)
        self.setMouseTracking(True)

    def mousePressEvent(self, ev: QMouseEvent) -> None:
        self.clicked.emit()
        return super().mousePressEvent(ev)


class InWindowNotification(QMainWindow):
    callInMain = Signal(object)

    def __init__(self, parent: QWidget, text: str):
        super().__init__(parent.window())
        self.callInMain.emit(lambda f: f())
        if parent.window():
            self.baseGeometry = parent.window().geometry()
        else:
            self.baseGeometry = QApplication.primaryScreen().geometry()
        self.setWindowFlag(Qt.WindowType.Window, False)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.label = QLabel(text, self)
        self.setCentralWidget(self.label)
        self.label.setObjectName("InWindowNotification")
        self.setObjectName("bg")
        self.setStyleSheet("#bg{background-color: transparent;}")
        self.setWindowOpacity(0)
        self.label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.opacity = QGraphicsOpacityEffect()
        self.opacity.setOpacity(0)
        self.label.setGraphicsEffect(self.opacity)
        self.setMouseTracking(True)
        effect = QGraphicsDropShadowEffect()
        effect.setBlurRadius(5)
        effect.setXOffset(0)
        effect.setYOffset(0)
        effect.setColor(Qt.GlobalColor.black)

        self.setGraphicsEffect(effect)

    def show(self, timeout: int = 5):
        super().show()
        self.update()
        self.repaint()
        self.setFixedHeight(34)
        self.setFixedWidth(self.sizeHint().width() + 32)
        self.move(self.baseGeometry.width() // 2 - self.sizeHint().width() // 2, self.baseGeometry.height() - 100)

        self.hideAnim = QVariantAnimation()
        self.hideAnim.setEasingCurve(QEasingCurve.Type.InOutQuart)
        self.hideAnim.setStartValue(100)
        self.hideAnim.setEndValue(0)
        self.hideAnim.setDuration(300)
        self.hideAnim.valueChanged.connect(lambda v: (self.opacity.setOpacity(v / 100)))
        self.hideAnim.finished.connect(lambda: self.hide())

        self.showAnim = QVariantAnimation()
        self.showAnim.setEasingCurve(QEasingCurve.Type.InOutQuart)
        self.showAnim.setStartValue(0)
        self.showAnim.setEndValue(100)
        self.showAnim.setDuration(300)
        self.showAnim.valueChanged.connect(lambda v: self.opacity.setOpacity(v / 100))
        self.timer = QTimer(self)
        self.timer.setInterval(timeout * 1000)
        self.timer.start()
        self.timer.timeout.connect(lambda: (print(""), self.hideAnim.start(), self.timer.stop()))
        self.showAnim.start()

    def mousePressEvent(self, event: QMouseEvent) -> None:
        print("")
        self.timer.stop()
        self.hideAnim.start()
        return super().mousePressEvent(event)


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
