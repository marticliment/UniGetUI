from PySide6.QtCore import *
import subprocess, os, sys, re
from tools import *
from tools import _

ansi_escape = re.compile(r'\x1B\[[0-?]*[ -/]*[@-~]')

scoop = "powershell -ExecutionPolicy ByPass -Command scoop"


from .PackageClasses import *

EXECUTABLE = scoop
PACKAGE_MANAGER_NAME = "Scoop"
CAHCE_FILE = os.path.join(os.path.expanduser("~"), f".wingetui/cacheddata/{PACKAGE_MANAGER_NAME}CachedPackages")
CAHCE_FILE_PATH = os.path.join(os.path.expanduser("~"), ".wingetui/cacheddata")

BLACKLISTED_PACKAGE_NAMES = []
BLACKLISTED_PACKAGE_IDS = []
BLACKLISTED_PACKAGE_VERSIONS = []


if not os.path.exists(CAHCE_FILE_PATH):
    os.makedirs(CAHCE_FILE_PATH)

def getAvailablePackages_v2(second_attempt: bool = False) -> list[Package]:
    f"""
    Will retieve the cached packages for the package manager {PACKAGE_MANAGER_NAME} in the format of a list[Package] object.
    If the cache is empty, will forcefully cache the packages and return a valid list[Package] object.
    Finally, it will start a background cacher thread.
    """
    print(f"🔵 Starting {PACKAGE_MANAGER_NAME} search for available packages")
    try:
        packages: list[Package] = []
        if os.path.exists(CAHCE_FILE):
            f = open(CAHCE_FILE, "r", encoding="utf-8", errors="ignore")
            content = f.read()
            f.close()
            if content != "":
                print(f"🟢 Found valid, non-empty cache file for {PACKAGE_MANAGER_NAME}!")
                for line in content.split("\n"):
                    package = line.split(",")
                    if len(package) >= 4 and not package[0] in BLACKLISTED_PACKAGE_NAMES and not package[1] in BLACKLISTED_PACKAGE_IDS and not package[2] in BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(Package(package[0], package[1], package[2], package[3]))
                Thread(target=cacheAvailablePackages_v2, daemon=True, name=f"{PACKAGE_MANAGER_NAME} package cacher thread").start()
                print(f"🟢 {PACKAGE_MANAGER_NAME} search for installed packages finished with {len(packages)} result(s)")
                return packages
            else:
                print(f"🟠 {PACKAGE_MANAGER_NAME} cache file exists but is empty!")
                if second_attempt:
                    print(f"🔴 Could not load {PACKAGE_MANAGER_NAME} packages, returning an empty list!")
                    return []
                cacheAvailablePackages_v2()
                return getAvailablePackages_v2(second_attempt = True)
        else:
            print(f"🟡 {PACKAGE_MANAGER_NAME} cache file does not exist, creating cache forcefully and returning new package list")
            if second_attempt:
                print(f"🔴 Could not load {PACKAGE_MANAGER_NAME} packages, returning an empty list!")
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
    print(f"🔵 Starting {PACKAGE_MANAGER_NAME} package caching")
    try:
        p = subprocess.Popen(f"{PACKAGE_MANAGER_NAME} search", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
        ContentsToCache = ""
        DashesPassed = False
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if not DashesPassed:
                    if "----" in line:
                        DashesPassed = True
                else:
                    package = list(filter(None, line.split(" ")))
                    name = formatPackageIdAsName(package[0])
                    id = package[0]
                    version = package[1]
                    source = f"Scoop: {package[2].strip()}"
                    if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                        ContentsToCache += f"{name},{id},{version},{source}\n"
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
        print(f"🟢 {PACKAGE_MANAGER_NAME} packages cached successfuly")
    except Exception as e:
        report(e)
        
def getAvailableUpdates_v2() -> list[UpgradablePackage]:
    f"""
    Will retieve the upgradable packages by {PACKAGE_MANAGER_NAME} in the format of a list[UpgradablePackage] object.
    """
    print(f"🔵 Starting {PACKAGE_MANAGER_NAME} search for updates")
    try:
        packages: list[UpgradablePackage] = []
        p = subprocess.Popen(f"{EXECUTABLE} status", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        DashesPassed = False
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if not DashesPassed:
                    if "----" in line:
                        DashesPassed = True
                else:
                    package = list(filter(None, line.split(" ")))
                    name = formatPackageIdAsName(package[0])
                    id = package[0]
                    version = package[1]
                    newVersion = package[2]
                    source = PACKAGE_MANAGER_NAME
                    if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS and not newVersion in BLACKLISTED_PACKAGE_VERSIONS:
                        packages.append(UpgradablePackage(name, id, version, newVersion, source))
        print(f"🟢 {PACKAGE_MANAGER_NAME} search for updates finished with {len(packages)} result(s)")
        return packages
    except Exception as e:
        report(e)
        return []

def getInstalledPackages_v2() -> list[Package]:
    f"""
    Will retieve the intalled packages by {PACKAGE_MANAGER_NAME} in the format of a list[Package] object.
    """
    print(f"🔵 Starting {PACKAGE_MANAGER_NAME} search for installed packages")
    time.sleep(2)
    try:
        packages: list[Package] = []
        p = subprocess.Popen(f"{EXECUTABLE} list", stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ.copy(), shell=True)
        DashesPassed = False
        while p.poll() is None:
            line: str = str(p.stdout.readline().strip(), "utf-8", errors="ignore")
            if line:
                if not DashesPassed:
                    if "----" in line:
                        DashesPassed = True
                else:
                    package = list(filter(None, line.split(" ")))
                    if len(package) >= 3:
                        name = formatPackageIdAsName(package[0])
                        id = package[0]
                        version = package[1]
                        source = f"Scoop: {package[2].strip()}"
                        if not name in BLACKLISTED_PACKAGE_NAMES and not id in BLACKLISTED_PACKAGE_IDS and not version in BLACKLISTED_PACKAGE_VERSIONS:
                            packages.append(Package(name, id, version, source))
        print(f"🟢 {PACKAGE_MANAGER_NAME} search for installed packages finished with {len(packages)} result(s)")
        return packages
    except Exception as e:
        report(e)
        return []


def searchForPackage(signal: Signal, finishSignal: Signal) -> None:
    for r in getAvailablePackages_v2():
        signal.emit(r.Name, r.Id, r.Version, r.Source)
    finishSignal.emit("scoop")  

def searchForInstalledPackage(signal: Signal, finishSignal: Signal) -> None:
    for package in getInstalledPackages_v2():
        signal.emit(package.Name, package.Id, package.Version, package.Source)
    finishSignal.emit("scoop")

def searchForUpdates(signal: Signal, finishSignal: Signal) -> None:
    for package in getAvailableUpdates_v2():
        signal.emit(package.Name, package.Id, package.Version, package.NewVersion, package.Source)
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
