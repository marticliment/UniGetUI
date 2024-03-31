function Remove-NegatedArguments
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $Arguments,
        [switch] $RemoveNegativeSwitches
    )

    # --no-foo cancels --foo
    $negativeSwitches = $Arguments.GetEnumerator() | Where-Object { $_.Key -match '^no-.' -and $_.Value -eq '' } | Select-Object -ExpandProperty Key
    foreach ($negativeSwitch in $negativeSwitches)
    {
        if ($null -eq $negativeSwitch)
        {
            continue
        }

        $parameterToRemove = $negativeSwitch.Substring(3)
        if ($Arguments.ContainsKey($parameterToRemove))
        {
            Write-Debug "Removing negated package parameter: '$parameterToRemove'"
            $Arguments.Remove($parameterToRemove)
        }

        if ($RemoveNegativeSwitches)
        {
            Write-Debug "Removing negative switch: '$negativeSwitch'"
            $Arguments.Remove($negativeSwitch)
        }
    }
}
