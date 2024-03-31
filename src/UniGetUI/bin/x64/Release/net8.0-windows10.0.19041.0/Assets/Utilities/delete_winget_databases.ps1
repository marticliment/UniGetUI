
$users = Get-ChildItem -Path 'C:\Users' -Directory

foreach ($user in $users) {
    $path = "C:\Users\$($user.Name)\AppData\Local\Microsoft\WinGet\Settings\defaultState"

    if (-not (Test-Path -Path $path)) {
        continue
    }

    Remove-Item -Path $path -Recurse -Force
}

foreach ($user in $users) {
    $path = "C:\Users\$($user.Name)\AppData\Local\Temp\WinGet\defaultState\"
    
    if (-not (Test-Path -Path $path)) {
        continue
    }

    Remove-Item -Path $path -Recurse -Force
}
