function Get-VSChannelReference
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [ValidateSet('2017', '2019', '2022')] [string] $VisualStudioYear,
        [bool] $Preview,
        [hashtable] $PackageParameters
    )

    $channelId = $null
    $channelUri = $null
    $installChannelUri = $null
    if ($null -ne $PackageParameters)
    {
        if ($PackageParameters.ContainsKey('channelId'))
        {
            $channelId = $PackageParameters['channelId']
        }

        if ($PackageParameters.ContainsKey('channelUri'))
        {
            $channelUri = $PackageParameters['channelUri']
        }

        if ($PackageParameters.ContainsKey('installChannelUri'))
        {
            $installChannelUri = $PackageParameters['installChannelUri']
        }
    }

    if ($null -eq $channelId)
    {
        switch ($VisualStudioYear)
        {
            '2017' { $majorVersion = 15 }
            '2019' { $majorVersion = 16 }
            '2022' { $majorVersion = 17 }
            default { throw "Unsupported VisualStudioYear: $VisualStudioYear"}
        }

        $mapPreviewOrReleaseToChannelTypeSuffix = @{ $true = 'Preview'; $false = 'Release' }
        $channelId = 'VisualStudio.{0}.{1}' -f $majorVersion, $mapPreviewOrReleaseToChannelTypeSuffix[$Preview]
    }

    if ($null -eq $channelUri)
    {
        $channelUri = Get-VSChannelUri -ChannelId $channelId -ErrorAction SilentlyContinue
    }

    $obj = New-VSChannelReference -ChannelId $channelId -ChannelUri $channelUri -InstallChannelUri $installChannelUri
    return $obj
}
