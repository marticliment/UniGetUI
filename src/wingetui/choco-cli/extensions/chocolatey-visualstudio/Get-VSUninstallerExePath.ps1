function Get-VSUninstallerExePath
{
    [CmdletBinding()]
    param(
      [string] $PackageName,
      [string] $UninstallerName,
      [switch] $AssumeNewVS2017Installer,
      [string] $ProgramsAndFeaturesDisplayName
    )

    $informMaintainer = "Please report this to the maintainer of this package ($PackageName)."
    $uninstallKey = Get-VSUninstallRegistryKey -ApplicationName $ProgramsAndFeaturesDisplayName
    $count = ($uninstallKey | Measure-Object).Count
    Write-Debug "Found $count Uninstall key(s)"
    if ($count -eq 0)
    {
        Write-Warning "Uninstall information for $ProgramsAndFeaturesDisplayName could not be found. This probably means the application was uninstalled outside Chocolatey."
        return $null
    }
    if ($count -gt 1)
    {
        throw "More than one Uninstall key found for $ProgramsAndFeaturesDisplayName! $informMaintainer"
    }

    Write-Debug "Using Uninstall key: $($uninstallKey.PSPath)"
    $uninstallString = $uninstallKey | Get-ItemProperty -Name UninstallString | Select-Object -ExpandProperty UninstallString
    Write-Debug "UninstallString: $uninstallString"
    if ($AssumeNewVS2017Installer)
    {
        # C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe /uninstall
        $uninstallerExePathRegexString = '^(.+[^\s])\s/uninstall$'
    }
    else
    {
        # "C:\ProgramData\Package Cache\{4f075c79-8ee3-4c85-9408-828736d1f7f3}\vs_community.exe"  /uninstall
        $uninstallerExePathRegexString = '^\s*(\"[^\"]+\")|([^\s]+)'
    }
    if (-not ($uninstallString -match $uninstallerExePathRegexString))
    {
        throw "UninstallString '$uninstallString' is not of the expected format. $informMaintainer"
    }
    $uninstallerPath = $matches[1].Trim('"')
    Write-Debug "uninstallerPath: $uninstallerPath"
    if ((Split-Path -Path $uninstallerPath -Leaf) -ne $UninstallerName)
    {
        throw "The uninstaller file name is unexpected (uninstallerPath: $uninstallerPath). $informMaintainer"
    }

    return $uninstallerPath
}
