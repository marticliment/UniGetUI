function Install-VisualStudio {
<#
.SYNOPSIS
Installs Visual Studio

.DESCRIPTION
Installs Visual Studio with ability to specify additional features and supply product key.

.PARAMETER PackageName
The name of the VisualStudio package - this is arbitrary.
It's recommended you call it the same as your nuget package id.

.PARAMETER Url
This is the url to download the VS web installer.

.PARAMETER ChecksumSha1
The SHA-1 hash of the VS web installer file.

.EXAMPLE
Install-VisualStudio -PackageName VisualStudio2015Community -Url 'http://download.microsoft.com/download/zzz/vs_community.exe' -ChecksumSha1 'ABCDEF0123456789ABCDEF0123456789ABCDEF12'

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to write to download and install Visual Studio.
This method has no error handling built into it.

.LINK
Install-ChocolateyPackage
#>
    [CmdletBinding()]
    param(
      [string] $PackageName,
      [string] $ApplicationName,
      [string] $Url,
      [string] $Checksum,
      [string] $ChecksumType,
      [ValidateSet('MsiVS2015OrEarlier', 'WillowVS2017OrLater')] [string] $InstallerTechnology,
      [string] $ProgramsAndFeaturesDisplayName = $ApplicationName,
      [string] $VisualStudioYear,
      [string] $Product,
      [bool] $Preview,
      [version] $DesiredProductVersion,
      [hashtable] $DefaultParameterValues
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }
    Write-Debug "Running 'Install-VisualStudio' for $PackageName with ApplicationName:'$ApplicationName' Url:'$Url' Checksum:$Checksum ChecksumType:$ChecksumType InstallerTechnology:'$InstallerTechnology' ProgramsAndFeaturesDisplayName:'$ProgramsAndFeaturesDisplayName' VisualStudioYear:'$VisualStudioYear' Product:'$Product' Preview:'$Preview' DesiredProductVersion:'$DesiredProductVersion'";

    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters -DefaultValues $DefaultParameterValues
    $creatingLayout = $packageParameters.ContainsKey('layout')
    $assumeNewVS2017Installer = $InstallerTechnology -eq 'WillowVS2017OrLater'

    $channelReference = $null
    $productReference = $null
    if ($VisualStudioYear -ne '')
    {
        $channelReference = Get-VSChannelReference -VisualStudioYear $VisualStudioYear -Preview $Preview -PackageParameters $packageParameters
    }
    elseif ($packageParameters.ContainsKey('channelId'))
    {
        # Fallback for old packages, which did not specify VisualStudioYear.
        # The actual year value passed here does not matter, because the function will use the channelId from package parameters.
        $channelReference = Get-VSChannelReference -VisualStudioYear '2017' -Preview $Preview -PackageParameters $packageParameters
    }

    if ($null -ne $channelReference -and $Product -ne '')
    {
        if ($Product -ne '')
        {
            $productReference = Get-VSProductReference -ChannelReference $channelReference -Product $Product -PackageParameters $packageParameters
        }
        elseif ($packageParameters.ContainsKey('productId'))
        {
            # Fallback for old packages, which did not specify VisualStudioYear.
            # The actual product name passed here does not matter, because the function will use the productId from package parameters.
            $productReference = Get-VSProductReference -ChannelReference $channelReference -Product 'Ignored' -PackageParameters $packageParameters
        }
    }

    if (-not $creatingLayout)
    {
        if ($assumeNewVS2017Installer)
        {
            # there is a single Programs and Features entry for all products, so its presence is not enough
            if ($null -ne $productReference)
            {
                $products = Resolve-VSProductInstance -ProductReference $productReference -PackageParameters $packageParameters
                $productsCount = ($products | Measure-Object).Count
                Write-Verbose ("Found {0} installed Visual Studio product(s) with ChannelId = {1} and ProductId = {2}" -f $productsCount, $productReference.ChannelId, $productReference.ProductId)
                if ($productsCount -gt 0)
                {
                    $allowUpdate = -not $packageParameters.ContainsKey('no-update')
                    if ($allowUpdate)
                    {
                        Write-Debug 'Updating existing VS instances is enabled (default)'
                        # The bootstrapper is used for updating (either from layout - indicated via bootstrapperPath, or downloaded from $Url).
                        # That way, users can expect that packages using Install-VisualStudio will always call the bootstrapper
                        # and workload packages will always call the installer, so the users will know which arguments will
                        # be supported in each case.
                        Start-VSModifyOperation `
                            -PackageName $PackageName `
                            -ArgumentList @() `
                            -ChannelReference $channelReference `
                            -ApplicableProducts @($Product) `
                            -OperationTexts @('update', 'updating', 'update') `
                            -Operation 'update' `
                            -DesiredProductVersion $DesiredProductVersion `
                            -PackageParameters $packageParameters `
                            -BootstrapperUrl $Url `
                            -BootstrapperChecksum $Checksum `
                            -BootstrapperChecksumType $ChecksumType `
                            -ProductReference $productReference `
                            -UseBootstrapper `
                            -ProductInstance $products
                    }
                    else
                    {
                        Write-Debug 'Updating existing VS instances is disabled because --no-update was passed in package parameters'
                        Write-Warning "$ApplicationName is already installed. Please use the Visual Studio Installer to modify or repair it."
                    }

                    return
                }
            }
        }
        else
        {
            $uninstallKey = Get-VSUninstallRegistryKey -ApplicationName $ProgramsAndFeaturesDisplayName
            $count = ($uninstallKey | Measure-Object).Count
            if ($count -gt 0)
            {
                Write-Warning "$ApplicationName is already installed. Please use Programs and Features in the Control Panel to modify or repair it."
                return
            }
        }
    }

    $installSourceInfo = Open-VSInstallSource -PackageParameters $packageParameters -Url $Url
    try
    {
        if ($assumeNewVS2017Installer)
        {
            $adminFile = $null
            $logFilePath = $null
        }
        else
        {
            $defaultAdminFile = (Join-Path $Env:ChocolateyPackageFolder 'tools\AdminDeployment.xml')
            Write-Debug "Default AdminFile: $defaultAdminFile"

            $adminFile = Generate-AdminFile -Parameters $packageParameters -DefaultAdminFile $defaultAdminFile -PackageName $PackageName -InstallSourceInfo $installSourceInfo -Url $Url -Checksum $Checksum -ChecksumType $ChecksumType
            Write-Debug "AdminFile: $adminFile"

            Update-AdminFile $packageParameters $adminFile

            $logFilePath = Join-Path $Env:TEMP ('{0}_{1:yyyyMMddHHmmss}.log' -f $PackageName, (Get-Date))
            Write-Debug "Log file path: $logFilePath"
        }

        if ($creatingLayout)
        {
            $layoutPath = $packageParameters['layout']
            Write-Warning "Creating an offline installation source for $PackageName in '$layoutPath'. $PackageName will not be actually installed."
        }

        if ($assumeNewVS2017Installer)
        {
            # Copy channel and product info back to package parameters. This helps packages which use the generic bootstrapper (vs_Setup.exe).
            $packageParameters = $packageParameters.Clone()
            if ($null -ne $channelReference)
            {
                if (-not $packageParameters.ContainsKey('channelId'))
                {
                    $packageParameters['channelId'] = $channelReference.ChannelId
                }

                if (-not $packageParameters.ContainsKey('channelUri') -and -not [string]::IsNullOrEmpty($channelReference.ChannelUri))
                {
                    $packageParameters['channelUri'] = $channelReference.ChannelUri
                }

                if (-not $packageParameters.ContainsKey('installChannelUri') -and -not [string]::IsNullOrEmpty($channelReference.InstallChannelUri))
                {
                    $packageParameters['installChannelUri'] = $channelReference.InstallChannelUri
                }
            }

            if ($null -ne $productReference)
            {
                if (-not $packageParameters.ContainsKey('productId'))
                {
                    $packageParameters['productId'] = $productReference.ProductId
                }
            }

            Assert-VSInstallerUpdated -PackageName $PackageName -PackageParameters $packageParameters -ChannelReference $channelReference -Url $Url -Checksum $Checksum -ChecksumType $ChecksumType -UseInstallChannelUri
        }

        $silentArgs = Generate-InstallArgumentsString -parameters $packageParameters -adminFile $adminFile -logFilePath $logFilePath -assumeNewVS2017Installer:$assumeNewVS2017Installer

        $arguments = @{
            packageName = $PackageName
            silentArgs = $silentArgs
            url = $Url
            checksum = $Checksum
            checksumType = $ChecksumType
            logFilePath = $logFilePath
            assumeNewVS2017Installer = $assumeNewVS2017Installer
            installerFilePath = $installSourceInfo.InstallerFilePath
        }
        $argumentsDump = ($arguments.GetEnumerator() | ForEach-Object { '-{0}:''{1}''' -f $_.Key,"$($_.Value)" }) -join ' '
        Write-Debug "Install-VSChocolateyPackage $argumentsDump"
        Install-VSChocolateyPackage @arguments
    }
    finally
    {
        Close-VSInstallSource -InstallSourceInfo $installSourceInfo
    }

    if ($creatingLayout)
    {
        Write-Warning "An offline installation source for $PackageName has been created in '$layoutPath'."
        $bootstrapperExeName = $Url -split '[/\\]' | Select-Object -Last 1
        if ($bootstrapperExeName -like '*.exe')
        {
            Write-Warning "To install $PackageName using this source, pass '--bootstrapperPath $layoutPath\$bootstrapperExeName' as package parameters."
        }
        Write-Warning 'Installation will now be terminated so that Chocolatey does not register this package as installed, do not be alarmed.'
        Set-PowerShellExitCode -exitCode 814
        throw 'An offline installation source has been created; the software has not been actually installed.'
    }
}
