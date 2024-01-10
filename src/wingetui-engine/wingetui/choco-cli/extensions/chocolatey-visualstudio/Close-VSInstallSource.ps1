function Close-VSInstallSource
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [PSObject] $InstallSourceInfo
    )

    if ($null -ne $InstallSourceInfo.MountedDiskImage)
    {
        Write-Host "Dismounting ISO"
        $InstallSourceInfo.MountedDiskImage | Dismount-DiskImage
    }
    else
    {
        Write-Verbose "No ISO to dismount"
    }
}
