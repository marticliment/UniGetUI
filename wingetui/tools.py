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
    tb = traceback.format_exception(*sys.exc_info())
    try:
        for line in tb:
            print("🔴 "+line)
            cprint("🔴 "+line)
        print(f"🔴 Note this traceback was caught by reporter and has been added to the log ({exception})")
    except UnicodeEncodeError:
        for line in tb:
            print("ERROR "+line)
            cprint("ERROR "+line)
        print(f"ERROR Note this traceback was caught by reporter and has been added to the log ({exception})")

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
        path = os.path.join(os.path.expanduser("~"), ".wingetui/lang/"+file)
        if not os.path.exists(path) or getSettings("DisableLangAutoUpdater") or bundled:
            print(f"🟡 Using bundled lang file (forced={bundled})")
            path = getPath("../lang/"+file)
        else:
            print("🟢 Using cached lang file")
        with open(path, "r", encoding='utf-8') as file:
            return json.load(file)
    except Exception as e:
        report(e)
        return {}


def updateLangFile(file: str):
    global lang
    try:
        try:
            oldlang = open(os.path.join(os.path.expanduser("~"), ".wingetui/lang/"+file), "rb").read()
        except FileNotFoundError:
            oldlang = ""
        newlang = urlopen("https://raw.githubusercontent.com/martinet101/WingetUI/main/wingetui/lang/"+file)
        if newlang.status == 200:
            langdata: bytes = newlang.read()
            if not os.path.isdir(os.path.join(os.path.expanduser("~"), ".wingetui/lang/")):
                os.makedirs(os.path.join(os.path.expanduser("~"), ".wingetui/lang/"))
            if oldlang != langdata:
                print("🟢 Updating outdated language file...")
                with open(os.path.join(os.path.expanduser("~"), ".wingetui/lang/"+file), "wb") as f:
                    f.write(langdata)
                    f.close()
                    lang = loadLangFile(file) | {"locale": lang["locale"] if "locale" in lang.keys() else "en"}
            else:
                print("🔵 Language file up-to-date")
    except Exception as e:
        report(e)

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
            if not getSettings("DisableLangAutoUpdater"):
                Thread(target=updateLangFile, args=(languages[ln],), name=f"DAEMON: Update lang_{ln}.json").start()
            break
    if (langFound == False):
        raise Exception(f"Value not found for {langNames}")
except Exception as e:
    report(e)
    lang = loadLangFile(languages["en"]) | {"locale": "en"}
    print("🔴 Unknown language")

langName = lang['locale']

if "zh" in langName:
    globals.textfont: str = "Microsoft JhengHei UI"
    globals.dispfont: str = "Microsoft JhengHei UI"
    globals.dispfontsemib: str = "Microsoft JhengHei UI"

try:
    englang = loadLangFile(languages["en"], bundled=True) | {"locale": "en"}
except Exception as e:
    report(e)
    englang = {"locale": "en"}

print(f"It took {time.time()-t0} to load all language files")
            

if __name__ == "__main__":
    import __init__
