function Get-VSLegacyInstance
{
    [CmdletBinding()]
    Param
    (
    )

    Write-Debug 'Detecting Visual Studio products installed using the classic MSI installer (2015 and earlier)'
    $bitness = [IntPtr]::Size * 8
    if ($bitness -eq 64)
    {
        $basePath = 'HKLM:\SOFTWARE\WOW6432Node'
    }
    else
    {
        $basePath = 'HKLM:\SOFTWARE'
    }
    Write-Debug "Process bitness is $bitness, using '$basePath' as base registry path"
    $keyPath = Join-Path -Path $basePath -ChildPath 'Microsoft\VisualStudio\SxS\VS7'
    $classicInstanceCount = 0
    if (-not (Test-Path -Path $keyPath))
    {
        Write-Debug "VS registry key '$keyPath' does not exist."
    }
    else
    {
        Write-Debug "Enumerating properties of key '$keyPath'"
        $props = Get-ItemProperty -Path $keyPath
        $props.PSObject.Properties | Where-Object { $_.Name -match '^\d+\.\d+$' } | ForEach-Object {
            $prop = $_
            Write-Debug ('Found possible classic VS instance: ''{0}'' = ''{1}''' -f $prop.Name, $prop.Value)
            $version = [version]$prop.Name
            if ($version -ge [version]'15.0')
            {
                Write-Debug ('VS instance version is 15.0 or greater, skipping (not a classic instance)')
            }
            else
            {
                $path = $prop.Value
                Write-Verbose ('Found classic VS instance: {0} = ''{1}''' -f $version, $path)
                $instanceProps = @{
                    Version = $version
                    Path = $path
                }
                $instance = New-Object -TypeName PSObject -Property $instanceProps
                Write-Output $instance
                $classicInstanceCount += 1
            }
        }
    }
    if ($classicInstanceCount -eq 0)
    {
        Write-Verbose 'No classic (MSI) Visual Studio installations detected.'
    }
    else
    {
        Write-Verbose "Detected $classicInstanceCount classic (MSI) Visual Studio installations."
    }
}
