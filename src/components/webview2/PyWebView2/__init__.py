from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
import sys
import clr
import os
import time
import traceback


if hasattr(sys, 'frozen'):
    BASE_PATH = sys._MEIPASS
else:
    BASE_PATH = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

DLL_PATH = os.path.join(BASE_PATH, "ExternalLibraries/PyWebView2/lib/WinFormsWebView.dll")

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
        
        try:
            clr.AddReference(DLL_PATH)
            import WinFormsWebView

            self.webview = WinFormsWebView.Form1(contextMenuEnabled=False)
            hWnd = self.webview.getHWND()
            window = QWindow.fromWinId(hWnd)
            window.setFlags(Qt.WindowType.CustomizeWindowHint)
            self.__webview_widget = QWidget.createWindowContainer(window)
            self.__webview_widget.setFocusPolicy(Qt.FocusPolicy.NoFocus)
            self.webview.uncoverWindow()

        except Exception as e:
            print("🔴 Could not load WebView due to", str(type(e)) + ":", str(e))
            traceback_str = "Something, somewhere, went terribly wrong.\nError details:\n\n" + traceback.format_exc()
            self.__webview_widget = QLabel()
            self.__webview_widget.setAlignment(Qt.AlignmentFlag.AlignLeft | Qt.AlignmentFlag.AlignVCenter)
            self.__webview_widget.setContentsMargins(200, 0, 0, 0)
            self.__webview_widget.setText(traceback_str)

        self.CustomLayout.addWidget(self.__webview_widget)
        self.CustomLayout.setContentsMargins(0, 0, 0, 0)
        self.setMouseTracking(True)

    def goBack(self):
        """
        Navigate back in the browser history
        """
        self.webview.webView.GoBack()
        
    def goForward(self):
        """
        Navigate forward in the browser history
        """
        self.webview.webView.GoForward()

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


"""if __name__ == "__main__":
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
"""