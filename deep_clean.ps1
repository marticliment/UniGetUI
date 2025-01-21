Get-ChildItem -Path . -Recurse -Directory -Force -Include bin, obj |
Where-Object { $_.FullName -notmatch 'choco-cli' } |
ForEach-Object {
    Write-Host "Removing folder: $($_.FullName)" -ForegroundColor Yellow
    Remove-Item -Path $_.FullName -Recurse -Force
}

Write-Host "Cleanup completed." -ForegroundColor Green

pause