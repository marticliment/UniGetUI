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

function Install-ChocolateyInstallPackage {
    <#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Installs software into "Programs and Features". Use
Install-ChocolateyPackage when software must be downloaded first.

.DESCRIPTION
This will run an installer (local file) on your machine.

.NOTES
This command will assert UAC/Admin privileges on the machine.

If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the community feed). Otherwise, please use
Install-ChocolateyPackage to download those resources from their
official distribution points.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

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
Pro/Business Editions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the community repository, it is nearly a requirement
for the package to be completely unattended.

When you are using this with an MSI, it will set up the arguments as
follows: `"C:\Full\Path\To\msiexec.exe" /i "$fileFullPath" $silentArgs`,
where `$fileFullPath` is `$file` or `$file64`, depending on what has been
decided to be used.

When you use this with MSU, it is similar to MSI above in that it finds
the right executable to run.

When you use this with executable installers, the `$fileFullPath` will
be `$file` or `$file64` and expects to be a full
path to the file. If the file is in the package, see the parameters for
"File" and "File64" to determine how you can get that path at runtime in
a deterministic way. SilentArgs is everything you call against that
file, as in `"$fileFullPath" $silentArgs"`. An example would be
`"c:\path\setup.exe" /S`, where `$fileFullPath = "c:\path\setup.exe"`
and `$silentArgs = "/S"`.

.PARAMETER File
Full file path to native installer to run. If embedding in the package,
you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

`FileFullPath` is an alias for File.

This can be a 32-bit or 64-bit file. This is mandatory in earlier versions
of Chocolatey, but optional if File64 has been provided.

.PARAMETER File64
Full file path to a 64-bit native installer to run.
If embedding in the package, you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

Provide this when you want to provide both 32-bit and 64-bit
installers or explicitly only a 64-bit installer (which will cause a package
install failure on 32-bit systems).

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

.PARAMETER UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and
only use the ones available with the package.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'INSTALLER_EMBEDDED_IN_PACKAGE'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs

.EXAMPLE
>
$packageArgs = @{
  packageName   = 'bob'
  fileType      = 'exe'
  file          = '\\SHARE_LOCATION\to\INSTALLER_FILE'
  silentArgs    = "/S"
  validExitCodes= @(0)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs


.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart MSIPROPERTY=`"true`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'
$mstFileLocation = Join-Path $toolsDir 'transform.mst'

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
Install-ChocolateyInstallPackage 'bob' 'exe' '/S' "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe"

.EXAMPLE
>
Install-ChocolateyInstallPackage -PackageName 'bob' -FileType 'exe' `
  -SilentArgs '/S' `
  -File "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe" `
  -ValidExitCodes @(0)

.LINK
Install-ChocolateyPackage

.LINK
Uninstall-ChocolateyPackage

.LINK
Get-UninstallRegistryKey

.LINK
Start-ChocolateyProcessAsAdmin
#>
    param(
        [parameter(Mandatory = $true, Position = 0)][string] $packageName,
        [parameter(Mandatory = $false, Position = 1)]
        [alias("installerType", "installType")][string] $fileType = 'exe',
        [parameter(Mandatory = $false, Position = 2)][string[]] $silentArgs = '',
        [alias("fileFullPath")][parameter(Mandatory = $false, Position = 3)][string] $file,
        [alias("fileFullPath64")][parameter(Mandatory = $false)][string] $file64,
        [parameter(Mandatory = $false)] $validExitCodes = @(0),
        [parameter(Mandatory = $false)]
        [alias("useOnlyPackageSilentArgs")][switch] $useOnlyPackageSilentArguments = $false,
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )
    [string]$silentArgs = $silentArgs -join ' '

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    $bitnessMessage = ''
    $fileFullPath = $file
    if ((Get-OSArchitectureWidth 32) -or $env:ChocolateyForceX86 -eq 'true') {
        if (!$file) {
            throw "32-bit installation is not supported for $packageName";
        }
        if ($file64) {
            $bitnessMessage = '32-bit ';
        }
    }
    elseif ($file64) {
        $fileFullPath = $file64
        $bitnessMessage = '64-bit '
    }

    if ($fileFullPath -eq '' -or $fileFullPath -eq $null) {
        throw 'Package parameters incorrect, either File or File64 must be specified.'
    }

    Write-Host "Installing $bitnessMessage$packageName..."

    if ($fileType -eq '' -or $fileType -eq $null) {
        Write-Debug 'No FileType supplied. Using the file extension to determine FileType'
        $fileType = [System.IO.Path]::GetExtension("$fileFullPath").Replace(".", "")
    }

    $installerTypeLower = $fileType.ToLower()
    if ('msi', 'exe', 'msu', 'msp' -notcontains $installerTypeLower) {
        Write-Warning "FileType '$fileType' is unrecognized, using 'exe' instead."
        $fileType = 'exe'
    }

    $env:ChocolateyInstallerType = $fileType

    $additionalInstallArgs = $env:chocolateyInstallArguments;
    if ($additionalInstallArgs -eq $null) {
        $additionalInstallArgs = '';
    }
    else {
        #Use a Regex Or ('|') to do the match, instead of multiple '-or' clauses
        $argPattern = @(
            'INSTALLDIR'
            'TARGETDIR'
            'dir\='
            '\/D\='
        ) -join '|'

        if ($additionalInstallArgs -match $argPattern) {
            @"
Pro / Business supports a single, ubiquitous install directory option.
 Stop the hassle of determining how to pass install directory overrides
 to install arguments for each package / installer type.
 Check out Pro / Business - https://chocolatey.org/compare"
"@ | Write-Warning
        }
    }
    $overrideArguments = $env:chocolateyInstallOverride;

    # remove \chocolatey\chocolatey\
    # might be a slight issue here if the download path is the older
    $silentArgs = $silentArgs -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
    $additionalInstallArgs = $additionalInstallArgs -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
    $updatedFilePath = $fileFullPath -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
    if ([System.IO.File]::Exists($updatedFilePath)) {
        $fileFullPath = $updatedFilePath
    }

    $ignoreFile = $fileFullPath + '.ignore'
    if ($env:ChocolateyInstall -and $ignoreFile -match [System.Text.RegularExpressions.Regex]::Escape($env:ChocolateyInstall)) {
        try {
            '' | Out-File $ignoreFile
        }
        catch {
            Write-Warning "Unable to generate `'$ignoreFile`'"
        }
    }

    $workingDirectory = Get-Location -PSProvider "FileSystem"
    try {
        $workingDirectory = [System.IO.Path]::GetDirectoryName($fileFullPath)
    }
    catch {
        Write-Warning "Unable to set the working directory for installer to location of '$fileFullPath'"
        $workingDirectory = $env:TEMP
    }

    try {
        # make sure any Logger.Logging folder exists
        $pattern = "(?:['`"])([a-zA-Z]\:\\[^'`"]+)(?:[`"'])|([a-zA-Z]\:\\[\S]+)"
        $silentArgs, $additionalInstallArgs |
            ForEach-Object { Select-String $pattern -input $_ -AllMatches } |
            ForEach-Object { $_.Matches } | ForEach-Object {
                $argDirectory = $_.Groups[1]
                if ($argDirectory -eq $null -or $argDirectory -eq '') {
                    continue
                }
                $argDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::GetDirectoryName($argDirectory))
                Write-Debug "Ensuring '$argDirectory' exists"
                if (![System.IO.Directory]::Exists($argDirectory)) {
                    [System.IO.Directory]::CreateDirectory($argDirectory) | Out-Null
                }
            }
    }
    catch {
        Write-Debug "Error ensuring directories exist -  $($_.Exception.Message)"
    }

    if ($fileType -like 'msi') {
        $msiArgs = "/i `"$fileFullPath`""
        $msiArgs = if ($overrideArguments) {
            Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')"
            "$msiArgs $additionalInstallArgs"
        }
        else {
            "$msiArgs $silentArgs $additionalInstallArgs"
        }

        $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msiArgs" "$($env:SystemRoot)\System32\msiexec.exe" -validExitCodes $validExitCodes -workingDirectory $workingDirectory
    }

    if ($fileType -like 'msp') {
        $msiArgs = '/update "{0}"' -f $fileFullPath
        if ($overrideArguments) {
            Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
            $msiArgs = "$msiArgs $additionalInstallArgs";
        }
        else {
            $msiArgs = "$msiArgs $silentArgs $additionalInstallArgs";
        }

        $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msiArgs" "$($env:SystemRoot)\System32\msiexec.exe" -validExitCodes $validExitCodes -workingDirectory $workingDirectory
    }

    if ($fileType -like 'exe') {
        if ($overrideArguments) {
            Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
            $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $fileFullPath -validExitCodes $validExitCodes -workingDirectory $workingDirectory
        }
        else {
            $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $fileFullPath -validExitCodes $validExitCodes -workingDirectory $workingDirectory
        }
    }

    if ($fileType -like 'msu') {
        if ($overrideArguments) {
            Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
            $msuArgs = "`"$fileFullPath`" $additionalInstallArgs"
        }
        else {
            $msuArgs = "`"$fileFullPath`" $silentArgs $additionalInstallArgs"
        }
        $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msuArgs" "$($env:SystemRoot)\System32\wusa.exe" -validExitCodes $validExitCodes -workingDirectory $workingDirectory
    }

    Write-Host "$packageName has been installed."
}

# SIG # Begin signature block
# MIIjgQYJKoZIhvcNAQcCoIIjcjCCI24CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCBhWVer0TXficgY
# McrERN/tnNRcWqSiJpaiVS4xFO+PPqCCHXowggUwMIIEGKADAgECAhAECRgbX9W7
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
# CSqGSIb3DQEJBDEiBCCfbaBMq9g1o5u0u24zDXoBswDA84YRwmE1JFflmwG68DAN
# BgkqhkiG9w0BAQEFAASCAQB4pDqK3PoAPyDH1a268uNJaLJpI+P4gxKMsoj6Bpu/
# BtVw28B8XG7TcR9qQ921Ystzosgkb7LXKpCdc7NiaFD5pYRE+Wc6cS8R4F0iFb6m
# jFsgpQSZrQuVRivib04EKci0s5GORegimpNkWKNvcAMmwT9HKyODxw98rktj+euX
# Fs0FdA+t5mtRC9YkoAfFkSd2z+grt1OHb/bUaWaV9MPJD4tk5PBDZ+aW3gCcsRtS
# lUBdMH5sJr17bJLnbWVWIqYwVkBE6mWrxrvYDLpdRJ+Rvauy1rOWBzRauLb4OpQp
# 84WEL2SAF16zCqjetaeYHQUb8qboyVjw2TPKXSex+IpYoYIDIDCCAxwGCSqGSIb3
# DQEJBjGCAw0wggMJAgEBMHcwYzELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lD
# ZXJ0LCBJbmMuMTswOQYDVQQDEzJEaWdpQ2VydCBUcnVzdGVkIEc0IFJTQTQwOTYg
# U0hBMjU2IFRpbWVTdGFtcGluZyBDQQIQBUSv85SdCDmmv9s/X+VhFjANBglghkgB
# ZQMEAgEFAKBpMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkF
# MQ8XDTIzMDgwODA3MDgzMlowLwYJKoZIhvcNAQkEMSIEIB3VKQ04oTfrz7q/kUPy
# vBktWkucHUP9MRYNFJO+30ONMA0GCSqGSIb3DQEBAQUABIICAATnvI4WcCnZEt7N
# 9O1YLHkiVaNRxBptmqzg/fbruhe7zDH5GUukIN1Dl0EidPa0H9Rr1ah19oT74jX2
# PO5rbMH/uf6ePToocr2KYwTY49wgRjy536L+H1RCtGC0e+if6I4J5IvZ8zflh0Wq
# YblaqFvGQgYPpDlycZVPkgZjPQNkcuy1T90SqaBJCPD5zj+X87D/UdT0k4NyGd9M
# xu+tb4om/JGD6NjrtZnIcNJ7kH48d6N78sJU56oBE4Uxp+MQIGkA453Upcej8JIL
# vharj/fqvi1OMG+jBFoQhJBVIUTvKXRkVEsjvmIIlhouCCHjbnkKmh8q4mPodoOn
# umPWL9A5DlFWHZENLikDiQqWwHOxpNu5z1IfxsSiLv9zEx4PcCTY7rD1zNWALHHB
# ZtI4jEbuD9898f6uZdPaBrSkIpcDBrxEWDcrjYqidsh5axLrOLTdU0FddLtT9R7S
# oB1KiTKFlrWOuIY8vi4pzOZ231BvRo4kmRrHrBwY3XYR0+VCs0VzxbdXg0mgLDIr
# 2lrcW/fArhRZ6szuROu3Nurt57mENOZG2EiV5AG87WA1pjGJfMzLWlSMx8QtMY5u
# qKpQ4/KMiuHFbHtoFW+VJzKqiXIZgD84CT9XAjE9qTMKN8gueE/rl3D/DqxQSLy6
# BZ7QulFK6tGvGrfSEDfdzEzhH+X/
# SIG # End signature block
