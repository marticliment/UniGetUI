function Get-VisualStudioVsixInstaller
{
<#
.SYNOPSIS
Locates the Visual Studio extension (VSIX) installer.

.DESCRIPTION
Locates all instances of the Visual Studio extension installer
(VSIXInstaller.exe) present on the machine and returns their
paths and version numbers.

.PARAMETER Latest
Returns the VSIXInstaller.exe with the highest version number only.

.OUTPUTS
For each VSIXInstaller.exe instance found, returns an object
containing these properties:
Path - the path to the VSIXInstaller.exe instance.
Version - the program version number, extracted from the ProductVersion
          property of the version resource embedded in the program.

.NOTES
Supports both VS 2017+ and "legacy" VS versions (2015 and earlier).
#>
    [CmdletBinding()]
    Param
    (
        [switch] $Latest
    )
    Write-Debug "Running 'Get-VisualStudioVsixInstaller' with Latest:'$Latest'";

    $candidates = New-Object System.Collections.ArrayList
    $modernProducts = Get-WillowInstalledProducts
    $modernProducts `
        | Where-Object { $null -ne $_ } `
        | ForEach-Object { $_['enginePath'] } `
        | Where-Object { -not [string]::IsNullOrEmpty($_) } `
        | Select-Object -Unique `
        | ForEach-Object { Get-Item -Path "$_\VSIXInstaller.exe" -ErrorAction SilentlyContinue } `
        | ForEach-Object { [void]$candidates.Add($_) }

    if (-not $Latest -or $candidates.Count -eq 0)
    {
        $legacyProducts = Get-VSLegacyInstance
        $legacyProducts `
            | Where-Object { $null -ne $_ } `
            | Select-Object -ExpandProperty Path -Unique `
            | Where-Object { -not [string]::IsNullOrEmpty($_) } `
            | ForEach-Object { Get-Item -Path "$_\Common7\IDE\VSIXInstaller.exe" -ErrorAction SilentlyContinue } `
            | ForEach-Object { [void]$candidates.Add($_) }
    }
    else
    {
        Write-Debug 'Not looking for VSIXInstaller in legacy VS products because -Latest was specified and more modern VS product(s) were found.'
    }

    $rxVersion = [regex]'^\d+(\.\d+)+'
    $sortedCandidates = $candidates `
        | Select-Object -Property `
            @{ Name = 'Path'; Expression = { $_.FullName } }, 
            @{ Name = 'Version'; Expression = { [version]($rxVersion.Match($_.VersionInfo.ProductVersion).Value) } } `
        | Sort-Object -Property Version -Descending `
        | ForEach-Object { Write-Debug ('Found VSIXInstaller.exe version ''{0}'': {1}' -f $_.Version, $_.Path); $_ }

    if ($Latest)
    {
        if (($sortedCandidates | Measure-Object).Count -eq 0)
        {
            Write-Error 'The VSIX Installer is not present.'
        }
        else
        {
            Write-Output ($sortedCandidates[0])
        }
    }
    else
    {
        $sortedCandidates | Write-Output
    }
}
