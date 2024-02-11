function Convert-VSProductReferenceToChannelReference
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [AllowNull()] [PSObject] $ProductReference
    )

    if ($null -eq $ProductReference)
    {
        return $null
    }

    $cr = New-VSChannelReference `
        -ChannelId $ProductReference.ChannelId `
        -ChannelUri $ProductReference.ChannelUri `
        -InstallChannelUri $ProductReference.InstallChannelUri
    return $cr
}
