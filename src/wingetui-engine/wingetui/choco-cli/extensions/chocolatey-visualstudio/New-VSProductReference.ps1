function New-VSProductReference
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $ChannelId,
        [Parameter(Mandatory = $true)] [string] $ProductId,
        [string] $ChannelUri,
        [string] $InstallChannelUri
    )

    $props = @{
        ChannelId = $ChannelId
        ChannelUri = $ChannelUri
        InstallChannelUri = $InstallChannelUri
        ProductId = $ProductId
    }

    $obj = New-Object -TypeName PSObject -Property $props
    return $obj
}
