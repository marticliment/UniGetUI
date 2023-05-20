from threading import Thread

import platform
import subprocess
import os
import sys
import locale
import time
import ctypes
from PySide6 import QtGui
from PySide6.QtGui import *
from PySide6.QtCore import *
import PySide6.QtGui
from PySide6.QtWidgets import *
#from PySide6.QtCore import pyqtSignal as Signal
import external.FramelessWindow as FramelessWindow

import globals

from languages import *
from tools import *
from tools import _

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
            SelectModeSlide(),
            #SelectFullScreenSlide(),
            #DateTimeFormat(),
            #ClockAppearance(),
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
                    background-color: transparent;
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
                    background-color: rgba(80, 80, 80, 15%);
                    padding: 20px;
                    border-radius: 8px;
                    border: 1px solid rgba(100, 100, 100, 25%);
                    border-top: 1px solid rgba(100, 100, 100, 25%);
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
                    border: 5px solid rgb({colors[1]});
                    border-radius: 5px;
                    background-color: rgb({colors[1]});
                }}
                QLabel {{
                    border: none;
                    border-radius: 6px;
                }}
                #TitleLabel {{
                    font-size: 26pt;
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
                    background-color: transparent;
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
                    background-color: #ffffff;
                    padding: 20px;
                    border-radius: 8px;
                    border: 1px solid rgba(230, 230, 230, 80%);
                    height: 25px;
                    border-bottom: 1px solid rgba(220, 220, 220, 100%);
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
                    color: white;
                    background-color: rgb({colors[3]});
                    border-color: rgb({colors[2]});
                    border-bottom-color: rgb({colors[4]});
                }}
                #AccentButton:hover{{
                    background-color: rgba({colors[2]}, 80%);
                    border-color: rgb({colors[1]});
                    border-bottom-color: rgb({colors[3]});
                }}
                #AccentButton:disabled{{
                    background-color: #212121;
                    border-color: #303030;
                    border-bottom-color: #363636;
                }}
                #FocusSelector {{
                    border: 5px solid rgb({colors[1]});
                    border-radius: 5px;
                    background-color: rgb({colors[1]});
                }}
                QLabel {{
                    border: none;
                    border-radius: 6px;
                }}
                #TitleLabel {{
                    font-size: 26pt;
                }}
                """)


        self.nextWidget(anim=False)

        self.show()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

    def setWidget(self, w: QWidget, back=False, anim=True) -> None:
        self.bgWidget.setCurrentIndex(self.bgWidget.addWidget(w))
        if anim:
            if back:
                w.invertedinAnim()
            else:
                w.inAnim()

    def nextWidget(self, anim: bool = True) -> None:
        if self.currentIndex == len(self.widgetOrder)-1:
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
        self.currentIndex = len(self.widgetOrder)-1
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
        self.l = QVBoxLayout()
        self.setLayout(self.l)

        if(readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)==0):
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
                startButton.setIcon(QIcon(getPath(f"next_{self.negIconMode}.png")))
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
                    self.nextButton.clicked.connect(lambda:self.outAnim(self.next.emit))
                    self.nextButton.setIcon(QIcon(getPath(f"next_{self.negIconMode}.png")))
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
        self.l.addWidget(w, stretch=1)
        self.l.addLayout(self.navLayout, stretch=0)
        self.opacityEffect = QGraphicsOpacityEffect(self.centralWidget)
        self.centralWidget.setGraphicsEffect(self.opacityEffect)
        self.opacityEffect.setOpacity(0)

    def inAnim(self) -> None:
        anim = QVariantAnimation(self.centralWidget)
        anim.setStartValue(0)
        anim.setEndValue(100)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v/100))
        anim.setEasingCurve(QEasingCurve.OutQuad)
        anim.setDuration(200)
        anim.start()
        
        bgAnim = QPropertyAnimation(self.centralWidget, b"pos", self.centralWidget)
        pos = self.centralWidget.pos()
        pos.setX(pos.x()+(self.centralWidget.width()/20))
        bgAnim.setStartValue(pos)
        bgAnim.setEasingCurve(QEasingCurve.OutQuad)
        bgAnim.setEndValue(self.centralWidget.pos())
        bgAnim.setDuration(200)
        bgAnim.start()

    def invertedinAnim(self) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(0)
        anim.setEndValue(100)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v/100))
        anim.setEasingCurve(QEasingCurve.OutQuad)
        anim.setDuration(20)
        anim.start()
        
        bgAnim = QPropertyAnimation(self.centralWidget, b"pos", self.centralWidget)
        pos = self.centralWidget.pos()
        pos.setX(self.centralWidget.x()-(self.centralWidget.width()/20))
        bgAnim.setStartValue(pos)
        bgAnim.setEndValue(self.centralWidget.pos())
        bgAnim.setEasingCurve(QEasingCurve.OutQuad)
        bgAnim.setDuration(200)
        bgAnim.start()

    def outAnim(self, f) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(100)
        anim.setEndValue(0)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v/100))
        anim.setEasingCurve(QEasingCurve.InQuad)
        anim.setDuration(100)
        anim.start()
        anim.finished.connect(f)
        
    def invertedOutAnim(self, f) -> None:
        anim = QVariantAnimation(self)
        anim.setStartValue(100)
        anim.setEndValue(0)
        anim.valueChanged.connect(lambda v: self.opacityEffect.setOpacity(v/100))
        anim.setEasingCurve(QEasingCurve.InQuad)
        anim.setDuration(100)
        anim.start()
        anim.finished.connect(f)
        

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

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
        self.iconLabel.setMinimumHeight((self.iconSize+40))
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize))
        self.textLabel = QLabel()
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        if frame: self.layout().addSpacing((40/96*self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((30/96*self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        if frame: self.layout().addSpacing((30/96*self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize)))

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

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
        self.iconLabel.setMinimumHeight((self.iconSize+40))
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize))
        self.textLabel = QLabel()
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.button = QPushButton()
        self.button.clicked.connect(self.clicked.emit)
        self.layout().addSpacing((40/96*self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20/96*self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((20/96*self.iconSize))
        self.layout().addWidget(self.button, stretch=0)
        self.layout().addSpacing((40/96*self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setButtonText(self, t: str) -> None:
        self.button.setText(t)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize)))

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

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
        self.layout().addSpacing((40/96*self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20/96*self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((40/96*self.iconSize))

    def setText(self, text: str) -> None:
        self.textLabel.setText(text)

    def setButtonText(self, t: str) -> None:
        self.button.setText(t)

    def setIcon(self, path: str) -> None:
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap((self.iconSize), (self.iconSize), Mode=Qt.KeepAspectRatio))

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class MovableFocusSelector(QLabel):
    def __init__(self, parent: QWidget = None) -> None:
        super().__init__(parent=parent)
        self.setObjectName("FocusSelector")

    def move(self, x: int, y: int) -> None:
        return super().move(x, y)

    def resize(self, w: int, h: int) -> None:
        return super().resize(w+17, h+17)

class ClickableButtonLabelWithBiggerIcon(QPushButton):
    buttonClicked = Signal()
    lastClick = 0
    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("ButtonItem")
        self.iconSize = size
        self.setCheckable(True)
        self.setLayout(QHBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = ClickableLabel()
        self.iconLabel.setMinimumHeight((self.iconSize))
        self.iconLabel.setMinimumWidth(size)
        self.iconLabel.clicked.connect(self.animateClick)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight(int(self.iconSize*1.5))
        self.textLabel = ClickableLabel()
        self.textLabel.clicked.connect(self.animateClick)
        self.textLabel.setTextInteractionFlags(Qt.LinksAccessibleByMouse)
        self.textLabel.setWordWrap(True)
        self.textLabel.setStyleSheet("font-size: 10pt;")
        self.textLabel.setOpenExternalLinks(True)
        self.layout().addSpacing((20/96*self.iconSize))
        self.layout().addWidget(self.iconLabel, stretch=0)
        self.layout().addSpacing((20/96*self.iconSize))
        self.layout().addWidget(self.textLabel, stretch=1)
        self.layout().addSpacing((40/96*self.iconSize))
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
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap(QSize((self.iconSize+20), (self.iconSize+20)), mode=QIcon.Normal))

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class ClickableImageWithText(QPushButton):
    def __init__(self, size=96) -> None:
        super().__init__()
        self.setAttribute(Qt.WA_StyledBackground)
        self.setObjectName("ButtonItem")
        self.iconSize = size
        self.setCheckable(True)
        self.setLayout(QVBoxLayout())
        self.layout().setContentsMargins(0, 0, 0, 0)
        self.iconLabel = ClickableLabel()
        self.iconLabel.setMinimumHeight(size)
        self.setMinimumWidth((size) * 2)
        self.iconLabel.clicked.connect(self.animateClick)
        self.iconLabel.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setMinimumHeight((self.iconSize + 50))
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
        self.iconLabel.setPixmap(QIcon(getPath(path)).pixmap(QSize((self.iconSize+20), (self.iconSize+20)), mode=QIcon.Normal))

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class FirstRunSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, noNavBar = True)
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(l)
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)
        vl.addSpacing(0)

        label1 = IconLabel(size=96, frame=False)
        label1.setIcon("icon.png")
        label1.setText(f"""
             <h1>{_("Welcome to WingetUI")}</h1>
             {_("If you already know how does this work, or you want to skip the welcome wizard, please click on the bottom-left <i>Skip</i> button.")}<br>
             """)

        label2 = IconLabel(size=64, frame=True)
        label2.setIcon("rocket.png")
        label2.setText(f"""
             <h3>{_("This wizard will help you configure and customize WingetUI!")}</h3>
             {_("Please select how you want to configure WingetUI")}""") # TODO: Actually implement this
        
        self.defaultPrefs = ClickableButtonLabelWithBiggerIcon(64)
        self.defaultPrefs.setText(f"""
            <h3>{_("Default preferences - suitable for regular users</h3>")}</h3>
            {_("Search for desktop software, warn me when updates are available and do not do nerdy things. I don't want WingetUI to overcomplicate, I just want a simple <b>software store</b>")}""")
        self.defaultPrefs.setIcon("simple_user.png")
        self.defaultPrefs.buttonClicked.connect(lambda: self.outAnim(self.skipped.emit))
        
        
        self.hacker = ClickableButtonLabelWithBiggerIcon(64)
        self.hacker.setText(f"""
            <h3>{_("Customize WingetUI - for hackers and advanced users only")}</h3>
            {_("Select which <b>package managers</b> to use ({0}), configure how packages are installed, manage how administrator rights are handled, etc.").format("Winget, Chocolatey, Scoop, Npm, Pip, etc.")}""")
        self.hacker.setIcon("hacker.png")
        self.hacker.buttonClicked.connect(lambda: (self.outAnim(self.next.emit)))
        
        vl.addWidget(label1)
        #vl.addStretch()
        vl.addWidget(label2)
        vl.addStretch()
        vl.addStretch()
        vl.addStretch()
        vl.addWidget(self.defaultPrefs)
        vl.addWidget(self.hacker)
        vl.addStretch()
        #vl.addStretch()
        self.setCentralWidget(widget)
        self.opacityEffect.setOpacity(1)

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class LastSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, finishEnabled=True)
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 0, 0, 10)
        widget.setLayout(l)
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)

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
             {_("Developing is hard, and this aplication is free. But if you liked the application, you can always <b>buy me a coffee</b> :)")}""")
        donate.setButtonText(_("Donate"))
        donate.clicked.connect(lambda: os.startfile("https://ko-fi.com/martinet101"))

        report = ButtonLabel(size=64)
        report.setIcon("github.png")
        report.setText(f"""
             <h3>{_("View WingetUI on GitHub")}</h3>
             {_("View WingetUI's source code. From there, you can report bugs or suggest features, or even contribute direcly to The WingetUI Project")}""")
        report.setButtonText(_("Open GitHub"))
        report.clicked.connect(lambda: os.startfile("https://github.com/marticliment/WingetUI"))

        vl.addWidget(label1)
        vl.addStretch()
        vl.addStretch()
        vl.addWidget(youtube)
        vl.addStretch()
        vl.addWidget(donate)
        vl.addStretch()
        vl.addWidget(report)
        vl.addStretch()
        vl.addStretch()
        self.setCentralWidget(widget)

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())
    
    def showEvent(self, event: QShowEvent) -> None:
        setSettings("ShownWelcomeWizard", True)
        return super().showEvent(event)
    
class SelectModeSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(l)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon(getPath("timespan_color.png"))
        label1.setText(f"""<h1>{_("What time do you want to see?")}</h1>
                       {_("Please select one of the following and click next.")}
                       {_("If you don't know which one is the best, choose {0}").format(_("Local time"))}""")

        self.localTime = ClickableButtonLabelWithBiggerIcon(size=64)
        self.localTime.setIcon(getPath(f"desktop_cactus.png"))
        self.localTime.clicked.connect(lambda: self.toggleClockMode("secondary", shouldChangePrefs=True))
        self.localTime.setText(f"""
            <h3>{_("Local time")}</h3>
            {_("Show the local computer time. The time will not be synced with the internet and might be inaccurate")}""")

        self.internetTime = ClickableButtonLabelWithBiggerIcon(size=64)
        self.internetTime.setIcon(getPath(f"globe.png"))
        self.internetTime.clicked.connect(lambda: self.toggleClockMode("format", shouldChangePrefs=True))
        self.internetTime.setText(f"""
             <h3>{_("Internet time")}</h3>
             {_("Precise internet time. Ideal if you are <b>not</b> using any kind of VPN or proxy")}""")


        vl.addWidget(label1)
        vl.addStretch()
        vl.addWidget(self.internetTime)
        vl.addStretch()
        vl.addWidget(self.localTime)
        vl.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "secondary":
            self.clockMode = "secondary"
            self.moveSelector(self.localTime)
            if shouldChangePrefs:
                setSettings("EnableInternetTime", False, r=True)
        elif mode == "format":
            self.clockMode = "format"
            self.moveSelector(self.internetTime)
            if shouldChangePrefs:
                setSettings("EnableInternetTime", True, r=True)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments. Accepted values are: custom, format, secondary")

    def showEvent(self, event) -> None:
        if not self.defaultSelected:
            self.toggleClockMode("secondary")
        return super().showEvent(event)

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
            s.setWidth(s.width()+18)
            s.setHeight(s.height()+18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class SelectFullScreenSlide(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(l)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon(getPath("video_color.png"))
        label1.setText(f"""<h1>{_("Fullscreen behaviour")}</h1>
                       {_("WingetUI can hide when there's a fullscreen window present (when you are watching a video, you are playing, etc.), but it can also show over those windows (It might be useful if you use fullscreened terminals, etc.).<br><br>Please select one of the following and click next to continue")}""")

        self.secondaryClock = ClickableButtonLabelWithBiggerIcon(size=64)
        self.secondaryClock.setIcon(getPath(f"hide_color.png"))
        self.secondaryClock.clicked.connect(lambda: self.toggleClockMode("hide", shouldChangePrefs=True))
        self.secondaryClock.setText(f"""
            <h3>{_("Hide the clock (<i>Recommended</i>)")}</h3>
            {_("Hide the clock, as the default windows clock would do.")}""")

        self.formattedClock = ClickableButtonLabelWithBiggerIcon(size=64)
        self.formattedClock.setIcon(getPath(f"show_color.png"))
        self.formattedClock.clicked.connect(lambda: self.toggleClockMode("show", shouldChangePrefs=True))
        self.formattedClock.setText(f"""
            <h3>{_("Show the clock over the fullscreen window")}</h3>
            {_("Show the clock over fullscreen windows. This might cover some in-app controls, like youtube's exit fullscreen button, but it might be useful to see the time when playing")}""")


        vl.addWidget(label1)
        vl.addStretch()
        vl.addWidget(self.secondaryClock)
        vl.addStretch()
        vl.addWidget(self.formattedClock)
        vl.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "hide":
            self.clockMode = "hide"
            self.moveSelector(self.secondaryClock)
            if shouldChangePrefs:
                setSettings("DisableHideOnFullScreen", False, r=shouldChangePrefs)
        elif mode == "show":
            self.clockMode = "show"
            self.moveSelector(self.formattedClock)
            if shouldChangePrefs:
                setSettings("DisableHideOnFullScreen", True, r=shouldChangePrefs)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments. Accepted values are: hide, show")

    def showEvent(self, event) -> None:
        if not self.defaultSelected:
            self.toggleClockMode("hide")
        return super().showEvent(event)

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
            s.setWidth(s.width()+18)
            s.setHeight(s.height()+18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class DateTimeFormat(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(l)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon(getPath("formatting.png"))
        label1.setText(f"""<h1>{_("Let's talk: Format")}</h1>
                       {_("Please select the date and time format you like the most. You will be able to change this after in the settings window")}""")

        self.noChanges = ClickableImageWithText(size=96)
        self.noChanges.setIcon(getPath(f"default_format.png"))
        self.noChanges.clicked.connect(lambda: self.toggleClockMode("default", shouldChangePrefs=True))
        self.noChanges.setText(f"""<h3>{_("Default")}</h3>""")

        self.weekday = ClickableImageWithText(size=96)
        self.weekday.setIcon(getPath(f"weekday.png"))
        self.weekday.clicked.connect(lambda: self.toggleClockMode("weekday", shouldChangePrefs=True))
        self.weekday.setText(f"""<h3>{_("Weekday")}</h3>""")

        self.OnlyTime = ClickableImageWithText(size=96)
        self.OnlyTime.setIcon(getPath(f"onlytime.png"))
        self.OnlyTime.clicked.connect(lambda: self.toggleClockMode("OnlyTime", shouldChangePrefs=True))
        self.OnlyTime.setText(f"""<h3>{_("Only Time")}</h3>""")

        self.OnlyDate = ClickableImageWithText(size=96)
        self.OnlyDate.setIcon(getPath(f"onlydate.png"))
        self.OnlyDate.clicked.connect(lambda: self.toggleClockMode("OnlyDate", shouldChangePrefs=True))
        self.OnlyDate.setText(f"""<h3>{_("Only Date")}</h3>""")

        self.WeekNumber = ClickableImageWithText(size=96)
        self.WeekNumber.setIcon(getPath(f"weeknumber.png"))
        self.WeekNumber.clicked.connect(lambda: self.toggleClockMode("WeekNumber", shouldChangePrefs=True))
        self.WeekNumber.setText(f"""<h3>{_("Week Number")}</h3>""")

        self.Seconds = ClickableImageWithText(size=96)
        self.Seconds.setIcon(getPath(f"seconds.png"))
        self.Seconds.clicked.connect(lambda: self.toggleClockMode("Seconds", shouldChangePrefs=True))
        self.Seconds.setText(f"""<h3>{_("Ft. Seconds")}</h3>""")

        hl1 = QHBoxLayout()
        hl1.addStretch()
        hl1.addWidget(self.weekday)
        hl1.addStretch()
        hl1.addWidget(self.noChanges)
        hl1.addStretch()
        hl1.addWidget(self.Seconds)
        hl1.addStretch()

        hl2 = QHBoxLayout()
        hl2.addStretch()
        hl2.addWidget(self.OnlyTime)
        hl2.addStretch()
        hl2.addWidget(self.WeekNumber)
        hl2.addStretch()
        hl2.addWidget(self.OnlyDate)
        hl2.addStretch()

        vl.addWidget(label1)
        vl.addStretch()
        vl.addLayout(hl1)
        vl.addStretch()
        vl.addLayout(hl2)
        vl.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "Seconds":
            self.clockMode = "Seconds"
            self.moveSelector(self.Seconds)
            if shouldChangePrefs:
                setSettings("EnableSeconds", True, r=False)
                setSettings("DisableTime", False, r=False)
                setSettings("DisableDate", False, r=False)
                setSettings("EnableWeekNumber", False, r=False)
                setSettings("EnableWeekDay", False, r=True)
        elif mode == "default":
            self.clockMode = "default"
            self.moveSelector(self.noChanges)
            if shouldChangePrefs:
                setSettings("EnableSeconds", False, r=False)
                setSettings("DisableTime", False, r=False)
                setSettings("DisableDate", False, r=False)
                setSettings("EnableWeekNumber", False, r=False)
                setSettings("EnableWeekDay", False, r=True)
        elif mode == "weekday":
            self.clockMode = "weekday"
            self.moveSelector(self.weekday)
            if shouldChangePrefs:
                setSettings("EnableSeconds", False, r=False)
                setSettings("DisableTime", False, r=False)
                setSettings("DisableDate", False, r=False)
                setSettings("EnableWeekNumber", False, r=False)
                setSettings("EnableWeekDay", True, r=True)
        elif mode == "OnlyDate":
            self.clockMode = "OnlyDate"
            self.moveSelector(self.OnlyDate)
            if shouldChangePrefs:
                setSettings("EnableSeconds", False, r=False)
                setSettings("DisableTime", True, r=False)
                setSettings("DisableDate", False, r=False)
                setSettings("EnableWeekNumber", False, r=False)
                setSettings("EnableWeekDay", False, r=True)
        elif mode == "OnlyTime":
            self.clockMode = "OnlyTime"
            self.moveSelector(self.OnlyTime)
            if shouldChangePrefs:
                setSettings("EnableSeconds", False, r=False)
                setSettings("DisableTime", False, r=False)
                setSettings("DisableDate", True, r=False)
                setSettings("EnableWeekNumber", False, r=False)
                setSettings("EnableWeekDay", False, r=True)
        elif mode == "WeekNumber":
            self.clockMode = "WeekNumber"
            self.moveSelector(self.WeekNumber)
            if shouldChangePrefs:
                setSettings("EnableSeconds", False, r=False)
                setSettings("DisableTime", False, r=False)
                setSettings("DisableDate", False, r=False)
                setSettings("EnableWeekNumber", True, r=False)
                setSettings("EnableWeekDay", False, r=True)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments ("+mode+"). Accepted values are: default, weekday, OnlyTime, OnlyDate, WeekNumber, Seconds")

    def showEvent(self, event) -> None:
        if not self.defaultSelected:
            self.toggleClockMode("default")
        return super().showEvent(event)

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
            s.setWidth(s.width()+18)
            s.setHeight(s.height()+18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())

class ClockAppearance(BasicNavWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent=parent, nextGreyed=True)
        self.defaultSelected = False
        widget = QWidget()
        l = QHBoxLayout()
        l.setContentsMargins(0, 10, 0, 10)
        widget.setLayout(l)
        self.selector = MovableFocusSelector(self)
        self.selector.hide()
        vl = QVBoxLayout()
        vl.setContentsMargins(0, 0, 0, 0)
        l.addSpacing(10)
        l.addLayout(vl)

        label1 = IconLabel(size=(96), frame=False)
        label1.setIcon(getPath("appearance.png"))
        label1.setText(f"""<h1>{_("One last thing: Appearance")}</h1>
                       {_("Please select the clock style you like the most. You will be able to change this after in the settings window")}""")

        self.default = ClickableImageWithText(size=96)
        self.default.setIcon(getPath(f"default_style.png"))
        self.default.clicked.connect(lambda: self.toggleClockMode("default", shouldChangePrefs=True))
        self.default.setText(f"""<h3>{_("Default")}</h3>""")

        self.msdos = ClickableImageWithText(size=96)
        self.msdos.setIcon(getPath(f"msdos.png"))
        self.msdos.clicked.connect(lambda: self.toggleClockMode("msdos", shouldChangePrefs=True))
        self.msdos.setText(f"""<h3>MS-DOS</h3>""")

        self.win95 = ClickableImageWithText(size=96)
        self.win95.setIcon(getPath(f"win95.png"))
        self.win95.clicked.connect(lambda: self.toggleClockMode("win95", shouldChangePrefs=True))
        self.win95.setText(f"""<h3>Windows 95</h3>""")

        self.bw = ClickableImageWithText(size=96)
        self.bw.setIcon(getPath(f"bw.png"))
        self.bw.clicked.connect(lambda: self.toggleClockMode("bw", shouldChangePrefs=True))
        self.bw.setText(f"""<h3>{_("Black&White")}</h3>""")

        self.wb = ClickableImageWithText(size=96)
        self.wb.setIcon(getPath(f"wb.png"))
        self.wb.clicked.connect(lambda: self.toggleClockMode("wb", shouldChangePrefs=True))
        self.wb.setText(f"""<h3>{_("White&Black")}</h3>""")

        self.accent = ClickableImageWithText(size=96)
        self.accent.setIcon(getPath(f"accent.png"))
        self.accent.clicked.connect(lambda: self.toggleClockMode("accent", shouldChangePrefs=True))
        self.accent.setText(f"""<h3>{_("Accent")}</h3>""")

        hl1 = QHBoxLayout()
        hl1.addStretch()
        hl1.addWidget(self.msdos)
        hl1.addStretch()
        hl1.addWidget(self.default)
        hl1.addStretch()
        hl1.addWidget(self.win95)
        hl1.addStretch()

        hl2 = QHBoxLayout()
        hl2.addStretch()
        hl2.addWidget(self.bw)
        hl2.addStretch()
        hl2.addWidget(self.accent)
        hl2.addStretch()
        hl2.addWidget(self.wb)
        hl2.addStretch()

        vl.addWidget(label1)
        vl.addStretch()
        vl.addLayout(hl1)
        vl.addStretch()
        vl.addLayout(hl2)
        vl.addStretch()
        self.setCentralWidget(widget)

        self.clockMode = ""

    def toggleClockMode(self, mode: str, shouldChangePrefs: bool = False) -> None:
        self.enableNextButton()
        if shouldChangePrefs:
            self.defaultSelected = True
        if mode == "msdos":
            self.clockMode = "msdos"
            self.moveSelector(self.msdos)
            if shouldChangePrefs:
                setSettingsValue("UseCustomFont", "Consolas,10,-1,5,50,0,0,0,0,0,Regular", r=False)
                setSettingsValue("UseCustomFontColor", "0,255,0", r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettingsValue("UseCustomBgColor", "0,0,0,100", r=False)
                setSettings("AccentBackgroundcolor", False, r=True)
        elif mode == "default":
            self.clockMode = "default"
            self.moveSelector(self.default)
            if shouldChangePrefs:
                setSettings("UseCustomFont", False, r=False)
                setSettings("UseCustomFontColor", False, r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettings("UseCustomBgColor", False, r=False)
                setSettings("AccentBackgroundcolor", False, r=True)
        elif mode == "bw":
            self.clockMode = "bw"
            self.moveSelector(self.bw)
            if shouldChangePrefs:
                setSettings("UseCustomFont", False, r=False)
                setSettingsValue("UseCustomFontColor", "0,0,0", r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettingsValue("UseCustomBgColor", "255,255,255,100", r=False)
                setSettings("AccentBackgroundcolor", False, r=True)
        elif mode == "wb":
            self.clockMode = "wb"
            self.moveSelector(self.wb)
            if shouldChangePrefs:
                setSettings("UseCustomFont", False, r=False)
                setSettingsValue("UseCustomFontColor", "255,255,255", r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettingsValue("UseCustomBgColor", "0,0,0,100", r=False)
                setSettings("AccentBackgroundcolor", False, r=True)
        elif mode == "accent":
            self.clockMode = "accent"
            self.moveSelector(self.accent)
            if shouldChangePrefs:
                setSettings("UseCustomFont", False, r=False)
                setSettings("UseCustomFontColor", False, r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettings("UseCustomBgColor", False, r=False)
                setSettings("AccentBackgroundcolor", True, r=True)
        elif mode == "win95":
            self.clockMode = "win95"
            self.moveSelector(self.win95)
            if shouldChangePrefs:
                setSettingsValue("UseCustomFont", "Segoe UI,11,-1,5,50,0,0,0,0,0,Normal", r=False)
                setSettingsValue("UseCustomFontColor", "205,205,205", r=False)
                setSettings("DisableTaskbarBackgroundColor", False, r=False)
                setSettingsValue("UseCustomBgColor", "1,127,128,100.0", r=False)
                setSettings("AccentBackgroundcolor", False, r=True)
        else:
            raise ValueError("Function toggleCheckMode() called with invalid arguments ("+mode+"). Accepted values are: default, weekday, OnlyTime, OnlyDate, WeekNumber, Seconds")

    def showEvent(self, event) -> None:
        if not self.defaultSelected:
            self.toggleClockMode("default")
        return super().showEvent(event)

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
            s.setWidth(s.width()+18)
            s.setHeight(s.height()+18)
            sizeAnim.setEndValue(s)
            sizeAnim.setEasingCurve(QEasingCurve.InOutCirc)
            sizeAnim.setDuration(200)

            posAnim.start()
            sizeAnim.start()

    def get6px(self, i: int) -> int:
        return round(i*self.screen().devicePixelRatio())


if __name__ == "__main__":
    from ctypes import c_int, windll
    windll.shcore.SetProcessDpiAwareness(c_int(2))
    import __init__
