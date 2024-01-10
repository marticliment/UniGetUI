from pythonnet import set_runtime, load
from clr_loader import get_coreclr

rt = get_coreclr(runtime_config=r"C:\SomePrograms\WingetUI-Store\wingetui\Interface\CustomWidgets\WinUIWidgets\ModernWidgets.runtimeconfig.json")
set_runtime(rt)

#load("coreclr")

import clr

clr.AddReference(r"C:\SomePrograms\WingetUI-Store\wingetui\Interface\CustomWidgets\WinUIWidgets\ModernWidgets.dll")

import ModernWidgets

app_instance = ModernWidgets.Program()
