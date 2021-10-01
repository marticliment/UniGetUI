import sys, os, darkdetect, qtmodern.styles
from PySide2 import QtWidgets, QtCore, QtGui
import MainWindow, Tools

if hasattr(sys, 'frozen'):
    realpath = sys._MEIPASS
else:
    realpath = '/'.join(sys.argv[0].replace("\\", "/").split("/")[:-1])



debugging = True

if hasattr(QtCore.Qt, 'AA_EnableHighDpiScaling'):
    QtWidgets.QApplication.setAttribute(QtCore.Qt.AA_EnableHighDpiScaling, True)
if hasattr(QtCore.Qt, 'AA_UseHighDpiPixmaps'):
    QtWidgets.QApplication.setAttribute(QtCore.Qt.AA_UseHighDpiPixmaps, True)

class MainApplication(QtWidgets.QApplication):
    def __init__(self):
        try:
            super().__init__(sys.argv)
            print("[        ] Starting main application...")


            
            
            
            os.chdir(os.path.expanduser("~"))

            self.window = MainWindow.MainWindow()

            self.trayIcon = QtWidgets.QSystemTrayIcon()

            Tools.registerApplication(self)
            self.trayIcon.setIcon(QtGui.QIcon(realpath+"/icon.png"))
            self.trayIcon.setToolTip("WingetUI Store")
            self.trayIcon.setVisible(True)

            
            if(darkdetect.isDark()):
                qtmodern.styles.dark(self)
            else:
                qtmodern.styles.light(self)
            

        

            self.exec_()
        except Exception as e:
            if(debugging):
                raise e

MainApplication()