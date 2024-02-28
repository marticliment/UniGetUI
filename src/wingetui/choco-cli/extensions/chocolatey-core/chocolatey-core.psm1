# Export functions that start with capital letter, others are private
# Include file names that start with capital letters, ignore others
$ScriptRoot = Split-Path $MyInvocation.MyCommand.Definition

$pre = Get-ChildItem Function:\*
Get-ChildItem "$ScriptRoot\*.ps1" |
    Where-Object { $_.Name -cmatch '^[A-Z]+' } |
    ForEach-Object { . $_  }
$post = Get-ChildItem Function:\*
$funcs = Compare-Object $pre $post |
    Select-Object -ExpandProperty InputObject |
    Select-Object -ExpandProperty Name
$funcs |
    Where-Object { $_ -cmatch '^[A-Z]+'} |
    ForEach-Object { Export-ModuleMember -Function $_ }