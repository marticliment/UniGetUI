from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *

common_params = ["--source", "winget", "--accept-source-agreements"]
    
winget = os.path.join(os.path.join(realpath, "winget-cli"), "winget.exe")
# winget = "winget"

def searchForPackage(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"[   OK   ] Starting winget search, winget on {winget}...")
    p = subprocess.Popen([winget, "search", ""] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 0):
                output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                l = l.split("\r")[-1]
                if("Id" in l):
                    idSeparator = len(l.split("Id")[0])
                    verSeparator = idSeparator+2
                    i=0
                    while l.split("Id")[1].split(" ")[i] == "":
                        verSeparator += 1
                        i += 1
                    counter += 1
    print(p.stdout)
    print(p.stderr)
    if p.returncode != 0 and not noretry:
        time.sleep(1)
        print(p.returncode)
        searchForPackage(signal, finishSignal, noretry=True)
    else:
        counter = 0
        for element in output:
            try:
                element = bytes(element, "utf-8")
                export = (element[0:idSeparator], element[idSeparator:verSeparator], element[verSeparator:])
                signal.emit(str(export[0], "utf-8").strip(), str(export[1], "utf-8").strip(), str(export[2], "utf-8").split(" ")[0].strip(), "Winget")
            except Exception as e:
                try:
                    element = str(element, "utf-8")
                    signal.emit(element[0:idSeparator].strip(), element[idSeparator:verSeparator].strip(), element[verSeparator:].split(" ")[0].strip(), "Winget")
                except Exception as e:
                    report(e)
        print("[   OK   ] Winget search finished")
        finishSignal.emit("winget")  # type: ignore

def searchForUpdates(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"[   OK   ] Starting winget search, winget on {winget}...")
    p = subprocess.Popen([winget, "upgrade", "--include-unknown"] + common_params[0:2], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()  # type: ignore
        line = line.strip()
        if line:
            if(counter > 0):
                if not b"upgrades available" in line:
                    output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                l = l.split("\r")[-1]
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
                element = bytes(element, "utf-8")
                export = (element[0:idSeparator], element[idSeparator:verSeparator], element[verSeparator:])
                signal.emit(str(export[0], "utf-8").strip(), str(export[1], "utf-8").strip(), str(export[2], "utf-8").split(" ")[0].strip(), "Winget")
            except Exception as e:
                try:
                    element = str(element, "utf-8")
                    signal.emit(element[0:idSeparator].strip(), element[idSeparator:verSeparator].strip(), element[verSeparator:newVerSeparator].split(" ")[0].strip(), element[newVerSeparator:].split(" ")[0].strip(), "Winget")
                except Exception as e:
                    report(e)
                except Exception as e:
                    report(e)
        print("[   OK   ] Winget search finished")
        finishSignal.emit("winget")

def searchForInstalledPackage(signal: Signal, finishSignal: Signal) -> None:
    print(f"[   OK   ] Starting winget search, winget on {winget}...")
    p = subprocess.Popen([winget, "list"] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 0 and not b"---" in line):
                output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                l = l.split("\r")[-1]
                if("Id" in l):
                    idSeparator = len(l.split("Id")[0])
                    verSeparator = len(l.split("Version")[0])
                    counter += 1
    counter = 0
    emptyStr = ""
    wingetName = "Winget"
    print(p.stdout)
    print(output)
    for element in output:
        try:
            element = bytes(element, "utf-8")
            export = (element[0:idSeparator], element[idSeparator:verSeparator], element[verSeparator:])
            signal.emit(str(export[0], "utf-8").strip(), str(export[1], "utf-8").strip(), str(export[2], "utf-8").strip(), wingetName)
        except Exception as e:
            try:
                element = str(element, "utf-8")
                signal.emit(element[0:idSeparator].strip(), element[idSeparator:].strip(), emptyStr, wingetName)
            except Exception as e:
                report(e)
    print("[   OK   ] Winget uninstallable packages search finished")
    finishSignal.emit("winget")

def getInfo(signal: Signal, title: str, id: str, useId: bool) -> None:
    if useId:
        p = subprocess.Popen([winget, "show", "--id", f"{id}", "--exact"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        print(f"[   OK   ] Starting get info for id {id}")
    else:
        p = subprocess.Popen([winget, "show", "--name", f"{title}", "--exact"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        print(f"[   OK   ] Starting get info for title {title}")

    output = []
    appInfo = {
        "title": title,
        "id": id,
        "publisher": "Unknown",
        "author": "Unknown",
        "description": "Unknown",
        "homepage": "Unknown",
        "license": "Unknown",
        "license-url": "Unknown",
        "installer-sha256": "Unknown",
        "installer-url": "Unknown",
        "installer-type": "Unknown",
        "manifest": "Not Applicable",
        "versions": [],
    }
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            output.append(str(line, encoding='utf-8', errors="ignore"))
    for line in output:
        if("Publisher:" in line):
            appInfo["publisher"] = line.replace("Publisher:", "").strip()
        elif("Description:" in line):
            appInfo["description"] = line.replace("Description:", "").strip()
        elif("Author:" in line):
            appInfo["author"] = line.replace("Author:", "").strip()
        elif("Publisher:" in line):
            appInfo["publisher"] = line.replace("Publisher:", "").strip()
        elif("Homepage:" in line):
            appInfo["homepage"] = line.replace("Homepage:", "").strip()
        elif("License:" in line):
            appInfo["license"] = line.replace("License:", "").strip()
        elif("License Url:" in line):
            appInfo["license-url"] = line.replace("License Url:", "").strip()
        elif("SHA256:" in line):
            appInfo["installer-sha256"] = line.replace("SHA256:", "").strip()
        elif("Download Url:" in line):
            appInfo["installer-url"] = line.replace("Download Url:", "").strip()
        elif("Type:" in line):
            appInfo["installer-type"] = line.replace("Type:", "").strip()
    print(f"[   OK   ] Loading versions for {title}")
    p = subprocess.Popen([winget, "show", f"{title}", "--versions"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 2):
                output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                counter += 1
    appInfo["versions"] = output
    signal.emit(appInfo)
    
def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"[   OK   ] winget installer assistant thread started for process {p}")
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
    print(f"[   OK   ] winget installer assistant thread started for process {p}")
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
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__
