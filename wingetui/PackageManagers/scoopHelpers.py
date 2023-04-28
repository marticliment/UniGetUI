from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _

ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

scoop = "powershell -ExecutionPolicy ByPass -Command scoop"

def searchForPackage(signal: Signal, finishSignal: Signal) -> None:
    print("🔵 Starting scoop search")
    cacheFile = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata/scooppackages")
    cachePath = os.path.dirname(cacheFile)
    correctCache = False
    if not os.path.exists(cachePath):
        os.makedirs(cachePath)
    if os.path.exists(cacheFile):
        with open(cacheFile, "r") as f:
            content = f.read()
            if content != "":
                print("🟢 Found valid cache for scoop!")
                for line in content.split("\n"):
                    export = list(filter(None, line.split(" ")))
                    if len(export) >= 3:
                        signal.emit(export[0].replace("-", " ").capitalize(), f"{export[0].strip()}", export[1].strip(), f"Scoop: {export[2].strip()}")
                finishSignal.emit("scoop")
                correctCache = True
        
    print("🔵 Starting scoop file update...")
    p = subprocess.Popen(f"{scoop} search", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = ""
    oldcontents = ""
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1 and not b"---" in line):
                output += ansi_escape.sub('', str(line, encoding='utf-8', errors="ignore")) +"\n"
                if not correctCache:
                    export = list(filter(None, str(line, encoding='utf-8', errors="ignore").split(" ")))
                    if len(export) >= 3:
                        signal.emit(export[0].replace("-", " ").capitalize(), f"{export[0].strip()}", export[1].strip(), f"Scoop: {export[2].strip()}")
            else:
                counter += 1
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
    finishSignal.emit("scoop")  
    print("🟢 Scoop cache rebuilt")

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
    if bucket in globals.scoopBuckets:
        bucketRoot = globals.scoopBuckets[bucket].replace(".git", "")
    else:
        bucketRoot = f"https://github.com/ScoopInstaller/{bucket}"
    packageDetails = {
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
        "installer-size": "",
        "installer-type": _("Scoop shim"),
        "manifest": f"{bucketRoot}/blob/master/bucket/{id.split('/')[-1]}.json",
        "updatedate": unknownStr,
        "releasenotes": unknownStr,
        "releasenotesurl": unknownStr,
        "versions": [],
        "architectures": [],
        "scopes": [_("Local"), _("Global")]
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
            packageDetails["description"] = data["description"]
            
        if "version" in data.keys():
            packageDetails["versions"].append(data["version"])

        if "homepage" in data.keys():
            w = data["homepage"]
            packageDetails["homepage"] = w
            if "https://github.com/" in w:
                packageDetails["author"] = w.replace("https://github.com/", "").split("/")[0]
            else:
                for e in ("https://", "http://", "www.", ".com", ".net", ".io", ".org", ".us", ".eu", ".es", ".tk", ".co.uk", ".in", ".it", ".fr", ".de", ".kde", ".microsoft"):
                    w = w.replace(e, "")
                packageDetails["author"] = w.split("/")[0].capitalize()
                
        if "notes" in data.keys():
            if type(data["notes"]) == list:
                packageDetails["releasenotes"] = "\n".join(data["notes"])
            else:
                packageDetails["releasenotes"] = data["notes"]
        if "license" in data.keys():
            packageDetails["license"] = data["license"] if type(data["license"]) != dict else data["license"]["identifier"]
            packageDetails["license-url"] = unknownStr if type(data["license"]) != dict else data["license"]["url"]

        if "url" in data.keys():
            packageDetails["installer-sha256"] = data["hash"][0] if type(data["hash"]) == list else data["hash"]
            url = data["url"][0] if type(data["url"]) == list else data["url"]
            packageDetails["installer-url"] = url
            try:
                packageDetails["installer-size"] = f"({int(urlopen(url).length/1000000)} MB)"
            except Exception as e:
                print("🟠 Can't get installer size:", type(e), str(e))
        elif "architecture" in data.keys():
            packageDetails["installer-sha256"] = data["architecture"]["64bit"]["hash"]
            url = data["architecture"]["64bit"]["url"]
            packageDetails["installer-url"] = url
            try:
                packageDetails["installer-size"] = f"({int(urlopen(url).length/1000000)} MB)"
            except Exception as e:
                print("🟠 Can't get installer size:", type(e), str(e))
            if type(data["architecture"]) == dict:
                packageDetails["architectures"] = list(data["architecture"].keys())
        
        if "checkver" in data.keys():
            if "url" in data["checkver"].keys():
                url = data["checkver"]["url"]
                packageDetails["releasenotesurl"] = f"<a href='{url}' style='color:%bluecolor%'>{url}</a>"
        
        packageDetails["installer-type"] = "Scoop package"
        
    except Exception as e:
        report(e)
        
    if packageDetails["releasenotesurl"] == unknownStr and "github.com" in packageDetails["installer-url"]:
        try:
            url = "/".join(packageDetails["installer-url"].replace("/download/", "/tag/").split("/")[:-1])
            packageDetails["releasenotesurl"] = f"<a href='{url}' style='color:%bluecolor%'>{url}</a>"
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
                    packageDetails["publisher"] = line.replace("Updated by", "").strip()[1:].strip()
                elif("Updated at" in line):
                    packageDetails["updatedate"] = line.replace("Updated at", "").strip()[1:].strip()                
    print(f"🔵 Scoop does not support specific version installs")
    packageDetails["versions"] = [version]
    packageDetails["title"] = packageDetails["title"] if lc else packageDetails["title"].capitalize()
    signal.emit(packageDetails, progId)
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
        outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
    elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
        outputCode = RETURNCODE_NEEDS_ELEVATION
    if "Latest versions for all apps are installed" in output:
        outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
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
        outputCode = RETURNCODE_NEEDS_SCOOP_ELEVATION
    elif "requires admin rights" in output or "requires administrator rights" in output or "you need admin rights to install global apps" in output:
        outputCode = RETURNCODE_NEEDS_ELEVATION
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
        try:
            while "  " in element.strip():
                element = element.strip().replace("  ", " ")
            element: list[str] = element.split(" ")
            packageSignal.emit(element[0].strip(), element[1].strip(), element[2].strip()+" "+element[3].strip(), element[4].strip())
        except IndexError as e:
            try:
                packageSignal.emit(element[0].strip(), element[1].strip(), "Unknown", "Unknown")
            except IndexError as f:
                print(e, f)
            print("IndexError: "+str(e))

    print("🟢 Scoop bucket search finished")
    finishSignal.emit()





if(__name__=="__main__"):
    import wingetui.__init__
