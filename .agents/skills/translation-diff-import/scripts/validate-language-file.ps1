param(
    [Parameter(Mandatory = $true)]
    [string]$NeutralJson,

    [Parameter(Mandatory = $true)]
    [string]$TargetJson,

    [string]$PatchJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '..\..\translation-diff-export\scripts\TranslationDiff.JsonTools.ps1')

$neutralPath = Get-FullPath -Path $NeutralJson
$targetPath = Get-FullPath -Path $TargetJson

if (-not (Test-Path -Path $neutralPath -PathType Leaf)) {
    throw "Neutral JSON file not found: $neutralPath"
}

if (-not (Test-Path -Path $targetPath -PathType Leaf)) {
    throw "Target JSON file not found: $targetPath"
}

$neutralMap = Read-OrderedJsonMap -Path $neutralPath
$targetMap = Read-OrderedJsonMap -Path $targetPath

$keysToValidate = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($PatchJson)) {
    $patchPath = Get-FullPath -Path $PatchJson
    if (-not (Test-Path -Path $patchPath -PathType Leaf)) {
        throw "Patch JSON file not found: $patchPath"
    }

    $patchMap = Read-OrderedJsonMap -Path $patchPath
    foreach ($entry in $patchMap.GetEnumerator()) {
        $keysToValidate.Add([string]$entry.Key)
    }
}
else {
    foreach ($entry in $targetMap.GetEnumerator()) {
        $keysToValidate.Add([string]$entry.Key)
    }
}

$validatedCount = 0
foreach ($key in $keysToValidate) {
    if (-not $neutralMap.Contains($key)) {
        throw "Target file contains key '$key' that does not exist in the neutral language file."
    }

    if (-not $targetMap.Contains($key)) {
        throw "Target file does not contain required key '$key'."
    }

    $targetValue = [string]$targetMap[$key]
    Assert-TranslationStructure -SourceValue ([string]$neutralMap[$key]) -TranslatedValue $targetValue -Key $key
    $validatedCount += 1
}

Write-Output "Validated $validatedCount translated entries against: $neutralPath"