function Merge-AdditionalArguments
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $Arguments,
        [Parameter(Mandatory = $true)] [hashtable] $AdditionalArguments
    )

    foreach ($kvp in $AdditionalArguments.GetEnumerator())
    {
        $val = $kvp.Value
        if ($null -ne $val)
        {
            # strip quotes; will be added later, if needed
            if ($val -is [string])
            {
                $val = $val.Trim('''" ')
            }
            else
            {
                if ($val -is [System.Collections.IList])
                {
                    $newList = New-Object -TypeName System.Collections.ArrayList
                    foreach ($oneVal in $val)
                    {
                        if ($oneVal -is [string])
                        {
                            $oneVal = $oneVal.Trim('''" ')
                        }

                        [void]$newList.Add($oneVal)
                    }

                    $val = $newList
                }
            }
        }

        if ($Arguments.ContainsKey($kvp.Key) -and $Arguments[$kvp.Key] -ne $val)
        {
            Write-Debug "Replacing argument '$($kvp.Key)' value '$($Arguments[$kvp.Key])' with '$val'"
        }

        $Arguments[$kvp.Key] = $val
    }
}
