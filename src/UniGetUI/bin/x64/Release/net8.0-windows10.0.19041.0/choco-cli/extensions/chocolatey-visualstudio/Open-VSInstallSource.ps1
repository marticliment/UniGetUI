function Open-VSInstallSource
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters,
        [string] $Url
    )

    $mountedIso = $null
    if ($packageParameters.ContainsKey('bootstrapperPath'))
    {
        $installerFilePath = $packageParameters['bootstrapperPath']
        Write-Debug "User-provided bootstrapper path: $installerFilePath"
    }
    else
    {
        if (Test-Path -Path Env:\visualStudio:setupFolder)
        {
            $setupFolder = "$Env:visualStudio:setupFolder"
            Write-Debug "Setup folder provided via environment variable: $setupFolder"
        }
        else
        {
            $setupFolder = $null
        }

        if ($null -eq $setupFolder -or -not (Test-Path -Path $setupFolder))
        {
            if ($PackageParameters.ContainsKey('IsoImage'))
            {
                $isoPath = $PackageParameters['IsoImage']
                Write-Debug "Using IsoImage from package parameters: $isoPath"
            }
            else
            {
                if (Test-Path -Path Env:\visualStudio:isoImage)
                {
                    $isoPath = "$Env:visualStudio:isoImage"
                    Write-Debug "Using isoImage from environment variable: $isoPath"
                }
                else
                {
                    $isoPath = $null
                }
            }

            if ($null -ne $isoPath)
            {
                $storageModule = Get-Module -ListAvailable -Name Storage
                if ($null -eq $storageModule)
                {
                    throw "ISO mounting is not available on this operating system (requires Windows 8 or later)."
                }

                Write-Host "Mounting ISO image $isoPath"
                $mountedIso = Mount-DiskImage -PassThru -ImagePath $isoPath
                Write-Host "Obtaining drive letter of the mounted ISO image"
                $isoDrive = Get-Volume -DiskImage $mountedIso | Select-Object -ExpandProperty DriveLetter
                Write-Host "Mounted ISO to $isoDrive"
                $setupFolder = "${isoDrive}:\"

                # on some systems the new drive is not recognized by PowerShell until the PSDrive subsystem is "touched"
                # - probably a caching issue inside PowerShell
                Get-PSDrive | Format-Table -AutoSize | Out-String | Write-Debug
                # if it does not immediately resolve the issue, give it some more time
                if (-not (Test-Path -Path $setupFolder))
                {
                    Write-Debug "The new drive has not been recognized by PowerShell yet, giving it some time"
                    $counter = 10
                    do
                    {
                        Write-Debug 'Sleeping for 500 ms'
                        Start-Sleep -Milliseconds 500
                        Get-PSDrive | Format-Table -AutoSize | Out-String | Write-Debug
                        $counter -= 1
                    }
                    while (-not (Test-Path -Path $setupFolder) -and $counter -gt 0)
                    if (-not (Test-Path -Path $setupFolder))
                    {
                        Write-Warning "Unable to test access to the mounted ISO image. Installation will probably fail."
                    }
                }
            }
        }

        if ($null -ne $setupFolder)
        {
            if ($Url -like '*.exe')
            {
                $exeName = Split-Path -Leaf -Path ([uri]$Url).LocalPath
                Write-Debug "Installer executable name determined from url: $exeName"
            }
            else
            {
                $exeName = 'vs_Setup.exe'
                Write-Debug "The installer url does not contain the executable name, using default: $exeName"
            }

            Write-Host "Installing Visual Studio from $setupFolder"
            $installerFilePath = Join-Path -Path $setupFolder -ChildPath $exeName
            Write-Debug "Installer path in setup folder: $installerFilePath"
        }
        else
        {
            $installerFilePath = $null
        }
    }

    if ($null -eq $installerFilePath)
    {
        Write-Verbose "Visual Studio installer will be downloaded from the Web"
    }
    else
    {
        Write-Host "Visual Studio will be installed using $installerFilePath"
    }

    $props = @{
        InstallerFilePath = $installerFilePath
        MountedDiskImage = $mountedIso
    }

    $obj = New-Object -TypeName PSObject -Property $props
    Write-Output $obj
}
