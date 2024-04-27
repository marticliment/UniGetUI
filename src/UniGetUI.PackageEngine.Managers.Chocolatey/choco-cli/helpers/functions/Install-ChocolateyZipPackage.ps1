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

function Install-ChocolateyZipPackage {
    <#
.SYNOPSIS
Downloads file from a url and unzips it on your machine. Use
Get-ChocolateyUnzip when local or embedded file.

.DESCRIPTION
This will download a file from a url and unzip it on your machine.
If you are embedding the file(s) directly in the package (or do not need
to download a file first), use Get-ChocolateyUnzip instead.

.NOTES
Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the community feed), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER Url
This is the 32 bit url to download the resource from. This resource can
be used on 64 bit systems when a package has both a Url and Url64bit
specified if a user passes `--forceX86`. If there is only a 64 bit url
available, please remove do not use the parameter (only use Url64bit).
Will fail on 32bit systems if missing or if a user attempts to force
a 32 bit installation on a 64 bit system.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER SpecificFolder
OPTIONAL - This is a specific directory within zip file to extract. The
folder and its contents will be extracted to the destination.

.PARAMETER Url64bit
OPTIONAL - If there is a 64 bit resource available, use this
parameter. Chocolatey will automatically determine if the user is
running a 64 bit OS or not and adjust accordingly. Please note that
the 32 bit url will be used in the absence of this. This parameter
should only be used for 64 bit native software. If the original Url
contains both (which is quite rare), set this to '$url' Otherwise remove
this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER UnzipLocation
This is the full path to a location to unzip the contents to, most
likely your script folder. If unzipping to your package folder, the path
will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.exe"`

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

**NOTE:** To determine checksums, you can get that from the original
site if provided. You can also use the [checksum tool available on
the community feed](https://community.chocolatey.org/packages/checksum) (`choco install checksum`)
and use it e.g. `checksum -t sha256 -f path\to\file`. Ensure you
provide checksums for all remote resources used.

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

.PARAMETER ChecksumType64
OPTIONAL - The type of checksum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to
ChecksumType parameter value.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Options
OPTIONAL - Specify custom headers.

.PARAMETER File
Will be used for Url if Url is empty.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

.PARAMETER File64
Will be used for Url64bit if Url64bit is empty.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

.PARAMETER DisableLogging
OPTIONAL - This disables Logger.Logging of the extracted items. It speeds up
extraction of archives with many files.

Usage of this parameter will prevent Uninstall-ChocolateyZipPackage
from working, extracted files will have to be cleaned up with
Remove-Item or a similar command instead.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Install-ChocolateyZipPackage -PackageName 'gittfs' -Url 'https://github.com/downloads/spraints/git-tfs/GitTfs-0.11.0.zip' -UnzipLocation $gittfsPath

.EXAMPLE
>
Install-ChocolateyZipPackage -PackageName 'sysinternals' `
 -Url 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 -UnzipLocation "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"

.EXAMPLE
>
Install-ChocolateyZipPackage -PackageName 'sysinternals' `
 -Url 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 -UnzipLocation "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)" `
 -Url64 'http://download.sysinternals.com/Files/SysinternalsSuitex64.zip'

.LINK
Get-ChocolateyWebFile

.LINK
Get-ChocolateyUnzip
#>
    param(
        [parameter(Mandatory = $true, Position = 0)][string] $packageName,
        [parameter(Mandatory = $false, Position = 1)][string] $url = '',
        [parameter(Mandatory = $true, Position = 2)]
        [alias("destination")][string] $unzipLocation,
        [parameter(Mandatory = $false, Position = 3)]
        [alias("url64")][string] $url64bit = '',
        [parameter(Mandatory = $false)][string] $specificFolder = '',
        [parameter(Mandatory = $false)][string] $checksum = '',
        [parameter(Mandatory = $false)][string] $checksumType = '',
        [parameter(Mandatory = $false)][string] $checksum64 = '',
        [parameter(Mandatory = $false)][string] $checksumType64 = '',
        [parameter(Mandatory = $false)][hashtable] $options = @{Headers = @{} },
        [alias("fileFullPath")][parameter(Mandatory = $false)][string] $file = '',
        [alias("fileFullPath64")][parameter(Mandatory = $false)][string] $file64 = '',
        [parameter(Mandatory = $false)][switch] $disableLogging,
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    $fileType = 'zip'

    $chocoTempDir = $env:TEMP
    $tempDir = Join-Path $chocoTempDir "$($env:chocolateyPackageName)"
    if ($env:chocolateyPackageVersion -ne $null) {
        $tempDir = Join-Path $tempDir "$($env:chocolateyPackageVersion)";
    }
    $tempDir = $tempDir -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
    if (![System.IO.Directory]::Exists($tempDir)) {
        [System.IO.Directory]::CreateDirectory($tempDir) | Out-Null
    }
    $downloadFilePath = Join-Path $tempDir "$($packageName)Install.$fileType"

    if ($url -eq '' -or $url -eq $null) {
        $url = $file
    }
    if ($url64bit -eq '' -or $url64bit -eq $null) {
        $url64bit = $file64
    }

    $filePath = Get-ChocolateyWebFile $packageName $downloadFilePath $url $url64bit -checkSum $checkSum -checksumType $checksumType -checkSum64 $checkSum64 -checksumType64 $checksumType64 -options $options -getOriginalFileName
    Get-ChocolateyUnzip "$filePath" $unzipLocation $specificFolder $packageName -disableLogging:$disableLogging
}

# SIG # Begin signature block
# MIIjgQYJKoZIhvcNAQcCoIIjcjCCI24CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCDKQVBTDy7pxVkV
# M29VOlaRW9UA29hkVL8a7C5Kjnz+T6CCHXowggUwMIIEGKADAgECAhAECRgbX9W7
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
# H4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIGwjCCBKqgAwIBAgIQBUSv
# 85SdCDmmv9s/X+VhFjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEXMBUG
# A1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0ZWQg
# RzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIzMDcxNDAwMDAw
# MFoXDTM0MTAxMzIzNTk1OVowSDELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lD
# ZXJ0LCBJbmMuMSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMzCCAiIw
# DQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKNTRYcdg45brD5UsyPgz5/X5dLn
# XaEOCdwvSKOXejsqnGfcYhVYwamTEafNqrJq3RApih5iY2nTWJw1cb86l+uUUI8c
# IOrHmjsvlmbjaedp/lvD1isgHMGXlLSlUIHyz8sHpjBoyoNC2vx/CSSUpIIa2mq6
# 2DvKXd4ZGIX7ReoNYWyd/nFexAaaPPDFLnkPG2ZS48jWPl/aQ9OE9dDH9kgtXkV1
# lnX+3RChG4PBuOZSlbVH13gpOWvgeFmX40QrStWVzu8IF+qCZE3/I+PKhu60pCFk
# cOvV5aDaY7Mu6QXuqvYk9R28mxyyt1/f8O52fTGZZUdVnUokL6wrl76f5P17cz4y
# 7lI0+9S769SgLDSb495uZBkHNwGRDxy1Uc2qTGaDiGhiu7xBG3gZbeTZD+BYQfvY
# sSzhUa+0rRUGFOpiCBPTaR58ZE2dD9/O0V6MqqtQFcmzyrzXxDtoRKOlO0L9c33u
# 3Qr/eTQQfqZcClhMAD6FaXXHg2TWdc2PEnZWpST618RrIbroHzSYLzrqawGw9/sq
# hux7UjipmAmhcbJsca8+uG+W1eEQE/5hRwqM/vC2x9XH3mwk8L9CgsqgcT2ckpME
# tGlwJw1Pt7U20clfCKRwo+wK8REuZODLIivK8SgTIUlRfgZm0zu++uuRONhRB8qU
# t+JQofM604qDy0B7AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0T
# AQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgGBmeB
# DAEEAjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxqII+e
# yG8wHQYDVR0OBBYEFKW27xPn783QZKHVVqllMaPe1eNJMFoGA1UdHwRTMFEwT6BN
# oEuGSWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJT
# QTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGDMIGA
# MCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYBBQUH
# MAKGTGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRH
# NFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQELBQAD
# ggIBAIEa1t6gqbWYF7xwjU+KPGic2CX/yyzkzepdIpLsjCICqbjPgKjZ5+PF7SaC
# inEvGN1Ott5s1+FgnCvt7T1IjrhrunxdvcJhN2hJd6PrkKoS1yeF844ektrCQDif
# XcigLiV4JZ0qBXqEKZi2V3mP2yZWK7Dzp703DNiYdk9WuVLCtp04qYHnbUFcjGnR
# uSvExnvPnPp44pMadqJpddNQ5EQSviANnqlE0PjlSXcIWiHFtM+YlRpUurm8wWkZ
# us8W8oM3NG6wQSbd3lqXTzON1I13fXVFoaVYJmoDRd7ZULVQjK9WvUzF4UbFKNOt
# 50MAcN7MmJ4ZiQPq1JE3701S88lgIcRWR+3aEUuMMsOI5ljitts++V+wQtaP4xeR
# 0arAVeOGv6wnLEHQmjNKqDbUuXKWfpd5OEhfysLcPTLfddY2Z1qJ+Panx+VPNTwA
# vb6cKmx5AdzaROY63jg7B145WPR8czFVoIARyxQMfq68/qTreWWqaNYiyjvrmoI1
# VygWy2nyMpqy0tg6uLFGhmu6F/3Ed2wVbK6rr3M66ElGt9V/zLY4wNjsHPW2obhD
# LN9OTH0eaHDAdwrUAuBcYLso/zjlUlrWrBciI0707NMX+1Br/wd3H3GXREHJuEbT
# bDJ8WC9nR2XlG3O2mflrLAZG70Ee8PBf4NvZrZCARK+AEEGKMYIFXTCCBVkCAQEw
# gYYwcjELMAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZMBcGA1UE
# CxMQd3d3LmRpZ2ljZXJ0LmNvbTExMC8GA1UEAxMoRGlnaUNlcnQgU0hBMiBBc3N1
# cmVkIElEIENvZGUgU2lnbmluZyBDQQIQCrnTEPshK+iMgbPSwujOUTANBglghkgB
# ZQMEAgEFAKCBhDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJ
# AzEMBgorBgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8G
# CSqGSIb3DQEJBDEiBCCvWcFD1dbPUcgtX5BxwKaTdaQQjtrUzOQpqs/GwaQ8pzAN
# BgkqhkiG9w0BAQEFAASCAQBIJFZw1RUXLTX1BU34RQvKfk34OeLOSMKhzbLQFUWc
# 6U67kFVHziaKQYPntU8IbmdRl3Et/u30DstmZUTJlNn7cFfOX74vn+UEqRCmraMD
# ZRflBg2EF1HffT0SdlkXra6PU23OI6zcZQhRQ6qOqEHtD7Vt/plbh1dnNYDo9VzH
# sQs7Z9cuCkmyIWQ6W0WErWLO4+QWVlnlGanAsSbDt/Gwcx83/m66n2mkr7TuKkiL
# Lbj4kTFvR78EMlSuBnZ3bR3h8HjSYE1BCbmaw9lu40URqTF/ENublIqu2t6H4z64
# BBLYdc/Rwae5CG4oz8kO1nl8fUWsSJU5GWJ0Cgk/jlWNoYIDIDCCAxwGCSqGSIb3
# DQEJBjGCAw0wggMJAgEBMHcwYzELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lD
# ZXJ0LCBJbmMuMTswOQYDVQQDEzJEaWdpQ2VydCBUcnVzdGVkIEc0IFJTQTQwOTYg
# U0hBMjU2IFRpbWVTdGFtcGluZyBDQQIQBUSv85SdCDmmv9s/X+VhFjANBglghkgB
# ZQMEAgEFAKBpMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkF
# MQ8XDTIzMDgwODA3MDgzM1owLwYJKoZIhvcNAQkEMSIEIOMgi+ZUjRLgAf5gfiY7
# w44UdY3nGxC0B24RXwO2e5X1MA0GCSqGSIb3DQEBAQUABIICADfwdPsHBRjVwHnr
# zEuuFf31EVn3KXQ/v1Fj90SL4voJQm443mxwmFqPiiVUu23aR1MxpOl+V1a/ok27
# vPQo6OAKEeWXvfYeZ5zDhBgtGbeRf6bWHLKc/ILqe0pStvrGAr/30iSOWfN1v/5p
# ZEmSCUSkHhKZJi6kZL3YjLhP/gdAqzgvnk1sT90MdAkwIv6pmBoPzgqzFTOJdmz5
# SHKSDo3lsh7bae8IpmOWLS4gf50TLGUMBvXKYU18WNp1Hn++J5kClaXVs5L5nHJA
# 2Tg0hx570t/DXAgoshKXR2t9ycntJbsrbCk9FE47slxxXxJJbdekeCbuNpVm0Az6
# 7rHBlE2uDKEzwo51Lnttawry/vqwJkwbi49faUDlYRD6SD1FfCZ0+pMbkdFDkCzv
# ts9YSjNr6TK0FvgD2m5UARBivpCysVQTX7JSq4aLHZxFxBRhnw/7ISCJS4ZO9PH2
# 0nx1xexsGqrLbA7grK3H+4rR2lxoRRBS5SkvsirC0ZFXsWHV4hK5dIbJZrgse6Gm
# Bh0bsJFFjv9rqIgildVDRCfmZYsLoBDVaQ7KXnk63srNAll73gCx6U8Yu3UhJtcd
# cYc6aBjR8ZoC0P5Gza+o1T31EYF1kSkL03+UFix0wYHgoFK3veuJNCIusEsV2e5a
# xlCV6FqCWKiETozJRQegW1rWx8pF
# SIG # End signature block
