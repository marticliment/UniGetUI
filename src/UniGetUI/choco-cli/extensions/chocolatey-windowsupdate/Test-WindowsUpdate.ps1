<#
.SYNOPSIS
    Tests whether a specified Windows update (KB) is installed.

.DESCRIPTION
    Returns $true if the specified Windows update is installed.
    Returns $false if the specified Windows update is not installed
    or if it was superseded by another installed update.

.NOTES
    A $false result might not be fully reliable if the requested KB
    was superseded and/or included in a later KB.

.PARAMETER Id
    The identifier of the update, in the format "KBnnnnnnnn".

.EXAMPLE
    Test-WindowsUpdate -Id KB3214628

    Returns $true if update KB3214628 is installed, $false otherwise.

.INPUTS
    none

.OUTPUTS
    System.Boolean
#>
function Test-WindowsUpdate
{
    [CmdletBinding()]
    Param
    (
        [ValidatePattern('^KB\d+$')] [Parameter(Mandatory = $true)] [string] $Id
    )
    Begin
    {
        Set-StrictMode -Version 2
        $ErrorActionPreference = 'Stop'
    }
    End
    {
        Write-Verbose "Looking for Win32_QuickFixEngineering with HotFixID = $Id"
        if ($null -ne (Get-Command -Name Get-CimInstance -ErrorAction SilentlyContinue)) {
            $qfe = Get-CimInstance -Class Win32_QuickFixEngineering -Filter ('HotFixID = "{0}"' -f $Id)
        } else {
            $qfe = Get-WmiObject -Class Win32_QuickFixEngineering -Filter ('HotFixID = "{0}"' -f $Id)
        }
        $found = $null -ne $qfe
        Write-Verbose "QFE $Id found: $found"
        return $found
    }
}
