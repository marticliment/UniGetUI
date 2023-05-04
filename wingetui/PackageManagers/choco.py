from PySide6.QtCore import *
import subprocess, time, os, sys
from tools import *
from tools import _


common_params = []


from .PackageClasses import *

if getSettings("UseSystemChocolatey"):
    EXECUTABLE = "choco.exe"
else:
    EXECUTABLE = os.path.join(os.path.join(realpath, "choco-cli"), "choco.exe")
    os.environ["chocolateyinstall"] = os.path.dirname(EXECUTABLE)

choco = EXECUTABLE


NAME = "Chocolatey"
CAHCE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{NAME}CachedPackages")
CAHCE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

BLACKLISTED_PACKAGE_NAMES =  ["Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output Is Package Name "]
BLACKLISTED_PACKAGE_IDS =  ["Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output is package name "]
BLACKLISTED_PACKAGE_VERSIONS =  ["Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "packages", "current version", "installed version"]


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
                    if len(package) >= 3 and not package[0] in BLACKLISTED_PACKAGE_NAMES and not package[1] in BLACKLISTED_PACKAGE_IDS and not package[2] in BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(formatPackageIdAsName(package[0]), package[1], package[2], NAME))
                Thread(target=cacheAvailablePackages_v2, daemon=True, name=f"{NAME} package cacher thread").start()
                print(f"🟢 {NAME} search for installed packages finished with {len(packages)} result(s)")
                return packages
            else:
                print(f"🟠 {NAME} cache file exists but is empty!")
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
    INTERNAL METHOD
    Will load the available packages and write them into the cache file
    """
    print(f"🔵 Starting {NAME} package caching")
    try:
        p = subprocess.Popen([NAME, "search", "*"] , stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, shell=True)
        ContentsToCache = ""
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if len(line.split(" ")) >= 2:
                    name = formatPackageIdAsName(line.split(" ")[0])
                    id = line.split(" ")[0]
                    version = line.split(" ")[1]
                    if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                        ContentsToCache += f"{name},{id},{version}\n"
        AlreadyCachedPackages = ""
        try:
            if os.path.exists(CAHCE_FILE):
                f = open(CAHCE_FILE, "r")
                AlreadyCachedPackages = f.read()
                f.close()
        except Exception as e:
            report(e)
        for line in AlreadyCachedPackages.split("\n"):
            if line.split(" ")[0] not in ContentsToCache:
                ContentsToCache += line + "\n"
        with open(CAHCE_FILE, "w") as f:
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
        p = subprocess.Popen([EXECUTABLE, "outdated"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                
                if len(line.split("|")) >= 3:
                    #Replace these lines with the parse mechanism
                    name = formatPackageIdAsName(line.split("|")[0])
                    id = line.split("|")[0]
                    version = line.split("|")[1]
                    newVersion = line.split("|")[2]
                    source = NAME
                else:
                    continue
                
                if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                    packages.append(UpgradablePackage(name, id, version, newVersion, source))
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
        p = subprocess.Popen([EXECUTABLE, "list", "--local-only"] , stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if len(line.split(" ")) >= 2:
                    name = formatPackageIdAsName(line.split(" ")[0])
                    id = line.split(" ")[0]
                    version = line.split(" ")[1]
                    source = NAME
                    if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(name, id, version, source))
        print(f"🟢 {NAME} search for installed packages finished with {len(packages)} result(s)")
        return packages
    except Exception as e:
        report(e)
        return []

def getInfo(signal: Signal, title: str, id: str, useId: bool, progId: bool) -> None:
    try:
        p = subprocess.Popen([choco, "info", id] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        print(f"🟢 Starting get info for id {id}")
        output = []
        unknownStr = _("Not available")
        packageDetails = {
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
            "installer-size": "",
            "installer-type": unknownStr,
            "updatedate": unknownStr,
            "releasenotes": unknownStr,
            "releasenotesurl": unknownStr,
            "manifest": f"https://community.chocolatey.org/packages/{id.lower()}",
            "versions": [],
            "architectures": ["x86"],
            "scopes": []
        }
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                output.append(str(line, encoding='utf-8', errors="ignore"))
        for line in output:
            if("Title:" in line):
                packageDetails["title"] = line.split("|")[0].replace("Title:", "").strip()
                packageDetails["updatedate"] = line.split("|")[1].replace("Published:", "").strip()
            elif("Author:" in line):
                packageDetails["author"] = line.replace("Author:", "").strip()
            elif("Software Site:" in line):
                packageDetails["homepage"] = line.replace("Software Site:", "").strip()
            elif("Software License:" in line):
                packageDetails["license-url"] = line.replace("Software License:", "").strip()
                packageDetails["license"] = ""
            elif("Package Checksum:" in line):
                packageDetails["installer-sha256"] = "<br>"+(line.replace("Package Checksum:", "").strip().replace("'", "").replace("(SHA512)", ""))
            elif("Description:" in line):
                packageDetails["description"] = line.replace("Description:", "").strip()
            elif("Release Notes" in line):
                url = line.replace("Release Notes:", "").strip()
                packageDetails["releasenotesurl"] = f"<a href='{url}' style='color:%bluecolor%'>{url}</a>"
        packageDetails["versions"] = []
        p = subprocess.Popen([choco, "find", "-e", id, "-a"] + common_params, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        print(f"🟢 Starting get info for id {id}")
        output = []
        while p.poll() is None:
            line = p.stdout.readline().strip()
            if line:
                output.append(str(line, encoding='utf-8', errors="ignore"))
        for line in output:
            if "[Approved]" in line:
                packageDetails["versions"].append(line.split(" ")[1])
        signal.emit(packageDetails, progId)
    except Exception as e:
        report(e)
    
def installAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 choco installer assistant thread started for process {p}")
    outputCode = RETURNCODE_OPERATION_SUCCEEDED
    counter = 0
    output = ""
    p.stdin = b"\r\n"
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
        outputCode = RETURNCODE_OPERATION_SUCCEEDED
    elif outputCode == 3010:
        outputCode = RETURNCODE_NEEDS_RESTART
    elif ("Run as administrator" in output or "The requested operation requires elevation" in output) and outputCode != 0:
        outputCode = RETURNCODE_NEEDS_ELEVATION
    closeAndInform.emit(outputCode, output)
 
def uninstallAssistant(p: subprocess.Popen, closeAndInform: Signal, infoSignal: Signal, counterSignal: Signal) -> None:
    print(f"🟢 choco installer assistant thread started for process {p}")
    outputCode = RETURNCODE_OPERATION_SUCCEEDED
    counter = 0
    output = ""
    p.stdin = b"\r\n"
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
    if outputCode in (1605, 1614, 1641):
        outputCode = RETURNCODE_OPERATION_SUCCEEDED
    elif outputCode == 3010:
        outputCode = RETURNCODE_NEEDS_RESTART
    elif "Run as administrator" in output or "The requested operation requires elevation" in output:
        outputCode = RETURNCODE_NEEDS_ELEVATION
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__