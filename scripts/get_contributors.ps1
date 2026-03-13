[CmdletBinding()]
param(
    [string]$Repository = 'Devolutions/UniGetUI',
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}

function Resolve-OutputPath {
    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        return [System.IO.Path]::GetFullPath($OutputPath)
    }

    return Join-Path (Get-RepositoryRoot) 'src\UniGetUI.Core.Data\Assets\Data\Contributors.list'
}

function Write-Utf8Lines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string[]]$Lines
    )

    $directory = Split-Path -Path $Path -Parent
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -Path $directory -ItemType Directory -Force | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllLines($Path, $Lines, $encoding)
}

$resolvedOutputPath = Resolve-OutputPath
$contributorsUrl = "https://api.github.com/repos/$Repository/contributors?anon=1&per_page=100"

try {
    Write-Output 'Getting contributors...'
    $response = Invoke-RestMethod -Uri $contributorsUrl -Headers @{ 'User-Agent' = 'UniGetUI-Scripts' }
    $contributors = @(
        $response |
            Where-Object { $_.type -eq 'User' -and -not [string]::IsNullOrWhiteSpace($_.login) } |
            ForEach-Object { [string]$_.login }
    )

    Write-Utf8Lines -Path $resolvedOutputPath -Lines $contributors
    Write-Output "Wrote $($contributors.Count) contributor login(s) to: $resolvedOutputPath"
}
catch {
    Write-Error "Failed to fetch contributors: $($_.Exception.Message)"
    exit 1
}