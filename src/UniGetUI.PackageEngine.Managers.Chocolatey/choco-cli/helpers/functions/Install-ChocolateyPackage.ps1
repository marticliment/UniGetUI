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

function Install-ChocolateyPackage {
    <#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Installs software into "Programs and Features" based on a remote file
download. Use Install-ChocolateyInstallPackage when local or embedded
file.

Building packages for an organization or for use internally? You want to
use Install-ChocolateyINSTALLPackage instead of this method (see links
below).

.DESCRIPTION
This will download a native installer from a url and install it on your
machine. Has error handling built in.

If you are embedding the file(s) directly in the package (or do not need
to download a file first), use Install-ChocolateyInstallPackage instead.

Building packages for an organization or for use internally? You want to
use Install-ChocolateyINSTALLPackage instead of this method
(see links below).

.NOTES
This command will assert UAC/Admin privileges on the machine.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

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

.PARAMETER FileType
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
Licensed editions of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

.PARAMETER SilentArgs
OPTIONAL - These are the parameters to pass to the native installer,
including any arguments to make the installer silent/unattended.
Licensed editions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent (unattended) installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the community repository, it is nearly a requirement
for the package to be completely unattended.

When you are using this with an MSI, it will set up the arguments as
follows:
`"C:\Full\Path\To\msiexec.exe" /i "$downloadedFileFullPath" $silentArgs`,
where `$downloadedfileFullPath` is `$url` or `$url64`, depending on what
has been decided to be used.

When you use this with MSU, it is similar to MSI above in that it finds
the right executable to run.

When you use this with executable installers, the
`$downloadedFileFullPath` will also be `$url`/`$url64` SilentArgs is
everything you call against that file, as in
`"$fileFullPath" $silentArgs"`. An example would be
`"c:\path\setup.exe" /S`, where
`$downloadedfileFullPath = "c:\path\setup.exe"` and `$silentArgs = "/S"`.

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
contains both (which is quite rare), set this to '$url' Otherwise remove
this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

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

.PARAMETER UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and only
use the ones available with the package.

.PARAMETER UseOriginalLocation
Do not download the resources. This is typically passed if Url/Url64bit
are pointed to local files or files on a share and those files should
be used in place.

NOTE: You can also use `Install-ChocolateyInstallPackage` for the same
functionality (see links).

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.PARAMETER BeforeInstall Script
Specifies the commands to run after download has completed but before install steps have begun.

Use this for starting an auxiliary process such as AutoHotkey, so that any timeouts are not
affected by the time to download.

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://somewhere.com/file.msi'
$url64      = 'https://somewhere.com/file-x64.msi'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  url           = $url
  url64bit      = $url64
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
  checksum      = '12345'
  checksumType  = 'sha256'
  checksum64    = '123356'
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://somewhere.com/file.msi'
$url64      = 'https://somewhere.com/file-x64.msi'


$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  url           = $url
  url64bit      = $url64
  silentArgs    = "/qn /norestart MSIPROPERTY=`"true`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
  checksum      = '12345'
  checksumType  = 'sha256'
  checksum64    = '123356'
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$urlTransform = 'https://somewhere.com/file.mst'
$mstFileLocation = Join-Path $toolsDir 'transform.mst'

Get-ChocolateyWebFile -PackageName 'bob' `
                      -Url $urlTransform -FileFullPath $mstFileLocation `
                      -Checksum '1234' -ChecksumType 'sha256'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart TRANSFORMS=`"$mstFileLocation`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs

.EXAMPLE
>
Install-ChocolateyPackage 'StExBar' 'msi' '/quiet' `
 'http://stexbar.googlecode.com/files/StExBar-1.8.3.msi' `
 'http://stexbar.googlecode.com/files/StExBar64-1.8.3.msi'

.EXAMPLE
>
Install-ChocolateyPackage 'mono' 'exe' '/SILENT' `
 'http://somewhere/something.exe' -ValidExitCodes @(0,21)

.EXAMPLE
>
Install-ChocolateyPackage 'ruby.devkit' 'exe' '/SILENT' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-32-4.7.2-20130224-1151-sfx.exe' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-64-4.7.2-20130224-1432-sfx.exe' `
 -checksum '9383f12958aafc425923e322460a84de' -checksumType 'md5' `
 -checksum64 'ce99d873c1acc8bffc639bd4e764b849'

.EXAMPLE
Install-ChocolateyPackage 'bob' 'exe' '/S' 'https://somewhere/bob.exe' 'https://somewhere/bob-x64.exe'

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

Install-ChocolateyPackage -PackageName 'package' -FileType 'exe' -SilentArgs '/S' 'https://somelocation.com/thefile.exe' -Options $options

.LINK
Get-ChocolateyWebFile

.LINK
Install-ChocolateyInstallPackage

.LINK
Get-UninstallRegistryKey

.LINK
Install-ChocolateyZipPackage
#>
    param(
        [parameter(Mandatory = $true, Position = 0)][string] $packageName,
        [parameter(Mandatory = $false, Position = 1)]
        [alias("installerType", "installType")][string] $fileType = 'exe',
        [parameter(Mandatory = $false, Position = 2)][string[]] $silentArgs = '',
        [parameter(Mandatory = $false, Position = 3)][string] $url = '',
        [parameter(Mandatory = $false, Position = 4)]
        [alias("url64")][string] $url64bit = '',
        [parameter(Mandatory = $false)] $validExitCodes = @(0),
        [parameter(Mandatory = $false)][string] $checksum = '',
        [parameter(Mandatory = $false)][string] $checksumType = '',
        [parameter(Mandatory = $false)][string] $checksum64 = '',
        [parameter(Mandatory = $false)][string] $checksumType64 = '',
        [parameter(Mandatory = $false)][hashtable] $options = @{Headers = @{} },
        [alias("fileFullPath")][parameter(Mandatory = $false)][string] $file = '',
        [alias("fileFullPath64")][parameter(Mandatory = $false)][string] $file64 = '',
        [parameter(Mandatory = $false)]
        [alias("useOnlyPackageSilentArgs")][switch] $useOnlyPackageSilentArguments = $false,
        [parameter(Mandatory = $false)][switch]$useOriginalLocation,
        [parameter(Mandatory = $false)][scriptblock] $beforeInstall,
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )
    [string]$silentArgs = $silentArgs -join ' '

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

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

    [string]$filePath = $downloadFilePath
    if ($useOriginalLocation) {
        $filePath = $url
        if (Get-OSArchitectureWidth 64) {
            $forceX86 = $env:chocolateyForceX86
            if ($forceX86) {
                Write-Debug "User specified '-x86' so forcing 32-bit"
            }
            else {
                if ($url64bit -ne $null -and $url64bit -ne '') {
                    $filePath = $url64bit
                }
            }
        }
    }
    else {
        $filePath = Get-ChocolateyWebFile -PackageName $packageName `
            -FileFullPath $downloadFilePath `
            -Url $url `
            -Url64bit $url64bit `
            -Checksum $checksum `
            -ChecksumType $checksumType `
            -Checksum64 $checksum64 `
            -ChecksumType64 $checksumType64 `
            -Options $options `
            -GetOriginalFileName
    }

    if ($beforeInstall) {
        & $beforeInstall
    }

    Install-ChocolateyInstallPackage -packageName $packageName `
        -fileType $fileType `
        -silentArgs $silentArgs `
        -file $filePath `
        -validExitCodes $validExitCodes `
        -UseOnlyPackageSilentArguments:$useOnlyPackageSilentArguments
}

# SIG # Begin signature block
# MIInKwYJKoZIhvcNAQcCoIInHDCCJxgCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCr5worPIXRCnmJ
# fBd03cqX9I783zWUienpJqyJQ+/OFqCCIK4wggWNMIIEdaADAgECAhAOmxiO+dAt
# 5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBa
# Fw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lD
# ZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoC
# ggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3E
# MB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKy
# unWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsF
# xl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU1
# 5zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJB
# MtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObUR
# WBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6
# nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxB
# YKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5S
# UUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+x
# q4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIB
# NjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwP
# TzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMC
# AYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdp
# Y2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNv
# bS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENB
# LmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0Nc
# Vec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnov
# Lbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65Zy
# oUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFW
# juyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPF
# mCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9z
# twGpn1eqXijiuZQwggauMIIElqADAgECAhAHNje3JFR82Ees/ShmKl5bMA0GCSqG
# SIb3DQEBCwUAMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRy
# dXN0ZWQgUm9vdCBHNDAeFw0yMjAzMjMwMDAwMDBaFw0zNzAzMjIyMzU5NTlaMGMx
# CzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMy
# RGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNIQTI1NiBUaW1lU3RhbXBpbmcg
# Q0EwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDGhjUGSbPBPXJJUVXH
# JQPE8pE3qZdRodbSg9GeTKJtoLDMg/la9hGhRBVCX6SI82j6ffOciQt/nR+eDzMf
# UBMLJnOWbfhXqAJ9/UO0hNoR8XOxs+4rgISKIhjf69o9xBd/qxkrPkLcZ47qUT3w
# 1lbU5ygt69OxtXXnHwZljZQp09nsad/ZkIdGAHvbREGJ3HxqV3rwN3mfXazL6IRk
# tFLydkf3YYMZ3V+0VAshaG43IbtArF+y3kp9zvU5EmfvDqVjbOSmxR3NNg1c1eYb
# qMFkdECnwHLFuk4fsbVYTXn+149zk6wsOeKlSNbwsDETqVcplicu9Yemj052FVUm
# cJgmf6AaRyBD40NjgHt1biclkJg6OBGz9vae5jtb7IHeIhTZgirHkr+g3uM+onP6
# 5x9abJTyUpURK1h0QCirc0PO30qhHGs4xSnzyqqWc0Jon7ZGs506o9UD4L/wojzK
# QtwYSH8UNM/STKvvmz3+DrhkKvp1KCRB7UK/BZxmSVJQ9FHzNklNiyDSLFc1eSuo
# 80VgvCONWPfcYd6T/jnA+bIwpUzX6ZhKWD7TA4j+s4/TXkt2ElGTyYwMO1uKIqjB
# Jgj5FBASA31fI7tk42PgpuE+9sJ0sj8eCXbsq11GdeJgo1gJASgADoRU7s7pXche
# MBK9Rp6103a50g5rmQzSM7TNsQIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB
# /wIBADAdBgNVHQ4EFgQUuhbZbU2FL3MpdpovdYxqII+eyG8wHwYDVR0jBBgwFoAU
# 7NfjgtJxXWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoG
# CCsGAQUFBwMIMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29j
# c3AuZGlnaWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDig
# NqA0hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9v
# dEc0LmNybDAgBgNVHSAEGTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZI
# hvcNAQELBQADggIBAH1ZjsCTtm+YqUQiAX5m1tghQuGwGC4QTRPPMFPOvxj7x1Bd
# 4ksp+3CKDaopafxpwc8dB+k+YMjYC+VcW9dth/qEICU0MWfNthKWb8RQTGIdDAiC
# qBa9qVbPFXONASIlzpVpP0d3+3J0FNf/q0+KLHqrhc1DX+1gtqpPkWaeLJ7giqzl
# /Yy8ZCaHbJK9nXzQcAp876i8dU+6WvepELJd6f8oVInw1YpxdmXazPByoyP6wCeC
# RK6ZJxurJB4mwbfeKuv2nrF5mYGjVoarCkXJ38SNoOeY+/umnXKvxMfBwWpx2cYT
# gAnEtp/Nh4cku0+jSbl3ZpHxcpzpSwJSpzd+k1OsOx0ISQ+UzTl63f8lY5knLD0/
# a6fxZsNBzU+2QJshIUDQtxMkzdwdeDrknq3lNHGS1yZr5Dhzq6YBT70/O3itTK37
# xJV77QpfMzmHQXh6OOmc4d0j/R0o08f56PGYX/sr2H7yRp11LB4nLCbbbxV7HhmL
# NriT1ObyF5lZynDwN7+YAN8gFk8n+2BnFqFmut1VwDophrCYoCvtlUG3OtUVmDG0
# YgkPCr2B2RP+v6TR81fZvAT6gt4y3wSJ8ADNXcL50CN/AAvkdgIm2fBldkKmKYcJ
# RyvmfxqkhQ/8mJb2VVQrH4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIG
# sDCCBJigAwIBAgIQCK1AsmDSnEyfXs2pvZOu2TANBgkqhkiG9w0BAQwFADBiMQsw
# CQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cu
# ZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBUcnVzdGVkIFJvb3QgRzQw
# HhcNMjEwNDI5MDAwMDAwWhcNMzYwNDI4MjM1OTU5WjBpMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0IFRydXN0
# ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0ExMIICIjAN
# BgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA1bQvQtAorXi3XdU5WRuxiEL1M4zr
# PYGXcMW7xIUmMJ+kjmjYXPXrNCQH4UtP03hD9BfXHtr50tVnGlJPDqFX/IiZwZHM
# gQM+TXAkZLON4gh9NH1MgFcSa0OamfLFOx/y78tHWhOmTLMBICXzENOLsvsI8Irg
# nQnAZaf6mIBJNYc9URnokCF4RS6hnyzhGMIazMXuk0lwQjKP+8bqHPNlaJGiTUyC
# EUhSaN4QvRRXXegYE2XFf7JPhSxIpFaENdb5LpyqABXRN/4aBpTCfMjqGzLmysL0
# p6MDDnSlrzm2q2AS4+jWufcx4dyt5Big2MEjR0ezoQ9uo6ttmAaDG7dqZy3SvUQa
# khCBj7A7CdfHmzJawv9qYFSLScGT7eG0XOBv6yb5jNWy+TgQ5urOkfW+0/tvk2E0
# XLyTRSiDNipmKF+wc86LJiUGsoPUXPYVGUztYuBeM/Lo6OwKp7ADK5GyNnm+960I
# HnWmZcy740hQ83eRGv7bUKJGyGFYmPV8AhY8gyitOYbs1LcNU9D4R+Z1MI3sMJN2
# FKZbS110YU0/EpF23r9Yy3IQKUHw1cVtJnZoEUETWJrcJisB9IlNWdt4z4FKPkBH
# X8mBUHOFECMhWWCKZFTBzCEa6DgZfGYczXg4RTCZT/9jT0y7qg0IU0F8WD1Hs/q2
# 7IwyCQLMbDwMVhECAwEAAaOCAVkwggFVMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYD
# VR0OBBYEFGg34Ou2O/hfEYb7/mF7CIhl9E5CMB8GA1UdIwQYMBaAFOzX44LScV1k
# TN8uZz/nupiuHA9PMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcD
# AzB3BggrBgEFBQcBAQRrMGkwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2lj
# ZXJ0LmNvbTBBBggrBgEFBQcwAoY1aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29t
# L0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcnQwQwYDVR0fBDwwOjA4oDagNIYyaHR0
# cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcmww
# HAYDVR0gBBUwEzAHBgVngQwBAzAIBgZngQwBBAEwDQYJKoZIhvcNAQEMBQADggIB
# ADojRD2NCHbuj7w6mdNW4AIapfhINPMstuZ0ZveUcrEAyq9sMCcTEp6QRJ9L/Z6j
# fCbVN7w6XUhtldU/SfQnuxaBRVD9nL22heB2fjdxyyL3WqqQz/WTauPrINHVUHmI
# moqKwba9oUgYftzYgBoRGRjNYZmBVvbJ43bnxOQbX0P4PpT/djk9ntSZz0rdKOtf
# JqGVWEjVGv7XJz/9kNF2ht0csGBc8w2o7uCJob054ThO2m67Np375SFTWsPK6Wrx
# oj7bQ7gzyE84FJKZ9d3OVG3ZXQIUH0AzfAPilbLCIXVzUstG2MQ0HKKlS43Nb3Y3
# LIU/Gs4m6Ri+kAewQ3+ViCCCcPDMyu/9KTVcH4k4Vfc3iosJocsL6TEa/y4ZXDlx
# 4b6cpwoG1iZnt5LmTl/eeqxJzy6kdJKt2zyknIYf48FWGysj/4+16oh7cGvmoLr9
# Oj9FpsToFpFSi0HASIRLlk2rREDjjfAVKM7t8RhWByovEMQMCGQ8M4+uKIw8y4+I
# Cw2/O/TOHnuO77Xry7fwdxPm5yg/rBKupS8ibEH5glwVZsxsDsrFhsP2JjMMB0ug
# 0wcCampAMEhLNKhRILutG4UI4lkNbcoFUCvqShyepf2gpx8GdOfy1lKQ/a+FSCH5
# Vzu0nAPthkX0tGFuv2jiJmCG6sivqf6UHedjGzqGVnhOMIIGwjCCBKqgAwIBAgIQ
# BUSv85SdCDmmv9s/X+VhFjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0
# ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIzMDcxNDAw
# MDAwMFoXDTM0MTAxMzIzNTk1OVowSDELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRp
# Z2lDZXJ0LCBJbmMuMSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMzCC
# AiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKNTRYcdg45brD5UsyPgz5/X
# 5dLnXaEOCdwvSKOXejsqnGfcYhVYwamTEafNqrJq3RApih5iY2nTWJw1cb86l+uU
# UI8cIOrHmjsvlmbjaedp/lvD1isgHMGXlLSlUIHyz8sHpjBoyoNC2vx/CSSUpIIa
# 2mq62DvKXd4ZGIX7ReoNYWyd/nFexAaaPPDFLnkPG2ZS48jWPl/aQ9OE9dDH9kgt
# XkV1lnX+3RChG4PBuOZSlbVH13gpOWvgeFmX40QrStWVzu8IF+qCZE3/I+PKhu60
# pCFkcOvV5aDaY7Mu6QXuqvYk9R28mxyyt1/f8O52fTGZZUdVnUokL6wrl76f5P17
# cz4y7lI0+9S769SgLDSb495uZBkHNwGRDxy1Uc2qTGaDiGhiu7xBG3gZbeTZD+BY
# QfvYsSzhUa+0rRUGFOpiCBPTaR58ZE2dD9/O0V6MqqtQFcmzyrzXxDtoRKOlO0L9
# c33u3Qr/eTQQfqZcClhMAD6FaXXHg2TWdc2PEnZWpST618RrIbroHzSYLzrqawGw
# 9/sqhux7UjipmAmhcbJsca8+uG+W1eEQE/5hRwqM/vC2x9XH3mwk8L9CgsqgcT2c
# kpMEtGlwJw1Pt7U20clfCKRwo+wK8REuZODLIivK8SgTIUlRfgZm0zu++uuRONhR
# B8qUt+JQofM604qDy0B7AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYD
# VR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgG
# BmeBDAEEAjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxq
# II+eyG8wHQYDVR0OBBYEFKW27xPn783QZKHVVqllMaPe1eNJMFoGA1UdHwRTMFEw
# T6BNoEuGSWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRH
# NFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGD
# MIGAMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYB
# BQUHMAKGTGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0
# ZWRHNFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQEL
# BQADggIBAIEa1t6gqbWYF7xwjU+KPGic2CX/yyzkzepdIpLsjCICqbjPgKjZ5+PF
# 7SaCinEvGN1Ott5s1+FgnCvt7T1IjrhrunxdvcJhN2hJd6PrkKoS1yeF844ektrC
# QDifXcigLiV4JZ0qBXqEKZi2V3mP2yZWK7Dzp703DNiYdk9WuVLCtp04qYHnbUFc
# jGnRuSvExnvPnPp44pMadqJpddNQ5EQSviANnqlE0PjlSXcIWiHFtM+YlRpUurm8
# wWkZus8W8oM3NG6wQSbd3lqXTzON1I13fXVFoaVYJmoDRd7ZULVQjK9WvUzF4UbF
# KNOt50MAcN7MmJ4ZiQPq1JE3701S88lgIcRWR+3aEUuMMsOI5ljitts++V+wQtaP
# 4xeR0arAVeOGv6wnLEHQmjNKqDbUuXKWfpd5OEhfysLcPTLfddY2Z1qJ+Panx+VP
# NTwAvb6cKmx5AdzaROY63jg7B145WPR8czFVoIARyxQMfq68/qTreWWqaNYiyjvr
# moI1VygWy2nyMpqy0tg6uLFGhmu6F/3Ed2wVbK6rr3M66ElGt9V/zLY4wNjsHPW2
# obhDLN9OTH0eaHDAdwrUAuBcYLso/zjlUlrWrBciI0707NMX+1Br/wd3H3GXREHJ
# uEbTbDJ8WC9nR2XlG3O2mflrLAZG70Ee8PBf4NvZrZCARK+AEEGKMIIG7TCCBNWg
# AwIBAgIQBNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0
# IFRydXN0ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0Ex
# MB4XDTI0MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMx
# DzANBgNVBAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9j
# b2xhdGV5IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdh
# cmUsIEluYzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RV
# lBBBniCyGy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAG
# DUPAGhXf0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDru
# pjshvLmQMSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz
# 2zCt8CvuR3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQgh
# KRoIwfp/hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN9
# 7++0BApX2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YV
# nKP2AOVca6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyF
# H8FVnZZV1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4IC
# AzCCAf8wHwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYE
# FFpfZUilS5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYI
# KwYBBQUHAgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8E
# BAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZN
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNp
# Z25pbmdSU0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5
# NlNIQTM4NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmlu
# Z1JTQTQwOTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEB
# CwUAA4ICAQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDH
# BZP9sDRZsFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGU
# iA3mKdbVit0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T
# 9D8ct/m5LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIW
# v/AHDf8ukJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIe
# M3FdtLstn9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4
# hf/0MqEtWSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOa
# QbKf0CxdxMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+c
# YcpmI4R13Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyV
# b6zGqAaE9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM1
# 8zHiN4WA4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDGCBdMwggXP
# AgEBMH0waTELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEw
# PwYDVQQDEzhEaWdpQ2VydCBUcnVzdGVkIEc0IENvZGUgU2lnbmluZyBSU0E0MDk2
# IFNIQTM4NCAyMDIxIENBMQIQBNI793flHTneCMtwLiiYFTANBglghkgBZQMEAgEF
# AKCBhDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgor
# BgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3
# DQEJBDEiBCBdcb2cqsIg0EhKi6GuOkY4pFNT3xCiRW/Keo5/wOrkuDANBgkqhkiG
# 9w0BAQEFAASCAYDvJtXxlEC8Pq9M3h3EpV0sqLpauwUzWVIV31Dp0yFmTzCjm0kW
# ytFWAoWdyknTo+mKHntj14JlwnrI5dgA3IdUtUn+aOb5ulJv1Cap7hHlSWt3/hti
# YMnOPTTU/Dglqa6QI7v4GCuNCGka1BQjp1eDfh50Y7Y8CpkFaanVFGqq2UrH/uw5
# mL9znZinS2uqqhmf+2/LPTHmB0KDGy53/ogn0n/ANOIfw1hiMk3D45dqClkjKNQE
# ftTUjApIG8S4VUE9LWX3172bx+ikfAz6pI3oOQfcslM6Otpnk0co6E+H5aTsTKxp
# b4bfdMegJle1ey89nKMgDU88x4kMkjAitUyKxUByTOv83OUnZqtsRbcf1KfanL69
# +Nepav/ZOLV8K+q6NM1OrPXBADsNWt71ElTLAKhccYiN5usoW5c4lxAWc1iY2bcL
# NfY6LeEJ8q1NCffvbCaXEY6xN5kxBtcGvVtpDkutnoUku7piUwUT9yUvtI3ulYdV
# U9MsCDup5MSXGf+hggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQsw
# CQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRp
# Z2lDZXJ0IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENB
# AhAFRK/zlJ0IOaa/2z9f5WEWMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkD
# MQsGCSqGSIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQwNTE2MTYxOTMwWjAvBgkq
# hkiG9w0BCQQxIgQgInZYZ7UPxtsFpgOeyMJ9WqdO8W27I9Z57RGugmSaNHQwDQYJ
# KoZIhvcNAQEBBQAEggIAc4F8mRQBQHH7NoOa+HaxmJ+lYFolqzMSaJeLPp8q5QgY
# 13kl3xhI7V6GWUa78/nGN7gUSGeyQZiL3QR8ZYQvlxNTP0e4nRd11FIm8os0X+EB
# pVGsBAKx/ErCuee45pEu4wDrTqo+vmlhRDE/jhNZK81XsUGbddbgPNct3jj2ynkT
# Dv26PDD1OJow7DB6tEjzInnSjgyg4p7vG6IEMDjwYJvG4M0hyGgctk24OTy9nCC/
# q0CyrroNNW82Kf4lU2QTD20ux5mbjXgRco3S2Iy3u8CSdhGud2diDrInIPJkeCFR
# GEdXeel9F48YuOdGGwkdZlyQbr4cpK4sKyyKHLVOGpOlWTMfa7lm0b7jwz5nbev4
# L/LM9iRebphOfPYb7Nx2ksoLDytkEk9UbGsAOiEhGKxxt/KPBj3mSO6WpV8R5oc1
# X+qSgfI7mHQMpLM3DdrUyyIR80HFXFgv7MCnbHwUtyxnvp0BvciPxkcP1Vd4h3CO
# rknJXb7ljoHg1rpliJkojfk4PD4x8oOXvVYPvaIWJ3tT5NQi/ziiXmJVzzQu0kVP
# gDmVwzrRkg6I9YqBbXTUjBnHHI7nL6qO51T9CuKK2g+07rx4L7n+Z/YN1zy7p0Mm
# VDn4gZzfNo3mEb/SigDEnfLxL4mNGPuonc/6TuMprHOZ4kp7I3MMHBxBMPijfbI=
# SIG # End signature block
