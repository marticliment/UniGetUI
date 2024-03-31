function Generate-InstallArgumentsString
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [hashtable] $parameters,
        [string] $adminFile,
        [string] $logFilePath,
        [switch] $assumeNewVS2017Installer
    )
    Write-Debug "Running 'Generate-InstallArgumentsString' with parameters:'$parameters', adminFile:'$adminFile', logFilePath:'$logFilePath', assumeNewVS2017Installer:'$assumeNewVS2017Installer'";
    if ($assumeNewVS2017Installer)
    {
        if ($logFilePath -ne '')
        {
            Write-Warning "The new VS 2017 installer does not support setting the path to the log file yet."
        }

        if ($adminFile -ne '')
        {
            Write-Warning "The new VS 2017 installer does not support an admin file yet."
        }

        $argumentSet = $parameters.Clone()
        $argumentSet['wait'] = ''
        if (-not $argumentSet.ContainsKey('layout'))
        {
            $argumentSet['norestart'] = ''
        }
        if (-not $argumentSet.ContainsKey('quiet') -and -not $argumentSet.ContainsKey('passive'))
        {
            $argumentSet['quiet'] = ''
        }

        $nativeInstallerDescription = 'VS Bootstrapper'
        $nativeInstallerArgumentBlacklist = @('bootstrapperPath', 'layoutPath')
        Remove-NegatedArguments -Arguments $argumentSet -RemoveNegativeSwitches
        Remove-VSPackageParametersNotPassedToNativeInstaller -PackageParameters $argumentSet -TargetDescription $nativeInstallerDescription -Blacklist $nativeInstallerArgumentBlacklist

        $s = ConvertTo-ArgumentString -Arguments $argumentSet -Syntax 'Willow'
    }
    else
    {
        $s = "/Quiet /NoRestart"
        if ($logFilePath -ne '')
        {
            $s = $s + " /Log ""$logFilePath"""
        }

        if ($adminFile -ne '')
        {
            $s = $s + " /AdminFile $adminFile"
        }

        if ($parameters.ContainsKey('layout'))
        {
            # TODO: CHECK THIS, perhaps /NoRestart is incompatible with this?
            $s = $s + " /Layout ""$($parameters['layout'])"""
        }
    }

    $pk = $parameters['ProductKey']
    if ($pk -and (-not [string]::IsNullOrEmpty($pk)))
    {
        Write-Verbose "Using provided product key: ...-$($pk.Substring([Math]::Max($pk.Length - 5, 0)))"
        if ($assumeNewVS2017Installer)
        {
            # nothing to do, all package parameters are passed to Willow
        }
        else
        {
            $s = $s + " /ProductKey $pk"
        }
    }

    return $s
}
