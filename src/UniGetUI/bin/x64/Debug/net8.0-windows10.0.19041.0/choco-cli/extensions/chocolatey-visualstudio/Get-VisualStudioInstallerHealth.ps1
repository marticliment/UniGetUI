function Get-VisualStudioInstallerHealth
{
<#
.SYNOPSIS
Checks the Visual Studio Installer for signs of corruption.

.DESCRIPTION
This function returns an object containing the health status
of the Visual Studio Installer, checking for the existence
of a few files observed missing after a failed Installer update.

.OUTPUTS
A System.Management.Automation.PSObject with the following properties:
Path (System.String)
IsHealthy (System.Boolean)
MissingFiles (System.String[])
#>
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true, ValueFromPipelineByPropertyName = $true)] [string] $Path,
        [version] $Version
    )
    Process
    {
        if ($Path -like '*.exe')
        {
            $dirPath = Split-Path -Path $Path
        }
        else
        {
            $dirPath = $Path
        }

        if ($null -eq $Version)
        {
            # be conservative
            $expectedFiles = @('vs_installer.exe')
        }
        elseif ($Version -lt [version]'2.9')
        {
            $expectedFiles = @('vs_installer.exe', 'vs_installershell.exe', 'node.dll', 'ffmpeg.dll')
        }
        else
        {
            $expectedFiles = @('vs_installer.exe', 'vs_installershell.exe', 'setup.exe')
        }

        $missingFiles = $expectedFiles | Where-Object { -not (Test-Path (Join-Path -Path $dirPath -ChildPath $_))}
        $obj = New-Object -TypeName PSObject -Property @{
            Path = $Path
            IsHealthy = ($missingFiles | Measure-Object).Count -eq 0
            MissingFiles = $missingFiles
        }

        Write-Output $obj
    }
}
