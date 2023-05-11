# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Get-ChocolateyWebFile {
<#
.SYNOPSIS
Downloads a file from the internet.

.DESCRIPTION
This will download a file from a url, tracking with a progress bar.
It returns the filepath to the downloaded file when it is complete.

.NOTES
Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the community feed), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER FileFullPath
This is the full path of the resulting file name.

.PARAMETER Url
This is the 32 bit url to download the resource from. This resource can
be used on 64 bit systems when a package has both a Url and Url64bit
specified if a user passes `--forceX86`. If there is only a 64 bit url
available, please remove do not use the parameter (only use Url64bit).
Will fail on 32bit systems if missing or if a user attempts to force
a 32 bit installation on a 64 bit system.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER Url64bit
OPTIONAL - If there is a 64 bit resource available, use this
parameter. Chocolatey will automatically determine if the user is
running a 64 bit OS or not and adjust accordingly. Please note that
the 32 bit url will be used in the absence of this. This parameter
should only be used for 64 bit native software. If the original Url
contains both (which is quite rare), set this to '$url' Otherwise
remove this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

In 0.10.1+, `Url64` is an alias for Url64bit.

.PARAMETER Checksum
The checksum hash value of the Url resource. This allows a checksum to
be validated for files that are not local. The checksum type is covered
by ChecksumType.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the community repository, it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://docs.chocolatey.org/en-us/create/automatic-packages)
to help provide that functionality.

.PARAMETER ChecksumType
The type of checksum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Checksum64
OPTIONAL if no Url64bit - The checksum hash value of the Url64bit
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType64.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the community repository, it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://docs.chocolatey.org/en-us/create/automatic-packages)
to help provide that functionality.

**NOTE:** To determine checksums, you can get that from the original
site if provided. You can also use the [checksum tool available on
the community feed](https://community.chocolatey.org/packages/checksum) (`choco install checksum`)
and use it e.g. `checksum -t sha256 -f path\to\file`. Ensure you
provide checksums for all remote resources used.

.PARAMETER ChecksumType64
OPTIONAL - The type of checksum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to
ChecksumType parameter value.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Options
OPTIONAL - Specify custom headers. Available in 0.9.10+.

.PARAMETER GetOriginalFileName
OPTIONAL switch to allow Chocolatey to determine the original file name
from the url resource. Available in 0.9.10+.

.PARAMETER ForceDownload
OPTIONAL switch to force download of file every time, even if the file
already exists.

Available in 0.10.1+.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Get-ChocolateyWebFile '__NAME__' 'C:\somepath\somename.exe' 'URL' '64BIT_URL_DELETE_IF_NO_64BIT'

.EXAMPLE
>
# Download from an HTTPS location
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'https://somewhere/bob.exe'

.EXAMPLE
>
# Download from FTP
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'ftp://somewhere/bob.exe'

.EXAMPLE
>
# Download from a file share
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'file:///\\fileshare\location\bob.exe'

.EXAMPLE
>
$options =
@{
  Headers = @{
    Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8';
    'Accept-Charset' = 'ISO-8859-1,utf-8;q=0.7,*;q=0.3';
    'Accept-Language' = 'en-GB,en-US;q=0.8,en;q=0.6';
    Cookie = 'requiredinfo=info';
    Referer = 'https://somelocation.com/';
  }
}

Get-ChocolateyWebFile -PackageName 'package' -FileFullPath "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\thefile.exe" -Url 'https://somelocation.com/thefile.exe' -Options $options

.LINK
Install-ChocolateyPackage

.LINK
Get-WebFile

.LINK
Get-WebFileName

.LINK
Get-FtpFile
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$true, Position=1)][string] $fileFullPath,
  [parameter(Mandatory=$false, Position=2)][string] $url = '',
  [parameter(Mandatory=$false, Position=3)]
  [alias("url64")][string] $url64bit = '',
  [parameter(Mandatory=$false)][string] $checksum = '',
  [parameter(Mandatory=$false)][string] $checksumType = '',
  [parameter(Mandatory=$false)][string] $checksum64 = '',
  [parameter(Mandatory=$false)][string] $checksumType64 = $checksumType,
  [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
  [parameter(Mandatory=$false)][switch] $getOriginalFileName,
  [parameter(Mandatory=$false)][switch] $forceDownload,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  # url overrides
  $urlOverride = $env:ChocolateyUrlOverride
  $url64bitOverride = $env:ChocolateyUrl64bitOverride
  if ($urlOverride -ne $null -and $urlOverride -ne '') { $url = $urlOverride }
  if ($url64bitOverride -ne $null -and $url64bitOverride -ne '') { $url64bit = $url64bitOverride }

  if ($url -ne $null) { $url = $url.Replace("//","/").Replace(":/","://") }
  if ($url64bit -ne $null) { $url64bit = $url64bit.Replace("//","/").Replace(":/","://") }

  $url32bit = $url

  # allow user provided values for checksumming
  $checksum32Override = $env:chocolateyChecksum32
  $checksumType32Override = $env:chocolateyChecksumType32
  $checksum64Override = $env:chocolateyChecksum64
  $checksumType64Override = $env:chocolateyChecksumType64
  if ($checksum32Override -ne $null -and $checksum32Override -ne '') { $checksum = $checksum32Override }
  if ($checksumType32Override -ne $null -and $checksumType32Override -ne '') { $checksumType = $checksumType32Override }
  if ($checksum64Override -ne $null -and $checksum64Override -ne '') { $checksum64 = $checksum64Override }
  if ($checksumType64Override -ne $null -and $checksumType64Override -ne '') { $checksumType64 = $checksumType64Override }

  $checksum32 = $checksum
  $checksumType32 = $checksumType
  $bitWidth = 32
  if (Get-OSArchitectureWidth 64) {
    $bitWidth = 64
  }
  Write-Debug "CPU is $bitWidth bit"

  $bitPackage = ''
  if ($url32bit -ne $url64bit -and $url64bit -ne $null -and $url64bit -ne '') { $bitPackage = '32 bit' }

  if ($bitWidth -eq 64 -and $url64bit -ne $null -and $url64bit -ne '') {
    Write-Debug "Setting url to '$url64bit' and bitPackage to $bitWidth"
    $bitPackage = '64 bit'
    $url = $url64bit
    # only set if urls are different
    if ($url32bit -ne $url64bit) {
      $checksum = $checksum64
      if ($checkSumType64 -ne '') {
        $checksumType = $checksumType64
      }
    }
  }

  $forceX86 = $env:chocolateyForceX86;
  if ($forceX86) {
    Write-Debug "User specified -x86 so forcing 32 bit"
    if ($url32bit -ne $url64bit) { $bitPackage = '32 bit' }
    $url = $url32bit
    $checksum =  $checksum32
    $checksumType = $checksumType32
  }

  # If we're on 32 bit or attempting to force 32 bit and there is no
  # 32 bit url, we need to throw an error.
  if ($url -eq $null -or $url -eq '') {
    throw "This package does not support $bitPackage architecture."
  }

  # determine if the url can be SSL/TLS
  if ($url.StartsWith('http:')) {
    try {
      $httpsUrl = $url.Replace("http://", "https://")
      Get-WebHeaders -Url $httpsUrl -ErrorAction "Stop" | Out-Null
      $url = $httpsUrl
      Write-Warning "Url has SSL/TLS available, switching to HTTPS for download"
    } catch {
      Write-Debug "Url does not have HTTPS available"
    }
  }

  if ($getOriginalFileName) {
    try {
      $fileFullPath = $fileFullPath -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
      $fileDirectory = [System.IO.Path]::GetDirectoryName($fileFullPath)
      $originalFileName = [System.IO.Path]::GetFileName($fileFullPath)
      $fileFullPath = Get-WebFileName -Url $url -DefaultName $originalFileName
      $fileFullPath = Join-Path $fileDirectory $fileFullPath
      $fileFullPath = [System.IO.Path]::GetFullPath($fileFullPath)
    } catch {
      Write-Host "Attempt to use original download file name failed for '$url'."
    }
  }

  try {
    $fileDirectory = $([System.IO.Path]::GetDirectoryName($fileFullPath))
    if (!(Test-Path($fileDirectory))) {
      [System.IO.Directory]::CreateDirectory($fileDirectory) | Out-Null
    }
  } catch {
    Write-Host "Attempt to create directory failed for '$fileFullPath'."
  }

  $urlIsRemote = $true
  $headers = @{}
  if ($url.StartsWith('http')) {
    try {
      $headers = Get-WebHeaders -Url $url -ErrorAction "Stop"
    } catch {
      if ($PSVersionTable.PSVersion -lt (New-Object 'Version' 3,0)) {
        Write-Debug "Converting Security Protocol to SSL3 only for PowerShell v2"
        # this should last for the entire duration
        $originalProtocol = [System.Net.ServicePointManager]::SecurityProtocol
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Ssl3
        try {
          $headers = Get-WebHeaders -Url $url -ErrorAction "Stop"
        } catch {
          Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
          [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol
        }
      } else {
        Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
      }
    }

    $needsDownload = $true
    $fiCached = New-Object System.IO.FileInfo($fileFullPath)
    if ($fiCached.Exists -and -not ($forceDownload)) {
      if ($checksum -ne $null -and $checksum -ne '') {
          try {
            Write-Host "File appears to be downloaded already. Verifying with package checksum to determine if it needs to be redownloaded."
            Get-ChecksumValid -file $fileFullPath -checkSum $checksum -checksumType $checksumType -originalUrl $url -ErrorAction "Stop"
            $needsDownload = $false
          } catch {
            Write-Debug "Existing file failed checksum. Will be redownloaded from url."
          }
      }
      elseif ($headers.Count -ne 0 -and $headers.ContainsKey("Content-Length")) {
        # if the file already exists there is no reason to download it again.
        if ($fiCached.Length -eq $headers["Content-Length"]) { $needsDownload = $false }
      }
    }

    if ($needsDownload) {
      Write-Host "Downloading $packageName $bitPackage
  from `'$url`'"
      Get-WebFile -Url $url -FileName $fileFullPath -Options $options
    } else {
      Write-Debug "$($packageName)'s requested file has already been downloaded. Using cached copy at
 '$fileFullPath'."
    }
  } elseif ($url.StartsWith('ftp')) {
    Write-Host "Ftp-ing $packageName
  from '$url'"
    Get-FtpFile -Url $url -FileName $fileFullPath
  } else {
    if ($url.StartsWith('file:')) { $url = ([uri] $url).LocalPath }
    Write-Host "Copying $packageName
  from `'$url`'"
    Copy-Item $url -Destination $fileFullPath -Force
    $urlIsRemote = $false
  }

  Start-Sleep 2 #give it a sec or two to finish up copying

  $fi = New-Object System.IO.FileInfo($fileFullPath)
  # validate file exists
  if (!($fi.Exists)) { throw "Chocolatey expected a file to be downloaded to `'$fileFullPath`' but nothing exists at that location." }

  Get-VirusCheckValid -Location $url -File $fileFullPath

  if ($headers.Count -ne 0 -and ($checksum -eq $null -or $checksum -eq '')) {
    # validate length is what we expected
    Write-Debug "Checking that '$fileFullPath' is the size we expect it to be."
    if ($headers.ContainsKey("Content-Length") -and ($fi.Length -ne $headers["Content-Length"]))  { throw "Chocolatey expected a file at '$fileFullPath' to be of length '$($headers["Content-Length"])' but the length was '$($fi.Length)'." }

    if ($headers.ContainsKey("X-Checksum-Sha1")) {
      $remoteChecksum = $headers["X-Checksum-Sha1"]
      Write-Debug "Verifying remote checksum of '$remoteChecksum' for '$fileFullPath'."
      Get-ChecksumValid -File $fileFullPath -Checksum $remoteChecksum -ChecksumType 'sha1' -OriginalUrl $url
    }
  }

  #skip requirement for embedded files if checksum is not provided
  if ($urlIsRemote -or ($checksum -ne $null -and $checksum -ne '')) {
    Write-Debug "Verifying package provided checksum of '$checksum' for '$fileFullPath'."
    Get-ChecksumValid -File $fileFullPath -Checksum $checksum -ChecksumType $checksumType -OriginalUrl $url
  }

  return $fileFullPath
}

# SIG # Begin signature block
# MIIjfwYJKoZIhvcNAQcCoIIjcDCCI2wCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAlsns28SIsOL8z
# oK/rTJjrSFpT9mNzTj2A1a7+cUn+lqCCHXgwggUwMIIEGKADAgECAhAECRgbX9W7
# ZnVTQ7VvlVAIMA0GCSqGSIb3DQEBCwUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0xMzEwMjIxMjAwMDBa
# Fw0yODEwMjIxMjAwMDBaMHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lD
# ZXJ0IFNIQTIgQXNzdXJlZCBJRCBDb2RlIFNpZ25pbmcgQ0EwggEiMA0GCSqGSIb3
# DQEBAQUAA4IBDwAwggEKAoIBAQD407Mcfw4Rr2d3B9MLMUkZz9D7RZmxOttE9X/l
# qJ3bMtdx6nadBS63j/qSQ8Cl+YnUNxnXtqrwnIal2CWsDnkoOn7p0WfTxvspJ8fT
# eyOU5JEjlpB3gvmhhCNmElQzUHSxKCa7JGnCwlLyFGeKiUXULaGj6YgsIJWuHEqH
# CN8M9eJNYBi+qsSyrnAxZjNxPqxwoqvOf+l8y5Kh5TsxHM/q8grkV7tKtel05iv+
# bMt+dDk2DZDv5LVOpKnqagqrhPOsZ061xPeM0SAlI+sIZD5SlsHyDxL0xY4PwaLo
# LFH3c7y9hbFig3NBggfkOItqcyDQD2RzPJ6fpjOp/RnfJZPRAgMBAAGjggHNMIIB
# yTASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAK
# BggrBgEFBQcDAzB5BggrBgEFBQcBAQRtMGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9v
# Y3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NhY2VydHMuZGln
# aWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNydDCBgQYDVR0fBHow
# eDA6oDigNoY0aHR0cDovL2NybDQuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJl
# ZElEUm9vdENBLmNybDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0Rp
# Z2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDBPBgNVHSAESDBGMDgGCmCGSAGG/WwA
# AgQwKjAoBggrBgEFBQcCARYcaHR0cHM6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAK
# BghghkgBhv1sAzAdBgNVHQ4EFgQUWsS5eyoKo6XqcQPAYPkt9mV1DlgwHwYDVR0j
# BBgwFoAUReuir/SSy4IxLVGLp6chnfNtyA8wDQYJKoZIhvcNAQELBQADggEBAD7s
# DVoks/Mi0RXILHwlKXaoHV0cLToaxO8wYdd+C2D9wz0PxK+L/e8q3yBVN7Dh9tGS
# dQ9RtG6ljlriXiSBThCk7j9xjmMOE0ut119EefM2FAaK95xGTlz/kLEbBw6RFfu6
# r7VRwo0kriTGxycqoSkoGjpxKAI8LpGjwCUR4pwUR6F6aGivm6dcIFzZcbEMj7uo
# +MUSaJ/PQMtARKUT8OZkDCUIQjKyNookAv4vcn4c10lFluhZHen6dGRrsutmQ9qz
# sIzV6Q3d9gEgzpkxYz0IGhizgZtPxpMQBvwHgfqL2vmCSfdibqFT+hKUGIUukpHq
# aGxEMrJmoecYpJpkUe8wggU5MIIEIaADAgECAhAKudMQ+yEr6IyBs9LC6M5RMA0G
# CSqGSIb3DQEBCwUAMHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJ
# bmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lDZXJ0
# IFNIQTIgQXNzdXJlZCBJRCBDb2RlIFNpZ25pbmcgQ0EwHhcNMjEwNDI3MDAwMDAw
# WhcNMjQwNDMwMjM1OTU5WjB3MQswCQYDVQQGEwJVUzEPMA0GA1UECBMGS2Fuc2Fz
# MQ8wDQYDVQQHEwZUb3Bla2ExIjAgBgNVBAoTGUNob2NvbGF0ZXkgU29mdHdhcmUs
# IEluYy4xIjAgBgNVBAMTGUNob2NvbGF0ZXkgU29mdHdhcmUsIEluYy4wggEiMA0G
# CSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQChcaeNqeO3O3hzbDYYMcxvv/QNSPE4
# fpI+NGECR+FYdDO2utX9/SPxRCzWBrsgntPs/7IPk/uFZk/yTIiNoXO+cqJE45L9
# 2Ldfn6gAcwjGna/j2f/bbSFSeXW9z9lM3DJecFwXQleWR/8OKCnD+d1ZmHB0BA5v
# 0bQCfU8ZT7S0u9+KAKqyqgZrJyQiPfBVqXes9RSua7+0SVXmaBrJf9njHAf5KNFY
# /TEgm1r1zYwxfcsuE5eYdr2/suytUJpN18m9DmAdYm72va0KMxoKIBGuQy9DnaDI
# +nMiegsdhkL9sIysIin7Pcwjkwx9lRmtIqJA27Hfgb1MaL0OnkpwRY+VAgMBAAGj
# ggHEMIIBwDAfBgNVHSMEGDAWgBRaxLl7KgqjpepxA8Bg+S32ZXUOWDAdBgNVHQ4E
# FgQUTvMFGF2V6ylQalFt+afRXjSaBIMwDgYDVR0PAQH/BAQDAgeAMBMGA1UdJQQM
# MAoGCCsGAQUFBwMDMHcGA1UdHwRwMG4wNaAzoDGGL2h0dHA6Ly9jcmwzLmRpZ2lj
# ZXJ0LmNvbS9zaGEyLWFzc3VyZWQtY3MtZzEuY3JsMDWgM6Axhi9odHRwOi8vY3Js
# NC5kaWdpY2VydC5jb20vc2hhMi1hc3N1cmVkLWNzLWcxLmNybDBLBgNVHSAERDBC
# MDYGCWCGSAGG/WwDATApMCcGCCsGAQUFBwIBFhtodHRwOi8vd3d3LmRpZ2ljZXJ0
# LmNvbS9DUFMwCAYGZ4EMAQQBMIGEBggrBgEFBQcBAQR4MHYwJAYIKwYBBQUHMAGG
# GGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBOBggrBgEFBQcwAoZCaHR0cDovL2Nh
# Y2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0U0hBMkFzc3VyZWRJRENvZGVTaWdu
# aW5nQ0EuY3J0MAwGA1UdEwEB/wQCMAAwDQYJKoZIhvcNAQELBQADggEBAKFxncHA
# zDFesUJXaM21qMRk5+nIZcDuISfGgJcDjMHsRLw7na5Yn7IhiNY+OsKnPVkfPhL/
# MNXSHG6on+IpxiB2/Bry9thqKvpQdPBe8mFN0ctJDgrSceyRC5SA9EiO22J3YNe0
# yVEKAG+Yk2A/WhKBzCCpRskMlRr7KeLm6DvAgvDsMfkKtePMl2PraON+tFNpc2b1
# LTKT4okiU5uAWpjYAt9sYBsKTeZb5NJt0ZQ3akEEIAQs63/mSDAZlzMOJMWNK/yv
# 4NU5CiPVcohJ0WjUJUIrAMmAVlZ2h8NhCXJOv28cHWEgPks/zqdDdIhJfDF+ALd1
# 0JTBrwCNcYQG68AwggWNMIIEdaADAgECAhAOmxiO+dAt5+/bUOIIQBhaMA0GCSqG
# SIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNVBAMTG0RpZ2lDZXJ0IEFz
# c3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBaFw0zMTExMDkyMzU5NTla
# MGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsT
# EHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9v
# dCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAL/mkHNo3rvkXUo8
# MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3EMB/zG6Q4FutWxpdtHauy
# efLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKyunWZanMylNEQRBAu34Lz
# B4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsFxl7sWxq868nPzaw0QF+x
# embud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU15zHL2pNe3I6PgNq2kZhA
# kHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJBMtfbBHMqbpEBfCFM1Lyu
# GwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObURWBf3JFxGj2T3wWmIdph2
# PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6nj3cAORFJYm2mkQZK37A
# lLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxBYKqxYxhElRp2Yn72gLD7
# 6GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5SUUd0viastkF13nqsX40/
# ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+xq4aLT8LWRV+dIPyhHsXA
# j6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIBNjAPBgNVHRMBAf8EBTAD
# AQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwPTzAfBgNVHSMEGDAWgBRF
# 66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMCAYYweQYIKwYBBQUHAQEE
# bTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wQwYIKwYB
# BQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3Vy
# ZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0aHR0cDovL2NybDMuZGln
# aWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDARBgNVHSAECjAI
# MAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0NcVec4X6CjdBs9thbX979X
# B72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnovLbc47/T/gLn4offyct4k
# vFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65ZyoUi0mcudT6cGAxN3J0TU
# 53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFWjuyk1T3osdz9HNj0d1pc
# VIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPFmCLBsln1VWvPJ6tsds5v
# Iy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9ztwGpn1eqXijiuZQwggau
# MIIElqADAgECAhAHNje3JFR82Ees/ShmKl5bMA0GCSqGSIb3DQEBCwUAMGIxCzAJ
# BgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5k
# aWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9vdCBHNDAe
# Fw0yMjAzMjMwMDAwMDBaFw0zNzAzMjIyMzU5NTlaMGMxCzAJBgNVBAYTAlVTMRcw
# FQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3Rl
# ZCBHNCBSU0E0MDk2IFNIQTI1NiBUaW1lU3RhbXBpbmcgQ0EwggIiMA0GCSqGSIb3
# DQEBAQUAA4ICDwAwggIKAoICAQDGhjUGSbPBPXJJUVXHJQPE8pE3qZdRodbSg9Ge
# TKJtoLDMg/la9hGhRBVCX6SI82j6ffOciQt/nR+eDzMfUBMLJnOWbfhXqAJ9/UO0
# hNoR8XOxs+4rgISKIhjf69o9xBd/qxkrPkLcZ47qUT3w1lbU5ygt69OxtXXnHwZl
# jZQp09nsad/ZkIdGAHvbREGJ3HxqV3rwN3mfXazL6IRktFLydkf3YYMZ3V+0VAsh
# aG43IbtArF+y3kp9zvU5EmfvDqVjbOSmxR3NNg1c1eYbqMFkdECnwHLFuk4fsbVY
# TXn+149zk6wsOeKlSNbwsDETqVcplicu9Yemj052FVUmcJgmf6AaRyBD40NjgHt1
# biclkJg6OBGz9vae5jtb7IHeIhTZgirHkr+g3uM+onP65x9abJTyUpURK1h0QCir
# c0PO30qhHGs4xSnzyqqWc0Jon7ZGs506o9UD4L/wojzKQtwYSH8UNM/STKvvmz3+
# DrhkKvp1KCRB7UK/BZxmSVJQ9FHzNklNiyDSLFc1eSuo80VgvCONWPfcYd6T/jnA
# +bIwpUzX6ZhKWD7TA4j+s4/TXkt2ElGTyYwMO1uKIqjBJgj5FBASA31fI7tk42Pg
# puE+9sJ0sj8eCXbsq11GdeJgo1gJASgADoRU7s7pXcheMBK9Rp6103a50g5rmQzS
# M7TNsQIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQU
# uhbZbU2FL3MpdpovdYxqII+eyG8wHwYDVR0jBBgwFoAU7NfjgtJxXWRM3y5nP+e6
# mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoGCCsGAQUFBwMIMHcGCCsG
# AQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29t
# MEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNl
# cnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDigNqA0hjJodHRwOi8vY3Js
# My5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNybDAgBgNVHSAE
# GTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZIhvcNAQELBQADggIBAH1Z
# jsCTtm+YqUQiAX5m1tghQuGwGC4QTRPPMFPOvxj7x1Bd4ksp+3CKDaopafxpwc8d
# B+k+YMjYC+VcW9dth/qEICU0MWfNthKWb8RQTGIdDAiCqBa9qVbPFXONASIlzpVp
# P0d3+3J0FNf/q0+KLHqrhc1DX+1gtqpPkWaeLJ7giqzl/Yy8ZCaHbJK9nXzQcAp8
# 76i8dU+6WvepELJd6f8oVInw1YpxdmXazPByoyP6wCeCRK6ZJxurJB4mwbfeKuv2
# nrF5mYGjVoarCkXJ38SNoOeY+/umnXKvxMfBwWpx2cYTgAnEtp/Nh4cku0+jSbl3
# ZpHxcpzpSwJSpzd+k1OsOx0ISQ+UzTl63f8lY5knLD0/a6fxZsNBzU+2QJshIUDQ
# txMkzdwdeDrknq3lNHGS1yZr5Dhzq6YBT70/O3itTK37xJV77QpfMzmHQXh6OOmc
# 4d0j/R0o08f56PGYX/sr2H7yRp11LB4nLCbbbxV7HhmLNriT1ObyF5lZynDwN7+Y
# AN8gFk8n+2BnFqFmut1VwDophrCYoCvtlUG3OtUVmDG0YgkPCr2B2RP+v6TR81fZ
# vAT6gt4y3wSJ8ADNXcL50CN/AAvkdgIm2fBldkKmKYcJRyvmfxqkhQ/8mJb2VVQr
# H4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIGwDCCBKigAwIBAgIQDE1p
# ckuU+jwqSj0pB4A9WjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEXMBUG
# A1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0ZWQg
# RzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIyMDkyMTAwMDAw
# MFoXDTMzMTEyMTIzNTk1OVowRjELMAkGA1UEBhMCVVMxETAPBgNVBAoTCERpZ2lD
# ZXJ0MSQwIgYDVQQDExtEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMiAtIDIwggIiMA0G
# CSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDP7KUmOsap8mu7jcENmtuh6BSFdDMa
# JqzQHFUeHjZtvJJVDGH0nQl3PRWWCC9rZKT9BoMW15GSOBwxApb7crGXOlWvM+xh
# iummKNuQY1y9iVPgOi2Mh0KuJqTku3h4uXoW4VbGwLpkU7sqFudQSLuIaQyIxvG+
# 4C99O7HKU41Agx7ny3JJKB5MgB6FVueF7fJhvKo6B332q27lZt3iXPUv7Y3UTZWE
# aOOAy2p50dIQkUYp6z4m8rSMzUy5Zsi7qlA4DeWMlF0ZWr/1e0BubxaompyVR4aF
# eT4MXmaMGgokvpyq0py2909ueMQoP6McD1AGN7oI2TWmtR7aeFgdOej4TJEQln5N
# 4d3CraV++C0bH+wrRhijGfY59/XBT3EuiQMRoku7mL/6T+R7Nu8GRORV/zbq5Xwx
# 5/PCUsTmFntafqUlc9vAapkhLWPlWfVNL5AfJ7fSqxTlOGaHUQhr+1NDOdBk+lbP
# 4PQK5hRtZHi7mP2Uw3Mh8y/CLiDXgazT8QfU4b3ZXUtuMZQpi+ZBpGWUwFjl5S4p
# kKa3YWT62SBsGFFguqaBDwklU/G/O+mrBw5qBzliGcnWhX8T2Y15z2LF7OF7ucxn
# EweawXjtxojIsG4yeccLWYONxu71LHx7jstkifGxxLjnU15fVdJ9GSlZA076XepF
# cxyEftfO4tQ6dwIDAQABo4IBizCCAYcwDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB
# /wQCMAAwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwgwIAYDVR0gBBkwFzAIBgZngQwB
# BAIwCwYJYIZIAYb9bAcBMB8GA1UdIwQYMBaAFLoW2W1NhS9zKXaaL3WMaiCPnshv
# MB0GA1UdDgQWBBRiit7QYfyPMRTtlwvNPSqUFN9SnDBaBgNVHR8EUzBRME+gTaBL
# hklodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRSU0E0
# MDk2U0hBMjU2VGltZVN0YW1waW5nQ0EuY3JsMIGQBggrBgEFBQcBAQSBgzCBgDAk
# BggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMFgGCCsGAQUFBzAC
# hkxodHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRS
# U0E0MDk2U0hBMjU2VGltZVN0YW1waW5nQ0EuY3J0MA0GCSqGSIb3DQEBCwUAA4IC
# AQBVqioa80bzeFc3MPx140/WhSPx/PmVOZsl5vdyipjDd9Rk/BX7NsJJUSx4iGNV
# CUY5APxp1MqbKfujP8DJAJsTHbCYidx48s18hc1Tna9i4mFmoxQqRYdKmEIrUPwb
# tZ4IMAn65C3XCYl5+QnmiM59G7hqopvBU2AJ6KO4ndetHxy47JhB8PYOgPvk/9+d
# EKfrALpfSo8aOlK06r8JSRU1NlmaD1TSsht/fl4JrXZUinRtytIFZyt26/+YsiaV
# OBmIRBTlClmia+ciPkQh0j8cwJvtfEiy2JIMkU88ZpSvXQJT657inuTTH4YBZJwA
# wuladHUNPeF5iL8cAZfJGSOA1zZaX5YWsWMMxkZAO85dNdRZPkOaGK7DycvD+5sT
# X2q1x+DzBcNZ3ydiK95ByVO5/zQQZ/YmMph7/lxClIGUgp2sCovGSxVK05iQRWAz
# gOAj3vgDpPZFR+XOuANCR+hBNnF3rf2i6Jd0Ti7aHh2MWsgemtXC8MYiqE+bvdgc
# mlHEL5r2X6cnl7qWLoVXwGDneFZ/au/ClZpLEQLIgpzJGgV8unG1TnqZbPTontRa
# mMifv427GFxD9dAq6OJi7ngE273R+1sKqHB+8JeEeOMIA11HLGOoJTiXAdI/Otrl
# 5fbmm9x+LMz/F0xNAKLY1gEOuIvu5uByVYksJxlh9ncBjDGCBV0wggVZAgEBMIGG
# MHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsT
# EHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lDZXJ0IFNIQTIgQXNzdXJl
# ZCBJRCBDb2RlIFNpZ25pbmcgQ0ECEAq50xD7ISvojIGz0sLozlEwDQYJYIZIAWUD
# BAIBBQCggYQwGAYKKwYBBAGCNwIBDDEKMAigAoAAoQKAADAZBgkqhkiG9w0BCQMx
# DAYKKwYBBAGCNwIBBDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkq
# hkiG9w0BCQQxIgQgjpPCKwO54hfZ3boAazk+8N9EBVXc71MoMfgF3D4RWGcwDQYJ
# KoZIhvcNAQEBBQAEggEAQTxszcJtsyswkmBjUv2OaME4wrg9PeteVy/FxFd+OfaW
# GLH2kH7n3shxGGsuwoDog1CRkCILJFqQScDsPVF1ECjN54JGwQgsqRh7s5+cHqAq
# fyW+ExEsc6gjA+WkZq4D3FrGlIn/4D7ADW+vdTCsE8HQgQtLDda+Z2ctEOkJbTjX
# BPTjjwr2zp7rf4se0/UNgV2/RPiUJfeiLag2gVZyVclYpFMIGWPQe7k0YCdYsqWb
# 7mwaMP3EbgotK8EoFfVk/IZDLAOz+OUoe6yCUX19gBMm5suwSnqEPAZJg+NVMW15
# fBCH4K12Le0PV1eA9o90C6PEOengzQMKmGA4XVue1KGCAyAwggMcBgkqhkiG9w0B
# CQYxggMNMIIDCQIBATB3MGMxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2Vy
# dCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNI
# QTI1NiBUaW1lU3RhbXBpbmcgQ0ECEAxNaXJLlPo8Kko9KQeAPVowDQYJYIZIAWUD
# BAIBBQCgaTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEP
# Fw0yMzA1MTAxMDUzMTlaMC8GCSqGSIb3DQEJBDEiBCB7Bz4nV0egPDPxREmvsfd5
# qESlzKwzNLHiiWwE8j2QvDANBgkqhkiG9w0BAQEFAASCAgBFvpOiRNO+7pjXqhV9
# ovjnXgwPeibEQOeBVQD9m/V9LlbpwGUh/sHtcpK6nxu5PEuFqGjNJc9A1P8VzsPJ
# 3KDs3XaPtdhwbIo/Vs/17g86uGH+Dkp1+tjbSLc/j3lKxtUXz5BEo1vSKzRqDrAa
# iMZ5NWQId685hp1MMMnVgaGuOBeY2lkSWsaPkVC4FkW3EEpbjMUymbr2db1uswCz
# SDbCTnuj1BCmnrCP5j2aRBsrK09ieBKDqvoZYhK77ZWd5Rd5BdKhgP+rIVDTeDmp
# UUXdG2UB6cvJEOsZUNjRGwHe7KNbBYs+UjzspH+kCCqgBCzDuiqyuqDZfhdYvdTD
# zsls+Cyt2Bb2eMyn54ZABY4x/Ed/7B9NXsoVC/6dQJXCKvzVXF7Q0siu44oTdO3e
# mIHa0BktGRKf3ZFrQjpPE3TwgZSLmzmEBq9OrxNJWoaXGSQQIT2TBfrxnJdhF3kO
# xvMPbOCd0ORdg/GDpqUaaUeJGI+8obQ/eIVvKpskVSLZIkt/jM5tBIsi5BvlWoOz
# nkepxKqFg/ZJ8MuHlN2Q9AntwZ0PYsT4YZq9iD85jLxT+1suVZ2rtJm1rEQndKC4
# fmM5W/HtfcwtVwwgWIb9cSpS0+QIDA79X21fhLQZhXjLdGYSM7GjGt3gZiQmL3Od
# 8MqbFb8eXSnK5iJgwE6WSc1PWQ==
# SIG # End signature block
