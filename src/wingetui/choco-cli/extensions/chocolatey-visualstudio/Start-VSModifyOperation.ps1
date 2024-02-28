function Start-VSModifyOperation
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [AllowEmptyCollection()] [AllowEmptyString()] [Parameter(Mandatory = $true)] [string[]] $ArgumentList,
        [Parameter(Mandatory = $true)] [PSObject] $ChannelReference,
        [Parameter(Mandatory = $true)] [string[]] $ApplicableProducts,
        [Parameter(Mandatory = $true)] [string[]] $OperationTexts,
        [ValidateSet('modify', 'uninstall', 'update')] [string] $Operation = 'modify',
        [version] $RequiredProductVersion,
        [version] $DesiredProductVersion,
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [string] $BootstrapperUrl,
        [string] $BootstrapperChecksum,
        [string] $BootstrapperChecksumType,
        [PSObject] $ProductReference,
        [switch] $UseBootstrapper,
        [PSObject[]] $ProductInstance
    )
    Write-Debug "Running 'Start-VSModifyOperation' with PackageName:'$PackageName' ArgumentList:'$ArgumentList' ChannelReference:'$ChannelReference' ApplicableProducts:'$ApplicableProducts' OperationTexts:'$OperationTexts' Operation:'$Operation' RequiredProductVersion:'$RequiredProductVersion' BootstrapperUrl:'$BootstrapperUrl' BootstrapperChecksum:'$BootstrapperChecksum' BootstrapperChecksumType:'$BootstrapperChecksumType' ProductReference:'$ProductReference' UseBootstrapper:'$UseBootstrapper'";

    if ($null -eq $ProductReference)
    {
        if ($PackageParameters.ContainsKey('productId'))
        {
            # Workload/component packages do not pass a ProductReference, because they may apply to several products.
            # However, the user can explicitly narrow the operation scope via package parameters.
            # The actual product name passed here does not matter, because the function will use the productId from package parameters.
            $ProductReference = Get-VSProductReference -ChannelReference $channelReference -Product 'Ignored' -PackageParameters $packageParameters
        }
        elseif ($Operation -eq 'update')
        {
            throw 'ProductReference is mandatory for update operations.'
        }
    }

    $frobbed, $frobbing, $frobbage = $OperationTexts

    $PackageParameters = $PackageParameters.Clone()

    $argumentSetFromArgumentList = @{}
    for ($i = 0; $i -lt $ArgumentList.Length; $i += 2)
    {
        $argumentSetFromArgumentList[$ArgumentList[$i]] = $ArgumentList[$i + 1]
    }

    $baseArgumentSet = $argumentSetFromArgumentList.Clone()
    Merge-AdditionalArguments -Arguments $baseArgumentSet -AdditionalArguments $packageParameters
    @('add', 'remove') | Where-Object { $argumentSetFromArgumentList.ContainsKey($_) } | ForEach-Object `
    {
        $value = $argumentSetFromArgumentList[$_]
        $existingValue = $baseArgumentSet[$_]
        if ($existingValue -is [System.Collections.IList])
        {
            if ($existingValue -notcontains $value)
            {
                Write-Debug "Adding back argument '$_' value '$value' (adding to existing list)"
                [void]$existingValue.Add($value)
            }
        }
        else
        {
            if ($existingValue -ne $value)
            {
                Write-Debug "Adding back argument '$_' value '$value' (converting to list)"
                $baseArgumentSet[$_] = New-Object -TypeName System.Collections.Generic.List``1[System.String] -ArgumentList (,[string[]]($existingValue, $value))
            }
        }
    }

    $argumentSets = ,$baseArgumentSet
    if ($baseArgumentSet.ContainsKey('installPath'))
    {
        $installedProducts = Resolve-VSProductInstance -AnyProductAndChannel -PackageParameters $PackageParameters
        if (($installedProducts | Measure-Object).Count -gt 0)
        {
            # Should be only one, but it is not guaranteed, hence the loop.
            foreach ($productInfo in $installedProducts)
            {
                if ($productInfo.channelId -ne $ChannelReference.ChannelId)
                {
                    Write-Warning "Product at path '$($productInfo.installationPath)' has channel id '$($productInfo.channelId)', expected '$($ChannelReference.ChannelId)'."
                }

                if ($null -ne $ProductReference -and $productInfo.productId -ne $ProductReference.ProductId)
                {
                    Write-Warning "Product at path '$($productInfo.installationPath)' has product id '$($productInfo.productId)', expected '$($ProductReference.ProductId)'."
                }

                $baseArgumentSet['__internal_productReference'] = New-VSProductReference -ChannelId $productInfo.channelId -ProductId $productInfo.productId -ChannelUri $productInfo.channelUri -InstallChannelUri $productInfo.installChannelUri
            }
        }
        else
        {
            Write-Warning "Did not detect any installed Visual Studio products at path $($baseArgumentSet['installPath'])."
        }
    }
    else
    {
        if (($ProductInstance | Measure-Object).Count -ne 0)
        {
            $installedProducts = $ProductInstance
        }
        else
        {
            if ($null -ne $ProductReference)
            {
                $installedProducts = Resolve-VSProductInstance -ProductReference $ProductReference -PackageParameters $PackageParameters
            }
            else
            {
                $installedProducts = Resolve-VSProductInstance -ChannelReference $ChannelReference -PackageParameters $PackageParameters
            }

            if (($installedProducts | Measure-Object).Count -eq 0)
            {
                throw "Unable to detect any supported Visual Studio product. You may try passing --installPath or both --productId and --channelId parameters."
            }
        }

        if ($Operation -eq 'modify')
        {
            # The VS instance filtering logic should be based on the primary operation,
            # i.e. 'add' for Add-VisualStudio* and 'remove' for Remove-VisualStudio*.
            # This can be extrapolated from ArgumentList, which is only set by those cmdlets, so trustworthy.
            $addArgumentIsPresent = $ArgumentList -contains 'add'
            $removeArgumentIsPresent = $ArgumentList -contains 'remove'
            if ($addArgumentIsPresent -and $removeArgumentIsPresent)
            {
                throw "Unsupported scenario: both 'add' and 'remove' are present in ArgumentList"
            }
            elseif ($addArgumentIsPresent)
            {
                $packageIdsList = $baseArgumentSet['add']
                $unwantedPackageSelector = { $productInfo.selectedPackages.ContainsKey($_) }
                $unwantedStateDescription = 'contains'
            }
            elseif ($removeArgumentIsPresent)
            {
                $packageIdsList = $baseArgumentSet['remove']
                $unwantedPackageSelector = { -not $productInfo.selectedPackages.ContainsKey($_) }
                $unwantedStateDescription = 'does not contain'
            }
            else
            {
                throw "Unsupported scenario: neither 'add' nor 'remove' is present in ArgumentList"
            }
        }
        elseif (@('uninstall', 'update') -contains $Operation)
        {
            $packageIdsList = ''
            $unwantedPackageSelector = { $false }
            $unwantedStateDescription = '<unused>'
        }
        else
        {
            throw "Unsupported Operation: $Operation"
        }

        $packageIds = ($packageIdsList -split ' ') | ForEach-Object { $_ -split ';' | Select-Object -First 1 }
        $applicableProductIds = $ApplicableProducts | ForEach-Object { "Microsoft.VisualStudio.Product.$_" }
        Write-Debug ('This package supports Visual Studio product id(s): {0}' -f ($applicableProductIds -join ' '))

        $argumentSets = @()
        foreach ($productInfo in $installedProducts)
        {
            $applicable = $false
            $thisProductIds = $productInfo.selectedPackages.Keys | Where-Object { $_ -like 'Microsoft.VisualStudio.Product.*' }
            Write-Debug ('Product at path ''{0}'' has product id(s): {1}' -f $productInfo.installationPath, ($thisProductIds -join ' '))
            foreach ($thisProductId in $thisProductIds)
            {
                if ($applicableProductIds -contains $thisProductId)
                {
                    $applicable = $true
                }
            }

            if (-not $applicable)
            {
                if (($packageIds | Measure-Object).Count -gt 0)
                {
                    Write-Verbose ('Product at path ''{0}'' will not be modified because it does not support package(s): {1}' -f $productInfo.installationPath, $packageIds)
                }
                else
                {
                    Write-Verbose ('Product at path ''{0}'' will not be modified because it is not present on the list of applicable products: {1}' -f $productInfo.installationPath, $ApplicableProducts)
                }

                continue
            }

            $unwantedPackages = $packageIds | Where-Object $unwantedPackageSelector
            if (($unwantedPackages | Measure-Object).Count -gt 0)
            {
                Write-Verbose ('Product at path ''{0}'' will not be modified because it already {1} package(s): {2}' -f $productInfo.installationPath, $unwantedStateDescription, ($unwantedPackages -join ' '))
                continue
            }

            $existingProductVersion = [version]$productInfo.installationVersion
            if ($null -ne $RequiredProductVersion)
            {
                if ($existingProductVersion -lt $RequiredProductVersion)
                {
                    throw ('Product at path ''{0}'' will not be modified because its version ({1}) is lower than the required minimum ({2}). Please update the product first and reinstall this package.' -f $productInfo.installationPath, $existingProductVersion, $RequiredProductVersion)
                }
                else
                {
                    Write-Verbose ('Product at path ''{0}'' will be modified because its version ({1}) satisfies the version requirement of {2} or higher.' -f $productInfo.installationPath, $existingProductVersion, $RequiredProductVersion)
                }
            }

            if ($Operation -eq 'update')
            {
                if ($null -eq $DesiredProductVersion)
                {
                    $firstProductId = $thisProductIds | Select-Object -First 1
                    Write-Verbose "DesiredProductVersion is not set, trying to obtain it from the channel manifest using product id $firstProductId"
                    $DesiredProductVersion = Get-VSProductVersionFromChannelManifest -ProductId $firstProductId -PackageParameters $PackageParameters -ChannelReference $ChannelReference
                    if ($null -ne $DesiredProductVersion)
                    {
                        Write-Verbose "Determined DesiredProductVersion from the channel manifest: $DesiredProductVersion"
                    }
                    else
                    {
                        Write-Verbose "Unable to determine DesiredProductVersion from the channel manifest. The script will not be able to determine the need for the update and to verify the update executed successfully."
                    }
                }

                if ($null -ne $DesiredProductVersion)
                {
                    if ($DesiredProductVersion -le $existingProductVersion)
                    {
                        Write-Verbose ('Product at path ''{0}'' will not be updated because its version ({1}) is greater than or equal to the desired version of {2}.' -f $productInfo.installationPath, $existingProductVersion, $DesiredProductVersion)
                        continue
                    }
                    else
                    {
                        Write-Debug ('Product at path ''{0}'' will be updated because its version ({1}) is lower than the desired version of {2}.' -f $productInfo.installationPath, $existingProductVersion, $DesiredProductVersion)
                    }
                }
            }

            $argumentSet = $baseArgumentSet.Clone()
            $argumentSet['installPath'] = $productInfo.installationPath
            $argumentSet['__internal_productReference'] = New-VSProductReference -ChannelId $productInfo.channelId -ProductId $productInfo.productId -ChannelUri $productInfo.channelUri -InstallChannelUri $productInfo.installChannelUri
            $argumentSets += $argumentSet
        }
    }

    $layoutPath = Resolve-VSLayoutPath -PackageParameters $baseArgumentSet
    $nativeInstallerPath = $null
    if ($UseBootstrapper)
    {
        $nativeInstallerDescription = 'VS Bootstrapper'
        $nativeInstallerArgumentBlacklist = @('bootstrapperPath', 'layoutPath')
        $layoutPathArgumentName = 'installLayoutPath'
        if ($baseArgumentSet.ContainsKey('bootstrapperPath'))
        {
            $nativeInstallerPath = $baseArgumentSet['bootstrapperPath']
            Write-Debug "Using bootstrapper path from package parameters: $nativeInstallerPath"
        }
        elseif ($BootstrapperUrl -ne '')
        {
            Write-Debug "Using bootstrapper url: $BootstrapperUrl"
        }
        else
        {
            throw 'When -UseBootstrapper is specified, BootstrapperUrl must not be empty and/or package parameters must contain bootstrapperPath'
        }
    }
    else
    {
        $nativeInstallerDescription = 'VS Installer'
        $nativeInstallerArgumentBlacklist = @('bootstrapperPath', 'installLayoutPath', 'wait')
        $layoutPathArgumentName = 'layoutPath'
    }

    Write-Debug "The $nativeInstallerDescription will be used as the native installer"

    $installer = $null
    $installerUpdated = $false
    $channelCacheCleared = $false
    $overallExitCode = 0
    foreach ($argumentSet in $argumentSets)
    {
        # installPath should always be present
        $productDescription = "Visual Studio product: [installPath = '$($argumentSet.installPath)']"
        Write-Debug "Modifying $productDescription"

        $thisProductReference = $ProductReference
        if ($argumentSet.ContainsKey('__internal_productReference'))
        {
            $thisProductReference = $argumentSet['__internal_productReference']
            $argumentSet.Remove('__internal_productReference')
        }

        $thisChannelReference = $ChannelReference
        if ($null -ne $thisProductReference)
        {
            $thisChannelReference = Convert-VSProductReferenceToChannelReference -ProductReference $thisProductReference
        }

        $shouldFixInstaller = $false
        if ($null -eq $installer)
        {
            $installer = Get-VisualStudioInstaller
            if ($null -eq $installer)
            {
                $shouldFixInstaller = $true
            }
            else
            {
                $health = $installer | Get-VisualStudioInstallerHealth
                $shouldFixInstaller = -not $health.IsHealthy
            }
        }

        if ($shouldFixInstaller -or ($Operation -ne 'uninstall' -and -not $installerUpdated))
        {
            if ($Operation -ne 'update' -and $argumentSet.ContainsKey('noWeb'))
            {
                Write-Debug 'InstallChannelUri will be used for VS Installer update because operation is not "update" and --noWeb was passed in package parameters'
                $useInstallChannelUri = $true
            }
            else
            {
                Write-Debug 'InstallChannelUri will not be used for VS Installer update because either operation is "update" or --noWeb was not passed in package parameters'
                $useInstallChannelUri = $false
            }

            if ($PSCmdlet.ShouldProcess("Visual Studio Installer", "update"))
            {
                Assert-VSInstallerUpdated -PackageName $PackageName -PackageParameters $PackageParameters -ChannelReference $thisChannelReference -Url $BootstrapperUrl -Checksum $BootstrapperChecksum -ChecksumType $BootstrapperChecksumType -UseInstallChannelUri:$useInstallChannelUri
                $installerUpdated = $true
                $shouldFixInstaller = $false
                $installer = Get-VisualStudioInstaller
            }
        }

        if (-not $UseBootstrapper)
        {
            if ($null -eq $installer)
            {
                throw 'The Visual Studio Installer is not present. Unable to continue.'
            }
            else
            {
                $nativeInstallerPath = $installer.Path
            }
        }

        if ($Operation -ne 'uninstall' -and -not $channelCacheCleared)
        {
            # This works around concurrency issues in some VS Installer versions (1.14.x),
            # which lead to product updates not being detected
            # due to the VS Installer failing to update the cached manifests (file in use).
            if ($PSCmdlet.ShouldProcess("Visual Studio Installer channel cache", "clear"))
            {
                Clear-VSChannelCache
                $channelCacheCleared = $true
            }
        }

        # if updating/modifying from layout, auto add --layoutPath (VS Installer) or --installLayoutPath (VS Bootstrapper)
        if (-not $argumentSet.ContainsKey($layoutPathArgumentName))
        {
            if ($null -ne $layoutPath)
            {
                Write-Debug "Using layout path: $layoutPath"
                $argumentSet[$layoutPathArgumentName] = $layoutPath
                if ($UseBootstrapper)
                {
                    Write-Debug 'Note: some older versions of the VS Setup Bootstrapper do not recognize the --installLayoutPath argument and, instead of consuming it, pass it unmodified to the VS Installer, which does not recognize it and signals an error. If installation fails, try suppressing the usage of this argument by passing --no-installLayoutPath in package parameters.'
                }
            }
        }

        $argumentSet['wait'] = ''
        $argumentSet['norestart'] = ''
        if (-not $argumentSet.ContainsKey('quiet') -and -not $argumentSet.ContainsKey('passive'))
        {
            $argumentSet['quiet'] = ''
        }

        Remove-NegatedArguments -Arguments $argumentSet -RemoveNegativeSwitches
        Remove-VSPackageParametersNotPassedToNativeInstaller -PackageParameters $argumentSet -TargetDescription $nativeInstallerDescription -Blacklist $nativeInstallerArgumentBlacklist
        if ($Operation -eq 'update')
        {
            # Remove arguments which cannot be used when updating an already installed VS instance.
            # This supports users who turned on the 'useRememberedArgumentsForUpgrades' feature of Chocolatey.
            # Reference: https://learn.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio?view=vs-2022#install-update-modify-repair-uninstall-and-export-commands-and-command-line-parameters
            $argumentsNotForUpdate = @('add', 'remove', 'addProductLang', 'removeProductLang', 'all', 'allWorkloads', 'includeRecommended', 'includeOptional', 'nickname', 'productKey', 'config')
            Remove-VSPackageParametersNotPassedToNativeInstaller -PackageParameters $argumentSet -TargetDescription "$nativeInstallerDescription for '$Operation' operation" -Blacklist $argumentsNotForUpdate
        }

        $silentArgs = ConvertTo-ArgumentString -InitialUnstructuredArguments @($Operation) -Arguments $argumentSet -Syntax 'Willow'

        $exitCode = -1
        $processed = $false
        if ($PSCmdlet.ShouldProcess("Executable: $nativeInstallerPath", "Install-VSChocolateyPackage with arguments: $silentArgs"))
        {
            $arguments = @{
                packageName = $PackageName
                silentArgs = $silentArgs
                url = $BootstrapperUrl
                checksum = $BootstrapperChecksum
                checksumType = $BootstrapperChecksumType
                logFilePath = $null
                assumeNewVS2017Installer = $true
                installerFilePath = $nativeInstallerPath
            }
            $argumentsDump = ($arguments.GetEnumerator() | ForEach-Object { '-{0}:''{1}''' -f $_.Key,"$($_.Value)" }) -join ' '
            Write-Debug "Install-VSChocolateyPackage $argumentsDump"
            Install-VSChocolateyPackage @arguments
            $exitCode = [int]$Env:ChocolateyExitCode
            Write-Debug "Exit code set by Install-VSChocolateyPackage: '$exitCode'"
            $processed = $true
        }

        if ($processed -and $Operation -eq 'update')
        {
            $instance = Resolve-VSProductInstance -ProductReference $thisProductReference -PackageParameters $argumentSet
            $instanceCount = ($instance | Measure-Object).Count
            if ($instanceCount -eq 1)
            {
                $currentProductVersion = [version]$instance.installationVersion
                if ($null -ne $DesiredProductVersion)
                {
                    if ($currentProductVersion -ge $DesiredProductVersion)
                    {
                        Write-Debug "After update operation, $productDescription is at version $currentProductVersion, which is greater than or equal to the desired version ($DesiredProductVersion)."
                    }
                    else
                    {
                        throw "After update operation, $productDescription is at version $currentProductVersion, which is lower than the desired version ($DesiredProductVersion). This means the update failed."
                    }
                }

                Write-Verbose "$productDescription is now at version $currentProductVersion."
            }
            elseif ($instanceCount -eq 0)
            {
                Write-Warning "Unable to detect the updated $productDescription instance. This might mean that update failed. "
            }
        }

        if ($overallExitCode -eq 0)
        {
            Write-Debug "Setting overall exit code to '$exitCode'"
            $overallExitCode = $exitCode
        }
    }

    Write-Debug "Setting Env:ChocolateyExitCode to overall exit code: '$overallExitCode'"
    $Env:ChocolateyExitCode = $overallExitCode
    if ($overallExitCode -eq 3010)
    {
        Write-Warning "${PackageName} has been ${frobbed}. However, a reboot is required to finalize the ${frobbage}."
    }
}
