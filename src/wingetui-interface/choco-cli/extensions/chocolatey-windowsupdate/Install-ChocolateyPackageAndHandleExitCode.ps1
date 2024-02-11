function Install-ChocolateyPackageAndHandleExitCode
{
    [CmdletBinding()]
    Param
    (
        # parameters of Install-ChocolateyPackage (slightly modified to be more strict)
        [parameter(Mandatory=$true)][string] $packageName,
        [parameter(Mandatory=$true)][alias("installerType","installType")][string] $fileType,
        [parameter(Mandatory=$false)][string[]] $silentArgs = '',
        [parameter(Mandatory=$false)][string] $url = '',
        [parameter(Mandatory=$false)][alias("url64")][string] $url64bit = '',
        [parameter(Mandatory=$false)][int[]] $validExitCodes = @(0),
        [parameter(Mandatory=$false)][string] $checksum = '',
        [parameter(Mandatory=$false)][string] $checksumType = '',
        [parameter(Mandatory=$false)][string] $checksum64 = '',
        [parameter(Mandatory=$false)][string] $checksumType64 = '',
        [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
        [parameter(Mandatory=$false)][alias("useOnlyPackageSilentArgs")][switch] $useOnlyPackageSilentArguments = $false,
        [parameter(Mandatory=$false)][switch] $useOriginalLocation,
        # new parameters
        [Parameter(Mandatory=$false)][ScriptBlock] $ExitCodeHandler,
        [Parameter(Mandatory=$false)][switch] $PassThru
    )
    Begin
    {
        Set-StrictMode -Version 2
        $ErrorActionPreference = 'Stop'
    }
    End
    {
        $exitCode = $null
        $invalidExitCodeErrorMessage = $null
        Set-StrictMode -Off
        try
        {
            # Start-ChocolateyProcessAsAdmin, invoked indirectly by Install-ChocolateyPackage,
            # overwrites a few arbitrary exit codes with 0. The only execution path
            # which faithfully preserves the original exit code is the error path.
            # Pass only 0 as a valid exit code and catch the error thrown when
            # the exit code is "invalid".
            $arguments = @{}
            $parametersToRemove = @('validExitCodes', 'ExitCodeHandler', 'PassThru')
            $PSBoundParameters.GetEnumerator() | Where-Object { $parametersToRemove -notcontains $_.Key } | ForEach-Object { $arguments[$_.Key] = $_.Value }
            Install-ChocolateyPackage `
                -validExitCodes @(0) `
                @arguments
        }
        catch [System.Management.Automation.RuntimeException]
        {
            Write-Debug "Caught $($_.Exception.GetType().FullName) with message = [$($_.Exception.Message)]"
            if ($_.Exception.Message -notmatch '(?s)Running\s+.+\s+was\s+not\s+successful.+Exit\s+code\s+was')
            {
                Write-Debug 'Exception message was not recognized, rethrowing'
                throw
            }

            Write-Debug 'Exception message was recognized as command execution failure with exit code.'
            $invalidExitCodeErrorMessage = $_.Exception.Message
        }
        catch
        {
            Write-Debug "Caught and rethrowing unexpected $($_.Exception.GetType().FullName) with message = [$($_.Exception.Message)]"
            throw
        }
        finally
        {
            Set-StrictMode -Version 2
        }

        $exitCode = Get-NativeInstallerExitCode
        if ($null -eq $exitCode -and $null -ne $invalidExitCodeErrorMessage)
        {
            # 0.10.1 "Running [`"$exeToRun`" $wrappedStatements] was not successful. Exit code was '$exitCode'. See log for possible error messages."
            # 0.9.10-rc1 "Running [`"$exeToRun`" $statements] was not successful. Exit code was '$exitCode'. See log for possible error messages."
            # 0.9.9-beta3 "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`' Error Message: $innerError."
            # 0.9.9-alpha "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`'."
            # 0.9.8.28-alpha2 - 0.9.8.33 "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`' Error Message: $innerError."
            # 0.9.8.17-alpha1 "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`'."
            # 0.9.8.16? "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`'."
            # earlier "[ERROR] Running $exeToRun with $statements was not successful."
            Write-Verbose 'Running on Chocolatey version which does not expose the native installer exit code (probably earlier than 0.9.10). Attempting to parse the exit code out of the error message.'
            Write-Verbose "Error message from Install-ChocolateyPackage: $invalidExitCodeErrorMessage"
            $rxExitCode = 'Running\ .+\ was\ not\ successful\.\ Exit\ code\ was\ ''(?<exitCode>-?\d+)'''
            if ($invalidExitCodeErrorMessage -match $rxExitCode)
            {
                $exitCodeString = $matches['exitCode']
                try
                {
                    $exitCode = [int]::Parse($exitCodeString)
                    Write-Verbose "Exit code determined from the error message: $exitCode"
                }
                catch
                {
                    Write-Verbose "Unable to parse the exit code string ($exitCodeString): $($_.Exception)"
                }
            }

            if ($null -eq $exitCode)
            {
                # are we running PowerShell Chocolatey?
                if ($null -ne $Env:ChocolateyInstall -and (Test-Path -Path (Join-Path -Path $Env:ChocolateyInstall -ChildPath 'chocolateyInstall\chocolatey.ps1')))
                {
                    Write-Warning 'This Chocolatey version does not provide a way to determine the installation result (exit code). Please upgrade to a newer version (at least 0.9.8.17).'
                }
                else
                {
                    Write-Warning 'Unable to determine the installation result (exit code). Please contact the maintainers of the ''chocolatey-windowsupdate.extension'' package.'
                }
            }
        }

        $shouldFail = $null -ne $exitCode -and ($validExitCodes | Measure-Object).Count -gt 0 -and $validExitCodes -notcontains $exitCode
        if ($null -eq $invalidExitCodeErrorMessage)
        {
            $errorMessage = "Installation of $packageName was not successful (exit code: $exitCode)."
        }
        else
        {
            $errorMessage = $invalidExitCodeErrorMessage
        }

        if ($null -ne $ExitCodeHandler)
        {
            $context = New-Object -TypeName PSObject -Property @{ ExitCode = $exitCode; ErrorMessage = $errorMessage; ShouldFailInstallation = $shouldFail }
            $_ = $context
            & $exitCodeHandler

            $shouldFail = $context.ShouldFailInstallation -eq $true
            $shouldGenerateErrorMessage = $false
            if ($null -ne $context.ExitCode -and $context.ExitCode -ne $exitCode)
            {
                $exitCode = $context.ExitCode
                Set-PowerShellExitCode -ExitCode $exitCode
                $shouldGenerateErrorMessage = $true
            }

            if ($null -ne $context.ErrorMessage -and $context.ErrorMessage -ne $errorMessage)
            {
                $errorMessage = $context.ErrorMessage
                $shouldGenerateErrorMessage = $false
            }

            if ($shouldGenerateErrorMessage)
            {
                $errorMessage = "Installation of $packageName was not successful (exit code: $exitCode)."
            }
        }

        if ($shouldFail)
        {
            throw $errorMessage
        }
        else
        {
            # prevent failure on PowerShell Chocolatey
            $failureLogPath = "$Env:TEMP\chocolatey\$packageName\failure.log"
            if (Test-Path -Path $failureLogPath)
            {
                Write-Verbose "Renaming file $failureLogPath so that Chocolatey does not treat the installation as failed"
                Rename-Item -Path $failureLogPath -NewName 'failure.old.log' -Force
            }
        }

        if ($PassThru)
        {
            $result = New-Object -TypeName PSObject -Property @{ ExitCode = $exitCode; ErrorMessage = $errorMessage }
            return $result
        }
    }
}
