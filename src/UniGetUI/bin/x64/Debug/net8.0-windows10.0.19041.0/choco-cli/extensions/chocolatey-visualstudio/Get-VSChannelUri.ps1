function Get-VSChannelUri
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $ChannelId
    )

    $manifestUri = $null
    $success = $ChannelId -match '^VisualStudio\.(?<version>\d+)\.(?<kind>[\w\.0-9]+)$' # VisualStudio.15.Release, VisualStudio.17.Release.LTSC.17.4
    if ($success)
    {
        $kind = switch ($Matches['kind'])
        {
            'Preview' { 'pre' }
            default { $_.ToLowerInvariant() }
        }

        $manifestUri = 'https://aka.ms/vs/{0}/{1}/channel' -f $Matches['version'], $kind
        Write-Debug "Using channel manifest URI computed from the channel id: '$manifestUri'"
    }
    else
    {
        $msg = "Channel id '$ChannelId' does not match the expected pattern and cannot be used to compute the channel manifest URI"
        Write-Debug $msg
        Write-Error $msg
    }

    return $manifestUri
}
