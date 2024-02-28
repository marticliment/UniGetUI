function Get-VSRequiredInstallerVersion
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [PSObject] $ChannelReference,
        [switch] $UseInstallChannelUri
    )
    Write-Verbose 'Trying to determine the required installer and engine version from the manifests'

    Write-Debug 'Obtaining the channel manifest in order to determine the required installer version'
    $channelManifest = Get-VSChannelManifest -PackageParameters $PackageParameters -ChannelReference $ChannelReference -UseInstallChannelUri:$UseInstallChannelUri

    # VS 2022 17.4+
    $version = Get-VSChannelManifestItemVersion -Manifest $channelManifest -ChannelItemType 'Bootstrapper' -PropertyName 'installerVersion'
    if ($null -ne $version)
    {
        Write-Verbose "Required installer version determined from the channel manifest (as bootstrapper installerVersion property): '$version'"
    }
    else
    {
        # VS <= 2022 17.3
        $version = Get-VSChannelManifestItemVersion -Manifest $channelManifest -ChannelItemType 'Bootstrapper' -PropertyName 'version'
        if ($null -ne $version)
        {
            Write-Verbose "Required installer version determined from the channel manifest (as bootstrapper version property): '$version'"
        }
        else
        {
            Write-Verbose "The required installer version could not be determined from the component manifest"
        }
    }

    Write-Debug 'Obtaining the component manifest in order to determine the required engine version'
    $manifest = Get-VSComponentManifest -PackageParameters $PackageParameters -ChannelReference $ChannelReference -ChannelManifest $channelManifest -UseInstallChannelUri:$UseInstallChannelUri

    Write-Debug 'Parsing the component manifest'
    $engineVersion = $null
    if ($manifest -is [Collections.IDictionary] -and $manifest.ContainsKey('engineVersion'))
    {
        $engineVersionString = $manifest['engineVersion']
        if ($engineVersionString -is [string])
        {
            $engineVersion = [version]$engineVersionString
        }
        else
        {
            Write-Debug 'Manifest parsing error: engineVersion is not a string'
        }
    }
    else
    {
        Write-Debug 'Manifest parsing error: manifest is not IDictionary or does not contain engineVersion'
    }

    if ($null -ne $engineVersion)
    {
        Write-Verbose "Required engine version determined from the component manifest: '$engineVersion'"
    }
    else
    {
        Write-Verbose "The required engine version could not be determined from the component manifest"
    }

    $props = @{
        Version = $version
        EngineVersion = $engineVersion
    }
    $obj = New-Object -TypeName PSObject -Property $props
    Write-Output $obj
}
