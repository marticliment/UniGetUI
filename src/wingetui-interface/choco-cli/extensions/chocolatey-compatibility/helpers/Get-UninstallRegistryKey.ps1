<#
.SYNOPSIS
    Retrieve registry key(s) for system-installed applications from an exact or wildcard search.

.DESCRIPTION
    This function will attempt to retrieve a matching registry key for an already installed application,
    usually to be used with a chocolateyUninstall.ps1 automation script.

    The function also prevents `Get-ItemProperty` from failing when handling wrongly encoded registry keys.

.PARAMETER SoftwareName
    Part or all of the Display Name as you see it in Programs and Features.
    It should be enough to be unique.
    The syntax follows the rules of the PowerShell `-like` operator, so the `*` character is interpreted
    as a wildcard, which matches any (zero or more) characters.

    If the display name contains a version number, such as "Launchy (2.5)", it is recommended you use a
    fuzzy search `"Launchy (*)"` (the wildcard `*`) so if Launchy auto-updates or is updated outside
    of chocolatey, the uninstall script will not fail.

    Take care not to abuse fuzzy/glob pattern searches. Be conscious of programs that may have shared
    or common root words to prevent overmatching. For example, "SketchUp*" would match two keys with software
    names "SketchUp 2016" and "SketchUp Viewer" that are different programs released by the same company.

.PARAMETER IgnoredArguments
    Allows splatting with arguments that do not apply and future expansion. Do not use directly.

.INPUTS
    System.String

.OUTPUTS
    PSCustomObject

.EXAMPLE
    [array]$key = Get-UninstallRegistryKey -SoftwareName "VLC media player"
    $key.UninstallString

    Exact match: software name in Programs and Features is "VLC media player"

.EXAMPLE
    [array]$key = Get-UninstallRegistryKey -SoftwareName "Gpg4win (*)"
    $key.UninstallString

    Version match: software name is "Gpg4Win (2.3.0)"

.EXAMPLE
    [array]$key = Get-UninstallRegistryKey -SoftwareName "SketchUp [0-9]*"
    $key.UninstallString

    Version match: software name is "SketchUp 2016"
    Note that the similar software name "SketchUp Viewer" would not be matched.

.LINK
    Uninstall-ChocolateyPackage
#>
function Get-UninstallRegistryKey {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SoftwareName,
    [parameter(ValueFromRemainingArguments = $true)]
    [Object[]] $IgnoredArguments
  )
  Write-Debug "Running 'Get-UninstallRegistryKey' for `'$env:ChocolateyPackageName`' with SoftwareName:`'$SoftwareName`'";

  $ErrorActionPreference = 'Stop'
  $local_key = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
  $machine_key = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
  $machine_key6432 = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'

  Write-Verbose "Retrieving all uninstall registry keys"
  [array]$keys = Get-ChildItem -Path @($machine_key6432, $machine_key, $local_key) -ea 0
  Write-Debug "Registry uninstall keys on system: $($keys.Count)"

  Write-Debug "Error handling check: `'Get-ItemProperty`' fails if a registry key is encoded incorrectly."
  [int]$maxAttempts = $keys.Count
  for ([int]$attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    $success = $false

    $keyPaths = $keys | Select-Object -ExpandProperty PSPath
    try {
      [array]$foundKey = Get-ItemProperty -Path $keyPaths -ea 0 | Where-Object { $_.DisplayName -like $SoftwareName }
      $success = $true
    }
    catch {
      Write-Debug "Found bad key."
      foreach ($key in $keys) { try { Get-ItemProperty $key.PsPath > $null } catch { $badKey = $key.PsPath } }
      Write-Verbose "Skipping bad key: $badKey"
      [array]$keys = $keys | Where-Object { $badKey -NotContains $_.PsPath }
    }

    if ($success) { break; }
    if ($attempt -eq 10) {
      Write-Warning "Found more than 10 bad registry keys. Run command again with `'--verbose --debug`' for more info."
      Write-Debug "Each key searched should correspond to an installed program. It is very unlikely to have more than a few programs with incorrectly encoded keys, if any at all. This may be indicative of one or more corrupted registry branches."
    }
  }

  Write-Debug "Found $($foundKey.Count) uninstall registry key(s) with SoftwareName:`'$SoftwareName`'";
  return $foundKey
}
