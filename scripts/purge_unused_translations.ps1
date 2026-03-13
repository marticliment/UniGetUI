[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$EnglishFilePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$excludedDirectories = @(
    '.git',
    '.vs',
    'bin',
    'obj',
    'generated',
    'node_modules',
    'packages'
)

$excludedExtensions = @(
    '.7z',
    '.a',
    '.avi',
    '.bmp',
    '.class',
    '.db',
    '.dll',
    '.dylib',
    '.eot',
    '.exe',
    '.gif',
    '.gz',
    '.ico',
    '.jar',
    '.jpeg',
    '.jpg',
    '.lib',
    '.mp3',
    '.mp4',
    '.nupkg',
    '.obj',
    '.pdb',
    '.pdf',
    '.png',
    '.pyc',
    '.snupkg',
    '.so',
    '.svg',
    '.tar',
    '.ttf',
    '.wav',
    '.webm',
    '.webp',
    '.woff',
    '.woff2',
    '.zip'
)

function Resolve-RepositoryRoot {
    if (-not [string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        return [System.IO.Path]::GetFullPath($RepositoryRoot)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}

function Resolve-EnglishFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepositoryRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($EnglishFilePath)) {
        return [System.IO.Path]::GetFullPath($EnglishFilePath)
    }

    return Join-Path $ResolvedRepositoryRoot 'src\UniGetUI.Core.LanguageEngine\Assets\Languages\lang_en.json'
}

function Read-JsonObject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $content = [System.IO.File]::ReadAllText($Path)
    if ([string]::IsNullOrWhiteSpace($content)) {
        return [ordered]@{}
    }

    $parsed = $content | ConvertFrom-Json -AsHashtable
    if ($null -eq $parsed) {
        return [ordered]@{}
    }

    if (-not ($parsed -is [System.Collections.IDictionary])) {
        throw "JSON root must be an object: $Path"
    }

    return $parsed
}

function Test-IsExcludedFile {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo]$File,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedRepositoryRoot
    )

    $relativePath = [System.IO.Path]::GetRelativePath($ResolvedRepositoryRoot, $File.FullName).Replace('\', '/')
    if ($relativePath -like 'src/UniGetUI.Core.LanguageEngine/Assets/Languages/lang_*.json') {
        return $true
    }

    foreach ($segment in $relativePath.Split('/')) {
        if ($excludedDirectories -contains $segment) {
            return $true
        }
    }

    return $excludedExtensions -contains $File.Extension.ToLowerInvariant()
}

$resolvedRepositoryRoot = Resolve-RepositoryRoot
$resolvedEnglishFilePath = Resolve-EnglishFilePath -ResolvedRepositoryRoot $resolvedRepositoryRoot

if (-not (Test-Path -Path $resolvedEnglishFilePath -PathType Leaf)) {
    throw "English translation file not found: $resolvedEnglishFilePath"
}

$englishTranslations = Read-JsonObject -Path $resolvedEnglishFilePath
Write-Output "Working on $resolvedRepositoryRoot"

$contentBuilder = [System.Text.StringBuilder]::new()
$filesScanned = 0
Get-ChildItem -Path $resolvedRepositoryRoot -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-IsExcludedFile -File $_ -ResolvedRepositoryRoot $resolvedRepositoryRoot) } |
    ForEach-Object {
        $filesScanned += 1
        try {
            [void]$contentBuilder.Append([System.IO.File]::ReadAllText($_.FullName))
            [void]$contentBuilder.Append("`n################################ File division ################################`n")
        }
        catch {
        }
    }

$allContent = $contentBuilder.ToString()
$unusedKeys = New-Object System.Collections.Generic.List[string]

foreach ($entry in $englishTranslations.GetEnumerator()) {
    $key = [string]$entry.Key
    $candidates = @(
        $key,
        $key.Replace('"', '\"'),
        $key.Replace("`r`n", '\n').Replace("`n", '\n'),
        $key.Replace('"', '\"').Replace("`r`n", '\n').Replace("`n", '\n')
    ) | Select-Object -Unique

    $found = $false
    foreach ($candidate in $candidates) {
        if ($allContent.Contains($candidate, [System.StringComparison]::Ordinal)) {
            $found = $true
            break
        }
    }

    if (-not $found) {
        $unusedKeys.Add($key)
        Write-Output "Unused key: $key"
    }
}

Write-Output "Scan completed. Checked $filesScanned file(s); found $($unusedKeys.Count) unused key(s)."
exit 0