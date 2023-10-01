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
    locationChanged = Signal(str)
    navigationStarted = Signal(str)
    navigationCompleted = Signal()
    webViewInitialized = Signal()
    __webview_console: subprocess.Popen = None
    __webview_widget: QWidget = None
    __webView_is_ready = False
    __returned_values = {}

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.CustomLayout = QHBoxLayout()
        self.setLayout(self.CustomLayout)
        self.__webview_console = subprocess.Popen(executable=os.path.join(os.path.dirname(__file__), "Component", "WinFormsWebView.exe"), args=["about:blank", str(
            self.winId())], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        threading.Thread(target=self.__handle_output, daemon=True).start()
        while self.hWnd == 0:
            time.sleep(0.001)
        window = QWindow.fromWinId(self.hWnd)
        window.setFlags(Qt.WindowType.CustomizeWindowHint)
        self.__webview_widget = QWidget.createWindowContainer(window)
        self.__webview_widget.setFocusPolicy(Qt.FocusPolicy.TabFocus)
        self.CustomLayout.addWidget(self.__webview_widget)
        self.CustomLayout.setContentsMargins(0, 0, 0, 0)
        self.CustomLayout.addWidget(self.__webview_widget)
        self.setMouseTracking(True)

    def setLocation(self, url: str):
        """
        Navigate to the given URL
        """
        FUNCTION = "LOADURL"
        self.__wait_for_webview()
        self.__print_to_component(FUNCTION + "#" + url)
        if self.__get_return_value(FUNCTION) is False:
            print("WebView> An error occurred when calling function " + FUNCTION)

    def reload(self):
        """
        Reload the current browser location
        """
        FUNCTION = "RELOAD"
        self.__wait_for_webview()
        self.__print_to_component(FUNCTION + "#NULL")
        if self.__get_return_value(FUNCTION) is False:
            print("WebView> An error occurred when calling function " + FUNCTION)

    def stop(self):
        """
        Abort webpage loading
        """
        FUNCTION = "ABORT"
        self.__wait_for_webview()
        self.__print_to_component(FUNCTION + "#NULL")
        if self.__get_return_value(FUNCTION) is False:
            print("WebView> An error occurred when calling function " + FUNCTION)

    def navigateToString(self, string: str):
        """
        Set the passed string as the WebView HTML content
        """
        FUNCTION = "NAVIGATETOSTRING"
        self.__wait_for_webview()
        self.__print_to_component(FUNCTION + "#" + string)
        if self.__get_return_value(FUNCTION) is False:
            print("WebView> An error occurred when calling function " + FUNCTION)

    def getUrl(self) -> str:
        """
        Get the current URL
        """
        FUNCTION = "GETURL"
        self.__wait_for_webview()
        self.__print_to_component(FUNCTION + "#NULL")
        return self.__get_return_value(FUNCTION)

    def __delete_webview(self):
        FUNCTION = "EXIT"
        self.__print_to_component(FUNCTION)
        self.__webview_console.kill()
        self.deleteLater()

    def __get_return_value(self, function, timeout: int = 1) -> int | str | bool | None:
        time0 = time.time()
        while function not in self.__returned_values and time0 + 1 > time.time():
            time.sleep(0.001)
        if function in self.__returned_values:
            val = self.__returned_values[function]
            del self.__returned_values[function]
            return val
        else:
            return False

    def __handle_output(self):
        while self.__webview_console.poll() is None:
            line = str(self.__webview_console.stdout.readline().replace(
                b"\r\n", b""), encoding="utf-8", errors="ignore").strip()
            if line:
                if line.startswith("EVENT#"):
                    try:
                        eventName = line.split("#")[1]
                        eventArgs = "#".join(line.split("#")[2:])
                        match eventName:
                            case "ACQUIREDHWND":
                                self.hWnd = int(eventArgs)
                            case "WEBVIEWLOADED":
                                self.__webView_is_ready = True
                                self.__print_to_component("SHOWWINDOW")
                                self.webViewInitialized.emit()
                            case "LOCATIONCHANGED":
                                self.locationChanged.emit(eventArgs)
                            case "NAVIGATIONSTARTED":
                                self.navigationStarted.emit(eventArgs)
                            case "NAVIGATIONCOMPLETED":
                                self.navigationCompleted.emit()
                            case default:
                                print(
                                    "WebViewHandler.eventHandler> Unknown event " + default)
                    except Exception as e:
                        print("WebViewHandler.eventHandler> " + type(e) + ": " + str(e))
                elif line.startswith("RETURN#"):
                    try:
                        functionName = line.split("#")[1]
                        returnType = line.split("#")[2]
                        rawValue = "#".join(line.split("#")[3:])
                        typedValue = None
                        match returnType:
                            case "BOOL":
                                typedValue = True if rawValue == "TRUE" else False
                            case "STRING":
                                typedValue = rawValue
                            case "INT":
                                typedValue = int(rawValue)
                            case "VOID":
                                typedValue = None
                            case default:
                                typedValue = default
                                print(
                                    "WebViewHandler.returnHandler> Unknown data type " + default)
                        self.__returned_values[functionName] = typedValue
                    except Exception as e:
                        print("WebViewHandler.returnHandler> " + type(e) + ": " + str(e))
                else:
                    print("WebView> " + line)

    def __wait_for_webview(self) -> None:
        while not self.__webView_is_ready:
            time.sleep(0.001)

    def __print_to_component(self, string: str) -> None:
        self.__webview_console.stdin.write(
            bytes(string + "\r\n", encoding="utf-8", errors="ignore"))
        self.__webview_console.stdin.flush()

    def closeEvent(self, event: QCloseEvent) -> None:
        self.__delete_webview()
        return super().closeEvent(event)

    def mousePressEvent(self, event: QMouseEvent):
        print("event")
        return super().mousePressEvent(event)


if __name__ == "__main__":
    a = QApplication(sys.argv)

    win = QWidget()
    win.resize(800, 480)
    win32mica.ApplyMica(win.winId(), True)
    wv = QWebView2Widget()
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
