function Get-VSUninstallRegistryKey
{
    [CmdletBinding()]
    Param (
        [string] $ApplicationName
    )

    Write-Debug "Looking for Uninstall key for '$ApplicationName'"
    $uninstallKey = @('Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall') `
    | Where-Object { Test-Path -Path $_ } `
    | Get-ChildItem `
    | Where-Object { $displayName = $_ | Get-ItemProperty -Name DisplayName -ErrorAction SilentlyContinue | Select-Object -ExpandProperty DisplayName; $displayName -eq $ApplicationName } `
    | Where-Object { $systemComponent = $_ | Get-ItemProperty -Name SystemComponent -ErrorAction SilentlyContinue | Select-Object -ExpandProperty SystemComponent; $systemComponent -ne 1 }

    return $uninstallKey
}
