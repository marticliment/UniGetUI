function Get-WillowInstalledProducts
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $false)] [string] $BasePath
    )

    Write-Debug 'Detecting Visual Studio products installed using the Willow installer (2017+)'

    $loadedVsSetup = Get-Module -Name VSSetup
    if (($loadedVsSetup | Measure-Object).Count -eq 0)
    {
        if ((Get-Module -Name VSSetup -ListAvailable | Measure-Object).Count -gt 0)
        {
            Write-Debug 'Importing the VSSetup module'
            Import-Module -Name VSSetup
            $loadedVsSetup = Get-Module -Name VSSetup
        }
    }

    $hasVsSetup = $false
    if ($null -ne $loadedVsSetup)
    {
        # check if VSSetup is at least 2.0 (which has prerelease VS support)
        Write-Debug 'Checking version of VSSetup module'
        if ($loadedVsSetup.Version.Major -ge 2)
        {
            Write-Debug "A supported VSSetup version is present ($($loadedVsSetup.Version))"
            $hasVsSetup = $true
        }
    }

    if ($hasVsSetup)
    {
        Write-Debug 'Using VSSetup to detect Visual Studio instances'
        $instances = Get-VSSetupInstance -All -Prerelease
        foreach ($instance in $instances)
        {
            if ($null -eq $instance)
            {
                continue
            }

            Write-Debug "Found product instance: $($instance.InstallationPath)"

            $instancePackages = @{}
            foreach ($package in $instance.Packages)
            {
                if ($null -eq $instance)
                {
                    continue
                }

                $instancePackages[$package.Id] = $true
            }

            $instanceData = @{
                installChannelUri = $null # Get-VSSetupInstance does not seem to provide it
                channelUri = $instance.ChannelUri
                channelId = $instance.ChannelId
                nickname = $instance.Properties['nickname']
                installationVersion = $instance.InstallationVersion
                enginePath = $instance.EnginePath
                productId = $instance.Product.Id
                productLineVersion = $instance.CatalogInfo['ProductLineVersion']
                installationPath = $instance.InstallationPath
                selectedPackages = $instancePackages # in this case all installed, not only selected by the user
            }

            Write-Debug ('Instance data: {0}' -f (($instanceData.GetEnumerator() | Where-Object Key -ne 'selectedPackages' | ForEach-Object { '{0} = ''{1}''' -f $_.Key, $_.Value }) -join ' '))
            Write-Debug ('Instance packages: {0}' -f ($instanceData.selectedPackages.Keys -join ' '))
            Write-Output $instanceData
        }

        return
    }

    # If BasePath is specified, use it, otherwise look in the registry for the cache location
    if ($BasePath -eq '')
    {
        # Package cache may have been moved, so check registry - https://blogs.msdn.microsoft.com/heaths/2017/04/17/moving-or-disabling-the-package-cache-for-visual-studio-2017/

        $searchPath = @(
            "HKLM:\SOFTWARE\Policies\Microsoft\VisualStudio\Setup",
            "HKLM:\SOFTWARE\Microsoft\VisualStudio\Setup",
            "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\Setup"
        )

        $cachePath = $searchPath | Get-ItemProperty -Name CachePath -ErrorAction SilentlyContinue | Select-Object -ExpandProperty CachePath -First 1

        # If unable to locate the cache, try the default location
        if ($null -ne $cachePath)
        {
            Write-Debug "Using VS CachePath obtained from registry: $cachePath"
        }
        else
        {
            $cachePath = "${Env:ProgramData}\Microsoft\VisualStudio\Packages"
            Write-Debug "Using the default VS CachePath: $cachePath"
        }

        $BasePath = Join-Path -Path $cachePath -ChildPath '_Instances'
    }
    else
    {
        Write-Debug "Using provided BasePath: $BasePath"
    }

    if (-not (Test-Path -Path $BasePath))
    {
        Write-Debug "Base path '$BasePath' does not exist, assuming no products installed"
        return $null
    }

    $expectedProductProperties = @{
        productLineVersion = 'productLineVersion'
        installationPath = 'installationPath'
        installationVersion = 'installationVersion'
        channelId = 'channelId'
        channelUri = 'channelUri'
        productId = 'product"\s*:\s*{\s*"id'
        enginePath = 'enginePath'
    }
    $optionalProductProperties = @{
        nickname = 'nickname'
        installChannelUri = 'installChannelUri'
    }
    $propertyNameSelector = (($expectedProductProperties.Values + $optionalProductProperties.Values) | ForEach-Object { "($_)" }) -join '|'
    $regexTextBasicInfo = '"(?<name>{0})"\s*:\s*"(?<value>[^\"]+)"' -f $propertyNameSelector
    $rxBasicInfo = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList @($regexTextBasicInfo, 'ExplicitCapture,IgnorePatternWhitespace,Singleline')
    $regexTextSingleProductInfo = '\s*{\s*[^}]*"id"\s*:\s*"(?<packageId>[^\"]+)"[^}]*}'
    $rxSelectedPackages = [regex]('"selectedPackages"\s*:\s*\[(({0})(\s*,{0})*)\]' -f $regexTextSingleProductInfo)

    $instanceDataPaths = Get-ChildItem -Path $BasePath | Where-Object { $_.PSIsContainer -eq $true } | Select-Object -ExpandProperty FullName
    foreach ($instanceDataPath in $instanceDataPaths)
    {
        if ($null -eq $instanceDataPath)
        {
            continue
        }

        Write-Debug "Examining possible product instance: $instanceDataPath"
        $stateJsonPath = Join-Path -Path $instanceDataPath -ChildPath 'state.json'
        if (-not (Test-Path -Path $stateJsonPath))
        {
            Write-Warning "File state.json does not exist, this is not a Visual Studio product instance or the file layout has changed! (path: '$instanceDataPath')"
            continue
        }

        $instanceData = @{ selectedPackages = @{} }
        foreach ($name in ($expectedProductProperties.Keys + $optionalProductProperties.Keys))
        {
            $instanceData[$name] = $null
        }

        # unfortunately, PowerShell 2.0 does not have ConvertFrom-Json
        $text = [IO.File]::ReadAllText($stateJsonPath)
        $matches = $rxBasicInfo.Matches($text)
        foreach ($match in $matches)
        {
            if ($null -eq $match -or -not $match.Success)
            {
                continue
            }

            $name = $match.Groups['name'].Value -replace '"id', 'Id' -replace '[^a-zA-Z]', ''
            $value = $match.Groups['value'].Value -replace '\\\\', '\'
            $instanceData[$name] = $value
        }

        Write-Debug ('Parsed instance data: {0}' -f (($instanceData.GetEnumerator() | ForEach-Object { '{0} = ''{1}''' -f $_.Key, $_.Value }) -join ' '))
        $missingExpectedProperties = $expectedProductProperties.GetEnumerator() | Where-Object { -not $instanceData.ContainsKey($_.Key) } | Select-Object -ExpandProperty Key
        if (($missingExpectedProperties | Measure-Object).Count -gt 0)
        {
            Write-Warning "Failed to fully parse state.json, perhaps the file structure has changed! (path: '$stateJsonPath' missing properties: $missingExpectedProperties)"
            continue
        }

        $match = $rxSelectedPackages.Match($text)
        if ($match.Success)
        {
            foreach ($capture in $match.Groups['packageId'].Captures)
            {
                $packageId = $capture.Value
                $instanceData.selectedPackages[$packageId] = $true
            }
        }

        Write-Debug ('Parsed instance selected packages: {0}' -f ($instanceData.selectedPackages.Keys -join ' '))

        Write-Output $instanceData
    }
}
