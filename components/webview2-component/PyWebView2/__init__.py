from PySide6.QtCore import Qt, Signal, QWindow
from PySide6.QtWidgets import QApplication, QHBoxLayout, QLabel, QLineEdit, QVBoxLayout, QWidget
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
    navigation_completed = Signal(bool)
    webview_initialized = Signal()
    webview_widget = None

    def __init__(self, parent=None):
        super().__init__(parent)
        self.call_in_main.connect(lambda f: f())
        self.custom_layout = QHBoxLayout()
        self.setLayout(self.custom_layout)

        try:
            clr.AddReference(DLL_PATH)
            import WinFormsWebView
            
            self.webview = WinFormsWebView.Form1(contextMenuEnabled=False)
            self.hwnd = self.webview.getHWND()
            window = QWindow.fromWinId(self.hwnd)
            window.setFlags(Qt.WindowType.CustomizeWindowHint)
            self.webview_widget = QWidget.createWindowContainer(window)
            self.webview_widget.setFocusPolicy(Qt.FocusPolicy.NoFocus)
            self.webview.uncoverWindow()
            
        except Exception as e:
            print(f"🔴 Could not load WebView due to {type(e).__name__}: {e}")
            traceback_str = f"Something, somewhere, went terribly wrong.\nError details:\n\n{traceback.format_exc()}"
            self.webview_widget = QLabel(traceback_str)
            self.webview_widget.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
            self.webview_widget.setContentsMargins(200, 0, 0, 0)

        self.custom_layout.addWidget(self.webview_widget)
        self.custom_layout.setContentsMargins(0, 0, 0, 0)
        self.setMouseTracking(True)

        # Events setup for WebView2
        self.webview.navigationStarting += self.on_navigation_starting
        self.webview.navigationCompleted += self.on_navigation_completed
        self.webview.documentTitleChanged += self.on_document_title_changed
        self.webview_initialized.emit()
    
    # Handlers for the webview events
    def on_navigation_starting(self, sender, args):
        url = args.uri
        self.navigation_started.emit(url)

    def on_navigation_completed(self, sender, args):
        self.navigation_completed.emit(args.isSuccess)
        if args.isSuccess:
            self.location_changed.emit(self.getUrl())

    def on_document_title_changed(self, sender, args):
        self.setWindowTitle(sender.documentTitle)

    # Implement the rest of the necessary class methods following the proper Python conventions
    
# Example usage: Uncomment the following code to run the application.
if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = QWidget()
    window.resize(800, 480)
    web_view = WebView2()
    layout = QVBoxLayout()
    edit = QLineEdit()

    edit.editingFinished.connect(lambda: web_view.setLocation(edit.text()))
    web_view.location_changed.connect(lambda l: edit.setText(l))
    web_view.navigation_started.connect(lambda l: (edit.setText(l), edit.setEnabled(False)))
    web_view.navigation_completed.connect(lambda: (edit.setEnabled(True)))
    layout.addWidget(edit)
    layout.addWidget(web_view)
    window.setLayout(layout)
    window.setWindowTitle("Embed Qt")
    window.show()
    sys.exit(app.exec())
