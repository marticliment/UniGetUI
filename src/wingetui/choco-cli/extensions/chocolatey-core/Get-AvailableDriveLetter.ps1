<#
.SYNOPSIS
  Get a 'free' drive letter

.DESCRIPTION
  Get a not yet in-use drive letter that can be used for mounting

.EXAMPLE
  Get-AvailableDriveLetter

.EXAMPLE
  Get-AvailableDriveLetter 'X'
  (do not return X, even if it'd be the next choice)

.INPUTS
  specific drive letter(s) that will be excluded as potential candidates

.OUTPUTS
  System.String (single drive-letter character)

.LINK
  http://stackoverflow.com/questions/12488030/getting-a-free-drive-letter/29373301#29373301
#>
function Get-AvailableDriveLetter {
  param (
    [char[]]$ExcludedLetters,

    # Allows splatting with arguments that do not apply and future expansion. Do not use directly.
    [parameter(ValueFromRemainingArguments = $true)]
    [Object[]] $IgnoredArguments
  )

  $Letter = [int][char]'C'
  $i = @()

  #getting all the used Drive letters reported by the Operating System
  $(Get-PSDrive -PSProvider filesystem) | ForEach-Object{$i += $_.name}

  #Adding the excluded letter
  $i+=$ExcludedLetters

  while($i -contains $([char]$Letter)){$Letter++}

  if ($Letter -gt [char]'Z') {
    throw "error: no drive letter available!"
  }
  Write-Verbose "available drive letter: '$([char]$Letter)'"
  Return $([char]$Letter)
}

