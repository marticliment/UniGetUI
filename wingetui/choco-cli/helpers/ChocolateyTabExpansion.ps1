# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2011 - 2017 RealDimensions Software, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Ideas from the Awesome Posh-Git - https://github.com/dahlbyk/posh-git
# Posh-Git License - https://github.com/dahlbyk/posh-git/blob/1941da2472eb668cde2d6a5fc921d5043a024386/LICENSE.txt
# http://www.jeremyskinner.co.uk/2010/03/07/using-git-with-windows-powershell/

$Global:ChocolateyTabSettings = New-Object PSObject -Property @{
    AllCommands = $false
}

$script:choco = "$env:ChocolateyInstall\choco.exe"

function script:chocoCmdOperations($commands, $command, $filter, $currentArguments) {
    $currentOptions = @('zzzz')
    if (-not [string]::IsNullOrWhiteSpace($currentArguments)) {
        $currentOptions = $currentArguments.Trim() -split ' '
    }

    $commands.$command.Replace("  ", " ") -split ' ' |
        Where-Object { $_ -notmatch "^(?:$($currentOptions -join '|' -replace "=", "\="))(?:\S*)\s?$" } |
        Where-Object { $_ -like "$filter*" }
}

$script:chocoCommands = @('-?','search','list','info','install','outdated','upgrade','uninstall','new','pack','push','-h','--help','pin','source','config','feature','apikey','export','help','template','--version')

# ensure these all have a space to start, or they will cause issues
$allcommands = " --debug --verbose --trace --noop --help -? --online --accept-license --confirm --limit-output --no-progress --log-file='' --execution-timeout='' --cache-location='' --proxy='' --proxy-user='' --proxy-password='' --proxy-bypass-list='' --proxy-bypass-on-local --force --no-color --skip-compatibility-checks"

$commandOptions = @{
    list      = "--id-only --pre --exact --by-id-only --id-starts-with --detailed --prerelease --include-programs --source='' --page='' --page-size=''"
    search    = "--id-only --pre --exact --by-id-only --id-starts-with --detailed --approved-only --not-broken --source='' --user='' --password='' --prerelease --include-programs --page='' --page-size='' --order-by-popularity --download-cache-only --disable-package-repository-optimizations"
    info      = "--pre --source='' --user='' --password='' --prerelease --disable-package-repository-optimizations"
    install   = "-y -whatif --pre --version= --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --user='' --password='' --prerelease --forcex86 --not-silent --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --allow-downgrade --force-dependencies --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --stop-on-first-package-failure --disable-package-repository-optimizations --pin"
    pin       = "--name='' --version=''"
    outdated  = "--source='' --user='' --password='' --ignore-pinned --ignore-unfound --pre --prerelease --disable-package-repository-optimizations"
    upgrade   = "-y -whatif --pre --version='' --except='' --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --user='' --password='' --prerelease --forcex86 --not-silent --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --allow-downgrade --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --fail-on-unfound --fail-on-not-installed --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --exclude-prerelease --stop-on-first-package-failure --use-remembered-options --ignore-remembered-options --skip-when-not-installed --install-if-not-installed --disable-package-repository-optimizations --pin"
    uninstall = "-y -whatif --force-dependencies --remove-dependencies --all-versions --source='windowsfeatures' --version= --uninstall-arguments='' --override-arguments --not-silent --params='' --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --use-autouninstaller --skip-autouninstaller --fail-on-autouninstaller --ignore-autouninstaller-failure --stop-on-first-package-failure"
    new       = "--template-name='' --output-directory='' --automaticpackage --version='' --maintainer='' packageversion='' maintainername='' maintainerrepo='' installertype='' url='' url64='' silentargs='' --use-built-in-template"
    pack      = "--version='' --output-directory=''"
    push      = "--source='' --api-key='' --timeout=''"
    source    = "--name='' --source='' --user='' --password='' --priority= --bypass-proxy --allow-self-service"
    config    = "--name='' --value=''"
    feature   = "--name=''"
    apikey    = "--source='' --api-key='' --remove"
    export    = "--include-version-numbers --output-file-path=''"
    template  = "--name=''"
}

$commandOptions['find'] = $commandOptions['search']

try {
    $licenseFile = Get-Item -Path "$env:ChocolateyInstall\license\chocolatey.license.xml" -ErrorAction Stop

    if ($licenseFile) {
        # Add pro-only commands
        $script:chocoCommands = @(
            $script:chocoCommands
            'download'
            'optimize'
        )

        $commandOptions.download = "--internalize --internalize-all-urls --ignore-dependencies --installed-packages --ignore-unfound-packages --resources-location='' --download-location='' --outputdirectory='' --source='' --version='' --prerelease --user='' --password='' --cert='' --certpassword='' --append-use-original-location --recompile --disable-package-repository-optimizations"
        $commandOptions.sync = "--output-directory='' --id='' --package-id=''"
        $commandOptions.optimize = "--deflate-nupkg-only --id=''"

        # Add pro switches to commands that have additional switches on Pro
        $proInstallUpgradeOptions = " --install-directory='' --package-parameters-sensitive='' --max-download-rate='' --install-arguments-sensitive='' --skip-download-cache --use-download-cache --skip-virus-check --virus-check --virus-positives-minimum='' --deflate-package-size --no-deflate-package-size --deflate-nupkg-only"

        $commandOptions.install += $proInstallUpgradeOptions
        $commandOptions.upgrade += $proInstallUpgradeOptions + " --exclude-chocolatey-packages-during-upgrade-all --include-chocolatey-packages-during-upgrade-all"
        $commandOptions.new += " --build-package --use-original-location --keep-remote --url='' --url64='' --checksum='' --checksum64='' --checksumtype='' --pause-on-error"
        $commandOptions.pin += " --note=''"

        # Add Business-only commands and options if the license is a Business or Trial license
        [xml]$xml = Get-Content -Path $licenseFile.FullName -ErrorAction Stop
        $licenseType = $xml.license.type

        if ('Business', 'BusinessTrial' -contains $licenseType) {

            # Add business-only commands
            $script:chocoCommands = @(
                $script:chocoCommands
                'support'
                'sync'
            )

            $commandOptions.list += " --audit"
            $commandOptions.uninstall += " --from-programs-and-features"
            $commandOptions.new += " --file='' --file64='' --from-programs-and-features --remove-architecture-from-name --include-architecture-in-name"

            # Add --use-self-service to commands that support it
            $selfServiceCommands = 'list', 'find', 'search', 'info', 'install', 'upgrade', 'uninstall', 'pin', 'outdated', 'push', 'download', 'sync', 'optimize'
            foreach ($command in $selfServiceCommands) {
                $commandOptions.$command += ' --use-self-service'
            }
        }
    }
}
catch {
    # Remove the error that last occurred from $error so it doesn't cause any
    # issues for users, as we're deliberately ignoring it.
    if ($error.Count -gt 0) {
        $error.RemoveAt(0)
    }
}

foreach ($key in @($commandOptions.Keys)) {
    $commandOptions.$key += $allcommands
}

# Consistent ordering for commands so the added pro commands aren't weirdly out of order
$script:chocoCommands = $script:chocoCommands | Sort-Object -Property { $_ -replace '[^a-z](.*$)', '$1--' }

function script:chocoCommands($filter) {
    $cmdList = @()
    if (-not $global:ChocolateyTabSettings.AllCommands) {
        $cmdList += $script:chocoCommands -like "$filter*"
    }
    else {
        $cmdList += (& $script:choco -h) |
            Where-Object { $_ -match '^  \S.*' } |
            ForEach-Object { $_.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) } |
            Where-Object { $_ -like "$filter*" }
    }

    $cmdList #| sort
}

function script:chocoLocalPackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @(& $script:choco list $filter -r --id-starts-with) | ForEach-Object { $_.Split('|')[0] }
}

function script:chocoLocalPackagesUpgrade($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('all|') + @(& $script:choco list $filter -r --id-starts-with) |
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function script:chocoRemotePackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('packages.config|') + @(& $script:choco search $filter --page='0' --page-size='30' -r --id-starts-with --order-by-popularity) |
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function Get-AliasPattern($exe) {
    $aliases = @($exe) + @(Get-Alias | Where-Object { $_.Definition -eq $exe } | Select-Object -Exp Name)

    "($($aliases -join '|'))"
}

function ChocolateyTabExpansion($lastBlock) {
    switch -regex ($lastBlock -replace "^$(Get-AliasPattern choco) ", "") {

        # Handles uninstall package names
        "^uninstall\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackages $matches['package']
        }

        # Handles install package names
        "^(install)\s+(?<package>[^\.][^-\s]+)$" {
            chocoRemotePackages $matches['package']
        }

        # Handles upgrade / uninstall package names
        "^upgrade\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackagesUpgrade $matches['package']
        }

        # Handles list/search first tab
        "^(list|search|find)\s+(?<subcommand>[^-\s]*)$" {
            @('<filter>', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles new first tab
        "^(new)\s+(?<subcommand>[^-\s]*)$" {
            @('<name>', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pack first tab
        "^(pack)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNuspec>', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles push first tab
        "^(push)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNupkg>', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles source first tab
        "^(source)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'add', 'remove', 'disable', 'enable', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pin first tab
        "^(pin)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'add', 'remove', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles feature first tab
        "^(feature)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'disable', 'enable', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }
        # Handles config first tab
        "^(config)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'get', 'set', 'unset', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles template first tab
        "^(template)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'info', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles more options after others
        "^(?<cmd>$($commandOptions.Keys -join '|'))(?<currentArguments>.*)\s+(?<op>\S*)$" {
            chocoCmdOperations $commandOptions $matches['cmd'] $matches['op'] $matches['currentArguments']
        }

        # Handles choco <cmd> <op>
        "^(?<cmd>$($commandOptions.Keys -join '|'))\s+(?<op>\S*)$" {
            chocoCmdOperations $commandOptions $matches['cmd'] $matches['op']
        }

        # Handles choco <cmd>
        "^(?<cmd>\S*)$" {
            chocoCommands $matches['cmd']
        }
    }
}

$PowerTab_RegisterTabExpansion = if (Get-Module -Name powertab) {
    Get-Command Register-TabExpansion -Module powertab -ErrorAction SilentlyContinue
}
if ($PowerTab_RegisterTabExpansion) {
    & $PowerTab_RegisterTabExpansion "choco" -Type Command {
        param($Context, [ref]$TabExpansionHasOutput, [ref]$QuoteSpaces)  # 1:

        $line = $Context.Line
        $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()
        $TabExpansionHasOutput.Value = $true
        ChocolateyTabExpansion $lastBlock
    }

    return
}

if (Test-Path Function:\TabExpansion) {
    Rename-Item Function:\TabExpansion TabExpansionBackup
}

function TabExpansion($line, $lastWord) {
    $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()

    switch -regex ($lastBlock) {
        # Execute Chocolatey tab completion for all choco-related commands
        "^$(Get-AliasPattern choco) (.*)" {
            ChocolateyTabExpansion $lastBlock
        }

        # Fall back on existing tab expansion
        default {
            if (Test-Path Function:\TabExpansionBackup) {
                TabExpansionBackup $line $lastWord
            }
        }
    }
}

# SIG # Begin signature block
# MIIjfwYJKoZIhvcNAQcCoIIjcDCCI2wCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAZ+UPV8DHIJdiA
# MDJtrwOnsa4WwnFRm9AQBkJgD//6zaCCHXgwggUwMIIEGKADAgECAhAECRgbX9W7
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
# hkiG9w0BCQQxIgQgiQh0TR4rmSUwpAM2HIbP36UM+VRbkU9oPBT6dOroZRUwDQYJ
# KoZIhvcNAQEBBQAEggEAlRSMBmP6PXfGkv5HPlGRnvL/b8ZV2jbJ/xUuo1nbv/XV
# fGzTIXQ4npWrHTNHoz2aZ3OefRQ03VtAdRYveP9901IMYXVRErAEWjUOEI30CkJ5
# eJ4H0Hspyn/F8Z5LXuIYd8BX7Ys5C94hdJlOICZ5+5iXOZ4EX9cd3SIfKazqhg4h
# eK4gbsaub6WUsuHHnkgm/MLMIwb0MUpsKHNfecwn5HXL+hYcjTXgoYx93d+yCuMg
# HRPULMdgrf0fcTaP2ijX5OrimvvKwrMJ4nI8C6/AyBiLysq43QDwhSbkdQxN2Y0c
# ZsPcEIk7cPCDLaTQbsd4F69N2FfOmGiEZagcLToJr6GCAyAwggMcBgkqhkiG9w0B
# CQYxggMNMIIDCQIBATB3MGMxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2Vy
# dCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3RlZCBHNCBSU0E0MDk2IFNI
# QTI1NiBUaW1lU3RhbXBpbmcgQ0ECEAxNaXJLlPo8Kko9KQeAPVowDQYJYIZIAWUD
# BAIBBQCgaTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEP
# Fw0yMzA1MzAxNjQ4NTRaMC8GCSqGSIb3DQEJBDEiBCC+PH/KvCFVutnF5UVexo4T
# xym+Uv5NuPAR8sXCk57wEzANBgkqhkiG9w0BAQEFAASCAgC/WUQ91dlpfjQ5+9Vf
# LazFNeFZKYxyEwvVzTp6ZqNwFW3GVV3xH5MObmHHQcrQJywsNMFIa/bDhOKImZ9O
# fM6MEwSgZsPAmfK8X4+P5gqXHq2e8p2OskL0g7TK3Yk9CEitcQfPB4riDrOlOWvw
# FPxj+fKtjzozzKPhppExbPlsD+NjunsgjxxMS9+WKCBv6JugI9Q6Km5suO+eHPcm
# 5Mbg1UBrERzYgrhF1Sq6LEsUHtfQIlFii5c5K0Kh4Qeem/IoUxVZVU9dtt712V2I
# Ukug2NzSmE3obQFoa2sM/ppBpqdoOf+i3ul0fhvWbeK6NPxijSClnXZjrPW0o7W2
# +FD8VfQP6wieJvctYzgY56V/l3zraeGlpCgVYTc5ZnbxIG7dktcvDHvAJWKKGxtj
# j7ztee7RP3EGnmQ6bJQbja3i//+6kcbo456D+ZFK1qKb0JVb9cmzBr7VtYOtBef6
# zj+tiaefSfjs8mkGPc+xYKfBzPoGnneEnd8LkDTMiIV5AooZLMPrYzJkVRqZmV0D
# qhS31BCyRftKYGAPMMq1tOQl++wtE+uRIsiuRxrTEsDk+7Y6cWSmoJwrNOl91rVo
# UkDrliMH4M3lHe4OKliLo+51cRYwHK8VAP7+FTBSwT3taV5EpcA0YC390N9Tzr7d
# GqIXnfX4WN9OOpWweJyGiAm6Kg==
# SIG # End signature block
