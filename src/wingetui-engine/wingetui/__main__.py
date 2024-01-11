if __name__ == "__main__":
    # WingetUI cannot be run directly from this file, it must be run by importing the wingetui module
    import os
    import subprocess
    import sys
    if __file__ not in sys.argv:
        sys.exit(subprocess.run(["cmd", "/C", "python", "-m", "wingetui"], shell=True, cwd=os.path.dirname(__file__).split("wingetui")[0]).returncode)



try:
    

    import sys
    if "--debugcrash" in sys.argv:
        import faulthandler
        faulthandler.enable()

    pathIsValid = True
    specialCharacter = ""
    for char in sys.executable:
        if char not in "\\/:abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSRTUVWXYZ1234567890_+()-., ":
            specialCharacter = char
            pathIsValid = False
            break

    if not pathIsValid:
        import ctypes
        ctypes.windll.user32.MessageBoxW(None, "WingetUI can't be installed in a path containing special characters. Please reinstall WingetUI on a valid location\n\n\nCurrent path: " + os.path.dirname(sys.executable) + "\nInvalid character detected: " + specialCharacter + "\n\n\nPlease run the WingetUI installer and select a different install location. A possible valid path could be C:\\Program Files\\WingetUI", "WingetUI Crash handler", 0x00000010)
        sys.exit(1)

    import os
    import sys

    from PySide6.QtCore import *
    from PySide6.QtGui import *
    from PySide6.QtWidgets import *
    
    import wingetui.Core.Globals as Globals
    from wingetui.Interface.Application import RunMainApplication
    from wingetui.Interface.Tools import *
    from wingetui.Interface.Tools import _
    from wingetui.PackageEngine.Classes import Package

    if "--daemon" in sys.argv:
        if getSettings("DisableAutostart"):
            sys.exit(0)

    print("---------------------------------------------------------------------------------------------------")
    print("")
    print(f"   WingetUI version {versionName} (version number {version}) log")
    print("   All modules loaded successfully and sys.stdout patched correctly, starting main script")
    print(f"   Translator function language set to \"{langName}\"")
    print("")
    print("---------------------------------------------------------------------------------------------------")
    print("")
    print(" Log legend:")
    print(" 🔵: Verbose")
    print(" 🟢: Information")
    print(" 🟡: Warning")
    print(" 🟠: Handled unexpected exception")
    print(" 🔴: Unhandled unexpected exception")
    print("")

    # Migrator from legacy settings
    legacy_ignored_updates = GetIgnoredPackageUpdates_Permanent()
    legacy_ignored_updates_version = GetIgnoredPackageUpdates_SpecificVersion()

    try:
        for pkglist in legacy_ignored_updates_version:
            if len(pkglist) == 3:
                package = Package(pkglist[0], pkglist[0], pkglist[1], pkglist[2], None)
                package.AddToIgnoredUpdates(package.Version)
        setSettings("SingleVersionIgnoredPackageUpdates", False)

        for pkglist in legacy_ignored_updates:
            if len(pkglist) == 2:
                package = Package(pkglist[0], pkglist[0], "", pkglist[1], None)
                package.AddToIgnoredUpdates()
        setSettings("PermanentlyIgnoredPackageUpdates", False)
    except Exception as e:
        report(e)

    sys.exit(RunMainApplication())

except (ModuleNotFoundError, ImportError, FileNotFoundError):
    import traceback
    tb = traceback.format_exception(*sys.exc_info())
    tracebacc = ""
    for line in tb:
        tracebacc += line + "\n"
    import ctypes
    ctypes.windll.user32.MessageBoxW(None, "Your WingetUI installation appears to have missing or corrupt components. Please reinstall WingetUI.\n\n" + tracebacc, "WingetUI Crash handler", 0x00000010)

except Exception as e:
    import platform
    import traceback
    import webbrowser
    if "langName" not in globals() and "langName" not in locals():
        langName = "Unknown"
    try:
        from wingetui.tools import version as appversion
    except Exception:
        appversion = "Unknown"
    os_info = "" + \
        f"                        OS: {platform.system()}\n" + \
        f"                   Version: {platform.win32_ver()}\n" + \
        f"           OS Architecture: {platform.machine()}\n" + \
        f"          APP Architecture: {platform.architecture()[0]}\n" + \
        f"                  Language: {langName}\n" + \
        f"               APP Version: {appversion}\n" + \
        f"                Executable: {sys.executable}\n" + \
        "                   Program: WingetUI\n" + \
        "           Program section: Main script" + \
        "\n\n-----------------------------------------------------------------------------------------"
    traceback_info = "Traceback (most recent call last):\n"
    try:
        for line in traceback.extract_tb(e.__traceback__).format():
            traceback_info += line
        traceback_info += f"\n{type(e).__name__}: {str(e)}"
    except Exception:
        traceback_info += "\nUnable to get traceback"
    webbrowser.open(("https://www.marticliment.com/error-report/?appName=WingetUI&errorBody=" + os_info.replace('\n', '{l}').replace(' ', '{s}') + "{l}{l}{l}{l}WingetUI Log:{l}" + str("\n\n\n\n" + traceback_info).replace('\n', '{l}').replace(' ', '{s}')).replace("#", "|=|"))
    print(traceback_info)
