import io
import json
import locale
import os
import re
import shutil
import subprocess
import sys
import time
import winreg
from datetime import datetime
from pathlib import Path
from threading import Thread
from urllib.request import urlopen

import globals
import clr
from external.blurwindow import GlobalBlur
from lang.languages import *
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *
from data.versions import *

OLD_STDOUT = sys.stdout
OLD_STDERR = sys.stderr
stdout_buffer = io.StringIO()
stderr_buffer = io.StringIO()
MissingTranslationList = []
SYSTEM_THEME_ON_LAUNCH = 0
realpath = 0
blueColor = "blue"

def cprint(*args) -> None:
    print(*args, file=OLD_STDOUT)

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
        if not s in MissingTranslationList:
            MissingTranslationList.append(s)
        return f"🔴{eng_(s)}🔴" if debugLang else eng_(s)

def eng_(s): # English translate function
    try:
        t = englang[s]
        return t if t else s
    except KeyError:
        if debugLang:
            print(s)
        return s

def getSettings(s: str, cache = True) -> bool:
    """
    Returns a boolean value representing if the given setting is enabled or not.
    """
    globals.settingsCache
    try:
        try:
            if not cache:
                raise KeyError("Cache disabled")
            return globals.settingsCache[s]
        except KeyError:
            v = os.path.exists(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s))
            globals.settingsCache[s] = v
            return v
    except Exception as e:
        print(e)
        return False

def setSettings(s: str, v: bool) -> None:
    """
    Sets a boolean value for the given setting
    """
    globals.settingsCache
    try:
        globals.settingsCache = {}
        if(v):
            open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "w").close()
        else:
            try:
                os.remove(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s))
            except FileNotFoundError:
                pass
    except Exception as e:
        print(e)
    if "Notifications" in s:
        globals.ENABLE_WINGETUI_NOTIFICATIONS = not getSettings("DisableNotifications")
        globals.ENABLE_SUCCESS_NOTIFICATIONS = not getSettings("DisableSuccessNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS
        globals.ENABLE_ERROR_NOTIFICATIONS = not getSettings("DisableErrorNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS
        globals.ENABLE_UPDATES_NOTIFICATIONS = not getSettings("DisableUpdatesNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS


def getSettingsValue(s: str) -> str:
    """
    Returns the stored value for the given setting. If the setting is unset or the function fails an empty string will be returned
    """
    globals.settingsCache
    try:
        try:
            return str(globals.settingsCache[s+"Value"])
        except KeyError:
            with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "r", encoding="utf-8", errors="ignore") as sf:
                v: str = sf.read()
                globals.settingsCache[s+"Value"] = v
                return v
    except FileNotFoundError:
        return ""
    except Exception as e:
        print(e)
        return ""

def setSettingsValue(s: str, v: str) -> None:
    """
    Sets the stored value for the given setting. A string value is required.
    """
    globals.settingsCache
    try:
        globals.settingsCache = {}
        with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "w", encoding="utf-8", errors="ignore") as sf:
            sf.write(v)
    except Exception as e:
        print(e)

def nativeWindowsShare(text: str, url: str, window: QWidget = None) -> int:
    coordinates = ""
    if window:
        coordinates = f"{window.mapToGlobal(QPoint(0, 0)).x()},{window.mapToGlobal(QPoint(0, 0)).y()},{window.width()},{window.height()}"
    clr.AddReference(SHARE_DLL_PATH)
    import WingetUIShareComponent
    WingetUIShareComponent.Form1(["", text, url, coordinates])

    #coordinates = ""
    #if window:
    #    coordinates = f"{window.mapToGlobal(QPoint(0, 0)).x()},{window.mapToGlobal(QPoint(0, 0)).y()},{window.width()},{window.height()}"
    #globals.shareProcessHandler = subprocess.Popen([SHARE_EXE_PATH, text, url, coordinates], shell=True)
    #cprint(globals.shareProcessHandler.args)

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
    return SYSTEM_THEME_ON_LAUNCH == 0

def isTaskbarDark() -> bool:
    return readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 1) == 0

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

operationsToAdd: dict[object:str] = {}

def AddOperationToLog(operation: str, package, commandline: str):
    global operationsToAdd
    stringToAdd =  f" Operation: {operation} - Perform date {str(datetime.now())}\n"
    stringToAdd += f" Package: {str(package)}\n"
    stringToAdd += f" Command-line call: {commandline}"
    operationsToAdd[package] = stringToAdd

def AddResultToLog(output: list, package, result: int):
    print(output)
    global operationsToAdd
    try:
        currentInstallations = getSettingsValue("OperationHistory").split("\n\n--------------------------------\n")
        stringToAdd =  operationsToAdd[package]
        stringToAdd += f" Output code: {result}\n"
        stringToAdd += f" Console output:\n"
        for line in output:
            for subline in line.split("\r"):
                stringToAdd += f"   | {subline}\n"
        setSettingsValue("OperationHistory", "\n\n--------------------------------\n".join(([stringToAdd] + currentInstallations)[0:100]))
    except Exception as e:
        report(e)

def update_tray_icon():
    if globals.tray_is_error:
        globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_orange")))
        globals.trayIcon.setToolTip(f"{_('Attention required')} - WingetUI")
    elif globals.tray_is_needs_restart:
        globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_turquoise")))
        globals.trayIcon.setToolTip(f"{_('Restart required')} - WingetUI")
    elif globals.tray_is_installing:
        globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_blue")))
        globals.trayIcon.setToolTip(f"{_('Operation in progress')} - WingetUI")
    elif globals.tray_is_available_updates:
        try:
            if globals.updates.availableUpdates == 1:
                trayIconToolTip = _("WingetUI - 1 update is available").replace("WingetUI - ", "")
            else:
                trayIconToolTip = _("WingetUI - {0} updates are available").format(globals.updates.availableUpdates).replace("WingetUI - ", "")
        except Exception as e:
            report(e)
            trayIconToolTip = _("Updates available!").replace('"', '')
        globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_green")))
        globals.trayIcon.setToolTip(f"{trayIconToolTip} - WingetUI")
    else:
        globals.trayIcon.setIcon(QIcon(getTaskbarMedia("tray_empty")))
        globals.trayIcon.setToolTip(f"{_('WingetUI - Everything is up to date').replace('WingetUI - ', '')} - WingetUI")

def ApplyMenuBlur(hwnd: int, window: QWidget, smallCorners: bool = False, avoidOverrideStyleSheet: bool = False, shadow: bool = True, useTaskbarModeCheck: bool = False):
    hwnd = int(hwnd)
    mode = isDark()
    isW11 = False
    try:
        import platform
        if int(platform.version().split('.')[2]) >= 22000:
            isW11 = True
    except Exception as e:
        report(e)
    if not avoidOverrideStyleSheet:
        if window.objectName() == "":
            window.setObjectName("MenuMenuMenuMenu")
        if not isDark():
            window.setStyleSheet(f'#{window.objectName()}{{ background-color: {"transparent" if isW11 else "rgba(255, 255, 255, 30%);border-radius: 0px;" };}}')
        else:
            window.setStyleSheet(f'#{window.objectName()}{{ background-color: {"transparent" if isW11 else "rgba(20, 20, 20, 25%);border-radius: 0px;" };}}')
    if mode:
        try:
            GlobalBlur(hwnd, Acrylic=True, hexColor="#21212140", Dark=True, smallCorners=smallCorners)
        except OverflowError:
            pass
    else:
        try:
            GlobalBlur(hwnd, Acrylic=True, hexColor="#eeeeee40", Dark=True, smallCorners=smallCorners)
        except OverflowError:
            pass

def getPath(s: str) -> str:
    return str(Path(f"{realpath}/resources/{s}").resolve()).replace("\\", "/")

def getIconMode() -> str:
    return "white" if isDark() else "black"

def getTaskbarIconMode() -> str:
    return "white" if isTaskbarDark() else "black"

def getMedia(m: str, autoIconMode = True) -> str:
    filename = ""
    if autoIconMode == True:
        filename = getPath(f"{m}_{getIconMode()}.png")
    if not filename or not os.path.exists(filename):
        filename = getPath(f"{m}.png")
    return filename

def getTaskbarMedia(m: str, autoIconMode = True) -> str:
    filename = ""
    if autoIconMode == True:
        filename = getPath(f"{m}_{getTaskbarIconMode()}.png")
    if not filename or not os.path.exists(filename):
        filename = getPath(f"{m}.png")
    return filename

def getint(s: str, fallback: int) -> int:
    try:
        return int(s)
    except:
        print("can't parse", s)
        return fallback

def blacklistUpdatesForPackage(id: str):
    """
    THIS FUNCTION IS DEPRECATED. USE IgnorePackageUpdates_Permanent INSTEAD
    """
    setSettingsValue("BlacklistedUpdates", getSettingsValue("BlacklistedUpdates")+id+",")
    try:
        raise Exception("This function has been deprecated, and shouldn't have been called")
    except Exception as e:
        report(e)

def IgnorePackageUpdates_Permanent(id: str, store: str):
    """
    With the given PACKAGE_ID and PACKAGE_STORE parameters, add the packages to the blacklist
    """
    baseList = [v for v in getSettingsValue("PermanentlyIgnoredPackageUpdates").split(";") if v]
    packageString = f"{id},{store.lower().split(':')[0]}"
    if not packageString in baseList:
        baseList.append(packageString)
    setSettingsValue("PermanentlyIgnoredPackageUpdates", ";".join(baseList))

def GetIgnoredPackageUpdates_Permanent() -> list[list[str, str]]:
    """
    Returns a list in the following format [[packageId, store], [packageId, store], etc.] representing the permanently ignored packages.
    """
    baseList = [v for v in getSettingsValue("PermanentlyIgnoredPackageUpdates").split(";") if v]
    return  [v.split(",") for v in baseList if len(v.split(",")) == 2]

def IgnorePackageUpdates_SpecificVersion(id: str, version: str, store: str):
    """
    With the given PACKAGE_ID, SKIPPED_VERSION and PACKAGE_STORE parameters, add the versions of the packages to the blacklist
    """
    baseList = [v for v in getSettingsValue("SingleVersionIgnoredPackageUpdates").split(";") if v]
    packageString = f"{id},{version.lower().replace(',', '.')},{store.lower().split(':')[0]}"
    if not packageString in baseList:
        baseList.append(packageString)
    setSettingsValue("SingleVersionIgnoredPackageUpdates", ";".join(baseList))

def GetIgnoredPackageUpdates_SpecificVersion() -> list[list[str, str, str]]:
    """
    Returns a list in the following format [[packageId, skippedVersion, store], [packageId, skippedVersion, store], etc.] representing the packages that have a version skipped.
    """
    baseList = [v for v in getSettingsValue("SingleVersionIgnoredPackageUpdates").split(";") if v]
    return  [v.split(",") for v in baseList if len(v.split(",")) == 3]

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
        return self.localtrace

def notify(title: str, text: str, iconpath: str = getMedia("notif_info")) -> None:
    if globals.ENABLE_WINGETUI_NOTIFICATIONS:
        globals.trayIcon.showMessage(title, text, QIcon())

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
        newlang = urlopen("https://raw.githubusercontent.com/marticliment/WingetUI/main/wingetui/lang/"+file)
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

def formatPackageIdAsName(id: str):
    """
    Returns a more beautiful name for the given ID
    """
    return " ".join([piece.capitalize() for piece in id.replace("-", " ").replace("_", " ").split(" ")])

def getPackageIcon(package) -> str:
    try:
        id = package.Id
        iconId = package.getIconId()
        iconpath = os.path.join(os.path.expanduser("~"), f".wingetui/cachedmeta/{iconId}.icon.png")
        if not os.path.exists(iconpath):
            iconurl = globals.packageMeta["icons_and_screenshots"][iconId]["icon"]
            print("🔵 Found icon: ", iconurl)
            if iconurl:
                icondata = urlopen(iconurl).read()
                with open(iconpath, "wb") as f:
                    f.write(icondata)
            else:
                print("🟡 Icon url empty")
                raise KeyError(f"{iconurl} was empty")
        else:
            cprint(f"🔵 Found cached image in {iconpath}")
        return iconpath
    except Exception as e:
        try:
            if type(e) != KeyError:
                report(e)
            else:
                print(f"🟡 Icon {iconId} not found in json")
        except Exception as e:
            report(e)
        return ""

def ConvertMarkdownToHtml(content: str) -> str:
    try:
        content = content.replace("\n\r", "<br>")
        content = content.replace("\n", "<br>")
        firsttext = "<br>"+content
        content = ""
        for line in firsttext.split("<br>"):
            if line:
                content += line+"<br>"
        content = "<br>"+content
        
        # Convert headers
        for match in re.findall("<br>[ ]*#{3,4}[^\>\<]*<br>", content):
            match: str
            content = content.replace(match, f'<br><b>{match.replace("#", "").strip()}</b>')
        
        for match in re.findall("<br>[ ]*##[^\>\<]*<br>", content):
            match: str
            content = content.replace(match, f'<br><b style="font-size:12.5pt;">{match.replace("#", "").strip()}</b>')
            
        for match in re.findall("<br>#[^\>\<]*<br>", content):
            match: str
            content = content.replace(match, f'<br><b style="font-size:14pt;">{match.replace("#", "").strip()}</b>')
            
        # Convert linked images to URLs
        for match in re.findall("\[!\[[^\[\]]*\]\([^\(\)]*\)\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(match, f'<a style="color:{blueColor}" href="{match.split("(")[-1][:-1]}">{match.split("]")[0][3:]}</a>')

        # Convert unlinked images to URLs
        for match in re.findall("!\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][2:]}</a>')
        
        # Convert URLs to <a href=></a> tags
        for match in re.findall("\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][1:]}</a>')

        i = 0
        linelist = content.split("<br>")
        while i<len(linelist):
            line = linelist[i]
            
            for match in re.findall("\*\*[^ ][^\*]+[^ ]\*\*", line):
                line = line.replace(match, "<b>"+match[2:-2]+"</b>")
                
            for match in re.findall("\*[^ ][^\*]+[^ ]\*", line):
                line = line.replace(match, "<i>"+match[1:-1]+"</i>")
                
            for match in re.findall("`[^ ][^`]+[^ ]`", line):
                line = line.replace(match, f"<span style='font-family: \"Consolas\";background-color:{'#303030' if isDark() else '#eeeeee'}'>"+match[1:-1]+"</span>")
                
            linelist[i] = line
            i += 1
            
        content = "<br>".join(linelist)
        
        content = content.replace("<br></b>", "</b><br>")
        
        # Convert unordered lists
        content = content.replace("<br>- ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br> - ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br>  - ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>   - ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>    - ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>     - ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>* ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br> * ", f"<br>{'&nbsp;'*4}● ")
        content = content.replace("<br>  * ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>   * ", f"<br>{'&nbsp;'*8}○ ")
        content = content.replace("<br>    * ", f"<br>{'&nbsp;'*12}□ ")
        content = content.replace("<br>     * ", f"<br>{'&nbsp;'*12}□ ")
        
        # Convert numbered lists
        for number in range(0, 20):
            content = content.replace(f"<br>{number}. ", f"<br>{'&nbsp;'*4}{number}. ")
            content = content.replace(f"<br> {number}. ", f"<br>{'&nbsp;'*4}{number}. ")
        
        # Filter empty newlines
        content = content.replace("<br><br>", "<br>").replace("<br><br>", "<br>")
        print(content)
        return content
    except Exception as e:
        report(e)
        return content

globals.ENABLE_WINGETUI_NOTIFICATIONS = not getSettings("DisableNotifications")
globals.ENABLE_SUCCESS_NOTIFICATIONS = not getSettings("DisableSuccessNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS
globals.ENABLE_ERROR_NOTIFICATIONS = not getSettings("DisableErrorNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS
globals.ENABLE_UPDATES_NOTIFICATIONS = not getSettings("DisableUpdatesNotifications") and globals.ENABLE_WINGETUI_NOTIFICATIONS


if (getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS')):
    sys.stdout = stdout_buffer = io.StringIO()
    sys.stderr = stdout_buffer

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

if not os.path.isdir(os.path.join(os.path.expanduser("~"), ".wingetui")):
    try:
        os.makedirs(os.path.join(os.path.expanduser("~"), ".wingetui"))
    except:
        pass

GSUDO_EXECUTABLE = os.path.join(os.path.join(realpath, "components"), "gsudo.exe") if not getSettings("UseUserGSudo") else shutil.which("gsudo")

try:
    GSUDO_EXE_LOCATION = os.path.dirname(GSUDO_EXECUTABLE)
except TypeError as e:
    report(e)
    GSUDO_EXE_LOCATION = os.path.expanduser("~")
SHARE_DLL_PATH = os.path.join(os.path.join(realpath, "components"), "ShareLibrary.dll")

#
# Begin Import C#.NET DLLs
#
 

#
# End Import C#.NET DLLs
#

SYSTEM_THEME_ON_LAUNCH = readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1)
if isDark():
    blueColor = f"rgb({getColors()[1]})"
else:
    blueColor = f"rgb({getColors()[4]})"

t0 = time.time()

if getSettingsValue("PreferredLanguage") == "":
    setSettingsValue("PreferredLanguage", "default")

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

langName: str = lang['locale']

if "zh_CN" in langName:
    globals.textfont: str = "Microsoft YaHei UI"
    globals.dispfont: str = "Microsoft YaHei UI"
    globals.dispfontsemib: str = "Microsoft YaHei UI"

if "zh_TW" in langName:
    globals.textfont: str = "Microsoft JhengHei UI"
    globals.dispfont: str = "Microsoft JhengHei UI"
    globals.dispfontsemib: str = "Microsoft JhengHei UI"

try:
    englang = loadLangFile(languages["en"], bundled=True) | {"locale": "en"}
except Exception as e:
    report(e)
    englang = {"locale": "en"}

print(f"It took {time.time()-t0} to load all language files")



Thread(target=checkQueue, daemon=True).start()
Thread(target=foregroundWindowThread, daemon=True, name="Tools: get foreground window").start()


if __name__ == "__main__":
    import __init__
