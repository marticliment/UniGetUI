# based on Install-ChocolateyPackage (a9519b5), with changes:
# - local file name is extracted from the url (to avoid passing -getOriginalFileName to Get-ChocolateyWebFile for compatibility with old Chocolatey)
# - removed Get-ChocolateyWebFile options support (for compatibility with old Chocolatey)
function Get-VSWebFile
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [string] $DefaultFileName,
        [Parameter(Mandatory = $true)] [string] $FileDescription,
        [string] $Url,
        [alias("Url64")][string] $Url64Bit = '',
        [string] $Checksum = '',
        [string] $ChecksumType = '',
        [string] $Checksum64 = '',
        [string] $ChecksumType64 = '',
        [string] $LocalFilePath,
        [hashtable] $Options
    )

    Write-Debug "Running 'Get-VSWebFile' for $PackageName with Url:'$Url', Url64Bit:'$Url64Bit', Checksum:'$Checksum', ChecksumType:'$ChecksumType', Checksum64:'$Checksum64', ChecksumType64:'$ChecksumType64', LocalFilePath:'$LocalFilePath'";

    if ($LocalFilePath -eq '') {
        $chocTempDir = $env:TEMP
        $tempDir = Join-Path $chocTempDir "$PackageName"
        if ($null -ne $env:packageVersion) { $tempDir = Join-Path $tempDir "$env:packageVersion" }

        if (![System.IO.Directory]::Exists($tempDir)) { [System.IO.Directory]::CreateDirectory($tempDir) | Out-Null }
        $urlForFileNameDetermination = $Url
        if ($urlForFileNameDetermination -eq '') { $urlForFileNameDetermination = $Url64Bit }
        if ($urlForFileNameDetermination -match '\.((exe)|(vsix))$') { $localFileName = $urlForFileNameDetermination.Substring($urlForFileNameDetermination.LastIndexOfAny(@('/', '\')) + 1) }
        else { $localFileName = $DefaultFileName }
        $LocalFilePath = Join-Path $tempDir $localFileName

        Write-Verbose "Downloading the $FileDescription"
        $arguments = @{
            PackageName = $PackageName
            FileFullPath = $LocalFilePath
            Url = $Url
            Url64Bit = $Url64Bit
            Checksum = $Checksum
            ChecksumType = $ChecksumType
            Checksum64 = $Checksum64
            ChecksumType64 = $ChecksumType64
        }

        $gcwf = Get-Command -Name Get-ChocolateyWebFile
        if ($gcwf.Parameters.ContainsKey('Options'))
        {
            $arguments.Options = $Options
        }
        else
        {
            if ($null -ne $Options -and $Options.Keys.Count -gt 0)
            {
                Write-Warning "This Chocolatey version does not support passing custom Options to Get-ChocolateyWebFile."
            }
        }

        Set-StrictMode -Off
        Get-ChocolateyWebFile @arguments | Out-Null
        Set-StrictMode -Version 2
    } else {
        if (-not (Test-Path -Path $LocalFilePath)) {
            throw "The local $FileDescription does not exist: $LocalFilePath"
        }
        Write-Verbose "Using a local ${FileDescription}: $LocalFilePath"
    }

    return $LocalFilePath
}
