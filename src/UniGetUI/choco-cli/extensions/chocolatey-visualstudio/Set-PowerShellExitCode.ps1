if (-not (Test-Path -Path Function:\Set-PowerShellExitCode))
{
    # based on Set-PowerShellExitCode (07277ac), included here unchanged to add exit code support to old Chocolatey
    function Set-PowerShellExitCode {
        param (
            [int]$exitCode
        )

        $host.SetShouldExit($exitCode); 
        $env:ChocolateyExitCode = $exitCode;
    }
}
