function Get-VSChannelManifest
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [PSObject] $ChannelReference,
        [switch] $UseInstallChannelUri,
        [string] $LayoutPath
    )

    $manifestUri = $null
    # first, see if the caller provided the manifest uri via package parameters or ChannelReference
    Write-Debug 'Checking if the channel manifest URI has been provided'
    Write-Debug ('InstallChannelUri will {0}' -f @{ $true = 'be used, if present'; $false = 'not be used' }[[bool]$UseInstallChannelUri])
    if ($UseInstallChannelUri -and $PackageParameters.ContainsKey('installChannelUri') -and -not [string]::IsNullOrEmpty($PackageParameters['installChannelUri']))
    {
        $manifestUri = $PackageParameters['installChannelUri']
        Write-Debug "Using channel manifest URI from the 'installChannelUri' package parameter: '$manifestUri'"
    }
    else
    {
        Write-Debug "Package parameters do not contain 'installChannelUri', it is empty or the scenario does not allow its use"
        if ($PackageParameters.ContainsKey('channelUri') -and -not [string]::IsNullOrEmpty($PackageParameters['channelUri']))
        {
            $manifestUri = $PackageParameters['channelUri']
            Write-Debug "Using channel manifest URI from the 'channelUri' package parameter: '$manifestUri'"
        }
        else
        {
            Write-Debug "Package parameters do not contain 'channelUri' or it is empty"
            if ($null -ne $ChannelReference)
            {
                if ($UseInstallChannelUri -and -not [string]::IsNullOrEmpty($ChannelReference.InstallChannelUri))
                {
                    $manifestUri = $ChannelReference.InstallChannelUri
                    Write-Debug "Using manifest URI from the InstallChannelUri property of the provided ChannelReference: '$manifestUri'"
                }
                else
                {
                    Write-Debug "ChannelReference InstallChannelUri property is empty"
                    if (-not [string]::IsNullOrEmpty($ChannelReference.ChannelUri))
                    {
                        $manifestUri = $ChannelReference.ChannelUri
                        Write-Debug "Using manifest URI from the ChannelUri property of the provided ChannelReference: '$manifestUri'"
                    }
                    else
                    {
                        Write-Debug "ChannelReference ChannelUri property is empty"
                    }
                }
            }
            else
            {
                Write-Debug "ChannelReference has not been provided"
            }
        }
    }

    if ($null -eq $manifestUri)
    {
        # second, try to compute the uri from the channel id
        Write-Debug 'Checking if the channel id has been provided'
        $channelId = $null
        if ($PackageParameters.ContainsKey('channelId') -and -not [string]::IsNullOrEmpty($PackageParameters['channelId']))
        {
            $channelId = $PackageParameters['channelId']
            Write-Debug "Using channel id from the 'channelId' package parameter: '$channelId'"
        }
        else
        {
            Write-Debug "Package parameters do not contain 'channelId' or it is empty"
            if ($null -ne $ChannelReference)
            {
                $channelId = $ChannelReference.ChannelId
                Write-Debug "Using channel id from the provided ChannelReference: '$channelId'"
            }
            else
            {
                Write-Debug "ChannelReference has not been provided; channel id is not known"
            }
        }
        if ($null -ne $channelId)
        {
            $manifestUri = Get-VSChannelUri -ChannelId $channelId -ErrorAction SilentlyContinue
        }
    }

    if ($null -eq $manifestUri)
    {
        # Finally, fall back to hardcoded.
        # This may currently happen only when Install-VisualStudio is called without -VisualStudioVersion and -Product (which are not mandatory for backward compat with old package versions).
        # Ultimately, code should be reworked to make ChannelReference mandatory in this function and eliminate this hardcoded value.
        $manifestUri = 'https://aka.ms/vs/15/release/channel'
        Write-Warning "Fallback: using hardcoded channel manifest URI: '$manifestUri'"
    }

    if ($LayoutPath -eq '')
    {
        # look in LayoutPath only if --noWeb
        if (-not $packageParameters.ContainsKey('noWeb'))
        {
            Write-Debug 'Not looking in LayoutPath because --noWeb was not passed in package parameters'
        }
        else
        {
            $LayoutPath = Resolve-VSLayoutPath -PackageParameters $PackageParameters
        }
    }
    else
    {
        Write-Debug "Using provided LayoutPath: $LayoutPath"
    }

    $manifest = Get-VSManifest -Description 'channel manifest' -Url $manifestUri -LayoutFileName 'ChannelManifest.json' -LayoutPath $LayoutPath

    return $manifest
}
