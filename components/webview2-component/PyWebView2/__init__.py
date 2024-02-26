from PySide6.QtCore import Qt, Signal, QWindow
from PySide6.QtWidgets import QApplication, QHBoxLayout, QLabel, QLineEdit, QVBoxLayout, QWidget
from PySide6.QtGui import QIcon
import sys
import clr
import os
import traceback


def get_base_path():
    if hasattr(sys, 'frozen'):
        return sys._MEIPASS
    return os.path.dirname(sys.argv[0])


BASE_PATH = get_base_path().replace("\\", "/")
DLL_PATH = os.path.join(BASE_PATH, "ExternalLibraries/PyWebView2/lib/WinFormsWebView.dll")


class WebView2(QWidget):
    call_in_main = Signal(object)
    location_changed = Signal(str)
    navigation_started = Signal(str)
    navigation_completed = Signal()
    webview_initialized = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.init_ui()
    
    def init_ui(self):
        self.call_in_main.connect(lambda f: f())
        self.custom_layout = QHBoxLayout(self)
        try:
            clr.AddReference(DLL_PATH)
            import WinFormsWebView
            
            self.webview = WinFormsWebView.Form1(contextMenuEnabled=False)
            self.hwnd = self.webview.getHWND()
            window = QWindow.fromWinId(self.hwnd)
            window.setFlags(Qt.WindowType.CustomizeWindowHint)
            self.webview_widget = QWidget.createWindowContainer(window, self)
            self.webview_widget.setFocusPolicy(Qt.NoFocus)
            self.webview.uncoverWindow()
            
        except Exception as e:
            self.handle_webview_loading_error(e)
        
        self.custom_layout.addWidget(self.webview_widget)
        self.set_layout_properties()

    def handle_webview_loading_error(self, e):
        print(f"🔴 Could not load WebView due to {type(e).__name__}: {e}")
        traceback_str = f"Something, somewhere, went terribly wrong.\nError details:\n\n{traceback.format_exc()}"
        self.webview_widget = QLabel(traceback_str, self)
        self.webview_widget.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self.webview_widget.setContentsMargins(200, 0, 0, 0)

    def set_layout_properties(self):
        self.custom_layout.setContentsMargins(0, 0, 0, 0)
        self.setMouseTracking(True)

    # Add the rest of the class' methods here... with similar refactorings applied

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = WebView2()
    window.resize(800, 600)
    window.show()
    sys.exit(app.exec())
