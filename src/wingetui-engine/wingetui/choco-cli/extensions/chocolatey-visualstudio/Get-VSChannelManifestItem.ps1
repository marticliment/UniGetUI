function Get-VSChannelManifestItem
{
    [OutputType([Collections.IDictionary])]
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Manifest,
        [ValidateSet('Bootstrapper', 'Manifest', 'ChannelProduct')] [Parameter(Mandatory = $true)] [string] $ChannelItemType,
        [string] $Id
    )

    if ($Id -eq '')
    {
        $searchDesc = "type $ChannelItemType"
        $additionalFilter = { $true }
        $expectSingle = $ChannelItemType -ne 'ChannelProduct'
    }
    else
    {
        $searchDesc = "type $ChannelItemType and id $Id"
        $additionalFilter = { $_.ContainsKey('id') -and $_['id'] -eq $Id }
        $expectSingle = $true
    }

    $totalCount = 0
    if ($Manifest -is [Collections.IDictionary] -and $Manifest.ContainsKey('channelItems'))
    {
        $channelItems = $Manifest['channelItems']
        if ($channelItems -is [array])
        {
            $matchingItems = @($channelItems | Where-Object { $_ -is [Collections.IDictionary] -and $_.ContainsKey('type') -and $_['type'] -eq $ChannelItemType } | Where-Object $additionalFilter)
            $matchingItemsCount = ($matchingItems | Measure-Object).Count
            if ($matchingItemsCount -eq 0)
            {
                Write-Debug "Manifest parsing error: zero channelItem objects found of $searchDesc"
            }
            elseif ($expectSingle -and $matchingItemsCount -gt 1)
            {
                Write-Debug "Manifest parsing error: expected 1 but found $matchingItemsCount channelItem objects of $searchDesc"
            }
            else
            {
                foreach ($channelItem in $matchingItems)
                {
                    if ($channelItem -is [Collections.IDictionary])
                    {
                        Write-Output $channelItem
                        $totalCount += 1
                    }
                    else
                    {
                        Write-Debug 'Manifest parsing error: channelItem is not IDictionary'
                    }
                }
            }
        }
        else
        {
            Write-Debug 'Manifest parsing error: channelItems is not an array'
        }
    }
    else
    {
        Write-Debug 'Manifest parsing error: manifest is not IDictionary or does not contain channelItems'
    }

    if ($totalCount -ne 0)
    {
        Write-Debug "Located $totalCount channel manifest item(s) of $searchDesc"
    }
    else
    {
        Write-Debug "Could not locate any channel manifest item(s) of $searchDesc"
    }
}
