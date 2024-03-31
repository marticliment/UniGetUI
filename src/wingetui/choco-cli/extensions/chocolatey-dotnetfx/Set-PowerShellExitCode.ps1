function Set-PowerShellExitCode
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [int] $ExitCode
    )
    End
    {
        chocolateyInstaller\Set-PowerShellExitCode @PSBoundParameters
        if ($ExitCode -eq 0 -and $Env:ChocolateyExitCode -ne '0')
        {
            Write-Debug 'chocolateyInstaller\Set-PowerShellExitCode ignored 0, setting the host exit code and environment variable manually'
            $Host.SetShouldExit(0)
            $Env:ChocolateyExitCode = '0'
        }
    }
}
