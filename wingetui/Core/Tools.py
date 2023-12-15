if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)


import io
import json
import locale
import os
import platform
import re
import shutil
import subprocess
import sys
import time
import tempfile
import traceback
import winreg
import win32gui
from datetime import datetime
from pathlib import Path
from threading import Thread
from typing import IO
from unicodedata import combining, normalize
from urllib.request import urlopen

import wingetui.Core.Globals as Globals
from wingetui.Core.Languages.LangReference import *
from wingetui.Core.Data.Versions import *


try:
    import clr
except RuntimeError:
    print("🔴 .NET Runtime not found, aborting...")
    import traceback
    tb = traceback.format_exception(*sys.exc_info())
    tracebacc = ""
    for line in tb:
        tracebacc += line + "\n"
    import ctypes
    ctypes.windll.user32.MessageBoxW(None, "WingetUI requires .NET to be installed on your machine. Please install .NET\n\n" + tracebacc, "WingetUI Crash handler", 0x00000010)

    sys.exit(1)

OLD_STDOUT = sys.stdout
OLD_STDERR = sys.stderr
stdout_buffer = io.StringIO()
stderr_buffer = io.StringIO()
MissingTranslationList = []
realpath = ""
blueColor = "blue"
try:
    winver = int(platform.version().split('.')[2])
except Exception as e:
    print(e)
    winver = 00000
isWin11 = winver >= 22000


def cprint(*args) -> None:
    print(*args, file=OLD_STDOUT)


def report(exception) -> None:  # Exception reporter
    tb = traceback.format_exception(*sys.exc_info())
    try:
        for line in tb:
            print("🔴 ", line)
            cprint("🔴 ", line)
        print(
            f"🔴 Note this traceback was caught by reporter and has been added to the log ({exception})")
    except UnicodeEncodeError:
        for line in tb:
            print("ERROR", line)
            cprint("ERROR", line)
        print(
            f"ERROR Note this traceback was caught by reporter and has been added to the log ({exception})")


def _(s):  # Translate function
    global lang
    try:
        t = lang[s]
        return ("🟢" + t + "🟢" if debugLang else t) if t else f"🟡{s}🟡" if debugLang else eng_(s)
    except KeyError:
        if debugLang:
            print(s)
        if s not in MissingTranslationList:
            MissingTranslationList.append(s)
        return f"🔴{eng_(s)}🔴" if debugLang else eng_(s)


def eng_(s):  # English translate function
    try:
        t = englang[s]
        return t if t else s
    except KeyError:
        if debugLang:
            print(s)
        return s


def getSettings(s: str, cache=True) -> bool:
    """
    Returns a boolean value representing if the given setting is enabled or not.
    """
    Globals.settingsCache
    try:
        try:
            if not cache:
                raise KeyError("Cache disabled")
            return Globals.settingsCache[s]
        except KeyError:
            v = os.path.exists(os.path.join(os.path.join(
                os.path.expanduser("~"), ".wingetui"), s))
            Globals.settingsCache[s] = v
            return v
    except Exception as e:
        print(e)
        return False


def setSettings(s: str, v: bool) -> None:
    """
    Sets a boolean value for the given setting
    """
    Globals.settingsCache
    try:
        Globals.settingsCache = {}
        if (v):
            open(os.path.join(os.path.join(
                os.path.expanduser("~"), ".wingetui"), s), "w").close()
        else:
            try:
                os.remove(os.path.join(os.path.join(
                    os.path.expanduser("~"), ".wingetui"), s))
            except FileNotFoundError:
                pass
    except Exception as e:
        print(e)
    if "Notifications" in s:
        Globals.ENABLE_WINGETUI_NOTIFICATIONS = not getSettings(
            "DisableNotifications")
        Globals.ENABLE_SUCCESS_NOTIFICATIONS = not getSettings(
            "DisableSuccessNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS
        Globals.ENABLE_ERROR_NOTIFICATIONS = not getSettings(
            "DisableErrorNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS
        Globals.ENABLE_UPDATES_NOTIFICATIONS = not getSettings(
            "DisableUpdatesNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS


def getSettingsValue(s: str) -> str:
    """
    Returns the stored value for the given setting. If the setting is unset or the function fails an empty string will be returned
    """
    Globals.settingsCache
    try:
        if (s + "Value") in Globals.settingsCache.keys():
            return str(Globals.settingsCache[s + "Value"])
        else:
            with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "r", encoding="utf-8", errors="ignore") as sf:
                v: str = sf.read()
                Globals.settingsCache[s + "Value"] = v
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
    Globals.settingsCache
    try:
        Globals.settingsCache = {}
        with open(os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), s), "w", encoding="utf-8", errors="ignore") as sf:
            sf.write(v)
    except Exception as e:
        print(e)


def GetJsonSettings(Name: str, Scope: str = "") -> dict:
    """
    Returns the stored value for the given setting. If the setting is unset or the function fails an empty string will be returned
    """
    Globals.settingsCache
    try:
        if (Name + "JSON") in Globals.settingsCache.keys():
            return Globals.settingsCache[Name + "JSON"]
        else:
            if not Scope:
                path = os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), Name + ".json")
            else:
                path = os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui", Scope), Name + ".json")
            with open(path, "r", encoding="utf-8", errors="ignore") as file:
                data: dict = json.load(file)
                Globals.settingsCache[Name + "JSON"] = data
                return data
    except FileNotFoundError:
        return {}
    except Exception as e:
        print(e)
        return {}


def SetJsonSettings(Name: str, Data: dict, Scope: str = "") -> None:
    """
    Sets the stored value for the given JSON-stored setting. A string value is required.
    """
    Globals.settingsCache
    try:
        Globals.settingsCache = {}
        if not Scope:
            path = os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui"), Name + ".json")
        else:
            path = os.path.join(os.path.join(os.path.expanduser("~"), ".wingetui", Scope), Name + ".json")
        if not os.path.isdir(os.path.dirname(path)):
            os.makedirs(os.path.dirname(path))
        with open(path, "w", encoding="utf-8", errors="ignore") as file:
            file.write(json.dumps(Data, indent=4))
    except Exception as e:
        print(e)


def readRegedit(aKey, sKey, default, storage=winreg.HKEY_CURRENT_USER):
    registry = winreg.ConnectRegistry(None, storage)
    reg_keypath = aKey
    try:
        reg_key = winreg.OpenKey(registry, reg_keypath)
    except FileNotFoundError:
        return default
    except Exception as e:
        print(e)
        return default

    for i in range(1024):
        try:
            value_name, value, _ = winreg.EnumValue(reg_key, i)
            if value_name == sKey:
                return value
        except OSError:
            return default
        except Exception as e:
            print(e)
            return default


def getColors() -> list:
    colors = ['215,226,228', '160,174,183', '101,116,134',
              '81,92,107', '69,78,94', '41,47,64', '15,18,36', '239,105,80']
    string = readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentPalette",
                         b'\xe9\xd8\xf1\x00\xcb\xb7\xde\x00\x96}\xbd\x00\x82g\xb0\x00gN\x97\x00H4s\x00#\x13K\x00\x88\x17\x98\x00')
    i = 0
    j = 0
    while (i + 2) < len(string):
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
    return readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0


def isTaskbarDark() -> bool:
    return readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", 1) == 0


def queueProgram(id: str):
    Globals.pending_programs.append(id)


def removeProgram(id: str):
    try:
        Globals.pending_programs.remove(id)
    except ValueError:
        pass
    if (Globals.current_program == id):
        Globals.current_program = ""


def checkQueue():
    print("🟢 checkQueue Thread started!")
    while True:
        if (Globals.current_program == ""):
            try:
                Globals.current_program = Globals.pending_programs[0]
                print(f"🔵 Current program set to {Globals.current_program}")
            except IndexError:
                pass
        time.sleep(0.2)


operationsToAdd: dict[object:str] = {}


def AddOperationToLog(operation: str, package, commandline: str):
    global operationsToAdd
    stringToAdd = f" Operation: {operation} - Perform date {str(datetime.now())}\n"
    stringToAdd += f" Package: {str(package)}\n"
    stringToAdd += f" Command-line call: {commandline}"
    operationsToAdd[package] = stringToAdd


def AddResultToLog(output: list, package, result: int):
    print(output)
    global operationsToAdd
    try:
        currentInstallations = getSettingsValue("OperationHistory").split(
            "\n\n--------------------------------\n")
        stringToAdd = operationsToAdd[package]
        stringToAdd += f" Output code: {result}\n"
        stringToAdd += " Console output:\n"
        for line in output:
            for subline in line.split("\r"):
                stringToAdd += f"   | {subline}\n"
        setSettingsValue("OperationHistory", "\n\n--------------------------------\n".join(
            ([stringToAdd] + currentInstallations)[0:100]))
    except Exception as e:
        report(e)


def getPath(s: str) -> str:
    return str(Path(f"{realpath}/resources/{s}").resolve()).replace("\\", "/")


def getIconMode() -> str:
    return "white" if isDark() else "black"


def getTaskbarIconMode() -> str:
    return "white" if isTaskbarDark() else "black"


def getMedia(m: str, autoIconMode=True) -> str:
    filename = ""
    if autoIconMode is True:
        filename = getPath(f"{m}_{getIconMode()}.png")
    if not filename or not os.path.exists(filename):
        filename = getPath(f"{m}.png")
    return filename


def getTaskbarMedia(m: str, autoIconMode=True) -> str:
    filename = ""
    if autoIconMode is True:
        filename = getPath(f"{m}_{getTaskbarIconMode()}.png")
    if not filename or not os.path.exists(filename):
        filename = getPath(f"{m}.png")
    return filename


def getint(s: str, fallback: int) -> int:
    try:
        return int(s)
    except Exception:
        print("can't parse", s)
        return fallback


def GetIgnoredPackageUpdates_Permanent() -> list[list[str, str]]:
    """
    Returns a list in the following format [[packageId, store], [packageId, store], etc.] representing the permanently ignored packages.
    """
    baseList = [v for v in getSettingsValue(
        "PermanentlyIgnoredPackageUpdates").split(";") if v]
    return [v.split(",") for v in baseList if len(v.split(",")) == 2]


def GetIgnoredPackageUpdates_SpecificVersion() -> list[list[str, str, str]]:
    """
    Returns a list in the following format [[packageId, skippedVersion, store], [packageId, skippedVersion, store], etc.] representing the packages that have a version skipped.
    """
    baseList = [v for v in getSettingsValue(
        "SingleVersionIgnoredPackageUpdates").split(";") if v]
    return [v.split(",") for v in baseList if len(v.split(",")) == 3]


carriedChar = b""


def getLineFromStdout(p: subprocess.Popen) -> (bytes, bool):
    """
    This function replaces p.stdout.readline(). Will return lines both from \\n-ending and \\r-ending character sequences.
    This function may be more resource-intensive, so it should be used only when live outputs must be analyzed in real time.
    """
    global carriedChar
    stdout: IO[bytes] = p.stdout
    is_newline: bool = False
    char = stdout.read(1)
    line = carriedChar
    carriedChar = b""
    while char not in (b"\n", b"\r") and p.poll() is None:
        line += char
        char = stdout.read(1)
    if b"\n" in char:
        is_newline = True
    elif b"\r" in char:
        carriedChar = stdout.read(1)
        if carriedChar == b"\n":
            is_newline = True
            carriedChar = b""
    line = line.replace(b"\r", b"").replace(b"\n", b"")
    return (line, is_newline) if (line or p.poll() is not None) else getLineFromStdout(p)


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
        if not (self.shouldBeRuning) and event == 'line':
            raise SystemExit()
        return self.localtrace


def foregroundWindowThread():
    """
    This thread will periodically get the window focused by the user every 10 secs, so the tray icon can monitor wether the app should be shown or not.
    """
    while True:
        fw = win32gui.GetForegroundWindow()
        time.sleep(2)
        Globals.lastFocusedWindow = fw
        time.sleep(8)


def loadLangFile(file: str, bundled: bool = False) -> dict:
    try:
        path = os.path.join(LANG_DIR, file)
        if not os.path.exists(path) or getSettings("DisableLangAutoUpdater") or bundled:
            print(f"🟡 Using bundled lang file (forced={bundled})")
            path = getPath("../Core/Languages/" + file)
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
            oldlang = open(os.path.join(LANG_DIR, file), "rb").read()
        except FileNotFoundError:
            oldlang = ""
        try:
            newlang = urlopen("https://raw.githubusercontent.com/marticliment/WingetUI/main/wingetui/Core/Languages/" + file)
        except Exception:
            newlang = urlopen("https://raw.githubusercontent.com/marticliment/WingetUI/main/wingetui/lang/" + file)
        if newlang.status == 200:
            langdata: bytes = newlang.read()
            if not os.path.isdir(LANG_DIR):
                os.makedirs(LANG_DIR)
            if oldlang != langdata:
                print("🟢 Updating outdated language file...")
                with open(os.path.join(LANG_DIR, file), "wb") as f:
                    f.write(langdata)
                    f.close()
                    lang = loadLangFile(file) | {
                        "locale": lang["locale"] if "locale" in lang.keys() else "en"}
            else:
                print("🔵 Language file up-to-date")
    except Exception as e:
        report(e)


def formatPackageIdAsName(id: str):
    """
    Returns a more beautiful name for the given ID
    """
    return " ".join([piece.capitalize() for piece in id.replace("-", " ").replace("_", " ").replace(".", " ").split(" ")]).replace(".install", " (" + _("Install") + ")").replace(".portable", " (" + _("Portable") + ")")


LATIN = "ä  æ  ǽ  đ ð ƒ ħ ı ł ø ǿ ö  œ  ß  ŧ ü  Ä  Æ  Ǽ  Đ Ð Ƒ Ħ I Ł Ø Ǿ Ö  Œ  ẞ  Ŧ Ü "
ASCII = "ae ae ae d d f h i l o o oe oe ss t ue AE AE AE D D F H I L O O OE OE SS T UE"


def normalizeString(s, outliers=str.maketrans(dict(zip(LATIN.split(), ASCII.split())))):
    # Got this function from https://stackoverflow.com/a/71408065/11632591
    return "".join(c for c in normalize("NFD", s.translate(outliers)) if not combining(c))


def getPackageIcon(package) -> str:
    return package.getPackageIcon()


def ConvertMarkdownToHtml(content: str) -> str:
    try:
        content = content.replace("\n\r", "<br>")
        content = content.replace("\n", "<br>")
        firsttext = "<br>" + content
        content = ""
        for line in firsttext.split("<br>"):
            if line:
                content += line + "<br>"
        content = "<br>" + content

        # Convert headers
        for match in re.findall(r"<br>[ ]*#{3,4}[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b>{match.replace("#", "").strip()}</b>')

        for match in re.findall(r"<br>[ ]*##[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b style="font-size:12.5pt;">{match.replace("#", "").strip()}</b>')

        for match in re.findall(r"<br>#[^\>\<]*<br>", content):
            match: str
            content = content.replace(
                match, f'<br><b style="font-size:14pt;">{match.replace("#", "").strip()}</b>')

        # Convert linked images to URLs
        for match in re.findall(r"\[!\[[^\[\]]*\]\([^\(\)]*\)\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("(")[-1][:-1]}">{match.split("]")[0][3:]}</a>')

        # Convert unlinked images to URLs
        for match in re.findall(r"!\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][2:]}</a>')

        # Convert URLs to <a href=></a> tags
        for match in re.findall(r"\[[^\[\]]*\]\([^\(\)]*\)", content):
            match: str
            content = content.replace(
                match, f'<a style="color:{blueColor}" href="{match.split("]")[1][1:-1]}">{match.split("]")[0][1:]}</a>')

        i = 0
        linelist = content.split("<br>")
        while i < len(linelist):
            line = linelist[i]

            for match in re.findall(r"\*\*[^ ][^\*]+[^ ]\*\*", line):
                line = line.replace(match, "<b>" + match[2:-2] + "</b>")

            for match in re.findall(r"\*[^ ][^\*]+[^ ]\*", line):
                line = line.replace(match, "<i>" + match[1:-1] + "</i>")

            for match in re.findall(r"`[^ ][^`]+[^ ]`", line):
                line = line.replace(
                    match, f"<span style='font-family: \"Consolas\";background-color:{'#303030' if isDark() else '#eeeeee'}'>" + match[1:-1] + "</span>")

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
            content = content.replace(
                f"<br>{number}. ", f"<br>{'&nbsp;'*4}{number}. ")
            content = content.replace(
                f"<br> {number}. ", f"<br>{'&nbsp;'*4}{number}. ")

        # Filter empty newlines
        content = content.replace(
            "<br><br>", "<br>").replace("<br><br>", "<br>")
        print(content)
        return content
    except Exception as e:
        report(e)
        return content


colors = getColors()

Globals.ENABLE_WINGETUI_NOTIFICATIONS = not getSettings("DisableNotifications")
Globals.ENABLE_SUCCESS_NOTIFICATIONS = not getSettings(
    "DisableSuccessNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS
Globals.ENABLE_ERROR_NOTIFICATIONS = not getSettings(
    "DisableErrorNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS
Globals.ENABLE_UPDATES_NOTIFICATIONS = not getSettings(
    "DisableUpdatesNotifications") and Globals.ENABLE_WINGETUI_NOTIFICATIONS

Globals.DEFAULT_PACKAGE_BACKUP_DIR = os.path.join(readRegedit(r"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Personal", os.path.expanduser("~")), "WingetUI")

TEMP_DIR = os.path.join(tempfile.gettempdir(), "WingetUI")
ICON_DIR = os.path.join(os.path.expanduser("~"), "AppData/Local/WingetUI/CachedIcons")
CACHED_DIR = os.path.join(os.path.expanduser("~"), "AppData/Local/WingetUI/CachedData")
LANG_DIR = os.path.join(os.path.expanduser("~"), "AppData/Local/WingetUI/CachedLangFiles")

if (getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS')):
    sys.stdout = stdout_buffer = io.StringIO()
    sys.stderr = stdout_buffer

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


if not os.path.isdir(os.path.join(os.path.expanduser("~"), ".wingetui")):
    try:
        os.makedirs(os.path.join(os.path.expanduser("~"), ".wingetui"))
    except Exception:
        pass

GSUDO_EXECUTABLE = os.path.join(os.path.join(realpath, "components"), "gsudo.exe") if not getSettings(
    "UseUserGSudo") else shutil.which("gsudo")

try:
    GSUDO_EXE_LOCATION = os.path.dirname(GSUDO_EXECUTABLE)
except TypeError as e:
    report(e)
    GSUDO_EXE_LOCATION = os.path.expanduser("~")
SHARE_DLL_PATH = os.path.join(os.path.join(
    realpath, "components"), "ShareLibrary.dll")


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
                Thread(target=updateLangFile, args=(
                    languages[ln],), name=f"DAEMON: Update lang_{ln}.json").start()
            break
    if langFound is False:
        raise Exception(f"Value not found for {langNames}")
except Exception as e:
    report(e)
    lang = loadLangFile(languages["en"]) | {"locale": "en"}
    print("🔴 Unknown language")

langName: str = lang['locale']

if "zh_CN" in langName:
    Globals.textfont: str = "Microsoft YaHei UI"
    Globals.dispfont: str = "Microsoft YaHei UI"
    Globals.dispfontsemib: str = "Microsoft YaHei UI"

if "zh_TW" in langName:
    Globals.textfont: str = "Microsoft JhengHei UI"
    Globals.dispfont: str = "Microsoft JhengHei UI"
    Globals.dispfontsemib: str = "Microsoft JhengHei UI"

try:
    englang = loadLangFile(languages["en"], bundled=True) | {"locale": "en"}
except Exception as e:
    report(e)
    englang = {"locale": "en"}

print(f"🔵 It took {time.time()-t0} to load all language files")


Thread(target=checkQueue, daemon=True).start()
Thread(target=foregroundWindowThread, daemon=True,
       name="Tools: get foreground window").start()


if __name__ == "__main__":
    import __init__
