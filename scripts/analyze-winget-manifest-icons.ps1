[CmdletBinding()]
param(
    [ValidateSet('Published', 'Git')]
    [string]$Mode = 'Published',

    [string]$RepositoryPath,

    [string]$RemoteUrl = 'https://github.com/microsoft/winget-pkgs',

    [string]$Branch = 'master',

    [string]$SourceName = 'winget',

    [string]$PackageId,

    [string]$OutputPath,

    [switch]$SkipRepositoryUpdate,

    [switch]$DownloadIcons,

    [string]$DownloadDirectory,

    [string[]]$PreferredTheme = @('__unspecified__', 'Default', 'Light', 'Dark', 'HighContrast'),

    [string[]]$PreferredFileType = @('png', 'ico', 'svg', 'jpg', 'jpeg', 'webp'),

    [int]$DownloadLimit = 0,

    [ValidateRange(1, 50000)]
    [int]$PublishedChunkSize = 1500,

    [ValidateRange(1, 32)]
    [int]$PublishedParallelism = 4
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-DefaultRepositoryPath {
    if (-not [string]::IsNullOrWhiteSpace($RepositoryPath)) {
        return $RepositoryPath
    }

    $basePath = if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $env:LOCALAPPDATA
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:TEMP)) {
        $env:TEMP
    }
    else {
        (Get-Location).Path
    }

    return Join-Path $basePath 'UniGetUI\Caches\winget-pkgs'
}

function Resolve-DefaultDownloadDirectory {
    param(
        [string]$RepoRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($DownloadDirectory)) {
        return $DownloadDirectory
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        return Join-Path $PSScriptRoot 'downloaded-icons'
    }

    return Join-Path $RepoRoot 'downloaded-icons'
}

function Test-CommandAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Resolve-PublishedAnalyzerProjectPath {
    $projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\UniGetUI.Tools.WingetPublishedIconAnalysis\UniGetUI.Tools.WingetPublishedIconAnalysis.csproj'))
    if (-not (Test-Path $projectPath)) {
        throw "Published analyzer project was not found at '$projectPath'."
    }

    return $projectPath
}

function Invoke-Dotnet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Verbose (('dotnet ' + ($Arguments -join ' ')))
    & dotnet @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Resolve-PublishedAnalyzerExecutablePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AnalyzerProjectPath
    )

    Invoke-Dotnet -Arguments @(
        'build',
        $AnalyzerProjectPath,
        '--configuration', 'Release',
        '--nologo'
    )

    $projectDirectory = Split-Path -Parent $AnalyzerProjectPath
    $executable = Get-ChildItem -Path (Join-Path $projectDirectory 'bin\Release') -Filter 'UniGetUI.Tools.WingetPublishedIconAnalysis.exe' -File -Recurse |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($null -eq $executable) {
        throw "Published analyzer executable was not found under '$projectDirectory\\bin\\Release'."
    }

    return $executable.FullName
}

function Invoke-PublishedAnalyzerExecutable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AnalyzerExecutablePath,

        [Parameter(Mandatory = $true)]
        [string]$PublishedSourceName,

        [string]$FilterPackageId,

        [int]$Skip = 0,

        [int]$Take = 0
    )

    $temporaryOutputPath = Join-Path ([System.IO.Path]::GetTempPath()) ("winget-published-icon-analysis.{0}.json" -f ([guid]::NewGuid().ToString('N')))
    try {
        $arguments = @(
            '--source-name', $PublishedSourceName,
            '--output', $temporaryOutputPath
        )

        if ($Skip -gt 0) {
            $arguments += @('--skip', "$Skip")
        }

        if ($Take -gt 0) {
            $arguments += @('--take', "$Take")
        }

        if (-not [string]::IsNullOrWhiteSpace($FilterPackageId)) {
            $arguments += @('--package-id', $FilterPackageId)
        }

        & $AnalyzerExecutablePath @arguments 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Published analyzer executable failed with exit code $LASTEXITCODE."
        }

        return Get-Content -Path $temporaryOutputPath -Raw | ConvertFrom-Json -Depth 10
    }
    finally {
        if (Test-Path $temporaryOutputPath) {
            Remove-Item -Path $temporaryOutputPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Merge-PublishedWingetAnalysisChunks {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Chunks
    )

    $mergedPackages = @($Chunks | ForEach-Object { $_.Packages })
    $mergedFailures = @($Chunks | ForEach-Object { $_.Failures })
    $processedPackages = [int](($Chunks | Measure-Object -Property ProcessedPackages -Sum).Sum)

    return [pscustomobject]@{
        SourceName       = [string]$Chunks[0].SourceName
        TotalPackages    = [int]$Chunks[0].TotalPackages
        ProcessedPackages = $processedPackages
        Packages         = $mergedPackages
        Failures         = $mergedFailures
    }
}

function Invoke-PublishedWingetIconAnalysis {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AnalyzerProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$PublishedSourceName,

        [string]$FilterPackageId,

        [Parameter(Mandatory = $true)]
        [int]$ChunkSize,

        [Parameter(Mandatory = $true)]
        [int]$Parallelism
    )

    $analyzerExecutablePath = Resolve-PublishedAnalyzerExecutablePath -AnalyzerProjectPath $AnalyzerProjectPath

    if (-not [string]::IsNullOrWhiteSpace($FilterPackageId)) {
        return Invoke-PublishedAnalyzerExecutable -AnalyzerExecutablePath $analyzerExecutablePath -PublishedSourceName $PublishedSourceName -FilterPackageId $FilterPackageId
    }

    Write-Host "Running published scan in chunks of $ChunkSize package(s) with parallelism $Parallelism"
    $firstChunk = Invoke-PublishedAnalyzerExecutable -AnalyzerExecutablePath $analyzerExecutablePath -PublishedSourceName $PublishedSourceName -Take $ChunkSize
    $totalPackages = [int]$firstChunk.TotalPackages
    if ($totalPackages -le $ChunkSize) {
        return $firstChunk
    }

    $allChunks = [System.Collections.Generic.List[object]]::new()
    $allChunks.Add($firstChunk)

    $chunkStarts = [System.Collections.Generic.List[int]]::new()
    for ($skip = $ChunkSize; $skip -lt $totalPackages; $skip += $ChunkSize) {
        $chunkStarts.Add($skip)
    }

    for ($batchStart = 0; $batchStart -lt $chunkStarts.Count; $batchStart += $Parallelism) {
        $batchEnd = [Math]::Min($batchStart + $Parallelism - 1, $chunkStarts.Count - 1)
        $jobs = @()
        for ($index = $batchStart; $index -le $batchEnd; $index++) {
            $skip = $chunkStarts[$index]
            $jobs += Start-Job -ScriptBlock {
                param($ExecutablePath, $SourceName, $ChunkSkip, $ChunkTake)

                $temporaryOutputPath = Join-Path ([System.IO.Path]::GetTempPath()) ("winget-published-icon-analysis.{0}.json" -f ([guid]::NewGuid().ToString('N')))
                try {
                    & $ExecutablePath --source-name $SourceName --skip $ChunkSkip --take $ChunkTake --output $temporaryOutputPath *> $null
                    if ($LASTEXITCODE -ne 0) {
                        throw "Published analyzer executable failed with exit code $LASTEXITCODE for chunk starting at $ChunkSkip."
                    }

                    Get-Content -Path $temporaryOutputPath -Raw | ConvertFrom-Json -Depth 10
                }
                finally {
                    if (Test-Path $temporaryOutputPath) {
                        Remove-Item -Path $temporaryOutputPath -Force -ErrorAction SilentlyContinue
                    }
                }
            } -ArgumentList $analyzerExecutablePath, $PublishedSourceName, $skip, $ChunkSize
        }

        Wait-Job -Job $jobs | Out-Null
        foreach ($job in $jobs) {
            try {
                $allChunks.Add((Receive-Job -Job $job -ErrorAction Stop))
            }
            finally {
                Remove-Job -Job $job -Force
            }
        }

        $completedChunks = [Math]::Min($batchEnd + 1, $chunkStarts.Count)
        Write-Host "Completed $completedChunks/$($chunkStarts.Count) background chunk(s)"
    }

    return Merge-PublishedWingetAnalysisChunks -Chunks $allChunks.ToArray()
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [string]$WorkingDirectory
    )

    $commandDisplay = @('git') + $Arguments
    Write-Verbose ($commandDisplay -join ' ')

    $output = if ($WorkingDirectory) {
        Push-Location $WorkingDirectory
        try {
            & git @Arguments 2>&1
        }
        finally {
            Pop-Location
        }
    }
    else {
        & git @Arguments 2>&1
    }

    if ($LASTEXITCODE -ne 0) {
        $message = ($output | Out-String).Trim()
        throw "Git command failed: $($commandDisplay -join ' ')`n$message"
    }

    return $output
}

function Initialize-WingetPkgsRepository {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$RepoUrl,

        [Parameter(Mandatory = $true)]
        [string]$RepoBranch,

        [Parameter(Mandatory = $true)]
        [bool]$ShouldUpdate
    )

    $gitDirectory = Join-Path $RepoRoot '.git'
    if (-not (Test-Path $gitDirectory)) {
        $parent = Split-Path -Parent $RepoRoot
        if ($parent) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }

        Write-Host "Cloning $RepoUrl into $RepoRoot"
        Invoke-Git -Arguments @(
            'clone',
            '--depth', '1',
            '--filter=blob:none',
            '--sparse',
            '--single-branch',
            '--branch', $RepoBranch,
            $RepoUrl,
            $RepoRoot
        )
        Invoke-Git -Arguments @('sparse-checkout', 'set', 'manifests') -WorkingDirectory $RepoRoot | Out-Null
        return
    }

    if (-not $ShouldUpdate) {
        return
    }

    Write-Host "Updating $RepoRoot"
    Invoke-Git -Arguments @('remote', 'set-branches', 'origin', $RepoBranch) -WorkingDirectory $RepoRoot | Out-Null
    Invoke-Git -Arguments @('fetch', 'origin', $RepoBranch, '--depth', '1') -WorkingDirectory $RepoRoot | Out-Null
    Invoke-Git -Arguments @('sparse-checkout', 'set', 'manifests') -WorkingDirectory $RepoRoot | Out-Null
    Invoke-Git -Arguments @('checkout', $RepoBranch) -WorkingDirectory $RepoRoot | Out-Null
    Invoke-Git -Arguments @('reset', '--hard', "origin/$RepoBranch") -WorkingDirectory $RepoRoot | Out-Null
}

function Get-VersionSortKey {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $tokens = [System.Collections.Generic.List[object]]::new()
    foreach ($match in [regex]::Matches($Version, '\d+|[A-Za-z]+')) {
        $value = $match.Value
        if ($value -match '^\d+$') {
            $tokens.Add([pscustomobject]@{
                Kind  = 0
                Value = [System.Numerics.BigInteger]::Parse($value)
            })
        }
        else {
            $tokens.Add([pscustomobject]@{
                Kind  = 1
                Value = $value.ToLowerInvariant()
            })
        }
    }

    return $tokens
}

function Compare-VersionStrings {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Left,

        [Parameter(Mandatory = $true)]
        [string]$Right
    )

    if ($Left -eq $Right) {
        return 0
    }

    $leftTokens = Get-VersionSortKey -Version $Left
    $rightTokens = Get-VersionSortKey -Version $Right
    $maxCount = [Math]::Max($leftTokens.Count, $rightTokens.Count)

    for ($index = 0; $index -lt $maxCount; $index++) {
        if ($index -ge $leftTokens.Count) {
            return -1
        }

        if ($index -ge $rightTokens.Count) {
            return 1
        }

        $leftToken = $leftTokens[$index]
        $rightToken = $rightTokens[$index]
        if ($leftToken.Kind -ne $rightToken.Kind) {
            if ($leftToken.Kind -lt $rightToken.Kind) {
                return 1
            }

            return -1
        }

        if ($leftToken.Kind -eq 0) {
            if ($leftToken.Value -lt $rightToken.Value) {
                return -1
            }

            if ($leftToken.Value -gt $rightToken.Value) {
                return 1
            }
        }
        else {
            $comparison = [string]::CompareOrdinal([string]$leftToken.Value, [string]$rightToken.Value)
            if ($comparison -lt 0) {
                return -1
            }

            if ($comparison -gt 0) {
                return 1
            }
        }
    }

    return [string]::CompareOrdinal($Left, $Right)
}

function Get-PackageMetadataFromVersionDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DirectoryPath,

        [Parameter(Mandatory = $true)]
        [string]$ManifestsRoot
    )

    $relativePath = [System.IO.Path]::GetRelativePath($ManifestsRoot, $DirectoryPath)
    $segments = $relativePath.Split([System.IO.Path]::DirectorySeparatorChar, [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($segments.Count -lt 3) {
        return $null
    }

    $packageSegments = $segments[1..($segments.Count - 2)]
    if ($packageSegments.Count -eq 0) {
        return $null
    }

    return [pscustomobject]@{
        PackageId     = ($packageSegments -join '.')
        PackagePath   = ($packageSegments -join [System.IO.Path]::DirectorySeparatorChar)
        PackageVersion = $segments[-1]
        DirectoryPath = $DirectoryPath
    }
}

function Parse-ResolutionArea {
    param(
        [string]$Resolution
    )

    if ([string]::IsNullOrWhiteSpace($Resolution)) {
        return 0
    }

    if ($Resolution -match '^(?<width>\d+)x(?<height>\d+)$') {
        return [int]$Matches.width * [int]$Matches.height
    }

    return 0
}

function Select-BestManifestIcon {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Icons,

        [Parameter(Mandatory = $true)]
        [string[]]$ThemePreference,

        [Parameter(Mandatory = $true)]
        [string[]]$FileTypePreference
    )

    $themeRank = @{}
    for ($index = 0; $index -lt $ThemePreference.Count; $index++) {
        $themeRank[$ThemePreference[$index].ToLowerInvariant()] = $index
    }

    $fileTypeRank = @{}
    for ($index = 0; $index -lt $FileTypePreference.Count; $index++) {
        $fileTypeRank[$FileTypePreference[$index].ToLowerInvariant()] = $index
    }

    $ranked = foreach ($icon in $Icons) {
        if ([string]::IsNullOrWhiteSpace([string]$icon.IconUrl)) {
            continue
        }

        $theme = [string]$icon.IconTheme
        $fileType = [string]$icon.IconFileType
        $themeKey = if ([string]::IsNullOrWhiteSpace($theme)) { '__unspecified__' } else { $theme.ToLowerInvariant() }
        $fileTypeKey = $fileType.ToLowerInvariant()
        $resolvedThemeRank = if ($themeRank.ContainsKey($themeKey)) { $themeRank[$themeKey] } else { [int]::MaxValue }
        $resolvedFileTypeRank = if ($fileTypeRank.ContainsKey($fileTypeKey)) { $fileTypeRank[$fileTypeKey] } else { [int]::MaxValue }

        [pscustomobject]@{
            Icon            = $icon
            ThemeRank       = $resolvedThemeRank
            FileTypeRank    = $resolvedFileTypeRank
            ResolutionArea  = Parse-ResolutionArea -Resolution ([string]$icon.IconResolution)
            IconUrl         = [string]$icon.IconUrl
        }
    }

    return $ranked |
        Sort-Object -Property @{ Expression = 'ThemeRank'; Ascending = $true }, @{ Expression = 'FileTypeRank'; Ascending = $true }, @{ Expression = 'ResolutionArea'; Ascending = $false }, @{ Expression = 'IconUrl'; Ascending = $true } |
        Select-Object -First 1 -ExpandProperty Icon
}

function Get-LatestPackageVersionMap {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$ManifestFiles,

        [Parameter(Mandatory = $true)]
        [string]$ManifestsRoot,

        [string]$FilterPackageId
    )

    $versionDirectories = @{}
    foreach ($file in $ManifestFiles) {
        $directoryPath = Split-Path -Parent $file.FullName
        if ($versionDirectories.ContainsKey($directoryPath)) {
            continue
        }

        $metadata = Get-PackageMetadataFromVersionDirectory -DirectoryPath $directoryPath -ManifestsRoot $ManifestsRoot
        if ($null -eq $metadata) {
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($FilterPackageId) -and $metadata.PackageId -ne $FilterPackageId) {
            continue
        }

        $versionDirectories[$directoryPath] = $metadata
    }

    $latestByPackage = @{}
    foreach ($metadata in $versionDirectories.Values) {
        if (-not $latestByPackage.ContainsKey($metadata.PackageId)) {
            $latestByPackage[$metadata.PackageId] = $metadata
            continue
        }

        $current = $latestByPackage[$metadata.PackageId]
        if ((Compare-VersionStrings -Left $metadata.PackageVersion -Right $current.PackageVersion) -gt 0) {
            $latestByPackage[$metadata.PackageId] = $metadata
        }
    }

    return $latestByPackage
}

function Get-ManifestIconPackages {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$LatestPackages,

        [Parameter(Mandatory = $true)]
        [string[]]$ThemePreference,

        [Parameter(Mandatory = $true)]
        [string[]]$FileTypePreference
    )

    $latestYamlFiles = foreach ($package in $LatestPackages) {
        Get-ChildItem -Path $package.DirectoryPath -Filter '*.yaml' -File
    }

    $iconBearingFiles = @(
        Select-String -Path $latestYamlFiles.FullName -Pattern '^\s*Icons:\s*$' | Select-Object -ExpandProperty Path -Unique
    )

    $filesByDirectory = @{}
    foreach ($path in $iconBearingFiles) {
        $directory = Split-Path -Parent $path
        if (-not $filesByDirectory.ContainsKey($directory)) {
            $filesByDirectory[$directory] = [System.Collections.Generic.List[string]]::new()
        }

        $filesByDirectory[$directory].Add($path)
    }

    $packagesWithIcons = [System.Collections.Generic.List[object]]::new()
    foreach ($package in $LatestPackages) {
        if (-not $filesByDirectory.ContainsKey($package.DirectoryPath)) {
            continue
        }

        $icons = [System.Collections.Generic.List[object]]::new()
        $manifestFiles = [System.Collections.Generic.List[object]]::new()
        foreach ($manifestPath in $filesByDirectory[$package.DirectoryPath]) {
            $yaml = Get-Content -Path $manifestPath -Raw | ConvertFrom-Yaml -Ordered
            $manifestIcons = @($yaml.Icons)
            if ($manifestIcons.Count -eq 0) {
                continue
            }

            $relativeManifestPath = [System.IO.Path]::GetFileName($manifestPath)
            foreach ($icon in $manifestIcons) {
                $icons.Add([pscustomobject]@{
                    IconUrl        = [string]$icon.IconUrl
                    IconFileType   = [string]$icon.IconFileType
                    IconResolution = [string]$icon.IconResolution
                    IconTheme      = [string]$icon.IconTheme
                    IconSha256     = [string]$icon.IconSha256
                    ManifestFile   = $relativeManifestPath
                    ManifestType   = [string]$yaml.ManifestType
                    PackageLocale  = [string]$yaml.PackageLocale
                })
            }

            $manifestFiles.Add([pscustomobject]@{
                FileName      = $relativeManifestPath
                ManifestType  = [string]$yaml.ManifestType
                PackageLocale = [string]$yaml.PackageLocale
                IconCount     = $manifestIcons.Count
            })
        }

        if ($icons.Count -eq 0) {
            continue
        }

        $selected = Select-BestManifestIcon -Icons $icons.ToArray() -ThemePreference $ThemePreference -FileTypePreference $FileTypePreference
        $packagesWithIcons.Add([pscustomobject]@{
            PackageId        = $package.PackageId
            PackageVersion   = $package.PackageVersion
            DirectoryPath    = $package.DirectoryPath
            ManifestFiles    = $manifestFiles.ToArray()
            IconCount        = $icons.Count
            SelectedIcon     = $selected
            Icons            = $icons.ToArray()
        })
    }

    return $packagesWithIcons
}

function Get-DownloadFileName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,

        [Parameter(Mandatory = $true)]
        [string]$PackageVersion,

        [Parameter(Mandatory = $true)]
        [string]$IconUrl,

        [string]$IconFileType
    )

    $resolvedExtension = if (-not [string]::IsNullOrWhiteSpace($IconFileType)) {
        '.' + $IconFileType.TrimStart('.').ToLowerInvariant()
    }
    else {
        $uri = [System.Uri]$IconUrl
        $extension = [System.IO.Path]::GetExtension($uri.AbsolutePath)
        if ([string]::IsNullOrWhiteSpace($extension)) {
            '.img'
        }
        else {
            $extension.ToLowerInvariant()
        }
    }

    $safePackageId = ($PackageId -replace '[^A-Za-z0-9._-]', '_')
    $safeVersion = ($PackageVersion -replace '[^A-Za-z0-9._-]', '_')
    return "$safePackageId.$safeVersion$resolvedExtension"
}

function Download-ManifestIcons {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Packages,

        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory,

        [Parameter(Mandatory = $true)]
        [int]$Limit
    )

    New-Item -ItemType Directory -Force -Path $TargetDirectory | Out-Null
    $downloaded = [System.Collections.Generic.List[object]]::new()
    $remaining = $Packages
    if ($Limit -gt 0) {
        $remaining = $Packages | Select-Object -First $Limit
    }

    foreach ($package in $remaining) {
        $selected = $package.SelectedIcon
        if ($null -eq $selected -or [string]::IsNullOrWhiteSpace([string]$selected.IconUrl)) {
            continue
        }

        $fileName = Get-DownloadFileName -PackageId $package.PackageId -PackageVersion $package.PackageVersion -IconUrl ([string]$selected.IconUrl) -IconFileType ([string]$selected.IconFileType)
        $destination = Join-Path $TargetDirectory $fileName
        Invoke-WebRequest -Uri ([string]$selected.IconUrl) -OutFile $destination

        $downloaded.Add([pscustomobject]@{
            PackageId      = $package.PackageId
            PackageVersion = $package.PackageVersion
            IconUrl        = [string]$selected.IconUrl
            DownloadedPath = $destination
        })
    }

    return $downloaded.ToArray()
}

if ($Mode -eq 'Published' -and -not (Test-CommandAvailable -Name 'dotnet')) {
    throw 'dotnet is required to analyze the published WinGet source.'
}

$resolvedRepositoryPath = $null
$failedPackages = @()
if ($Mode -eq 'Git') {
    if (-not (Test-CommandAvailable -Name 'git')) {
        throw 'git is required to clone or update winget-pkgs.'
    }

    if (-not (Test-CommandAvailable -Name 'ConvertFrom-Yaml')) {
        throw 'ConvertFrom-Yaml is required. Install powershell-yaml or import a module that provides it.'
    }

    $resolvedRepositoryPath = Resolve-DefaultRepositoryPath
    Initialize-WingetPkgsRepository -RepoRoot $resolvedRepositoryPath -RepoUrl $RemoteUrl -RepoBranch $Branch -ShouldUpdate (-not $SkipRepositoryUpdate)

    $manifestsRoot = Join-Path $resolvedRepositoryPath 'manifests'
    if (-not (Test-Path $manifestsRoot)) {
        throw "No manifests directory was found at '$manifestsRoot'."
    }

    Write-Host 'Enumerating manifest files'
    $manifestFiles = @(Get-ChildItem -Path $manifestsRoot -Filter '*.yaml' -File -Recurse)
    $latestPackageMap = Get-LatestPackageVersionMap -ManifestFiles $manifestFiles -ManifestsRoot $manifestsRoot -FilterPackageId $PackageId
    $latestPackages = @($latestPackageMap.Values | Sort-Object PackageId)

    Write-Host "Inspecting latest manifests for $($latestPackages.Count) package(s)"
    $packagesWithIcons = @(Get-ManifestIconPackages -LatestPackages $latestPackages -ThemePreference $PreferredTheme -FileTypePreference $PreferredFileType)
    $totalPackages = $latestPackages.Count
    $processedPackages = $latestPackages.Count
}
else {
    $analyzerProjectPath = Resolve-PublishedAnalyzerProjectPath
    Write-Host "Inspecting published WinGet metadata from source '$SourceName'"
    $publishedAnalysis = Invoke-PublishedWingetIconAnalysis -AnalyzerProjectPath $analyzerProjectPath -PublishedSourceName $SourceName -FilterPackageId $PackageId -ChunkSize $PublishedChunkSize -Parallelism $PublishedParallelism

    $packagesWithIcons = @($publishedAnalysis.Packages | ForEach-Object {
        $selected = Select-BestManifestIcon -Icons @($_.Icons) -ThemePreference $PreferredTheme -FileTypePreference $PreferredFileType
        [pscustomobject]@{
            PackageId      = $_.PackageId
            PackageVersion = $_.PackageVersion
            IconCount      = $_.IconCount
            SelectedIcon   = $selected
            ManifestFiles  = @()
            Icons          = @($_.Icons)
        }
    })
    $failedPackages = @($publishedAnalysis.Failures)
    $totalPackages = [int]$publishedAnalysis.TotalPackages
    $processedPackages = [int]$publishedAnalysis.ProcessedPackages
}

$packagesWithoutIcons = $processedPackages - $packagesWithIcons.Count
$iconPercentage = if ($totalPackages -eq 0) { 0 } else { [Math]::Round(($packagesWithIcons.Count / $totalPackages) * 100, 4) }
$processedCoveragePercent = if ($processedPackages -eq 0) { 0 } else { [Math]::Round(($packagesWithIcons.Count / $processedPackages) * 100, 4) }

$iconEntries = @($packagesWithIcons | ForEach-Object { $_.Icons })
$summary = [pscustomobject]@{
    Mode                     = $Mode
    SourceName               = if ($Mode -eq 'Published') { $SourceName } else { 'winget-pkgs' }
    PublishedChunkSize       = if ($Mode -eq 'Published' -and [string]::IsNullOrWhiteSpace($PackageId)) { $PublishedChunkSize } else { $null }
    PublishedParallelism     = if ($Mode -eq 'Published' -and [string]::IsNullOrWhiteSpace($PackageId)) { $PublishedParallelism } else { $null }
    RepositoryPath           = $resolvedRepositoryPath
    Branch                   = if ($Mode -eq 'Git') { $Branch } else { $null }
    TotalPackages            = $totalPackages
    ProcessedPackages        = $processedPackages
    FailedPackages           = $failedPackages.Count
    PackagesWithIcons        = $packagesWithIcons.Count
    PackagesWithoutIcons     = $packagesWithoutIcons
    CoveragePercent          = $iconPercentage
    ProcessedCoveragePercent = $processedCoveragePercent
    TotalIconEntries         = $iconEntries.Count
    FileTypes                = @($iconEntries | Group-Object IconFileType | Sort-Object Count -Descending | ForEach-Object {
        [pscustomobject]@{
            Name  = if ([string]::IsNullOrWhiteSpace([string]$_.Name)) { '<unspecified>' } else { $_.Name }
            Count = $_.Count
        }
    })
    Themes                   = @($iconEntries | Group-Object IconTheme | Sort-Object Count -Descending | ForEach-Object {
        [pscustomobject]@{
            Name  = if ([string]::IsNullOrWhiteSpace([string]$_.Name)) { '<unspecified>' } else { $_.Name }
            Count = $_.Count
        }
    })
    Failures                 = @($failedPackages)
    Packages                 = @($packagesWithIcons | Sort-Object PackageId | ForEach-Object {
        [pscustomobject]@{
            PackageId      = $_.PackageId
            PackageVersion = $_.PackageVersion
            IconCount      = $_.IconCount
            SelectedIcon   = $_.SelectedIcon
            ManifestFiles  = $_.ManifestFiles
            Icons          = $_.Icons
        }
    })
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputDirectory = Split-Path -Parent $OutputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }

    $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding utf8
}

$downloads = @()
if ($DownloadIcons) {
    $targetDirectory = Resolve-DefaultDownloadDirectory -RepoRoot $resolvedRepositoryPath
    $downloads = @(Download-ManifestIcons -Packages $packagesWithIcons -TargetDirectory $targetDirectory -Limit $DownloadLimit)
}

Write-Host ''
Write-Host "Winget icon coverage ($($summary.Mode.ToLowerInvariant()))"
Write-Host "  Total packages (latest only): $($summary.TotalPackages)"
Write-Host "  Processed packages:           $($summary.ProcessedPackages)"
Write-Host "  Failed packages:              $($summary.FailedPackages)"
Write-Host "  Packages with icon info:     $($summary.PackagesWithIcons)"
Write-Host "  Packages without icon info:  $($summary.PackagesWithoutIcons)"
Write-Host "  Coverage:                    $($summary.CoveragePercent)%"
Write-Host "  Coverage (processed only):   $($summary.ProcessedCoveragePercent)%"
Write-Host "  Total icon entries:          $($summary.TotalIconEntries)"
if ($OutputPath) {
    Write-Host "  Report:                      $OutputPath"
}
if ($DownloadIcons) {
    Write-Host "  Downloaded icons:            $($downloads.Count)"
}

[pscustomobject]@{
    Summary   = $summary
    Downloads = $downloads
}