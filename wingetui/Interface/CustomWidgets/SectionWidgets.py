"""

wingetui/Interface/CustomWidgets/SectionWidgets.py

This file contains the classes for the set of Widgets used on:
 - The settings tab
 - The filter pane
 - The "Install Options" section, on the Package Details tab

"""

if __name__ == "__main__":
    import subprocess
    import os
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "__init__.py"], shell=True, cwd=os.path.join(os.path.dirname(__file__), "../..")).returncode)


from functools import partial
import PySide6.QtCore
import PySide6.QtGui
import PySide6.QtWidgets
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from tools import *
from tools import _
from win32mica import *

from Interface.CustomWidgets.GenericWidgets import *


class CollapsableSection(QWidget):
    oldScrollValue = 0
    showing = False
    searchMode = False
    childrenw = []
    callInMain = Signal(object)
    baseHeight: int = 70
    registeredThemeEvent = False

    def __init__(self, text: str, icon: str, descText: str = "No description provided"):
        if isDark():
            self.iconMode = "white"
        else:
            self.iconMode = "black"
        super().__init__()
        self.callInMain.connect(lambda f: f())
        self.icon = icon
        self.setObjectName("subtitleLabel")
        self.TitleLabel = QLabel(text, self)
        self.setMaximumWidth(1000)
        self.DescriptionLabel = QLabel(descText, self)
        self.bg70 = QWidget(self)
        self.bg70.setObjectName("micaRegularBackground")
        self.DescriptionLabel.setObjectName("greyishLabel")
        if lang["locale"] == "zh_TW":
            self.TitleLabel.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft JhengHei UI\";")
            self.DescriptionLabel.setStyleSheet("font-size: 8pt;background: none;font-family: \"Microsoft JhengHei UI\";")
        elif lang["locale"] == "zh_CN":
            self.TitleLabel.setStyleSheet("font-size: 10pt;background: none;font-family: \"Microsoft YaHei UI\";")
            self.DescriptionLabel.setStyleSheet("font-size: 8pt;background: none;font-family: \"Microsoft YaHei UI\";")
        else:
            self.TitleLabel.setStyleSheet("font-size: 10pt;background: none;font-family: \"Segoe UI Variable Text\";")
            self.DescriptionLabel.setStyleSheet("font-size: 8pt;background: none;font-family: \"Segoe UI Variable Text\";")

        self.IconLabel = QLabel(self)
        self.IconLabel.setStyleSheet("padding: 1px;background: transparent;")
        self.setAttribute(Qt.WA_StyledBackground)
        self.compressibleWidget = QWidget(self)
        self.compressibleWidget.show()
        self.compressibleWidget.setObjectName("compressibleWidget")
        self.compressibleWidget.setStyleSheet("#compressibleWidget{background-color: transparent;}")

        self.showHideButton = QPushButton("", self)
        self.showHideButton.setIcon(QIcon(getMedia("collapse")))
        self.showHideButton.setStyleSheet("border: none; background-color:none;")
        self.showHideButton.clicked.connect(self.toggleChilds)
        vLayout = QVBoxLayout()
        vLayout.setSpacing(0)
        vLayout.setContentsMargins(0, 0, 0, 0)
        self.childrenVisible = False
        self.compressibleWidget.setLayout(vLayout)

        self.setStyleSheet("QWidget#subtitleLabel{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;}")
        self.bg70.setStyleSheet("QWidget#subtitleLabel{border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;}")

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

        self.HoverableButton = QPushButton("", self)
        self.HoverableButton.setObjectName("subtitleLabelHover")
        self.HoverableButton.clicked.connect(self.toggleChilds)
        self.HoverableButton.setStyleSheet("border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
        self.HoverableButton.setStyleSheet("border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")
        self.setChildFixedHeight(0)

        self.newShowAnim = QVariantAnimation(self)
        self.newShowAnim.setEasingCurve(QEasingCurve.OutQuart)
        self.newShowAnim.setStartValue(self.baseHeight - 20)
        self.newShowAnim.setEndValue(self.baseHeight)
        self.newShowAnim.setDuration(200)
        self.newShowAnim.valueChanged.connect(lambda i: (self.compressibleWidget.move(0, i), self.childrenOpacity.setOpacity((i - (self.baseHeight - 20)) / 20)))

        self.newHideAnim = QVariantAnimation(self)
        self.newHideAnim.setEasingCurve(QEasingCurve.InQuart)
        self.newHideAnim.setStartValue(self.baseHeight)
        self.newHideAnim.setEndValue(self.baseHeight - 20)
        self.newHideAnim.setDuration(200)
        self.newHideAnim.valueChanged.connect(lambda i: (self.compressibleWidget.move(0, i), self.childrenOpacity.setOpacity((i - (self.baseHeight - 20)) / 20)))
        self.newHideAnim.finished.connect(lambda: (self.compressibleWidget.hide(), self.setChildFixedHeight(self.baseHeight)))

        self.childrenOpacity = QGraphicsOpacityEffect(self.compressibleWidget)
        self.childrenOpacity.setOpacity(0)
        self.compressibleWidget.setGraphicsEffect(self.childrenOpacity)

        self.compressibleWidget.move(-1500, -1500)
        self.ApplyIcons()

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
        self.callInMain.emit(lambda: self.compressibleWidget.move(0, (self.baseHeight - 20)))
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(self.newShowAnim.start)

    def setChildFixedHeight(self, h: int) -> None:
        self.compressibleWidget.setFixedHeight(h)
        self.setFixedHeight(h + self.baseHeight)

    def invertNotAnimated(self):
        self.NotAnimated = not self.NotAnimated

    def toggleChilds(self):
        if self.childrenVisible:
            self.childrenVisible = False
            self.invertNotAnimated()
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
            Thread(target=lambda: (time.sleep(0.2), self.HoverableButton.setStyleSheet("border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;"), self.bg70.setStyleSheet("border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")), daemon=True).start()
            Thread(target=self.hideChildren).start()
        else:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
            self.HoverableButton.setStyleSheet("border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.bg70.setStyleSheet("border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.invertNotAnimated()
            self.childrenVisible = True
            Thread(target=self.showChildren).start()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def setIcon(self, icon: str) -> None:
        self.IconLabel.setPixmap(QIcon(icon).pixmap(QSize((24), (24))))

    def resizeEvent(self, event: QResizeEvent = None) -> None:
        if not self.searchMode:
            self.IconLabel.show()
            self.showHideButton.show()
            self.HoverableButton.show()
            self.TitleLabel.show()
            self.DescriptionLabel.show()
            self.HoverableButton.move(0, 0)
            self.HoverableButton.resize(self.width(), 70)
            self.showHideButton.setIconSize(QSize(12, 12))
            self.showHideButton.setFixedSize(30, 30)
            self.showHideButton.move(self.width() - 55, 20)

            self.TitleLabel.move(60, 17)
            self.TitleLabel.setFixedHeight(20)
            self.DescriptionLabel.move(60, 37)
            self.DescriptionLabel.setFixedHeight(20)
            self.DescriptionLabel.setFixedWidth(self.width() - 14)

            self.IconLabel.move(17, 20)
            self.IconLabel.setFixedHeight(30)
            if self.childrenVisible and self.NotAnimated:
                self.setFixedHeight(self.compressibleWidget.sizeHint().height() + 70)
                self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
            elif self.NotAnimated:
                self.setFixedHeight(70)
            self.compressibleWidget.move(0, 70)
            self.compressibleWidget.setFixedWidth(self.width() + 10)
            self.IconLabel.setFixedHeight(30)
            self.TitleLabel.setFixedWidth(self.width() - 140)
            self.IconLabel.setFixedWidth(30)
            self.bg70.show()
            self.bg70.move(0, 0)
            self.bg70.resize(self.width(), 70)
        else:
            self.bg70.hide()
            self.IconLabel.hide()
            self.showHideButton.hide()
            self.HoverableButton.hide()
            self.IconLabel.hide()
            self.TitleLabel.hide()
            self.DescriptionLabel.hide()

            self.setFixedHeight(self.compressibleWidget.sizeHint().height())
            self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
            self.compressibleWidget.move(0, 0)
        if event:
            return super().resizeEvent(event)

    def addWidget(self, widget: QWidget) -> None:
        self.compressibleWidget.layout().addWidget(widget)
        self.childrenw.append(widget)

    def getChildren(self) -> list:
        return self.childrenw

    def showEvent(self, event: QShowEvent) -> None:
        if not self.registeredThemeEvent:
            self.registeredThemeEvent = False
            globals.mainWindow.OnThemeChange.connect(self.ApplyIcons)
            self.ApplyIcons()
        return super().showEvent(event)

    def ApplyIcons(self):
        if isDark():
            self.setIcon(self.icon.replace("black", "white"))
        else:
            self.setIcon(self.icon.replace("white", "black"))
        if self.childrenVisible:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
        else:
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))


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
        self.callInMain.emit(lambda: self.compressibleWidget.move(-1500, -1500))
        self.callInMain.emit(lambda: self.setChildFixedHeight(40))

    def showChildren(self) -> None:
        self.callInMain.emit(lambda: self.compressibleWidget.move(0, 20))
        self.callInMain.emit(lambda: self.setChildFixedHeight(self.compressibleWidget.sizeHint().height()))
        self.callInMain.emit(lambda: self.compressibleWidget.show())
        self.callInMain.emit(self.newShowAnim.start)

    def setChildFixedHeight(self, h: int) -> None:
        self.compressibleWidget.setFixedHeight(h)
        self.setFixedHeight(h + 40)

    def invertNotAnimated(self):
        self.NotAnimated = not self.NotAnimated

    def toggleChilds(self):
        if self.childrenVisible:
            self.childrenVisible = False
            self.invertNotAnimated()
            self.showHideButton.setIcon(QIcon(getMedia("collapse")))
            Thread(target=lambda: (time.sleep(0.2), self.HoverableButton.setStyleSheet("border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;"), self.bg70.setStyleSheet("border-bottom-left-radius: 8px;border-bottom-right-radius: 8px;")), daemon=True).start()
            Thread(target=self.hideChildren).start()
        else:
            self.showHideButton.setIcon(QIcon(getMedia("expand")))
            self.HoverableButton.setStyleSheet("border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.bg70.setStyleSheet("border-bottom-left-radius: 0;border-bottom-right-radius: 0;")
            self.invertNotAnimated()
            self.childrenVisible = True
            Thread(target=self.showChildren).start()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def setIcon(self, icon: str) -> None:
        self.IconLabel.setPixmap(QIcon(icon).pixmap(QSize(24, 24)))

    def resizeEvent(self, event: QResizeEvent = None) -> None:
        self.IconLabel.show()
        self.showHideButton.show()
        self.HoverableButton.show()
        self.TitleLabel.show()
        self.HoverableButton.move(0, 0)
        self.HoverableButton.resize(self.width(), 40)
        self.showHideButton.setIconSize(QSize(12, 12))
        self.showHideButton.setFixedSize(30, 30)
        self.showHideButton.move(self.width() - 45, 5)

        self.TitleLabel.move(45, 10)
        self.TitleLabel.setFixedHeight(20)

        self.IconLabel.move(10, 8)
        self.IconLabel.setFixedHeight(24)
        if self.childrenVisible and self.NotAnimated:
            self.setFixedHeight(self.compressibleWidget.sizeHint().height() + 40)
            self.compressibleWidget.setFixedHeight(self.compressibleWidget.sizeHint().height())
        elif self.NotAnimated:
            self.setFixedHeight(40)
        self.compressibleWidget.move(0, 40)
        self.compressibleWidget.setFixedWidth(self.width())
        self.TitleLabel.setFixedWidth(self.width() - 140)
        self.IconLabel.setFixedWidth(30)
        self.bg70.show()
        self.bg70.move(0, 0)
        self.bg70.resize(self.width(), 40)

    def addWidget(self, widget: QWidget) -> None:
        self.compressibleWidget.layout().addWidget(widget)
        self.childrenw.append(widget)

    def getChildren(self) -> list:
        return self.childrenw


class SectionHWidget(QWidget):
    def __init__(self, lastOne: bool = False, smallerMargins: bool = False, biggerMargins: bool = False):
        super().__init__()
        if not lastOne:
            self.setStyleSheet("#stBtn{border-radius: 0px;border-bottom: 0px}")
        self.setAttribute(Qt.WA_StyledBackground)
        self.setAutoFillBackground(True)
        self.setLayout(QHBoxLayout())
        self.setObjectName("stBtn")
        self.setFixedHeight(40)
        if smallerMargins:
            self.setStyleSheet(self.styleSheet() + "#stBtn{margin: 0px;}")
            self.setContentsMargins(0, 0, 0, 0)
        elif biggerMargins:
            self.setContentsMargins(65, 0, 10, 0)
        else:
            self.setContentsMargins(40, 0, 0, 0)

    def addWidget(self, w: QWidget):
        self.layout().addWidget(w)
        if w.sizeHint().height() + 20 > self.height():
            self.setFixedHeight(w.sizeHint().height() + 20)

    def addStretch(self):
        self.layout().addStretch()


class SectionVWidget(QWidget):
    def __init__(self, lastOne: bool = False, smallerMargins: bool = False):
        super().__init__()
        if not lastOne:
            self.setStyleSheet("#stBtn{border-radius: 0px;border-bottom: 0px}")
        self.setAttribute(Qt.WA_StyledBackground)
        self.setAutoFillBackground(True)
        self.setLayout(QVBoxLayout())
        self.setObjectName("stBtn")
        if smallerMargins:
            self.setStyleSheet(self.styleSheet() + "#stBtn{margin: 0px;}")
            self.setContentsMargins(5, 0, 0, 0)
        else:
            self.setContentsMargins(40, 0, 0, 0)

    def addWidget(self, w: QWidget):
        self.layout().addWidget(w)

    def addStretch(self):
        self.layout().addStretch()


class SectionButton(QWidget):
    clicked = Signal()

    def __init__(self, text="", btntext="", parent=None, h: int = 30):
        super().__init__(parent)
        self.fh = h
        self.setAttribute(Qt.WA_StyledBackground)
        self.button = QPushButton(btntext + " ", self)
        self.button.setLayoutDirection(Qt.RightToLeft)
        self.setObjectName("stBtn")
        self.button.setMinimumWidth(270)
        self.setFixedHeight(50)
        self.button.setFixedHeight(30)
        self.label = QLabel(text, self)
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
        self.setLayout(QHBoxLayout())
        self.layout().addWidget(self.label)
        self.layout().addStretch()
        self.layout().addWidget(self.button)
        self.layout().setContentsMargins(70, 0, 20, 0)

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
        self.combobox.disableScrolling = True
        self.combobox.setFixedWidth(270)
        self.setObjectName("stBtn")
        self.restartButton = QPushButton("Restart WingetUI", self)
        self.restartButton.hide()
        self.restartButton.setFixedHeight(30)
        self.restartButton.setFixedWidth(200)
        self.restartButton.setObjectName("AccentButton")
        self.label = QLabel(text, self)

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
        self.setFixedHeight(50)
        self.setLayout(QHBoxLayout())
        self.layout().addWidget(self.label)
        self.layout().addStretch()
        self.layout().addWidget(self.restartButton)
        self.layout().addWidget(self.combobox)
        self.layout().setContentsMargins(70, 0, 20, 0)

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

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

    def toggleRestartButton(self, force=None) -> None:
        if self.buttonOn:
            if force is None:
                force = self.restartButton.isHidden
            if force is True:
                self.restartButton.show()
            else:
                self.restartButton.hide()

    def text(self) -> str:
        return self.label.text() + " " + self.combobox.currentText()


class SectionCheckBox(QWidget):
    stateChanged = Signal(bool)

    def __init__(self, text="", parent=None, margin=70, bigfont: bool = False):
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
        self.setLayout(QHBoxLayout())
        self.layout().addWidget(self.checkbox)
        self.layout().setContentsMargins(70, 0, 20, 0)

    def setChecked(self, checked: bool) -> None:
        self.checkbox.setChecked(checked)

    def isChecked(self) -> bool:
        return self.checkbox.isChecked()

    def get6px(self, i: int) -> int:
        return round(i * self.screen().devicePixelRatio())

    def text(self) -> str:
        return self.checkbox.text()


class SectionCheckBoxTextBox(SectionCheckBox):
    stateChanged = Signal(bool)
    valueChanged = Signal(str)

    def __init__(self, text: str, parent=None, helpLabel: str = ""):
        super().__init__(text=text, parent=parent)
        self.setAttribute(Qt.WA_StyledBackground)
        self.lineedit = CustomLineEdit(self)
        self.oldtext = ""
        self.lineedit.setObjectName("")
        self.lineedit.textChanged.connect(self.valuechangedEvent)
        self.checkbox.stateChanged.connect(self.stateChangedEvent)
        self.helplabel = QLabel(helpLabel, self)
        self.helplabel.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        self.helplabel.setOpenExternalLinks(True)
        self.stateChangedEvent(self.checkbox.isChecked())
        self.checkbox.move((70), 10)
        self.checkbox.setFixedHeight(30)
        self.setFixedHeight(50)

        self.setLayout(QHBoxLayout())
        self.layout().addWidget(self.checkbox)
        self.layout().addStretch()
        self.layout().addWidget(self.helplabel)
        self.layout().addWidget(self.lineedit)
        self.layout().setContentsMargins(70, 5, 20, 0)

    def valuechangedEvent(self, text: str):
        self.valueChanged.emit(text)

    def setPlaceholderText(self, text: str):
        self.lineedit.setPlaceholderText(text)
        self.oldtext = text

    def setText(self, text: str):
        self.lineedit.setText(text)

    def stateChangedEvent(self, v: bool):
        self.lineedit.setEnabled(self.checkbox.isChecked())
        if not self.checkbox.isChecked():
            self.lineedit.setEnabled(False)
            self.oldtext = self.lineedit.placeholderText()
            self.lineedit.setToolTip(_("<b>{0}</b> needs to be enabled to change this setting").format(self.checkbox.text()))
            self.lineedit.setPlaceholderText(_("<b>{0}</b> needs to be enabled to change this setting").format(self.checkbox.text()).replace("<b>", "\"").replace("</b>", "\""))
            self.stateChanged.emit(v)
        else:
            self.stateChanged.emit(v)
            self.lineedit.setEnabled(True)
            self.lineedit.setToolTip("")
            self.lineedit.setPlaceholderText(self.oldtext)
            self.valueChanged.emit(self.lineedit.text())


class SectionCheckBoxDirPicker(SectionCheckBox):
    stateChanged = Signal(bool)
    valueChanged = Signal(str)
    defaultText: str
    
    def __init__(self, text: str, parent=None, helpLabel: str = "", smallerMargins: bool = False):
        super().__init__(text=text, parent=parent)
        self.defaultText = _("Select")
        self.setAttribute(Qt.WA_StyledBackground)
        self.pushButton = QPushButton(self)
        self.oldtext = ""
        self.pushButton.setObjectName("")
        self.pushButton.clicked.connect(self.showDialog)
        self.checkbox.stateChanged.connect(self.stateChangedEvent)
        self.helplabel = QLabel(helpLabel, self)
        self.helplabel.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        self.helplabel.setOpenExternalLinks(True)
        self.stateChangedEvent(self.checkbox.isChecked())
        self.checkbox.move((70), 10)
        self.checkbox.setFixedHeight(30)

        self.setLayout(QHBoxLayout())
        self.layout().addWidget(self.checkbox)
        self.layout().addStretch()
        self.layout().addWidget(self.helplabel)
        self.layout().addWidget(self.pushButton)
        if smallerMargins:
            self.layout().setContentsMargins(50, 2, 10, 0)
            self.setFixedHeight(40)
            self.pushButton.setFixedWidth(400)
        else:
            self.layout().setContentsMargins(70, 5, 20, 0)
            self.setFixedHeight(50)
            self.pushButton.setFixedWidth(450)
        
    def currentValue(self) -> str:
        if self.pushButton.text() != self.defaultText:
            return self.pushButton.text()
        return ""
    
    def setValue(self, value: str) -> None:
        self.setText(value)
        
    def showDialog(self):
        folder = QFileDialog.getExistingDirectory(self, _("Select a folder"), os.path.expanduser("~"))
        if folder:
            self.valuechangedEvent(folder)

    def valuechangedEvent(self, text: str):
        self.setText(text)
        self.valueChanged.emit(text)

    def setPlaceholderText(self, text: str):
        self.pushButton.setText(text)
        self.oldtext = text
        
    def setDefaultText(self, text: str):
        self.defaultText = text

    def setText(self, text: str):
        if not self.checkbox.isChecked():
            self.pushButton.setText(_("<b>{0}</b> needs to be enabled to change this setting").format(self.checkbox.text()).replace("<b>", "\"").replace("</b>", "\""))
        elif text:
            self.pushButton.setText(text)
        else:
            self.pushButton.setText(self.defaultText)

    def stateChangedEvent(self, v: bool):
        self.pushButton.setEnabled(self.checkbox.isChecked())
        if not self.checkbox.isChecked():
            self.pushButton.setEnabled(False)
            self.oldtext = self.pushButton.text()
            self.pushButton.setToolTip(_("<b>{0}</b> needs to be enabled to change this setting").format(self.checkbox.text()))
            self.pushButton.setText(_("<b>{0}</b> needs to be enabled to change this setting").format(self.checkbox.text()).replace("<b>", "\"").replace("</b>", "\""))
            self.stateChanged.emit(v)
        else:
            self.stateChanged.emit(v)
            self.pushButton.setEnabled(True)
            self.pushButton.setToolTip("")
            if not self.oldtext:
                self.oldtext = self.defaultText
            self.pushButton.setText(self.oldtext)
            self.valueChanged.emit(self.pushButton.text())
