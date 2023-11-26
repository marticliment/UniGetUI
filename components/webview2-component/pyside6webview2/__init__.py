from typing import Optional
from PySide6.QtCore import *
import PySide6.QtCore
from PySide6.QtGui import *
import PySide6.QtGui
from PySide6.QtWidgets import *
import os
import sys
import win32mica
import PySide6.QtWidgets
import subprocess
import threading
import time
import re
import time


class WebView2(QWidget):
    hWnd: int = 0
    callInMain = Signal(object)
    locationChanged = Signal(str)
    navigationStarted = Signal(str)
    navigationCompleted = Signal()
    webViewInitialized = Signal()
    __webview_widget: QWidget = None

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.callInMain.connect(lambda f: f())
        self.CustomLayout = QHBoxLayout()
        self.setLayout(self.CustomLayout)
        
        import clr
        clr.AddReference(r"C:\SomePrograms\WingetUI-Store\wingetui\ExternalLibraries\pyside6webview2\Component\WinFormsWebView.dll")
        import WinFormsWebView        

        self.webview = WinFormsWebView.Form1()
        hWnd = self.webview.getHWND()
        window = QWindow.fromWinId(hWnd)
        window.setFlags(Qt.WindowType.CustomizeWindowHint)
        self.__webview_widget = QWidget.createWindowContainer(window)
        self.__webview_widget.setFocusPolicy(Qt.FocusPolicy.NoFocus)
        self.CustomLayout.addWidget(self.__webview_widget)
        self.CustomLayout.setContentsMargins(0, 0, 0, 0)
        self.CustomLayout.addWidget(self.__webview_widget)
        self.setMouseTracking(True)
        self.webview.uncoverWindow()


    def setLocation(self, url: str):
        """
        Navigate to the given URL
        """
        print(url)
        self.webview.navigateTo(url)

    def reload(self):
        """
        Reload the current browser location
        """
        self.webview.reload()

    def stop(self):
        """
        Abort webpage loading
        """
        self.webview.stop()
    
    def navigateToString(self, string: str):
        """
        Set the passed string as the WebView HTML content
        """
        self.webview.navigateToString(string)

    def getUrl(self) -> str:
        """
        Get the current URL
        """
        return self.webview.getUrl()


if __name__ == "__main__":
    a = QApplication(sys.argv)

    win = QWidget()
    win.resize(800, 480)
    win32mica.ApplyMica(win.winId(), True)
    wv = WebView2()
    layout = QVBoxLayout()
    edit = QLineEdit()

    edit.editingFinished.connect(lambda: wv.setLocation(edit.text()))
    wv.locationChanged.connect(lambda l: edit.setText(l))
    wv.webViewInitialized.connect(
        lambda: wv.setLocation("https://www.google.com"))
    wv.navigationStarted.connect(lambda l: (edit.setText(l), edit.setEnabled(False)))
    wv.navigationCompleted.connect(lambda: (edit.setEnabled(True)))
    layout.addWidget(edit)
    layout.addWidget(wv)
    win.setLayout(layout)
    win.setWindowTitle("Embed Qt")
    win.show()
    sys.exit(a.exec())
