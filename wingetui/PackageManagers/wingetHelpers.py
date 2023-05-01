from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _


common_params = ["--source", "winget", "--accept-source-agreements"]

if getSettings("UseSystemWinget"):
    winget = "winget.exe"
else:
    winget = os.path.join(os.path.join(realpath, "PackageManagers/winget-cli"), "winget.exe")

def processElement(element: str, idSeparator: int, verSeparator: int) -> tuple[str]:
    """
    Will return a tuple made out of 4 strings.
    """
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
            return (element[0:idSeparator].strip(), id, ver, "Winget")
        else:
            element = bytes(element, "utf-8")
            export = (element[0:idSeparator], str(element[idSeparator:], "utf-8").strip().split(" ")[0], list(filter(None, str(element[idSeparator:], "utf-8").strip().split(" ")))[1])
            return (str(export[0], "utf-8").strip(), export[1], export[2], "Winget")
    except Exception as e:
        try:
            report(e)
            try:
                element = str(element, "utf-8")
            except Exception as e:
                print(e)
            return (element[0:idSeparator].strip(), element[idSeparator:verSeparator].strip(), element[verSeparator:].split(" ")[0].strip(), "Winget")
        except Exception as e:
            report(e)
    return ("", "", "", "")
   
def searchForPackage(signal: Signal, finishSignal: Signal, noretry: bool = False) -> None:
    try:
        print("🔵 Starting winget search")
        cacheFile = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata/wingetpackages")
        cachePath = os.path.dirname(cacheFile)
        correctCache = False
        NeedToCacheIdSeparator = True
        if not os.path.exists(cachePath):
            os.makedirs(cachePath)
        if os.path.exists(cacheFile):
            with open(cacheFile, "r", encoding='utf-8', errors="ignore") as f:
                content = f.read()
                if content != "":
                    print("🟢 Found valid cache for winget!")
                    for line in content.split("\n"):
                        if NeedToCacheIdSeparator:
                            line = line.split("\r")[-1]
                            if("Id" in line):
                                idSeparator = len(line.split("Id")[0])
                                verSeparator = idSeparator+2
                                i=0
                                while line.split("Id")[1].split(" ")[i] == "":
                                    verSeparator += 1
                                    i += 1
                                NeedToCacheIdSeparator = False
                        else:
                            r = processElement(line, idSeparator, verSeparator)
                            if r[0] and r[1]:
                                signal.emit(r[0], r[1], r[2], r[3])
                            correctCache = True
                    if correctCache:
                        finishSignal.emit("winget")
            
        print("🔵 Starting winget file update...")
        p = subprocess.Popen(["mode", "400,30&", winget, "search", ""] + common_params ,stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        output = ""
        oldcontents = ""
        NeedToCacheIdSeparator = True
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                l = str(line, encoding='utf-8', errors="ignore").replace("\x08-\x08\\\x08|\x08 \r","")
                output += l +"\n"
                if not correctCache:
                    if NeedToCacheIdSeparator:
                        l = l.split("\r")[-1]
                        if("Id" in l):
                            idSeparator = len(l.split("Id")[0])
                            verSeparator = idSeparator+2
                            i=0
                            while l.split("Id")[1].split(" ")[i] == "":
                                verSeparator += 1
                                i += 1
                            NeedToCacheIdSeparator = False
                    else:
                        r = processElement(l, idSeparator, verSeparator)
                        if r[0] and r[1]:
                            signal.emit(r[0], r[1], r[2], r[3])

        try:
            with open(cacheFile, "r", encoding="utf-8", errors="ignore") as f:
                oldcontents = f.read()
                f.close()
        except Exception as e:
            report(e)
        for line in oldcontents.split("\n"):
            if line.split(" ")[0] not in output:
                output += line + "\n"
        with open(cacheFile, "w", encoding="utf-8", errors="ignore") as f:
            f.write(output)
        finishSignal.emit("winget")  
        print("🟢 Winget cache rebuilt")
    except Exception as e:
        report(e)
        finishSignal.emit("winget")  

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
    return (id, id)

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
            element = element.replace("2010  x", "2010 x").replace("Microsoft.VCRedist.2010", " Microsoft.VCRedist.2010") # Fix an issue with MSVC++ 2010, where it shows with a double space (see https://github.com/marticliment/WingetUI#450)
            verElement = element[idSeparator:].strip()
            verElement.replace("\t", " ")
            while "  " in verElement:
                verElement = verElement.replace("  ", " ")
            iOffset = 0
            id = " ".join(verElement.split(" ")[iOffset:-1])
            ver = verElement.split(" ")[-1]
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

def getInfo(signal: Signal, title: str, id: str, useId: bool, progId: str) -> None:
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
            unknownStr = _("Not available")
            packageDetails = {
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
                "installer-size": "",
                "installer-type": unknownStr,
                "updatedate": unknownStr,
                "releasenotes": unknownStr,
                "releasenotesurl": unknownStr,
                "manifest": f"https://github.com/microsoft/winget-pkgs/tree/master/manifests/{id[0].lower()}/{'/'.join(id.split('.'))}",
                "versions": [],
                "architectures": ["x64", "x86", "arm64"],
                "scopes": [_("Current user"), _("Local machine")]
            }
            while p.poll() is None:
                line = p.stdout.readline()
                if line:
                    output.append(str(line, encoding='utf-8', errors="ignore"))
            weAreDescripting = False
            weAreReleaseNoting = False
            for line in output:
                if line[0] == " " and weAreDescripting:
                    packageDetails["description"] += "\n"+line
                else:
                    weAreDescripting = False
                if line[0] == " " and weAreReleaseNoting:
                    packageDetails["releasenotes"] += line + "<br>"
                else:
                    weAreReleaseNoting = False
                line: str = line.strip()
                if("Publisher:" in line):
                    packageDetails["publisher"] = line.replace("Publisher:", "").strip()
                    validCount += 1
                elif("Description:" in line):
                    packageDetails["description"] = line.replace("Description:", "").strip()
                    weAreDescripting = True
                    validCount += 1
                elif("Author:" in line):
                    packageDetails["author"] = line.replace("Author:", "").strip()
                    validCount += 1
                elif("Publisher:" in line):
                    packageDetails["publisher"] = line.replace("Publisher:", "").strip()
                    validCount += 1
                elif("Homepage:" in line):
                    packageDetails["homepage"] = line.replace("Homepage:", "").strip()
                    validCount += 1
                elif("License:" in line):
                    packageDetails["license"] = line.replace("License:", "").strip()
                    validCount += 1
                elif("License Url:" in line):
                    packageDetails["license-url"] = line.replace("License Url:", "").strip()
                    validCount += 1
                elif("Installer SHA256:" in line):
                    packageDetails["installer-sha256"] = line.replace("Installer SHA256:", "").strip()
                    validCount += 1
                elif("Installer Url:" in line):
                    url = line.replace("Installer Url:", "").strip()
                    packageDetails["installer-url"] = url
                    try:
                        packageDetails["installer-size"] = f"({int(urlopen(url).length/1000000)} MB)"
                    except Exception as e:
                        print("🟠 Can't get installer size:", type(e), str(e))
                    validCount += 1
                elif("Release Date:" in line):
                    packageDetails["updatedate"] = line.replace("Release Date:", "").strip()
                    validCount += 1
                elif("Release Notes Url:" in line):
                    url = line.replace("Release Notes Url:", "").strip()
                    packageDetails["releasenotesurl"] = f"<a href='{url}' style='color:%bluecolor%'>{url}</a>"
                    validCount += 1
                elif("Release Notes:" in line):
                    packageDetails["releasenotes"] = ""
                    weAreReleaseNoting = True
                    validCount += 1
                elif("Installer Type:" in line):
                    packageDetails["installer-type"] = line.replace("Installer Type:", "").strip()
        print(f"🟢 Loading versions for {title}")
        retryCount = 0
        output = []
        while output == [] and retryCount < 50:
            retryCount += 1
            if useId:
                p = subprocess.Popen([winget, "show", "--id", f"{id}", "-e", "--versions"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
            else:
                p = subprocess.Popen([winget, "show", "--name",  f"{title}", "-e", "--versions"]+common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
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
            print("Output: ")
        packageDetails["versions"] = output
        signal.emit(packageDetails, progId)
    except Exception as e:
        report(e)

def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 winget installer assistant thread started for process {p}")
    counter = RETURNCODE_OPERATION_SUCCEEDED
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
    match p.returncode:
        case 0x8A150011:
            outputCode = RETURNCODE_INCORRECT_HASH
        case 0x8A150109: # need restart
            outputCode = RETURNCODE_NEEDS_RESTART
        case other:
            outputCode = p.returncode
    if "No applicable upgrade found" in output:
        outputCode = RETURNCODE_NO_APPLICABLE_UPDATE_FOUND
    closeAndInform.emit(outputCode, output)

def uninstallAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 winget uninstaller assistant thread started for process {p}")
    counter = RETURNCODE_OPERATION_SUCCEEDED
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
    if "1603" in output or "0x80070005" in output or "Access is denied" in output:
        outputCode = RETURNCODE_NEEDS_ELEVATION
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__