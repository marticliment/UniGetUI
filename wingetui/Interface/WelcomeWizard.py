"""

wingetui/welcome.py

This file contains the code that handles the Welcome Wizard

"""


import ctypes
import os
import time

from wingetui.Interface.CustomWidgets.SpecificWidgets import *
from wingetui.Core.Languages.LangReference import *
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *

from wingetui.Core.Tools import *
from wingetui.Core.Tools import _
from win32mica import *

dwm = ctypes.windll.dwmapi


class WelcomeWindow(QMainWindow):
    callback: object = None

    def __init__(self, callback: object) -> None:
        super().__init__()
        self.callback = callback

        self.switched = False

        self.widgetOrder = (
            FirstRunSlide(),
            PackageManagersSlide(),
            AdministratorPreferences(),
            UpdatesPreferences(),
            LastSlide(),
        )

        for w in self.widgetOrder:
            w.hide()

        for w in self.widgetOrder:
            w.next.connect(self.nextWidget)
            w.previous.connect(self.previousWidget)
            w.skipped.connect(self.lastWidget)
            w.finished.connect(self.close)

        self.currentIndex = -1

        self.setFixedSize((800), (600))
        self.bgWidget = QStackedWidget(self)
        self.bgWidget.setObjectName("BackgroundWidget")
        self.setWindowFlag(Qt.WindowMinimizeButtonHint, False)
        self.setWindowFlag(Qt.WindowCloseButtonHint, False)
        self.setAutoFillBackground(True)
        self.setAttribute(Qt.WA_TranslucentBackground)
        self.setWindowTitle(_("Welcome to WingetUI"))
        self.setWindowTitle(" ")
        self.setWindowIcon(QIcon(getPath("empty.png")))
        self.setCentralWidget(self.bgWidget)

        ApplyMica(self.winId().__int__(), isDark())

        colors = getColors()

        if isDark():
            self.bgWidget.setStyleSheet(f"""
                * {{
                    color: #eeeeee;
                    background-color: transparent;
                    border-radius: 4px;
                    font-family: "Segoe UI Variable Text"
                }}
                #BackgroundWidget {{
                    border: 0 solid rgba(80, 80, 80, 25%);
                    padding: 20px;
                    background-color: {'transparent' if sys.getwindowsversion().build >= 22000 else '#202020'};
                    border-radius: 0;
                    padding-left: 30px;
                    padding-right: 30px;
                }}
                QLabel {{
                    background-color: transparent;
                }}
                #SampleItem {{
                    font-family: "Segoe UI Variable Text";
                    width: 100px;
                    background-color: rgba(80, 80, 80, 7%);
                    padding: 20px;
                    border-radius: 8px;
                    border: 0px solid rgba(100, 100, 100, 15%);
                    border-top: 0px solid rgba(100, 100, 100, 15%);
                    height: 25px;
                }}
                #FramelessSampleItem {{
                    font-family: "Segoe UI Variable Text";
                    width: 100px;
                    background-color: transparent;
                    padding: 20px;
                    border-radius: 8px;
                    border: none;
                    height: 25px;
                }}
                QPushButton {{
                    font-family: "Segoe UI Variable Text";
                    font-size: 9pt;
                    width: 100px;
                    background-color: rgba(60, 60, 60, 25%);
                    border: 1px solid rgba(100, 100, 100, 25%);
                    border-top: 1px solid rgba(100, 100, 100, 25%);
                    border-radius: 8px;
                    height: 30px;
                }}
                QPushButton:hover {{
                    background-color: rgba(77, 77, 77, 50%);
                    border: 1px solid rgba(89, 89, 89, 50%);
                    border-top: 1px solid rgba(95, 95, 95, 50%);
                }}
                QPushButton:pressed {{
                    background-color: rgba(89, 89, 89, 50%);
                    border: 1px solid rgba(95, 95, 95, 50%);
                    border-top: 1px solid rgba(99, 99, 99 , 50%);
                }}
                #AccentButton{{
                    color: black;
                    background-color: rgb({colors[1]});
                    border-color: rgb({colors[1]});
                    border-bottom-color: rgb({colors[2]});
                }}
                #AccentButton:hover{{
                    background-color: rgba({colors[1]}, 80%);
                    border-color: rgb({colors[1]});
                    border-bottom-color: rgb({colors[2]});
                }}
                #AccentButton:disabled{{
                    background-color: #212121;
                    border-color: #303030;
                    border-bottom-color: #363636;
                }}
                #FocusSelector {{
                    border: 4px solid rgb({colors[1]});
                    border-radius: 16px;
                    background-color: transparent;/*rgb({colors[1]})*/;
                }}
                QLabel {{
                    border: none;
                    border-radius: 6px;
                }}
                #TitleLabel {{
                    font-size: 26pt;
                }}
                QCheckBox {{
                    font-family: Segoe UI Variable Display;
                    font-weight: bold;
                    font-size: 15pt;
                }}
                QCheckBox::indicator{{
                    height: 20px;
                    width: 20px;
                }}
                QTreeView::indicator:unchecked,QCheckBox::indicator:unchecked {{
                    background-color: rgba(30, 30, 30, 25%);
                    border: 1px solid #444444;
                    border-radius: 6px;
                }}
                QTreeView::indicator:disabled,QCheckBox::indicator:disabled {{
                    background-color: rgba(30, 30, 30, 5%);
                    color: #dddddd;
                    border: 1px solid rgba(255, 255, 255, 5%);
                    border-radius: 6px;
                }}
                QTreeView::indicator:unchecked:hover,QCheckBox::indicator:unchecked:hover {{
                    background-color: #2a2a2a;
                    border: 1px solid #444444;
                    border-radius: 6px;
                }}
                QTreeView::indicator:checked,QCheckBox::indicator:checked {{
                    border: 1px solid #444444;
                    background-color: rgba({colors[1]}, 80%);
                    border-radius: 6px;
                    image: url("{getMedia("tick")}");
                }}
                QTreeView::indicator:disabled,QCheckBox::indicator:checked:disabled {{
                    border: 1px solid #444444;
                    background-color: #303030;
                    color: #dddddd;
                    border-radius:6px;
                }}
                QTreeView::indicator:checked:hover,QCheckBox::indicator:checked:hover {{
                    border: 1px solid #444444;
                    background-color: rgb({colors[2]});
                    border-radius: 6px;
                }}
                QScrollBar {{
                    background: transparent;
                    margin: 4px;
                    margin-left: 0;
                    width: 16px;
                    height: 20px;
                    border: none;
                    border-radius: 5px;
                }}
                QScrollBar:horizontal {{
                    margin-bottom: 0;
                    padding-bottom: 0;
                    height: 12px;
                }}
                QScrollBar::handle {{
                    margin: 3px;
                    min-height: 20px;
                    min-width: 20px;
                    border-radius: 3px;
                    background: rgba(80, 80, 80, 40%);
                }}
                QScrollBar::handle:hover {{
                    margin: 3px;
                    border-radius: 3px;
                    background: rgba(112, 112, 112, 35%);
                }}
                QScrollBar::add-line {{
                    height: 0;
                    width: 0;
                    subcontrol-position: bottom;
                    subcontrol-origin: margin;
                }}
                QScrollBar::sub-line {{
                    height: 0;
                    width: 0;
                    subcontrol-position: top;
                    subcontrol-origin: margin;
                }}
                QScrollBar::up-arrow, QScrollBar::down-arrow {{
                    background: none;
                }}
                QScrollBar::add-page, QScrollBar::sub-page {{
                    background: none;
                }}
                """)
        else:
            self.bgWidget.setStyleSheet(f"""
                * {{
                    color: black;
                    background-color: transparent;
                    border-radius: 4px;
                    font-family: "Segoe UI Variable Text"
                }}
                #BackgroundWidget {{
                    border: 0 solid #eeeeee;
                    padding: 20px;
                    background-color: {'transparent' if sys.getwindowsversion().build >= 22000 else '#f5f5f5'};
                    border-radius: 0;
                    padding-left: 30px;
                    padding-right: 30px;
                }}
                QLabel {{
                    background-color: none;
                }}
                #SampleItem {{
                    font-family: "Segoe UI Variable Text";
                    width: 100px;
                    background-color: rgba(255, 255, 255, 50%);
                    padding: 20px;
                    border-radius: 8px;
                    border: 1px solid rgba(255, 255, 255, 50%);
                    height: 25px;
                }}
                #FramelessSampleItem {{
                    font-family: "Segoe UI Variable Text";
                    width: 100px;
                    background-color: transparent;
                    padding: 20px;
                    border-radius: 8px;
                    border: none;
                    height: 25px;
                }}
                QPushButton {{
                    font-family: "Segoe UI Variable Text";
                    font-size: 9pt;
                    width: 100px;
                    background-color: #ffffff;
                    border-radius: 8px;
                    border: 1px solid rgba(230, 230, 230, 80%);
                    height: 30px;
                    border-bottom: 1px solid rgba(220, 220, 220, 100%);
                }}
                QPushButton:hover {{
                    background-color: rgba(240, 240, 240, 50%);
                    border: 1px solid rgba(220, 220, 220, 80%);
                    border-bottom: 1px solid rgba(200, 200, 200, 100%);
                }}
                QPushButton:pressed {{
                    background-color: rgba(89, 89, 89, 50%);
                    border: 1px solid rgba(95, 95, 95, 50%);
                    border-top: 1px solid rgba(99, 99, 99 , 50%);
                }}
                #AccentButton{{
                    color: black;
                    background-color: rgb({colors[1]});
                    border: 1px solid rgba(230, 230, 230, 80%);
                    height: 30px;
                    border-bottom: 1px solid rgba(220, 220, 220, 100%);
                }}
                #AccentButton:hover{{
                    background-color: rgba({colors[2]}, 80%);
                    border: 1px solid rgba(95, 95, 95, 50%);
                    border-top: 1px solid rgba(99, 99, 99 , 50%);
                }}
                #AccentButton:disabled{{
                    background-color: transparent;
                    border-color: #eeeeee;
                    border-bottom-color: #eeeeee;
                }}
                #FocusSelector {{
                    border: 4px solid rgb({colors[1]});
                    border-radius: 16px;
                    background-color: transparent;
                }}
                QLabel {{
                    border: none;
                    border-radius: 6px;
                }}
                #TitleLabel {{
                    font-size: 26pt;
                }}
                QCheckBox {{
                    font-weight: bold;
                    font-dize: 15pt;
                }}
                QCheckBox::indicator{{
                    height: 20px;
                    width: 20px;
                }}
                QTreeView::indicator:unchecked,QCheckBox::indicator:unchecked {{
                    background-color: rgba(255, 255, 255, 25%);
                    border: 1px solid rgba(0, 0, 0, 10%);
                    border-radius: 6px;
                }}
                QTreeView::indicator:disabled,QCheckBox::indicator:disabled {{
                    background-color: rgba(240, 240, 240, 0%);
                    color: #444444;
                    border: 1px solid rgba(0, 0, 0, 5%);
                    border-radius: 6px;
                }}
                QTreeView::indicator:unchecked:hover,QCheckBox::indicator:unchecked:hover {{
                    background-color: rgba(0, 0, 0, 5%);
                    border: 1px solid rgba(0, 0, 0, 20%);
                    border-radius: 6px;
                }}
                QTreeView::indicator:checked,QCheckBox::indicator:checked {{
                    border: 1px solid rgb({colors[3]});
                    background-color: rgb({colors[2]});
                    border-radius: 6px;
                    image: url("{getMedia("tick")}");
                }}
                QTreeView::indicator:checked:disabled,QCheckBox::indicator:checked:disabled {{
                    border: 1px solid #444444;
                    background-color: #303030;
                    color: #444444;
                    border-radius: 6px;
                }}
                QTreeView::indicator:checked:hover,QCheckBox::indicator:checked:hover {{
                    border: 1px solid rgb({colors[3]});
                    background-color: rgb({colors[3]});
                    border-radius: 6px;
                }}
                QScrollBar {{
                    background: transparent;
                    margin: 4px;
                    margin-left: 0;
                    width: 16px;
                    height: 20px;
                    border: none;
                    border-radius: 5px;
                }}
                QScrollBar:horizontal {{
                    margin-bottom: 0;
                    padding-bottom: 0;
                    height: 12px;
                }}
                QScrollBar:vertical {{
                    background: rgba(255, 255, 255, 0%);
                    margin: 4px;
                    width: 16px;
                    border: none;
                    border-radius: 5px;
                }}
                QScrollBar::handle:vertical {{
                    margin: 3px;
                    border-radius: 3px;
                    min-height: 20px;
                    background: rgba(90, 90, 90, 25%);
                }}
                QScrollBar::handle:vertical:hover {{
                    margin: 3px;
                    border-radius: 3px;
                    background: rgba(90, 90, 90, 35%);
                }}
                QScrollBar::add-line:vertical {{
                    height: 0;
                    subcontrol-position: bottom;
                    subcontrol-origin: margin;
                }}
                QScrollBar::sub-line:vertical {{
                    height: 0;
                    subcontrol-position: top;
                    subcontrol-origin: margin;
                }}
                QScrollBar::up-arrow:vertical, QScrollBar::down-arrow:vertical {{
                    background: none;
                }}
                QScrollBar::add-page:vertical, QScrollBar::sub-page:vertical {{
                    background: none;
                }}
                """)

        self.nextWidget(anim=False)

        self.show()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def setWidget(self, w: QWidget, back=False, anim=True) -> None:
        self.bgWidget.setCurrentIndex(self.bgWidget.addWidget(w))
        if anim:
            if back:
                w.invertedinAnim()
            else:
                w.inAnim()

    def nextWidget(self, anim: bool = True) -> None:
        if self.currentIndex == len(self.widgetOrder) - 1:
            self.close()
        else:
            self.currentIndex += 1
            w: BasicNavWidget = self.widgetOrder[self.currentIndex]
            self.setWidget(w, anim=anim)

    def previousWidget(self) -> None:
        if self.currentIndex == 0:
            try:
                raise ValueError("The specified index is not present in the list of wizard widgets")
            except Exception as e:
                report(e)
        else:
            self.currentIndex -= 1
            w: BasicNavWidget = self.widgetOrder[self.currentIndex]
            self.setWidget(w, back=True)

    def lastWidget(self) -> None:
        self.currentIndex = len(self.widgetOrder) - 1
        w: BasicNavWidget = self.widgetOrder[-1]
        self.setWidget(w)

    def close(self) -> bool:
        if self.callback:
            self.callback()
        return super().close()


class BasicNavWidget(QWidget):
    next = Signal()
    previous = Signal()
    finished = Signal()
    skipped = Signal()
    centralWidget: QWidget = None

    def __init__(self, parent: bool = None, startEnabled: bool = False, closeEnabled: bool = False, finishEnabled: bool = False, nextGreyed: bool = False, noNavBar: bool = False) -> None:
        super().__init__(parent=parent)
        self.vLayout = QVBoxLayout()
        self.setLayout(self.vLayout)

        if isDark():
            self.iconMode = "white"
            self.negIconMode = "black"
        else:
            self.iconMode = "black"
            self.negIconMode = "white"

        self.navLayout = QHBoxLayout()
        if not noNavBar:
            if closeEnabled:
                closeButton = QPushButton(_("Skip"))
                closeButton.setIconSize(QSize(12, 12))
                closeButton.setFixedSize((96), (36))
                closeButton.setIcon(QIcon(getPath(f"close_{self.iconMode}.png")))
                closeButton.clicked.connect(lambda: self.outAnim(self.skipped.emit))
                self.navLayout.addWidget(closeButton)
            self.navLayout.addStretch()
            if startEnabled:
                startButton = QPushButton(_("Start"))
                startButton.setLayoutDirection(Qt.RightToLeft)
                startButton.setIconSize(QSize(12, 12))
                startButton.setFixedSize((96), (36))
                startButton.setIcon(QIcon(getPath("next_black.png")))
                startButton.clicked.connect(lambda: self.outAnim(self.next.emit))
                startButton.setObjectName("AccentButton")
                self.navLayout.addWidget(startButton)
            else:
                backButton = QPushButton("")
                backButton.setFixedSize((36), (36))
                backButton.clicked.connect(lambda: self.invertedOutAnim(self.previous.emit))
                backButton.setIcon(QIcon(getPath(f"previous_{self.iconMode}.png")))
                backButton.setIconSize(QSize(12, 12))
                self.navLayout.addWidget(backButton)
                if finishEnabled:
                    finishButton = QPushButton(_("Finish"))
                    finishButton.setObjectName("AccentButton")
                    finishButton.setFixedSize((96), (36))
                    finishButton.setIconSize(QSize(12, 12))
                    finishButton.setLayoutDirection(Qt.RightToLeft)
                    finishButton.clicked.connect(lambda: self.outAnim(self.finished.emit))
                    self.navLayout.addWidget(finishButton)
                else:
                    self.nextButton = QPushButton("")
                    self.nextButton.setEnabled(not nextGreyed)
                    self.nextButton.setIconSize(QSize(12, 12))
                    self.nextButton.setFixedSize((36), (36))
                    self.nextButton.clicked.connect(lambda: self.outAnim(self.next.emit))
                    self.nextButton.setIcon(QIcon(getPath("next_black.png")))
                    self.nextButton.setObjectName("AccentButton")
                    self.navLayout.addWidget(self.nextButton)

    def enableNextButton(self) -> None:
        self.nextButton.setEnabled(True)

    def nextWidget(self):
        self.outAnim(self.next.emit)

    def lastWidget(self):
        self.outAnim(self.skipped.emit)

    def setCentralWidget(self, w: QWidget) -> QWidget:
        self.centralWidget = w
        self.vLayout.addWidget(w, stretch=1)
        self.vLayout.addLayout(self.navLayout, stretch=0)
        self.opacityEffect = QGraphicsOpacityEffect(self.centralWidget)
        self.centralWidget.setGraphicsEffect(self.opacityEffect)
        self.opacityEffect.setOpacity(0)

    def inAnim(self) -> None:
        anim = QVariantAnimation(self.centralWidget)
        anim.setStartValue(0)
        anim.setEndValue(100)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v / 100))
        anim.setEasingCurve(QEasingCurve.OutQuad)
        anim.setDuration(200)
        anim.start()

        bgAnim = QPropertyAnimation(self.centralWidget, b"pos", self.centralWidget)
        pos = self.centralWidget.pos()
        pos.setX(pos.x() + (self.centralWidget.width() / 20))
        bgAnim.setStartValue(pos)
        bgAnim.setEasingCurve(QEasingCurve.OutQuad)
        bgAnim.setEndValue(self.centralWidget.pos())
        bgAnim.setDuration(200)
        bgAnim.start()

    def invertedinAnim(self) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(0)
        anim.setEndValue(100)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v / 100))
        anim.setEasingCurve(QEasingCurve.OutQuad)
        anim.setDuration(20)
        anim.start()

        bgAnim = QPropertyAnimation(self.centralWidget, b"pos", self.centralWidget)
        pos = self.centralWidget.pos()
        pos.setX(self.centralWidget.x() - (self.centralWidget.width() / 20))
        bgAnim.setStartValue(pos)
        bgAnim.setEndValue(self.centralWidget.pos())
        bgAnim.setEasingCurve(QEasingCurve.OutQuad)
        bgAnim.setDuration(200)
        bgAnim.start()

    def outAnim(self, f) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(100)
        anim.setEndValue(0)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v / 100))
        anim.setEasingCurve(QEasingCurve.InQuad)
        anim.setDuration(100)
        anim.start()
        anim.finished.connect(f)

    def invertedOutAnim(self, f) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(100)
        anim.setEndValue(0)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v / 100))
        anim.setEasingCurve(QEasingCurve.InQuad)
        anim.setDuration(100)
        anim.start()
        anim.finished.connect(f)

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def window(self) -> WelcomeWindow:
        return super().window()


class IconLabel(QWidget):
    def __init__(self, size=96, frame=True) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        if frame:
            self.setObjectName("SampleItem")
        else:
            self.setObjectName("FramelessSampleItem")
        self.iconSize = size
        self.setLayout(QHBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = QLabel()
        self.iconLabel.setMinimumHeight((self.iconSize + 40))
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize))
        self.textLabel = QLabel()
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        if frame:
            self.layout().addSpacing((40 / 96 * self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((30 / 96 * self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        if frame:
            self.layout().addSpacing((30 / 96 * self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize)))

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class ButtonLabel(QWidget):
    clicked = Signal()

    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("SampleItem")
        self.iconSize = size
        self.setLayout(QHBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = QLabel()
        self.iconLabel.setMinimumHeight(self.iconSize + 40)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize))
        self.textLabel = QLabel()
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.button = QPushButton()
        self.button.clicked.connect(self.clicked.emit)
        self.layout().addSpacing((40 / 96 * self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20 / 96 * self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((20 / 96 * self.iconSize))
        self.layout().addWidget(self.button, stretch=0)
        self.layout().addSpacing((40 / 96 * self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setButtonText(self, t: str) -> None:
        self.button.setText(t)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize)))

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class ClickableLabel(QLabel):
    clicked = Signal()

    def __init__(self) -> None:
        super().__init__()
        self.setMouseTracking(True)

    def mousePressEvent(self, ev) -> None:
        self.clicked.emit()
        return super().mousePressEvent(ev)


class ClickableButtonLabel(QPushButton):
    clicked = Signal()

    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("ButtonItem")
        self.iconSize = size
        self.setLayout(QHBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = QLabel()
        self.iconLabel.setMinimumHeight((self.iconSize))
        self.iconLabel.setMinimumWidth(size)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize))
        self.textLabel = QLabel()
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.layout().addSpacing((40 / 96 * self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20 / 96 * self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((40 / 96 * self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setButtonText(self, t: str) -> None:
        self.button.setText(t)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize), Mode=Qt.KeepAspectRatio))

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class MovableFocusSelector(QLabel):
    def __init__(self, parent: QWidget = None) -> None:
        super().__init__(parent=parent)
        self.setObjectName("FocusSelector")

    def move(self, x: int, y: int) -> None:
        return super().move(x, y)

    def resize(self, w: int, h: int) -> None:
        return super().resize(w + 17, h + 17)


class ClickableButtonLabelWithBiggerIcon(QPushButton):
    buttonClicked = Signal()
    lastClick = 0

    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("ButtonItem")
        self.bIconSize = size
        self.setCheckable(True)
        self.setLayout(QHBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = ClickableLabel()
        self.iconLabel.setMinimumHeight((self.bIconSize))
        self.iconLabel.setMinimumWidth(size)
        self.iconLabel.clicked.connect(self.animateClick)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight(int(self.bIconSize * 1.5))
        self.textLabel = ClickableLabel()
        self.textLabel.clicked.connect(self.animateClick)
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.layout().addSpacing((20 / 96 * self.bIconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20 / 96 * self.bIconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((40 / 96 * self.bIconSize))
        self.clicked.connect(self.mightClick)

    def mightClick(self):
        if time.time() - self.lastClick > 1:
            self.lastClick = time.time()
            self.buttonClicked.emit()

    def animateClick(self) -> None:
        self.mightClick()
        return super().animateClick()

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap(QSize((self.bIconSize + 20), (self.bIconSize + 20)), mode=QIcon.Normal))

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class ClickableImageWithText(QPushButton):
    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("ButtonItem")
        self.bIconSize = size
        self.setCheckable(True)
        self.setLayout(QVBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = ClickableLabel()
        self.iconLabel.setMinimumHeight(size)
        self.setMinimumWidth((size) * 2)
        self.iconLabel.clicked.connect(self.animateClick)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.bIconSize + 50))
        self.textLabel = ClickableLabel()
        self.textLabel.clicked.connect(self.animateClick)
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.layout().addStretch()
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addStretch()

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setButtonText(self, t: str) -> None:
        self.button.setText(t)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap(QSize((self.bIconSize + 20), (self.bIconSize + 20)), mode=QIcon.Normal))

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class FirstRunSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, noNavBar=True)
        widget = QWidget()
        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(hLayout)
        vLayout = QVBoxLayout()
        vLayout.setContentsMargins(0, 0, 0, 0)
        hLayout.addSpacing(10)
        hLayout.addLayout(vLayout)
        vLayout.addSpacing(0)

        label1 = IconLabel(size=96, frame=False)
        label1.setIcon("icon.png")
        label1.setText(f"""
             <h1>{_("Welcome to WingetUI")}</h1>
             """)

        label2 = IconLabel(size=64, frame=True)
        label2.setIcon("rocket.png")
        label2.setText(f"""
             <h3>{_("This wizard will help you configure and customize WingetUI!")}</h3>
             {_("Please select how you want to configure WingetUI")}""")

        self.defaultPrefs = ClickableButtonLabelWithBiggerIcon(64)
        self.defaultPrefs.setText(f"""
            <h3>{_("Default preferences - suitable for regular users")}</h3>
            {_("Search for desktop software, warn me when updates are available and do not do nerdy things. I don't want WingetUI to overcomplicate, I just want a simple <b>software store</b>")}""")
        self.defaultPrefs.setIcon("simple_user.png")

        def loadDefaultsAndSkip():
            setSettings("DisableUpdatesNotifications", False)
            setSettings("DisableAutoCheckforUpdates", False)
            setSettings("AutomaticallyUpdatePackages", False)

            for manager in PackageManagersList:
                setSettings(f"AlwaysElevate{manager.NAME}", False)
            setSettings("DoCacheAdminRights", False)

            setSettings("DisableWinget", False)
            setSettings("DisableChocolatey", False)
            setSettings("DisableScoop", True)
            setSettings("DisablePip", True)
            setSettings("DisableNpm", True)
            setSettings("Disable.Net Tool", True)

            self.outAnim(self.skipped.emit)

        self.defaultPrefs.buttonClicked.connect(lambda: loadDefaultsAndSkip())

        self.hacker = ClickableButtonLabelWithBiggerIcon(64)
        self.hacker.setText(f"""
            <h3>{_("Customize WingetUI - for hackers and advanced users only")}</h3>
            {_("Select which <b>package managers</b> to use ({0}), configure how packages are installed, manage how administrator rights are handled, etc.").format("Winget, Chocolatey, Scoop, Npm, Pip, etc.")}""")
        self.hacker.setIcon("hacker.png")
        self.hacker.buttonClicked.connect(lambda: (self.outAnim(self.next.emit)))

        vLayout.addWidget(label1)
        vLayout.addWidget(label2)
        vLayout.addStretch()
        vLayout.addStretch()
        vLayout.addStretch()
        vLayout.addWidget(self.defaultPrefs)
        vLayout.addWidget(self.hacker)
        vLayout.addStretch()
        self.setCentralWidget(widget)
        self.opacityEffect.setOpacity(1)

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class LastSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, finishEnabled=True)
        widget = QWidget()
        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 0, 10)
        widget.setLayout(hLayout)
        vLayout = QVBoxLayout()
        vLayout.setContentsMargins(0, 0, 0, 0)
        hLayout.addSpacing(10)
        hLayout.addLayout(vLayout)

        label1 = IconLabel(size=96, frame=False)
        label1.setIcon("finish.png")
        label1.setText(f"""<h1>{_("Systems are now ready to go!")}</h1>
                       <h3>{_("But here are other things you can do to learn about WingetUI even more:")}</h3>""")

        youtube = ButtonLabel(size=64)
        youtube.setIcon("youtube.png")
        youtube.setText(f"""
             <h3>{_("Check out some WingetUI overviews")}</h3>
             {_("There are some great videos on YouTube that showcase WingetUI and its capabilities. You could learn useful tricks and tips!")}""")
        youtube.setButtonText(_("Open"))
        youtube.clicked.connect(lambda: os.startfile("https://www.youtube.com/results?search_query=WingetUI&sp=CAI%253D"))

        donate = ButtonLabel(size=64)
        donate.setIcon("kofi.png")
        donate.setText(f"""
             <h3>{_("Suport the developer")}</h3>
             {_("Developing is hard, and this application is free. But if you liked the application, you can always <b>buy me a coffee</b> :)")}""")
        donate.setButtonText(_("Donate"))
        donate.clicked.connect(lambda: os.startfile("https://ko-fi.com/martinet101"))

        report = ButtonLabel(size=64)
        report.setIcon("github.png")
        report.setText(f"""
             <h3>{_("View WingetUI on GitHub")}</h3>
             {_("View WingetUI's source code. From there, you can report bugs or suggest features, or even contribute direcly to The WingetUI Project")}""")
        report.setButtonText(_("Open GitHub"))
        report.clicked.connect(lambda: os.startfile("https://github.com/marticliment/WingetUI"))

        vLayout.addWidget(label1)
        vLayout.addStretch()
        vLayout.addStretch()
        vLayout.addWidget(youtube)
        vLayout.addStretch()
        vLayout.addWidget(donate)
        vLayout.addStretch()
        vLayout.addWidget(report)
        vLayout.addStretch()
        vLayout.addStretch()
        self.setCentralWidget(widget)

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def showEvent(self, event: QShowEvent) -> None:
        setSettings("ShownWelcomeWizard", True)
        return super().showEvent(event)


class PackageManagersSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent)
        self.defaultSelected = False
        widget = QWidget()
        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 0, 0, 0)
        widget.setLayout(hLayout)
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        hLayout.addLayout(vl)

        label1 = IconLabel(size=(64), frame=False)
        label1.setIcon("console_color.png")
        label1.setText(f"""<h1>{_("Which package managers do you want to use?")}</h1>
                       {_("They are the programs in charge of installing, updating and removing packages.")}""")

        self.managers = DynamicScrollArea()
        winget = WelcomeWizardPackageManager("Winget", _("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"), getMedia("winget_color"))
        scoop = WelcomeWizardPackageManager("Scoop", _("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"), getMedia("scoop_color"))
        choco = WelcomeWizardPackageManager("Chocolatey", _("The classical package manager for windows. You'll find everything there. <br>Contains: <b>General Software</b>"), getMedia("choco_color"))
        pip = WelcomeWizardPackageManager("Pip", _("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"), getMedia("pip_color"))
        npm = WelcomeWizardPackageManager("Npm", _("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"), getMedia("node_color"))
        dotnet = WelcomeWizardPackageManager(".NET Tool", _("A repository full of tools designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related Tools</b>"), getMedia("dotnet_color"))

        managers = [winget, scoop, choco, pip, npm, dotnet]

        for manager in managers:
            self.managers.addItem(manager)

        def enablePackageManagers():
            setSettings("DisableWinget", not winget.isChecked())
            setSettings("DisableChocolatey", not choco.isChecked())
            if choco.isChecked():
                if shutil.which("choco.exe") is not None:
                    setSettings("UseSystemChocolatey", True)
            setSettings("DisableScoop", not scoop.isChecked())
            setSettings("DisablePip", not pip.isChecked())
            setSettings("DisableNpm", not npm.isChecked())
            setSettings("Disable.NET Tool", not npm.isChecked())

        self.nextButton.clicked.connect(lambda: enablePackageManagers())

        winget.setChecked(True)
        choco.setChecked(True)
        scoop.setChecked(shutil.which("scoop") is not None)
        npm.setChecked(shutil.which("npm") is not None)
        pip.setChecked(shutil.which("pip") is not None)
        dotnet.setChecked(shutil.which("dotnet") is not None)

        vl.addWidget(label1)
        vl.addWidget(self.managers, stretch=1)
        self.setCentralWidget(widget)

        self.clockMode = ""

    def showEvent(self, event) -> None:
        return super().showEvent(event)

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class AdministratorPreferences(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(hLayout)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vLayout = QVBoxLayout()
        vLayout.setContentsMargins(0, 0, 0, 0)
        hLayout.addSpacing(10)
        hLayout.addLayout(vLayout)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon("admin_color.png")
        label1.setText(f"""<h1>{_("Administrator rights")}</h1>
                       {_("How should installations that require administrator privileges be treated?")}""")

        self.default = ClickableButtonLabelWithBiggerIcon(64)
        self.default.setText(f"""
            <h3>{_("Ask for administrator rights when required")}</h3>
            {_("WingetUI will show a UAC prompt every time a package requires elevation to be installed.")+" "+_("This is the <b>default choice</b>.")}""")
        self.default.setIcon("shield_green.png")
        self.default.clicked.connect(lambda: self.toggleClockMode("hide", shouldChangePrefs=True))

        self.askOnce = ClickableButtonLabelWithBiggerIcon(64)
        self.askOnce.setText(f"""
            <h3>{_("Cache administrator rights, but elevate installers only when required")}</h3>
            {_("You will be prompted only once, and administrator rights will be granted to packages that request them.")+" "+_("This could represent a <b>security risk</b>.")}""")
        self.askOnce.setIcon("shield_yellow.png")
        self.askOnce.clicked.connect(lambda: self.toggleClockMode("show", shouldChangePrefs=True))

        self.askNever = ClickableButtonLabelWithBiggerIcon(64)
        self.askNever.setText(f"""
            <h3>{_("Cache administrator rights and elevate installers by default")}</h3>
            {_("You will be prompted only once, and every future installation will be elevated automatically.")+" "+_("Select only <b>if you know what you are doing</b>.")}""")
        self.askNever.setIcon("shield_red.png")
        self.askNever.clicked.connect(lambda: self.toggleClockMode("elevate", shouldChangePrefs=True))

        vLayout.addWidget(label1)
        vLayout.addStretch()
        vLayout.addWidget(self.default)
        vLayout.addStretch()
        vLayout.addWidget(self.askOnce)
        vLayout.addStretch()
        vLayout.addWidget(self.askNever)
        vLayout.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        if mode != "hidedef":
            self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "hide" or mode == "hidedef":
            self.clockMode = "hide"
            self.moveSelector(self.default)
            if shouldChangePrefs:
                for manager in PackageManagersList:
                    setSettings(f"AlwaysElevate{manager.NAME}", False)
                setSettings("DoCacheAdminRights", False)
        elif mode == "show":
            self.clockMode = "show"
            self.moveSelector(self.askOnce)
            if shouldChangePrefs:
                for manager in PackageManagersList:
                    setSettings(f"AlwaysElevate{manager.NAME}", False)
                setSettings("DoCacheAdminRights", True)
        elif mode == "elevate":
            self.clockMode = "elevate"
            self.moveSelector(self.askNever)
            if shouldChangePrefs:
                for manager in PackageManagersList:
                    setSettings(f"AlwaysElevate{manager.NAME}", True)
                setSettings("DoCacheAdminRights", True)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments. Accepted values are: hide, show")

    def moveSelector(self, w: QWidget) -> None:
        if not self.selector.isVisible():
            self.selector.show()
            self.selector.move(w.pos().x(), w.pos().y())
            self.selector.resize(w.size().width(), w.size().height())
        else:
            posAnim = QPropertyAnimation(self.selector, b"pos", self)
            posAnim.setStartValue(self.selector.pos())
            posAnim.setEndValue(w.pos())
            posAnim.setEasingCurve(QEasingCurve.InOutCirc)
            posAnim.setDuration(200)

            sizeAnim = QPropertyAnimation(self.selector, b"size", self)
            sizeAnim.setStartValue(self.selector.size())
            s = w.size()
            s.setWidth(s.width() + 18)
            s.setHeight(s.height() + 18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


class UpdatesPreferences(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        hLayout = QHBoxLayout()
        hLayout.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(hLayout)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vLayout = QVBoxLayout()
        vLayout.setContentsMargins(0, 0, 0, 0)
        hLayout.addSpacing(10)
        hLayout.addLayout(vLayout)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon("update_pc_color.png")
        label1.setText(f"""<h1>{_("Updates")}</h1>
                       {_("WingetUI can check if your software has available updates, and install them automatically if you want to")}""")

        self.default = ClickableButtonLabelWithBiggerIcon(64)
        self.default.setText(f"""
            <h3>{_("Do NOT check for updates")}</h3>
            {_("WingetUI will not check for updates periodically. They will still be checked at launch, but you won't be warned about them.")}""")
        self.default.setIcon("shield_yellow.png")
        self.default.clicked.connect(lambda: self.toggleClockMode("noupdates", shouldChangePrefs=True))

        self.askOnce = ClickableButtonLabelWithBiggerIcon(64)
        self.askOnce.setText(f"""
            <h3>{_("Check for updates periodically")}</h3>
            {_("Check for updates regularly, and ask me what to do when updates are found.")+" "+_("This is the <b>default choice</b>.")}""")
        self.askOnce.setIcon("shield_question.png")
        self.askOnce.clicked.connect(lambda: self.toggleClockMode("checkupdates", shouldChangePrefs=True))

        self.askNever = ClickableButtonLabelWithBiggerIcon(64)
        self.askNever.setText(f"""
            <h3>{_("Install updates automatically")}</h3>
            {_("Check for updates regularly, and automatically install available ones.")}""")
        self.askNever.setIcon("shield_reload.png")
        self.askNever.clicked.connect(lambda: self.toggleClockMode("installupdates", shouldChangePrefs=True))

        vLayout.addWidget(label1)
        vLayout.addStretch()
        vLayout.addWidget(self.default)
        vLayout.addStretch()
        vLayout.addWidget(self.askOnce)
        vLayout.addStretch()
        vLayout.addWidget(self.askNever)
        vLayout.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        if mode != "hidedef":
            self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "noupdates" or mode == "hidedef":
            self.clockMode = "noupdates"
            self.moveSelector(self.default)
            if shouldChangePrefs:
                setSettings("DisableUpdatesNotifications", True)
                setSettings("DisableAutoCheckforUpdates", True)
                setSettings("AutomaticallyUpdatePackages", False)
        elif mode == "checkupdates":
            self.clockMode = "checkupdates"
            self.moveSelector(self.askOnce)
            if shouldChangePrefs:
                setSettings("DisableUpdatesNotifications", False)
                setSettings("DisableAutoCheckforUpdates", False)
                setSettings("AutomaticallyUpdatePackages", False)
        elif mode == "installupdates":
            self.clockMode = "installupdates"
            self.moveSelector(self.askNever)
            if shouldChangePrefs:
                setSettings("DisableUpdatesNotifications", False)
                setSettings("DisableAutoCheckforUpdates", False)
                setSettings("AutomaticallyUpdatePackages", True)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments. Accepted values are: hide, show")

    def moveSelector(self, w: QWidget) -> None:
        if not self.selector.isVisible():
            self.selector.show()
            self.selector.move(w.pos().x(), w.pos().y())
            self.selector.resize(w.size().width(), w.size().height())
        else:
            posAnim = QPropertyAnimation(self.selector, b"pos", self)
            posAnim.setStartValue(self.selector.pos())
            posAnim.setEndValue(w.pos())
            posAnim.setEasingCurve(QEasingCurve.InOutCirc)
            posAnim.setDuration(200)

            sizeAnim = QPropertyAnimation(self.selector, b"size", self)
            sizeAnim.setStartValue(self.selector.size())
            s = w.size()
            s.setWidth(s.width() + 18)
            s.setHeight(s.height() + 18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())


if __name__ == "__main__":
    from ctypes import c_int, windll
    windll.shcore.SetProcessDpiAwareness(c_int(2))
    import __init__
