function ConvertTo-ArgumentString
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $Arguments,
        [Parameter(Mandatory = $true)] [ValidateSet('Willow', 'VSIXInstaller')] [string] $Syntax,
        [string[]] $InitialUnstructuredArguments,
        [string[]] $FinalUnstructuredArguments
    )

    switch ($Syntax)
    {
        'Willow' { $prefix = '--'; $separator = ' ' }
        'VSIXInstaller' { $prefix = '/'; $separator = ':' }
        default { throw "Unknown Syntax parameter value: '$Syntax'" }
    }

    Write-Debug "Generating argument string using prefix = '$prefix', separator = '$separator'"

    $chunks = New-Object System.Collections.Generic.List``1[System.String]
    $rxNeedsQuoting = [regex]'^(([^"].*\s)|(\s))'

    if (($InitialUnstructuredArguments | Measure-Object).Count -gt 0)
    {
        foreach ($val in $InitialUnstructuredArguments)
        {
            if ($rxNeedsQuoting.IsMatch($val))
            {
                $val = """$val"""
            }

            $chunks.Add($val)
        }
    }

    foreach ($kvp in $Arguments.GetEnumerator())
    {
        if ($null -eq $kvp.Value -or ($kvp.Value -isnot [System.Collections.IList] -and [string]::IsNullOrEmpty($kvp.Value)))
        {
            $chunk = '{0}{1}' -f $prefix, $kvp.Key
            $chunks.Add($chunk)
        }
        else
        {
            $vals = $kvp.Value
            if ($vals -isnot [System.Collections.IList])
            {
                $vals = ,$vals
            }

            foreach ($val in $vals)
            {
                if ($rxNeedsQuoting.IsMatch($val))
                {
                    $val = """$val"""
                }

                $chunk = '{0}{1}{2}{3}' -f $prefix, $kvp.Key, $separator, $val
                $chunks.Add($chunk)
            }
        }
    }

    if (($FinalUnstructuredArguments | Measure-Object).Count -gt 0)
    {
        foreach ($val in $FinalUnstructuredArguments)
        {
            if ($rxNeedsQuoting.IsMatch($val))
            {
                $val = """$val"""
            }

            $chunks.Add($val)
        }
    }

    $argsString = $chunks -join ' '
    Write-Debug "Generated argument string: [$argsString]"
    return $argsString
}
