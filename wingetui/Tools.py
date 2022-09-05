from ast import Attribute
import winreg
import io
from threading import Thread
import sys, time, subprocess, os
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from win32mica import ApplyMica, MICAMODE

import globals

old_stdout = sys.stdout
old_stderr = sys.stderr
buffer = io.StringIO()
errbuffer = io.StringIO()
#sys.stdout = buffer = io.StringIO()
#sys.stderr = errbuffer = io.StringIO()

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

sudoPath = os.path.join(os.path.join(realpath, "sudo"), "gsudo.exe")
sudoLocation = os.path.dirname(sudoPath)


settingsCache = {}
version = 1.3
installersWidget = None
updatesAvailable = False

if not os.path.isdir(os.path.join(os.path.expanduser("~"), ".wingetui")):
    try:
        os.makedirs(os.path.join(os.path.expanduser("~"), ".wingetui"))
    except:
        pass

def getSettings(s: str, cache = True):
    global settingsCache
    try:
        try:
            if not cache:
                raise KeyError("Cache disabled")
            return settingsCache[s]
        except KeyError:
            v = os.path.exists(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s))
            settingsCache[s] = v
            return v
    except Exception as e:
        print(e)
        return False

def setSettings(s: str, v: bool):
    global settingsCache
    try:
        settingsCache = {}
        if(v):
            open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "w").close()
        else:
            try:
                os.remove(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s))
            except FileNotFoundError:
                pass
    except Exception as e:
        print(e)

def getSettingsValue(s: str):
    global settingsCache
    try:
        try:
            return settingsCache[s+"Value"]
        except KeyError:
            with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "r") as sf:
                v = sf.read()
                settingsCache[s+"Value"] = v
                return v
    except FileNotFoundError:
        return ""
    except Exception as e:
        print(e)
        return ""

def setSettingsValue(s: str, v: str):
    global settingsCache
    try:
        settingsCache = {}
        with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "w") as sf:
            sf.write(v)
    except Exception as e:
        print(e)



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

def getColors() -> list:
    colors = ['215,226,228', '160,174,183', '101,116,134', '81,92,107', '69,78,94', '41,47,64', '15,18,36', '239,105,80']
    string = readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentPalette", b'\xe9\xd8\xf1\x00\xcb\xb7\xde\x00\x96}\xbd\x00\x82g\xb0\x00gN\x97\x00H4s\x00#\x13K\x00\x88\x17\x98\x00')
    i = 0
    j = 0
    while (i+2)<len(string):
        colors[j] = f"{string[i]},{string[i+1]},{string[i+2]}"
        j += 1
        i += 4
    return colors

def isDark() -> str:
    return readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)==0

if isDark():
    blueColor = f"rgb({getColors()[1]})"
else:
    blueColor = f"rgb({getColors()[4]})"

def queueProgram(id: str):
    globals.pending_programs.append(id)

def removeProgram(id: str):
    try:
       globals.pending_programs.remove(id)
    except ValueError:
        pass
    if(globals.current_program == id):
        globals.current_program = ""

def checkQueue():
    print("[   OK   ] checkQueue Thread started!")
    while True:
        if(globals.current_program == ""):
            try:
                globals.current_program = globals.pending_programs[0]
                print(f"[ THREAD ] Current program set to {globals.current_program}")
            except IndexError:
                pass
        time.sleep(0.2)


def ApplyMenuBlur(hwnd: int, window: QWidget, smallCorners: bool = False, avoidOverrideStyleSheet: bool = False, shadow: bool = True, useTaskbarModeCheck: bool = False):
    hwnd = int(hwnd)
    mode = isDark()
    from blurwindow import GlobalBlur
    if not avoidOverrideStyleSheet:
        window.setStyleSheet("background-color: transparent;")
    if mode:
        GlobalBlur(hwnd, Acrylic=True, hexColor="#21212140", Dark=True, smallCorners=smallCorners)
        if shadow:
            pass
            #QtWin.extendFrameIntoClientArea(window, -1, -1, -1, -1)
    else:
        GlobalBlur(hwnd, Acrylic=True, hexColor="#eeeeee40", Dark=True, smallCorners=smallCorners)
        if shadow: 
            pass
            #QtWin.extendFrameIntoClientArea(window, -1, -1, -1, -1)


def getPath(s):
    return os.path.join(os.path.join(realpath, "resources"), s).replace("\\", "/")

def getIconMode() -> str:
    return "white" if isDark() else "black"

def getMedia(m: str) -> str:
    return getPath(m+"_"+getIconMode()+".png")

def getint(s: str, fallback: int) -> int:
    try:
        return int(s)
    except:
        print("can't parse", s)
        return fallback



Thread(target=checkQueue, daemon=True).start()

class KillableThread(Thread):
    def __init__(self, *args, **keywords): 
        super(KillableThread, self).__init__(*args, **keywords) 
        self.shouldBeRuning = True

    def start(self): 
        self._run = self.run 
        self.run = self.settrace_and_run
        Thread.start(self)

    def settrace_and_run(self): 
        sys.settrace(self.globaltrace) 
        self._run()

    def globaltrace(self, frame, event, arg): 
        return self.localtrace if event == 'call' else None

    def kill(self) -> None:
        self.shouldBeRuning = False
        
    def localtrace(self, frame, event, arg): 
        if not(self.shouldBeRuning) and event == 'line': 
            raise SystemExit()
            print("Killed")
        return self.localtrace 


def notify(title: str, text: str, iconpath: str = getMedia("notif_info")) -> None:
    globals.trayIcon.showMessage(title, text, QIcon(iconpath))


def genericInstallAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"[   OK   ] winget installer assistant thread started for process {p}")
    outputCode = 1
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            output += line+"\n"
            infoSignal.emit(line)
            print(line)
    print(p.returncode)
    closeAndInform.emit(p.returncode, output)


class MessageBox(QMessageBox):
    def __init__(self, parent: object = None) -> None:
        super().__init__(parent)
        ApplyMica(self.winId(), MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
        self.setStyleSheet("QMessageBox{background-color: transparent;}")
        

class TreeWidget(QTreeWidget):
    def __init__(self, emptystr: str = "") -> None:
        super().__init__()
        self.label = QLabel(emptystr, self)
        self.label.setAlignment(Qt.AlignVCenter | Qt.AlignHCenter)
        op=QGraphicsOpacityEffect(self.label)
        op.setOpacity(0.5)
        self.label.setGraphicsEffect(op)
        self.label.setAutoFillBackground(True)
        font = self.label.font()
        font.setBold(True)
        font.setPointSize(20)
        self.label.setFont(font)
        self.label.setFixedWidth(2050)
        self.label.setFixedHeight(50)

    def resizeEvent(self, event: QResizeEvent) -> None:
        self.label.move((self.width()-self.label.width())//2, (self.height()-self.label.height())//2,)
        return super().resizeEvent(event)

    def addTopLevelItem(self, item: QTreeWidgetItem) -> None:
        self.label.hide()
        return super().addTopLevelItem(item)

    def clear(self) -> None:
        self.label.show()
        return super().clear()

class ScrollWidget(QWidget):
    def __init__(self, scroller: QWidget) -> None:
        self.scroller = scroller
        super().__init__()

    def wheelEvent(self, event: QWheelEvent) -> None:
        self.scroller.wheelEvent(event)
        return super().wheelEvent(event)

class CustomLineEdit(QLineEdit):
    def __init__(self, parent = None):
        super().__init__(parent=parent)
        self.textChanged.connect(self.updateTextColor)
        self.updateTextColor(self.text())

    def updateTextColor(self, text: str) -> None:
        if text == "":
            self.startStyleSheet = super().styleSheet()
            super().setStyleSheet(self.startStyleSheet+"color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)

    def setStyleSheet(self, styleSheet: str) -> None:
        if self.text() == "":
            self.startStyleSheet = styleSheet
            super().setStyleSheet(self.startStyleSheet+"color: grey;")
        else:
            super().setStyleSheet(self.startStyleSheet)

class ResizableWidget(QWidget):
    resized = Signal(QResizeEvent)
    def __init__(self, parent = None) -> None:
        super().__init__(parent)
        
    def resizeEvent(self, event: QResizeEvent) -> None:
        self.resized.emit(event)
        return super().resizeEvent(event)


class DynamicScrollArea(QWidget):
    maxHeight = 200
    def __init__(self, parent = None) -> None:
        super().__init__(parent)
        l = QVBoxLayout()
        l.setContentsMargins(0, 0, 0, 0)
        self.scrollArea = QScrollArea()
        self.coushinWidget = QWidget()
        l.addWidget(self.coushinWidget)
        l.addWidget(self.scrollArea)
        self.w = ResizableWidget()
        self.w.resized.connect(self.rss)
        self.vlayout = QVBoxLayout()
        self.vlayout.setContentsMargins(0, 0, 0, 0)
        self.w.setLayout(self.vlayout)
        self.scrollArea.setWidget(self.w)
        self.scrollArea.setFrameShape(QFrame.NoFrame)
        self.scrollArea.setWidgetResizable(True)
        self.setLayout(l)
        self.rss()

    def rss(self):
        if self.w.sizeHint().height() >= self.maxHeight:
            self.setFixedHeight(self.maxHeight)
        else:
            self.setFixedHeight(self.w.sizeHint().height()+10 if self.w.sizeHint().height() > 0 else 4)

    def removeItem(self, item: QWidget):
        self.vlayout.removeWidget(item)
        self.rss()

    def addItem(self, item: QWidget):
        self.vlayout.addWidget(item)

class TreeWidgetItemWithQAction(QTreeWidgetItem):
    itemAction: QAction = QAction
    def __init__(self):
        super().__init__()

    def setAction(self, action: QAction):
        self.itemAction = action

    def action(self) -> QAction:
        return self.itemAction

    def setHidden(self, hide: bool) -> None:
        self.itemAction.setVisible(not hide)
        return super().setHidden(hide)

class ErrorMessage(QWidget):
    showerr = Signal(dict, bool)
    fHeight = 100
    callInMain = Signal(object)
    def __init__(self, parent):
        super().__init__(parent)
        self.showerr.connect(self.em)
        self.callInMain.connect(lambda f: f())
        self.setWindowFlag(Qt.Window)
        self.setObjectName("bg")
        self.setStyleSheet("QWidget#bg{background-color: transparent;}")
        ApplyMica(self.winId().__int__(), MICAMODE.DARK if isDark() else MICAMODE.LIGHT)
        self.hide()
        l = QVBoxLayout()
        self.titleLabel = QLabel()
        self.titleLabel.setStyleSheet("font-size: 20pt;")
        l.addSpacing(10)
        l.addWidget(self.titleLabel)
        l.addSpacing(2)
        self.textLabel = QLabel()
        self.textLabel.setWordWrap(True)
        l.addWidget(self.textLabel)
        l.addSpacing(10)
        l.addStretch()
        self.iconLabel = QLabel()
        self.iconLabel.setFixedSize(64, 64)
        layout = QVBoxLayout()
        hl = QHBoxLayout()
        hl.addWidget(self.iconLabel)
        hl.addLayout(l)
        hl.addSpacing(16)
        layout.addLayout(hl)
        hl = QHBoxLayout()
        self.okButton = QPushButton()
        self.okButton.setFixedHeight(30)
        self.okButton.clicked.connect(self.delete)
        self.moreInfoButton = QPushButton("Show details")
        self.moreInfoButton.setFixedHeight(30)
        self.moreInfoButton.clicked.connect(self.moreInfo)
        #hl.addStretch()
        hl.addWidget(self.moreInfoButton)
        hl.addWidget(self.okButton)
        layout.addLayout(hl)
        self.moreInfoTextArea = QPlainTextEdit()
        self.moreInfoTextArea.setReadOnly(True)
        self.moreInfoTextArea.setVisible(False)
        self.moreInfoTextArea.setMinimumHeight(120)
        layout.addWidget(self.moreInfoTextArea, stretch=1)
        self.setLayout(layout)
        self.setMinimumWidth(320)

    def delete(self):
        self.hide()

    def moreInfo(self):
        spacingAdded = False
        self.moreInfoTextArea.setVisible(not self.moreInfoTextArea.isVisible())
        self.moreInfoButton.setText("Hide details" if self.moreInfoTextArea.isVisible() else "Show details")
        if self.moreInfoTextArea.isVisible():
            # show textedit
            s = self.size()
            s.setHeight(s.height() + self.moreInfoTextArea.height() + (self.layout().spacing() if not spacingAdded else 0))
            spacingAdded = True
            self.resize(s)
            self.setMinimumWidth(450)
            self.setMinimumHeight(self.minimumSizeHint().height())
            self.setMaximumHeight(2048)
        else:
            # Hide textedit
            s = self.size()
            s.setHeight(s.height() - self.moreInfoTextArea.height() - self.layout().spacing())
            self.setMaximumSize(s)
            self.resize(s)
            self.setMaximumSize(2048, 2048)
            self.setMinimumWidth(450)
            self.fHeight = self.minimumSizeHint().height()
            self.setFixedHeight(self.fHeight)
            self.setMinimumHeight(self.fHeight)
            self.setMaximumHeight(self.fHeight+1)

    
    def showErrorMessage(self, data: dict, showNotification = True):
        self.showerr.emit(data, showNotification)

    def em(self, data: dict, showNotification = True):
        errorData = {
            "titlebarTitle": "Window title",
            "mainTitle": "Error message",
            "mainText": "An error occurred",
            "buttonTitle": "Ok",
            "errorDetails": "The details say that there were no details to detail the detailed error",
            "icon": QIcon(getMedia("cancel")),
            "notifTitle": "Error notification",
            "notifText": "An error occurred",
            "notifIcon": QIcon(getMedia("cancel")),
        } | data
        self.setWindowTitle(errorData["titlebarTitle"])
        self.titleLabel.setText(errorData["mainTitle"])
        self.textLabel.setText(errorData["mainText"])
        self.okButton.setText(errorData["buttonTitle"])
        self.iconLabel.setPixmap(QIcon(errorData["icon"]).pixmap(64, 64))
        self.moreInfoTextArea.setPlainText(errorData["errorDetails"])
        self.setMinimumWidth(450)
        self.resize(self.minimumSizeHint())
        wVisible = False
        wExists = False
        if self.parent():
            try:
                if self.parent().window():
                    wExists = True
                    if self.parent().window().isVisible():
                        wVisible = True
                        g: QRect = self.parent().window().geometry()
                        self.move(g.x()+g.width()//2-self.width()//2, g.y()+g.height()//2-self.height()//2)
            except AttributeError:
                print("Parent has no window!")
        if showNotification:
            if not wVisible:
                globals.trayIcon.showMessage(errorData["notifTitle"], errorData["notifText"], errorData["notifIcon"])
        if wExists:
            if wVisible:
                self.show()
                globals.app.beep()
            else:
                def waitNShow():
                    while not self.parent().window().isVisible():
                        time.sleep(0.5)
                    self.callInMain.emit(lambda: (self.show(), globals.app.beep()))
                Thread(target=waitNShow, daemon=True, name="Error message waiting to be shown").start()
        else:
            self.show()
            globals.app.beep()

            

if __name__ == "__main__":
    import __init__