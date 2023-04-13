from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _


common_params = []

print


if getSettings("UseSystemChocolatey"):
    choco = "choco.exe"
else:
    choco = os.path.join(os.path.join(realpath, "choco-cli"), "choco.exe")
    os.environ["chocolateyinstall"] = os.path.dirname(choco)


CHOCO_BLACKLISTED_PACKAGES = ["Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This"]

def searchForPackage(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    cprint("🔵 Starting choco search")
    cacheFile = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata/chocolateypackages")
    cachePath = os.path.dirname(cacheFile)
    if not os.path.exists(cachePath):
        os.makedirs(cachePath)
    if os.path.exists(cacheFile):
        with open(cacheFile, "r") as f:
            content = f.read()
            if content != "":
                cprint("🟢 Found valid cache for chocolatey!")
                for line in content.split("\n"):
                    export = line.split(" ")
                    if len(export) > 1 and not export[0] in CHOCO_BLACKLISTED_PACKAGES:
                        signal.emit(export[0].replace("-", " ").capitalize(), export[0], export[1], "Chocolatey")
                try:
                    lastCache = int(getSettingsValue("ChocolateyCacheDate"))
                    if int(time.time())-lastCache > 60*60*2:
                        shouldReloadCache = True
                    else:
                        shouldReloadCache = False
                except:
                    shouldReloadCache = True
                finishSignal.emit("chocolatey-cached")
            else:
                shouldReloadCache = True
                finishSignal.emit("chocolatey-caching")
    else:
        shouldReloadCache = True
        finishSignal.emit("chocolatey-caching")
    
    if shouldReloadCache and not getSettings("CachingChocolatey"):
        setSettings("CachingChocolatey", True)
        print(f"🟢 Starting choco search, choco on {choco}...")
        p = subprocess.Popen([choco, "search", "*"] + common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
        output = ""
        counter = 0
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if(counter > 1):
                    if not b"packages found" in line:
                        output += str(line, encoding='utf-8', errors="ignore") + "\n"
                else:
                    counter += 1
        oldcontents = ""
        try:
            with open(cacheFile, "r") as f:
                oldcontents = f.read()
                f.close()
        except Exception as e:
            report(e)
        for line in oldcontents.split("\n"):
            if line.split(" ")[0] not in output:
                output += line + "\n"
        with open(cacheFile, "w") as f:
            f.write(output)
        print("🟢 Chocolatey search finished")
        finishSignal.emit("chocolatey-finishedcache")  # type: ignore
        setSettings("CachingChocolatey", False)
        setSettingsValue("ChocolateyCacheDate", str(int(time.time())))


def searchForUpdates(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"🟢 Starting choco search, choco on {choco}...")
    p = subprocess.Popen([choco, "outdated"] + common_params[0:2], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()  # type: ignore
        line = line.strip()
        if line:
            if(counter > 2):
                if not b"Chocolatey has determined" in line:
                    output.append(line)
            else:
               counter += 1
    else:
        counter = 0
        for element in output:
            try:
                export = str(element, "utf-8", errors="ignore").split("|")
                if len(export) > 1 and "Output is package name" not in export[0] and export[0] not in CHOCO_BLACKLISTED_PACKAGES and export[2] != export[1]:
                    signal.emit(export[0].replace("-", " ").capitalize(), export[0], export[1], export[2], "Chocolatey")
            except Exception as e:
                report(e)
        print("🟢 Chocolatey search finished")
        finishSignal.emit("chocolatey")

def searchForInstalledPackage(signal: Signal, finishSignal: Signal) -> None:
    print(f"🟢 Starting choco search, choco on {choco}...")
    p = subprocess.Popen([choco, "list", "--local-only"] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 0 and not b"---" in line and not b"packages installed" in line):
                output.append(line)
            else:
                counter += 1
    counter = 0
    chocoName = "Chocolatey"
    for element in output:
        try:
            export = str(element, encoding="utf-8", errors="ignore").split(" ")
            if export[0] != "-" and len(export) > 1 and not export[0] in CHOCO_BLACKLISTED_PACKAGES and export[1] != "validations" and export[0] != "Directory":
                signal.emit(export[0].replace("-", " ").capitalize(), export[0], export[1], chocoName)
        except Exception as e:
            report(e)
    print("🟢 Chocolatey uninstallable packages search finished")
    finishSignal.emit("chocolatey")

def getInfo(signal: Signal, title: str, id: str, useId: bool, progId: bool) -> None:
    try:
        p = subprocess.Popen([choco, "info", id]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        print(f"🟢 Starting get info for id {id}")
        output = []
        unknownStr = _("Not available")
        appInfo = {
            "title": title,
            "id": id,
            "publisher": unknownStr,
            "author": unknownStr,
            "description": unknownStr,
            "homepage": unknownStr,
            "license": unknownStr,
            "license-url": unknownStr,
            "installer-sha256": unknownStr,
            "installer-url": unknownStr,
            "installer-type": unknownStr,
            "updatedate": unknownStr,
            "releasenotes": unknownStr,
            "manifest": f"https://community.chocolatey.org/packages/{id.lower()}",
            "versions": []
        }
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            cprint(line)
            if line:
                output.append(str(line, encoding='utf-8', errors="ignore"))
        print(p.stdout)
        for line in output:
            cprint(line)
            if("Title:" in line):
                appInfo["title"] = line.split("|")[0].replace("Title:", "").strip()
                appInfo["updatedate"] = line.split("|")[1].replace("Published:", "").strip()
            elif("Author:" in line):
                appInfo["author"] = line.replace("Author:", "").strip()
            elif("Software Site:" in line):
                appInfo["homepage"] = line.replace("Software Site:", "").strip()
            elif("Software License:" in line):
                appInfo["license-url"] = line.replace("Software License:", "").strip()
            elif("Package Checksum:" in line):
                appInfo["installer-sha256"] = line.replace("Package Checksum:", "").strip()
            elif("Description:" in line):
                appInfo["description"] = line.replace("Description:", "").strip()
        appInfo["versions"] = []
        signal.emit(appInfo, progId)
    except Exception as e:
        report(e)
    
def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 choco installer assistant thread started for process {p}")
    outputCode = 0
    counter = 0
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            infoSignal.emit(line)
            counter += 1
            counterSignal.emit(counter)
            output += line+"\n"
    p.wait()
    outputCode = p.returncode
    if outputCode in (1641, 3010):
        outputCode = 0
    elif outputCode == 3010:
        outputCode = 3
    elif ("Run as administrator" in output or "The requested operation requires elevation" in output) and outputCode != 0:
        outputCode = 1603
    closeAndInform.emit(outputCode, output)
 
def uninstallAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 choco installer assistant thread started for process {p}")
    outputCode = 0
    counter = 0
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            infoSignal.emit(line)
            counter += 1
            counterSignal.emit(counter)
            output += line+"\n"
    p.wait()
    cprint(output)
    outputCode = p.returncode
    if outputCode in (1605, 1614, 1641):
        outputCode = 0
    elif outputCode == 3010:
        outputCode = 3
    elif "Run as administrator" in output or "The requested operation requires elevation" in output:
        outputCode = 1603
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__