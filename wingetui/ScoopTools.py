from PySide2 import QtCore
import subprocess, time, os, sys, signal

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])


def searchForPackage(signal: QtCore.Signal, finishSignal: QtCore.Signal) -> None:
    print("[   OK   ] Starting scoop search...")
    p = subprocess.Popen(' '.join(["scoop", "search"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
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
        try:
            signal.emit(element.split("(")[0].strip().capitalize(), f"scoop.{element.split('(')[0].strip()}", element.split("(")[1].replace(")", "").strip(), "Scoop")
        except IndexError as e:
            print("IndexError: "+str(e))
    print("[   OK   ] Scoop search finished")
    finishSignal.emit("scoop")

def searchForInstalledPackage(signal: QtCore.Signal, finishSignal: QtCore.Signal) -> None:
    print("[   OK   ] Starting scoop search...")
    p = subprocess.Popen(' '.join(["scoop", "list"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    counter = 1
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            if(counter > 1):
                output.append(str(line, encoding='utf-8', errors="ignore"))
            else:
                counter += 1
    counter = 0
    print(output)
    for element in output:
        print(element)
        try:
            elList = element.split(" ")
            if(len(elList)>=2):
                if len(elList)==2:
                    provider = "scoop"
                else:
                    provider = f"Scoop ({elList[2].replace('[', '').replace(']', '')} bucket)"
                signal.emit(elList[0].capitalize(), f"scoop.{elList[0]}", elList[1], provider)
        except IndexError as e:
            print("IndexError: "+str(e))
    print("[   OK   ] Scoop search finished")
    finishSignal.emit("scoop")

def getInfo(signal: QtCore.Signal, title: str, id: str, goodTitle: bool) -> None:
    title = title.lower()
    print(f"[   OK   ] Starting get info for title {title}")
    p = subprocess.Popen(' '.join(["scoop", "info", f"{title}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
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
        "manifest": "Unknown",
        "versions": [],
    }
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        if line:
            output.append(str(line, encoding='utf-8', errors="ignore"))
    manifest = False
    version = ""
    for line in output:
        if("Description:" in line):
            appInfo["description"] = line.replace("Description:", "").strip()
        elif("Website:" in line):
            appInfo["homepage"] = line.replace("Website:", "").strip()
        elif("Version:" in line):
            version = line.replace("Version:", "").strip()
        elif("License:" in line):
            appInfo["license"] = line.replace("License:", "").strip().split("(")[0].strip()
            appInfo["license-url"] = line.replace("License:", "").strip().split("(")[1].strip().replace(")", "")
        elif("Manifest:" in line):
            manifest = True # This is because manifest path is in the following line.
        elif(manifest):
            manifest = False
            appInfo["manifest"] = line.strip()
    print(f"[  INFO  ] Scoop does not support specific version installs")
    appInfo["versions"] = [version]
    signal.emit(appInfo)
    
def installAssistant(p: subprocess.Popen, closeAndInform: QtCore.Signal, infoSignal: QtCore.Signal, counterSignal: QtCore.Signal) -> None:
    print(f"[   OK   ] scoop installer assistant thread started for process {p}")
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
            print(line)
            if("was installed successfully" in line):
                outputCode = 0
            elif ("is already installed" in line):
                outputCode = 0
            output += line+"\n"
    print(outputCode)
    closeAndInform.emit(outputCode, output)

   
def uninstallAssistant(p: subprocess.Popen, closeAndInform: QtCore.Signal, infoSignal: QtCore.Signal, counterSignal: QtCore.Signal) -> None:
    print(f"[   OK   ] scoop uninstaller assistant thread started for process {p}")
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
            print(line)
            if("was uninstalled" in line):
                outputCode = 0
            output += line+"\n"
    print(outputCode)
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__
