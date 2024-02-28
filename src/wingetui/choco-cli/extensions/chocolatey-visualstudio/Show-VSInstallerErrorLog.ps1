function Show-VSInstallerErrorLog
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [datetime] $Since,
        [string] $LogPath = $Env:TEMP # note: choco.exe adjusts TEMP to realTEMP\chocolatey
    )

    Write-Verbose "Examining Visual Studio Installer log files (${LogPath}\dd_*.log)"

    $installerLogs = @(Get-ChildItem -Path "${LogPath}\dd_installer_*.log" | Where-Object { $_.Length -gt 0 -and $_.LastWriteTime -gt $Since })
    foreach ($installerLog in $installerLogs)
    {
        $installerLogPath = $installerLog.FullName
        Write-Verbose "Found non-empty installer log: ${installerLogPath}"
        $interestingLines = @($installerLog `
            | Get-Content `
            | Select-String '^[^\s]+\s(Warning|Error)' `
            | Select-Object -ExpandProperty Line `
            | Where-Object { $_ -notlike '*Skipping non-applicable package*' })
        if (($interestingLines | Measure-Object).Count -gt 0)
        {
            Write-Warning "Errors/warnings from the Visual Studio Installer log file ${installerLogPath}:"
            $interestingLines | Write-Warning
        }
    }

    $errorLogs = @(Get-ChildItem -Path "${LogPath}\dd_*_errors.log" | Where-Object { $_.Length -gt 0 -and $_.LastWriteTime -gt $Since })
    foreach ($errorLog in $errorLogs)
    {
        $errorLogPath = $errorLog.FullName
        Write-Verbose "Found non-empty error log: ${errorLogPath}"
        $matchingFullLogPath = $errorLogPath -replace '_errors\.log$', '.log'
        if (Test-Path -Path $matchingFullLogPath)
        {
            $tailLines = 200 # determined empirically - should be enough to contain the interesting messages
            $fullLogTail = Get-Content -Tail $tailLines -Path $matchingFullLogPath
            Write-Verbose "Last $tailLines lines of Visual Studio Installer log file ${matchingFullLogPath}:"
            $fullLogTail | Write-Verbose
        }

        Write-Warning "Content of Visual Studio Installer error log file ${errorLogPath}:"
        $errorLog | Get-Content | Write-Warning
    }
}
