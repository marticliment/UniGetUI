from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _

from .PackageClasses import *

common_params = ["--source", "winget", "--accept-source-agreements"]

if getSettings("UseSystemWinget"):
    EXECUTABLE = "winget.exe"
else:
    EXECUTABLE = os.path.join(os.path.join(realpath, "winget-cli"), "winget.exe")

winget = EXECUTABLE

NAME = "Winget"
CAHCE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
CAHCE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

BLACKLISTED_PACKAGE_NAMES = [""]
BLACKLISTED_PACKAGE_IDS = [""]
BLACKLISTED_PACKAGE_VERSIONS = []


if not os.path.exists(CAHCE_FILE_PATH):
    os.makedirs(CAHCE_FILE_PATH)
    
def isEnabled() -> bool:
    return not getSettings(f"Disable{NAME}")

def getAvailablePackages_v2(second_attempt: bool = False) -> list[Package]:
    f"""
    Will retieve the cached packages for the package manager {NAME} in the format of a list[Package] object.
    If the cache is empty, will forcefully cache the packages and return a valid list[Package] object.
    Finally, it will start a background cacher thread.
    """
    print(f"🔵 Starting {NAME} search for available packages")
    try:
        packages: list[Package] = []
        if os.path.exists(CAHCE_FILE):
            f = open(CAHCE_FILE, "r", encoding="utf-8", errors="ignore")
            content = f.read()
            f.close()
            if content != "":
                print(f"🟢 Found valid, non-empty cache file for {NAME}!")
                for line in content.split("\n"):
                    package = line.split(",")
                    if len(package) >= 2:
                        packages.append(Package(package[0], package[1], package[2], NAME))
                Thread(target=cacheAvailablePackages_v2, daemon=True, name=f"{NAME} package cacher thread").start()
                print(f"🟢 {NAME} search for installed packages finished with {len(packages)} result(s)")
                return packages
            else:
                print(f"🟠 {NAME} cache file exists but is empty!")
                f.close()
                if second_attempt:
                    print(f"🔴 Could not load {NAME} packages, returning an empty list!")
                    return []
                cacheAvailablePackages_v2()
                return getAvailablePackages_v2(second_attempt = True)
        else:
            print(f"🟡 {NAME} cache file does not exist, creating cache forcefully and returning new package list")
            if second_attempt:
                print(f"🔴 Could not load {NAME} packages, returning an empty list!")
                return []
            cacheAvailablePackages_v2()
            return getAvailablePackages_v2(second_attempt = True)
    except Exception as e:
        report(e)
        return []
    
def cacheAvailablePackages_v2() -> None:
    """
    Internal method, should not be called manually externally.
    Will load the available packages and write them into the cache file
    """
    print(f"🔵 Starting {NAME} package caching")
    try:
        p = subprocess.Popen([NAME, "search", "", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
        ContentsToCache = ""
        hasShownId: bool = False
        idPosition: int = 0
        versionPosition: int = 0
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if not hasShownId:
                    if "Id" in line:
                        line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                        for char in ("\r", "/", "|", "\\", "-"):
                            line = line.split(char)[-1].strip()
                        hasShownId = True
                        idPosition = len(line.split("Id")[0])
                        versionPosition = len(line.split("Version")[0])
                elif "---" in line:
                    pass
                else:
                    try:
                        name = line[0:idPosition].strip()
                        idVersionSubstr = line[idPosition:].strip()
                        if "  " in name:
                            oName = name
                            while "  " in oName:
                                oName = oName.replace("  ", " ")
                            idVersionSubstr = oName.split(" ")[-1]+idVersionSubstr
                            name = " ".join(oName.split(" ")[:-1])
                        idVersionSubstr.replace("\t", " ")
                        while "  " in idVersionSubstr:
                            idVersionSubstr = idVersionSubstr.replace("  ", " ")
                        iOffset = 0
                        id = idVersionSubstr.split(" ")[iOffset]
                        ver = idVersionSubstr.split(" ")[iOffset+1]
                        if len(id) == 1:
                            iOffset + 1
                            id = idVersionSubstr.split(" ")[iOffset]
                            ver = idVersionSubstr.split(" ")[iOffset+1]
                        if ver.strip() in ("<", "-"):
                            iOffset += 1
                            ver = idVersionSubstr.split(" ")[iOffset+1]
                        if not "  " in name:
                            if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                                ContentsToCache += f"{name},{id},{ver}\n"
                        else:
                            if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                                name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                                while "##" in name:
                                    name = name.replace("##", "#")
                                print(f"🟡 package {name} failed parsing, going for method 2...")
                                ContentsToCache += f"{name},{id},{ver}\n"
                    except Exception as e:
                        ContentsToCache += f"{line[0:idPosition].strip()},{line[idPosition:versionPosition].strip()},{line[versionPosition:].strip()}\n"
                        if type(e) != IndexError:
                            report(e)
        AlreadyCachedPackages = ""
        try:
            if os.path.exists(CAHCE_FILE):
                f = open(CAHCE_FILE, "r", encoding="utf-8", errors="ignore")
                AlreadyCachedPackages = f.read()
                f.close()
        except Exception as e:
            report(e)
        for line in AlreadyCachedPackages.split("\n"):
            if line.split(",")[0] not in ContentsToCache:
                ContentsToCache += line + "\n"
        with open(CAHCE_FILE, "w", encoding="utf-8", errors="ignore") as f:
            f.write(ContentsToCache)
        print(f"🟢 {NAME} packages cached successfuly")
    except Exception as e:
        report(e)
        
def getAvailableUpdates_v2() -> list[UpgradablePackage]:
    f"""
    Will retieve the upgradable packages by {NAME} in the format of a list[UpgradablePackage] object.
    """
    print(f"🔵 Starting {NAME} search for updates")
    try:
        packages: list[UpgradablePackage] = []
        p = subprocess.Popen(["mode", "400,30&", EXECUTABLE, "upgrade", "--include-unknown", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        hasShownId: bool = False
        idPosition: int = 0
        versionPosition: int = 0
        newVerPosition: int = 0
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if not hasShownId:
                if "Id" in line:
                    line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                    for char in ("\r", "/", "|", "\\", "-"):
                        line = line.split(char)[-1].strip()
                    hasShownId = True
                    idPosition = len(line.split("Id")[0])
                    versionPosition = len(line.split("Version")[0])
                    newVerPosition = len(line.split("Available")[0])
                else:
                    pass
            elif "---" in line:
                pass
            else:
                element = line
                try:
                    verElement = element[idPosition:].strip()
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
                    name = element[0:idPosition].strip()
                    if not "  " in name:
                        if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(UpgradablePackage(name, id, ver, newver, NAME))
                    else:
                        name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                        while "##" in name:
                            name = name.replace("##", "#")
                        if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(UpgradablePackage(name.split("#")[0], name.split("#")[-1]+id, ver, newver, NAME))
                except Exception as e:
                    packages.append(UpgradablePackage(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:newVerPosition].split(" ")[0].strip(), element[newVerPosition:].split(" ")[0].strip(), NAME))
                    if type(e) != IndexError:
                        report(e)
        print(f"🟢 {NAME} search for updates finished with {len(packages)} result(s)")
        return packages
    except Exception as e:
        report(e)
        return []

def getInstalledPackages_v2() -> list[Package]:
    f"""
    Will retieve the intalled packages by {NAME} in the format of a list[Package] object.
    """
    print(f"🔵 Starting {NAME} search for installed packages")
    try:
        packages: list[Package] = []
        p = subprocess.Popen(["mode", "400,30&", EXECUTABLE, "list", "--source", "winget", "--accept-source-agreements"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        hasShownId: bool = False
        idPosition: int = 0
        versionPosition: int = 0
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if not hasShownId:
                if "Id" in line:
                    line = line.replace("\x08-\x08\\\x08|\x08 \r","")
                    for char in ("\r", "/", "|", "\\", "-"):
                        line = line.split(char)[-1].strip()
                    hasShownId = True
                    idPosition = len(line.split("Id")[0])
                    versionPosition = len(line.split("Version")[0])
                else:
                    pass
            elif "---" in line:
                pass
            else:
                element = line.replace("2010  x", "2010 x").replace("Microsoft.VCRedist.2010", " Microsoft.VCRedist.2010") # Fix an issue with MSVC++ 2010, where it shows with a double space (see https://github.com/marticliment/WingetUI#450)
                try:
                    verElement = element[idPosition:].strip()
                    verElement.replace("\t", " ")
                    while "  " in verElement:
                        verElement = verElement.replace("  ", " ")
                    iOffset = 0
                    id = " ".join(verElement.split(" ")[iOffset:-1])
                    ver = verElement.split(" ")[-1]
                    if len(id) == 1:
                        iOffset + 1
                        id = verElement.split(" ")[iOffset+0]
                        ver = verElement.split(" ")[iOffset+1]
                    if ver.strip() in ("<", "-"):
                        iOffset += 1
                        ver = verElement.split(" ")[iOffset+1]
                    name = element[0:idPosition].strip()
                    if not "  " in name:
                        if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(name, id, ver, NAME))
                    else:
                        if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                            print(f"🟡 package {name} failed parsing, going for method 2...")
                            name = name.replace("  ", "#").replace("# ", "#").replace(" #", "#")
                            while "##" in name:
                                name = name.replace("##", "#")
                            packages.append(Package(name.split("#")[0], name.split("#")[-1]+id, ver, NAME))
                except Exception as e:
                    packages.append(Package(element[0:idPosition].strip(), element[idPosition:versionPosition].strip(), element[versionPosition:].strip(), NAME))
                    if type(e) != IndexError:
                        report(e)
        print(f"🟢 {NAME} search for installed packages finished with {len(packages)} result(s)")
        return packages
    except Exception as e:
        report(e)
        return []
 
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
    os.chdir("..")
    import __init__