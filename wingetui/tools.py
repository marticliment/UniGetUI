from PySide2 import QtCore
from threading import Thread
import sys

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

def notify(title: str, body: str) -> None:
    print(title, body)