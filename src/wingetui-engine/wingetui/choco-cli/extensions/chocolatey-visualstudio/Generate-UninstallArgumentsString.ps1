function Generate-UninstallArgumentsString
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [hashtable] $parameters,
        [string] $logFilePath,
        [switch] $assumeNewVS2017Installer,
        [bool] $supportsNoRestart
    )
    Write-Debug "Running 'Generate-UninstallArgumentsString' with logFilePath:'$logFilePath', assumeNewVS2017Installer:'$assumeNewVS2017Installer', supportsNoRestart:'$supportsNoRestart'";
    if ($assumeNewVS2017Installer)
    {
        if ($logFilePath -ne '')
        {
            Write-Warning "The new VS 2017 installer does not support setting the path to the log file yet."
        }

        $argumentSet = $parameters.Clone()
        if ($supportsNoRestart)
        {
            $argumentSet['norestart'] = ''
        }

        if (-not $argumentSet.ContainsKey('quiet') -and -not $argumentSet.ContainsKey('passive'))
        {
            $argumentSet['quiet'] = ''
        }

        $s = ConvertTo-ArgumentString -InitialUnstructuredArguments @('/uninstall') -Arguments $argumentSet -Syntax 'Willow'
    }
    else
    {
        $s = "/Uninstall /Force /Quiet /NoRestart"
        if ($logFilePath -ne '')
        {
            $s = $s + " /Log ""$logFilePath"""
        }
    }

    return $s
}
