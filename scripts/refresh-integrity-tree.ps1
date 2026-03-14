#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates IntegrityTree.json and validates it against the current folder contents.

.PARAMETER Path
    The directory whose IntegrityTree.json should be refreshed and validated.

.PARAMETER FailOnUnexpectedFiles
    Fail validation if files exist in the directory tree but are not present in
    IntegrityTree.json.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $Path,

    [switch] $FailOnUnexpectedFiles
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path -PathType Container)) {
    throw "The directory '$Path' does not exist."
}

$Path = (Resolve-Path $Path).Path
$GenerateScriptPath = Join-Path $PSScriptRoot 'generate-integrity-tree.ps1'
$VerifyScriptPath = Join-Path $PSScriptRoot 'verify-integrity-tree.ps1'

if (-not (Test-Path $GenerateScriptPath -PathType Leaf)) {
    throw "Integrity tree generator not found at '$GenerateScriptPath'."
}

if (-not (Test-Path $VerifyScriptPath -PathType Leaf)) {
    throw "Integrity tree validator not found at '$VerifyScriptPath'."
}

Write-Host "Refreshing integrity tree in $Path..."
& $GenerateScriptPath -Path $Path -MinOutput

$ValidationParameters = @{ Path = $Path }
if ($FailOnUnexpectedFiles) {
    $ValidationParameters.FailOnUnexpectedFiles = $true
}

& $VerifyScriptPath @ValidationParameters