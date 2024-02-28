function Get-VSChannelManifestItemUrl
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [System.Collections.IDictionary] $Manifest,
        [ValidateSet('Bootstrapper', 'Manifest')] [Parameter(Mandatory = $true)] [string] $ChannelItemType
    )

    $url = $null
    $checksum = $null
    $checksumType = $null
    $channelItem = Get-VSChannelManifestItem -Manifest $Manifest -ChannelItemType $ChannelItemType
    if ($channelItem -is [Collections.IDictionary] -and $channelItem.ContainsKey('payloads'))
    {
        $payloads = $channelItem['payloads']
        if ($payloads -is [array])
        {
            if (($payloads | Measure-Object).Count -eq 1)
            {
                $payload = $payloads[0]
                if ($payload -is [Collections.IDictionary] -and $payload.ContainsKey('url'))
                {
                    $url = $payload['url']
                    if (-not [string]::IsNullOrEmpty($url) -and $payload.ContainsKey('sha256'))
                    {
                        $checksum = $payload['sha256']
                        $checksumType = 'sha256'
                    }
                    else
                    {
                        Write-Debug 'Manifest parsing error: payload url is empty or payload does not contain sha256'
                        # url will still be returned; it might be useful even without the checksum
                    }
                }
                else
                {
                    Write-Debug 'Manifest parsing error: payload is not IDictionary or does not contain url'
                }
            }
            else
            {
                Write-Debug 'Manifest parsing error: zero or more than one payload objects found'
            }
        }
        else
        {
            Write-Debug 'Manifest parsing error: payloads is not an array'
        }
    }
    else
    {
        Write-Debug 'Manifest parsing error: channelItem is not IDictionary or does not contain payloads'
    }

    if (-not [string]::IsNullOrEmpty($url))
    {
        Write-Verbose "$ChannelItemType url determined from the channel manifest: '$url' (checksum: '$checksum', type: '$checksumType')"
        return $url, $checksum, $checksumType
    }
    else
    {
        Write-Verbose "The $ChannelItemType url could not be determined from the channel manifest"
        return $null
    }
}
