#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads and extracts the WinGet CLI bundle for the requested architectures.

.PARAMETER Version
    Winget-cli release tag (e.g. "v1.12.460"). Default: v1.12.470.

.PARAMETER Architectures
    Architectures to extract: x64, arm64, x86. Default: x64, arm64.

.PARAMETER DestinationRoot
    Root directory that will contain winget-cli_<arch> folders.

.PARAMETER Force
    Overwrite existing winget-cli_<arch> folders.
#>

[CmdletBinding()]
param(
    [string] $Version = "v1.12.470",
    [string[]] $Architectures = @("x64", "arm64"),
    [string] $DestinationRoot = (Join-Path $PSScriptRoot ".." "src" "UniGetUI.PackageEngine.Managers.WinGet"),
    [string] $UpstreamRepo = "",
    [string] $UpstreamRef = "main",
    [string] $UpstreamReferencePath = "",
    [string] $GitHubToken = "",
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Headers = @{
    "User-Agent" = "UniGetUI-build"
    "Accept" = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
    $GitHubToken = $env:GITHUB_TOKEN
}
if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
    $GitHubToken = $env:GH_TOKEN
}

if (-not [string]::IsNullOrWhiteSpace($GitHubToken)) {
    $Headers["Authorization"] = "Bearer $GitHubToken"
    Write-Host "Using authenticated GitHub API requests."
}
else {
    Write-Warning "No GitHub token found (GITHUB_TOKEN/GH_TOKEN). Requests may be rate-limited."
}

$BundledReferenceFiles = @{
    x64 = @(
        "concrt140_app.dll",
        "libsmartscreenn.dll",
        "Microsoft.Management.Configuration.dll",
        "Microsoft.Web.WebView2.Core.dll",
        "msvcp140_1_app.dll",
        "msvcp140_2_app.dll",
        "msvcp140_app.dll",
        "msvcp140_atomic_wait_app.dll",
        "resources.pri",
        "vcamp140_app.dll",
        "vccorlib140_app.dll",
        "vcomp140_app.dll",
        "vcruntime140_1_app.dll",
        "vcruntime140_app.dll",
        "WindowsPackageManager.dll",
        "WindowsPackageManagerServer.exe",
        "winget.exe"
    )
    arm64 = @(
        "concrt140_app.dll",
        "libsmartscreenn.dll",
        "Microsoft.Management.Configuration.dll",
        "Microsoft.Web.WebView2.Core.dll",
        "msvcp140_1_app.dll",
        "msvcp140_2_app.dll",
        "msvcp140_app.dll",
        "msvcp140_atomic_wait_app.dll",
        "resources.pri",
        "vcamp140_app.dll",
        "vccorlib140_app.dll",
        "vcomp140_app.dll",
        "vcruntime140_1_app.dll",
        "vcruntime140_app.dll",
        "WindowsPackageManager.dll",
        "WindowsPackageManagerServer.exe",
        "winget.exe"
    )
    x86 = @(
        "concrt140_app.dll",
        "libsmartscreenn.dll",
        "Microsoft.Management.Configuration.dll",
        "Microsoft.Web.WebView2.Core.dll",
        "msvcp140_1_app.dll",
        "msvcp140_2_app.dll",
        "msvcp140_app.dll",
        "msvcp140_atomic_wait_app.dll",
        "resources.pri",
        "vcamp140_app.dll",
        "vccorlib140_app.dll",
        "vcomp140_app.dll",
        "vcruntime140_1_app.dll",
        "vcruntime140_app.dll",
        "WindowsPackageManager.dll",
        "WindowsPackageManagerServer.exe",
        "winget.exe"
    )
}

function Get-ReleaseInfo {
    param([string] $Tag)

    if ([string]::IsNullOrWhiteSpace($Tag) -or $Tag -eq "latest") {
        $url = "https://api.github.com/repos/microsoft/winget-cli/releases/latest"
    } else {
        if (-not $Tag.StartsWith("v")) { $Tag = "v$Tag" }
        $url = "https://api.github.com/repos/microsoft/winget-cli/releases/tags/$Tag"
    }

    return Invoke-RestMethod -Uri $url -Headers $Headers
}

function Find-AssetUrl {
    param(
        [object] $Release,
        [string] $AssetName
    )

    $asset = $Release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1
    if (-not $asset) {
        throw "Release asset '$AssetName' not found."
    }

    return $asset.browser_download_url
}

function Get-UpstreamReferenceFiles {
    param(
        [string] $Repository,
        [string] $Ref,
        [string] $DirectoryPath
    )

    $apiUrl = "https://api.github.com/repos/${Repository}/contents/${DirectoryPath}?ref=${Ref}"
    $items = Invoke-RestMethod -Uri $apiUrl -Headers $Headers

    if (-not $items) {
        throw "Upstream directory '$DirectoryPath' from '$Repository@$Ref' returned no entries."
    }

    $files = @(
        $items |
            Where-Object { $_.type -eq "file" } |
            Select-Object -ExpandProperty name |
            Sort-Object -Unique
    )

    if ($files.Count -eq 0) {
        throw "Upstream directory '$DirectoryPath' from '$Repository@$Ref' contains no files."
    }

    return $files
}

function Resolve-UpstreamReferencePathForArchitecture {
    param(
        [string] $BasePath,
        [string] $Architecture
    )

    $archKey = $Architecture.ToLowerInvariant()

    if ($BasePath -match 'winget-cli_[^/\\]+$') {
        return ($BasePath -replace 'winget-cli_[^/\\]+$', "winget-cli_$archKey")
    }

    return $BasePath
}

function Get-BundledReferenceFiles {
    param([string] $Architecture)

    $archKey = $Architecture.ToLowerInvariant()
    if ($BundledReferenceFiles.ContainsKey($archKey)) {
        return @($BundledReferenceFiles[$archKey])
    }

    return @($BundledReferenceFiles["x64"])
}

function Get-ReferenceFiles {
    param([string] $Architecture)

    $archKey = $Architecture.ToLowerInvariant()
    $upstreamConfigured = -not [string]::IsNullOrWhiteSpace($UpstreamRepo) -and -not [string]::IsNullOrWhiteSpace($UpstreamReferencePath)

    if ($upstreamConfigured) {
        $upstreamReferencePathForArch = Resolve-UpstreamReferencePathForArchitecture -BasePath $UpstreamReferencePath -Architecture $archKey

        try {
            $upstreamFiles = Get-UpstreamReferenceFiles -Repository $UpstreamRepo -Ref $UpstreamRef -DirectoryPath $upstreamReferencePathForArch

            return @{
                Files = @($upstreamFiles)
                SourceDescription = "$UpstreamRepo@$UpstreamRef/$upstreamReferencePathForArch"
            }
        }
        catch {
            if ($upstreamReferencePathForArch -ne $UpstreamReferencePath) {
                Write-Warning "Unable to load architecture-specific upstream reference '$upstreamReferencePathForArch'. Falling back to '$UpstreamReferencePath'."

                try {
                    $upstreamFiles = Get-UpstreamReferenceFiles -Repository $UpstreamRepo -Ref $UpstreamRef -DirectoryPath $UpstreamReferencePath

                    return @{
                        Files = @($upstreamFiles)
                        SourceDescription = "$UpstreamRepo@$UpstreamRef/$UpstreamReferencePath"
                    }
                }
                catch {
                    Write-Warning "Unable to load upstream reference '$UpstreamRepo@$UpstreamRef/$UpstreamReferencePath'. Falling back to bundled reference manifest."
                }
            }
            else {
                Write-Warning "Unable to load upstream reference '$UpstreamRepo@$UpstreamRef/$UpstreamReferencePath'. Falling back to bundled reference manifest."
            }
        }
    }

    return @{
        Files = @(Get-BundledReferenceFiles -Architecture $archKey)
        SourceDescription = "bundled manifest ($archKey)"
    }
}

$release = Get-ReleaseInfo -Tag $Version
Write-Host "Using winget-cli release: $($release.tag_name)"

$bundleUrl = Find-AssetUrl -Release $release -AssetName "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle"
$depsUrl = Find-AssetUrl -Release $release -AssetName "DesktopAppInstaller_Dependencies.zip"

$tempRoot = Join-Path $env:TEMP "winget-cli-$([Guid]::NewGuid().ToString('N'))"
$bundlePath = Join-Path $tempRoot "bundle.msixbundle"
$depsPath = Join-Path $tempRoot "deps.zip"
$bundleDir = Join-Path $tempRoot "bundle"
$depsDir = Join-Path $tempRoot "deps"

New-Item $tempRoot -ItemType Directory | Out-Null

Write-Host "Downloading msixbundle..."
Invoke-WebRequest -Uri $bundleUrl -OutFile $bundlePath -Headers $Headers

Write-Host "Downloading dependencies..."
Invoke-WebRequest -Uri $depsUrl -OutFile $depsPath -Headers $Headers

Write-Host "Extracting bundle..."
Expand-Archive -Path $bundlePath -DestinationPath $bundleDir -Force

Write-Host "Extracting dependencies..."
Expand-Archive -Path $depsPath -DestinationPath $depsDir -Force

foreach ($arch in $Architectures) {
    $archKey = $arch.ToLowerInvariant()
    $reference = Get-ReferenceFiles -Architecture $archKey
    $expectedFiles = @($reference.Files)

    Write-Host "[$archKey] Using reference list from $($reference.SourceDescription) ($($expectedFiles.Count) files)"

    $msix = Get-ChildItem $bundleDir -Filter "*${archKey}*.msix" -Recurse | Select-Object -First 1
    if (-not $msix) {
        throw "No msix found for architecture '$arch'."
    }

    $msixDir = Join-Path $tempRoot "msix-$archKey"
    Expand-Archive -Path $msix.FullName -DestinationPath $msixDir -Force

    $destDir = Join-Path $DestinationRoot "winget-cli_$archKey"
    if (Test-Path $destDir) {
        if ($Force) {
            Remove-Item $destDir -Recurse -Force
        } else {
            throw "Destination folder already exists: $destDir (use -Force to overwrite)"
        }
    }
    New-Item $destDir -ItemType Directory | Out-Null

    $depsArchDir = Join-Path $depsDir $archKey
    if (-not (Test-Path $depsArchDir)) { $depsArchDir = $depsDir }

    foreach ($fileName in $expectedFiles) {
        $destinationFile = Join-Path $destDir $fileName
        $source = Get-ChildItem $msixDir -Recurse -Filter $fileName -File | Select-Object -First 1
        if (-not $source) {
            $source = Get-ChildItem $depsArchDir -Recurse -Filter $fileName -File | Select-Object -First 1
        }
        if (-not $source) {
            if (-not [string]::IsNullOrWhiteSpace($UpstreamRepo) -and -not [string]::IsNullOrWhiteSpace($UpstreamReferencePath)) {
                $upstreamReferencePathForArch = Resolve-UpstreamReferencePathForArchitecture -BasePath $UpstreamReferencePath -Architecture $archKey
                $fallbackUrl = "https://raw.githubusercontent.com/${UpstreamRepo}/${UpstreamRef}/${upstreamReferencePathForArch}/${fileName}"
                Write-Warning "Release payload missing '$fileName' for $arch. Downloading fallback from upstream reference."
                try {
                    Invoke-WebRequest -Uri $fallbackUrl -OutFile $destinationFile -Headers $Headers
                    continue
                }
                catch {
                    throw "Required upstream file '$fileName' not found for $arch and fallback download failed: $fallbackUrl"
                }
            }

            throw "Required file '$fileName' not found for $arch in the winget release payload."
        }

        Copy-Item $source.FullName $destinationFile -Force
    }

    $copiedFiles = @(
        Get-ChildItem $destDir -File |
            Select-Object -ExpandProperty Name |
            Sort-Object -Unique
    )

    $missingFiles = @($expectedFiles | Where-Object { $_ -notin $copiedFiles })
    $extraFiles = @($copiedFiles | Where-Object { $_ -notin $expectedFiles })

    if ($missingFiles.Count -gt 0 -or $extraFiles.Count -gt 0) {
        throw "Validation failed for $arch. Missing: $($missingFiles -join ', '). Extra: $($extraFiles -join ', ')"
    }

    Write-Host "Prepared $destDir"
}

Remove-Item $tempRoot -Recurse -Force
Write-Host "WinGet CLI bundle extracted successfully."
