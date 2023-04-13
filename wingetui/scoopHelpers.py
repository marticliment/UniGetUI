from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _

ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

scoop = "powershell -ExecutionPolicy ByPass -Command scoop"

def searchForPackage(signal: Signal, finishSignal: Signal) -> None:
    print("🟢 Starting scoop search...")
    p = subprocess.Popen(f"{scoop} search", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1 and not b"---" in line):
                output.append(ansi_escape.sub('',                 #print(line, ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")))
str(line, encoding='utf-8', errors="ignore")))
            else:
                counter += 1
    counter = 0
    lc = getSettings("LowercaseScoopApps")
    for element in output:
        try:
            signal.emit(element.split(" ")[0].replace("-", " ").capitalize(), f"{element.split(' ')[0].strip()}", list(filter(None, element.split(" ")))[1].strip(), f"Scoop: {list(filter(None, element.split(' ')))[2].strip()}")
        except IndexError as e:
            print("IndexError: "+str(e))
    print("🟢 Scoop search finished")
    finishSignal.emit("scoop")

def searchForInstalledPackage(signal: Signal, finishSignal: Signal) -> None:
    print("🟢 Starting scoop search...")
    time.sleep(2) # dumb wait, but it works
    p = subprocess.Popen(f"{scoop} list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 1
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1 and not b"---" in line):
                output.append(ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore").strip()))
            else:
                counter += 1
    counter = 0
    lc = getSettings("LowercaseScoopApps")
    for element in output:
        try:
            if "Name" in element:
                continue
            items = list(filter(None, element.split(" ")))
            if(len(items)>=2):
                signal.emit(items[0].replace("-", " ").capitalize(), f"{items[0]}", items[1], f"Scoop: {list(filter(None, element.split(' ')))[2].strip()}")
        except IndexError as e:
            print("IndexError: "+str(e))
        except Exception as e:
            print(e)
    print("🟢 Scoop search finished")
    finishSignal.emit("scoop")

def searchForUpdates(signal: Signal, finishSignal: Signal) -> None:
    print("🟢 Starting scoop search...")
    p = subprocess.Popen(f"{scoop} status", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1 and not b"---" in line):
                output.append(ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore").strip()))
            else:
                counter += 1
    counter = 0
    lc = getSettings("LowercaseScoopApps")
    for element in output:
        if "WARN" in element:
            continue
        if "fatal" in element:
            continue
        if "Name" in element:
            continue
        try:
            signal.emit(element.split(" ")[0].replace("-", " ").capitalize(), f"{element.split(' ')[0].strip()}", list(filter(None, element.split(" ")))[1].strip(), list(filter(None, element.split(" ")))[2].strip(), f"Scoop")
        except Exception as e:
            report(e)
    print("🟢 Scoop search finished")
    finishSignal.emit("scoop")

def getInfo(signal: Signal, title: str, id: str, useId: bool, progId: bool, verbose: bool = False) -> None:
    print(f"🟢 Starting get info for title {title}")
    title = title.lower()
    output = []
    unknownStr = _("Not available") if verbose else _("Loading...")
    bucket = "main" if len(id.split("/")) == 1 else id.split('/')[0]
    appInfo = {
        "title": title.split("/")[-1],
        "id": id,
        "publisher": unknownStr,
        "author": unknownStr,
        "description": unknownStr,
        "homepage": unknownStr,
        "license": unknownStr,
        "license-url": unknownStr,
        "installer-sha256": unknownStr,
        "installer-url": unknownStr,
        "installer-type": _("Scoop shim"),
        "manifest": f"https://github.com/ScoopInstaller/{bucket}/blob/master/bucket/{id.split('/')[-1]}.json",
        "updatedate": unknownStr,
        "releasenotes": unknownStr,
        "versions": [],
    }
    
    rawOutput = b""
    p = subprocess.Popen(' '.join([scoop, "cat", f"{id}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    while p.poll() is None:
        pass
    for line in p.stdout.readlines():
        line = line.strip()
        if line:
            rawOutput += line+b"\n"
    manifest = False
    version = ""
    lc = getSettings("LowercaseScoopApps")

    with open(os.path.join(os.path.expanduser("~"), ".wingetui", "scooptemp.json"), "wb") as f:
        f.write(rawOutput)
    try:
        mfest = open(os.path.join(os.path.expanduser("~"), ".wingetui", "scooptemp.json"), "r")
        import json
        data: dict = json.load(mfest)
        if "description" in data.keys():
            appInfo["description"] = data["description"]
            
        if "version" in data.keys():
            appInfo["versions"].append(data["version"])

        if "homepage" in data.keys():
            w = data["homepage"]
            appInfo["homepage"] = w
            if "https://github.com/" in w:
                appInfo["author"] = w.replace("https://github.com/", "").split("/")[0]
            else:
                for e in ("https://", "http://", "www.", ".com", ".net", ".io", ".org", ".us", ".eu", ".es", ".tk", ".co.uk", ".in", ".it", ".fr", ".de", ".kde", ".microsoft"):
                    w = w.replace(e, "")
                appInfo["author"] = w.split("/")[0].capitalize()
                
        if "notes" in data.keys():
            if type(data["notes"]) == list:
                appInfo["releasenotes"] = "\n".join(data["notes"])
            else:
                appInfo["releasenotes"] = data["notes"]
        if "license" in data.keys():
            appInfo["license"] = data["license"] if type(data["license"]) != dict else data["license"]["identifier"]
            appInfo["license-url"] = unknownStr if type(data["license"]) != dict else data["license"]["url"]

        if "url" in data.keys():
            appInfo["installer-url"] = data["url"][0] if type(data["url"]) == list else data["url"]
            appInfo["installer-sha256"] = data["hash"][0] if type(data["hash"]) == list else data["hash"]
        elif "architecture" in data.keys():
            appInfo["installer-url"] = data["architecture"]["64bit"]["url"]
            appInfo["installer-sha256"] = data["architecture"]["64bit"]["hash"]
        
        appInfo["installer-type"] = "Scoop package"
        
    except Exception as e:
        report(e)
        
    if verbose:
        p = subprocess.Popen(' '.join([scoop, "info", f"{title.replace(' ', '-')}"]+ (["--verbose"] if verbose else [])), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        while p.poll() is None:
            pass
        for line in p.stdout.readlines():
            line = line.strip()
            if line:
                output.append(ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")))
        manifest = False
        version = ""
        for line in output:
            for line in output:
                if("Updated by" in line):
                    appInfo["publisher"] = line.replace("Updated by", "").strip()[1:].strip()
                elif("Updated at" in line):
                    appInfo["updatedate"] = line.replace("Updated at", "").strip()[1:].strip()                
    print(f"🔵 Scoop does not support specific version installs")
    appInfo["versions"] = [version]
    appInfo["title"] = appInfo["title"] if lc else appInfo["title"].capitalize()
    signal.emit(appInfo, progId)
    if not verbose:
        getInfo(signal, title, id, useId, progId, verbose=True)
    
def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal, alreadyGlobal: bool = False) -> None:
    print(f"🟢 scoop installer assistant thread started for process {p}")
    outputCode = 1
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            if("Installing" in line):
                counterSignal.emit(1)
            elif("] 100%" in line or "Downloading" in line):
                counterSignal.emit(4)
            elif("was installed successfully!" in line):
                counterSignal.emit(6)
            infoSignal.emit(line)
            if("was installed successfully" in line):
                outputCode = 0
            elif ("is already installed" in line):
                outputCode = 0
            output += line+"\n"
    if "-g" in output and not "successfully" in output and not alreadyGlobal:
        outputCode = 1602
    elif "requires admin rights" in output or "requires administrator rights" in output:
        outputCode = 1603
    closeAndInform.emit(outputCode, output)

   
def uninstallAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal, alreadyGlobal: bool = False) -> None:
    print(f"🟢 scoop uninstaller assistant thread started for process {p}")
    outputCode = 1
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            if("Uninstalling" in line):
                counterSignal.emit(1)
            elif("Removing shim for" in line):
                counterSignal.emit(4)
            elif("was uninstalled" in line):
                counterSignal.emit(6)
            infoSignal.emit(line)
            if("was uninstalled" in line):
                outputCode = 0
            output += line+"\n"
    if "-g" in output and not "was uninstalled" in output and not alreadyGlobal:
        outputCode = 1602
    elif "requires admin rights" in output:
        outputCode = 1603
    closeAndInform.emit(outputCode, output)


def loadBuckets(packageSignal: Signal, finishSignal: Signal) -> None:
    print("🟢 Starting scoop search...")
    p = subprocess.Popen(f"{scoop} bucket list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1 and not b"---" in line):
                output.append(ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")))
            else:
                counter += 1
    counter = 0
    for element in output:
        cprint(element)
        try:
            while "  " in element.strip():
                element = element.strip().replace("  ", " ")
            element: list[str] = element.split(" ")
            packageSignal.emit(element[0].strip(), element[1].strip(), element[2].strip()+" "+element[3].strip(), element[4].strip())
        except IndexError as e:
            try:
                packageSignal.emit(element[0].strip(), element[1].strip(), "Unknown", "Unknown")
            except IndexError as f:
                cprint(e, f)
            print(element)
            print("IndexError: "+str(e))

    print("🟢 Scoop bucket search finished")
    finishSignal.emit()
    




if(__name__=="__main__"):
    import __init__
