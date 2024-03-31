<#
.SYNOPSIS
    Get the current proxy using several methods

.DESCRIPTION
    Function tries to find the current proxy using several methods, in the given order:
        - $env:chocolateyProxyLocation variable
        - $env:http_proxy environment variable
        - IE proxy
        - Chocolatey config
        - Winhttp proxy
        - WebClient proxy

    Use Verbose parameter to see which of the above locations was used for the result, if any.
    The function currently doesn't handle the proxy username and password.

.OUTPUTS
    [String] in the form of http://<proxy>:<port>
#>
function Get-EffectiveProxy(){

    # Try chocolatey proxy environment vars
    if ($env:chocolateyProxyLocation) {
        Write-Verbose "Using `$Env:chocolateyProxyLocation"
        return $env:chocolateyProxyLocation
    }

    # Try standard Linux variable
    if ($env:http_proxy) {
        Write-Verbose "Using `$Env:http_proxy"
        return $env:http_proxy
    }
    
    # Try to get IE proxy
    $key = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings"
    $r = Get-ItemProperty $key
    if ($r.ProxyEnable -and $r.ProxyServer) {
        Write-Verbose "Using IE proxy settings"
        return "http://" + $r.ProxyServer
    }
    
    # Try chocolatey config file
    [xml] $cfg = Get-Content $env:ChocolateyInstall\config\chocolatey.config
    $p = $cfg.chocolatey.config | ForEach-Object { $_.add } | Where-Object { $_.key -eq 'proxy' } | Select-Object -Expand value
    if ($p) {
        Write-Verbose "Using choco config proxy"
        return $p
    }

    # Try winhttp proxy
    (netsh.exe winhttp show proxy) -match 'Proxy Server\(s\)' | Set-Variable proxy 
    $proxy = $proxy -split ' :' | Select-Object -Last 1
    $proxy = $proxy.Trim()
    if ($proxy) {
        Write-Verbose "Using winhttp proxy server"
        return "http://" + $proxy
    }

    # Try using WebClient
    $url = "http://chocolatey.org"
    $client = New-Object System.Net.WebClient
    if ($client.Proxy.IsBypassed($url)) { return $null }

    Write-Verbose "Using WebClient proxy"
    return "http://" + $client.Proxy.GetProxy($url).Authority
}
