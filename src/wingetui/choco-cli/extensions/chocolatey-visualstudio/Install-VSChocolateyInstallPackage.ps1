# based on Install-ChocolateyInstallPackage (fbe24a8), with changes:
# - added recognition of exit codes signifying reboot requirement
# - VS installers are exe
# - dropped support for chocolateyInstallArguments and chocolateyInstallOverride
# - removed unreferenced parameter
# - refactored logic shared with Uninstall-VSChocolateyPackage to a generic function
# - removed exit code parameters in order to centralize exit code logic
function Install-VSChocolateyInstallPackage {
    [CmdletBinding()]
    param(
        [string] $packageName,
        [string] $silentArgs = '',
        [string] $file,
        [string] $logFilePath,
        [switch] $assumeNewVS2017Installer
    )
    Write-Debug "Running 'Install-VSChocolateyInstallPackage' for $packageName with file:'$file', silentArgs:'$silentArgs', logFilePath:'$logFilePath', assumeNewVS2017Installer:'$assumeNewVS2017Installer'"
    $installMessage = "Installing $packageName..."
    Write-Host $installMessage

    if ([string]::IsNullOrEmpty($file)) {
        throw 'Package parameters incorrect, File cannot be empty.'
    }

    Start-VSServicingOperation @PSBoundParameters -operationTexts @('installed', 'installing', 'installation')
}
