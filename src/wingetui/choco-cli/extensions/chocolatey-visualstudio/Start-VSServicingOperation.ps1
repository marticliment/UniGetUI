function Start-VSServicingOperation
{
    [CmdletBinding()]
    param(
        [string] $packageName,
        [string] $silentArgs,
        [string] $file,
        [string] $logFilePath,
        [string[]] $operationTexts,
        [switch] $assumeNewVS2017Installer
    )
    Write-Debug "Running 'Start-VSServicingOperation' for $packageName with silentArgs:'$silentArgs', file:'$file', logFilePath:$logFilePath', operationTexts:'$operationTexts', assumeNewVS2017Installer:'$assumeNewVS2017Installer'"

    Wait-VSInstallerProcesses -Behavior 'Fail'

    $frobbed, $frobbing, $frobbage = $operationTexts

    $successExitCodes = @(
        0 # success
    )
    $rebootExitCodes = @(
        3010 # success, restart required
    )
    $priorRebootRequiredExitCodes = @(
        -2147185721 # Restart is required before (un)installation can continue
    )
    $blockExitCodes = @(
        -2147205120, # block, restart not required
        -2147172352 # block, restart required
    )

    $startTime = Get-Date
    $exitCode = Start-VSChocolateyProcessAsAdmin -statements $silentArgs -exeToRun $file -acceptAllExitCodes
    Write-Debug "Exit code returned from Start-VSChocolateyProcessAsAdmin: '$exitCode'"
    if ($assumeNewVS2017Installer)
    {
        $auxExitCode = Wait-VSInstallerProcesses -Behavior 'Wait'
        if ($null -ne $auxExitCode -and $exitCode -eq 0)
        {
            Write-Debug "Using aux exit code returned from Wait-VSInstallerProcesses ('$auxExitCode')"
            $exitCode = $auxExitCode
        }
    }
    Write-Debug "Setting Env:ChocolateyExitCode to '$exitCode'"
    $Env:ChocolateyExitCode = $exitCode
    $warnings = @()
    if (($successExitCodes | Measure-Object).Count -gt 0 -and $successExitCodes -contains $exitCode)
    {
        $needsReboot = $false
        $success = $true
    }
    elseif (($rebootExitCodes | Measure-Object).Count -gt 0 -and $rebootExitCodes -contains $exitCode)
    {
        $needsReboot = $true
        $success = $true
    }
    elseif (($priorRebootRequiredExitCodes | Measure-Object).Count -gt 0 -and $priorRebootRequiredExitCodes -contains $exitCode)
    {
        $exceptionMessage = "The computer must be rebooted before ${frobbing} ${packageName}. Please reboot the computer and run the ${frobbage} again."
        $success = $false
    }
    elseif (($blockExitCodes | Measure-Object).Count -gt 0 -and $blockExitCodes -contains $exitCode)
    {
        $exceptionMessage = "${packageName} cannot be ${frobbed} on this system."
        $success = $false
        if ($logFilePath -ne '' -and (Test-Path -Path $logFilePath))
        {
            # [0C40:07D8][2016-05-28T23:17:32]i000: MUX:  Stop Block: MinimumOSLevel : This version of Visual Studio requires a computer with a !$!http://go.microsoft.com/fwlink/?LinkID=647155&clcid=0x409!,!newer version of Windows!@!.
            # [0C40:07D8][2016-05-28T23:17:32]i000: MUX:  Stop Block: SystemRebootPendingBlock : The computer needs to be restarted before setup can continue. Please restart the computer and run setup again.
            $blocks = Get-Content -Path $logFilePath `
                | Select-String '(?<=Stop Block: ).+$' `
                | Select-Object -ExpandProperty Matches `
                | Where-Object { $_.Success -eq $true } `
                | Select-Object -ExpandProperty Value `
                | Sort-Object -Unique
            if (($blocks | Measure-Object).Count -gt 0)
            {
                $warnings = @("${packageName} cannot be ${frobbed} due to the following issues:") + $blocks
                $exceptionMessage += " You may attempt to fix the issues listed and try again."
            }
        }
    }
    else
    {
        $exceptionMessage = "The ${frobbage} of ${packageName} failed (installer exit code: ${exitCode})."
        $success = $false
    }

    if ($success)
    {
        if ($needsReboot)
        {
            Write-Warning "${packageName} has been ${frobbed}. However, a reboot is required to finalize the ${frobbage}."
        }
        else
        {
            Write-Host "${packageName} has been ${frobbed}."
        }
    }
    else
    {
        if (($warnings | Measure-Object).Count -gt 0)
        {
            $warnings | Write-Warning
        }
        if ($assumeNewVS2017Installer)
        {
            Show-VSInstallerErrorLog -Since $startTime
        }
        throw $exceptionMessage
    }
}
