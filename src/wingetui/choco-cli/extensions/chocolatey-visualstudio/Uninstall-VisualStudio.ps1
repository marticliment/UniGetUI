function Uninstall-VisualStudio {
<#
.SYNOPSIS
Uninstalls Visual Studio

.DESCRIPTION
Uninstalls Visual Studio.

.PARAMETER PackageName
The name of the VisualStudio package.

.PARAMETER ApplicationName
The VisualStudio app name - i.e. 'Microsoft Visual Studio Community 2015'.

.PARAMETER UninstallerName
This name of the installer executable - i.e. 'vs_community.exe'.

.EXAMPLE
Uninstall-VisualStudio 'VisualStudio2015Community' 'Microsoft Visual Studio Community 2015' 'vs_community.exe'

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to write to uninstall Visual Studio.
This method has no error handling built into it.

.LINK
Uninstall-ChocolateyPackage
#>
    [CmdletBinding()]
    param(
      [string] $PackageName,
      [string] $ApplicationName,
      [string] $UninstallerName,
      [ValidateSet('MsiVS2015OrEarlier', 'WillowVS2017OrLater')] [string] $InstallerTechnology,
      [string] $ProgramsAndFeaturesDisplayName = $ApplicationName
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }
    Write-Debug "Running 'Uninstall-VisualStudio' for $PackageName with ApplicationName:'$ApplicationName' UninstallerName:'$UninstallerName' InstallerTechnology:'$InstallerTechnology' ProgramsAndFeaturesDisplayName:'$ProgramsAndFeaturesDisplayName'";

    $assumeNewVS2017Installer = $InstallerTechnology -eq 'WillowVS2017OrLater'

    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters
    if ($assumeNewVS2017Installer)
    {
        $vsInstaller = Get-VisualStudioInstaller
        if ($null -eq $vsInstaller)
        {
            Write-Warning "Uninstall information for $PackageName could not be found. This probably means the application was uninstalled outside Chocolatey."
            return
        }

        $uninstallerPath = $vsInstaller.Path
        $logFilePath = $null
        $supportsNoRestart = $vsInstaller.Traits -contains 'SelfUninstallNoRestart'
    }
    else
    {
        $uninstallerPath = Get-VSUninstallerExePath `
            -PackageName $PackageName `
            -UninstallerName $UninstallerName `
            -ProgramsAndFeaturesDisplayName $ProgramsAndFeaturesDisplayName `
            -AssumeNewVS2017Installer:$assumeNewVS2017Installer

        $logFilePath = Join-Path $Env:TEMP "${PackageName}_uninstall.log"
        Write-Debug "Log file path: $logFilePath"
        $supportsNoRestart = $true
    }

    $silentArgs = Generate-UninstallArgumentsString -parameters $packageParameters -logFilePath $logFilePath -assumeNewVS2017Installer:$assumeNewVS2017Installer -supportsNoRestart:$supportsNoRestart

    $arguments = @{
        packageName = $PackageName
        silentArgs = $silentArgs
        file = $uninstallerPath
        assumeNewVS2017Installer = $assumeNewVS2017Installer
    }
    $argumentsDump = ($arguments.GetEnumerator() | ForEach-Object { '-{0}:''{1}''' -f $_.Key,"$($_.Value)" }) -join ' '
    Write-Debug "Uninstall-VSChocolateyPackage $argumentsDump"
    Uninstall-VSChocolateyPackage @arguments
}
