from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _


common_params = []


if getSettings("UseSystemChocolatey"):
    choco = "choco.exe"
else:
    choco = os.path.join(os.path.join(realpath, "choco-cli"), "choco.exe")


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
                    if len(export) > 1:
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
        p = subprocess.Popen([choco, "search", "*"] + common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=False)
        output = ""
        counter = 0
        idSeparator = 0
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                if(counter > 1):
                    if not b"packages found" in line:
                        output += str(line, encoding='utf-8', errors="ignore") + "\n"
                else:
                    counter += 1
        with open(cacheFile, "w") as f:
            f.write(output)
        print("🟢 Chocolatey search finished")
        finishSignal.emit("chocolatey-finishedcache")  # type: ignore
        setSettings("CachingChocolatey", False)
        setSettingsValue("ChocolateyCacheDate", str(int(time.time())))


def searchForUpdates(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"🟢 Starting choco search, choco on {choco}...")
    p = subprocess.Popen(["mode", "400,30&", choco, "upgrade", "--include-unknown"] + common_params[0:2], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()  # type: ignore
        line = line.strip()
        if line:
            if(counter > 0):
                if not b"upgrades available" in line:
                    output.append(line)
            else:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                for char in ("\r", "/", "|", "\\", "-"):
                    l = l.split(char)[-1].strip()
                print(l)
                if("Id" in l):
                    idSeparator = len(l.split("Id")[0])
                    verSeparator = len(l.split("Version")[0])
                    newVerSeparator = len(l.split("Available")[0])
                    counter += 1
    
    if p.returncode != 0 and not noretry:
        time.sleep(1)
        print(p.returncode)
        searchForUpdates(signal, finishSignal, noretry=True)
    else:
        counter = 0
        for element in output:
            try:
                element = str(element, "utf-8", errors="ignore")
                verElement = element[idSeparator:].strip()
                verElement.replace("\t", " ")
                while "  " in verElement:
                    verElement = verElement.replace("  ", " ")
                iOffset = 0
                id = verElement.split(" ")[iOffset+0]
                ver = verElement.split(" ")[iOffset+1]
                newver = verElement.split(" ")[iOffset+2]
                if len(id)==1:
                    iOffset + 1
                    id = verElement.split(" ")[iOffset+0]
                    newver = verElement.split(" ")[iOffset+2]
                    ver = verElement.split(" ")[iOffset+1]
                if ver.strip() in ("<", ">", "-"):
                    iOffset += 1
                    ver = verElement.split(" ")[iOffset+1]
                    newver = verElement.split(" ")[iOffset+2]
                if not "  " in element[0:idSeparator].strip():
                    signal.emit(element[0:idSeparator].strip(), id, ver, newver, "Winget")
                else:
                    print(f"🟡 package {element[0:idSeparator].strip()} failed parsing, going for method 2...")
                    print(element, verSeparator)
                    name = element[0:idSeparator].strip().replace("  ", "#").replace("# ", "#").replace(" #", "#")
                    while "##" in name:
                        name = name.replace("##", "#")
                    signal.emit(name.split("#")[0], name.split("#")[-1]+id, ver, newver, "Winget")
            except Exception as e:
                try:
                    signal.emit(element[0:idSeparator].strip(), element[idSeparator:verSeparator].strip(), element[verSeparator:newVerSeparator].split(" ")[0].strip(), element[newVerSeparator:].split(" ")[0].strip(), "Winget")
                except Exception as e:
                    report(e)
                except Exception as e:
                    report(e)
        print("🟢 Winget search finished")
        finishSignal.emit("choco")

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
    emptyStr = ""
    chocoName = "Chocolatey"
    for element in output:
        try:
            output = str(element, encoding="utf-8", errors="ignore").split(" ")
            signal.emit(output[0].replace("-", " ").capitalize(), output[0], output[1], chocoName)
        except Exception as e:
            report(e)
    print("🟢 Winget uninstallable packages search finished")
    finishSignal.emit("chocolatey")

def getInfo(signal: Signal, title: str, id: str, useId: bool) -> None:
    try:
        p = subprocess.Popen([choco, "info", id]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        print(f"🟢 Starting get info for id {id}")
        output = []
        unknownStr = _("Unknown")
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
        signal.emit(appInfo)
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
    if outputCode == 0x8A150011:
        outputCode = 2
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
    outputCode = p.returncode
    if "1603" in output:
        outputCode = 1603
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__