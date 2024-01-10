function Resolve-VSLayoutPath
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [hashtable] $PackageParameters
    )

    Write-Debug 'Detecting if a layout path was provided via package parameters'

    if ($PackageParameters.ContainsKey('installLayoutPath'))
    {
        $installLayoutPath = $PackageParameters['installLayoutPath']
        if (-not [string]::IsNullOrEmpty($installLayoutPath))
        {
            Write-Debug "Using installLayoutPath provided via package parameters: $installLayoutPath"
            return $installLayoutPath
        }
        else
        {
            Write-Debug 'Package parameters contain installLayoutPath, but it is empty - ignoring'
        }
    }

    if ($PackageParameters.ContainsKey('bootstrapperPath'))
    {
        $bootstrapperPath = $PackageParameters['bootstrapperPath']
        if (-not [string]::IsNullOrEmpty($bootstrapperPath))
        {
            $installLayoutPath = Split-Path -Path $bootstrapperPath
            Write-Debug "Using installLayoutPath computed from bootstrapperPath provided via package parameters: $installLayoutPath"
            return $installLayoutPath
        }
        else
        {
            Write-Debug 'Package parameters contain $bootstrapperPath, but it is empty - ignoring'
        }
    }

    Write-Debug 'A layout path was not provided via package parameters'
    return $null
}
