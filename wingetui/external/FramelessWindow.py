from ctypes import c_int, windll
windll.shcore.SetProcessDpiAwareness(c_int(2))

#
#
# This file comes from https://github.com/mustafaahci/FramelessWindow
#
#

import winreg
from win32mica import ApplyMica, MICAMODE

from ctypes.wintypes import DWORD, LONG, LPCVOID

from win32con import PAN_SERIF_SQUARE, WM_NCCALCSIZE, GWL_STYLE, WM_NCHITTEST, WS_MAXIMIZEBOX, WS_THICKFRAME, \
    WS_CAPTION, HTTOPLEFT, HTBOTTOMRIGHT, HTTOPRIGHT, HTBOTTOMLEFT, \
    HTTOP, HTBOTTOM, HTLEFT, HTRIGHT, HTCAPTION, WS_POPUP, WS_SYSMENU, WS_MINIMIZEBOX


from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from external.blurwindow import ExtendFrameIntoClientArea

def readRegedit(aKey, sKey, default, storage=winreg.HKEY_CURRENT_USER):
    registry = winreg.ConnectRegistry(None, storage)
    reg_keypath = aKey
    try:
        reg_key = winreg.OpenKey(registry, reg_keypath)
    except FileNotFoundError as e:
        return default
    except Exception as e:
        print(e)
        return default

    for i in range(1024):
        try:
            value_name, value, _ = winreg.EnumValue(reg_key, i)
            if value_name == sKey:
                return value
        except OSError as e:
            return default
        except Exception as e:
            print(e)
            return default

def isWindowDark() -> bool:
    return readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)==0


class QFramelessWindow(QMainWindow):
    BORDER_WIDTH = 10

    def __init__(self, parent=None):
        self.updateSize = True
        self.settingsWidget = QWidget()
        super().__init__(parent=parent)
        self.hwnd = self.winId().__int__()
        self.setObjectName("QFramelessWindow")
        self.setWindowFlags(Qt.WindowType.Window | Qt.WindowType.CustomizeWindowHint)
        #window_style = win32gui.GetWindowLong(self.hwnd, GWL_STYLE)
        #win32gui.SetWindowLong(self.hwnd, GWL_STYLE, window_style | WS_POPUP | WS_THICKFRAME | WS_CAPTION | WS_SYSMENU | WS_MAXIMIZEBOX | WS_MINIMIZEBOX)

        ExtendFrameIntoClientArea(self.winId().__int__())

        self.setAutoFillBackground(True)

        # Window Widgets
        self.resize(800, 600)
        self._layout = QVBoxLayout()
        self._layout.setContentsMargins(0, 0, 0, 0)
        self._layout.setSpacing(0)


        # main widget is here
        self.mainWidget = QWidget()
        self.mainWidgetLayout = QVBoxLayout()
        self.mainWidgetLayout.setContentsMargins(0, 0, 0, 0)
        self.mainWidget.setLayout(self.mainWidgetLayout)
        self.mainWidget.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)

        # set background color
        p = self.palette()
        p.setColor(self.backgroundRole(), QColor("#272727"))
        self.setPalette(p)

        self._layout.addWidget(self.mainWidget)
        self.setLayout(self._layout)
    
    def showEvent(self, event) -> None:
        ApplyMica(self.winId(), MICAMODE.DARK if isWindowDark() else MICAMODE.LIGHT)
        return super().showEvent(event)

    def moveEvent(self, event) -> None:
        self.repaint()
        return super().moveEvent(event)

if __name__ == "__main__":
    from ctypes import c_int, windll
    windll.shcore.SetProcessDpiAwareness(c_int(2))
    import __init__