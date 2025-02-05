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

$script:chocoCommands = @('-?','search','list','info','install','outdated','upgrade','uninstall','new','pack','push','-h','--help','pin','source','config','feature','apikey','export','help','template','cache','--version','rule')

# ensure these all have a space to start, or they will cause issues
$allcommands = " --debug --verbose --trace --noop --help -? --online --accept-license --confirm --limit-output --no-progress --log-file='' --execution-timeout='' --cache-location='' --proxy='' --proxy-user='' --proxy-password='' --proxy-bypass-list='' --proxy-bypass-on-local --force --no-color --skip-compatibility-checks --ignore-http-cache"

$commandOptions = @{
    list      = "--id-only --pre --exact --by-id-only --id-starts-with --detailed --prerelease --include-programs --source='' --page='' --page-size=''"
    search    = "--id-only --pre --exact --by-id-only --id-starts-with --detailed --approved-only --not-broken --source='' --user='' --password='' --prerelease --include-programs --page='' --page-size='' --order-by-popularity --download-cache-only --disable-package-repository-optimizations --include-configured-sources"
    info      = "--source='' --local-only --version='' --prerelease --user='' --password='' --cert='' --certpassword='' --disable-package-repository-optimizations --include-configured-sources"
    install   = "-y -whatif --pre --version='' --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --user='' --password='' --prerelease --forcex86 --not-silent --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --allow-downgrade --force-dependencies --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --stop-on-first-package-failure --disable-package-repository-optimizations --pin --include-configured-sources"
    pin       = "--name='' --version=''"
    outdated  = "--source='' --user='' --password='' --ignore-pinned --ignore-unfound --pre --prerelease --disable-package-repository-optimizations --include-configured-sources"
    upgrade   = "-y -whatif --pre --version='' --except='' --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --user='' --password='' --prerelease --forcex86 --not-silent --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --allow-downgrade --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --fail-on-unfound --fail-on-not-installed --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --exclude-prerelease --stop-on-first-package-failure --use-remembered-options --ignore-remembered-options --skip-when-not-installed --install-if-not-installed --disable-package-repository-optimizations --pin --ignore-pinned --include-configured-sources"
    uninstall = "-y -whatif --force-dependencies --remove-dependencies --all-versions --source='windowsfeatures' --version='' --uninstall-arguments='' --override-arguments --not-silent --params='' --package-parameters='' --exit-when-reboot-detected --ignore-detected-reboot --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --use-autouninstaller --skip-autouninstaller --fail-on-autouninstaller --ignore-autouninstaller-failure --stop-on-first-package-failure"
    new       = "--template-name='' --output-directory='' --automaticpackage --version='' --maintainer='' packageversion='' maintainername='' maintainerrepo='' installertype='' url='' url64='' silentargs='' --use-built-in-template"
    pack      = "--version='' --output-directory=''"
    push      = "--source='' --api-key='' --timeout=''"
    source    = "--name='' --source='' --user='' --password='' --priority='' --bypass-proxy --allow-self-service"
    config    = "--name='' --value=''"
    feature   = "--name=''"
    apikey    = "--source='' --api-key='' --remove"
    export    = "--include-version-numbers --output-file-path=''"
    template  = "--name=''"
    cache     = "--expired"
    rule      = "--name=''"
}

$commandOptions['find'] = $commandOptions['search']

$licenseFile = "$env:ChocolateyInstall\license\chocolatey.license.xml"

if (Test-Path $licenseFile) {
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
    [xml]$xml = Get-Content -Path $licenseFile -ErrorAction Stop
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
            @('list', 'get', 'disable', 'enable', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }
        # Handles config first tab
        "^(config)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'get', 'set', 'unset', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles template first tab
        "^(template)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'info', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles cache first tab
        "^(cache)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'remove', '-?') | Where-Object { $_ -like "$($matches['subcommand'])*" }
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

# PowerShell up to v5.x: use a custom TabExpansion function.
if ($PSVersionTable.PSVersion.Major -lt 5) { 
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
}
else { # PowerShell v5+: use the Register-ArgumentCompleter cmdlet (PowerShell no longer calls TabExpansion after 7.4, but this available from 5.x)
    function script:Get-AliasNames($exe) {
        @($exe) + @(Get-Alias | Where-Object { $_.Definition -eq $exe } | Select-Object -Exp Name)
    }
    
    Register-ArgumentCompleter -Native -CommandName (Get-AliasNames choco) -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorColumn)
    
        # NOTE:
        # * The stringified form of $commandAst is the command's own command line (irrespective of
        #   whether other statements are on the same line or whether it is part of a pipeline).
        # * However, trailing whitespace is trimmed in the string representation of $commandAst. 
        #   Therefore, when the actual command line ends in space(s), they must be added back
        #   so that ChocolateyTabExpansion recognizes the start of a new argument.
        $ownCommandLine = [string] $commandAst
        $ownCommandLine = $ownCommandLine.Substring(0, [Math]::Min($ownCommandLine.Length, $cursorColumn))
        $ownCommandLine += ' ' * ($cursorColumn - $ownCommandLine.Length)
    
        ChocolateyTabExpansion $ownCommandLine
    }
}

# SIG # Begin signature block
# MIInJQYJKoZIhvcNAQcCoIInFjCCJxICAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCrrVVd0TkryWd/
# lQnstQWAiB3jLfsm+MkIv62xgjw9RaCCIKgwggWNMIIEdaADAgECAhAOmxiO+dAt
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
# Vzu0nAPthkX0tGFuv2jiJmCG6sivqf6UHedjGzqGVnhOMIIGvDCCBKSgAwIBAgIQ
# C65mvFq6f5WHxvnpBOMzBDANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0
# ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTI0MDkyNjAw
# MDAwMFoXDTM1MTEyNTIzNTk1OVowQjELMAkGA1UEBhMCVVMxETAPBgNVBAoTCERp
# Z2lDZXJ0MSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyNDCCAiIwDQYJ
# KoZIhvcNAQEBBQADggIPADCCAgoCggIBAL5qc5/2lSGrljC6W23mWaO16P2RHxjE
# iDtqmeOlwf0KMCBDEr4IxHRGd7+L660x5XltSVhhK64zi9CeC9B6lUdXM0s71EOc
# Re8+CEJp+3R2O8oo76EO7o5tLuslxdr9Qq82aKcpA9O//X6QE+AcaU/byaCagLD/
# GLoUb35SfWHh43rOH3bpLEx7pZ7avVnpUVmPvkxT8c2a2yC0WMp8hMu60tZR0Cha
# V76Nhnj37DEYTX9ReNZ8hIOYe4jl7/r419CvEYVIrH6sN00yx49boUuumF9i2T8U
# uKGn9966fR5X6kgXj3o5WHhHVO+NBikDO0mlUh902wS/Eeh8F/UFaRp1z5SnROHw
# SJ+QQRZ1fisD8UTVDSupWJNstVkiqLq+ISTdEjJKGjVfIcsgA4l9cbk8Smlzddh4
# EfvFrpVNnes4c16Jidj5XiPVdsn5n10jxmGpxoMc6iPkoaDhi6JjHd5ibfdp5uzI
# Xp4P0wXkgNs+CO/CacBqU0R4k+8h6gYldp4FCMgrXdKWfM4N0u25OEAuEa3Jyidx
# W48jwBqIJqImd93NRxvd1aepSeNeREXAu2xUDEW8aqzFQDYmr9ZONuc2MhTMizch
# NULpUEoA6Vva7b1XCB+1rxvbKmLqfY/M/SdV6mwWTyeVy5Z/JkvMFpnQy5wR14GJ
# cv6dQ4aEKOX5AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0TAQH/
# BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgGBmeBDAEE
# AjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxqII+eyG8w
# HQYDVR0OBBYEFJ9XLAN3DigVkGalY17uT5IfdqBbMFoGA1UdHwRTMFEwT6BNoEuG
# SWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJTQTQw
# OTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGDMIGAMCQG
# CCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYBBQUHMAKG
# TGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJT
# QTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQELBQADggIB
# AD2tHh92mVvjOIQSR9lDkfYR25tOCB3RKE/P09x7gUsmXqt40ouRl3lj+8QioVYq
# 3igpwrPvBmZdrlWBb0HvqT00nFSXgmUrDKNSQqGTdpjHsPy+LaalTW0qVjvUBhcH
# zBMutB6HzeledbDCzFzUy34VarPnvIWrqVogK0qM8gJhh/+qDEAIdO/KkYesLyTV
# OoJ4eTq7gj9UFAL1UruJKlTnCVaM2UeUUW/8z3fvjxhN6hdT98Vr2FYlCS7Mbb4H
# v5swO+aAXxWUm3WpByXtgVQxiBlTVYzqfLDbe9PpBKDBfk+rabTFDZXoUke7zPgt
# d7/fvWTlCs30VAGEsshJmLbJ6ZbQ/xll/HjO9JbNVekBv2Tgem+mLptR7yIrpaid
# RJXrI+UzB6vAlk/8a1u7cIqV0yef4uaZFORNekUgQHTqddmsPCEIYQP7xGxZBIhd
# mm4bhYsVA6G2WgNFYagLDBzpmk9104WQzYuVNsxyoVLObhx3RugaEGru+SojW4dH
# PoWrUhftNpFC5H7QEY7MhKRyrBe7ucykW7eaCuWBsBb4HOKRFVDcrZgdwaSIqMDi
# CLg4D+TPVgKx2EgEdeoHNHT9l3ZDBD+XgbF+23/zBjeCtxz+dL/9NWR6P2eZRi7z
# cEO1xwcdcqJsyz/JceENc2Sg8h3KeFUCS7tpFk7CrDqkMIIG7TCCBNWgAwIBAgIQ
# BNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQGEwJVUzEX
# MBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0IFRydXN0
# ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0ExMB4XDTI0
# MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMxDzANBgNV
# BAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9jb2xhdGV5
# IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdhcmUsIElu
# YzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RVlBBBniCy
# Gy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAGDUPAGhXf
# 0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDrupjshvLmQ
# MSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz2zCt8Cvu
# R3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQghKRoIwfp/
# hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN97++0BApX
# 2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YVnKP2AOVc
# a6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyFH8FVnZZV
# 1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4ICAzCCAf8w
# HwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYEFFpfZUil
# S5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYIKwYBBQUH
# AgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8EBAMCB4Aw
# EwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZNaHR0cDov
# L2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNpZ25pbmdS
# U0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0LmRpZ2lj
# ZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5NlNIQTM4
# NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcwAYYYaHR0
# cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8vY2FjZXJ0
# cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmluZ1JTQTQw
# OTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEBCwUAA4IC
# AQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDHBZP9sDRZ
# sFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGUiA3mKdbV
# it0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T9D8ct/m5
# LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIWv/AHDf8u
# kJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIeM3FdtLst
# n9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4hf/0MqEt
# WSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOaQbKf0Cxd
# xMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+cYcpmI4R1
# 3Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyVb6zGqAaE
# 9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM18zHiN4WA
# 4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDGCBdMwggXPAgEBMH0w
# aTELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEwPwYDVQQD
# EzhEaWdpQ2VydCBUcnVzdGVkIEc0IENvZGUgU2lnbmluZyBSU0E0MDk2IFNIQTM4
# NCAyMDIxIENBMQIQBNI793flHTneCMtwLiiYFTANBglghkgBZQMEAgEFAKCBhDAY
# BgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3
# AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8GCSqGSIb3DQEJBDEi
# BCACwVrD35kTKywCVz7zjbwM/lhq6QqjIzS0Czl9d+q76DANBgkqhkiG9w0BAQEF
# AASCAYCw2061YTLAjaZ1gBIYpmTs57O7Y+mAs0W9QmGgi7/JAESQV69EWtvkG4EW
# eT7k1F0N4CwO96MhakffLcPkUaZFyCDDqLnzUTQ0xbPw+hxNKm2RcpCCTtBcibP6
# jkfYTpRS7SzCd0SAyqkxJ/moBEItQDJebMIUUaFgsW82YVyXYRz55khKS9zBykPj
# W1qxg2j0s9IvxOQIYjb6j8P8M1LMsJqDiNFMHwludh/MYFXZH1ydQWCXypJla9ZU
# MyP8csLQVa+LldIA7luoXbY1RPaCv1wkf7EGUCuYTc/rk5Dx4zg3s3Dp/QvvHMtJ
# B/l0vEX7VYs+jMbGpGcigfQ0DhQ7xrmmZht6ROw70EyigUxVKppf+ZwydoecUwcX
# 3K40gj4FFPwCV7y5TyN0MWCwRP9O4N4of6Bcfa1cioBd6qTXlCbEXZIGVfTjJLS6
# WWojePxIx+Ee0DCcQxwZkDiSE/kLpF0qzq96orQ3utyHVJwsVKOM9Xdjm1we1T0p
# s1Vu6aOhggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0
# IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBAhALrma8
# Wrp/lYfG+ekE4zMEMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkDMQsGCSqG
# SIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjQxMTA0MTkxOTQ2WjAvBgkqhkiG9w0B
# CQQxIgQgP8WSnvRLZUYfjuZAFxkdIQhswpxyr+rtr/LU0oR+DIQwDQYJKoZIhvcN
# AQEBBQAEggIArauuS+N1yx86sAFt/6HemR0XsMsy9dS6ZeKpcPn+S8BsbOT9JIHW
# /lFfNtzb7abHS2Ol4MFkY60fG6qKZ4uOD8pNIRO7sHD044rtPAk7YhTcbJp2GPxJ
# dgLIpdL9tSYDqdvH/LFO95VhOWFmHkqy0maeI2WK9xy7ZLwziDrEz7qMPF+TaQc3
# bGIV8RhYPy1AAfklb4pwKi5+kKPR5lX5qV4933rjpLnugSG1/GMI7mE1L+55KBiD
# PmvW8EwcEKoEezEs5idusmh6F4pMYiyEO8/1rCcRMkbGmLeGnB85X+aHvsgTVq5f
# +YdjSYPRfBAXtiosPAvpHobeM0lfpCoeSU7A7z64gSkW1Fsv2GKTZjzbceNro/bW
# XRchT9BMArT3oHcAePb9wOH3hldkZ3tKuprh0TSUzzcYnCGIhlHE48atpkP/sVZU
# bhucTsVY8RuJyzAA4daAIvZ0xN/ca0V9KdeOsjSdTYw0grcnkiicsmitVYgonvnp
# 8SRjhWU9YUIniJRuUMITTBy2dOYCXYpE0vTPYr+LmZKkdG9SF6H74DBLjQmfSPP/
# x6Wg7ZKia1WDVJezMOH77Lstr5pTs7GdSixphs+LvrCrkmjxYrBZ2EnJXcKHzPOX
# d7unSsvjYZzfmNot9e6W+vCWjV2EWmJNzFS2H01h+6CB5molbvlVZUw=
# SIG # End signature block
