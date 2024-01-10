function Get-VisualStudioInstance
{
<#
.SYNOPSIS
Returns information about installed Visual Studio instances.

.DESCRIPTION
For each Visual Studio instance installed on the machine, this function returns an object
containing the basic properties of the instance.

.OUTPUTS
A System.Management.Automation.PSObject with the following properties:
InstallationPath (System.String)
InstallationVersion (System.Version)
ProductId (System.String; Visual Studio 2017+ only)
ChannelId (System.String; Visual Studio 2017+ only)
#>
    [CmdletBinding()]
    Param
    (
    )

    Get-WillowInstalledProducts | Where-Object { $null -ne $_ } | ForEach-Object {
        $props = @{
            InstallationPath = $_.installationPath
            InstallationVersion = [version]$_.installationVersion
            ProductId = $_.productId
            ChannelId = $_.channelId
        }
        $obj = New-Object -TypeName PSObject -Property $props
        Write-Output $obj
    }

    Get-VSLegacyInstance | Where-Object { $null -ne $_ } | ForEach-Object {
        $props = @{
            InstallationPath = $_.Path
            InstallationVersion = $_.Version
            ProductId = $null
            ChannelId = $null
        }
        $obj = New-Object -TypeName PSObject -Property $props
        Write-Output $obj
    }
}
