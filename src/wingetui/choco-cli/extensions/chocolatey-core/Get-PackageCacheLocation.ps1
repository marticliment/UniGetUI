<#
.SYNOPSIS
    Get temporary location for the package based on its name and version.

.DESCRIPTION
    The function returns package cache directory within $Env:TEMP. It will not create the directory
    if it doesn't exist.

    This function is useful when you have to obtain the file using `Get-ChocolateyWebFile` in order
    to perform certain installation steps that other helpers can't do.

.EXAMPLE
    Get-PackageCacheLocation

.OUTPUTS
    [String]

.LINKS
    Get-ChocolateyWebFile
#>
function Get-PackageCacheLocation {
    [CmdletBinding()]
    param (
        # Name of the package, by default $Env:ChocolateyPackageName
        [string] $Name    = $Env:ChocolateyPackageName,
        # Version of the package, by default $Env:ChocolateyPackageVersion
        [string] $Version = $Env:ChocolateyPackageVersion,
        # Allows splatting with arguments that do not apply and future expansion. Do not use directly.
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $IgnoredArguments
    )

    if (!$Name) { Write-Warning 'Environment variable $Env:ChocolateyPackageName is not set' }
    $res = Join-Path $Env:TEMP $Name

    if (!$Version) { Write-Warning 'Environment variable $Env:ChocolateyPackageVersion is not set' }
    $res = Join-Path $res $Version

    $res
}
