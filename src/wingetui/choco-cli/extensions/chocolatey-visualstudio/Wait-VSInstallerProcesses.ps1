function Wait-VSInstallerProcesses
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [ValidateSet('Wait', 'Fail')] $Behavior
    )

    $exitCode = $null

    Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Looking for still running VS installer processes' -f (Get-Date))
    $lazyQuitterProcessNames = @('vs_installershell', 'vs_installerservice')
    do
    {
        $lazyQuitterProcesses = Get-Process -Name $lazyQuitterProcessNames -ErrorAction SilentlyContinue | Where-Object { $null -ne $_ -and -not $_.HasExited }
        $lazyQuitterProcessCount = ($lazyQuitterProcesses | Measure-Object).Count
        if ($lazyQuitterProcessCount -gt 0)
        {
            try
            {
                Write-Debug "Found $lazyQuitterProcessCount still running Visual Studio installer processes which are known to exit asynchronously:"
                $lazyQuitterProcesses | Sort-Object -Property Name, Id | ForEach-Object { '[{0}] {1}' -f $_.Id, $_.Name } | Write-Debug
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Giving the processes some time to exit' -f (Get-Date))
                $lazyQuitterProcesses | Wait-Process -Timeout 1 -ErrorAction SilentlyContinue
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Looking for still running VS installer processes' -f (Get-Date))
            }
            finally
            {
                $lazyQuitterProcesses | ForEach-Object { $_.Dispose() }
                $lazyQuitterProcesses = $null
            }
        }
    }
    while ($lazyQuitterProcessCount -gt 0)

    # This sometimes happens when the VS installer is updated by the invoked bootstrapper.
    # The initial process exits, leaving another instance of the VS installer performing the actual install in the background.
    # This happens despite passing '--wait'.
    $vsInstallerProcessNames = @('vs_bootstrapper', 'vs_setup_bootstrapper', 'vs_installer', 'vs_installershell', 'vs_installerservice', 'setup')
    $vsInstallerProcessFilter = { $_.Name -ne 'setup' -or $_.Path -like '*\Microsoft Visual Studio\Installer*\setup.exe' }
    do
    {
        $vsInstallerProcesses = Get-Process -Name $vsInstallerProcessNames -ErrorAction SilentlyContinue | Where-Object { $null -ne $_ -and -not $_.HasExited } | Where-Object $vsInstallerProcessFilter
        $vsInstallerProcessCount = ($vsInstallerProcesses | Measure-Object).Count
        if ($vsInstallerProcessCount -gt 0)
        {
            try
            {
                Write-Warning "Found $vsInstallerProcessCount still running Visual Studio installer processes:"
                $vsInstallerProcesses | Sort-Object -Property Name, Id | ForEach-Object { '[{0}] {1}' -f $_.Id, $_.Name } | Write-Warning

                if ($Behavior -eq 'Fail')
                {
                    throw 'There are Visual Studio installer processes already running. Installation cannot continue.'
                }

                foreach ($p in $vsInstallerProcesses)
                {
                    [void] $p.Handle # make sure we get the exit code http://stackoverflow.com/a/23797762/266876
                }
                Write-Warning "Waiting for the processes to finish..."
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Waiting for the processes to finish' -f (Get-Date))
                $vsInstallerProcesses | Wait-Process -Timeout 60 -ErrorAction SilentlyContinue
                foreach ($proc in $vsInstallerProcesses)
                {
                    if (-not $proc.HasExited)
                    {
                        continue
                    }
                    if ($null -eq $exitCode)
                    {
                        $exitCode = $proc.ExitCode
                    }
                    Write-Debug ("[{0:yyyyMMdd HH:mm:ss.fff}] $($proc.Name) process $($proc.Id) exited with code '$($proc.ExitCode)'" -f (Get-Date))
                    if ($proc.ExitCode -ne 0 -and $null -ne $proc.ExitCode)
                    {
                        Write-Warning "$($proc.Name) process $($proc.Id) exited with code $($proc.ExitCode)"
                        if ($exitCode -eq 0)
                        {
                            $exitCode = $proc.ExitCode
                        }
                    }
                }

                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Looking for still running VS installer processes' -f (Get-Date))
            }
            finally
            {
                $vsInstallerProcesses | ForEach-Object { $_.Dispose() }
                $vsInstallerProcesses = $null
            }
        }
        else
        {
            Write-Debug 'Did not find any running VS installer processes.'
        }
    }
    while ($vsInstallerProcessCount -gt 0)

    # Not only does a process remain running after vs_installer /uninstall finishes, but that process
    # pops up a message box at end! Sheesh.
    Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Looking for vs_installer.windows.exe processes spawned by the uninstaller' -f (Get-Date))
    do
    {
        $uninstallerProcesses = Get-Process -Name 'vs_installer.windows' -ErrorAction SilentlyContinue | Where-Object { $null -ne $_ -and -not $_.HasExited }
        $uninstallerProcessesCount = ($uninstallerProcesses | Measure-Object).Count
        if ($uninstallerProcessesCount -gt 0)
        {
            try
            {
                if ($Behavior -eq 'Fail')
                {
                    Write-Warning "Found $uninstallerProcessesCount vs_installer.windows.exe process(es): $($uninstallerProcesses | Select-Object -ExpandProperty Id)"
                    throw 'There are Visual Studio installer processes already running. Installation cannot continue.'
                }

                Write-Debug "Found $uninstallerProcessesCount vs_installer.windows.exe process(es): $($uninstallerProcesses | Select-Object -ExpandProperty Id)"
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Waiting for all vs_installer.windows.exe processes to become input-idle' -f (Get-Date))
                foreach ($p in $uninstallerProcesses)
                {
                    [void] $p.Handle # make sure we get the exit code http://stackoverflow.com/a/23797762/266876
                    $waitSeconds = 60
                    try
                    {
                        $result = $p.WaitForInputIdle($waitSeconds * 1000)
                    }
                    catch [InvalidOperationException]
                    {
                        $result = $false
                    }

                    if ($result)
                    {
                        Write-Debug "Process $($p.Id) has reached input idle state"
                    }
                    else
                    {
                        Write-Debug "Process $($p.Id) has not reached input idle state after $waitSeconds seconds, continuing regardless"
                    }
                }
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Sending CloseMainWindow to all vs_installer.windows.exe processes' -f (Get-Date))
                foreach ($p in $uninstallerProcesses)
                {
                    $result = $p.CloseMainWindow()
                    if ($result)
                    {
                        Write-Debug "Successfully sent CloseMainWindow to process $($p.Id)"
                    }
                    else
                    {
                        Write-Debug "Failed to send CloseMainWindow to process $($p.Id), continuing regardless"
                    }
                }
                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Waiting for all vs_installer.windows.exe processes to exit' -f (Get-Date))
                $uninstallerProcesses | Wait-Process -Timeout 60 -ErrorAction SilentlyContinue
                foreach ($proc in $uninstallerProcesses)
                {
                    if (-not $proc.HasExited)
                    {
                        continue
                    }
                    if ($null -eq $exitCode)
                    {
                        $exitCode = $proc.ExitCode
                    }
                    Write-Debug ("[{0:yyyyMMdd HH:mm:ss.fff}] $($proc.Name) process $($proc.Id) exited with code '$($proc.ExitCode)'" -f (Get-Date))
                    if ($proc.ExitCode -ne 0 -and $null -ne $proc.ExitCode)
                    {
                        Write-Warning "$($proc.Name) process $($proc.Id) exited with code $($proc.ExitCode)"
                        if ($exitCode -eq 0)
                        {
                            $exitCode = $proc.ExitCode
                        }
                    }
                }

                Write-Debug ('[{0:yyyyMMdd HH:mm:ss.fff}] Looking for vs_installer.windows.exe processes spawned by the uninstaller' -f (Get-Date))
            }
            finally
            {
                $uninstallerProcesses | ForEach-Object { $_.Dispose() }
                $uninstallerProcesses = $null
            }
        }
        else
        {
            Write-Debug 'Did not find any running vs_installer.windows.exe processes.'
        }
    }
    while ($uninstallerProcessesCount -gt 0)

    return $exitCode
}
