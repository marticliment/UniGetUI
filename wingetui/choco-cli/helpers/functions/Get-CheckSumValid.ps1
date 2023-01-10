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

function Get-ChecksumValid {
<#
.SYNOPSIS
Checks a file's checksum versus a passed checksum and checksum type.

.DESCRIPTION
Makes a determination if a file meets an expected checksum signature.
This function is usually used when comparing a file that is downloaded
from an official distribution point. If the checksum fails to match the
expected output, this function throws an error.

Checksums have been used for years as a means of verification. A
checksum hash is a unique value or signature that corresponds to the
contents of a file. File names and extensions can be altered without
changing the checksum signature. However if you changed the contents of
the file, even one character, the checksum will be different.

Checksums are used to provide as a means of cryptographically ensuring
the contents of a file have not been changed. While some cryptographic
algorithms, including MD5 and SHA1, are no longer considered secure
against attack, the goal of a checksum algorithm is to make it
extremely difficult (near impossible with better algorithms) to alter
the contents of a file (whether by accident or for malicious reasons)
and still result in the same checksum signature.

When verifying a checksum using a secure algorithm, if the checksum
matches the expected signature, the contents of the file are identical
to what is expected.

.NOTES
This uses the checksum.exe tool available separately at
https://community.chocolatey.org/packages/checksum.

Options that affect checksum verification:

* `--ignore-checksums` - skips checksumming
* `--allow-empty-checksums` - skips checksumming when the package is missing a checksum
* `--allow-empty-checksums-secure` - skips checksumming when the package is missing a checksum for secure (HTTPS) locations
* `--require-checksums` - requires checksums for both non-secure and secure locations
* `--download-checksum`, `--download-checksum-type` - allows user to pass their own checksums
* `--download-checksum-x64`, `--download-checksum-type-x64` - allows user to pass their own checksums

Features that affect checksum verification:

* `checksumFiles` - when turned off, skips checksumming
* `allowEmptyChecksums` - when turned on, skips checksumming when the package is missing a checksum
* `allowEmptyChecksumsSecure` - when turned on, skips checksumming when the package is missing a checksum for secure (HTTPS) locations

.INPUTS
None

.OUTPUTS
None

.PARAMETER File
The full path to a binary file that is checksummed and compared to the
passed Checksum parameter value.

.PARAMETER Checksum
The expected checksum hash value of the File resource. The checksum
type is covered by ChecksumType.

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
The type of checksum that the file is validated with - 'md5', 'sha1',
'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Get-ChecksumValid -File $fileFullPath -CheckSum $checksum -ChecksumType $checksumType

.LINK
Get-ChocolateyWebFile

.LINK
Install-ChocolateyPackage
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $file,
  [parameter(Mandatory=$false, Position=1)][string] $checksum = '',
  [parameter(Mandatory=$false, Position=2)][string] $checksumType = 'md5',
  [parameter(Mandatory=$false, Position=3)][string] $originalUrl = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if ($env:ChocolateyIgnoreChecksums -eq 'true') {
    Write-Warning "Ignoring checksums due to feature checksumFiles turned off or option --ignore-checksums set."
    return
  }

  if ($checksum -eq '' -or $checksum -eq $null) {
    $allowEmptyChecksums = $env:ChocolateyAllowEmptyChecksums
    $allowEmptyChecksumsSecure = $env:ChocolateyAllowEmptyChecksumsSecure
    if ($allowEmptyChecksums -eq 'true') {
      Write-Debug "Empty checksums are allowed due to allowEmptyChecksums feature or option."
      return
    }

    if ($originalUrl -ne $null -and $originalUrl.ToLower().StartsWith("https") -and $allowEmptyChecksumsSecure -eq 'true') {
      Write-Debug "Download from HTTPS source with feature 'allowEmptyChecksumsSecure' enabled."
      return
    }

    Write-Warning "Missing package checksums are not allowed (by default for HTTP/FTP, `n HTTPS when feature 'allowEmptyChecksumsSecure' is disabled) for `n safety and security reasons. Although we strongly advise against it, `n if you need this functionality, please set the feature `n 'allowEmptyChecksums' ('choco feature enable -n `n allowEmptyChecksums') `n or pass in the option '--allow-empty-checksums'. You can also pass `n checksums at runtime (recommended). See `choco install -?` for details."
    Write-Debug "If you are a maintainer attempting to determine the checksum for packaging purposes, please run `n 'choco install checksum' and run 'checksum -t sha256 -f $file' `n Ensure you do this for all remote resources."
    if ($PSVersionTable.PSVersion.Major -ge 4){
      Write-Debug "Because you are running PowerShell with a major version of v4 or greater, you could also opt to run `n '(Get-FileHash -Path $file -Algorithm SHA256).Hash' `n rather than install a separate tool."
    }

    if ($env:ChocolateyPowerShellHost -eq 'true') {
      $statement = "The integrity of the file '$([System.IO.Path]::GetFileName($file))'"
      if ($originalUrl -ne $null -and $originalUrl -ne '') {
        $statement += " from '$originalUrl'"
      }
      $statement += " has not been verified by a checksum in the package scripts."
      $question = 'Do you wish to allow the install to continue (not recommended)?'
      $choices = New-Object System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]
      $choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription -ArgumentList '&Yes'))
      $choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription -ArgumentList '&No'))

      $selection = $Host.UI.PromptForChoice($statement, $question, $choices, 1)

      if ($selection -eq 0) { return }
    }

    if ($originalUrl -ne $null -and $originalUrl.ToLower().StartsWith("https")) {
      throw "This package downloads over HTTPS but does not yet have package checksums to verify the package. We recommend asking the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksumsSecure, provide the runtime switch '--allow-empty-checksums-secure', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details)."
    } else {
      throw "Empty checksums are no longer allowed by default for non-secure sources. Please ask the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksums, provide the runtime switch '--allow-empty-checksums', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details). It is strongly advised against allowing empty checksums for non-internal HTTP/FTP sources."
    }
  }

  if (!([System.IO.File]::Exists($file))) { throw "Unable to checksum a file that doesn't exist - Could not find file `'$file`'" }

  if ($checksumType -eq $null -or $checksumType -eq ''){
    $checksumType = 'md5'
  }

  if ($checksumType -ne 'sha1' -and $checksumType -ne 'sha256' -and $checksumType -ne 'sha512' -and $checksumType -ne 'md5') {
    Write-Debug 'Setting checksumType to md5 due to non-set value or type is not specified correctly.'
    throw "Checksum type '$checksumType' is unsupported. This type may be supported in a newer version of Chocolatey."
  }

  $checksumExe = Join-Path "$helpersPath" '..\tools\checksum.exe'
  if (!([System.IO.File]::Exists($checksumExe))) {
    Update-SessionEnvironment
    $checksumExe = Join-Path "$env:ChocolateyInstall" 'tools\checksum.exe'
  }
  Write-Debug "checksum.exe found at `'$checksumExe`'"

  $params = "-c=`"$checksum`" -t=`"$checksumType`" -f=`"$file`""

  Write-Debug "Executing command ['$checksumExe' $params]"
  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($checksumExe, $params)
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden

  $process.Start() | Out-Null
  $process.WaitForExit()
  $exitCode = $process.ExitCode
  $process.Dispose()

  Write-Debug "Command [`'$checksumExe`' $params] exited with `'$exitCode`'."

  if ($exitCode -ne 0) {
    throw "Checksum for '$file' did not meet '$checksum' for checksum type '$checksumType'. Consider passing the actual checksums through with `--checksum --checksum64` once you validate the checksums are appropriate. A less secure option is to pass `--ignore-checksums` if necessary."
  }

  #$fileCheckSumActual = $md5Output.Split(' ')[0]
  # if ($fileCheckSumActual -ne $checkSum) {
  #   throw "CheckSum for `'$file'` did not meet `'$checkSum`'."
  # }
}

# SIG # Begin signature block
# MIIjfwYJKoZIhvcNAQcCoIIjcDCCI2wCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAwa9JoM762ljj4
# LXP3Y/xwfoXIJm56EVwwNCNYTIIXcKCCHXgwggUwMIIEGKADAgECAhAECRgbX9W7
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
# hkiG9w0BCQQxIgQgB+ubPnJpOpVsLqeoOYIx0E5hAZDyK4xr2LAJDLRkUQ0wDQYJ
# KoZIhvcNAQEBBQAEggEAbrENb/PeeyaaQqxgMBv1G/9MDelj8JCswLgki2462M8F
# zKpmksrSyCkdVJI6PM/PKE96Tz/maHAsMCwe90HbMKb1mOdk51WegQBcyOtPpwDS
# 2MSLRJcwSD+7yqdpIj6dqSRkoDKvs+o0KfLxl9nUMGU0V2swaxhk5EMQPUrhcEdW
# cySSDTpu8Ex6qlJLKEUveBAhOZ9mTdm/qUdQtqmpmpJ6+RWpy0wARERxDOPc9hWV
# hF9DOFD1qpCYIqLFdkx487GuONwDyuXIKpJ+Id5esu0tyrdEH1uIUIS6n330AAvR
# Xw634TvM8H3IK8q8cgqZfGaK8vwwQ1BACheLR29SJKGCAyAwggMcBgkqhkiG9w0B
# CQYxggMNMIIDCQIBATB3MGMxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2Vy
# dCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNI
# QTI1NiBUaW1lU3RhbXBpbmcgQ0ECEAxNaXJLlPo8Kko9KQeAPVowDQYJYIZIAWUD
# BAIBBQCgaTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEP
# Fw0yMjEyMDYxOTM5MTRaMC8GCSqGSIb3DQEJBDEiBCBOT5fPfZPoy3q/XpHGTyDs
# 1H2bVnYCd0HdHS0dlZ8muzANBgkqhkiG9w0BAQEFAASCAgChPi/kb2THv8EP0ld4
# A7SXFyr0tueGDEzK6lY8equESJi+bkRshpJJjmRJ80hbCuCBOnPNEcscDYZfARrO
# LRBdi7pA59ljNvf7gvHRxcS9jLe1I5VS0kSJcz8oN2xOb9q0/gP9KuUb2lVB16Nm
# KNFt1CTYv8RK8hR4JmoZsG6zBc81+krtwSLNOTUoP7Rv1SRxXUdd0BCxdPZpxdZY
# bqvao+qvdNM1643FiaisvLQf01m0M+GB5AKf3DthPL1XwEACCkEvdJK+uhLnutQe
# pFmQ+NOSmNv1IJsvLy2k2s6SXCfI4SsY3heXzaKmjp1lh8hxhwXMmlBszWUwSvgu
# WLip9DP910oVGVFy8asZnzXdbdCatCplqkq/I1k8ilkPrRQMqEnWHvmNv7GD1rjQ
# Ev7H4Y5fm6PaKoz0A4wMXOAsMzc8HpEtIZkbKdXvG7CD6A14xfYH2PWVxbxkcVM+
# 0RsnVFIv8NRLS41RsXlASurtZ2D/aKWEXjzB7ZonmP2UzZss9ziqfHv+AgdKQKis
# RjOW3LOh5828HReYPzNAZqfC4wwxo5x0dEdYJQ7hGdDxDjUTbTK7UxXvJMjRCnhl
# ZUONTpSpLNtmM3Yu05ts0tWT2a1kl+qNvAIF0f+l2rVOXGa8GRbHC0HMwU6OX+pN
# bLNrPSqO/7vLbEVyroJ+didBuQ==
# SIG # End signature block
