#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates a generated IntegrityTree.json against the files in a directory.

.DESCRIPTION
    This mirrors UniGetUI runtime integrity verification and can optionally fail
    if the directory contains files that are not listed in IntegrityTree.json.

.PARAMETER Path
    The directory containing IntegrityTree.json and the files to validate.

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
$IntegrityTreePath = Join-Path $Path 'IntegrityTree.json'

if (-not (Test-Path $IntegrityTreePath -PathType Leaf)) {
    throw "IntegrityTree.json was not found in '$Path'."
}

$rawData = Get-Content $IntegrityTreePath -Raw

try {
    $data = ConvertFrom-Json $rawData -AsHashtable
}
catch {
    throw "IntegrityTree.json is not valid JSON: $($_.Exception.Message)"
}

if ($null -eq $data) {
    throw 'IntegrityTree.json did not deserialize into a JSON object.'
}

$missingFiles = New-Object System.Collections.Generic.List[string]
$mismatchedFiles = New-Object System.Collections.Generic.List[string]
$unexpectedFiles = New-Object System.Collections.Generic.List[string]

$expectedFiles = @{}
foreach ($entry in $data.GetEnumerator()) {
    $relativePath = [string] $entry.Key
    $expectedHash = [string] $entry.Value
    $expectedFiles[$relativePath] = $true

    $fullPath = Join-Path $Path $relativePath
    if (-not (Test-Path $fullPath -PathType Leaf)) {
        $missingFiles.Add($relativePath)
        continue
    }

    $currentHash = (Get-FileHash $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($currentHash -ne $expectedHash.ToLowerInvariant()) {
        $mismatchedFiles.Add("$relativePath|expected=$expectedHash|got=$currentHash")
    }
}

if ($FailOnUnexpectedFiles) {
    Get-ChildItem $Path -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($Path.Length).TrimStart('\', '/') -replace '\\', '/'
        if ($relativePath -eq 'IntegrityTree.json') {
            return
        }

        if (-not $expectedFiles.ContainsKey($relativePath)) {
            $unexpectedFiles.Add($relativePath)
        }
    }
}

if ($missingFiles.Count -or $mismatchedFiles.Count -or $unexpectedFiles.Count) {
    if ($missingFiles.Count) {
        Write-Error "Missing files listed in IntegrityTree.json:`n - $($missingFiles -join "`n - ")"
    }

    if ($mismatchedFiles.Count) {
        Write-Error "Files with mismatched SHA256 values:`n - $($mismatchedFiles -join "`n - ")"
    }

    if ($unexpectedFiles.Count) {
        Write-Error "Unexpected files not present in IntegrityTree.json:`n - $($unexpectedFiles -join "`n - ")"
    }

    throw 'Integrity tree validation failed.'
}

$validatedFileCount = $data.Count
Write-Host "Integrity tree validation succeeded for $validatedFileCount file(s) in $Path"