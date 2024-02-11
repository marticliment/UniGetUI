<#
.SYNOPSIS
    Returns the exit code set earlier by a native installer executed via the Start-ChocolateyProcessAsAdmin helper.

.OUTPUT
    System.Int32 or $null
#>
function Get-NativeInstallerExitCode
{
    [CmdletBinding()]
    Param
    (
    )
    End
    {
        $exitCodeString = Get-EnvironmentVariable -Name ChocolateyExitCode -Scope Process
        if ([string]::IsNullOrEmpty($exitCodeString))
        {
            return $null
        }

        [int] $exitCode = 0
        if (-not ([int]::TryParse($exitCodeString, [ref]$exitCode)))
        {
            Write-Warning "Unable to parse ChocolateyExitCode value: $exitCodeString"
            return $null
        }

        return $exitCode
    }
}
