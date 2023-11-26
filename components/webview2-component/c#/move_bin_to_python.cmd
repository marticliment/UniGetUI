pushd bin\x64\Release
rmdir /Q /S WinFormsWebView.exe.WebView2
popd
rmdir /Q /S "..\Python Module\Component"
mkdir "..\Python Module\Component"
xcopy /S bin\x64\Release\* "..\src\pyside6webview2\Component"
pause