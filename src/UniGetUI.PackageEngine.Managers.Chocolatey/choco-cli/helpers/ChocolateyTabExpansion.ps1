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

$script:chocoCommands = @('apikey','cache','config','export','feature','help','info','install','license','list','new','outdated','pack','pin','push','rule','search','source','support','template','uninstall','upgrade','--help','--version')

# ensure these all have a space to start, or they will cause issues
$allCommands = " --accept-license --cache-location='' --debug --fail-on-standard-error --force --help --ignore-http-cache --include-headers --limit-output --log-file='' --no-color --no-progress --noop --online --proxy='' --proxy-bypass-list='' --proxy-bypass-on-local --proxy-password='' --proxy-user='' --skip-compatibility-checks --timeout='' --trace --use-system-powershell --verbose --yes"

$commandOptions = @{
    apikey    = "--api-key='' --source=''"
    cache     = "--expired"
    config    = "--name='' --value=''"
    export    = "--include-version --output-file-path=''"
    feature   = "--name=''"
    info      = "--cert='' --certpassword='' --disable-repository-optimizations --include-configured-sources --local-only --password='' --prerelease --source='' --user='' --version=''"
    install   = "--allow-downgrade --allow-empty-checksums --allow-empty-checksums-secure --apply-args-to-dependencies --apply-package-parameters-to-dependencies --cert='' --certpassword='' --disable-repository-optimizations --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --download-checksum-type-x64='' --exit-when-reboot-detected --force-dependencies --forcex86 --ignore-checksum --ignore-dependencies --ignore-detected-reboot --ignore-package-exit-codes --include-configured-sources --install-arguments='' --not-silent --override-arguments --package-parameters='' --password='' --pin --prerelease --require-checksums --skip-hooks --skip-scripts --source='' --stop-on-first-failure --use-package-exit-codes --user='' --version=''"
    license   = ""
    list      = "--by-id-only --by-tag-only --detail --exact --id-only --id-starts-with --ignore-pinned --include-programs --page='' --page-size='' --prerelease --source='' --version=''"
    new       = "--automaticpackage --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --maintainer='' --name='' --output-directory='' --template='' --use-built-in-template --version=''"
    outdated  = "--cert='' --certpassword='' --disable-repository-optimizations --ignore-pinned --ignore-unfound --include-configured-sources --password='' --prerelease --source='' --user=''"
    pack      = "--output-directory='' --version=''"
    pin       = "--name='' --version=''"
    push      = "--api-key='' --source=''"
    rule      = "--name=''"
    search    = "--all-versions --approved-only --by-id-only --by-tag-only --cert='' --certpassword='' --detail --disable-repository-optimizations --download-cache-only --exact --id-only --id-starts-with --include-configured-sources --include-programs --not-broken --order-by='' --order-by-popularity --page='' --page-size='' --password='' --prerelease --source='' --user=''"
    source    = "--admin-only --allow-self-service --bypass-proxy --cert='' --certpassword='' --name='' --password='' --priority='' --source='' --user=''"
    support   = ""
    template  = "--name=''"
    uninstall = "--all-versions --apply-args-to-dependencies --apply-package-parameters-to-dependencies --exit-when-reboot-detected --fail-on-autouninstaller --force-dependencies --ignore-autouninstaller-failure --ignore-detected-reboot --ignore-package-exit-codes --not-silent --override-arguments --package-parameters='' --skip-autouninstaller --skip-hooks --skip-scripts --source='' --stop-on-first-failure --uninstall-arguments='' --use-autouninstaller --use-package-exit-codes --version=''"
    upgrade   = "--allow-downgrade --allow-empty-checksums --allow-empty-checksums-secure --apply-args-to-dependencies --apply-package-parameters-to-dependencies --cert='' --certpassword='' --disable-repository-optimizations --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --download-checksum-type-x64='' --except='' --exclude-prerelease --exit-when-reboot-detected --fail-on-not-installed --fail-on-unfound --forcex86 --ignore-checksums --ignore-dependencies --ignore-detected-reboot --ignore-package-exit-codes --ignore-pinned --ignore-remembered-arguments --ignore-unfound --include-configured-sources --install-arguments='' --install-if-not-installed --not-silent --override-arguments --package-parameters='' --password='' --pin --prerelease --require-checksums --skip-hooks --skip-if-not-installed --skip-scripts --source='' --stop-on-first-failure --use-package-exit-codes --use-remembered-arguments --user='' --version=''"
}

$licenseFile = "$env:ChocolateyInstall\license\chocolatey.license.xml"

if (Test-Path $licenseFile) {
    # Add pro-only commands
    $script:chocoCommands = @(
        $script:chocoCommands
        'download'
        'optimize'
    )

    $commandOptions.download = "--append-use-original-location --cert='' --certpassword='' --disable-repository-optimizations --download-location='' --ignore-dependencies --ignore-unfound --installed-packages --internalize --internalize-all-urls --output-directory='' --password='' --prerelease --resources-location='' --skip-download-cache --skip-virus-check --source='' --use-download-cache --user='' --version='' --virus-check --virus-positives-minimum=''"
    $commandOptions.optimize = "--id='' --reduce-nupkg-only"

    # Add pro switches to commands that have additional switches on Pro
    $proInstallUpgradeOptions = " --install-arguments-sensitive='' --install-directory='' --max-download-bits-per-second='' --no-reduce-package-size --package-parameters-sensitive='' --reason='' --reduce-package-size --reduce-nupkg-only --skip-download-cache --skip-virus-check --use-download-cache --virus-check --virus-positives-minimum=''"

    $commandOptions.install += $proInstallUpgradeOptions
    $commandOptions.new += " --build-package --pause-on-error --use-original-location"
    $commandOptions.pin += " --reason=''"
    $commandOptions.upgrade += $proInstallUpgradeOptions + " --exclude-chocolatey-packages-during-upgrade-all --include-chocolatey-packages-during-upgrade-all"

    # Add Business-only commands and options if the license is a Business or Trial license
    [xml]$xml = Get-Content -Path $licenseFile -ErrorAction Stop
    $licenseType = $xml.license.type

    if ('Business', 'BusinessTrial' -contains $licenseType) {

        # Add business-only commands
        $script:chocoCommands = @(
            $script:chocoCommands
            'convert'
            'sync'
        )

        $commandOptions.convert = "--ignore-dependencies --include-all --to-format=''"
        $commandOptions.list += " --show-audit"
        $commandOptions.new += " --file='' --file64='' --from-programs-and-features --include-architecture-in-name --remove-architecture-from-name --url='' --url64=''"
        $commandOptions.push += " --client-code='' --endpoint='' redirect-url='' --skip-cleanup"
        $commandOptions.sync = "--id='' --output-directory='' --package-id=''"
        $commandOptions.uninstall += " --from-programs-and-features"

        # Add --use-self-service to commands that support it
        $selfServiceCommands = 'download', 'info', 'install', 'list', 'optimize', 'outdated', 'pin', 'push', 'search', 'sync', 'uninstall', 'upgrade'
        foreach ($command in $selfServiceCommands) {
            $commandOptions.$command += ' --use-self-service'
        }
    }
}

foreach ($key in @($commandOptions.Keys)) {
    $commandOptions.$key = ($commandOptions.$key + $allCommands).Trim()
}

# Consistent ordering for commands so the added pro commands aren't weirdly out of order
$script:chocoCommands = $script:chocoCommands | Sort-Object -Property { $_ -replace '[^a-z](.*$)', '$1--' }

function script:chocoCommands($filter) {
    $cmdList = @()
    if (-not $global:ChocolateyTabSettings.AllCommands) {
        $cmdList += $script:chocoCommands -like "$filter*"
    }
    else {
        $cmdList += (& $script:choco --help) |
            Where-Object { $_ -match '^  \S.*' } |
            ForEach-Object { $_.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) } |
            Where-Object { $_ -like "$filter*" }
    }

    $cmdList #| sort
}

function script:chocoApiKeysList() {
    @(& $script:choco apikey list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Source
}

function script:chocoConfigsList() {
    @(& $script:choco config list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Name
}

function script:chocoFeaturesList() {
    @(& $script:choco feature list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Name
}

function script:chocoLocalNonPinnedPackages() {
    @(& $script:choco list --limit-output --ignore-pinned --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
}

function script:chocoLocalPackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @(& $script:choco list $filter --limit-output --id-starts-with --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
}

function script:chocoLocalPackagesUpgrade($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('all|') + @(& $script:choco list $filter --limit-output --id-starts-with --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function script:chocoLocalPinnedPackages() {
    @(& $script:choco pin list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
}

function script:chocoRemotePackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('packages.config|') + @(& $script:choco search $filter --page='0' --page-size='30' --limit-output --id-starts-with --include-headers --order-by='popularity') |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function script:chocoRemotePackageVersions($Name, $Version) {
        $packageVersions = & $script:choco search $Name --exact --all-versions --limit-output --include-headers --order-by='LastPublished' |
            ConvertFrom-Csv -Delimiter '|'

        if ($Version -and $packageVersions) {
            $packageVersions | Where-Object Version -Like "$Version*" | Select-Object -ExpandProperty Version -First 30
        } else {
            $packageVersions | Select-Object -ExpandProperty Version -First 30
        }
}

function script:chocoRulesList() {
    @(& $script:choco rule list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Id
}

function script:chocoSourcesList() {
    @(& $script:choco source list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Name
}

function script:chocoTemplatesList() {
    @(& $script:choco template list --limit-output --include-headers) |
        ConvertFrom-Csv -Delimiter '|' |
        Select-Object -ExpandProperty Name
}

function Get-AliasPattern($exe) {
    $aliases = @($exe) + @(Get-Alias | Where-Object { $_.Definition -eq $exe } | Select-Object -Exp Name)

    "($($aliases -join '|'))"
}

function Get-ChocoOrderByOptions {
    <#
        .SYNOPSIS
        Returns the list of canonical --order-by values for Chocolatey.

        .DESCRIPTION
        These values correspond to the distinct, non-aliased entries in the
        PackageOrder enum. They are sorted alphabetically and must be updated
        manually when the enum changes.

        .OUTPUTS
        A string in the format "Id|LastPublished|Popularity|Title|Unsorted"
    #>
    return @("Id", "LastPublished", "Popularity", "Title", "Unsorted")
}

function ChocolateyTabExpansion($lastBlock) {
    switch -regex ($lastBlock -replace "^$(Get-AliasPattern choco) ", "") {

        # Handles apikey first tab
        "^(apikey)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco apikey remove
        "^apikey remove.*--source='?(?<source>.*)'?$" {
            $name = $matches['source']
            return chocoApiKeysList | Where-Object { $_ -like "*$source*" } | ForEach-Object { "--source='$_'"}
        }

        # Handles cache first tab
        "^(cache)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles config first tab
        "^(config)\s+(?<subcommand>[^-\s]*)$" {
            @('get', 'list', 'set', 'unset', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco config get/set/unset
        "^config (get|set|unset).*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoConfigsList | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles feature first tab
        "^(feature)\s+(?<subcommand>[^-\s]*)$" {
            @('disable', 'enable', 'get', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco feature get/enable/disable
        "^feature (get|disable|enable).*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoFeaturesList | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles install/upgrade package versions for a specific package
        "^(install|upgrade)\s+(?<package>[^\.\-][^\s]+).*--version='?(?<version>[^\s']*)'?$" {
            chocoRemotePackageVersions -Name $matches['package'] -Version $matches['version'] | ForEach-Object { "--version='$_'" }
        }

        # Handles install package names
        "^(install)\s+(?<package>[^\.][^-\s]+)$" {
            chocoRemotePackages $matches['package']
        }

        # Handles license first tab
        "^(license)\s+(?<subcommand>[^-\s]*)$" {
            @('info', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles list first tab
        "^(list)\s+(?<subcommand>[^-\s]*)$" {
            @('<filter>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles new first tab
        "^(new)\s+(?<subcommand>[^-\s]*)$" {
            @('<name>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pack first tab
        "^(pack)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNuspec>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pin first tab
        "^(pin)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco pin add
        "^pin add.*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoLocalNonPinnedPackages | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Custom completion choco pin remove
        "^pin remove.*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoLocalPinnedPackages | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles push first tab
        "^(push)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNupkg>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles rule first tab
        "^(rule)\s+(?<subcommand>[^-\s]*)$" {
            @('get', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco rule get
        "^rule get.*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoRulesList | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles search first tab
        "^(search)\s+(?<subcommand>[^-\s]*)$" {
            @('<filter>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles source first tab
        "^(source)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'disable', 'enable', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco source disable/enable/remove
        "^source (disable|enable|remove).*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoSourcesList | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles template first tab
        "^(template)\s+(?<subcommand>[^-\s]*)$" {
            @('info', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Custom completion choco template info
        "^template info.*--name='?(?<name>.*)'?$" {
            $name = $matches['name']
            return chocoTemplatesList | Where-Object { $_ -like "*$name*" } | ForEach-Object { "--name='$_'"}
        }

        # Handles uninstall package names
        "^uninstall\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackages $matches['package']
        }

        # Handles upgrade / uninstall package names
        "^upgrade\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackagesUpgrade $matches['package']
        }

        # Custom completion for --order-by values
        "^search.*--order-by='?(?<prefix>.*)'?$" {
            $prefix = $matches['prefix']
            return Get-ChocoOrderByOptions | Where-Object { $_ -like "$prefix*" } | ForEach-Object { "--order-by='$_'"}
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
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCC7CBFgLXOxbhFU
# jelLmAobz/9S4h05UhOX1ThKj1lgzKCCIKgwggWNMIIEdaADAgECAhAOmxiO+dAt
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
# BCDt0pZR9uNdi0kB2NcN8oEb0MJzuaA/+MrfPjBI71PaKzANBgkqhkiG9w0BAQEF
# AASCAYA4cK66HcdQveoHC7i1Eroy7awuEyTl6/0VOXFUcNftjUF5e4rZwLFqi1gc
# 47b5sFaeG0DQkJQCNP/AJD86hAmGFjTN9/BFOvWbW8EG/l94Jofo2rjivPjchSR8
# 1lf4IJR1suzxVlp1hAyy2GKa1musFdYYGr2PECKsq1qeJATNklQDMDIvsRFwUP/3
# BviwhmHpzU1R3dFExGkYrKQK1iK/mbvreYtFoov1YcVkkU5Y3pH0vOjqiJjrVIoH
# cd+S9j36u2Lo9P3KGv4tYqb9/b0oA59jOm7yiRGwQ0gOTkCTU2Dd0pVMgzFKDCyQ
# wdlkzpRKOtExgzYtjuDclXbzA8OLgDliTDXeF1Cq/d1C7NnWX8+DomMuUmHzws7k
# +/kqcCVDmK4F/U5FUINc6ft0J3wJcSveoFE8aN/aktDUnpvT5T9t92FOvnT/Vvba
# j2GaQBTaMSVyZPLwwiSq3a9j9Dfbi6ITzjsF75i5ftVtltvpbumMuSIc6bAW7OhO
# kGj8lK+hggMgMIIDHAYJKoZIhvcNAQkGMYIDDTCCAwkCAQEwdzBjMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0
# IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBAhALrma8
# Wrp/lYfG+ekE4zMEMA0GCWCGSAFlAwQCAQUAoGkwGAYJKoZIhvcNAQkDMQsGCSqG
# SIb3DQEHATAcBgkqhkiG9w0BCQUxDxcNMjUwNzAyMTE0NDAxWjAvBgkqhkiG9w0B
# CQQxIgQg9O3AxexOleseUfc0G/4dx1bimWJ4+wt+21na57bqbeAwDQYJKoZIhvcN
# AQEBBQAEggIASiZtXYYr9sPEo7uWHy05+Jypj/zQOMIl2SlUns0kl2PVTi6RcYQT
# fXBD8v0zxnXmLwYTOuHB9PlzY0VCF4U+2IRNC6tWhCrLgV+nPdGD9d3bguPMdJhF
# uefySJrv/YageXZ1jev8bAF8uGNKs3F8WEYRZP53Knpu92oF88OUlHAy+Qoyq4q1
# JCuBuI+kjX3135YYRs99LdGTzr9uOmWq8G5OAwY8DDuP0916sWpC3rzGTw6U+V5k
# EzN81/dEFsrS2YpzQqoewjqxx3Rvp3DhDXe2Z1kbERibpRYfGFJj86hf5qphVlL+
# gYbxJJlv7nPxKSuwUYsy7Uql9e0L2jPiUwGGI3DjV81CaLDauKoA+pons9N4wHNT
# tuY0MKNuzqB2vhVigsG/wGsQFVG+dQ92AHyZuf2g1VpYYFo2VtaEFTX/r/SwHv2H
# MtIBxrqOo9b/kH2/PrKAfQpFm+s/k+E6b40mSWEYwaydPHDTZb3QkGX4tp+OMXzi
# jk4GfFkXnJAs8akE3WJKfwpOsChvldE/KRvMfv+EsyO+SO+A6wu+qNtBuSz7Q+tm
# BIezlca+KZcTh2ZzmO2XKnPXaZxS7oZ0hjEZ1rKc27kRv+8PLdb/dAcT9d+NqLKw
# YZMaPLXQfH3QyG1Y/KgdTFWaLyazL2L/Ug59DOGW1sF1QXtM9k+9gso=
# SIG # End signature block
