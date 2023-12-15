import os
import sys

os.chdir(os.path.join(os.path.dirname(__file__), "..")) # move to root project

try:
    sys.path.append("wingetui")

    from Core.Data.Versions import *


    def fileReplaceLinesWith(filename: str, list: dict[str, str], encoding="utf-8"):
        with open(filename, "r+", encoding=encoding, errors="ignore") as f:
            data = ""
            for line in f.readlines():
                match = False
                for key, value in list.items():
                    if (line.startswith(key)):
                        data += f"{key}{value}"
                        match = True
                        continue
                if (not match):
                    data += line
            f.seek(0)
            f.write(data)
            f.truncate()


    fileReplaceLinesWith("WingetUI.iss", {
        "#define MyAppVersion": f" \"{versionName}\"\n",
        "VersionInfoVersion=": f"{versionISS}\n",
    }, encoding = "utf-8-sig")

    fileReplaceLinesWith("wingetui-version-file", {
        "      StringStruct(u'FileVersion'": f", u'{versionName}'),\n",
        "      StringStruct(u'ProductVersion'": f", u'{versionName}'),\n",
    })
    print("done!")
except FileNotFoundError as e:
    print(f"Error: {e.strerror}: {e.filename}")
except Exception as e:
    print(f"Error: {str(e)}")
