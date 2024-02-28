function Remove-VisualStudioProduct
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [string] $Product,
        [Parameter(Mandatory = $true)] [string] $VisualStudioYear,
        [bool] $Preview,
        [hashtable] $DefaultParameterValues
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }

    Write-Debug "Running 'Remove-VisualStudioProduct' with PackageName:'$PackageName' Product:'$Product' VisualStudioYear:'$VisualStudioYear' Preview:'$Preview'";
    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters -DefaultValues $DefaultParameterValues
    $channelReference = Get-VSChannelReference -VisualStudioYear $VisualStudioYear -Preview:$Preview -PackageParameters $packageParameters
    $productReference = Get-VSProductReference -ChannelReference $channelReference -Product $Product -PackageParameters $packageParameters
    Start-VSModifyOperation `
        -PackageName $PackageName `
        -ArgumentList @() `
        -ChannelReference $channelReference `
        -ApplicableProducts @($Product) `
        -OperationTexts @('uninstalled', 'uninstalling', 'uninstallation') `
        -Operation 'uninstall' `
        -PackageParameters $packageParameters `
        -ProductReference $productReference
    $remainingProductsCount = (Get-WillowInstalledProducts | Measure-Object).Count
    Write-Verbose ("Found {0} installed Visual Studio 2017 or later product(s)" -f $remainingProductsCount)
    if ($remainingProductsCount -gt 0)
    {
        Write-Warning "If Chocolatey asks permission to run the Auto Uninstaller, please answer No. Otherwise, you might lose other Visual Studio products installed on your machine."
    }
}
