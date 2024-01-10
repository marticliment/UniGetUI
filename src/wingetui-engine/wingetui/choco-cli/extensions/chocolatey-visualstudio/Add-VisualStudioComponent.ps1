function Add-VisualStudioComponent
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [string] $Component,
        [Parameter(Mandatory = $true)] [string] $VisualStudioYear,
        [Parameter(Mandatory = $true)] [string[]] $ApplicableProducts,
        [version] $RequiredProductVersion,
        [bool] $Preview,
        [hashtable] $DefaultParameterValues
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }

    Write-Debug "Running 'Add-VisualStudioComponent' with PackageName:'$PackageName' Component:'$Component' VisualStudioYear:'$VisualStudioYear' RequiredProductVersion:'$RequiredProductVersion' Preview:'$Preview'";
    $argumentList = @('add', "$Component")

    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters -DefaultValues $DefaultParameterValues
    $channelReference = Get-VSChannelReference -VisualStudioYear $VisualStudioYear -Preview:$Preview -PackageParameters $packageParameters
    Start-VSModifyOperation `
        -PackageName $PackageName `
        -PackageParameters $packageParameters `
        -ArgumentList $argumentList `
        -ChannelReference $channelReference `
        -ApplicableProducts $ApplicableProducts `
        -RequiredProductVersion $RequiredProductVersion `
        -OperationTexts @('installed', 'installing', 'installation')
}
