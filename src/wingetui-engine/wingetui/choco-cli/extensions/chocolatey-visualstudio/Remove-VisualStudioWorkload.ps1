function Remove-VisualStudioWorkload
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [string] $Workload,
        [Parameter(Mandatory = $true)] [string] $VisualStudioYear,
        [Parameter(Mandatory = $true)] [string[]] $ApplicableProducts,
        [bool] $Preview,
        [hashtable] $DefaultParameterValues
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }

    Write-Debug "Running 'Remove-VisualStudioWorkload' with PackageName:'$PackageName' Workload:'$Workload' VisualStudioYear:'$VisualStudioYear' Preview:'$Preview'";
    $argumentList = @('remove', "Microsoft.VisualStudio.Workload.$Workload")

    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters -DefaultValues $DefaultParameterValues
    $channelReference = Get-VSChannelReference -VisualStudioYear $VisualStudioYear -Preview:$Preview -PackageParameters $packageParameters
    Start-VSModifyOperation `
        -PackageName $PackageName `
        -PackageParameters $packageParameters `
        -ArgumentList $argumentList `
        -ChannelReference $channelReference `
        -ApplicableProducts $ApplicableProducts `
        -OperationTexts @('uninstalled', 'uninstalling', 'uninstallation')
}
