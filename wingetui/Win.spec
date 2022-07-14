# -*- mode: python ; coding: utf-8 -*-

block_cipher = None

import importlib, os

#package_imports = [['qtmodern', ['resources/frameless.qss', 'resources/style.qss']]]



a = Analysis(['__init__.py'],
             pathex=['Y:\WingetUI-Store\wingetui'],
             binaries=[('MainWindow.py', '.'), ('Tabs.py', '.'), ('Tools.py', '.'), ('WingetTools.py', '.'), ('ScoopTools.py', '.')],
             datas=[('*.png', '.'), ('*.ico', '.'), ('*.cmd', '.'), ("winget-cli/", "winget-cli/"), ("sudo/", "sudo/")],
             hiddenimports=['pkg_resources.py2_warn'],
             hookspath=[],
             runtime_hooks=[],
             excludes=['eel', 'tkinter', "PyQt5"],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher,
             noarchive=False)


pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          a.binaries,
          a.zipfiles,
          a.datas,
          [],
          name='WingetUI',
          icon="icon.ico",
          debug=False,
          bootloader_ignore_signals=False,
          strip=False,
          upx=True,
          upx_exclude=[],
          runtime_tmpdir=None,
          console=False)
