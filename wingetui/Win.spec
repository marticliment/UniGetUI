# -*- mode: python ; coding: utf-8 -*-

block_cipher = None

import importlib, os

#package_imports = [['qtmodern', ['resources/frameless.qss', 'resources/style.qss']]]



a = Analysis(['launcher.py'],
             pathex=['WingetUI-Store\wingetui_bin'],
             binaries=[],
#             binaries=[('*.pyc', '.')],
#             datas=[('choco-cli/', 'choco-cli/'), ("components/", "components/"), ("Core/", "Core/"), ("ExternalLibraries/", "ExternalLibraries/"), ("Interface/", "Interface/"), ("PackageEngine/", "PackageEngine/"), ("resources/", "resources")],             hiddenimports=['pkg_resources.py2_warn', "win32gui", "cls"],
             datas=[('wingetui/', 'wingetui/')],
             hiddenimports=['pkg_resources.py2_warn', "win32gui", "clr", "ctypes", "os", "win32mica", "PySide6.QtWidgets", "PySide6.QtCore", "PySide6.QtGui", "pythonnet", "windows_toasts", "sys", "subprocess", "threading", "re", "socket", "flask", "flask_cors", "waitress", "urllib.request", "glob", "hashlib", "time", "faulthandler", "yaml"],
             hookspath=[],
             runtime_hooks=[],
             excludes=['eel', 'tkinter', "PyQt5", "PySide2", "pygame", "numpy", "matplotlib", "wingetui", "zroya"],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher,
             noarchive=False)


pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name='wingetui',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    contents_directory='.',
    codesign_identity=None,
    entitlements_file=None,
    icon="wingetui/resources/icon.ico",
    version="../wingetui-version-file"
)


coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='wingetuiBin',
)