Write-Output "Installing scoop..."

iex "& {$(irm get.scoop.sh)} -RunAsAdmin"

$Env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")  

If (-Not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Output "Installing git..."
    scoop install git
}

Write-Output "Done!"
