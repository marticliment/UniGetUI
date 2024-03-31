# Export functions that start with capital letter, and located in helpers directory.
# Other files are considered private, and should not be imported.
$ScriptRoot = Split-Path $MyInvocation.MyCommand.Definition

# The functions available in Chocolatey is not available in a variable
# we need to skip the import of a function if it already exist, as such
# we need to check the file system directly.
$ChocolateyScriptPath = Resolve-Path "$env:ChocolateyInstall\helpers\functions"

$existingMembers = Get-ChildItem $ChocolateyScriptPath | ForEach-Object BaseName

Get-ChildItem "$ScriptRoot\helpers\*.ps1" | `
  Where-Object { $_.Name -cmatch "^[A-Z]+" } | `
  ForEach-Object {
  $name = $_.BaseName

  if ($existingMembers -notcontains $name) {
    Write-Debug "Exporting function '$name' for backwards compatibility"
    . $_
    Export-ModuleMember -Function $name
  }
  else {
    Write-Debug "Function '$name' exists, ignoring export."
  }
}
