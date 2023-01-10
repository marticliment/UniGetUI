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

function Install-ChocolateyVsixPackage {
<#
.SYNOPSIS
Downloads and installs a VSIX package for Visual Studio

.DESCRIPTION
VSIX packages are Extensions for the Visual Studio IDE. The Visual
Studio Gallery at  http://visualstudiogallery.msdn.microsoft.com/ is the
public extension feed and hosts thousands of extensions. You can locate
a VSIX Url by finding the download link of Visual Studio extensions on
the Visual Studio Gallery.

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

In 0.10.4+, `Name` is an alias for PackageName.

.PARAMETER VsixUrl
The URL of the package to be installed.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

In 0.10.4+, `Url` is an alias for VsixUrl.

.PARAMETER VsVersion
The major version number of Visual Studio where the
package should be installed. This is optional. If not
specified, the most recent Visual Studio installation
will be targeted.

NOTE: For Visual Studio 2015, the VsVersion is 14. It can be determined
by looking at the folders under Program Files / Program Files (x86).

In 0.10.4+, `VisualStudioVersion` is an alias for VsVersion.

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

.PARAMETER Options
OPTIONAL - Specify custom headers. Available in 0.9.10+.

.PARAMETER File
Will be used for VsixUrl if VsixUrl is empty. Available in 0.10.7+.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to the latest version of VS.

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix

.EXAMPLE
>
# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to Visual Studio 2012 (v11.0).

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix `
  -VsVersion 11

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Install-ChocolateyZipPackage
#>
param(
  [alias("name")][parameter(Mandatory=$true, Position=0)][string] $packageName,
  [alias("url")][parameter(Mandatory=$false, Position=1)][string] $vsixUrl,
  [alias("visualStudioVersion")][parameter(Mandatory=$false, Position=2)][int] $vsVersion = 0,
  [parameter(Mandatory=$false)][string] $checksum = '',
  [parameter(Mandatory=$false)][string] $checksumType = '',
  [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
  [alias("fileFullPath")][parameter(Mandatory=$false)][string] $file = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    if ($vsixUrl -eq '' -or $vsixUrl -eq $null) {
        $vsixUrl = $file
    }

    if($vsVersion -eq 0) {
        if ([System.IntPtr]::Size -eq 4)
        {
            <# 32bits system case #>
            $versions = Get-ChildItem HKLM:SOFTWARE\Microsoft\VisualStudio -ErrorAction SilentlyContinue |
              Where-Object { ($_.PSChildName -match "^[0-9\.]+$") } |
              Where-Object { $_.property -contains "InstallDir" } |
              Sort-Object { [int]($_.PSChildName) } -descending
        }
        else
        {
            $versions=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | Where-Object { ($_.PSChildName -match "^[0-9\.]+$") } | Where-Object {$_.property -contains "InstallDir"} | Sort-Object {[int]($_.PSChildName)} -descending)
        }
        if($versions -and $versions.Length){
            $version = $versions[0]
        }elseif($versions){
            $version = $versions
        }
    }
    else {
        if ([System.IntPtr]::Size -eq 4)
        {
            <# 32bits system case #>
            $versions=(get-ChildItem HKLM:SOFTWARE\Microsoft\VisualStudio -ErrorAction SilentlyContinue | Where-Object { ($_.PSChildName.EndsWith("$vsVersion.0")) } | Where-Object {$_.property -contains "InstallDir"})
        }
        else
        {
            $version=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | Where-Object { ($_.PSChildName.EndsWith("$vsVersion.0")) } | Where-Object {$_.property -contains "InstallDir"})
        }
    }

    if ($version) {
        $vnum=$version.PSPath.Substring($version.PSPath.LastIndexOf('\')+1)
        if($vnum -as [int] -lt 10) {
            throw "This installed VS version, $vnum, does not support installing VSIX packages. Version 10 is the minimum acceptable version."
        }
        $dir=(get-itemProperty $version.PSPath "InstallDir").InstallDir
        $installer = Join-Path $dir "VsixInstaller.exe"
    }

    if ($installer) {
        $download="$env:TEMP\$($packageName.Replace(' ','')).vsix"
        try{
            Get-ChocolateyWebFile $packageName $download $vsixUrl -checksum $checksum -checksumType $checksumType -Options $options
        }
        catch {
            throw "There were errors attempting to retrieve the vsix from $vsixUrl. The error message was '$_'."
        }

        Write-Debug "Installing VSIX using $installer"
        $exitCode = Install-Vsix "$installer" "$download"
        if($exitCode -gt 0 -and $exitCode -ne 1001) { #1001: Already installed
           throw "There was an error installing '$packageName'. The exit code returned was $exitCode."
        }
    }
    else {
        throw "Visual Studio is not installed or the specified version is not present."
    }
}

# SIG # Begin signature block
# MIIjfwYJKoZIhvcNAQcCoIIjcDCCI2wCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCb9v+X9fcDfKTL
# ++O91eqGb9TQL/6cAjhx+zviZ6qbyqCCHXgwggUwMIIEGKADAgECAhAECRgbX9W7
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
# hkiG9w0BCQQxIgQgM0A2UTv7NAawFKCA1xfCYaWtX5Vui24vZLB9fTI4r4MwDQYJ
# KoZIhvcNAQEBBQAEggEAOVEN6NvlI/FFwScq6yhzmp2Q5VnSZKo2D/lsC0oQJ35o
# f/7EYijzDKkjCFpPmt7QcZo9fmbPbDKKsO/PVDM5NZ3hd4cGHsZXnaTx5+SWbD2N
# w/Ivyjr+lbw1uocEKm+O6GXxZWcblVb7jkMnGiAU0P45EXDa2qRI/iPis4QhF3Ao
# eU6IbXzSJp5A6XHeAZOdIgu9464l/gxLd0ECKmPJzkT6D5vi5sZu4ElCjJHNPoBJ
# Pz60ibHalljP2My8aB6K0YYX4e3RxUXNgGc7wCVBpGf/93P208+LEXBqd9mRE4vl
# 6lrrl5acaAdD4+GOyAe2MIVUrHK89j7ABsPQ+I8PPKGCAyAwggMcBgkqhkiG9w0B
# CQYxggMNMIIDCQIBATB3MGMxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2Vy
# dCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNI
# QTI1NiBUaW1lU3RhbXBpbmcgQ0ECEAxNaXJLlPo8Kko9KQeAPVowDQYJYIZIAWUD
# BAIBBQCgaTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEP
# Fw0yMjEyMDYxOTM5MTdaMC8GCSqGSIb3DQEJBDEiBCDqsXKfLyvZFB/6tOybMQgI
# xyQR8fIRsjN9rGoT9DZ/bTANBgkqhkiG9w0BAQEFAASCAgAsHFr5fiBZKfNx3rz8
# saHK+GSMUG2vGXVqccoXwVSfnIT0iALwUZhiRkiwLna40jy6m8lKQztoM2/Q4EB4
# 89zJiWcf6R+Pzp5XGaA3Vf0Cqv2gfEskhjZH37KZzQBeV7WYnJwiaLOvK44hRpE5
# T79fiId/48UopSYcnjk0UJJovMlhsSVnO8FE2XgLZD3JMP8c/ien9r3oHo1Adwd+
# J8pm7CgcWJFu0OEYAWZyXdEiMymwhJcCh9IwiPJG8aH4k00c9+n0dI+7RL9hd1M4
# ML3KO3jtzxpbfDskD1a8H9+rSx4BJzatxU+MZqKaKtvpsNvPg0GKoaM/Lc0S6gCr
# QQzhvVpWaYg0uwAE2kEV1qejtX7lchhqn4IqEfqE8TCnNWIkstcr4YAqVIlwzueS
# TBikkyBn8jS6fx7kTlaPwgN6sfIre02Fr00oLunvkCkyAPck3aDwdXLTCWVBvtnx
# L22DhJfSVynJCvqpGcQ5APkJG9Xh26Ibi30U5d2SZiY3acHBDBsKFWfr6THNhE5C
# 84ORYV7QRcEculZcHBWoMj1W3cRG7AVvMKhu8s8usm4srZEVvD/iwHmSgOn6HYUp
# dclDbIBk/ZK/tzU+gJkyGEmFcnf2BnVv++zCYmxrG19stvFP/8D/ReBNKwI8arBj
# KZKdczbqcIN0mTa8QuIMsFvRAA==
# SIG # End signature block
