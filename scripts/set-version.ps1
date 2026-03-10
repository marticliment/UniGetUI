#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stamps version information into all required source files.
    CI-friendly replacement for the interactive scripts/apply_versions.py.

.PARAMETER Version
    Semantic version string, e.g. "3.3.7" or "3.4.0-beta1".
    A four-part version (X.X.X.X) is derived automatically for assembly info.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Version
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

# Derive four-part version: 3.3.7 -> 3.3.7.0, 3.4.0-beta1 -> 3.4.0.0
$CleanVersion = ($Version -Split '-')[0]   # strip prerelease tag
$Parts = $CleanVersion -Split '\.'
while ($Parts.Count -lt 4) { $Parts += '0' }
$FourPartVersion = ($Parts[0..3]) -Join '.'

Write-Host "Version name  : $Version"
Write-Host "Assembly ver  : $FourPartVersion"

# --- Bump build number ---
$BuildNumberFile = Join-Path $PSScriptRoot "BuildNumber"
$BuildNumber = 0
if (Test-Path $BuildNumberFile) {
    $BuildNumber = [int](Get-Content $BuildNumberFile -Raw).Trim()
}
$BuildNumber++
Set-Content $BuildNumberFile $BuildNumber
Write-Host "Build number  : $BuildNumber"

# --- Helper: replace lines in a file by prefix match ---
function Set-LinesByPrefix {
    param(
        [string] $FilePath,
        [hashtable] $Replacements,
        [string] $Encoding = 'utf8BOM'
    )

    if (-not (Test-Path $FilePath)) {
        Write-Warning "File not found, skipping: $FilePath"
        return
    }

    $lines = Get-Content $FilePath -Encoding $Encoding
    $output = foreach ($line in $lines) {
        $matched = $false
        foreach ($prefix in $Replacements.Keys) {
            if ($line.TrimStart().StartsWith($prefix)) {
                $Replacements[$prefix]
                $matched = $true
                break
            }
        }
        if (-not $matched) { $line }
    }
    $output | Set-Content $FilePath -Encoding $Encoding
}

# --- CoreData.cs ---
Set-LinesByPrefix -FilePath (Join-Path $RepoRoot "src" "UniGetUI.Core.Data" "CoreData.cs") -Replacements @{
    'public const string VersionName =' = "        public const string VersionName = `"$Version`"; // Do not modify this line, use file scripts/apply_versions.py"
    'public const int BuildNumber ='    = "        public const int BuildNumber = $BuildNumber; // Do not modify this line, use file scripts/apply_versions.py"
}

# --- SharedAssemblyInfo.cs ---
Set-LinesByPrefix -FilePath (Join-Path $RepoRoot "src" "SharedAssemblyInfo.cs") -Replacements @{
    '[assembly: AssemblyVersion("'              = "[assembly: AssemblyVersion(`"$FourPartVersion`")]"
    '[assembly: AssemblyFileVersion("'          = "[assembly: AssemblyFileVersion(`"$FourPartVersion`")]"
    '[assembly: AssemblyInformationalVersion("' = "[assembly: AssemblyInformationalVersion(`"$Version`")]"
}

# --- UniGetUI.iss ---
Set-LinesByPrefix -FilePath (Join-Path $RepoRoot "UniGetUI.iss") -Replacements @{
    '#define MyAppVersion'   = "#define MyAppVersion `"$Version`""
    'VersionInfoVersion='    = "VersionInfoVersion=$FourPartVersion"
    'VersionInfoProductVersion=' = "VersionInfoProductVersion=$FourPartVersion"
}

# --- app.manifest (only the assemblyIdentity version, not manifestVersion) ---
$ManifestPath = Join-Path $RepoRoot "src" "UniGetUI" "app.manifest"
if (Test-Path $ManifestPath) {
    $content = Get-Content $ManifestPath -Raw -Encoding utf8BOM
    $content = $content -Replace '(?<!manifest)(version=\s*")[^"]*(")', "`${1}$FourPartVersion`${2}"
    Set-Content $ManifestPath $content -Encoding utf8BOM -NoNewline
}

Write-Host "Version stamped successfully."
