Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Path $MyInvocation.MyCommand.Definition

Get-ChildItem -Path "$scriptRoot\*.ps1" | ForEach-Object { . $_ }
Export-ModuleMember -Function '*-VisualStudio*'
