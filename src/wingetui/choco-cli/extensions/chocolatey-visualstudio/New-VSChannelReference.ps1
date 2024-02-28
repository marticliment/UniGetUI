function New-VSChannelReference
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $ChannelId,
        [string] $ChannelUri,
        [string] $InstallChannelUri
    )

    $props = @{
        ChannelId = $ChannelId
        ChannelUri = $ChannelUri
        InstallChannelUri = $InstallChannelUri
    }

    $obj = New-Object -TypeName PSObject -Property $props
    return $obj
}
