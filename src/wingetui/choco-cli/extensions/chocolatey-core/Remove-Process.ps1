<#
.SYNOPSIS
    Ensure that process is stopped in reliable way

.DESCRIPTION
    Close the processes matching filters gracefully first, then forcefully.
    If no process is found, function will simply do and return nothing.

.EXAMPLE
    notepad; Remove-Process notepad -PathFilter 'system32'

    Close main window of notepad that has 'system32' word in its path

.EXAMPLE
    Remove-Process notepad -WaitFor 30
    notepad; notepad  #in another shell

    Close all instances of notepad but wait for them up to 30 seconds to start

.OUTPUTS
    Array of closeed processes with details about each one.

.NOTES
    https://github.com/chocolatey-community/chocolatey-coreteampackages/issues/1364
#>

function Remove-Process {
    param(
        # RegEx expression of process name, returned by Get-Process function
        [string] $NameFilter,

        # RegEx expression of process path, returned by Get-Process function
        [string] $PathFilter,

        # Wait for process to start number of seconds
        # Function will try to find process every second until timeout.
        [int] $WaitFor,

        # Close/Kill child processes, by default they are filtered out as
        # parent-child relationship usually have its own heartbeat feature
        [switch] $WithChildren
    )

    function getp {
        foreach ($p in Get-Process) {
            $b1 = if ($NameFilter) { $p.ProcessName -match $NameFilter }
            $b2 = if ($PathFilter) { $p.Path        -match $PathFilter }
            $b  = if (($b1 -ne $null) -and ($b2 -ne $null)) { $b1 -and $b2 } else { $b1 -or $b2 }
            if (!$b) { continue }

            $w = Get-WmiObject win32_process -Filter "ProcessId = $($p.Id)"
            [PSCustomObject]@{
                Id          = $p.Id
                ParentId    = $w.ParentProcessId
                Name        = $p.ProcessName
                Path        = $p.Path
                CommandLine = $w.CommandLine
                Process     = $p
                Username    = $w.GetOwner().Domain + "\"+ $w.GetOwner().User
            }
        }
    }

    $proc = getp
    if (!$proc -and $WaitFor) {
        Write-Verbose "Waiting for process up to $WaitFor seconds"
        for ($i=0; $i -lt $WaitFor; $i++) { Start-Sleep 1; $proc = getp; if ($proc) {break} }
    }
    if (!$proc) { return }

    # Process might spawn multiple children, typical for browsers; remove all children as parent will handle them
    if (!$WithChildren) {
        Write-Verbose "Remove all children processes"
        $proc = $proc | Where-Object { $proc.Id -notcontains $_.ParentId }
    }

    foreach ($p in $proc)  {
        Write-Verbose "Trying to close app '$($p.Name)' run by user '$($p.Username)'"

        if ( $p.Process.CloseMainWindow() ) {
            # wait for app to shut down for some time, max 5s
            for ($i=0; $i -lt 5; $i++) {
                Start-Sleep 1
                $p2 = Get-Process -PID $p.id -ea 0
                if (!$p2) { break }
            }
        }

        # Return value of CloseMainWindow() 'True' is not reliable
        # so if process is still active kill it
        $p2 = Get-Process -PID $p.id -ea 0
        if (($p.Process.Name -eq $p2.Name) -and ($p.Process.StartTime -eq $p2.StartTime)) {
            $p | Stop-Process -ea STOP
            Start-Sleep 1 # Running to fast here still gets the killed process in next line
        }

        $p2 = Get-Process -PID $p.id -ea 0
        if (($p.Process.Name -eq $p2.Name) -and ($p.Process.StartTime -eq $p2.StartTime)) {
            Write-Warning "Process '$($p.Name)' run by user '$($p.Username)' can't be closed"
        }
    }
    $proc
}
