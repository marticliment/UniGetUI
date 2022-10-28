from ast import Attribute
import shutil
import winreg
import io
from threading import Thread
import sys, time, subprocess, os, json, locale
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from win32mica import ApplyMica, MICAMODE
from urllib.request import urlopen
from versions import *
from languages import *

import globals



old_stdout = sys.stdout
old_stderr = sys.stderr
buffer = io.StringIO()
errbuffer = io.StringIO()
settingsCache = {}
installersWidget = None
updatesAvailable = False

missingTranslationList = []

if getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS'):
    sys.stdout = buffer = io.StringIO()
    sys.stderr = errbuffer = io.StringIO()

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

if not os.path.isdir(os.path.join(os.path.expanduser("~"), ".wingetui")):
    try:
        os.makedirs(os.path.join(os.path.expanduser("~"), ".wingetui"))
    except:
        pass

def cprint(*args) -> None:
    print(*args, file=old_stdout)

def report(exception) -> None: # Exception reporter
    import traceback
    for line in traceback.format_exception(*sys.exc_info()):
        print("🔴 "+line)
        cprint("🔴 "+line)
    print(f"🔴 Note this traceback was caught by reporter and has been added to the log ({exception})")

def _(s): # Translate function
    global lang
    try:
        t = lang[s]
        return ("🟢"+t+"🟢" if debugLang else t) if t else f"🟡{s}🟡" if debugLang else eng_(s)
    except KeyError:
        if debugLang: print(s)
        if not s in missingTranslationList:
            missingTranslationList.append(s)
        return f"🔴{eng_(s)}🔴" if debugLang else eng_(s)

def eng_(s): # English translate function
    try:
        t = englang[s]
        return t if t else s
    except KeyError:
        if debugLang:
            print(s)
        return s

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

sudoPath = os.path.join(os.path.join(realpath, "sudo"), "gsudo.exe") if not getSettings("UseUserGSudo") else shutil.which("gsudo")
try:
    sudoLocation = os.path.dirname(sudoPath)
except TypeError as e:
    report(e)
    sudoLocation = os.path.expanduser("~")
print(sudoPath)
print(sudoLocation)

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

def isDark() -> bool:
    prefs = getSettingsValue("PreferredTheme")
    match prefs:
        case "dark":
            return True
        case "light":
            return False      
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
    print("🟢 checkQueue Thread started!")
    while True:
        if(globals.current_program == ""):
            try:
                globals.current_program = globals.pending_programs[0]
                print(f"🔵 Current program set to {globals.current_program}")
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
    print(f"🟢 winget installer assistant thread started for process {p}")
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
        self.label.setAttribute(Qt.WA_TransparentForMouseEvents)
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
        self.label.setText("")
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
        self.setClearButtonEnabled(True)

    def contextMenuEvent(self, arg__1: QContextMenuEvent) -> None:
        m = self.createStandardContextMenu()
        m.setContentsMargins(0, 0, 0, 0)
        ApplyMenuBlur(m.winId(), m)
        m.exec(arg__1.globalPos())

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
    def __init__(self, showHideArrow: QWidget = None, parent = None) -> None:
        super().__init__(parent)
        l = QVBoxLayout()
        self.showHideArrow = showHideArrow
        l.setContentsMargins(5, 5, 5, 5)
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
        self.itemCount = 0
        self.rss()

    def rss(self):
        if self.w.sizeHint().height() >= self.maxHeight:
            self.setFixedHeight(self.maxHeight)
        else:
            self.setFixedHeight(self.w.sizeHint().height()+20 if self.w.sizeHint().height() > 0 else 4)

    def removeItem(self, item: QWidget):
        self.vlayout.removeWidget(item)
        self.rss()
        self.itemCount = self.vlayout.count()
        if self.itemCount <= 0:
            globals.trayIcon.setIcon(QIcon(getMedia("greyicon"))) 
            self.showHideArrow.hide()

    def addItem(self, item: QWidget):
        self.vlayout.addWidget(item)
        self.itemCount = self.vlayout.count()
        self.showHideArrow.show()
        globals.trayIcon.setIcon(QIcon(getMedia("icon")))

class TreeWidgetItemWithQAction(QTreeWidgetItem):
    itemAction: QAction = QAction
    def __init__(self):
        super().__init__()

    def setAction(self, action: QAction):
        self.itemAction = action

    def action(self) -> QAction:
        return self.itemAction

    def setHidden(self, hide: bool) -> None:
        if self.itemAction != QAction:
            self.itemAction.setVisible(not hide)
        return super().setHidden(hide)
    
    def setText(self, column: int, text: str) -> None:
        self.setToolTip(column, text)
        return super().setText(column, text)

    def treeWidget(self) -> TreeWidget:
        return super().treeWidget()

class ErrorMessage(QWidget):
    showerr = Signal(dict, bool)
    fHeight = 100
    callInMain = Signal(object)
    def __init__(self, parent):
        super().__init__(parent)
        self.showerr.connect(self.em)
        self.callInMain.connect(lambda f: f())
        self.setWindowFlag(Qt.Window)
        self.setObjectName("micawin")
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
        self.moreInfoButton = QPushButton(_("Show details"))
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
        self.moreInfoButton.setText(_("Hide details") if self.moreInfoTextArea.isVisible() else _("Show details"))
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


class QLinkLabel(QLabel):
    def __init__(self, text: str = "", stylesheet: str = ""):
        super().__init__(text)
        self.setStyleSheet(stylesheet)
        self.setTextFormat(Qt.RichText)
        self.setTextInteractionFlags(Qt.TextBrowserInteraction)
        self.setWordWrap(True)
        self.setOpenExternalLinks(True)
        self.setContextMenuPolicy(Qt.CustomContextMenu)
        self.customContextMenuRequested.connect(self.showmenu)
        self.lineedit = QLineEdit(self)
        self.lineedit.hide()
        self.lineedit.setReadOnly(True)

    def setText(self, text: str) -> None:
        super().setText(text)

    def showmenu(self, pos: QPoint) -> None:
        self.lineedit.setText(self.selectedText())
        self.lineedit.selectAll()
        c = QLineEdit.createStandardContextMenu(self.lineedit)
        ApplyMenuBlur(c.winId().__int__(), c)
        c.exec(QCursor.pos())

class QAnnouncements(QLabel):
    callInMain = Signal(object)

    def __init__(self):
        super().__init__()
        self.area = QScrollArea()
        self.setMaximumWidth(self.getPx(1000))
        self.callInMain.connect(lambda f: f())
        self.setFixedHeight(self.getPx(110))
        self.setAlignment(Qt.AlignHCenter | Qt.AlignVCenter)
        self.setStyleSheet(f"#subtitleLabel{{border-bottom-left-radius: {self.getPx(4)}px;border-bottom-right-radius: {self.getPx(4)}px;border-bottom: {self.getPx(1)}px;font-size: 12pt;}}*{{padding: 3px;}}")
        self.setTtext("Fetching latest announcement, please wait...")
        layout = QHBoxLayout()
        layout.setContentsMargins(0, 0, 0, 0)
        self.setContentsMargins(0, 0, 0, 0)
        layout.setSpacing(0)
        self.pictureLabel = QLabel()
        self.pictureLabel.setContentsMargins(0, 0, 0, 0)
        self.pictureLabel.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        self.textLabel = QLabel()
        self.textLabel.setOpenExternalLinks(True)
        self.textLabel.setContentsMargins(self.getPx(10), 0, self.getPx(10), 0)
        self.textLabel.setAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        layout.addStretch()
        layout.addWidget(self.textLabel, stretch=0)
        layout.addWidget(self.pictureLabel, stretch=0)
        layout.addStretch()
        self.w = QWidget()
        self.w.setObjectName("backgroundWindow")
        self.w.setLayout(layout)
        self.pictureLabel.setText("Loading media...")
        self.w.setContentsMargins(0, 0, 0, 0)
        self.area.setWidget(self.w)
        l = QVBoxLayout()
        l.setSpacing(0)
        l.setContentsMargins(0, self.getPx(5), 0, self.getPx(5))
        l.addWidget(self.area, stretch=1)
        self.area.setWidgetResizable(True)
        self.area.setContentsMargins(0, 0, 0, 0)
        self.area.setObjectName("backgroundWindow")
        self.area.setStyleSheet("border: 0px solid black; padding: 0px; margin: 0px;")
        self.area.setFrameShape(QFrame.NoFrame)
        self.area.setHorizontalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.area.setVerticalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.pictureLabel.setFixedHeight(self.area.height())
        self.textLabel.setFixedHeight(self.area.height())
        self.setLayout(l)



    def loadAnnouncements(self, useHttps: bool = True):
        try:
            response = urlopen(f"http{'s' if useHttps else ''}://www.somepythonthings.tk/resources/wingetui.announcement")
            print("🔵 Announcement URL:", response.url)
            response = response.read().decode("utf8")
            self.callInMain.emit(lambda: self.setTtext(""))
            announcement_body = response.split("////")[0].strip().replace("http://", "ignore:").replace("https://", "ignoreSecure:").replace("linkId", "http://somepythonthings.tk/redirect/").replace("linkColor", f"rgb({getColors()[2 if isDark() else 4]})")
            self.callInMain.emit(lambda: self.textLabel.setText(announcement_body))
            announcement_image_url = response.split("////")[1].strip()
            try:
                response = urlopen(announcement_image_url)
                print("🔵 Image URL:", response.url)
                response = response.read()
                self.file =  open(os.path.join(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui")), "announcement.png"), "wb")
                self.file.write(response)
                self.callInMain.emit(lambda: self.pictureLabel.setText(""))
                self.file.close()
                h = self.area.height()
                self.callInMain.emit(lambda: self.pictureLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.textLabel.setFixedHeight(h))
                self.callInMain.emit(lambda: self.pictureLabel.setPixmap(QPixmap(self.file.name).scaledToHeight(h-self.getPx(8), Qt.SmoothTransformation)))
            except Exception as ex:
                s = "Couldn't load the announcement image"+"\n\n"+str(ex)
                self.callInMain.emit(lambda: self.pictureLabel.setText(s))
                print("🟠 Unable to retrieve announcement image")
                print(ex)
        except Exception as e:
            if useHttps:
                self.loadAnnouncements(useHttps=False)
            else:
                s = "Couldn't load the announcements. Please try again later"+"\n\n"+str(e)
                self.callInMain.emit(lambda: self.setTtext(s))
                print("🟠 Unable to retrieve latest announcement")
                print(e)

    def showEvent(self, a0: QShowEvent) -> None:
        return super().showEvent(a0)

    def getPx(self, i: int) -> int:
        return i

    def setTtext(self, a0: str) -> None:
        return super().setText(a0)

    def setText(self, a: str) -> None:
        raise Exception("This member should not be used under any circumstances")

class PushButtonWithAction(QPushButton):
    action: QAction = None
    def __init__(self, text: str = ""):
        super().__init__(text)
        self.action = QAction(text, self)
        self.action.triggered.connect(self.click)


class CustomComboBox(QComboBox):
    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self.setItemDelegate(QStyledItemDelegate(self))

    def showEvent(self, event: QShowEvent) -> None:
        v = self.view().window()
        ApplyMenuBlur(v.winId(), v)
        return super().showEvent(event)

    def dg(self):
        pass

class TenPxSpacer(QWidget):
    def __init__(self) -> None:
        super().__init__()
        self.setFixedWidth(10)

def foregroundWindowThread():
    """
    This thread will periodically get the window focused by the user every 10 secs, so the tray icon can monitor wether the app should be shown or not.
    """
    import win32gui
    while True:
        fw = win32gui.GetForegroundWindow()
        time.sleep(2)
        globals.lastFocusedWindow = fw
        time.sleep(8)

class CustomScrollBar(QScrollBar):
    def __init__(self):
        super().__init__()
        self.rangeChanged.connect(self.showHideIfNeeded)

    def showHideIfNeeded(self, min: int, max: int):
        self.setVisible(min != max)


class ThemeSignal(QObject):
    signal = Signal()

    def __init__(self):
        super().__init__()

def themeThread(ts):
    oldResult = isDark()
    while True:
        if oldResult != isDark():
            oldResult = isDark()            
            while globals.mainWindow.isVisible():
                cprint("🟡 MainWindow is visible, can't reload!")
                time.sleep(1)
            ts.signal.emit()
        time.sleep(1)


ts = ThemeSignal()
ts.signal.connect(lambda: globals.app.reloadWindow())

#Thread(target=themeThread, args=(ts,), daemon=True, name="UI Theme thread").start()
Thread(target=foregroundWindowThread, daemon=True, name="Tools: get foreground window").start()

if getSettingsValue("PreferredLanguage") == "":
    setSettingsValue("PreferredLanguage", "default")

def loadLangFile(file: str, bundled: bool = False) -> dict:
    try:
        path = os.path.join(os.path.expanduser("~"), ".elevenclock/lang/"+file)
        if not os.path.exists(path) or getSettings("DisableLangAutoUpdater") or bundled or True:
            #print(f"🟡 Using bundled lang file (forced={bundled})")
            path = getPath("../lang/"+file)
        #else:
        #    print("🟢 Using cached lang file")
        with open(path, "r", encoding='utf-8') as file:
            return json.load(file)
    except Exception as e:
        report(e)
        return {}

t0 = time.time()

langName = getSettingsValue("PreferredLanguage")
try:
    if (langName == "default"):
        langName = locale.getdefaultlocale()[0]
    langNames = [langName, langName[0:2]]
    langFound = False
    for ln in langNames:
        if (ln in languages):
            lang = loadLangFile(languages[ln]) | {"locale": ln}
            langFound = True
            #if not getSettings("DisableLangAutoUpdater"):
            #    Thread(target=updateLangFile, args=(languages[ln],), name=f"DAEMON: Update lang_{ln}.json").start()
            break
    if (langFound == False):
        raise Exception(f"Value not found for {langNames}")
except Exception as e:
    report(e)
    lang = loadLangFile(languages["en"]) | {"locale": "en"}
    print("🔴 Unknown language")

langName = lang['locale']

try:
    englang = loadLangFile(languages["en"], bundled=True) | {"locale": "en"}
except Exception as e:
    report(e)
    englang = {"locale": "en"}

print(f"It took {time.time()-t0} to load all language files")
            

if __name__ == "__main__":
    import __init__
