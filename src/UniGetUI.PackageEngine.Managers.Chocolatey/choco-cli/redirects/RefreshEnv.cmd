@echo off
REM Code generously provided by @beatcracker: https://github.com/beatcracker/detect-batch-subshell

setlocal EnableDelayedExpansion

REM Dequote path to command processor and this script path
set ScriptPath=%~0
set CmdPath=%COMSPEC:"=%

REM Get command processor filename and filename with extension
for %%c in (!CmdPath!) do (
    set CmdExeName=%%~nxc
    set CmdName=%%~nc
)

REM Get this process' PID
REM Adapted from: http://www.dostips.com/forum/viewtopic.php?p=22675#p22675
set "uid="
for /l %%i in (1 1 128) do (
    set /a "bit=!random!&1"
    set "uid=!uid!!bit!"
)

for /f "tokens=2 delims==" %%i in (
    'wmic Process WHERE "Name='!CmdExeName!' AND CommandLine LIKE '%%!uid!%%'" GET ParentProcessID /value'
) do (
    rem Get commandline of parent
    for /f "tokens=1,2,*" %%j in (
        'wmic Process WHERE "Handle='%%i'" GET CommandLine /value'
    ) do (

        rem Strip extra CR's from wmic output
        rem http://www.dostips.com/forum/viewtopic.php?t=4266
        for /f "delims=" %%x in ("%%l") do (
            rem Dequote path to batch file, if any (3rd argument)
            set ParentScriptPath=%%x
            set ParentScriptPath=!ParentScriptPath:"=!
        )

        rem Get parent process path
        for /f "tokens=2 delims==" %%y in ("%%j") do (
            rem Dequote parent path
            set ParentPath=%%y
            set ParentPath=!ParentPath:"=!

            rem Handle different invocations: C:\Windows\system32\cmd.exe , cmd.exe , cmd
            for %%p in (!CmdPath! !CmdExeName! !CmdName!) do (
                if !ParentPath!==%%p set IsCmdParent=1
            )

            rem Check if we're running in cmd.exe with /c switch and this script path as argument
            if !IsCmdParent!==1 if %%k==/c if "!ParentScriptPath!"=="%ScriptPath%" set IsExternal=1
        )
    )
)

if !IsExternal!==1 (
    echo %~nx0 does not work when run from this process. If you're in PowerShell, please 'Import-Module $env:ChocolateyInstall\helpers\chocolateyProfile.psm1' and try again.
    exit 1
)

REM End code from @beatcracker
@echo off
REM
REM RefreshEnv.cmd
REM
REM Batch file to read environment variables from registry and
REM set session variables to these values.
REM
REM With this batch file, there should be no need to reload command
REM environment every time you want environment changes to propagate

REM echo "RefreshEnv.cmd only works from cmd.exe, please install the Chocolatey Profile to take advantage of refreshenv from PowerShell"
echo | set /p dummy="Refreshing environment variables from registry for cmd.exe. Please wait..."

goto main

REM Set one environment variable from registry key
:SetFromReg
    "%WinDir%\System32\Reg" QUERY "%~1" /v "%~2" > "%TEMP%\_envset.tmp" 2>NUL
    for /f "usebackq skip=2 tokens=2,*" %%A IN ("%TEMP%\_envset.tmp") do (
        set "tempvar=%%B"
        REM Remove double quotes from temporary value
        set "__tempvar=!tempvar:"=!"

        REM Only escape percentage signs when the value type
        REM is not defined as an expandable string.
        if /I NOT "%%~A"=="REG_EXPAND_SZ" (
            set "tempvar=!tempvar:%%=%%%%!"
        )

        REM If the dequoted string differs from the original string, the variable contains double quotes.
        REM Escape the | and & in the variable to avoid errors.
        if NOT "!__tempvar!" == "!tempvar!" (
            set "tempvar=!tempvar:|=^|!"
            set "tempvar=!tempvar:&=^&!"
        )

        echo/set "%~3=!tempvar!"
    )
    goto :EOF

REM Get a list of environment variables from registry
:GetRegEnv
    "%WinDir%\System32\Reg" QUERY "%~1" > "%TEMP%\_envget.tmp"
    for /f "usebackq skip=2" %%A IN ("%TEMP%\_envget.tmp") do (
        if /I not "%%~A"=="Path" (
            call :SetFromReg "%~1" "%%~A" "%%~A"
        )
    )
    goto :EOF

:main
    echo/@echo off >"%TEMP%\_env.cmd"

    REM Slowly generating final file
    call :GetRegEnv "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" >> "%TEMP%\_env.cmd"
    call :GetRegEnv "HKCU\Environment">>"%TEMP%\_env.cmd" >> "%TEMP%\_env.cmd"

    REM Special handling for PATH - mix both User and System
    call :SetFromReg "HKLM\System\CurrentControlSet\Control\Session Manager\Environment" Path Path_HKLM >> "%TEMP%\_env.cmd"
    call :SetFromReg "HKCU\Environment" Path Path_HKCU >> "%TEMP%\_env.cmd"

    endlocal

    REM Caution: do not insert space-chars before >> redirection sign
    echo/set "Path=%%Path_HKLM%%;%%Path_HKCU%%" >> "%TEMP%\_env.cmd"

    REM Cleanup
    del /f /q "%TEMP%\_envset.tmp" 2>nul
    del /f /q "%TEMP%\_envget.tmp" 2>nul

    REM capture user / architecture
    SET "OriginalUserName=%USERNAME%"
    SET "OriginalArchitecture=%PROCESSOR_ARCHITECTURE%"

    REM Set these variables
    call "%TEMP%\_env.cmd"

    REM Cleanup
    del /f /q "%TEMP%\_env.cmd" 2>nul

    REM reset user / architecture
    SET "USERNAME=%OriginalUserName%"
    SET "PROCESSOR_ARCHITECTURE=%OriginalArchitecture%"

    echo | set /p dummy="Finished."
    echo .
