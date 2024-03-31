# based on Install-ChocolateyPackage (a9519b5), with changes:
# - added recognition of exit codes signifying reboot requirement
# - VS installers are exe
# - removed exit code parameters in order to centralize exit code logic
# - download logic refactored into a separate function for reuse
function Install-VSChocolateyPackage
{
    [CmdletBinding()]
    param(
        [string] $packageName,
        [string] $silentArgs = '',
        [string] $url,
        [alias("url64")][string] $url64bit = '',
        [string] $checksum = '',
        [string] $checksumType = '',
        [string] $checksum64 = '',
        [string] $checksumType64 = '',
        [string] $logFilePath,
        [switch] $assumeNewVS2017Installer,
        [string] $installerFilePath
    )

    Write-Debug "Running 'Install-VSChocolateyPackage' for $packageName with url:'$url', args:'$silentArgs', url64bit:'$url64bit', checksum:'$checksum', checksumType:'$checksumType', checksum64:'$checksum64', checksumType64:'$checksumType64', logFilePath:'$logFilePath', installerFilePath:'$installerFilePath'";

    $localFilePath = Get-VSWebFile `
        -PackageName $PackageName `
        -DefaultFileName 'vs_setup.exe' `
        -FileDescription 'installer executable' `
        -Url $url `
        -Url64Bit $url64bit `
        -Checksum $checksum `
        -ChecksumType $checksumType `
        -Checksum64 $checksum64 `
        -ChecksumType64 $checksumType64 `
        -LocalFilePath $installerFilePath

    $arguments = @{
        packageName = $packageName
        silentArgs = $silentArgs
        file = $localFilePath
        logFilePath = $logFilePath
        assumeNewVS2017Installer = $assumeNewVS2017Installer
    }
    Install-VSChocolateyInstallPackage @arguments
}
