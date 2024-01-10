<#
.SYNOPSIS
    Installs a Windows update (KB), downloading the appropriate MSU package
    from Microsoft.

.DESCRIPTION
    The function builds on top of the standard Install-ChocolateyPackage
    Chocolatey helper function and provides the following features:
    - detection of operating system version and selection of the appropriate
      download link,
    - detection of the presence of the update (in case it is installed already),
    - verification of the operating system Service Pack version (in case the
      update requires a minimum specific Service Pack number of the given
      operating system),
    - warning the user of the need to reboot the computer (requires Chocolatey
      0.9.10 or later),
    - recognition of update installation result codes to provide descriptive
      error messages (requires Chocolatey 0.9.10 or later),
    - support for -WhatIf and -Confirm common parameters to make testing easier.

    Given an update ID (KBnnnnnnnn) and a set of rules, the function performs
    the following actions:
    - checks whether the update applies to this operating system and exits
      successfully if that is the case;
    - checks whether the update is already installed and exits successfully
      if that is the case;
    - if the update requires a specific minimum Service Pack number on this
      operating system, higher than the present Service Pack number, throws
      an error with a message instructing the user to install the appropriate
      Service Pack, possibly suggesting the appropriate Chocolatey package;
    - proceeds to install the update using the standard
      Install-ChocolateyPackage helper.

.NOTES
    The Windows Update service must not be disabled.

    The update will be downloaded directly from addresses specified in the
    MsuData parameter, bypassing existing Windows Update Agent configuration
    (for example, ignoring the Windows Server Update Services (WSUS) server
    address, if the computer is configured to use one).

    Because some updates become superseded by others, this function might not
    accurately detect that a given update is already present on the system or is
    not required. The function will proceed to download the update file and
    attempt to install it. If the update is not actually needed, the update
    installer will return a specific exit code (WU_E_NOT_APPLICABLE), which the
    function will treat as a success.

    However, the same exit code may be returned when the operating system is
    missing a prerequisite (such as Service Pack 1 on Windows 7 or KB2919355 on
    Windows 8.1). For that reason, when authoring KB packages, it is important
    to accurately specify dependencies on prerequisite KBs and/or pass the
    appropriate service pack requirements to this functions.

    For increased reliability, in Chocolatey package scripts it is advisable
    to call this function using the module-qualified syntax
    (chocolateyInstaller\Install-WindowsUpdate).
    This works around an issue in the Boxstarter framework
    (https://github.com/chocolatey/boxstarter/issues/293).

.PARAMETER Id
    The identifier of the update, in the format "KBnnnnnnnn".

.PARAMETER MsuData
    A collection of URLs and checksums of MSU files for specific operating
    system versions.

    The value of this parameter should be a hashtable.
    The keys are interpreted as two- or three-part operating system version
    numbers, with an optional suffix distinguishing between client and server
    systems.
    The values should be hashtables with keys: Url, Checksum, Url64, Checksum64,
    interpreted according to established Chocolatey practices, and appropriate
    values. One of the pairs (Url+Checksum or Url64+Checksum64) may be missing,
    for example, starting from Server 2008 R2, server systems are 64-bit only.

    The operating system version numbers are:
        6.0 - Windows Vista / Server 2008
        6.1 - Windows 7 / Server 2008 R2
        6.2 - Windows 8 / Server 2012
        6.3 - Windows 8.1 / Server 2012 R2
        10.0 - Windows 10 / Server 2016 (any build)
        10.0.10240 - Windows 10 RTM
        10.0.10586 - Windows 10 1511
        10.0.14393 - Windows 10 1607 / Server 2016
        10.0.15063 - Windows 10 1703
        10.0.16299 - Windows 10 1709 / Server 1709
        10.0.17134 - Windows 10 1803
        10.0.17763 - Windows 10 1809 / Server 2019

    The optional suffixes are "-client" and "-server", for respective operating
    system variants.

    Example keys:
        '6.0-client' = Windows Vista
        '6.2' = Windows 8 / Server 2012

    Example value of MsuData (with abbreviated URLs and checksums):
        @{
            '6.0' = @{
                Url = 'https://download.../.../Windows6.0-KB2533623-x86.msu'
                Url64 = 'https://download.../.../Windows6.0-KB2533623-x64.msu'
                Checksum = '7218...EC01'
                Checksum64 = 'E398...00AA'
            }
            '6.1-client' = @{
                Url = 'https://download.../.../Windows6.1-KB2533623-x86.msu'
                Url64 = 'https://download.../.../Windows6.1-KB2533623-x64.msu'
                Checksum = '43BE...CC41'
                Checksum64 = '58B8...C0D6'
            }
            '6.1-server' = @{
                Url64 = 'https://download.../.../Windows6.1-KB2533623-x64.msu'
                Checksum64 = '58B8...C0D6'
            }
        }

    One easy way of obtaining checksum values is to use the Get-FileHash cmdlet,
    after downloading the MSU files:
        gci *KB2999226* | Get-FileHash -Algorithm SHA256 | ft -a

.PARAMETER ChecksumType
    The checksum algorithm used for all checksums provided in the MsuData
    parameter. The supported algorithms are the same as Chocolatey supports.
    SHA256 is the recommended minimum.

.PARAMETER ServicePackRequirements
    A collection of requisite Service Pack numbers for specific operating
    system versions.

    The value of this parameter should be a hashtable.
    The keys are interpreted as two-part operating system version numbers,
    with an optional suffix distinguishing between client and server systems
    (same as in the MsuData parameter).
    The values should be hashtables with keys:
        ServicePackNumber - the minimum required Service Pack for the given OS;
        ChocolateyPackage - (optional) the ID of the Chocolatey package which
                            can be used to install the required Service Pack
                            on the given OS. Will be displayed as a hint for
                            the user.

    Because the installation of a Service Pack is a major operation, it is
    recommended not to depend on the Chocolatey package for the Service Pack
    directly. Instead, this parameter should be used to enable this function to
    perform the neccessary check and inform the user of the need to install the
    Service Pack.

    Example value of ServicePackRequirements:
        @{
            '6.1' = @{ ServicePackNumber = 1; ChocolateyPackage = 'KB976932' }
        }

.EXAMPLE
    Install-WindowsUpdate -Id KB2533623 -MsuData @{...} -ChecksumType SHA256

    Installs update KB2533623 if it is applicable to this operating system.
#>
function Install-WindowsUpdate
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    Param
    (
        [ValidatePattern('^KB\d+$')] [Parameter(Mandatory = $true)] [string] $Id,
        [Parameter(Mandatory = $true)] [hashtable] $MsuData,
        [Parameter(Mandatory = $true)] [string] $ChecksumType,
        [hashtable] $ServicePackRequirements
    )
    Begin
    {
        Set-StrictMode -Version 2
        $ErrorActionPreference = 'Stop'
    }
    End
    {
        function Get-OS
        {
            if ($null -ne (Get-Command -Name Get-CimInstance -ErrorAction SilentlyContinue)) {
                $wmiOS = Get-CimInstance -ClassName Win32_OperatingSystem
            } else {
                $wmiOS = Get-WmiObject -Class Win32_OperatingSystem
            }

            $version = [Version]$wmiOS.Version
            $caption = $wmiOS.Caption.Trim()
            $sp = $wmiOS.ServicePackMajorVersion
            if ($sp -gt 0) {
                $caption += " Service Pack $sp"
            }

            if ($wmiOS.ProductType -eq '1') {
                $productType = 'client'
            } else {
                $productType = 'server'
            }

            $version3 = $version.ToString(3)
            $version2 = $version.ToString(2)
            $selectors = @(
                ('{0}-{1}' -f $version3, $productType),
                $version3,
                ('{0}-{1}' -f $version2, $productType),
                $version2
            )

            $props = @{
                Version = $version
                Caption = $caption
                ServicePackMajorVersion = $wmiOS.ServicePackMajorVersion
                ProductType = $productType
                Selectors = $selectors
            }

            Write-Verbose "Operating system: $caption, version $version, product type '$productType'"
            return New-Object -TypeName PSObject -Property $props
        }

        function Get-RulesForOS
        {
            [CmdletBinding()]
            Param
            (
                [Parameter(Mandatory = $true)] [object] $OS,
                [Parameter(Mandatory = $true)] [hashtable] $Rules,
                [Parameter(Mandatory = $true)] [string] $RulesDescription
            )
            End
            {
                foreach ($selector in $OS.Selectors)
                {
                    if ($Rules.ContainsKey($selector))
                    {
                        Write-Verbose "Located $RulesDescription rules using selector: $selector"
                        return $Rules[$selector]
                    }
                }

                Write-Verbose "No $RulesDescription rules defined for this operating system"
                return $null
            }
        }

        Write-Verbose 'Obtaining operating system information'
        $os = Get-OS

        Write-Verbose 'Locating MSU rules for this operating system'
        $urlArguments = Get-RulesForOS -OS $os -Rules $MsuData -RulesDescription 'MSU'
        if ($null -eq $urlArguments)
        {
            Write-Host "Skipping installation because update $Id does not apply to this operating system ($($os.Caption))."
            return
        }

        Write-Verbose "Checking if update $Id is already installed"
        if (Test-WindowsUpdate -Id $Id)
        {
            Write-Host "Skipping installation because update $Id is already installed."
            return
        }

        if ($null -ne $ServicePackRequirements)
        {
            Write-Verbose 'Locating Service Pack rules for this operating system'
            $spRules = Get-RulesForOS -OS $os -Rules $ServicePackRequirements -RulesDescription 'Service Pack'
            if ($null -ne $spRules)
            {
                if ($os.ServicePackMajorVersion -lt $spRules.ServicePackNumber)
                {
                    Write-Verbose "The installed Service Pack number ($($os.ServicePackMajorVersion)) is lower than required ($($spRules.ServicePackNumber))."
                    if ($spRules.ContainsKey('ChocolateyPackage') -and -not [string]::IsNullOrEmpty($spRules.ChocolateyPackage))
                    {
                        $hint = ', for example using the {0} package' -f $spRules.ChocolateyPackage
                    }
                    else
                    {
                        $hint = $null
                    }

                    $msg = "To install $Id on $($os.Caption) you must install Service Pack $($spRules.ServicePackNumber) first${hint}."
                    throw $msg
                }
                else
                {
                    Write-Verbose "The installed Service Pack number ($($os.ServicePackMajorVersion)) is sufficient (required: $($spRules.ServicePackNumber))."
                }
            }
            else
            {
                Write-Verbose "No Service Pack requirements are defined for this update on this operating system."
            }
        }

        $ERROR_SUCCESS = 0
        $ERROR_SUCCESS_REBOOT_REQUIRED = 3010
        $WU_E_NOT_APPLICABLE = 0x80240017
        $WU_S_ALREADY_INSTALLED = 0x00240006

        $logPath = '{0}\{1}.Install.evt' -f $Env:TEMP, $Id
        $silentArgs = '/quiet /norestart /log:"{0}"' -f $logPath
        $validExitCodes = @($ERROR_SUCCESS, $ERROR_SUCCESS_REBOOT_REQUIRED, $WU_E_NOT_APPLICABLE, $WU_S_ALREADY_INSTALLED)

        $exitCodeHandler = {
            $installResult = $_
            $exitCode = $installResult.ExitCode
            if ($exitCode -eq $ERROR_SUCCESS_REBOOT_REQUIRED)
            {
                Write-Warning "Update $Id has been installed, but a reboot is required to finalize the installation. Until the computer is rebooted, dependent packages may fail to install or function properly."
            }
            elseif ($exitCode -eq $WU_E_NOT_APPLICABLE)
            {
                Write-Host "Update $Id does not apply to this system. Either it was superseded by another already installed update, or a prerequisite update is missing."
                $installResult.ExitCode = $ERROR_SUCCESS
            }
            elseif ($exitCode -eq $WU_S_ALREADY_INSTALLED)
            {
                Write-Verbose "Update $Id is already installed on this system (probably superseded by another already installed update)."
                $installResult.ExitCode = $ERROR_SUCCESS
            }
            elseif ($exitCode -eq $ERROR_SUCCESS)
            {
                Write-Verbose "Update $Id has been installed successfully, a reboot is not required."
            }
            elseif ($null -eq $exitCode)
            {
                Write-Warning "Update $Id installation has finished, but this Chocolatey version does not provide the installer exit code. Please inform the maintainer of the chocolatey-windowsupdate.extension package."
            }
            else
            {
                $errorDesc = Get-WindowsUpdateErrorDescription -ErrorCode $exitCode
                if ($null -ne $errorDesc)
                {
                    $errorMessage = 'Update {0} installation failed with code 0x{1:X8} ({2}: {3}).' -f $Id, [int]$exitCode, $errorDesc.Name, $errorDesc.Description
                }
                else
                {
                    $errorMessage = "Update $Id installation failed (exit code $exitCode)."
                }

                # Write the error message as a warning so that the hint about log files comes after it. Then let the error message be thrown as an exception.
                Write-Warning $errorMessage
                Write-Warning "More details may be found in the installation log ($logPath) or the system CBS log (${Env:SystemRoot}\Logs\CBS\CBS.log)."
                $installResult.ErrorMessage = $errorMessage
            }
        }

        if ($PSCmdlet.ShouldProcess("Update $Id", 'Download and install'))
        {
            Install-ChocolateyPackageAndHandleExitCode `
                -PackageName $Id `
                -FileType 'msu' `
                -SilentArgs $silentArgs `
                -ValidExitCodes $validExitCodes `
                -ChecksumType $ChecksumType `
                -ChecksumType64 $ChecksumType `
                -ExitCodeHandler $exitCodeHandler `
                @urlArguments
        }
    }
}
