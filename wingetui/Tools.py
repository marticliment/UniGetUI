import winreg
from PySide2 import QtCore
from threading import Thread
import sys, time, subprocess, os
from PySide2.QtCore import *
from PySide2.QtWinExtras import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *
from win32mica import ApplyMica, MICAMODE

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

pending_programs = []
current_program = ""
version = 1.0

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

app = None

def queueProgram(id: str):
    global pending_programs
    pending_programs.append(id)

def removeProgram(id: str):
    global pending_programs, current_program
    try:
        pending_programs.remove(id)
    except ValueError:
        pass
    if(current_program == id):
        current_program = ""

def checkQueue():
    global current_program, pending_programs
    print("[   OK   ] checkQueue Thread started!")
    while True:
        if(current_program == ""):
            try:
                current_program = pending_programs[0]
                print(f"[ THREAD ] Current program set to {current_program}")
            except IndexError:
                pass
        time.sleep(0.2)


def ApplyMenuBlur(hwnd: int, window: QWidget, smallCorners: bool = False, avoidOverrideStyleSheet: bool = False, shadow: bool = True, useTaskbarModeCheck: bool = False):
    hwnd = int(hwnd)
    #window.setAttribute(Qt.WA_TranslucentBackground)
    #window.setAttribute(Qt.WA_NoSystemBackground)

    mode = isDark()
    from blurwindow import GlobalBlur
    if not avoidOverrideStyleSheet:
        window.setStyleSheet("background-color: transparent;")
    if mode:
        GlobalBlur(hwnd, Acrylic=True, hexColor="#21212140", Dark=True, smallCorners=smallCorners)
        if shadow:
            QtWin.extendFrameIntoClientArea(window, -1, -1, -1, -1)
    else:
        GlobalBlur(hwnd, Acrylic=True, hexColor="#eeeeee40", Dark=True, smallCorners=smallCorners)
        if shadow: 
            QtWin.extendFrameIntoClientArea(window, -1, -1, -1, -1)


def getPath(s):
    return os.path.join(realpath, s).replace("\\", "/")

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


def notify(title: str, text: str) -> None:
    app.trayIcon.showMessage(title, text)

def registerApplication(newApp):
    global app
    app = newApp

def genericInstallAssistant(p: subprocess.Popen, closeAndInform: QtCore.Signal, infoSignal: QtCore.Signal, counterSignal: QtCore.Signal) -> None:
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