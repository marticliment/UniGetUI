function Get-DefaultChocolateyLocalFilePath
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $OriginalFileName
    )

    # adapted from Install-ChocolateyPackage 0.10.8
    $chocTempDir = $env:TEMP
    $tempDir = Join-Path $chocTempDir "$($env:chocolateyPackageName)"
    if ($env:chocolateyPackageVersion -ne $null) { $tempDir = Join-Path $tempDir "$($env:chocolateyPackageVersion)"; }
    $tempDir = $tempDir -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
    if (![System.IO.Directory]::Exists($tempDir)) { [System.IO.Directory]::CreateDirectory($tempDir) | Out-Null }
    $downloadFilePath = Join-Path $tempDir $OriginalFileName
    Write-Debug "Local file path: $downloadFilePath"
    return $downloadFilePath
}
