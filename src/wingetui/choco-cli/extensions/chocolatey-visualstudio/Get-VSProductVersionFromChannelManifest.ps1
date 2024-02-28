function Get-VSProductVersionFromChannelManifest
{
    [OutputType([System.Version])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $ProductId,
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [PSObject] $ChannelReference,
        [switch] $UseInstallChannelUri
    )
    Write-Verbose "Trying to determine the product $ProductId version from the channel manifest"

    Write-Debug 'Obtaining the channel manifest'
    $manifest = Get-VSChannelManifest -PackageParameters $PackageParameters -ChannelReference $ChannelReference -UseInstallChannelUri:$UseInstallChannelUri

    Write-Debug 'Parsing the channel manifest'
    $version = Get-VSChannelManifestItemVersion -Manifest $manifest -ChannelItemType 'ChannelProduct' -Id $ProductId

    return $version
}
