# based on Uninstall-ChocolateyPackage (01db65b), with changes:
# - added recognition of exit codes signifying reboot requirement
# - VS installers are exe
# - dropped support for chocolateyInstallArguments and chocolateyInstallOverride
# - refactored logic shared with Install-VSChocolateyInstallPackage to a generic function
# - removed exit code parameters in order to centralize exit code logic
function Uninstall-VSChocolateyPackage
{
    [CmdletBinding()]
    param(
        [string] $packageName,
        [string] $silentArgs = '',
        [string] $file,
        [switch] $assumeNewVS2017Installer
    )
    Write-Debug "Running 'Uninstall-VSChocolateyPackage' for $packageName with silentArgs:'$silentArgs', file:'$file', assumeNewVS2017Installer:'$assumeNewVS2017Installer'"

    $installMessage = "Uninstalling $packageName..."
    Write-Host $installMessage

    Start-VSServicingOperation @PSBoundParameters -operationTexts @('uninstalled', 'uninstalling', 'uninstallation')
}
