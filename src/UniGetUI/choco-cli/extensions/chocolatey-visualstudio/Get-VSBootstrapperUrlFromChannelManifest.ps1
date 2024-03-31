function Get-VSBootstrapperUrlFromChannelManifest
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [PSObject] $ChannelReference,
        [switch] $UseInstallChannelUri
    )
    Write-Verbose 'Trying to determine the bootstrapper (vs_Setup.exe) url from the channel manifest'

    Write-Debug 'Obtaining the channel manifest'
    $manifest = Get-VSChannelManifest -PackageParameters $PackageParameters -ChannelReference $ChannelReference -UseInstallChannelUri:$UseInstallChannelUri

    Write-Debug 'Parsing the channel manifest'
    $url, $checksum, $checksumType = Get-VSChannelManifestItemUrl -Manifest $manifest -ChannelItemType 'Bootstrapper'

    return $url, $checksum, $checksumType
}
