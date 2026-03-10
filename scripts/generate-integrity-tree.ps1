#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates an IntegrityTree.json file containing SHA256 hashes of all files
    in the specified directory. Used at build time; verified at runtime by
    UniGetUI.Core.Tools.IntegrityTester.

.PARAMETER Path
    The directory to scan (typically the publish/output directory).

.PARAMETER MinOutput
    Suppress per-file progress output.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $Path,

    [switch] $MinOutput
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path -PathType Container)) {
    throw "The directory '$Path' does not exist."
}

$Path = (Resolve-Path $Path).Path
$OutputFileName = 'IntegrityTree.json'
$ScriptName = [System.IO.Path]::GetFileName($PSCommandPath)

$integrityData = [ordered]@{}

Get-ChildItem $Path -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Substring($Path.Length).TrimStart('\', '/') -replace '\\', '/'

    # Skip the output file itself
    if ($relativePath -eq $OutputFileName) { return }

    if (-not $MinOutput) {
        Write-Host " - Computing SHA256 of $relativePath..."
    }

    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
    $integrityData[$relativePath] = $hash
}

# Sort keys for deterministic output
$sorted = [ordered]@{}
foreach ($key in ($integrityData.Keys | Sort-Object)) {
    $sorted[$key] = $integrityData[$key]
}

$json = $sorted | ConvertTo-Json -Depth 1
Set-Content (Join-Path $Path $OutputFileName) $json -Encoding utf8NoBOM -NoNewline

Write-Host "Integrity tree was generated and saved to $Path/$OutputFileName"
