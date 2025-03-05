# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Start-ChocolateyProcessAsAdmin {
    <#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Runs a process with administrative privileges. If `-ExeToRun` is not
specified, it is run with PowerShell.

.NOTES
This command will assert UAC/Admin privileges on the machine.

Will automatically call Set-PowerShellExitCode to set the package exit
code in the following ways:

- 4 if the binary turns out to be a text file.
- The same exit code returned from the process that is run. If a 3010 is returned, it will set 3010 for the package.

Aliases `Start-ChocolateyProcess` and `Invoke-ChocolateyProcess`.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Statements
Arguments to pass to `ExeToRun` or the PowerShell script block to be
run.

.PARAMETER ExeToRun
The executable/application/installer to run. Defaults to `'powershell'`.

.PARAMETER Elevated
Indicate whether the process should run elevated.

.PARAMETER Minimized
Switch indicating if a Windows pops up (if not called with a silent
argument) that it should be minimized.

.PARAMETER NoSleep
Used only when calling PowerShell - indicates the window that is opened
should return instantly when it is complete.

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

.PARAMETER WorkingDirectory
The working directory for the running process. Defaults to
`Get-Location`. If current location is a UNC path, uses
`$env:TEMP` for default.

.PARAMETER SensitiveStatements
Arguments to pass to  `ExeToRun` that are not logged.

Note that only licensed versions of Chocolatey provide a way to pass
those values completely through without having them in the install
script or on the system in some way.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$msiArgs" -ExeToRun 'msiexec'

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file -ValidExitCodes @(0,21)

.EXAMPLE
>
# Run PowerShell statements
$psFile = Join-Path "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" 'someInstall.ps1'
Start-ChocolateyProcessAsAdmin "& `'$psFile`'"

.EXAMPLE
# This also works for cmd and is required if you have any spaces in the paths within your command
$appPath = "$env:ProgramFiles\myapp"
$cmdBatch = "/c `"$appPath\bin\installmyappservice.bat`""
Start-ChocolateyProcessAsAdmin $cmdBatch cmd
# or more explicitly
Start-ChocolateyProcessAsAdmin -Statements $cmdBatch -ExeToRun "cmd.exe"

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage
#>
    param(
        [parameter(Mandatory = $false, Position = 0)][string[]] $statements,
        [parameter(Mandatory = $false, Position = 1)][string] $exeToRun = 'powershell',
        [parameter(Mandatory = $false)][switch] $elevated = $true,
        [parameter(Mandatory = $false)][switch] $minimized,
        [parameter(Mandatory = $false)][switch] $noSleep,
        [parameter(Mandatory = $false)] $validExitCodes = @(0),
        [parameter(Mandatory = $false)][string] $workingDirectory = $null,
        [parameter(Mandatory = $false)][string] $sensitiveStatements = '',
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )
    [string]$statements = $statements -join ' '

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    if ($workingDirectory -eq $null) {
        $pwd = $(Get-Location -PSProvider 'FileSystem')
        if ($pwd -eq $null -or $pwd.ProviderPath -eq $null) {
            Write-Debug "Unable to use current location for Working Directory. Using Cache Location instead."
            $workingDirectory = $env:TEMP
        }
        $workingDirectory = $pwd.ProviderPath
    }

    $alreadyElevated = $false
    if (Test-ProcessAdminRights) {
        $alreadyElevated = $true
    }

    if ($elevated -and -not $alreadyElevated -and $env:ChocolateyAttemptedElevation) {
        $env:ChocolateyAttemptedElevation = [string]::Empty
        throw "Elevation attempt failed. Aborting..."
    }

    $dbMessagePrepend = "Elevating permissions and running"
    if (!$elevated) {
        $dbMessagePrepend = "Running"
    }

    try {
        if ($exeToRun -ne $null) {
            $exeToRun = $exeToRun -replace "`0", ""
        }
        if ($statements -ne $null) {
            $statements = $statements -replace "`0", ""
        }
    }
    catch {
        Write-Debug "Removing null characters resulted in an error - $($_.Exception.Message)"
    }

    if ($exeToRun -ne $null) {
        $exeToRun = $exeToRun.Trim().Trim("'").Trim('"')
    }

    $wrappedStatements = $statements
    if ($wrappedStatements -eq $null) {
        $wrappedStatements = ''
    }

    if ($exeToRun -eq 'powershell') {
        $exeToRun = "$($env:SystemRoot)\System32\WindowsPowerShell\v1.0\powershell.exe"
        $importChocolateyHelpers = "& import-module -name '$helpersPath\chocolateyInstaller.psm1' -Verbose:`$false | Out-Null;"
        $block = @"
      `$noSleep = `$$noSleep
      #`$env:ChocolateyEnvironmentDebug='false'
      #`$env:ChocolateyEnvironmentVerbose='false'
      $importChocolateyHelpers
      try{
        `$progressPreference="SilentlyContinue"
        $statements
        if(!`$noSleep){start-sleep 6}
      }
      catch{
        if(!`$noSleep){start-sleep 8}
        throw
      }
"@
        $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($block))
        $wrappedStatements = "-NoLogo -NonInteractive -NoProfile -ExecutionPolicy Bypass -InputFormat Text -OutputFormat Text -EncodedCommand $encoded"
        $dbgMessage = @"
$dbMessagePrepend powershell block:
$block
This may take a while, depending on the statements.
"@
    }
    else {
        $dbgMessage = @"
$dbMessagePrepend [`"$exeToRun`" $wrappedStatements]. This may take a while, depending on the statements.
"@
    }

    Write-Debug $dbgMessage

    $exeIsTextFile = [System.IO.Path]::GetFullPath($exeToRun) + ".istext"
    if ([System.IO.File]::Exists($exeIsTextFile)) {
        Set-PowerShellExitCode 4
        throw "The file was a text file but is attempting to be run as an executable - '$exeToRun'"
    }

    if ($exeToRun -eq 'msiexec' -or $exeToRun -eq 'msiexec.exe') {
        $exeToRun = "$($env:SystemRoot)\System32\msiexec.exe"
    }

    if (!([System.IO.File]::Exists($exeToRun)) -and $exeToRun -notmatch 'msiexec') {
        Write-Warning "May not be able to find '$exeToRun'. Please use full path for executables."
        # until we have search paths enabled, let's just pass a warning
        #Set-PowerShellExitCode 2
        #throw "Could not find '$exeToRun'"
    }

    # Redirecting output slows things down a bit.
    $writeOutput = {
        if ($EventArgs.Data -ne $null) {
            Write-Verbose "$($EventArgs.Data)"
        }
    }

    $writeError = {
        if ($EventArgs.Data -ne $null) {
            Write-Error "$($EventArgs.Data)"
        }
    }

    $process = New-Object System.Diagnostics.Process
    $process.EnableRaisingEvents = $true
    Register-ObjectEvent -InputObject $process -SourceIdentifier "LogOutput_ChocolateyProc" -EventName OutputDataReceived -Action $writeOutput | Out-Null
    Register-ObjectEvent -InputObject $process -SourceIdentifier "LogErrors_ChocolateyProc" -EventName ErrorDataReceived -Action  $writeError | Out-Null

    #$process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($exeToRun, $wrappedStatements)
    # in case empty args makes a difference, try to be compatible with the older
    # version
    $psi = New-Object System.Diagnostics.ProcessStartInfo

    $psi.FileName = $exeToRun
    if ($wrappedStatements -ne '') {
        $psi.Arguments = "$wrappedStatements"
    }
    if ($sensitiveStatements -ne $null -and $sensitiveStatements -ne '') {
        Write-Host "Sensitive arguments have been passed. Adding to arguments."
        $psi.Arguments += " $sensitiveStatements"
    }
    $process.StartInfo = $psi

    # process start info
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.WorkingDirectory = $workingDirectory

    if ($elevated -and -not $alreadyElevated -and [Environment]::OSVersion.Version -ge (New-Object 'Version' 6, 0)) {
        # this doesn't actually currently work - because we are not running under shell execute
        Write-Debug "Setting RunAs for elevation"
        $process.StartInfo.Verb = "RunAs"
    }
    if ($minimized) {
        $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized
    }

    try {
        if ($elevated -and -not $alreadyElevated) {
            # Set this in case of recursive calls into Start-ChocolateyProcessAsAdmin
            $env:ChocolateyAttemptedElevation = "true"
        }

        $process.Start() | Out-Null
        if ($process.StartInfo.RedirectStandardOutput) {
            $process.BeginOutputReadLine()
        }
        if ($process.StartInfo.RedirectStandardError) {
            $process.BeginErrorReadLine()
        }
        $process.WaitForExit()

        # For some reason this forces the jobs to finish and waits for
        # them to do so. Without this it never finishes.
        Unregister-Event -SourceIdentifier "LogOutput_ChocolateyProc"
        Unregister-Event -SourceIdentifier "LogErrors_ChocolateyProc"

        # sometimes the process hasn't fully exited yet.
        for ($loopCount = 1; $loopCount -le 15; $loopCount++) {
            if ($process.HasExited) {
                break;
            }
            Write-Debug "Waiting for process to exit - $loopCount/15 seconds";
            Start-Sleep 1;
        }

        $exitCode = $process.ExitCode
        $process.Dispose()
    }
    finally {
        # Unset the value so that any further invocations do not get confused.
        $env:ChocolateyAttemptedElevation = [string]::Empty
    }

    Write-Debug "Command [`"$exeToRun`" $wrappedStatements] exited with `'$exitCode`'."

    $exitErrorMessage = ''
    $errorMessageAddendum = " This is most likely an issue with the '$env:chocolateyPackageName' package and not with Chocolatey itself. Please follow up with the package maintainer(s) directly."

    switch ($exitCode) {
        0 {
            break
        }
        1 {
            break
        }
        3010 {
            break
        }
        # NSIS - http://nsis.sourceforge.net/Docs/AppendixD.html
        # InnoSetup - http://www.jrsoftware.org/ishelp/index.php?topic=setupexitcodes
        2 {
            $exitErrorMessage = 'Setup was cancelled.'; break
        }
        3 {
            $exitErrorMessage = 'A fatal error occurred when preparing or moving to next install phase. Check to be sure you have enough memory to perform an installation and try again.'; break
        }
        4 {
            $exitErrorMessage = 'A fatal error occurred during installation process.' + $errorMessageAddendum; break
        }
        5 {
            $exitErrorMessage = 'User (you) cancelled the installation.'; break
        }
        6 {
            $exitErrorMessage = 'Setup process was forcefully terminated by the debugger.'; break
        }
        7 {
            $exitErrorMessage = 'While preparing to install, it was determined setup cannot proceed with the installation. Please be sure the software can be installed on your system.'; break
        }
        8 {
            $exitErrorMessage = 'While preparing to install, it was determined setup cannot proceed with the installation until you restart the system. Please reboot and try again.'; break
        }
        # MSI - https://msdn.microsoft.com/en-us/library/windows/desktop/aa376931.aspx
        1602 {
            $exitErrorMessage = 'User (you) cancelled the installation.'; break
        }
        1603 {
            $exitErrorMessage = "Generic MSI Error. This is a local environment error, not an issue with a package or the MSI itself - it could mean a pending reboot is necessary prior to install or something else (like the same version is already installed). Please see MSI log if available. If not, try again adding `'--install-arguments=`"`'/l*v c:\$($env:chocolateyPackageName)_msi_install.log`'`"`'. Then search the MSI Log for `"Return Value 3`" and look above that for the error."; break
        }
        1618 {
            $exitErrorMessage = 'Another installation currently in progress. Try again later.'; break
        }
        1619 {
            $exitErrorMessage = 'MSI could not be found - it is possibly corrupt or not an MSI at all. If it was downloaded and the MSI is less than 30K, try opening it in an editor like Notepad++ as it is likely HTML.' + $errorMessageAddendum; break
        }
        1620 {
            $exitErrorMessage = 'MSI could not be opened - it is possibly corrupt or not an MSI at all. If it was downloaded and the MSI is less than 30K, try opening it in an editor like Notepad++ as it is likely HTML.' + $errorMessageAddendum; break
        }
        1622 {
            $exitErrorMessage = 'Something is wrong with the install log location specified. Please fix this in the package silent arguments (or in install arguments you specified). The directory specified as part of the log file path must exist for an MSI to be able to log to that directory.' + $errorMessageAddendum; break
        }
        1623 {
            $exitErrorMessage = 'This MSI has a language that is not supported by your system. Contact package maintainer(s) if there is an install available in your language and you would like it added to the packaging.'; break
        }
        1625 {
            $exitErrorMessage = 'Installation of this MSI is forbidden by system policy. Please contact your system administrators.'; break
        }
        1632 {
            $exitErrorMessage = 'Installation of this MSI is not supported on this platform. Contact package maintainer(s) if you feel this is in error or if you need an architecture that is not available with the current packaging.'; break
        }
        1633 {
            $exitErrorMessage = 'Installation of this MSI is not supported on this platform. Contact package maintainer(s) if you feel this is in error or if you need an architecture that is not available with the current packaging.'; break
        }
        1638 {
            $exitErrorMessage = 'This MSI requires uninstall prior to installing a different version. Please ask the package maintainer(s) to add a check in the chocolateyInstall.ps1 script and uninstall if the software is installed.' + $errorMessageAddendum; break
        }
        1639 {
            $exitErrorMessage = 'The command line arguments passed to the MSI are incorrect. If you passed in additional arguments, please adjust. Otherwise followup with the package maintainer(s) to get this fixed.' + $errorMessageAddendum; break
        }
        1640 {
            $exitErrorMessage = 'Cannot install MSI when running from remote desktop (terminal services). This should automatically be handled in licensed editions. For open source editions, you may need to run change.exe prior to running Chocolatey or not use terminal services.'; break
        }
        1645 {
            $exitErrorMessage = 'Cannot install MSI when running from remote desktop (terminal services). This should automatically be handled in licensed editions. For open source editions, you may need to run change.exe prior to running Chocolatey or not use terminal services.'; break
        }
    }

    if ($exitErrorMessage) {
        $errorMessageSpecific = "Exit code indicates the following: $exitErrorMessage."
        Write-Warning $exitErrorMessage
    }
    else {
        $errorMessageSpecific = 'See log for possible error messages.'
    }

    if ($validExitCodes -notcontains $exitCode) {
        Set-PowerShellExitCode $exitCode
        throw "Running [`"$exeToRun`" $wrappedStatements] was not successful. Exit code was '$exitCode'. $($errorMessageSpecific)"
    }
    else {
        $chocoSuccessCodes = @(0, 1605, 1614, 1641, 3010)
        if ($chocoSuccessCodes -notcontains $exitCode) {
            Write-Warning "Exit code '$exitCode' was considered valid by script, but not as a Chocolatey success code. Returning '0'."
            $exitCode = 0
        }
    }

    Write-Debug "Finishing '$($MyInvocation.InvocationName)'"

    return $exitCode
}

Set-Alias Start-ChocolateyProcess Start-ChocolateyProcessAsAdmin
Set-Alias Invoke-ChocolateyProcess Start-ChocolateyProcessAsAdmin

# SIG # Begin signature block
# MIInKwYJKoZIhvcNAQcCoIInHDCCJxgCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCBkscKuwgb3Y8/Q
# sjo3E9b3jZgzhxiCIfXOeQYd4jnjS6CCIK4wggWNMIIEdaADAgECAhAOmxiO+dAt
# 5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBa
# Fw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lD
# ZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoC
# ggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3E
# MB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKy
# unWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsF
# xl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU1
# 5zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJB
# MtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObUR
# WBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6
# nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxB
# YKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5S
# UUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+x
# q4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIB
# NjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwP
# TzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMC
# AYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdp
# Y2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNv
# bS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENB
# LmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0Nc
# Vec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnov
# Lbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65Zy
# oUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFW
# juyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPF
# mCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9z
# twGpn1eqXijiuZQwggauMIIElqADAgECAhAHNje3JFR82Ees/ShmKl5bMA0GCSqG
# SIb3DQEBCwUAMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRy
# dXN0ZWQgUm9vdCBHNDAeFw0yMjAzMjMwMDAwMDBaFw0zNzAzMjIyMzU5NTlaMGMx
# CzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMy
# RGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNIQTI1NiBUaW1lU3RhbXBpbmcg
# Q0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDGhjUGSbPBPXJJUVXH
# JQPE8pE3qZdRodbSg9GeTKJtoLDMg/la9hGhRBVCX6SI82j6ffOciQt/nR+eDzMf
# UBMLJnOWbfhXqAJ9/UO0hNoR8XOxs+4rgISKIhjf69o9xBd/qxkrPkLcZ47qUT3w
# 1lbU5ygt69OxtXXnHwZljZQp09nsad/ZkIdGAHvbREGJ3HxqV3rwN3mfXazL6IRk
# tFLydkf3YYMZ3V+0VAshaG43IbtArF+y3kp9zvU5EmfvDqVjbOSmxR3NNg1c1eYb
# qMFkdECnwHLFuk4fsbVYTXn+149zk6wsOeKlSNbwsDETqVcplicu9Yemj052FVUm
# cJgmf6AaRyBD40NjgHt1biclkJg6OBGz9vae5jtb7IHeIhTZgirHkr+g3uM+onP6
# 5x9abJTyUpURK1h0QCirc0PO30qhHGs4xSnzyqqWc0Jon7ZGs506o9UD4L/wojzK
# QtwYSH8UNM/STKvvmz3+DrhkKvp1KCRB7UK/BZxmSVJQ9FHzNklNiyDSLFc1eSuo
# 80VgvCONWPfcYd6T/jnA+bIwpUzX6ZhKWD7TA4j+s4/TXkt2ElGTyYwMO1uKIqjB
# Jgj5FBASA31fI7tk42PgpuE+9sJ0sj8eCXbsq11GdeJgo1gJASgADoRU7s7pXche
# MBK9Rp6103a50g5rmQzSM7TNsQIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB
# /wIBADAdBgNVHQ4EFgQUuhbZbU2FL3MpdpovdYxqII+eyG8wHwYDVR0jBBgwFoAU
# 7NfjgtJxXWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoG
# CCsGAQUFBwMIMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29j
# c3AuZGlnaWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDig
# NqA0hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9v
# dEc0LmNybDAgBgNVHSAEGTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZI
# hvcNAQELBQADggIBAH1ZjsCTtm+YqUQiAX5m1tghQuGwGC4QTRPPMFPOvxj7x1Bd
# 4ksp+3CKDaopafxpwc8dB+k+YMjYC+VcW9dth/qEICU0MWfNthKWb8RQTGIdDAiC
# qBa9qVbPFXONASIlzpVpP0d3+3J0FNf/q0+KLHqrhc1DX+1gtqpPkWaeLJ7giqzl
# /Yy8ZCaHbJK9nXzQcAp876i8dU+6WvepELJd6f8oVInw1YpxdmXazPByoyP6wCeC
# RK6ZJxurJB4mwbfeKuv2nrF5mYGjVoarCkXJ38SNoOeY+/umnXKvxMfBwWpx2cYT
# gAnEtp/Nh4cku0+jSbl3ZpHxcpzpSwJSpzd+k1OsOx0ISQ+UzTl63f8lY5knLD0/
# a6fxZsNBzU+2QJshIUDQtxMkzdwdeDrknq3lNHGS1yZr5Dhzq6YBT70/O3itTK37
# xJV77QpfMzmHQXh6OOmc4d0j/R0o08f56PGYX/sr2H7yRp11LB4nLCbbbxV7HhmL
# NriT1ObyF5lZynDwN7+YAN8gFk8n+2BnFqFmut1VwDophrCYoCvtlUG3OtUVmDG0
# YgkPCr2B2RP+v6TR81fZvAT6gt4y3wSJ8ADNXcL50CN/AAvkdgIm2fBldkKmKYcJ
# RyvmfxqkhQ/8mJb2VVQrH4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIG
# sDCCBJigAwIBAgIQCK1AsmDSnEyfXs2pvZOu2TANBgkqhkiG9w0BAQwFADBiMQsw
# CQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cu
# ZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBUcnVzdGVkIFJvb3QgRzQw
# HhcNMjEwNDI5MDAwMDAwWhcNMzYwNDI4MjM1OTU5WjBpMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0IFRydXN0
# ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0ExMIICIjAN
# BgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA1bQvQtAorXi3XdU5WRuxiEL1M4zr
# PYGXcMW7xIUmMJ+kjmjYXPXrNCQH4UtP03hD9BfXHtr50tVnGlJPDqFX/IiZwZHM
# gQM+TXAkZLON4gh9NH1MgFcSa0OamfLFOx/y78tHWhOmTLMBICXzENOLsvsI8Irg
# nQnAZaf6mIBJNYc9URnokCF4RS6hnyzhGMIazMXuk0lwQjKP+8bqHPNlaJGiTUyC
# EUhSaN4QvRRXXegYE2XFf7JPhSxIpFaENdb5LpyqABXRN/4aBpTCfMjqGzLmysL0
# p6MDDnSlrzm2q2AS4+jWufcx4dyt5Big2MEjR0ezoQ9uo6ttmAaDG7dqZy3SvUQa
# khCBj7A7CdfHmzJawv9qYFSLScGT7eG0XOBv6yb5jNWy+TgQ5urOkfW+0/tvk2E0
# XLyTRSiDNipmKF+wc86LJiUGsoPUXPYVGUztYuBeM/Lo6OwKp7ADK5GyNnm+960I
# HnWmZcy740hQ83eRGv7bUKJGyGFYmPV8AhY8gyitOYbs1LcNU9D4R+Z1MI3sMJN2
# FKZbS110YU0/EpF23r9Yy3IQKUHw1cVtJnZoEUETWJrcJisB9IlNWdt4z4FKPkBH
# X8mBUHOFECMhWWCKZFTBzCEa6DgZfGYczXg4RTCZT/9jT0y7qg0IU0F8WD1Hs/q2
# 7IwyCQLMbDwMVhECAwEAAaOCAVkwggFVMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYD
# VR0OBBYEFGg34Ou2O/hfEYb7/mF7CIhl9E5CMB8GA1UdIwQYMBaAFOzX44LScV1k
# TN8uZz/nupiuHA9PMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcD
# AzB3BggrBgEFBQcBAQRrMGkwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2lj
# ZXJ0LmNvbTBBBggrBgEFBQcwAoY1aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29t
# L0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcnQwQwYDVR0fBDwwOjA4oDagNIYyaHR0
# cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcmww
# HAYDVR0gBBUwEzAHBgVngQwBAzAIBgZngQwBBAEwDQYJKoZIhvcNAQEMBQADggIB
# ADojRD2NCHbuj7w6mdNW4AIapfhINPMstuZ0ZveUcrEAyq9sMCcTEp6QRJ9L/Z6j
# fCbVN7w6XUhtldU/SfQnuxaBRVD9nL22heB2fjdxyyL3WqqQz/WTauPrINHVUHmI
# moqKwba9oUgYftzYgBoRGRjNYZmBVvbJ43bnxOQbX0P4PpT/djk9ntSZz0rdKOtf
# JqGVWEjVGv7XJz/9kNF2ht0csGBc8w2o7uCJob054ThO2m67Np375SFTWsPK6Wrx
# oj7bQ7gzyE84FJKZ9d3OVG3ZXQIUH0AzfAPilbLCIXVzUstG2MQ0HKKlS43Nb3Y3
# LIU/Gs4m6Ri+kAewQ3+ViCCCcPDMyu/9KTVcH4k4Vfc3iosJocsL6TEa/y4ZXDlx
# 4b6cpwoG1iZnt5LmTl/eeqxJzy6kdJKt2zyknIYf48FWGysj/4+16oh7cGvmoLr9
# Oj9FpsToFpFSi0HASIRLlk2rREDjjfAVKM7t8RhWByovEMQMCGQ8M4+uKIw8y4+I
# Cw2/O/TOHnuO77Xry7fwdxPm5yg/rBKupS8ibEH5glwVZsxsDsrFhsP2JjMMB0ug
# 0wcCampAMEhLNKhRILutG4UI4lkNbcoFUCvqShyepf2gpx8GdOfy1lKQ/a+FSCH5
# Vzu0nAPthkX0tGFuv2jiJmCG6sivqf6UHedjGzqGVnhOMIIGwjCCBKqgAwIBAgIQ
# BUSv85SdCDmmv9s/X+VhFjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0
# ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIzMDcxNDAw
# MDAwMFoXDTM0MTAxMzIzNTk1OVowSDELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRp
# Z2lDZXJ0LCBJbmMuMSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMzCC
# AiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKNTRYcdg45brD5UsyPgz5/X
# 5dLnXaEOCdwvSKOXejsqnGfcYhVYwamTEafNqrJq3RApih5iY2nTWJw1cb86l+uU
# UI8cIOrHmjsvlmbjaedp/lvD1isgHMGXlLSlUIHyz8sHpjBoyoNC2vx/CSSUpIIa
# 2mq62DvKXd4ZGIX7ReoNYWyd/nFexAaaPPDFLnkPG2ZS48jWPl/aQ9OE9dDH9kgt
# XkV1lnX+3RChG4PBuOZSlbVH13gpOWvgeFmX40QrStWVzu8IF+qCZE3/I+PKhu60
# pCFkcOvV5aDaY7Mu6QXuqvYk9R28mxyyt1/f8O52fTGZZUdVnUokL6wrl76f5P17
# cz4y7lI0+9S769SgLDSb495uZBkHNwGRDxy1Uc2qTGaDiGhiu7xBG3gZbeTZD+BY
# QfvYsSzhUa+0rRUGFOpiCBPTaR58ZE2dD9/O0V6MqqtQFcmzyrzXxDtoRKOlO0L9
# c33u3Qr/eTQQfqZcClhMAD6FaXXHg2TWdc2PEnZWpST618RrIbroHzSYLzrqawGw
# 9/sqhux7UjipmAmhcbJsca8+uG+W1eEQE/5hRwqM/vC2x9XH3mwk8L9CgsqgcT2c
# kpMEtGlwJw1Pt7U20clfCKRwo+wK8REuZODLIivK8SgTIUlRfgZm0zu++uuRONhR
# B8qUt+JQofM604qDy0B7AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYD
# VR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgG
# BmeBDAEEAjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxq
# II+eyG8wHQYDVR0OBBYEFKW27xPn783QZKHVVqllMaPe1eNJMFoGA1UdHwRTMFEw
# T6BNoEuGSWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRH
# NFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGD
# MIGAMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYB
# BQUHMAKGTGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0
# ZWRHNFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQEL
# BQADggIBAIEa1t6gqbWYF7xwjU+KPGic2CX/yyzkzepdIpLsjCICqbjPgKjZ5+PF
# 7SaCinEvGN1Ott5s1+FgnCvt7T1IjrhrunxdvcJhN2hJd6PrkKoS1yeF844ektrC
# QDifXcigLiV4JZ0qBXqEKZi2V3mP2yZWK7Dzp703DNiYdk9WuVLCtp04qYHnbUFc
# jGnRuSvExnvPnPp44pMadqJpddNQ5EQSviANnqlE0PjlSXcIWiHFtM+YlRpUurm8
# wWkZus8W8oM3NG6wQSbd3lqXTzON1I13fXVFoaVYJmoDRd7ZULVQjK9WvUzF4UbF
# KNOt50MAcN7MmJ4ZiQPq1JE3701S88lgIcRWR+3aEUuMMsOI5ljitts++V+wQtaP
# 4xeR0arAVeOGv6wnLEHQmjNKqDbUuXKWfpd5OEhfysLcPTLfddY2Z1qJ+Panx+VP
# NTwAvb6cKmx5AdzaROY63jg7B145WPR8czFVoIARyxQMfq68/qTreWWqaNYiyjvr
# moI1VygWy2nyMpqy0tg6uLFGhmu6F/3Ed2wVbK6rr3M66ElGt9V/zLY4wNjsHPW2
# obhDLN9OTH0eaHDAdwrUAuBcYLso/zjlUlrWrBciI0707NMX+1Br/wd3H3GXREHJ
# uEbTbDJ8WC9nR2XlG3O2mflrLAZG70Ee8PBf4NvZrZCARK+AEEGKMIIG7TCCBNWg
# AwIBAgIQBNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0
# IFRydXN0ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0Ex
# MB4XDTI0MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMx
# DzANBgNVBAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9j
# b2xhdGV5IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdh
# cmUsIEluYzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RV
# lBBBniCyGy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAG
# DUPAGhXf0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDru
# pjshvLmQMSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz
# 2zCt8CvuR3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQgh
# KRoIwfp/hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN9
# 7++0BApX2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YV
# nKP2AOVca6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyF
# H8FVnZZV1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4IC
# AzCCAf8wHwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYE
# FFpfZUilS5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYI
# KwYBBQUHAgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8E
# BAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZN
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNp
# Z25pbmdSU0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5
# NlNIQTM4NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmlu
# Z1JTQTQwOTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEB
# CwUAA4ICAQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDH
# BZP9sDRZsFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGU
# iA3mKdbVit0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T
# 9D8ct/m5LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIW
# v/AHDf8ukJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIe
# M3FdtLstn9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4
# hf/0MqEtWSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOa
# QbKf0CxdxMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+c
# YcpmI4R13Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyV
# b6zGqAaE9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM1
# 8zHiN4WA4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDGCBdMwggXP
# AgEBMH0waTELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEw
# PwYDVQQDEzhEaWdpQ2VydCBUcnVzdGVkIEc0IENvZGUgU2lnbmluZyBSU0E0MDk2
# IFNIQTM4NCAyMDIxIENBMQIQBNI793flHTneCMtwLiiYFTANBglghkgBZQMEAgEF
# AKCBhDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgor
# BgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3
# DQEJBDEiBCALlzX5EJQxIX5Se+3pgFN8mDDtCp8oVnCHn1cQstmAcDANBgkqhkiG
# 9w0BAQEFAASCAYBnhZdaUgLYxJC55UrYZS+hDV9S5H7fB/KG5fXKrX3aMsJN5R/q
# uLTii9AOC71fMp2qPFyQl05Oh2KIcbIfjtDpeIaeVTI1V0Ch+EFZkXjWTiBzbKRn
# ToTajOQbtjMdv/EgeTu1CGjgv9O9LMxJAkoiEcfwU9ruNkEfz97rnNbw6h2CiR95
# 4UI63oBOGeHDRekgu+HHCFgHW+4Z2bng5c0WyA6w6L01gHHDpLw7d7D22BqXQtsi
# iq4FgEc1QqO1v43gVnaXYlProbONcEnPTUnAOhbQFSpXuFR8ieergQS9wOgVPRpS
# GeNJX92GmXEX2PhR3nmddnqAMbd3e2jhE9gqF+X+upnDTv9PSmRRJjh2/GZipNnK
# I5YsEZjYC/ArtSeIsJKOTSN06Y2+AHxKKnQxg18Yr4gzruRfD6QQgouPS546zJcS
# 6zAzG2LSA21EMwFfApDZxFuw/PbUmyaLNc3aHwRqppXKNCVnfr4Q16Hbp62gzx0g
# qc6qhktrGO6b1xChggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQsw
# CQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRp
# Z2lDZXJ0IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENB
# AhAFRK/zlJ0IOaa/2z9f5WEWMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkD
# MQsGCSqGSIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQwNTE2MTYxOTM1WjAvBgkq
# hkiG9w0BCQQxIgQg9FUDVW3LVrCrcAU/230KzrUxe96ba+tELoi4n5KqlFUwDQYJ
# KoZIhvcNAQEBBQAEggIAMv92+Pnr/dMRw+ege2oKN/2HiT4qCdGL8XV4sr76n+Bf
# VSt/DA+coikkAOs/htnZz+2/UNfe2UNQZTmN3wWYXsM0JKXKLeRoTgJ4GmOYOZIT
# lBgHQAlwwiKsTlu2p6tNWOXCMwGUZ+uc/FIbJXN/MpqA/uUxcUoCQZtvI3Ukuhyk
# V1BA0zG4jlKSm+IPx9eASfpCA23gkTtGQTBY7DYsb/gLD1qGou+NlbcKCZ43FyLx
# /meHcIp33tDSew3tsRtXo1UfWDN9EVAf+tmYuKAHrUHgf1zlx6KVZOfjmt9DYDdF
# elx6i6M1cnePxXBRUkqFXsIFfEbGI/Nr8p5LVcaFsWcG1AT+NkdbrP0CTbwUjKTx
# cFyV/76FGWTSwkkjr0/bwbIeGBqUYEWNQ58bmiID9+e4UQ5csp+tvQWw+3bAUiFA
# ZqlKCNzeLu2CFvqFbpe3N9dkM7r42SorZ1TiRrW8wyMzxbkfRUHKnl4VFuhirvBH
# L+2gcJdXUk+2Y7WbUFqjwc9/RVfVAcLYUihdegW0ODo9gcxUkh1lCSeLyklrgg5G
# wmzP2rF/7QTc6iFKpLoyAyHBvoUWGIB3PRszcToBu3U+HTmMZFgoyIyJ0qnfV1rM
# dioHMkCqsUFn4/DmkA1kkqxGBIFQUngncyZ2NoKOP7Zqlt+LgUUx3Cll/1wU62o=
# SIG # End signature block
