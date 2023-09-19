"""

wingetui/upgradeAssistant.py

This file contains the code that will migrate WingetUI to C:\\Program Files when installing version 2.0.3

"""


import os
import winreg
import glob


def doTheMagic():
    try:
        REG_PATH = r"Software\Microsoft\Windows\CurrentVersion\Uninstall\{889610CC-4337-4BDB-AC3B-4F21806C0BDD}_is1"

        INSTALL_LOCATION = os.path.join(os.path.expanduser("~"), "AppData/Local/Programs/WingetUI")
        try:
            key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_ALL_ACCESS)
            for i in range(1024):
                try:
                    val = winreg.EnumValue(key, i)
                    if val[0] == "InstallLocation":
                        print("Found install path on ", val[1])
                        INSTALL_LOCATION = val[1]
                    winreg.DeleteValue(key, val[0])
                    print(val)
                except OSError:
                    break
            winreg.DeleteKey(winreg.HKEY_CURRENT_USER, REG_PATH)
        except FileNotFoundError:
            print("No old installation found")
            return
        except Exception as e:
            print("Can't delete registry keys, ", e)

        try:
            os.remove(os.path.join(os.path.expanduser("~"), "AppData/Roaming/Microsoft/Windows/Start Menu/Programs/WingetUI.lnk"))
        except Exception as e:
            print("Can't delete start menu entry, ", e)

        try:
            os.remove(os.path.join(os.path.expanduser("~"), "AppData/Roaming/Microsoft/Windows/Start Menu/WingetUI.lnk"))
        except Exception as e:
            print("Can't delete start menu entry, ", e)

        try:
            os.remove(os.path.join(os.path.expanduser("~"), "Desktop/WingetUI.lnk"))
        except Exception as e:
            print("Can't delete desktop entry, ", e)

        for file in glob.glob(INSTALL_LOCATION + "/**/*.*", recursive=True):
            if "choco-cli" in file:
                print(f"Not deleting {file} because is chocolatey component!")
            else:
                try:
                    os.remove(file)
                except Exception as e:
                    print(f"Can't delete file {file}, ", e)
    except Exception as e:
        print(e)
