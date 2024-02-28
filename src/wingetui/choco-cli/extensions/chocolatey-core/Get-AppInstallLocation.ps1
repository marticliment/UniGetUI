<#
.SYNOPSIS
    Get application install location

.DESCRIPTION
    Function tries to find install location in multiple places. It returns $null if all fail. The following
    locations are tried:
      - local and machine (x32 & x64) various Uninstall keys
      - x32 & x64 Program Files up to the 2nd level of depth
      - native commands available via PATH
      - locale and machine registry key SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths

    Use Verbose parameter to see which of the above locations was used for the result, if any.

.EXAMPLE
    PS> Get-AppInstallLocation choco

    Returns the install location of the application 'choco'.

.OUTPUTS
    [String] or $null
#>
function Get-AppInstallLocation {
    [CmdletBinding()]
    param(
        # Regular expression pattern
        [ValidateNotNullOrEmpty()]
        [string] $AppNamePattern,

        # Allows splatting with arguments that do not apply and future expansion. Do not use directly.
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $IgnoredArguments
    )

    function strip($path) { if ($path.EndsWith('\')) { return $path -replace '.$' } else { $path } }

    function is_dir( $path ) { $path -and (Get-Item $path -ea 0).PsIsContainer -eq $true }

    $ErrorActionPreference = "SilentlyContinue"

    Write-Verbose "Trying local and machine (x32 & x64) Uninstall keys"
    [array] $key = Get-UninstallRegistryKey $AppNamePattern
    if ($key.Count -eq 1) {
        Write-Verbose "Trying Uninstall key property 'InstallLocation'"
        $location = $key.InstallLocation
        if (is_dir $location) { return strip $location }

        Write-Verbose "Trying Uninstall key property 'UninstallString'"
        $location = $key.UninstallString
        if ($location) { $location = $location.Replace('"', '') | Split-Path }
        if (is_dir $location) { return strip $location }

        Write-Verbose "Trying Uninstall key property 'DisplayIcon'"
        $location = $key.DisplayIcon
        if ($location) { $location = Split-Path $location }
        if (is_dir $location) { return strip $location }
    } else { Write-Verbose "Found $($key.Count) keys, aborting this method" }

    $dirs = $Env:ProgramFiles, "$Env:ProgramFiles\*\*"
    if (Get-OSArchitectureWidth 64) { $dirs += ${ENV:ProgramFiles(x86)}, "${ENV:ProgramFiles(x86)}\*\*" }
    Write-Verbose "Trying Program Files with 2 levels depth: $dirs"
    $location = (Get-ChildItem $dirs | Where-Object {$_.PsIsContainer}) -match $AppNamePattern | Select-Object -First 1 | ForEach-Object {$_.FullName}
    if (is_dir $location) { return strip $location }

    Write-Verbose "Trying native commands on PATH"
    $location = (Get-Command -CommandType Application) -match $AppNamePattern | Select-Object -First 1 | ForEach-Object { Split-Path $_.Source }
    if (is_dir $location) { return strip $location }

    $appPaths =  "\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"
    Write-Verbose "Trying Registry: $appPaths"
    $location = (Get-ChildItem "HKCU:\$appPaths", "HKLM:\$appPaths") -match $AppNamePattern | Select-Object -First 1
    if ($location) { $location = Split-Path $location }
    if (is_dir $location) { return strip $location }

    Write-Verbose "No location found"
}
