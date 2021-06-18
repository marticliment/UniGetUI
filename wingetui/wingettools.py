from PySide2 import QtCore
import subprocess, time


def searchForPackage(signal: QtCore.Signal, finishSignal: QtCore.Signal) -> None:
    print("[   OK   ] Starting internet search...")
    p = subprocess.Popen(["winget", "search", ""], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    output = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1):
                output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                counter += 1
    counter = 0
    for element in output:
        if(counter>=100):
            time.sleep(0.01)
            counter = 0
        else:
            counter += 1
        signal.emit(element[0:27].strip(), element[27:77].strip(), element[77:109].replace("Moniker:", "").strip())
    finishSignal.emit()

def getInfo(signal: QtCore.Signal, title: str, id: str, goodTitle: bool) -> None:
    if not(goodTitle):
        print(f"[   OK   ] Acquiring title for id \"{title}\"")
        p = subprocess.Popen(["winget", "search", f"{title}"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        output = []
        while p.poll() is None:
            line = p.stdout.readline()
            line = line.strip()
            if line:
                output.append(str(line, encoding='utf-8', errors="ignore"))
        title = output[-1][0:output[0].split("\r")[-1].index("Id")].strip()
    print(f"[   OK   ] Starting get info for title {title}")
    p = subprocess.Popen(["winget", "show", f"{title}"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
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
    p = subprocess.Popen(["winget", "show", f"{title}", "--versions"], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
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
    
def install(closeAndInform: QtCore.Signal, infoSignal: QtCore.Signal, title: str, version: list) -> None:
    print(f"[   OK   ] Starting installation title {title}")
    p = subprocess.Popen(["winget", "install", f"{title}"] + version, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    outputCode = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            infoSignal.emit(line)
            print(line)
            if("failed" in line):
                outputCode = 1
    print(outputCode)
    closeAndInform.emit(outputCode)


if(__name__=="__main__"):
    import __init__