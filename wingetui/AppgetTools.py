from PySide2 import QtCore
import subprocess, time, os, sys, signal

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])


appget_path = os.path.join(os.getenv("SystemDrive"), "/ProgramData/AppGet/bin/appget.exe")

appget_folder = os.path.join(os.getenv("SystemDrive"), "/ProgramData/AppGet/bin")


def searchForPackage(signal: QtCore.Signal, finishSignal: QtCore.Signal) -> None:
    print("[   OK   ] Starting appget search...")
    p = subprocess.Popen(' '.join([appget_path, "search", "*.*"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    alreadyIn = []
    counter = 0
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore")
        if line:
            if(counter > 4):
                program = line.split("| ")
                if not(program[1] in alreadyIn):
                    alreadyIn.append(program[1])
                    output.append(program)
            else:
                counter += 1
    counter = 0
    for element in output:
        signal.emit(element[2].strip(), element[1].strip(), element[3].strip(), "AppGet")
    print("[   OK   ] AppGet search finished")
    finishSignal.emit("appget")

def getInfo(signal: QtCore.Signal, title: str, id: str, goodTitle: bool) -> None:
    print(f"[   OK   ] Starting get info for title {id}")
    p = subprocess.Popen(' '.join([appget_path, "view", f"{id}"]), stdout=subprocess.PIPE, stderr=subprocess.STDOUT, stdin=subprocess.PIPE, cwd=os.getcwd(), env=os.environ, shell=True)
    output = []
    appInfo = {
        "title": title,
        "id": id,
        "publisher": "Unknown",
        "author": "Unknown",
        "description": "No description provided",
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
        if("home:" in line):
            appInfo["homepage"] = line.replace("home:", "").strip()
        elif("sha256:" in line):
            appInfo["installer-sha256"] = line.replace("sha256:", "").strip()
        elif("- location:" in line):
            appInfo["installer-url"] = line.replace("- location:", "").strip()
        elif("installMethod:" in line):
            appInfo["installer-type"] = line.replace("installMethod:", "").strip()
        elif("Loading package manifest from" in line):
            appInfo["manifest"] = line.replace("Loading package manifest from", "").strip()
        elif("version:" in line):
            appInfo["versions"] = [line.replace("version:", "").strip()]
        
    print(f"[  INFO  ] Appget does not support specific version installs")
    signal.emit(appInfo)
    
def installAssistant(p: subprocess.Popen, closeAndInform: QtCore.Signal, infoSignal: QtCore.Signal, counterSignal: QtCore.Signal) -> None:
    print(f"[   OK   ] appget installer assistant thread started for process {p}")
    outputCode = 1
    output = ""
    while p.poll() is None:
        line = p.stdout.readline()
        line = line.strip()
        line = str(line, encoding='utf-8', errors="ignore").strip()
        if line:
            if("Beginning installation of" in line):
                counterSignal.emit(1)
            elif("Checksum verification PASSED" in line):
                counterSignal.emit(4)
            elif("Installation completed successfully" in line):
                counterSignal.emit(6)
            infoSignal.emit(line)
            print(line)
            if("Installation completed successfully" in line):
                outputCode = 0
            output += line+"\n"
    print(outputCode)
    closeAndInform.emit(outputCode, output)



if(__name__=="__main__"):
    import __init__