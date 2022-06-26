@rem C:\Users\marti\scoop\apps\scoop\current\bin\scoop.ps1
@echo off
set mypath=%cd%
where /q pwsh.exe
if %errorlevel% equ 0 (
    pwsh -noprofile -ex unrestricted -file "%mypath%\sudoscript.ps1"  %*
) else (
    powershell -noprofile -ex unrestricted -file "%mypath%\sudoscript.ps1"  %*
)
