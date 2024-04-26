# http://poshcode.org/417
## Get-WebFile (aka wget for PowerShell)
##############################################################################################################
## Downloads a file or page from the web
## History:
## v3.6 - Add -Passthru switch to output TEXT files
## v3.5 - Add -Quiet switch to turn off the progress reports ...
## v3.4 - Add progress report for files which don't report size
## v3.3 - Add progress report for files which report their size
## v3.2 - Use the pure Stream object because StreamWriter is based on TextWriter:
##        it was messing up binary files, and making mistakes with extended characters in text
## v3.1 - Unwrap the filename when it has quotes around it
## v3   - rewritten completely using HttpWebRequest + HttpWebResponse to figure out the file name, if possible
## v2   - adds a ton of parsing to make the output pretty
##        added measuring the scripts involved in the command, (uses Tokenizer)
##############################################################################################################
## Additional functionality added by Chocolatey Team / Chocolatey Contributors
##  - Proxy
##  - Better error handling
##  - Inline documentation
##  - Cmdlet conversion
##  - Closing request/response and cleanup
##  - Request / ReadWriteResponse Timeouts
##############################################################################################################
function Get-WebFile {
    <#
.SYNOPSIS
Downloads a file from an HTTP/HTTPS location. Prefer HTTPS when
available.

.DESCRIPTION
This will download a file from an HTTP/HTTPS location, saving the file
to the FileName location specified.

.NOTES
This is a low-level function and not recommended for use in package
scripts. It is recommended you call `Get-ChocolateyWebFile` instead.

Will automatically call Set-PowerShellExitCode to set the package exit
code to 404 if the resource is not found.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Url
This is the url to download the file from. Prefer HTTPS when available.

.PARAMETER FileName
This is the full path to the file to create. If downloading to the
package folder next to the install script, the path will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.exe"`

.PARAMETER UserAgent
The user agent to use as part of the request. Defaults to 'chocolatey
command line'.

.PARAMETER PassThru
DO NOT USE - holdover from original function.

.PARAMETER Quiet
Silences the progress output.

.PARAMETER Options
OPTIONAL - Specify custom headers.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.LINK
Get-ChocolateyWebFile

.LINK
Get-FtpFile

.LINK
Get-WebHeaders

.LINK
Get-WebFileName
#>
    param(
        [parameter(Mandatory = $false, Position = 0)][string] $url = '', #(Read-Host "The URL to download"),
        [parameter(Mandatory = $false, Position = 1)][string] $fileName = $null,
        [parameter(Mandatory = $false, Position = 2)][string] $userAgent = 'chocolatey command line',
        [parameter(Mandatory = $false)][switch] $Passthru,
        [parameter(Mandatory = $false)][switch] $quiet,
        [parameter(Mandatory = $false)][hashtable] $options = @{Headers = @{} },
        [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
    )

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    try {
        $uri = [System.Uri]$url
        if ($uri.IsFile()) {
            Write-Debug "Url is local file, setting destination"
            if ($url.LocalPath -ne $fileName) {
                Copy-Item $uri.LocalPath -Destination $fileName -Force
            }

            return
        }
    }
    catch {
        #continue on
    }

    $req = [System.Net.HttpWebRequest]::Create($url);
    $defaultCreds = [System.Net.CredentialCache]::DefaultCredentials
    if ($defaultCreds -ne $null) {
        $req.Credentials = $defaultCreds
    }

    $webclient = New-Object System.Net.WebClient
    if ($defaultCreds -ne $null) {
        $webClient.Credentials = $defaultCreds
    }

    # check if a proxy is required
    $explicitProxy = $env:chocolateyProxyLocation
    $explicitProxyUser = $env:chocolateyProxyUser
    $explicitProxyPassword = $env:chocolateyProxyPassword
    $explicitProxyBypassList = $env:chocolateyProxyBypassList
    $explicitProxyBypassOnLocal = $env:chocolateyProxyBypassOnLocal
    if ($explicitProxy -ne $null) {
        # explicit proxy
        $proxy = New-Object System.Net.WebProxy($explicitProxy, $true)
        if ($explicitProxyPassword -ne $null) {
            $passwd = ConvertTo-SecureString $explicitProxyPassword -AsPlainText -Force
            $proxy.Credentials = New-Object System.Management.Automation.PSCredential ($explicitProxyUser, $passwd)
        }

        if ($explicitProxyBypassList -ne $null -and $explicitProxyBypassList -ne '') {
            $proxy.BypassList = $explicitProxyBypassList.Split(',', [System.StringSplitOptions]::RemoveEmptyEntries)
        }
        if ($explicitProxyBypassOnLocal -eq 'true') {
            $proxy.BypassProxyOnLocal = $true;
        }

        Write-Host "Using explicit proxy server '$explicitProxy'."
        $req.Proxy = $proxy
    }
    elseif ($webclient.Proxy -and !$webclient.Proxy.IsBypassed($url)) {
        # system proxy (pass through)
        $creds = [net.CredentialCache]::DefaultCredentials
        if ($creds -eq $null) {
            Write-Debug "Default credentials were null. Attempting backup method"
            $cred = Get-Credential
            $creds = $cred.GetNetworkCredential();
        }
        $proxyaddress = $webclient.Proxy.GetProxy($url).Authority
        Write-Host "Using system proxy server '$proxyaddress'."
        $proxy = New-Object System.Net.WebProxy($proxyaddress)
        $proxy.Credentials = $creds
        $proxy.BypassProxyOnLocal = $true
        $req.Proxy = $proxy
    }

    $req.Accept = "*/*"
    $req.AllowAutoRedirect = $true
    $req.MaximumAutomaticRedirections = 20
    #$req.KeepAlive = $true
    $req.AutomaticDecompression = [System.Net.DecompressionMethods]::GZip -bor [System.Net.DecompressionMethods]::Deflate
    $req.Timeout = 30000
    if ($env:chocolateyRequestTimeout -ne $null -and $env:chocolateyRequestTimeout -ne '') {
        Write-Debug "Setting request timeout to  $env:chocolateyRequestTimeout"
        $req.Timeout = $env:chocolateyRequestTimeout
    }
    if ($env:chocolateyResponseTimeout -ne $null -and $env:chocolateyResponseTimeout -ne '') {
        Write-Debug "Setting read/write timeout to  $env:chocolateyResponseTimeout"
        $req.ReadWriteTimeout = $env:chocolateyResponseTimeout
    }

    #http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
    $req.CookieContainer = New-Object System.Net.CookieContainer
    if ($userAgent -ne $null) {
        Write-Debug "Setting the UserAgent to `'$userAgent`'"
        $req.UserAgent = $userAgent
    }

    if ($options.Headers.Count -gt 0) {
        Write-Debug "Setting custom headers"
        foreach ($key in $options.headers.keys) {
            $uri = (New-Object -TypeName system.uri $url)
            switch ($key) {
                'Accept' {
                    $req.Accept = $options.headers.$key
                }
                'Cookie' {
                    $req.CookieContainer.SetCookies($uri, $options.headers.$key)
                }
                'Referer' {
                    $req.Referer = $options.headers.$key
                }
                'User-Agent' {
                    $req.UserAgent = $options.headers.$key
                }
                Default {
                    $req.Headers.Add($key, $options.headers.$key)
                }
            }
        }
    }

    try {
        $res = $req.GetResponse();

        try {
            $headers = @{}
            foreach ($key in $res.Headers) {
                $value = $res.Headers[$key];
                if ($value) {
                    $headers.Add("$key", "$value")
                }
            }

            $binaryIsTextCheckFile = "$fileName.istext"
            if (Test-Path($binaryIsTextCheckFile)) {
                Remove-Item $binaryIsTextCheckFile -Force -EA SilentlyContinue;
            }

            if ($headers.ContainsKey("Content-Type")) {
                $contentType = $headers['Content-Type']
                if ($null -ne $contentType) {
                    if ($contentType.ToLower().Contains("text/html") -or $contentType.ToLower().Contains("text/plain")) {
                        Write-Warning "$fileName is of content type $contentType"
                        Set-Content -Path $binaryIsTextCheckFile -Value "$fileName has content type $contentType" -Encoding UTF8 -Force
                    }
                }
            }
        }
        catch {
            # not able to get content-type header
            Write-Debug "Error getting content type - $($_.Exception.Message)"
        }

        if ($fileName -and !(Split-Path $fileName)) {
            $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
        }
        elseif ((!$Passthru -and ($fileName -eq $null)) -or (($fileName -ne $null) -and (Test-Path -PathType "Container" $fileName))) {
            [string]$fileName = ([regex]'(?i)filename=(.*)$').Match( $res.Headers["Content-Disposition"] ).Groups[1].Value
            $fileName = $fileName.trim("\/""'")
            if (!$fileName) {
                $fileName = $res.ResponseUri.Segments[-1]
                $fileName = $fileName.trim("\/")
                if (!$fileName) {
                    $fileName = Read-Host "Please provide a file name"
                }
                $fileName = $fileName.trim("\/")
                if (!([IO.FileInfo]$fileName).Extension) {
                    $fileName = $fileName + "." + $res.ContentType.Split(";")[0].Split("/")[1]
                }
            }
            $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
        }
        if ($Passthru) {
            $encoding = [System.Text.Encoding]::GetEncoding( $res.CharacterSet )
            [string]$output = ""
        }

        if ($res.StatusCode -eq 401 -or $res.StatusCode -eq 403 -or $res.StatusCode -eq 404) {
            $env:ChocolateyExitCode = $res.StatusCode
            throw "Remote file either doesn't exist, is unauthorized, or is forbidden for '$url'."
        }

        if ($res.StatusCode -eq 200) {
            [long]$goal = $res.ContentLength
            $goalFormatted = Format-FileSize $goal
            $reader = $res.GetResponseStream()

            if ($fileName) {
                $fileDirectory = $([System.IO.Path]::GetDirectoryName($fileName))
                if (!(Test-Path($fileDirectory))) {
                    [System.IO.Directory]::CreateDirectory($fileDirectory) | Out-Null
                }

                try {
                    $writer = New-Object System.IO.FileStream $fileName, "Create"
                }
                catch {
                    throw $_.Exception
                }
            }

            [byte[]]$buffer = New-Object byte[] 1048576
            [long]$total = [long]$count = [long]$iterLoop = 0

            $originalEAP = $ErrorActionPreference
            $ErrorActionPreference = 'Stop'
            try {
                do {
                    $count = $reader.Read($buffer, 0, $buffer.Length);
                    if ($fileName) {
                        $writer.Write($buffer, 0, $count);
                    }

                    if ($Passthru) {
                        $output += $encoding.GetString($buffer, 0, $count)
                    }
                    elseif (!$quiet) {
                        $total += $count
                        $totalFormatted = Format-FileSize $total
                        if ($goal -gt 0 -and ++$iterLoop % 10 -eq 0) {
                            $percentComplete = [Math]::Truncate(($total / $goal) * 100)
                            Write-Progress "Downloading $url to $fileName" "Saving $totalFormatted of $goalFormatted" -Id 0 -PercentComplete $percentComplete
                        }

                        if ($total -eq $goal -and $count -eq 0) {
                            Write-Progress "Completed download of $url." "Completed download of $fileName ($goalFormatted)." -Id 0 -Completed -PercentComplete 100
                        }
                    }
                } while ($count -gt 0)
                Write-Host ""
                Write-Host "Download of $([System.IO.Path]::GetFileName($fileName)) ($goalFormatted) completed."
            }
            catch {
                throw $_.Exception
            }
            finally {
                $ErrorActionPreference = $originalEAP
            }

            $reader.Close()
            if ($fileName) {
                $writer.Flush()
                $writer.Close()
            }
            if ($Passthru) {
                $output
            }
        }
    }
    catch {
        if ($null -ne $req) {
            $req.ServicePoint.MaxIdleTime = 0
            $req.Abort();
            # ruthlessly remove $req to ensure it isn't reused
            Remove-Variable req
            Start-Sleep 1
            [GC]::Collect()
        }

        Set-PowerShellExitCode 404
        if ($env:DownloadCacheAvailable -eq 'true') {
            throw "The remote file either doesn't exist, is unauthorized, or is forbidden for url '$url'. $($_.Exception.Message) `nThis package is likely not broken for licensed users - see https://docs.chocolatey.org/en-us/features/private-cdn."
        }
        else {
            throw "The remote file either doesn't exist, is unauthorized, or is forbidden for url '$url'. $($_.Exception.Message)"
        }
    }
    finally {
        if ($null -ne $res) {
            $res.Close()
        }

        Start-Sleep 1
    }
}

# this could be cleaned up with http://learn-powershell.net/2013/02/08/powershell-and-events-object-events/

# SIG # Begin signature block
# MIIjgQYJKoZIhvcNAQcCoIIjcjCCI24CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCD9kFJKsngp7JEq
# Qf5/NNgctY2DG7yEvRLT6CGE8L2Sr6CCHXowggUwMIIEGKADAgECAhAECRgbX9W7
# ZnVTQ7VvlVAIMA0GCSqGSIb3DQEBCwUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0xMzEwMjIxMjAwMDBa
# Fw0yODEwMjIxMjAwMDBaMHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lD
# ZXJ0IFNIQTIgQXNzdXJlZCBJRCBDb2RlIFNpZ25pbmcgQ0EwggEiMA0GCSqGSIb3
# DQEBAQUAA4IBDwAwggEKAoIBAQD407Mcfw4Rr2d3B9MLMUkZz9D7RZmxOttE9X/l
# qJ3bMtdx6nadBS63j/qSQ8Cl+YnUNxnXtqrwnIal2CWsDnkoOn7p0WfTxvspJ8fT
# eyOU5JEjlpB3gvmhhCNmElQzUHSxKCa7JGnCwlLyFGeKiUXULaGj6YgsIJWuHEqH
# CN8M9eJNYBi+qsSyrnAxZjNxPqxwoqvOf+l8y5Kh5TsxHM/q8grkV7tKtel05iv+
# bMt+dDk2DZDv5LVOpKnqagqrhPOsZ061xPeM0SAlI+sIZD5SlsHyDxL0xY4PwaLo
# LFH3c7y9hbFig3NBggfkOItqcyDQD2RzPJ6fpjOp/RnfJZPRAgMBAAGjggHNMIIB
# yTASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAK
# BggrBgEFBQcDAzB5BggrBgEFBQcBAQRtMGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9v
# Y3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NhY2VydHMuZGln
# aWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNydDCBgQYDVR0fBHow
# eDA6oDigNoY0aHR0cDovL2NybDQuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJl
# ZElEUm9vdENBLmNybDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0Rp
# Z2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDBPBgNVHSAESDBGMDgGCmCGSAGG/WwA
# AgQwKjAoBggrBgEFBQcCARYcaHR0cHM6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAK
# BghghkgBhv1sAzAdBgNVHQ4EFgQUWsS5eyoKo6XqcQPAYPkt9mV1DlgwHwYDVR0j
# BBgwFoAUReuir/SSy4IxLVGLp6chnfNtyA8wDQYJKoZIhvcNAQELBQADggEBAD7s
# DVoks/Mi0RXILHwlKXaoHV0cLToaxO8wYdd+C2D9wz0PxK+L/e8q3yBVN7Dh9tGS
# dQ9RtG6ljlriXiSBThCk7j9xjmMOE0ut119EefM2FAaK95xGTlz/kLEbBw6RFfu6
# r7VRwo0kriTGxycqoSkoGjpxKAI8LpGjwCUR4pwUR6F6aGivm6dcIFzZcbEMj7uo
# +MUSaJ/PQMtARKUT8OZkDCUIQjKyNookAv4vcn4c10lFluhZHen6dGRrsutmQ9qz
# sIzV6Q3d9gEgzpkxYz0IGhizgZtPxpMQBvwHgfqL2vmCSfdibqFT+hKUGIUukpHq
# aGxEMrJmoecYpJpkUe8wggU5MIIEIaADAgECAhAKudMQ+yEr6IyBs9LC6M5RMA0G
# CSqGSIb3DQEBCwUAMHIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJ
# bmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAvBgNVBAMTKERpZ2lDZXJ0
# IFNIQTIgQXNzdXJlZCBJRCBDb2RlIFNpZ25pbmcgQ0EwHhcNMjEwNDI3MDAwMDAw
# WhcNMjQwNDMwMjM1OTU5WjB3MQswCQYDVQQGEwJVUzEPMA0GA1UECBMGS2Fuc2Fz
# MQ8wDQYDVQQHEwZUb3Bla2ExIjAgBgNVBAoTGUNob2NvbGF0ZXkgU29mdHdhcmUs
# IEluYy4xIjAgBgNVBAMTGUNob2NvbGF0ZXkgU29mdHdhcmUsIEluYy4wggEiMA0G
# CSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQChcaeNqeO3O3hzbDYYMcxvv/QNSPE4
# fpI+NGECR+FYdDO2utX9/SPxRCzWBrsgntPs/7IPk/uFZk/yTIiNoXO+cqJE45L9
# 2Ldfn6gAcwjGna/j2f/bbSFSeXW9z9lM3DJecFwXQleWR/8OKCnD+d1ZmHB0BA5v
# 0bQCfU8ZT7S0u9+KAKqyqgZrJyQiPfBVqXes9RSua7+0SVXmaBrJf9njHAf5KNFY
# /TEgm1r1zYwxfcsuE5eYdr2/suytUJpN18m9DmAdYm72va0KMxoKIBGuQy9DnaDI
# +nMiegsdhkL9sIysIin7Pcwjkwx9lRmtIqJA27Hfgb1MaL0OnkpwRY+VAgMBAAGj
# ggHEMIIBwDAfBgNVHSMEGDAWgBRaxLl7KgqjpepxA8Bg+S32ZXUOWDAdBgNVHQ4E
# FgQUTvMFGF2V6ylQalFt+afRXjSaBIMwDgYDVR0PAQH/BAQDAgeAMBMGA1UdJQQM
# MAoGCCsGAQUFBwMDMHcGA1UdHwRwMG4wNaAzoDGGL2h0dHA6Ly9jcmwzLmRpZ2lj
# ZXJ0LmNvbS9zaGEyLWFzc3VyZWQtY3MtZzEuY3JsMDWgM6Axhi9odHRwOi8vY3Js
# NC5kaWdpY2VydC5jb20vc2hhMi1hc3N1cmVkLWNzLWcxLmNybDBLBgNVHSAERDBC
# MDYGCWCGSAGG/WwDATApMCcGCCsGAQUFBwIBFhtodHRwOi8vd3d3LmRpZ2ljZXJ0
# LmNvbS9DUFMwCAYGZ4EMAQQBMIGEBggrBgEFBQcBAQR4MHYwJAYIKwYBBQUHMAGG
# GGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBOBggrBgEFBQcwAoZCaHR0cDovL2Nh
# Y2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0U0hBMkFzc3VyZWRJRENvZGVTaWdu
# aW5nQ0EuY3J0MAwGA1UdEwEB/wQCMAAwDQYJKoZIhvcNAQELBQADggEBAKFxncHA
# zDFesUJXaM21qMRk5+nIZcDuISfGgJcDjMHsRLw7na5Yn7IhiNY+OsKnPVkfPhL/
# MNXSHG6on+IpxiB2/Bry9thqKvpQdPBe8mFN0ctJDgrSceyRC5SA9EiO22J3YNe0
# yVEKAG+Yk2A/WhKBzCCpRskMlRr7KeLm6DvAgvDsMfkKtePMl2PraON+tFNpc2b1
# LTKT4okiU5uAWpjYAt9sYBsKTeZb5NJt0ZQ3akEEIAQs63/mSDAZlzMOJMWNK/yv
# 4NU5CiPVcohJ0WjUJUIrAMmAVlZ2h8NhCXJOv28cHWEgPks/zqdDdIhJfDF+ALd1
# 0JTBrwCNcYQG68AwggWNMIIEdaADAgECAhAOmxiO+dAt5+/bUOIIQBhaMA0GCSqG
# SIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNVBAMTG0RpZ2lDZXJ0IEFz
# c3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBaFw0zMTExMDkyMzU5NTla
# MGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsT
# EHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9v
# dCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAL/mkHNo3rvkXUo8
# MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3EMB/zG6Q4FutWxpdtHauy
# efLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKyunWZanMylNEQRBAu34Lz
# B4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsFxl7sWxq868nPzaw0QF+x
# embud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU15zHL2pNe3I6PgNq2kZhA
# kHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJBMtfbBHMqbpEBfCFM1Lyu
# GwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObURWBf3JFxGj2T3wWmIdph2
# PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6nj3cAORFJYm2mkQZK37A
# lLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxBYKqxYxhElRp2Yn72gLD7
# 6GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5SUUd0viastkF13nqsX40/
# ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+xq4aLT8LWRV+dIPyhHsXA
# j6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIBNjAPBgNVHRMBAf8EBTAD
# AQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwPTzAfBgNVHSMEGDAWgBRF
# 66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMCAYYweQYIKwYBBQUHAQEE
# bTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wQwYIKwYB
# BQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3Vy
# ZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0aHR0cDovL2NybDMuZGln
# aWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDARBgNVHSAECjAI
# MAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0NcVec4X6CjdBs9thbX979X
# B72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnovLbc47/T/gLn4offyct4k
# vFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65ZyoUi0mcudT6cGAxN3J0TU
# 53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFWjuyk1T3osdz9HNj0d1pc
# VIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPFmCLBsln1VWvPJ6tsds5v
# Iy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9ztwGpn1eqXijiuZQwggau
# MIIElqADAgECAhAHNje3JFR82Ees/ShmKl5bMA0GCSqGSIb3DQEBCwUAMGIxCzAJ
# BgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5k
# aWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9vdCBHNDAe
# Fw0yMjAzMjMwMDAwMDBaFw0zNzAzMjIyMzU5NTlaMGMxCzAJBgNVBAYTAlVTMRcw
# FQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgVHJ1c3Rl
# ZCBHNCBSU0E0MDk2IFNIQTI1NiBUaW1lU3RhbXBpbmcgQ0EwggIiMA0GCSqGSIb3
# DQEBAQUAA4ICDwAwggIKAoICAQDGhjUGSbPBPXJJUVXHJQPE8pE3qZdRodbSg9Ge
# TKJtoLDMg/la9hGhRBVCX6SI82j6ffOciQt/nR+eDzMfUBMLJnOWbfhXqAJ9/UO0
# hNoR8XOxs+4rgISKIhjf69o9xBd/qxkrPkLcZ47qUT3w1lbU5ygt69OxtXXnHwZl
# jZQp09nsad/ZkIdGAHvbREGJ3HxqV3rwN3mfXazL6IRktFLydkf3YYMZ3V+0VAsh
# aG43IbtArF+y3kp9zvU5EmfvDqVjbOSmxR3NNg1c1eYbqMFkdECnwHLFuk4fsbVY
# TXn+149zk6wsOeKlSNbwsDETqVcplicu9Yemj052FVUmcJgmf6AaRyBD40NjgHt1
# biclkJg6OBGz9vae5jtb7IHeIhTZgirHkr+g3uM+onP65x9abJTyUpURK1h0QCir
# c0PO30qhHGs4xSnzyqqWc0Jon7ZGs506o9UD4L/wojzKQtwYSH8UNM/STKvvmz3+
# DrhkKvp1KCRB7UK/BZxmSVJQ9FHzNklNiyDSLFc1eSuo80VgvCONWPfcYd6T/jnA
# +bIwpUzX6ZhKWD7TA4j+s4/TXkt2ElGTyYwMO1uKIqjBJgj5FBASA31fI7tk42Pg
# puE+9sJ0sj8eCXbsq11GdeJgo1gJASgADoRU7s7pXcheMBK9Rp6103a50g5rmQzS
# M7TNsQIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQU
# uhbZbU2FL3MpdpovdYxqII+eyG8wHwYDVR0jBBgwFoAU7NfjgtJxXWRM3y5nP+e6
# mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoGCCsGAQUFBwMIMHcGCCsG
# AQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29t
# MEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNl
# cnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDigNqA0hjJodHRwOi8vY3Js
# My5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNybDAgBgNVHSAE
# GTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZIhvcNAQELBQADggIBAH1Z
# jsCTtm+YqUQiAX5m1tghQuGwGC4QTRPPMFPOvxj7x1Bd4ksp+3CKDaopafxpwc8d
# B+k+YMjYC+VcW9dth/qEICU0MWfNthKWb8RQTGIdDAiCqBa9qVbPFXONASIlzpVp
# P0d3+3J0FNf/q0+KLHqrhc1DX+1gtqpPkWaeLJ7giqzl/Yy8ZCaHbJK9nXzQcAp8
# 76i8dU+6WvepELJd6f8oVInw1YpxdmXazPByoyP6wCeCRK6ZJxurJB4mwbfeKuv2
# nrF5mYGjVoarCkXJ38SNoOeY+/umnXKvxMfBwWpx2cYTgAnEtp/Nh4cku0+jSbl3
# ZpHxcpzpSwJSpzd+k1OsOx0ISQ+UzTl63f8lY5knLD0/a6fxZsNBzU+2QJshIUDQ
# txMkzdwdeDrknq3lNHGS1yZr5Dhzq6YBT70/O3itTK37xJV77QpfMzmHQXh6OOmc
# 4d0j/R0o08f56PGYX/sr2H7yRp11LB4nLCbbbxV7HhmLNriT1ObyF5lZynDwN7+Y
# AN8gFk8n+2BnFqFmut1VwDophrCYoCvtlUG3OtUVmDG0YgkPCr2B2RP+v6TR81fZ
# vAT6gt4y3wSJ8ADNXcL50CN/AAvkdgIm2fBldkKmKYcJRyvmfxqkhQ/8mJb2VVQr
# H4D6wPIOK+XW+6kvRBVK5xMOHds3OBqhK/bt1nz8MIIGwjCCBKqgAwIBAgIQBUSv
# 85SdCDmmv9s/X+VhFjANBgkqhkiG9w0BAQsFADBjMQswCQYDVQQGEwJVUzEXMBUG
# A1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0IFRydXN0ZWQg
# RzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMB4XDTIzMDcxNDAwMDAw
# MFoXDTM0MTAxMzIzNTk1OVowSDELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lD
# ZXJ0LCBJbmMuMSAwHgYDVQQDExdEaWdpQ2VydCBUaW1lc3RhbXAgMjAyMzCCAiIw
# DQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAKNTRYcdg45brD5UsyPgz5/X5dLn
# XaEOCdwvSKOXejsqnGfcYhVYwamTEafNqrJq3RApih5iY2nTWJw1cb86l+uUUI8c
# IOrHmjsvlmbjaedp/lvD1isgHMGXlLSlUIHyz8sHpjBoyoNC2vx/CSSUpIIa2mq6
# 2DvKXd4ZGIX7ReoNYWyd/nFexAaaPPDFLnkPG2ZS48jWPl/aQ9OE9dDH9kgtXkV1
# lnX+3RChG4PBuOZSlbVH13gpOWvgeFmX40QrStWVzu8IF+qCZE3/I+PKhu60pCFk
# cOvV5aDaY7Mu6QXuqvYk9R28mxyyt1/f8O52fTGZZUdVnUokL6wrl76f5P17cz4y
# 7lI0+9S769SgLDSb495uZBkHNwGRDxy1Uc2qTGaDiGhiu7xBG3gZbeTZD+BYQfvY
# sSzhUa+0rRUGFOpiCBPTaR58ZE2dD9/O0V6MqqtQFcmzyrzXxDtoRKOlO0L9c33u
# 3Qr/eTQQfqZcClhMAD6FaXXHg2TWdc2PEnZWpST618RrIbroHzSYLzrqawGw9/sq
# hux7UjipmAmhcbJsca8+uG+W1eEQE/5hRwqM/vC2x9XH3mwk8L9CgsqgcT2ckpME
# tGlwJw1Pt7U20clfCKRwo+wK8REuZODLIivK8SgTIUlRfgZm0zu++uuRONhRB8qU
# t+JQofM604qDy0B7AgMBAAGjggGLMIIBhzAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0T
# AQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEFBQcDCDAgBgNVHSAEGTAXMAgGBmeB
# DAEEAjALBglghkgBhv1sBwEwHwYDVR0jBBgwFoAUuhbZbU2FL3MpdpovdYxqII+e
# yG8wHQYDVR0OBBYEFKW27xPn783QZKHVVqllMaPe1eNJMFoGA1UdHwRTMFEwT6BN
# oEuGSWh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNFJT
# QTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcmwwgZAGCCsGAQUFBwEBBIGDMIGA
# MCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wWAYIKwYBBQUH
# MAKGTGh0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRH
# NFJTQTQwOTZTSEEyNTZUaW1lU3RhbXBpbmdDQS5jcnQwDQYJKoZIhvcNAQELBQAD
# ggIBAIEa1t6gqbWYF7xwjU+KPGic2CX/yyzkzepdIpLsjCICqbjPgKjZ5+PF7SaC
# inEvGN1Ott5s1+FgnCvt7T1IjrhrunxdvcJhN2hJd6PrkKoS1yeF844ektrCQDif
# XcigLiV4JZ0qBXqEKZi2V3mP2yZWK7Dzp703DNiYdk9WuVLCtp04qYHnbUFcjGnR
# uSvExnvPnPp44pMadqJpddNQ5EQSviANnqlE0PjlSXcIWiHFtM+YlRpUurm8wWkZ
# us8W8oM3NG6wQSbd3lqXTzON1I13fXVFoaVYJmoDRd7ZULVQjK9WvUzF4UbFKNOt
# 50MAcN7MmJ4ZiQPq1JE3701S88lgIcRWR+3aEUuMMsOI5ljitts++V+wQtaP4xeR
# 0arAVeOGv6wnLEHQmjNKqDbUuXKWfpd5OEhfysLcPTLfddY2Z1qJ+Panx+VPNTwA
# vb6cKmx5AdzaROY63jg7B145WPR8czFVoIARyxQMfq68/qTreWWqaNYiyjvrmoI1
# VygWy2nyMpqy0tg6uLFGhmu6F/3Ed2wVbK6rr3M66ElGt9V/zLY4wNjsHPW2obhD
# LN9OTH0eaHDAdwrUAuBcYLso/zjlUlrWrBciI0707NMX+1Br/wd3H3GXREHJuEbT
# bDJ8WC9nR2XlG3O2mflrLAZG70Ee8PBf4NvZrZCARK+AEEGKMYIFXTCCBVkCAQEw
# gYYwcjELMAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IEluYzEZMBcGA1UE
# CxMQd3d3LmRpZ2ljZXJ0LmNvbTExMC8GA1UEAxMoRGlnaUNlcnQgU0hBMiBBc3N1
# cmVkIElEIENvZGUgU2lnbmluZyBDQQIQCrnTEPshK+iMgbPSwujOUTANBglghkgB
# ZQMEAgEFAKCBhDAYBgorBgEEAYI3AgEMMQowCKACgAChAoAAMBkGCSqGSIb3DQEJ
# AzEMBgorBgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgorBgEEAYI3AgEVMC8G
# CSqGSIb3DQEJBDEiBCDYwmPfW1D4BflE/QGdr0D3SL8dIbR5dxOkPom2ta9bjjAN
# BgkqhkiG9w0BAQEFAASCAQAUQuIiim3voSxcf7BiDCjA1fUrxgbCBgLXD2VZodNe
# mcndN6L3fb+SMQmNjPznbjT/HYp+AjQlxPmNfuvDjrBKuyqZLWQNwlzz2h++VCIQ
# zdr7FvGHHzXgbX6WBS8qsrhk90ZEjXRGpD48EiBNGKE2Abe7q7UuKIZpyZCzOkGO
# MvYLzGmM9qwgmj3E86a7n7b9ejFgS/x74/AgESVe1i3+w6tQlokLvoAYSG6VueRs
# Eggl8pyRpeb/PSCioQExpLawWNZ9RQgTOii+pDUExYEIeOAWFEav/KAoH/POcQWf
# touZibjaChDvEgYBwidm8aCTpkvDa6E6xBquGtXXllsEoYIDIDCCAxwGCSqGSIb3
# DQEJBjGCAw0wggMJAgEBMHcwYzELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lD
# ZXJ0LCBJbmMuMTswOQYDVQQDEzJEaWdpQ2VydCBUcnVzdGVkIEc0IFJTQTQwOTYg
# U0hBMjU2IFRpbWVTdGFtcGluZyBDQQIQBUSv85SdCDmmv9s/X+VhFjANBglghkgB
# ZQMEAgEFAKBpMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkF
# MQ8XDTIzMDgwODA3MDgzMVowLwYJKoZIhvcNAQkEMSIEIN+1lgDG7R7XGpZD9Ib9
# FkD4FSjBdDnswI5udAZ+oqmwMA0GCSqGSIb3DQEBAQUABIICAA2eUpM+LLiSSzxA
# c3DOrdurY8BaM/G7KzQvFqPwbeUXKjJOkhdCm0noSZReDbv8qTumNmkXHWejgGkw
# Q0L3SpQ4Sa73391Jn4ByTrYVXRgaX6/y6ORrlpUIQR3ze4nPuvKfD3RHsdaahYrr
# 3kg33sSDNLntDewWJInOFg/mwMO9342TbXIv3v8q51GLod+d8LKDg5BPk9qcBS3g
# HeP76VK93DpVxFscTRT+TNKC1D9WKhBzceftafNPB+6/G4o9ask4wgA844d2umPh
# pJrLi/oYsCp5i2tpXIBDHcjaP6fhRu1SUvSGysTPZZhpz70EN27lsFG6x804TTou
# 5qWAy1mmrAZKx++VjqsOqNAyK9xlDA9EEOAfDms+nl7SthYTKD5u1zoijonlG/Mi
# LBaC67Ll04fCTM2vXT/C3h1qDTWMaxu1Ehjz0QObB2WKsjqiHQUo8J2exJhIH8s4
# IF6C/8/rp+XB7CEgbC72Aos8lwk+EGZtdAIyFArHhUnmbSdSPmt1zVTCz6qfhNwr
# TEbubNwLipuYXE8VDwVcOFE6hJFmdDrfI37LAHXwUH9T9eObZYIemdhbixaxsZtk
# dSdXX20hKwObomvirlSadATsjGv4HVtWcF9GmEY5VmWaArOAEdoAg+ua06iDWqnq
# NpsTHE5iq5WQf7VN4QvsGaEEXzoP
# SIG # End signature block
