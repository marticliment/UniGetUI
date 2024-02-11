$scriptRoot = Split-Path -Path $MyInvocation.MyCommand.Definition

$publicFunctions = @(
    'Install-DotNetFramework',
    'Install-DotNetDevPack'
)

Get-ChildItem -Path "$scriptRoot\*.ps1" | ForEach-Object { . $_ }
Export-ModuleMember -Function $publicFunctions
