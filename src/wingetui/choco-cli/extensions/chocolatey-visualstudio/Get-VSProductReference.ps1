function Get-VSProductReference
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [PSObject] $ChannelReference,
        [Parameter(Mandatory = $true)] [string] $Product,
        [hashtable] $PackageParameters
    )

    $productId = $null
    if ($null -ne $PackageParameters)
    {
        if ($PackageParameters.ContainsKey('productId'))
        {
            $productId = $PackageParameters['productId']
        }
    }

    if ($null -eq $productId)
    {
        $productId = "Microsoft.VisualStudio.Product." + $Product
    }

    $obj = New-VSProductReference -ChannelId $ChannelReference.ChannelId -ProductId $ProductId -ChannelUri $ChannelReference.ChannelUri -InstallChannelUri $ChannelReference.InstallChannelUri
    return $obj
}
