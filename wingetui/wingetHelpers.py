from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _


common_params = ["--source", "winget", "--accept-source-agreements"]


if getSettings("UseSystemWinget"):
    winget = "winget.exe"
else:
    winget = os.path.join(os.path.join(realpath, "winget-cli"), "winget.exe")


def searchForPackage(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"🟢 Starting winget search, winget on {winget}...")
    p = subprocess.Popen(["mode", "400,30&", winget, "search", ""] + common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
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
                verElement = element[idSeparator:].strip()
                verElement.replace("\t", " ")
                while "  " in verElement:
                    verElement = verElement.replace("  ", " ")
                iOffset = 0
                id = verElement.split(" ")[iOffset+0]
                try:
                    ver = verElement.split(" ")[iOffset+1]
                except IndexError:
                    ver = _("Unknown")
                if len(id)==1:
                    iOffset + 1
                    id = verElement.split(" ")[iOffset+0]
                    try:
                        ver = verElement.split(" ")[iOffset+1]
                    except IndexError:
                        ver = "Unknown"
                if ver.strip() in ("<", "-", ""):
                    iOffset += 1
                    ver = verElement.split(" ")[iOffset+1]
                if not "  " in element[0:idSeparator].strip():
                    signal.emit(element[0:idSeparator].strip(), id, ver, "Winget")
                else:
                    print(f"🟡 package {element[0:idSeparator].strip()} failed parsing, going for method 2...")
                    element = bytes(element, "utf-8")
                    print(element, verSeparator)
                    export = (element[0:idSeparator], str(element[idSeparator:], "utf-8").strip().split(" ")[0], list(filter(None, str(element[idSeparator:], "utf-8").strip().split(" ")))[1])
                    signal.emit(str(export[0], "utf-8").strip(), export[1], export[2], "Winget")
            except Exception as e:
                try:
                    report(e)
                    try:
                        element = str(element, "utf-8")
                    except Exception as e:
                        print(e)
                    signal.emit(element[0:idSeparator].strip(), element[idSeparator:verSeparator].strip(), element[verSeparator:].split(" ")[0].strip(), "Winget")
                except Exception as e:
                    report(e)
        print("🟢 Winget search finished")
        finishSignal.emit("winget")  # type: ignore

def searchForOnlyOnePackage(id: str) -> tuple[str, str]:
    print(f"🟢 Starting winget search, winget on {winget}...")
    p = subprocess.Popen(["mode", "400,30&", winget, "search", "--id", id.replace("…", "")] + common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 0):
                if not b"---" in line:
                    return str(line[:idSeparator], "utf-8", errors="ignore").strip(), str(line[idSeparator:], "utf-8", errors="ignore").split(" ")[0].strip()
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
    return id

def searchForUpdates(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    print(f"🟢 Starting winget search, winget on {winget}...")
    p = subprocess.Popen(["mode", "400,30&", winget, "upgrade", "--include-unknown"] + common_params[0:2], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
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
                if ver.strip() in ("<", "-"):
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
        finishSignal.emit("winget")

def searchForInstalledPackage(signal: Signal, finishSignal: Signal) -> None:
    print(f"🟢 Starting winget search, winget on {winget}...")
    p = subprocess.Popen(["mode", "400,30&", winget, "list"] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
    output = []
    counter = 0
    idSeparator = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 0 and not b"---" in line):
                output.append(line)
            else:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                for char in ("\r", "/", "|", "\\", "-"):
                    l = l.split(char)[-1].strip()
                if("Id" in l):
                    idSeparator = len(l.split("Id")[0])
                    verSeparator = len(l.split("Version")[0])
                    counter += 1
    counter = 0
    emptyStr = ""
    wingetName = "Winget"
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
            if len(id)==1:
                iOffset + 1
                id = verElement.split(" ")[iOffset+0]
                ver = verElement.split(" ")[iOffset+1]
            if ver.strip() in ("<", "-"):
                iOffset += 1
                ver = verElement.split(" ")[iOffset+1]
            if not "  " in element[0:idSeparator].strip():
                signal.emit(element[0:idSeparator].strip(), id, ver, wingetName)
            else:
                print(f"🟡 package {element[0:idSeparator].strip()} failed parsing, going for method 2...")
                print(element, verSeparator)
                name = element[0:idSeparator].strip().replace("  ", "#").replace("# ", "#").replace(" #", "#")
                while "##" in name:
                    name = name.replace("##", "#")
                signal.emit(name.split("#")[0], name.split("#")[-1]+id, ver, wingetName)
        except Exception as e:
            try:
                report(e)
                element = str(element, "utf-8")
                signal.emit(element[0:idSeparator].strip(), element[idSeparator:].strip(), emptyStr, wingetName)
            except Exception as e:
                report(e)
    print("🟢 Winget uninstallable packages search finished")
    finishSignal.emit("winget")

def getInfo(signal: Signal, title: str, id: str, useId: bool) -> None:
    try:
        oldid = id
        id = id.replace("…", "")
        oldtitle = title
        title = title.replace("…", "")
        if "…" in oldid:
            title, id = searchForOnlyOnePackage(oldid)
            oldid = id
            oldtitle = title
            useId = True
        elif "…" in oldtitle:
            title = searchForOnlyOnePackage(oldid)[0]
            oldtitle = title
        validCount = 0
        iterations = 0
        while validCount < 2 and iterations < 50:
            iterations += 1
            if useId:
                p = subprocess.Popen([winget, "show", "--id", f"{id}", "--exact"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
                print(f"🟢 Starting get info for id {id}")
            else:
                p = subprocess.Popen([winget, "show", "--name", f"{title}", "--exact"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
                print(f"🟢 Starting get info for title {title}")
            output = []
            unknownStr = _("Unknown")
            appInfo = {
                "title": oldtitle,
                "id": oldid,
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
                "manifest": f"https://github.com/microsoft/winget-pkgs/tree/master/manifests/{id[0].lower()}/{'/'.join(id.split('.'))}",
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
                if("Publisher:" in line):
                    appInfo["publisher"] = line.replace("Publisher:", "").strip()
                    validCount += 1
                elif("Description:" in line):
                    appInfo["description"] = line.replace("Description:", "").strip()
                    validCount += 1
                elif("Author:" in line):
                    appInfo["author"] = line.replace("Author:", "").strip()
                    validCount += 1
                elif("Publisher:" in line):
                    appInfo["publisher"] = line.replace("Publisher:", "").strip()
                    validCount += 1
                elif("Homepage:" in line):
                    appInfo["homepage"] = line.replace("Homepage:", "").strip()
                    validCount += 1
                elif("License:" in line):
                    appInfo["license"] = line.replace("License:", "").strip()
                    validCount += 1
                elif("License Url:" in line):
                    appInfo["license-url"] = line.replace("License Url:", "").strip()
                    validCount += 1
                elif("SHA256:" in line):
                    appInfo["installer-sha256"] = line.replace("SHA256:", "").strip()
                    validCount += 1
                elif("Download Url:" in line):
                    appInfo["installer-url"] = line.replace("Download Url:", "").strip()
                    validCount += 1
                elif("Release Date:" in line):
                    appInfo["updatedate"] = line.replace("Release Date:", "").strip()
                    validCount += 1
                elif("Release Notes Url:" in line):
                    url = line.replace("Release Notes Url:", "").strip()
                    appInfo["releasenotes"] = f"<a href={url} style='color:%bluecolor%'>{url}</a>"
                    validCount += 1
                elif("Type:" in line):
                    appInfo["installer-type"] = line.replace("Type:", "").strip()
        print(f"🟢 Loading versions for {title}")    
        if useId:
            p = subprocess.Popen([winget, "show", "--id", f"{id}", "-e", "--versions"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        else:
            p = subprocess.Popen([winget, "show", "--name",  f"{title}", "-e", "--versions"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        output = []
        counter = 0
        print(p.args)
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
    except Exception as e:
        report(e)
    
def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 winget installer assistant thread started for process {p}")
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
    print(f"🟢 winget installer assistant thread started for process {p}")
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