# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2016 - 2017 Original authors from https://github.com/chocolatey/chocolatey-coreteampackages
# Copyright © 2016 Miodrag Milić - https://github.com/majkinetor/au-packages/commit/bf95d56fe5851ee2e4f6f15f79c1a2877a7950a1
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

# special thanks to the Core Community Maintainers team and their work
# on the Get-PackageParameters function that is in the
# `chocolatey-core.extension` package. It was used as a reference to
# create this function and some of the documentation and naming was
# used. However nearly all of the code is a different implementation.

function Get-PackageParameters {
    <#
.SYNOPSIS
Parses a string and returns a hash table array of those values for use
in package scripts.

.DESCRIPTION
This looks at a string value and parses it into a hash table array for
use in package scripts. By default this will look at
`$env:ChocolateyPackageParameters` (`--params="'/ITEM:value'"`) and
`$env:ChocolateyPackageParametersSensitive`
(`--package-parameters-sensitive="'/PASSWORD:value'"` in commercial
editions).

Learn more about using this at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument

.NOTES
If you need compatibility with older versions of Chocolatey,
take a dependency on the `chocolatey-core.extension` package which
also provides this functionality. If you are pushing to the community
package repository (https://community.chocolatey.org/packages), you are required
to take a dependency on the core extension until January 2018. How to
do this is explained at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument#step-3---use-core-community-extension.

The differences between this and the `chocolatey-core.extension` package
functionality is that the extension function can only do one string at a
time and it only looks at `$env:ChocolateyPackageParameters` by default.
It also only supports splitting by `:`, with this function you can
either split by `:` or `=`. For compatibility with the core extension,
build all docs with `/Item:Value`.

.INPUTS
None

.OUTPUTS
[HashTable]

.PARAMETER Parameters
OPTIONAL - Specify a string to parse. If not set, will use
`$env:ChocolateyPackageParameters` and
`$env:ChocolateyPackageParametersSensitive` to parse values from.

Parameters should be passed as "/NAME:value" or "/NAME=value". For
compatibility with `chocolatey-core.extension`, use `:`.

For example `-Parameters "/ITEM1:value /ITEM2:value with spaces"

To maintain compatibility with the prior art of the chocolatey-core.extension
function by the same name, quotes and apostrophes surrounding
parameter values will be removed. When the param is used, those items
can be added back if desired, but it's most important to ensure that
existing packages are compatible on upgrade.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply and future expansion.
Do not use directly.

.EXAMPLE
>
# The default way of calling, uses `$env:ChocolateyPackageParameters`
# and `$env:ChocolateyPackageParametersSensitive` - this is typically
# how things are passed in from choco.exe
$pp = Get-PackageParameters

.EXAMPLE
>
# see https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument
# command line call: `choco install <pkg_id> --params "'/LICENSE:value'"`
$pp = Get-PackageParameters
# Read-Host, PromptForChoice, etc are not blocking calls with Chocolatey.
# Chocolatey has a custom PowerShell host that will time these calls
# after 30 seconds, allowing headless operation to continue but offer
# prompts to users to ask questions during installation.
if (!$pp['LICENSE']) { $pp['LICENSE'] = Read-Host 'License key?' }
# set a default if not passed
if (!$pp['LICENSE']) { $pp['LICENSE'] = '1234' }

.EXAMPLE
>
$pp = Get-PackageParameters
if (!$pp['UserName']) { $pp['UserName'] = "$env:UserName" }
if (!$pp['Password']) { $pp['Password'] = Read-Host "Enter password for $($pp['UserName']):" -AsSecureString}
# fail the install/upgrade if not value is not determined
if (!$pp['Password']) { throw "Package needs Password to install, that must be provided in params or in prompt." }

.EXAMPLE
>
# Pass in your own values
Get-PackageParameters -Parameters "/Shortcut /InstallDir:'c:\program files\xyz' /NoStartup" | set r
if ($r.Shortcut) {... }
Write-Host $r.InstallDir

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Install-ChocolateyZipPackage
#>
    param(
        [parameter(Mandatory = $false, Position = 0)]
        [alias("params")][string] $parameters = '',
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    $useDefaultParameters = $false
    $loggingAllowed = $true
    $paramStrings = @($parameters)

    if (!$parameters -or $parameters -eq '') {
        $useDefaultParameters = $true
        # if we are using default parameters, we are going to loop over two items
        Write-Debug 'Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters'
        $paramStrings = @("$env:ChocolateyPackageParameters", "$env:ChocolateyPackageParametersSensitive")
        if ($env:ChocolateyPackageParametersSensitive) {
            Write-Debug "Sensitive parameters detected, no logging of parameters."
            $loggingAllowed = $false
        }
    }

    $paramHash = @{}

    foreach ($paramString in $paramStrings) {
        if (!$paramString -or $paramString -eq '') {
            continue
        }

        Select-String '(?:^|\s+)\/(?<ItemKey>[^\:\=\s)]+)(?:(?:\:|=){1}(?:\''|\"){0,1}(?<ItemValue>.*?)(?:\''|\"){0,1}(?:(?=\s+\/)|$))?' -Input $paramString -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object {
            if (!$_) {
                continue
            } #Posh v2 issue?
            $paramItemName = ($_.Groups["ItemKey"].Value).Trim()
            $paramItemValue = ($_.Groups["ItemValue"].Value).Trim()
            if (!$paramItemValue -or $paramItemValue -eq '') {
                $paramItemValue = $true
            }

            if ($loggingAllowed) {
                Write-Debug "Adding package param '$paramItemName'='$paramItemValue'"
            }
            $paramHash[$paramItemName] = $paramItemValue
        }
    }

    $paramHash
}

# override Get-PackageParameters in chocolatey-core.extension package
Copy-Item Function:Get-PackageParameters Function:Get-PackageParametersBuiltIn -Force
#Rename-Item Function:Get-PackageParameters Get-PackageParametersBuiltIn
Set-Alias -Name Get-PackageParameters -Value Get-PackageParametersBuiltIn -Scope Global

# SIG # Begin signature block
# MIInKwYJKoZIhvcNAQcCoIInHDCCJxgCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCJfmARIaUvswGV
# 6+CzIBt16rh6a5OszJiGhsZy1V8eaKCCIK4wggWNMIIEdaADAgECAhAOmxiO+dAt
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
# DQEJBDEiBCC/t3WvU8XR8kfxCdc79uk8y0EfG5mDohFJ39bNtFFdJjANBgkqhkiG
# 9w0BAQEFAASCAYDL286K+6Hxug0i05H02Lt/BqWApWCUborYiKNNxAloJR1ZRuen
# FGBx+9i/7arsJrAlFlUIQivk/m7aCIYKY8pLcGXA7bhBA3xjOUK+yBa0DFeZ7WwZ
# PNDlqFvhXQILjh4L8Kv71LLeA+IK9gk9cUkJGwEBilPTZNYTF8JuIjOpYqX5tNBm
# Fs5BYqSwQ+6YtmN0e/WZnWrJqxOeWtanuMh6DOghIEp2DzUYLvhpeZzK/n0jFolW
# T7qq6JK+VJyDSaaQohr81u3rhVlj84MzsNUSyDIQ3i7qbaljcpLOlsAnY0lBGB5z
# 1bw/A10zDykyoONp/yLofhwoF+aNrD5OeH1HsomG/hSZ4lZ0NMJylnF5cnPnPxh0
# sPyiYJHuMDK7F1zytzkIV0TJwALj6nDxtS1boG9YW39g4tyeGKy0FAxEjcpZK6B6
# RVdQU/6QcofUkW8wfhceEN4+kpZEKjsxQ5EaIgcDNNkw8WitY+Oa7638LhnDyEN5
# E5wor2TxBHw/jsihggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQsw
# CQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRp
# Z2lDZXJ0IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENB
# AhAFRK/zlJ0IOaa/2z9f5WEWMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkD
# MQsGCSqGSIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQwNTE2MTYxOTIxWjAvBgkq
# hkiG9w0BCQQxIgQgxC/JlGc5MUB0owctwYf5ERt3fA4lvc/DtREAum1IZvIwDQYJ
# KoZIhvcNAQEBBQAEggIAHo55rDW1QyUER5tgqbMXRMYBhXJTG8HlARo1VKUBswdd
# XkGTkKJD+WYD0iOl0GuJIsMjeqBnTQecTcuY4uznEMSdJnKzenTnVM4duAp0UTpO
# J0hHGx5Q1a2vjiN8rsX3giXxdS1THkjIeR2byU5PYswN9wezGQnDXhB9Sf29jS2Z
# 4eB62BAxquyl2yJd+acDww/Tjd9Qt37VKYciYh/1yNUv5X7Yr2dFIAuknUSsCNQ2
# Vi2FABPeYbJvFAycJAcD7UrVYx4mLmRqoyfZFeO+pgcdqOSJHND5e6VAmo9QCsNJ
# 27YKfkO2E2lSST6wcK8SD0nEeuMB9Rka8liP5P5sen6ULlKTl5gtqPunqZ7hbt7q
# MZA0wy2g04UsS/EZtShC8N6L8aG42tm2Wh53UfD1bo3NKMeLPTAaqSX6L/y4EV7h
# h95E1R0ofKRz9ptupO6eHOGdnX+js/h5A8Za7pqjhnZa95Cl3wt5qTC7sAVetUWI
# aezn7rG2DY1Skn2ZJXEjCgoA1DhxTaxL3p3aOOuHYsIdO+UtJdU0yKnMMSOqEjpj
# ql/ORCmNpxiiZ/QWG+EbDfYrJiENf+wP3Gs8i/dC2RR4CU2L4v2A8G+gr8PXyybU
# SbGdm00AZPYjcxhzjz5K0mYFewPEN7W2Cb01BVuSl3hC0W3O1pW93IiUJuqrseA=
# SIG # End signature block
