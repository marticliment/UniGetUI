Set-ExecutionPolicy Bypass -Scope Process -Force
if ($null -eq (Get-Command "winget.exe" -ErrorAction SilentlyContinue)) 
{ 
    Write-Output "WinGet is not present on the system"
    if (!(Get-Command -Verb Repair -Noun WinGetPackageManager)) {
        Write-Output "Microsoft.WinGet.Client is not installed or is not on the latest version"
        try
        {
            Write-Output "Attempting to uninstall an older version of Microsoft.WinGet.Client..."
            Uninstall-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force -Scope CurrentUser    
        }
        catch 
        {
            Write-Output "Microsoft.WinGet.Client was not installed."
        }
        Write-Output "Installing Microsoft.WinGet.Client..."
        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Confirm:$false -Scope CurrentUser
        Install-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force -Scope CurrentUser
        Write-Output "Microsoft.WinGet.Client was installed successfully"
    }

    Write-Output "Checking for updates for Microsoft.WinGet.Client module..."
    if ((Get-Module -Name Microsoft.WinGet.Client -ListAvailable).Version -ge '1.8.1791')
    {
        Write-Output "Microsoft.WinGet.Client is up-to-date"
    } else {
        Write-Output "Updating Microsoft.WinGet.Client module..."
        Update-Module -Name Microsoft.WinGet.Client -Confirm:$false -Force -Scope CurrentUser
    }

    Write-Output "Installing WinGet..."
    Repair-WinGetPackageManager
    Write-Output "WinGet was installed successfully"
}


