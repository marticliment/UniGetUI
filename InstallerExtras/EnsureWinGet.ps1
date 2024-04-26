Set-ExecutionPolicy Bypass -Scope Process -Force
if (!(Get-Command -Verb Repair -Noun WinGetPackageManager)) {
    Write-Output "Microsoft.WinGet.Client is not installed or is not on the latest version"
    try
    {
        Write-Output "Attempting to uninstall an older version of Microsoft.WinGet.Client..."
        Uninstall-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force        
    }
    catch 
    {
        Write-Output "Microsoft.WinGet.Client was not installed."
    }
    Write-Output "Installing Microsoft.WinGet.Client..."
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Confirm:$false
    Install-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force -AllowClobber
    Write-Output "Microsoft.WinGet.Client was installed successfully"
}

Write-Output "Checking for updates for Microsoft.WinGet.Client module..."
if ((Get-Module -Name Microsoft.WinGet.Client -ListAvailable).Version -ge '1.7.10861')
{
    Write-Output "Microsoft.WinGet.Client is up-to-date"
} else {
    Write-Output "Updating Microsoft.WinGet.Client module..."
    Update-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force
}

if ($null -eq (Get-Command "winget.exe" -ErrorAction SilentlyContinue)) 
{ 
    Write-Output "WinGet is not present on the system"
    Write-Output "Installing WinGet..."
    Repair-WinGetPackageManager
    Write-Output "WinGet was installed successfully"
}
