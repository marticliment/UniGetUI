Write-Output "Installing scoop..."

Invoke-Expression "& {$(Invoke-RestMethod get.scoop.sh)}"

If (-Not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Output "Installing git..."
    scoop install git
}

Write-Output "Done!"
