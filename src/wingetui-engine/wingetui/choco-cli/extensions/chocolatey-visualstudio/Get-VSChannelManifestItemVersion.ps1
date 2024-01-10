function Get-VSChannelManifestItemVersion
{
    [OutputType([System.Version])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Manifest,
        [ValidateSet('Bootstrapper', 'Manifest', 'ChannelProduct')] [Parameter(Mandatory = $true)] [string] $ChannelItemType,
        [string] $Id,
        [string] $PropertyName = 'version'
    )

    $versionObject = $null
    $channelItem = Get-VSChannelManifestItem -Manifest $Manifest -ChannelItemType $ChannelItemType -Id $Id
    if (($channelItem | Measure-Object).Count -eq 1 -and $channelItem -is [Collections.IDictionary] -and $channelItem.ContainsKey($PropertyName))
    {
        $versionString = $channelItem[$PropertyName]
        if ($versionString -is [string])
        {
            if (-not [version]::TryParse($versionString, [ref]$versionObject))
            {
                Write-Debug "Manifest parsing error: property '$PropertyName' value '$versionString' failed to parse as System.Version"
            }
        }
        else
        {
            Write-Debug "Manifest parsing error: property '$PropertyName' value is not a string"
        }
    }
    else
    {
        Write-Debug "Manifest parsing error: channelItem is not IDictionary or does not contain property '$PropertyName'"
    }

    if ($null -ne $versionObject)
    {
        Write-Verbose "$ChannelItemType $Id $PropertyName determined from the channel manifest: $versionObject"
        return $versionObject
    }
    else
    {
        Write-Verbose "The $ChannelItemType $Id $PropertyName could not be determined from the channel manifest"
        return $null
    }
}
