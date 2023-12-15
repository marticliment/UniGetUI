if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module 
    print("redirecting...")
    import subprocess, os, sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *

from wingetui.Core.Tools import *
from wingetui.Core.Tools import _

import wingetui.Core.Globals as Globals


def nativeWindowsShare(text: str, url: str, window: QWidget = None) -> int:
    coordinates = ""
    if window:
        coordinates = f"{window.mapToGlobal(QPoint(0, 0)).x()},{window.mapToGlobal(QPoint(0, 0)).y()},{window.width()},{window.height()}"
    clr.AddReference(SHARE_DLL_PATH)
    import WingetUIShareComponent
    if window and window.window().winId():
        print("🔵 Starting hWnd native sharing")
        WingetUIShareComponent.Form1(window.window().winId(), text, url.replace("^&", "&"))
    else:
        print("🟡 Starting fallback wrapper window sharing")
        WingetUIShareComponent.Form1(["", text, url, coordinates])


def update_tray_icon():
    if Globals.tray_is_installing:
        Globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_blue")))
        Globals.trayIcon.setToolTip(f"{_('Operation in progress')} - WingetUI")
    elif Globals.tray_is_error:
        Globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_orange")))
        Globals.trayIcon.setToolTip(f"{_('Attention required')} - WingetUI")
    elif Globals.tray_is_needs_restart:
        Globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_turquoise")))
        Globals.trayIcon.setToolTip(f"{_('Restart required')} - WingetUI")
    elif Globals.tray_is_available_updates:
        try:
            if Globals.updates.availableUpdates == 1:
                trayIconToolTip = _(
                    "WingetUI - 1 update is available").replace("WingetUI - ", "")
            else:
                trayIconToolTip = _("WingetUI - {0} updates are available").format(
                    Globals.updates.availableUpdates).replace("WingetUI - ", "")
        except Exception as e:
            report(e)
            trayIconToolTip = _("Updates available!").replace('"', '')
        Globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_green")))
        Globals.trayIcon.setToolTip(f"{trayIconToolTip} - WingetUI")
    else:
        Globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_empty")))
        Globals.trayIcon.setToolTip(
            f"{_('WingetUI - Everything is up to date').replace('WingetUI - ', '')} - WingetUI")


def ApplyMenuBlur(hwnd: int, window: QWidget, smallCorners: bool = False, avoidOverrideStyleSheet: bool = False, shadow: bool = True, useTaskbarModeCheck: bool = False):
    hwnd = int(hwnd)
    mode = isDark()
    if not avoidOverrideStyleSheet:
        if window.objectName() == "":
            window.setObjectName("MenuMenuMenuMenu")
        if not isDark():
            window.setStyleSheet(
                f'#{window.objectName()}{{ background-color: {"transparent" if isWin11 else "rgba(255, 255, 255, 30%);border-radius: 0px;" };}}')
        else:
            window.setStyleSheet(
                f'#{window.objectName()}{{ background-color: {"transparent" if isWin11 else "rgba(20, 20, 20, 25%);border-radius: 0px;" };}}')
    if mode:
        try:
            GlobalBlur(hwnd, Acrylic=True, hexColor="#21212140",
                       Dark=True, smallCorners=smallCorners)
        except OverflowError:
            pass
    else:
        try:
            GlobalBlur(hwnd, Acrylic=True, hexColor="#eeeeee40",
                       Dark=True, smallCorners=smallCorners)
        except OverflowError:
            pass


def notify(title: str, text: str, iconpath: str = getMedia("notif_info")) -> None:
    if Globals.ENABLE_WINGETUI_NOTIFICATIONS:
        Globals.trayIcon.showMessage(title, text, QIcon())


def getMaskedIcon(iconName: str) -> QIcon:
    if getMedia(iconName) in Globals.maskedImages.keys():
        return Globals.maskedImages[getMedia(iconName)]
    R, G, B = getColors()[2 if isDark() else 1].split(",")
    R, G, B = (int(R), int(G), int(B))
    base_img = QImage(getMedia(iconName))
    for x in range(base_img.width()):
        for y in range(base_img.height()):
            color = base_img.pixelColor(x, y)
            if color.green() >= 205 and color.red() <= 50 and color.blue() <= 50:
                base_img.setPixelColor(x, y, QColor(R, G, B))
    Globals.maskedImages[getMedia(iconName)] = QIcon(
        QPixmap.fromImage(base_img))
    return Globals.maskedImages[getMedia(iconName)]


def getIcon(iconName: str) -> QIcon:
    iconPath = getMedia(iconName)
    if iconPath in Globals.cachedIcons:
        return Globals.cachedIcons[iconPath]
    else:
        Globals.cachedIcons[iconPath] = QIcon(iconPath)
        return Globals.cachedIcons[iconPath]