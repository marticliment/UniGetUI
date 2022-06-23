import hashlib
import socket
import subprocess
import sys, os, win32mica, glob
import time
from threading import Thread
from tempfile import tempdir
from urllib.request import urlopen
from PySide6 import QtWidgets, QtCore, QtGui
from PySide6.QtGui import *
from PySide6.QtCore import *
from PySide6.QtWidgets import *
import MainWindow, Tools

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])



debugging = True

if hasattr(QtCore.Qt, 'AA_EnableHighDpiScaling'):
    QtWidgets.QApplication.setAttribute(QtCore.Qt.AA_EnableHighDpiScaling, True)
if hasattr(QtCore.Qt, 'AA_UseHighDpiPixmaps'):
    QtWidgets.QApplication.setAttribute(QtCore.Qt.AA_UseHighDpiPixmaps, True)

class MainApplication(QtWidgets.QApplication):
    kill = QtCore.Signal()
    callInMain = QtCore.Signal(object)
    running = True
    def __init__(self):
        try:
            super().__init__(sys.argv)
            self.popup = QWidget()
            self.popup.setFixedSize(QSize(600, 400))
            self.popup.setWindowFlag(Qt.FramelessWindowHint)
            self.popup.setWindowFlag(Qt.WindowStaysOnTopHint)
            self.popup.setLayout(QVBoxLayout())
            self.popup.layout().addStretch()
            titlewidget = QHBoxLayout()
            titlewidget.addStretch()
            icon = QLabel()
            icon.setPixmap(QtGui.QPixmap(realpath+"/icon.png"))
            text = QLabel("WingetUI")
            text.setStyleSheet(f"font-family: \"Segoe UI Variable Display semib\"; color: {'white' if Tools.isDark() else 'black'};font-size: 60px;")
            titlewidget.addWidget(icon)
            titlewidget.addWidget(text)
            titlewidget.addStretch()
            self.popup.layout().addLayout(titlewidget)
            self.popup.layout().addStretch()
            self.loadingText = QLabel("Loading WingetUI...")
            self.loadingText.setStyleSheet(f"font-family: \"Segoe UI Variable Display semib\"; color: {'white' if Tools.isDark() else 'black'};font-size: 12px;")
            self.popup.layout().addWidget(self.loadingText)
            Tools.ApplyMenuBlur(self.popup.winId().__int__(), self.popup)
            self.popup.show()

            print("[        ] Starting main application...")
            os.chdir(os.path.expanduser("~"))
            self.kill.connect(sys.exit)
            self.callInMain.connect(lambda f: f())
            Thread(target=self.checkForRunningInstances).start()
            self.loadingText.setText("Checking for other running instances...")
        except Exception as e:
            print(e)

    def checkForRunningInstances(self):
            print("Scanning for instances...")
            self.nowTime = time.time()
            self.lockFileName = f"WingetUI_{self.nowTime}"
            Tools.setSettings(self.lockFileName, True)
            try:
                for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_*")): # for every lock file
                    timestamp = float(file.replace(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "WingetUI_"), ""))
                    if(timestamp < self.nowTime): # If there's an older instance available: post a block file and wait until it has been deleted in a timeout. If this happens, quit
                        self.callInMain.emit(lambda: self.loadingText.setText(f"Evaluating found instace ID={timestamp}..."))
                        print("Found lock file, reactivating...")
                        Tools.setSettings("RaiseWindow_"+str(timestamp), True)
                        for i in range(8):
                            time.sleep(0.2)
                            self.callInMain.emit(lambda: self.loadingText.setText(f"Evaluating found instace ID={timestamp}... ({int(i/7*100)}%)"))
                            if not Tools.getSettings("RaiseWindow_"+str(timestamp), cache = False):
                                print("Quitting...")
                                #Tools.setSettings(self.lockFileName, False)
                                self.kill.emit()
                                sys.exit(0)
                        print("Reactivation signal ignored: RaiseWindow_"+str(timestamp))
                        Tools.setSettings("RaiseWindow_"+str(timestamp), False)
                        Tools.setSettings("WingetUI_"+str(timestamp), False)
            except Exception as e:
                print(e)
            Thread(target=self.instanceThread, daemon=False).start()
            Thread(target=self.updateIfPossible).start()
            self.callInMain.emit(lambda: self.loadingText.setText(f"Loading UI components..."))
            self.callInMain.emit(self.loadMainUI)

    def loadMainUI(self):
        print("load main UI")
        try:
            self.window = MainWindow.MainWindow()
            self.trayIcon = QtWidgets.QSystemTrayIcon()
            Tools.registerApplication(self)
            self.trayIcon.setIcon(QtGui.QIcon(realpath+"/icon.png"))
            self.trayIcon.setToolTip("WingetUI")
            self.trayIcon.setVisible(True)
            if(not Tools.isDark()):
                self.setStyle("fusion")
                r = win32mica.ApplyMica(self.window.winId(), win32mica.MICAMODE.LIGHT)
                if r != 0x32:
                    pass#self.window.setAttribute(QtCore.Qt.WA_TranslucentBackground)
                self.window.setStyleSheet(lightCSS.replace("mainbg", "transparent" if r == 0x0 else "#ffffff")) 
            else:
                self.setStyle("fusion")
                r = win32mica.ApplyMica(self.window.winId(), win32mica.MICAMODE.DARK)
                if r != 0x32:
                    pass#self.window.setAttribute(QtCore.Qt.WA_TranslucentBackground)
                self.window.setStyleSheet(darkSS.replace("mainbg", "transparent" if r == 0x0 else "#202020"))
            self.loadingText.setText(f"Latest details...")
            self.window.show()
            self.popup.hide()

        except Exception as e:
            if(debugging):
                raise e

    def instanceThread(self):
        while True:
            try:
                for file in glob.glob(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), "RaiseWindow_*")):
                    if Tools.getSettings("RaiseWindow_"+str(self.nowTime), cache = False):
                        print("[   OK   ] Found reactivation lock file...")
                        Tools.setSettings("RaiseWindow_"+str(self.nowTime), False)
                        if not self.window.isMaximized():
                            self.callInMain.emit(self.window.hide)
                            self.callInMain.emit(self.window.showMinimized)
                            self.callInMain.emit(self.window.show)
                            self.callInMain.emit(self.window.showNormal)
                        else:
                            self.callInMain.emit(self.window.hide)
                            self.callInMain.emit(self.window.showMinimized)
                            self.callInMain.emit(self.window.show)
                            self.callInMain.emit(self.window.showMaximized)
                        self.callInMain.emit(self.window.setFocus)
                        self.callInMain.emit(self.window.raise_)
                        self.callInMain.emit(self.window.activateWindow)
            except Exception as e:
                print(e)
            time.sleep(0.5)

    def updateIfPossible(self):
        if not Tools.getSettings("DisableAutoUpdateWingetUI"):
            print("🔵 Starting update check")
            integrityPass = False
            dmname = socket.gethostbyname_ex("versions.somepythonthings.tk")[0]
            if(dmname == dmname): # Check provider IP to prevent exploits
                integrityPass = True
            try:
                response = urlopen("https://versions.somepythonthings.tk/versions/wingetui.ver")
            except Exception as e:
                print(e)
                response = urlopen("http://www.somepythonthings.tk/versions/wingetui.ver")
                integrityPass = True
            print("🔵 Version URL:", response.url)
            response = response.read().decode("utf8")
            new_version_number = response.split("///")[0]
            provided_hash = response.split("///")[1].replace("\n", "").lower()
            if float(new_version_number) > Tools.version:
                print("🟢 Updates found!")
                if(integrityPass):
                    url = "https://github.com/martinet101/WingetUI/releases/latest/download/WingetUI.Installer.exe"
                    filedata = urlopen(url)
                    datatowrite = filedata.read()
                    filename = ""
                    with open(os.path.join(os.path.expanduser("~"), "WingetUI-Updater.exe"), 'wb') as f:
                        f.write(datatowrite)
                        filename = f.name
                    if(hashlib.sha256(datatowrite).hexdigest().lower() == provided_hash):
                        print("🔵 Hash: ", provided_hash)
                        print("🟢 Hash ok, starting update")
                        while self.running:
                            time.sleep(0.1)
                        if not Tools.getSettings("DisableAutoUpdateWingetUI"):
                            subprocess.run('start /B "" "{0}" /silent'.format(filename), shell=True)
                        else:
                            print("🟠 Hash not ok")
                            print("🟠 File hash: ", hashlib.sha256(datatowrite).hexdigest())
                            print("🟠 Provided hash: ", provided_hash)
                    else:
                        print("🟠 Can't verify update server authenticity, aborting")
                        print("🟠 Provided DmName:", dmname)
                        print("🟠 Expected DmNane: 769432b9-3560-4f94-8f90-01c95844d994.id.repl.co")
                else:
                    print("🟢 Updates not found")


colors = Tools.getColors()

darkSS = f"""
* {{
    background-color: transparent;
    color: #eeeeee;
    font-family: "Segoe UI Variable Display semib";
}}
#micawin {{
    background-color: mainbg;
    color: red;
}}
QMenu {{
    border: 1px solid rgb(60, 60, 60);
    padding: 2px;
    outline: 0px;
    color: white;
    background: #262626;
    border-radius: 8px;
}}
QMenu::separator {{
    margin: 2px;
    height: 1px;
    background: rgb(60, 60, 60);
}}
QMenu::icon{{
    padding-left: 10px;
}}
QMenu::item{{
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
    margin: 2px;
}}
QMenu::item:selected{{
    background: rgba(255, 255, 255, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}  
QMenu::item:selected:disabled{{
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView {{
    border: 1px solid rgba(36, 36, 36, 50%);
    padding: 4px;
    outline: 0px;
    padding-right: 0px;
    background-color: #303030;
    border-radius: 8px;
}}
QComboBox QAbstractItemView::item{{
    height: 10px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(255, 255, 255, 6%);
    height: 10px;
    outline: none;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QMessageBox{{
    background-color: #202020;
}}
#greyLabel {{
    color: #bbbbbb;
}}
QPushButton,#FocusLabel {{
    width: 150px;
    background-color:rgba(81, 81, 81, 25%);
    border-radius: 6px;
    border: 1px solid rgba(86, 86, 86, 25%);
    height: 25px;
    font-size: 10pt;
    border-top: 1px solid rgba(99, 99, 99, 25%);
    margin: 0px;
}}
#FlatButton {{
    width: 150px;
    background-color: rgba(255, 255, 255, 1%);
    border-radius: 6px;
    border: 0px solid rgba(255, 255, 255, 1%);
    height: 25px;
    font-size: 10pt;
    border-top: 0px solid rgba(255, 255, 255, 1%);
}}
QPushButton:hover {{
    background-color:rgba(86, 86, 86, 25%);
    border-radius: 6px;
    border: 1px solid rgba(100, 100, 100, 25%);
    height: 30px;
    border-top: 1px solid rgba(107, 107, 107, 25%);
}}
#Headerbutton {{
    width: 150px;
    background-color:rgba(0, 0, 0, 1%);
    border-radius: 6px;
    border: 0px solid transparent;
    height: 25px;
    font-size: 10pt;
    margin: 0px;
}}
#Headerbutton:hover {{
    background-color:rgba(100, 100, 100, 12%);
    border-radius: 8px;
    height: 30px;
}}
#Headerbutton:checked {{
    background-color:rgba(100, 100, 100, 25%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#buttonier {{
    border: 0px solid rgba(100, 100, 100, 25%);
    border-radius: 12px;
}}
#AccentButton{{
    color: #202020;
    font-size: 8pt;
    background-color: rgb({colors[1]});
    border-color: rgb({colors[1]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:hover{{
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:pressed{{
    color: #555555;
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:disabled{{
    color: grey;
    background-color: rgba(50,50,50, 80%);
    border-color: rgb(50, 50, 50);
    border-bottom-color: rgb(50, 50, 50);
}}
QLineEdit {{
    background-color: rgba(81, 81, 81, 25%);
    font-family: "Segoe UI Variable Display";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(86, 86, 86, 25%);
    border-bottom: 2px solid rgb({colors[4]});
}}
QLineEdit:disabled {{
    background-color: rgba(81, 81, 81, 25%);
    font-family: "Segoe UI Variable Display";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(86, 86, 86, 25%);
}}

QScrollBar:vertical {{
    background: transparent;
    border: 1px solid #1f1f1f;
    margin: 3px;
    width: 18px;
    border: none;
    border-radius: 4px;
}}
QScrollBar::handle {{
    margin: 3px;
    min-height: 20px;
    min-width: 20px;
    border-radius: 3px;
    background: #505050;
}}
QScrollBar::handle:hover {{
    margin: 3px;
    border-radius: 3px;
    background: #808080;
}}
QScrollBar::add-line {{
    height: 0;
    subcontrol-position: bottom;
    subcontrol-origin: margin;
}}
QScrollBar::sub-line {{
    height: 0;
    subcontrol-position: top;
    subcontrol-origin: margin;
}}
QScrollBar::up-arrow, QScrollBar::down-arrow {{
    background: none;
}}
QScrollBar::add-page, QScrollBar::sub-page {{
    background: none;
}}
QHeaderView,QAbstractItemView {{
    background-color: #55303030;
    border-radius: 6px;
    border: none;
    padding: 1px;
    height: 25px;
    border: 1px solid #1f1f1f;
    margin-bottom: 5px;
    margin-left: 10px;
    margin-right: 10px;
}}
QHeaderView::section {{
    background-color: transparent;
    border-radius: 6px;
    padding: 4px;
    height: 25px;
    margin: 1px;
}}
QHeaderView::section:first {{
    background-color: transparent;
    border-radius: 6px;
    padding: 4px;
    height: 25px;
    margin: 1px;
    margin-left: -20px;
    padding-left: 30px;
}}
QTreeWidget {{
    show-decoration-selected: 0;
    background-color: transparent;
    padding: 5px;
    border-radius: 6px;
    border: 0px solid #1f1f1f;
}}
QTreeWidget::item {{
    margin-top: 3px;
    margin-bottom: 3px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: #55303030;
    height: 25px;
    border-bottom: 1px solid #1f1f1f;
    border-top: 1px solid #1f1f1f;
}}
QTreeWidget::item:selected {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: #77303030;
    height: 25px;
    border-bottom: 1px solid #303030;
    border-top: 1px solid #303030;
    color: rgb({colors[2]});
}}
QTreeWidget::item:hover {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: #88343434;
    height: 25px;
    border-bottom: 1px solid #303030;
    border-top: 1px solid #303030;
}}
QTreeWidget::item:first {{
    border-top-left-radius: 6px;
    border-bottom-left-radius: 6px;
    border-left: 1px solid #1f1f1f;
}}
QTreeWidget::item:last {{
    border-top-right-radius: 6px;
    border-bottom-right-radius: 6px;
    border-right: 1px solid #1f1f1f;
}}
QTreeWidget::item:first:selected {{
    border-left: 1px solid #303030;
}}
QTreeWidget::item:last:selected {{
    border-right: 1px solid #303030;
}}
QTreeWidget::item:first:hover {{
    border-left: 1px solid #303030;
}}
QTreeWidget::item:last:hover {{
    border-right: 1px solid #303030;
}}
QProgressBar {{
    border-radius: 2px;
    height: 4px;
    border: 0px;
}}
QProgressBar::chunk {{
    background-color: rgb({colors[2]});
    border-radius: 2px;
}}
QCheckBox::indicator{{
    height: 12px;
    width: 12px;
}}
QCheckBox::indicator:unchecked {{
    background-color: rgba(30, 30, 30, 25%);
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:disabled {{
    background-color: rgba(71, 71, 71, 0%);
    color: #bbbbbb;
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:unchecked:hover {{
    background-color: #2a2a2a;
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:checked {{
    border: 1px solid #444444;
    background-color: rgb({colors[1]});
    border-radius: 4px;
}}
QCheckBox::indicator:checked:disabled {{
    border: 1px solid #444444;
    background-color: #303030;
    color: #bbbbbb;
    border-radius: 4px;
}}
QCheckBox::indicator:checked:hover {{
    border: 1px solid #444444;
    background-color: rgb({colors[2]});
    border-radius: 4px;
}}
QComboBox {{
    width: 100px;
    background-color:rgba(81, 81, 81, 25%);
    border-radius: 6px;
    border: 1px solid rgba(86, 86, 86, 25%);
    height: 30px;
    padding-left: 10px;
    border: 1px solid rgba(86, 86, 86, 25%);
}}
QComboBox:disabled {{
    width: 100px;
    background-color: #303030;
    color: #bbbbbb;
    border-radius: 6px;
    border: 0.6px solid #262626;
    height: 25px;
    padding-left: 10px;
}}
QComboBox:hover {{
    background-color:rgba(86, 86, 86, 25%);
    border-radius: 6px;
    border: 1px solidrgba(100, 100, 100, 25%);
    height: 25px;
    padding-left: 10px;
}}
QComboBox::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    background-color: none;
    padding: 5px;
    border-radius: 6px;
    border: none;
    width: 30px;
}}
QComboBox::down-arrow {{
    image: url("{Tools.getMedia("drop-down")}");
    height: 8px;
    width: 8px;
}}
QComboBox::down-arrow:disabled {{
    image: url("{Tools.getMedia("drop-down")}");
    height: 2px;
    width: 2px;
}}
QComboBox QAbstractItemView {{
    border: 1px solid rgba(36, 36, 36, 50%);
    padding: 4px;
    outline: 0px;
    padding-right: 0px;
    background-color: #303030;
    border-radius: 8px;
}}
QComboBox QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(255, 255, 255, 6%);
    height: 30px;
    outline: none;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
#package {{
    margin: 0px;
    padding: 0px;
    background-color: #55303030;
    border-radius: 8px;
    border: 1px solid #1f1f1f;
}}
QListWidget{{
    border: 0px;
    background-color: transparent;
    color: transparent;
}}
QListWidget::item{{
    border: 0px;
    background-color: transparent;
    color: transparent;
}}
"""

lightCSS = f"""
* {{
    background-color: transparent;
    color: #000000;
    font-family: "Segoe UI Variable Display"
}}
#micawin {{
    background-color: mainbg;
    color: red;
}}
QMenu {{
    border: 1px solid rgb(200, 200, 200);
    padding: 2px;
    outline: 0px;
    color: black;
    background: #eeeeee;
    border-radius: 8px;
}}
QMenu::separator {{
    margin: 2px;
    height: 1px;
    background: rgb(200, 200, 200);
}}
QMenu::icon{{
    padding-left: 10px;
}}
QMenu::item{{
    height: 30px;
    border: none;
    background: transparent;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
    margin: 2px;
}}
QMenu::item:selected{{
    background: rgba(0, 0, 0, 10%);
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}  
QMenu::item:selected:disabled{{
    background: transparent;
    height: 30px;
    outline: none;
    border: none;
    padding-right: 10px;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView {{
    border: 1px solid rgba(196, 196, 196, 25%);
    padding: 4px;
    outline: 0px;
    background-color: rgba(255, 255, 255, 10%);
    border-radius: 8px;
}}
QComboBox QAbstractItemView::item{{
    height: 10px;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(0, 0, 0, 6%);
    height: 10px;
    outline: none;
    color: black;
    border: none;
    padding-left: 10px;
    border-radius: 4px;
}}
QMessageBox{{
    background-color: #f9f9f9;
}}
#greyLabel {{
    color: #404040;
}}
QPushButton,#FocusLabel {{
    width: 150px;
    background-color:rgba(255, 255, 255, 55%);
    border: 1px solid rgba(220, 220, 220, 55%);
    border-top: 1px solid rgba(220, 220, 220, 75%);
    border-radius: 6px;
    height: 25px;
    font-size: 10pt;
    margin: 0px;
}}
#FlatButton {{
    width: 150px;
    background-color: rgba(255, 255, 255, 0.1%);
    border-radius: 6px;
    border: 0px solid rgba(255, 255, 255, 1%);
    height: 25px;
    font-size: 10pt;
    border-top: 0px solid rgba(255, 255, 255, 1%);
}}
QPushButton:hover {{
    background-color: rgba(255, 255, 255, 90%);
    border: 1px solid rgba(220, 220, 220, 65%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
    border-radius: 6px;
    height: 30px;
}}
#AccentButton{{
    color: #000000;
    font-size: 8pt;
    background-color: rgb({colors[1]});
    border-color: rgb({colors[1]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:hover{{
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:pressed{{
    color: #000000;
    background-color: rgba({colors[1]}, 80%);
    border-color: rgb({colors[2]});
    border-bottom-color: rgb({colors[2]});
}}
#AccentButton:disabled{{
    color: #000000;
    background-color: rgba(200,200,200, 80%);
    border-color: rgb(200, 200, 200);
    border-bottom-color: rgb(200, 200, 200);
}}
#Headerbutton {{
    width: 150px;
    background-color:rgba(255, 255, 255, 1%);
    border-radius: 6px;
    border: 0px solid transparent;
    height: 25px;
    font-size: 10pt;
    margin: 0px;
}}
#Headerbutton:hover {{
    background-color:rgba(240, 240, 240, 12%);
    border-radius: 8px;
    height: 30px;
}}
#Headerbutton:checked {{
    background-color:rgba(200, 200, 200, 75%);
    border-radius: 8px;
    border: 0px solid rgba(100, 100, 100, 25%);
    height: 30px;
}}
#buttonier {{
    border: 0px solid rgba(100, 100, 100, 25%);
    border-radius: 12px;
}}
QLineEdit {{
    background-color: rgba(255, 255, 255, 25%);
    font-family: "Segoe UI Variable Display";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(86, 86, 86, 25%);
    border-bottom: 2px solid rgb({colors[3]});
}}
QLineEdit:disabled {{
    background-color: rgba(255, 255, 255, 25%);
    font-family: "Segoe UI Variable Display";
    font-size: 9pt;
    width: 300px;
    padding: 5px;
    border-radius: 6px;
    border: 0.6px solid rgba(255, 255, 255, 55%);
}}
QScrollBar:vertical {{
    background: transparent;
    border: 1px solid rgba(240, 240, 240, 55%);
    margin: 3px;
    width: 18px;
    border: none;
    border-radius: 4px;
}}
QScrollBar::handle {{
    margin: 3px;
    min-height: 20px;
    min-width: 20px;
    border-radius: 3px;
    background: #a0a0a0;
}}
QScrollBar::handle:hover {{
    margin: 3px;
    border-radius: 3px;
    background: #808080;
}}
QScrollBar::add-line {{
    height: 0;
    subcontrol-position: bottom;
    subcontrol-origin: margin;
}}
QScrollBar::sub-line {{
    height: 0;
    subcontrol-position: top;
    subcontrol-origin: margin;
}}
QScrollBar::up-arrow, QScrollBar::down-arrow {{
    background: none;
}}
QScrollBar::add-page, QScrollBar::sub-page {{
    background: none;
}}
QHeaderView,QAbstractItemView {{
    background-color: rgba(240, 240, 240, 55%);
    border-radius: 6px;
    border: none;
    padding: 1px;
    height: 25px;
    border: 1px solid rgba(220, 220, 220, 55%);
    margin-bottom: 5px;
    margin-left: 10px;
    margin-right: 10px;
}}
QHeaderView::section {{
    background-color: transparent;
    border-radius: 6px;
    padding: 4px;
    height: 25px;
    margin: 1px;
}}
QHeaderView::section:first {{
    background-color: transparent;
    border-radius: 6px;
    padding: 4px;
    height: 25px;
    margin: 1px;
    margin-left: -20px;
    padding-left: 30px;
}}
QTreeWidget {{
    show-decoration-selected: 0;
    background-color: transparent;
    padding: 5px;
    border-radius: 6px;
    border: 0px solid rgba(240, 240, 240, 55%);
}}
QTreeWidget::item {{
    margin-top: 3px;
    margin-bottom: 3px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    height: 25px;
    background-color:rgba(255, 255, 255, 55%);
    border-top: 1px solid rgba(220, 220, 220, 55%);
    border-bottom: 1px solid rgba(220, 220, 220, 55%);
}}
QTreeWidget::item:selected {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: rgba(240, 240, 240, 90%);
    height: 25px;
    border-bottom: 1px solid rgba(220, 220, 220, 80%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
    color: rgb({colors[5]});
}}
QTreeWidget::item:hover {{
    margin-top: 2px;
    margin-bottom: 2px;
    padding: 0px;
    padding-top: 3px;
    padding-bottom: 3px;
    outline: none;
    background-color: rgba(255, 255, 255, 90%);
    height: 25px;
    border-bottom: 1px solid rgba(220, 220, 220, 80%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
}}
QTreeWidget::item:first {{
    border-top-left-radius: 6px;
    border-bottom-left-radius: 6px;
    border-left: 1px solid rgba(220, 220, 220, 55%);
}}
QTreeWidget::item:last {{
    border-top-right-radius: 6px;
    border-bottom-right-radius: 6px;
    border-right: 1px solid rgba(220, 220, 220, 55%);
}}
QTreeWidget::item:first:selected {{
    border-left: 1px solid rgba(220, 220, 220, 80%);
}}
QTreeWidget::item:last:selected {{
    border-right: 1px solid rgba(220, 220, 220, 80%);
}}
QTreeWidget::item:first:hover {{
    border-left: 1px solid rgba(220, 220, 220, 80%);
}}
QTreeWidget::item:last:hover {{
    border-right: 1px solid rgba(220, 220, 220, 80%);
}}
QProgressBar {{
    border-radius: 2px;
    height: 4px;
    border: 0px;
}}
QProgressBar::chunk {{
    background-color: rgb({colors[3]});
    border-radius: 2px;
}}
QCheckBox::indicator{{
    height: 12px;
    width: 12px;
}}
QCheckBox::indicator:unchecked {{
    background-color: rgba(30, 30, 30, 25%);
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:disabled {{
    background-color: rgba(71, 71, 71, 0%);
    color: #000000;
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:unchecked:hover {{
    background-color: #2a2a2a;
    border: 1px solid #444444;
    border-radius: 4px;
}}
QCheckBox::indicator:checked {{
    border: 1px solid #444444;
    background-color: rgb({colors[1]});
    border-radius: 4px;
}}
QCheckBox::indicator:checked:disabled {{
    border: 1px solid #444444;
    background-color: #303030;
    color: #000000;
    border-radius: 4px;
}}
QCheckBox::indicator:checked:hover {{
    border: 1px solid #444444;
    background-color: rgb({colors[2]});
    border-radius: 4px;
}}
QComboBox {{
    width: 100px;
    background-color:rgba(255, 255, 255, 55%);
    border: 1px solid rgba(220, 220, 220, 55%);
    border-radius: 6px;
    height: 30px;
    padding-left: 10px;
}}
QComboBox:disabled {{
    width: 100px;
    background-color: #bbbbbb;
    color: #000000;
    border-radius: 6px;
    border: 0.6px solid #262626;
    height: 25px;
    padding-left: 10px;
}}
QComboBox:hover {{
    border-radius: 6px;
    height: 25px;
    padding-left: 10px;
    background-color: rgba(255, 255, 255, 90%);
    border: 1px solid rgba(220, 220, 220, 65%);
    border-top: 1px solid rgba(220, 220, 220, 80%);
}}
QComboBox::drop-down {{
    subcontrol-origin: padding;
    subcontrol-position: top right;
    background-color: none;
    padding: 5px;
    border-radius: 6px;
    border: none;
    color: white;
    width: 30px;
}}
QComboBox::down-arrow {{
    image: url("{Tools.getMedia("drop-down")}");
    height: 8px;
    width: 8px;
}}
QComboBox::down-arrow:disabled {{
    image: url("{Tools.getMedia("drop-down")}");
    height: 2px;
    width: 2px;
}}
QComboBox QAbstractItemView {{
    border: 1px solid rgba(36, 36, 36, 50%);
    padding: 4px;
    outline: 0px;
    padding-right: 0px;
    background-color: #ffffff;
    border-radius: 8px;
    color: black;
}}
QComboBox QAbstractItemView::item{{
    height: 30px;
    border: none;
    padding-left: 10px;
    color: black;
    border-radius: 4px;
    background-color: white;
}}
QComboBox QAbstractItemView::item:selected{{
    background: rgba(255, 255, 255, 6%);
    height: 30px;
    outline: none;
    border: none;
    padding-left: 10px;
    background-color: white;
    color: black;
    border-radius: 4px;
}}
#package {{
    margin: 0px;
    padding: 0px;
    border-radius: 8px;
    background-color:rgba(255, 255, 255, 55%);
    border: 1px solid rgba(220, 220, 220, 55%);
}}
"""

a = MainApplication()
a.exec()
a.running = False
sys.exit()