function Assert-VSInstallerUpdated
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [PSObject] $ChannelReference,
        [string] $Url,
        [string] $Checksum,
        [string] $ChecksumType,
        [switch] $UseInstallChannelUri
    )

    if ($PackageParameters.ContainsKey('noUpdateInstaller'))
    {
        Write-Verbose "Skipping update of the VS Installer because --noUpdateInstaller was passed in package parameters"
        return
    }

    Write-Verbose 'Checking installer version required by the package'
    $packageRequiredVersionInfo = Get-VSRequiredInstallerVersion -PackageParameters $PackageParameters -ChannelReference $ChannelReference -UseInstallChannelUri:$UseInstallChannelUri `
        | Add-Member -PassThru -NotePropertyMembers @{ ForPackage = $true; Source = 'package' }

    # If there are other VS products installed (e.g. we are installing/updating VS 2017 and a 2019 product is installed),
    # the VS Installer will also check other channels for installer update requirement.
    $installedProductsUpdateableChannels = Get-WillowInstalledProducts `
        | Where-Object { $null -ne $_ } `
        | ForEach-Object { New-VSChannelReference -ChannelId $_.channelId -ChannelUri $_.channelUri } `
        | Where-Object { -not [string]::IsNullOrEmpty($_.ChannelUri) <# may be empty to disable updates #> } `
        | Sort-Object -Property ChannelId, ChannelUri -Unique
    $otherChannelsToCheck = $installedProductsUpdateableChannels | Where-Object { $null -eq $ChannelReference -or $ChannelReference.ChannelId -ne $_.ChannelId }
    $otherRequiredVersionInfos = @()
    if (($otherChannelsToCheck | Measure-Object).Count -gt 0)
    {
        if (-not $PackageParameters.ContainsKey('noWeb'))
        {
            Write-Verbose 'Checking installer version required by installed Visual Studio product(s) from other channel(s)'
            $otherRequiredVersionInfos = @($otherChannelsToCheck `
                | ForEach-Object {
                    Get-VSRequiredInstallerVersion -ChannelReference $_ -PackageParameters @{} `
                        | Add-Member -PassThru -NotePropertyMembers @{ ForPackage = $false; Source = "channel $($_.ChannelId)"; ChannelReference = $_ }
                })
        }
        else
        {
            Write-Verbose 'Not checking installer version required by installed Visual Studio product(s) from other channel(s) because --noWeb was passed in package parameters.'
        }
    }

    $allRequiredVersionInfos = @($packageRequiredVersionInfo) + $otherRequiredVersionInfos
    $requiredVersionInfo = $allRequiredVersionInfos | Sort-Object -Property Version,EngineVersion -Descending | Select-Object -First 1
    Write-Verbose "Highest required installer version: $($requiredVersionInfo.Version) (engine: $($requiredVersionInfo.EngineVersion)); requirement source: $($requiredVersionInfo.Source)"

    if ($requiredVersionInfo.ForPackage)
    {
        # Installing the VS Installer update from the channel of this package,
        # so supporting all normal features (offline layout, explicit bootstrapper path etc).
        Install-VSInstaller `
            -DoNotInstallIfNotPresent `
            -RequiredInstallerVersion $requiredVersionInfo.Version `
            -RequiredEngineVersion $requiredVersionInfo.EngineVersion `
            @PSBoundParameters
    }
    else
    {
        # Installing the VS Installer update from a different channel than this package,
        # so using the default manifest source for the other channel
        # and the default bootstrapper location from the channel manifest.
        Install-VSInstaller `
            -DoNotInstallIfNotPresent `
            -RequiredInstallerVersion $requiredVersionInfo.Version `
            -RequiredEngineVersion $requiredVersionInfo.EngineVersion `
            -ChannelReference $requiredVersionInfo.ChannelReference `
            -PackageName $PackageName `
            -PackageParameters @{}
    }
}
