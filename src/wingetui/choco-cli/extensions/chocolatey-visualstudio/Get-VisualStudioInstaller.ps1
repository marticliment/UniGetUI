function Get-VisualStudioInstaller
{
<#
.SYNOPSIS
Returns information about the Visual Studio Installer.

.DESCRIPTION
This function returns an object containing the basic properties (path, version)
of the Visual Studio Installer used by VS 2017+ (vs_installer.exe),
if it is present.

.OUTPUTS
A System.Management.Automation.PSObject with the following properties:
Path (System.String)
Version (System.Version)
EngineVersion (System.Version)
Traits (System.String[])
#>
    [CmdletBinding()]
    Param
    (
    )

    $rxVersion = [regex]'"version":\s+"(?<value>[0-9\.]+)"'
    $basePaths = @(${Env:ProgramFiles}, ${Env:ProgramFiles(x86)}, ${Env:ProgramW6432})
    $installer = $basePaths | Where-Object { $null -ne $_ } | Sort-Object -Unique | ForEach-Object {
        $basePath = $_
        $candidateDirPath = Join-Path -Path $basePath -ChildPath 'Microsoft Visual Studio\Installer'
        $candidateDirFiles = Get-ChildItem -Path $candidateDirPath -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsContainer }
        if (($candidateDirFiles | Measure-Object).Count -gt 0)
        {
            Write-Debug "Located VS installer directory: $candidateDirPath"
            $version = $null
            $versionJsonPath = Join-Path -Path $candidateDirPath -ChildPath 'resources\app\package.json'
            $setupExePath = Join-Path -Path $candidateDirPath -ChildPath 'setup.exe'
            if (Test-Path -Path $versionJsonPath)
            {
                # old, Electron-based app
                $source = 'package.json'
                $text = Get-Content -Path $versionJsonPath
                $matchesCollection = $rxVersion.Matches($text)
                foreach ($match in $matchesCollection)
                {
                    if ($null -eq $match -or -not $match.Success)
                    {
                        continue
                    }

                    $value = $match.Groups['value'].Value
                    try
                    {
                        $version = [version]$value
                        Write-Debug ('Parsed Visual Studio Installer version from {0}: {1}' -f $source, $version)
                        break
                    }
                    catch
                    {
                        Write-Warning ('Unable to parse Visual Studio Installer version ''{0}'' from {1}' -f $value, $source)
                    }
                }
            }
            elseif (Test-Path -Path $setupExePath)
            {
                $source = 'setup.exe'
                $setupExeItem = Get-Item -Path $setupExePath
                $value = $setupExeItem.VersionInfo.FileVersion
                try
                {
                    $version = [version]$value
                    Write-Debug ('Parsed Visual Studio Installer version from {0}: {1}' -f $source, $version)
                }
                catch
                {
                    Write-Warning ('Unable to parse Visual Studio Installer version ''{0}'' from {1}' -f $value, $source)
                }
            }
            else
            {
                Write-Warning ('Visual Studio Installer version information not found in {0}' -f $candidateDirPath)
            }

            $engineVersion = $null
            $engineDllPath = Join-Path -Path $candidateDirPath -ChildPath 'resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\Microsoft.VisualStudio.Setup.dll'
            if (Test-Path -Path $engineDllPath)
            {
                $item = Get-Item -Path $engineDllPath
                $engineVersionString = $item.VersionInfo.FileVersion
                try
                {
                    $engineVersion = [version]$engineVersionString
                    Write-Debug ('Parsed Visual Studio Installer engine version: {0}' -f $engineVersion)
                }
                catch
                {
                    Write-Warning ('Unable to parse Visual Studio Installer engine version ''{0}''' -f $engineVersionString)
                }
            }
            else
            {
                Write-Warning ('Visual Studio Installer engine file not found: {0}' -f $engineDllPath)
            }

            $traits = @()
            if ($version -lt [version]'2.9')
            {
                # Before Visual Studio 2019 16.9 (VS Installer 2.9.*), the installer supported the --norestart switch for the special /uninstall command.
                $traits += 'SelfUninstallNoRestart'
            }

            $installerExePath = Join-Path -Path $candidateDirPath -ChildPath 'vs_installer.exe'
            $props = @{
                Path = $installerExePath
                Version = $version
                EngineVersion = $engineVersion
                Traits = ([string[]]$traits)
            }
            $obj = New-Object -TypeName PSObject -Property $props
            Write-Verbose ('Visual Studio Installer version {0} (engine version {1}) (traits: {3}) is present ({2}).' -f $obj.Version, $obj.EngineVersion, $obj.Path, ($obj.Traits -join ' '))
            $health = $obj | Get-VisualStudioInstallerHealth -Version $version
            if (-not $health.IsHealthy)
            {
                if (($health.MissingFiles | Measure-Object).Count -gt 0)
                {
                    Write-Warning ('The Visual Studio Installer in directory ''{0}'' appears broken - missing files: {1}' -f $obj.Path, ($health.MissingFiles -join ', '))
                }
                else
                {
                    Write-Warning ('The Visual Studio Installer in directory ''{0}'' appears broken' -f $obj.Path)
                }
            }
            Write-Output $obj
        }
    }
    $installerCount = ($installer | Measure-Object).Count
    if ($installerCount -eq 0)
    {
        Write-Verbose 'Visual Studio Installer is not present.'
    }
    elseif ($installerCount -gt 1)
    {
        Write-Warning ('Multiple instances of Visual Studio Installer found ({0}), using the first one. This is unexpected, please inform the maintainer of this package.' -f $installerCount)
    }
    $installer | Select-Object -First 1 | Write-Output
}
