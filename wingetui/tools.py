from PySide2 import QtCore
from threading import Thread
import sys, time


if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])

pending_programs = []
current_program = ""

app = None

def queueProgram(id: str):
    global pending_programs
    pending_programs.append(id)

def removeProgram(id: str):
    global pending_programs, current_program
    try:
        pending_programs.remove(id)
    except ValueError:
        pass
    if(current_program == id):
        current_program = ""

def checkQueue():
    global current_program, pending_programs
    print("[   OK   ] checkQueue Thread started!")
    while True:
        if(current_program == ""):
            try:
                current_program = pending_programs[0]
                print(f"[ THREAD ] Current program set to {current_program}")
            except IndexError:
                pass
        time.sleep(0.2)

Thread(target=checkQueue, daemon=True).start()

class KillableThread(Thread):
    def __init__(self, *args, **keywords): 
        super(KillableThread, self).__init__(*args, **keywords) 
        self.shouldBeRuning = True

    def start(self): 
        self._run = self.run 
        self.run = self.settrace_and_run
        Thread.start(self)

    def settrace_and_run(self): 
        sys.settrace(self.globaltrace) 
        self._run()

    def globaltrace(self, frame, event, arg): 
        return self.localtrace if event == 'call' else None

    def kill(self) -> None:
        self.shouldBeRuning = False
        
    def localtrace(self, frame, event, arg): 
        if not(self.shouldBeRuning) and event == 'line': 
            raise SystemExit()
            print("Killed")
        return self.localtrace 


def notify(title: str, text: str) -> None:
    app.trayIcon.showMessage(title, text)

def registerApplication(newApp):
    global app
    app = newApp