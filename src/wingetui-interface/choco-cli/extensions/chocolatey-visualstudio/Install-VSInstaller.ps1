function Install-VSInstaller
{
    [CmdletBinding()]
    param(
      [Parameter(Mandatory = $true)] [string] $PackageName,
      [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
      [PSObject] $ChannelReference,
      [string] $Url,
      [string] $Checksum,
      [string] $ChecksumType,
      [Alias('RequiredVersion')] [version] $RequiredInstallerVersion,
      [version] $RequiredEngineVersion,
      [switch] $Force,
      [switch] $UseInstallChannelUri,
      [switch] $DoNotInstallIfNotPresent
    )
    Write-Debug "Running 'Install-VSInstaller' for $PackageName with Url:'$Url' Checksum:$Checksum ChecksumType:$ChecksumType RequiredInstallerVersion:'$RequiredInstallerVersion' RequiredEngineVersion:'$RequiredEngineVersion' Force:'$Force' UseInstallChannelUri:'$UseInstallChannelUri' DoNotInstallIfNotPresent:'$DoNotInstallIfNotPresent'";
    $argumentSet = $PackageParameters.Clone()

    Write-Debug 'Determining whether the Visual Studio Installer needs to be installed/updated/reinstalled'
    $shouldUpdate = $false
    $existing = Get-VisualStudioInstaller
    if ($null -ne $existing)
    {
        Write-Debug 'The Visual Studio Installer is already present'
        if ($null -ne $existing.Version -and $null -ne $RequiredInstallerVersion)
        {
            if ($existing.Version -lt $RequiredInstallerVersion)
            {
                Write-Debug 'The existing Visual Studio Installer version is lower than requested, so it should be updated'
                $shouldUpdate = $true
            }
            elseif ($existing.Version -eq $RequiredInstallerVersion)
            {
                Write-Debug 'The existing Visual Studio Installer version is equal to requested (no update required)'
            }
            else
            {
                Write-Debug 'The existing Visual Studio Installer version is greater than requested (no update required)'
            }
        }

        if ($null -ne $existing.EngineVersion -and $null -ne $RequiredEngineVersion)
        {
            if ($existing.EngineVersion -lt $RequiredEngineVersion)
            {
                Write-Debug 'The existing Visual Studio Installer engine version is lower than requested, so it should be updated'
                $shouldUpdate = $true
            }
            elseif ($existing.EngineVersion -eq $RequiredEngineVersion)
            {
                Write-Debug 'The existing Visual Studio Installer engine version is equal to requested (no update required)'
            }
            else
            {
                Write-Debug 'The existing Visual Studio Installer engine version is greater than requested (no update required)'
            }
        }
    }
    else
    {
        if ($DoNotInstallIfNotPresent)
        {
            Write-Debug 'The Visual Studio Installer is not present'
        }
        else
        {
            Write-Debug 'The Visual Studio Installer is not present and will be installed'
            $shouldUpdate = $true
        }
    }

    $attemptingRepair = $false
    if (-not $shouldUpdate)
    {
        $existingHealth = $existing | Get-VisualStudioInstallerHealth
        if ($null -ne $existingHealth -and -not $existingHealth.IsHealthy)
        {
            Write-Warning "The Visual Studio Installer is broken (missing files: $($existingHealth.MissingFiles -join ', ')). Attempting to reinstall it."
            $shouldUpdate = $true
            $attemptingRepair = $true
        }
    }

    if (-not $shouldUpdate -and $Force)
    {
        Write-Debug 'The Visual Studio Installer does not need to be updated, but it will be reinstalled because -Force was used'
        $shouldUpdate = $true
    }

    if (-not $shouldUpdate)
    {
        return
    }

    # if installing from layout, check for existence of vs_installer.opc and auto add --offline
    if (-not $argumentSet.ContainsKey('offline'))
    {
        $layoutPath = Resolve-VSLayoutPath -PackageParameters $argumentSet
        if ($null -ne $layoutPath)
        {
            $installerOpcPath = Join-Path -Path $layoutPath -ChildPath 'vs_installer.opc'
            if (Test-Path -Path $installerOpcPath)
            {
                Write-Debug "The VS Installer package is present in the layout path: $installerOpcPath"
                # TODO: also if the version in layout will satisfy version requirements
                if ($argumentSet.ContainsKey('noWeb'))
                {
                    Write-Debug "Using the VS Installer package present in the layout path because --noWeb was passed in package parameters"
                    $argumentSet['offline'] = $installerOpcPath
                }
                else
                {
                    Write-Debug "Not using the VS Installer package present in the layout path because --noWeb was not passed in package parameters"
                }
            }
        }
    }

    if ($argumentSet.ContainsKey('bootstrapperPath'))
    {
        $installerFilePath = $argumentSet['bootstrapperPath']
        $argumentSet.Remove('bootstrapperPath')
        Write-Debug "User-provided bootstrapper path: $installerFilePath"
    }
    else
    {
        $installerFilePath = $null
        if ($Url -eq '')
        {
            $Url, $Checksum, $ChecksumType = Get-VSBootstrapperUrlFromChannelManifest -PackageParameters $argumentSet -ChannelReference $ChannelReference -UseInstallChannelUri:$UseInstallChannelUri
        }
    }

    $downloadedOrProvidedExe = Get-VSWebFile `
        -PackageName 'Visual Studio Installer' `
        -DefaultFileName 'vs_setup.exe' `
        -FileDescription 'installer executable' `
        -Url $Url `
        -Checksum $Checksum `
        -ChecksumType $ChecksumType `
        -LocalFilePath $installerFilePath

    $isBox = (Split-Path -Leaf -Path $downloadedOrProvidedExe) -ne 'vs_setup_bootstrapper.exe' # in case the user pointed us to already extracted vs_setup_bootstrapper.exe
    if ($isBox)
    {
        # vs_Setup.exe 15.6 has a flaw in its handling of --quiet --update:
        # because vs_Setup.exe appends an additional argument (--env) to vs_setup_bootstrapper.exe,
        # the latter thinks it is in "roundtrip update" and starts vs_installer.exe at the end.
        # This flaw is not present in vs_Setup.exe 15.7 or later, presumably because of improved
        # parameter handling in vs_setup_bootstrapper.exe.
        Write-Debug 'Checking the version of the box executable'
        $boxVersion = [version](Get-Item -Path $downloadedOrProvidedExe).VersionInfo.FileVersion
        $shouldUnpackBox = [version]'15.6' -le $boxVersion -and $boxVersion -lt [version]'15.7'
        if ($shouldUnpackBox)
        {
            Write-Debug "The box executable (version $boxVersion) is affected by the --quiet --update flaw, so it will be unpacked as a workaround"

            $chocTempDir = $env:TEMP
            $tempDir = Join-Path $chocTempDir "$PackageName"
            if ($null -ne $env:packageVersion) { $tempDir = Join-Path $tempDir "$env:packageVersion" }

            $extractedBoxPath = Join-Path -Path $tempDir -ChildPath (Get-Item -Path $downloadedOrProvidedExe).BaseName
            if (Test-Path -Path $extractedBoxPath)
            {
                Write-Debug "Removing already existing box extraction path: $extractedBoxPath"
                Remove-Item -Path $extractedBoxPath -Recurse
            }

            Get-ChocolateyUnzip `
                -PackageName 'Visual Studio Installer' `
                -FileFullPath $downloadedOrProvidedExe `
                -Destination $extractedBoxPath `
                | Out-Null

            $vsSetupBootstrapperExe = Join-Path -Resolve -Path $extractedBoxPath -ChildPath 'vs_bootstrapper_d15\vs_setup_bootstrapper.exe'
            $installerToRun = $vsSetupBootstrapperExe
        }
        else
        {
            Write-Debug "The box executable (version $boxVersion) is not affected by the --quiet --update flaw, so it will be used directly"
            $installerToRun = $downloadedOrProvidedExe
        }
    }
    else
    {
        Write-Debug "It looks like the provided bootstrapperPath points to an already extracted vs_setup_bootstrapper.exe"
        $installerToRun = $downloadedOrProvidedExe
    }

    $whitelist = @('quiet', 'offline')
    Remove-VSPackageParametersNotPassedToNativeInstaller -PackageParameters $argumentSet -TargetDescription 'bootstrapper during VS Installer update' -Whitelist $whitelist

    # --update must be last
    $argumentSet['quiet'] = $null
    $silentArgs = ConvertTo-ArgumentString -Arguments $argumentSet -FinalUnstructuredArguments @('--update') -Syntax 'Willow'
    $arguments = @{
        packageName = 'Visual Studio Installer'
        silentArgs = $silentArgs
        file = $installerToRun
        logFilePath = $null
        assumeNewVS2017Installer = $true
    }
    $argumentsDump = ($arguments.GetEnumerator() | ForEach-Object { '-{0}:''{1}''' -f $_.Key,"$($_.Value)" }) -join ' '

    $attempt = 0
    do
    {
        $retry = $false
        $attempt += 1
        Write-Debug "Install-VSChocolateyInstallPackage $argumentsDump"
        Install-VSChocolateyInstallPackage @arguments

        $updated = Get-VisualStudioInstaller
        if ($null -eq $updated)
        {
            throw 'The Visual Studio Installer is not present even after supposedly successful update!'
        }

        if ($null -eq $existing)
        {
            Write-Verbose "The Visual Studio Installer version $($updated.Version) (engine version $($updated.EngineVersion)) was installed."
        }
        else
        {
            if ($updated.Version -eq $existing.Version -and $updated.EngineVersion -eq $existing.EngineVersion)
            {
                Write-Verbose "The Visual Studio Installer version $($updated.Version) (engine version $($updated.EngineVersion)) was reinstalled."
            }
            else
            {
                if ($updated.Version -lt $existing.Version)
                {
                    Write-Warning "The Visual Studio Installer got updated, but its version after update ($($updated.Version)) is lower than the version before update ($($existing.Version))."
                }
                else
                {
                    if ($updated.EngineVersion -lt $existing.EngineVersion)
                    {
                        Write-Warning "The Visual Studio Installer got updated, but its engine version after update ($($updated.EngineVersion)) is lower than the engine version before update ($($existing.EngineVersion))."
                    }
                    else
                    {
                        Write-Verbose "The Visual Studio Installer got updated to version $($updated.Version) (engine version $($updated.EngineVersion))."
                    }
                }
            }
        }

        if ($null -ne $updated.Version)
        {
            if ($null -ne $RequiredInstallerVersion)
            {
                if ($updated.Version -lt $RequiredInstallerVersion)
                {
                    Write-Warning "The Visual Studio Installer got updated to version $($updated.Version), which is still lower than the requirement of version $RequiredInstallerVersion or later."
                }
                else
                {
                    Write-Verbose "The Visual Studio Installer got updated to version $($updated.Version), which satisfies the requirement of version $RequiredInstallerVersion or later."
                }
            }
        }
        else
        {
            Write-Warning "Unable to determine the Visual Studio Installer version after the update."
        }

        if ($null -ne $updated.EngineVersion)
        {
            if ($null -ne $RequiredEngineVersion)
            {
                if ($updated.EngineVersion -lt $RequiredEngineVersion)
                {
                    Write-Warning "The Visual Studio Installer engine got updated to version $($updated.EngineVersion), which is still lower than the requirement of version $RequiredEngineVersion or later."
                }
                else
                {
                    Write-Verbose "The Visual Studio Installer engine got updated to version $($updated.EngineVersion), which satisfies the requirement of version $RequiredEngineVersion or later."
                }
            }
        }
        else
        {
            Write-Warning "Unable to determine the Visual Studio Installer engine version after the update."
        }

        $updatedHealth = $updated | Get-VisualStudioInstallerHealth
        if (-not $updatedHealth.IsHealthy)
        {
            if ($attempt -eq 1)
            {
                if ($attemptingRepair)
                {
                    $msg = 'is still broken after reinstall'
                }
                else
                {
                    $msg = 'got broken after update'
                }

                Write-Warning "The Visual Studio Installer $msg (missing files: $($updatedHealth.MissingFiles -join ', ')). Attempting to repair it."
                $installerDir = Split-Path -Path $updated.Path
                $newName = '{0}.backup-{1:yyyyMMddHHmmss}' -f (Split-Path -Leaf -Path $installerDir), (Get-Date)
                Write-Verbose "Renaming directory '$installerDir' to '$newName'"
                Rename-Item -Path $installerDir -NewName $newName
                Write-Verbose 'Retrying the installation'
                $retry = $true
            }
            else
            {
                throw "The Visual Studio Installer is still broken even after the attempt to repair it."
            }
        }
        else
        {
            Write-Verbose 'The Visual Studio Installer is healthy (no missing files).'
        }
    }
    while ($retry)
}
