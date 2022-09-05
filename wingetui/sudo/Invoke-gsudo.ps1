<#
.SYNOPSIS
Executes a ScriptBlock in a new elevated instance of powershell, using `gsudo`.

.DESCRIPTION
Serializes a scriptblock and executes it in an elevated powershell. 
The ScriptBlock runs in a different process, so it can´t read/write variables from the invoking scope.
If you reference a variable in a scriptblock using the `$using:variableName` it will be replaced with it´s serialized value.
The elevated command can accept input from the pipeline with $Input. It will be serialized, so size matters.
The script result is serialized, sent back to the non-elevated instance, and returned.
Optionally you can check for "$LastExitCode -eq 999" to find out if gsudo failed to elevate (UAC popup cancelled) 

.PARAMETER ScriptBlock
Specifies a ScriptBlock that will be run in an elevated PowerShell instance. '
e.g. { Get-Process Notepad }

.PARAMETER ArgumentList
An list of elements that will be accesible inside the script as: $args

.PARAMETER NoElevate
A test mode where the command is executed out-of-scope but without real elevation: The serialization/marshalling is still done.

.INPUTS
You can pipe any object to Invoke-Gsudo. It will be serialized and available in the userScript as $Input.

.OUTPUTS
Whatever the scriptblock returns. Use explicit "return" in your scriptblock. 

.EXAMPLE
PS> Get-Process notepad | Invoke-gsudo { Stop-Process }

PS> Invoke-gsudo { return Get-Content 'C:\My Secret Folder\My Secret.txt'}

PS> $a=1; $b = Invoke-gsudo { $using:a+10 }; Write-Host "Sum returned: $b";
Sum returned: 11

.LINK
https://github.com/gerardog/gsudo

    #>
[CmdletBinding(DefaultParameterSetName = 'None')]
param
(
    # The script block to execute in an elevated context.
    [Parameter(Mandatory = $true, Position = 0)]
    [System.Management.Automation.ScriptBlock]
    $ScriptBlock,

    # Optional argument list for the program or the script block.
    [Parameter(Mandatory = $false, Position = 1)]
    [System.Object[]]
    $ArgumentList,

    [Parameter(ValueFromPipeline)]
    [pscustomobject]
    $InputObject,

	[Parameter()]
	[switch]
	$LoadProfile = $false,
	
	#test mode
	[Parameter()]
	[switch]
	$NoElevate = $false
)

# Replaces $using:variableName with the serialized value of $variableName.
# Credit: https://stackoverflow.com/a/60583163/97471
Function Serialize-Scriptblock
{ 	
    param(
        [scriptblock]$Scriptblock
    )
    $rxp = '(?<!`)\$using:(?<var>\w+)'
    $ssb = $Scriptblock.ToString()
    $cb = {
        $v = (Get-Variable -Name $args[0].Groups['var'] -ValueOnly)
		if ($v -eq $null)
		{ '$null' }
		else 
		{ 
			"`$([System.Management.Automation.PSSerializer]::Deserialize([System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{0}'))))" -f [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Management.Automation.PSSerializer]::Serialize($v)))
		}		
    }
    $sb = [RegEx]::Replace($ssb, $rxp, $cb, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase);
	return $sb;
}

Function Deserialize-Scriptblock
{
    param(
		[string]$sb
    )
    [Scriptblock]::Create($sb).GetNewClosure()
}

$expectingInput = $myInvocation.expectingInput;
$debug = if ($PSBoundParameters['Debug']) {$true} else {$false};
$userScriptBlock = Serialize-Scriptblock $ScriptBlock
$InputArray = $Input
$location = (Get-Location).Path;

if($PSBoundParameters["ErrorAction"]) {
	#Write-Verbose -verbose "Received ErrorAction $($PSBoundParameters["ErrorAction"])"
	$errorAction = $PSBoundParameters["ErrorAction"] | Out-String
} else {
	#Write-Verbose -verbose "ErrorActionPreference is $ErrorActionPreference"
	$errorAction = $ErrorActionPreference | Out-String
}

$remoteCmd = Serialize-Scriptblock {
	$InputObject = $using:InputArray;
	$argumentList = $using:ArgumentList;
	$expectingInput = $using:expectingInput;
	$sb = [Scriptblock]::Create($using:userScriptBlock);
	Set-Location $using:location;
	$ErrorActionPreference=$using:errorAction;

	if ($expectingInput) { 
		try { 
			($InputObject | Invoke-Command $sb -ArgumentList $argumentList)
		} catch {throw $_} 
	} else { 
		try{
			(Invoke-Command $sb -ArgumentList $argumentList)
		} catch {throw $_} 
	} 
}

if ($Debug) {
	Write-Debug "User ScriptBlock : $userScriptBlock"
	Write-Debug "Full Script to run on the isolated instance: { $remoteCmd }" 
} 

if($NoElevate) { 
	# We could invoke using Invoke-Command:
	#		$result = $InputObject | Invoke-Command (Deserialize-Scriptblock $remoteCmd) -ArgumentList $ArgumentList
	# Or run in a Job to ensure same variable isolation:

	$job = Start-Job -ScriptBlock (Deserialize-Scriptblock $remoteCmd) -errorAction $errorAction | Wait-Job; 
	$result = Receive-Job $job -errorAction $errorAction
} else {
	$pwsh = ("""$([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)""") # Get same running powershell EXE.
	
	if ($host.Name -notmatch 'consolehost') { # Workaround for PowerShell ISE, or PS hosted inside other process
		if ($PSVersionTable.PSVersion.Major -le 5) 
			{ $pwsh = "powershell.exe" } 
		else 
			{ $pwsh = "pwsh.exe" }
	} 
	
	$windowTitle = $host.ui.RawUI.WindowTitle;

	$dbg = if ($debug) {"--debug "} else {" "}
	$NoProfile = if ($gsudoLoadProfile -or $LoadProfile) {""} else {"-NoProfile "}
	
	$arguments = "-d $dbg--LogLevel Error $pwsh -nologo $NoProfile-NonInteractive -OutputFormat Xml -InputFormat Text -encodedCommand IAAoACQAaQBuAHAAdQB0ACAAfAAgAE8AdQB0AC0AUwB0AHIAaQBuAGcAKQAgAHwAIABpAGUAeAAgAA==".Split(" ")

	# Must Read: https://stackoverflow.com/questions/68136128/how-do-i-call-the-powershell-cli-robustly-with-respect-to-character-encoding-i?noredirect=1&lq=1
	$result = $remoteCmd | & gsudo.exe $arguments *>&1
	
	$host.ui.RawUI.WindowTitle = $windowTitle;
}

ForEach ($item in $result)
{
	if (
	$item.Exception.SerializedRemoteException.WasThrownFromThrowStatement -or
	$item.Exception.WasThrownFromThrowStatement
	)
	{
		throw $item
	}
	if ($item -is [System.Management.Automation.ErrorRecord])
	{ 
		Write-Error $item
	}
	else 
	{ 
		Write-Output $item; 
	}
}

# SIG # Begin signature block
# MIIr5QYJKoZIhvcNAQcCoIIr1jCCK9ICAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCAz7JjDLkVzlIX8
# hWn4IEadUf8iyqJkJ568HsjAC2ttOqCCJQUwggVvMIIEV6ADAgECAhBI/JO0YFWU
# jTanyYqJ1pQWMA0GCSqGSIb3DQEBDAUAMHsxCzAJBgNVBAYTAkdCMRswGQYDVQQI
# DBJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcMB1NhbGZvcmQxGjAYBgNVBAoM
# EUNvbW9kbyBDQSBMaW1pdGVkMSEwHwYDVQQDDBhBQUEgQ2VydGlmaWNhdGUgU2Vy
# dmljZXMwHhcNMjEwNTI1MDAwMDAwWhcNMjgxMjMxMjM1OTU5WjBWMQswCQYDVQQG
# EwJHQjEYMBYGA1UEChMPU2VjdGlnbyBMaW1pdGVkMS0wKwYDVQQDEyRTZWN0aWdv
# IFB1YmxpYyBDb2RlIFNpZ25pbmcgUm9vdCBSNDYwggIiMA0GCSqGSIb3DQEBAQUA
# A4ICDwAwggIKAoICAQCN55QSIgQkdC7/FiMCkoq2rjaFrEfUI5ErPtx94jGgUW+s
# hJHjUoq14pbe0IdjJImK/+8Skzt9u7aKvb0Ffyeba2XTpQxpsbxJOZrxbW6q5KCD
# J9qaDStQ6Utbs7hkNqR+Sj2pcaths3OzPAsM79szV+W+NDfjlxtd/R8SPYIDdub7
# P2bSlDFp+m2zNKzBenjcklDyZMeqLQSrw2rq4C+np9xu1+j/2iGrQL+57g2extme
# me/G3h+pDHazJyCh1rr9gOcB0u/rgimVcI3/uxXP/tEPNqIuTzKQdEZrRzUTdwUz
# T2MuuC3hv2WnBGsY2HH6zAjybYmZELGt2z4s5KoYsMYHAXVn3m3pY2MeNn9pib6q
# RT5uWl+PoVvLnTCGMOgDs0DGDQ84zWeoU4j6uDBl+m/H5x2xg3RpPqzEaDux5mcz
# mrYI4IAFSEDu9oJkRqj1c7AGlfJsZZ+/VVscnFcax3hGfHCqlBuCF6yH6bbJDoEc
# QNYWFyn8XJwYK+pF9e+91WdPKF4F7pBMeufG9ND8+s0+MkYTIDaKBOq3qgdGnA2T
# OglmmVhcKaO5DKYwODzQRjY1fJy67sPV+Qp2+n4FG0DKkjXp1XrRtX8ArqmQqsV/
# AZwQsRb8zG4Y3G9i/qZQp7h7uJ0VP/4gDHXIIloTlRmQAOka1cKG8eOO7F/05QID
# AQABo4IBEjCCAQ4wHwYDVR0jBBgwFoAUoBEKIz6W8Qfs4q8p74Klf9AwpLQwHQYD
# VR0OBBYEFDLrkpr/NZZILyhAQnAgNpFcF4XmMA4GA1UdDwEB/wQEAwIBhjAPBgNV
# HRMBAf8EBTADAQH/MBMGA1UdJQQMMAoGCCsGAQUFBwMDMBsGA1UdIAQUMBIwBgYE
# VR0gADAIBgZngQwBBAEwQwYDVR0fBDwwOjA4oDagNIYyaHR0cDovL2NybC5jb21v
# ZG9jYS5jb20vQUFBQ2VydGlmaWNhdGVTZXJ2aWNlcy5jcmwwNAYIKwYBBQUHAQEE
# KDAmMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5jb21vZG9jYS5jb20wDQYJKoZI
# hvcNAQEMBQADggEBABK/oe+LdJqYRLhpRrWrJAoMpIpnuDqBv0WKfVIHqI0fTiGF
# OaNrXi0ghr8QuK55O1PNtPvYRL4G2VxjZ9RAFodEhnIq1jIV9RKDwvnhXRFAZ/ZC
# J3LFI+ICOBpMIOLbAffNRk8monxmwFE2tokCVMf8WPtsAO7+mKYulaEMUykfb9gZ
# pk+e96wJ6l2CxouvgKe9gUhShDHaMuwV5KZMPWw5c9QLhTkg4IUaaOGnSDip0TYl
# d8GNGRbFiExmfS9jzpjoad+sPKhdnckcW67Y8y90z7h+9teDnRGWYpquRRPaf9xH
# +9/DUp/mBlXpnYzyOmJRvOwkDynUWICE5EV7WtgwggWNMIIEdaADAgECAhAOmxiO
# +dAt5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYD
# VQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAi
# BgNVBAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAw
# MDBaFw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdp
# Q2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERp
# Z2lDZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCC
# AgoCggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsb
# hA3EMB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iT
# cMKyunWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGb
# NOsFxl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclP
# XuU15zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCr
# VYJBMtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFP
# ObURWBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTv
# kpI6nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWM
# cCxBYKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls
# 5Q5SUUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBR
# a2+xq4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6
# MIIBNjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qY
# rhwPTzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8E
# BAMCAYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5k
# aWdpY2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0
# LmNvbS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDig
# NoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9v
# dENBLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCg
# v0NcVec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQT
# SnovLbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh
# 65ZyoUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSw
# uKFWjuyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAO
# QGPFmCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjD
# TZ9ztwGpn1eqXijiuZQwggYaMIIEAqADAgECAhBiHW0MUgGeO5B5FSCJIRwKMA0G
# CSqGSIb3DQEBDAUAMFYxCzAJBgNVBAYTAkdCMRgwFgYDVQQKEw9TZWN0aWdvIExp
# bWl0ZWQxLTArBgNVBAMTJFNlY3RpZ28gUHVibGljIENvZGUgU2lnbmluZyBSb290
# IFI0NjAeFw0yMTAzMjIwMDAwMDBaFw0zNjAzMjEyMzU5NTlaMFQxCzAJBgNVBAYT
# AkdCMRgwFgYDVQQKEw9TZWN0aWdvIExpbWl0ZWQxKzApBgNVBAMTIlNlY3RpZ28g
# UHVibGljIENvZGUgU2lnbmluZyBDQSBSMzYwggGiMA0GCSqGSIb3DQEBAQUAA4IB
# jwAwggGKAoIBgQCbK51T+jU/jmAGQ2rAz/V/9shTUxjIztNsfvxYB5UXeWUzCxEe
# AEZGbEN4QMgCsJLZUKhWThj/yPqy0iSZhXkZ6Pg2A2NVDgFigOMYzB2OKhdqfWGV
# oYW3haT29PSTahYkwmMv0b/83nbeECbiMXhSOtbam+/36F09fy1tsB8je/RV0mIk
# 8XL/tfCK6cPuYHE215wzrK0h1SWHTxPbPuYkRdkP05ZwmRmTnAO5/arnY83jeNzh
# P06ShdnRqtZlV59+8yv+KIhE5ILMqgOZYAENHNX9SJDm+qxp4VqpB3MV/h53yl41
# aHU5pledi9lCBbH9JeIkNFICiVHNkRmq4TpxtwfvjsUedyz8rNyfQJy/aOs5b4s+
# ac7IH60B+Ja7TVM+EKv1WuTGwcLmoU3FpOFMbmPj8pz44MPZ1f9+YEQIQty/NQd/
# 2yGgW+ufflcZ/ZE9o1M7a5Jnqf2i2/uMSWymR8r2oQBMdlyh2n5HirY4jKnFH/9g
# Rvd+QOfdRrJZb1sCAwEAAaOCAWQwggFgMB8GA1UdIwQYMBaAFDLrkpr/NZZILyhA
# QnAgNpFcF4XmMB0GA1UdDgQWBBQPKssghyi47G9IritUpimqF6TNDDAOBgNVHQ8B
# Af8EBAMCAYYwEgYDVR0TAQH/BAgwBgEB/wIBADATBgNVHSUEDDAKBggrBgEFBQcD
# AzAbBgNVHSAEFDASMAYGBFUdIAAwCAYGZ4EMAQQBMEsGA1UdHwREMEIwQKA+oDyG
# Omh0dHA6Ly9jcmwuc2VjdGlnby5jb20vU2VjdGlnb1B1YmxpY0NvZGVTaWduaW5n
# Um9vdFI0Ni5jcmwwewYIKwYBBQUHAQEEbzBtMEYGCCsGAQUFBzAChjpodHRwOi8v
# Y3J0LnNlY3RpZ28uY29tL1NlY3RpZ29QdWJsaWNDb2RlU2lnbmluZ1Jvb3RSNDYu
# cDdjMCMGCCsGAQUFBzABhhdodHRwOi8vb2NzcC5zZWN0aWdvLmNvbTANBgkqhkiG
# 9w0BAQwFAAOCAgEABv+C4XdjNm57oRUgmxP/BP6YdURhw1aVcdGRP4Wh60BAscjW
# 4HL9hcpkOTz5jUug2oeunbYAowbFC2AKK+cMcXIBD0ZdOaWTsyNyBBsMLHqafvIh
# rCymlaS98+QpoBCyKppP0OcxYEdU0hpsaqBBIZOtBajjcw5+w/KeFvPYfLF/ldYp
# mlG+vd0xqlqd099iChnyIMvY5HexjO2AmtsbpVn0OhNcWbWDRF/3sBp6fWXhz7Dc
# ML4iTAWS+MVXeNLj1lJziVKEoroGs9Mlizg0bUMbOalOhOfCipnx8CaLZeVme5yE
# Lg09Jlo8BMe80jO37PU8ejfkP9/uPak7VLwELKxAMcJszkyeiaerlphwoKx1uHRz
# NyE6bxuSKcutisqmKL5OTunAvtONEoteSiabkPVSZ2z76mKnzAfZxCl/3dq3dUNw
# 4rg3sTCggkHSRqTqlLMS7gjrhTqBmzu1L90Y1KWN/Y5JKdGvspbOrTfOXyXvmPL6
# E52z1NZJ6ctuMFBQZH3pwWvqURR8AgQdULUvrxjUYbHHj95Ejza63zdrEcxWLDX6
# xWls/GDnVNueKjWUH3fTv1Y8Wdho698YADR7TNx8X8z2Bev6SivBBOHY+uqiirZt
# g0y9ShQoPzmCcn63Syatatvx157YK9hlcPmVoa1oDE5/L9Uo2bC5a4CH2RwwggZj
# MIIEy6ADAgECAhEArSSHt1l2lEEDfGcsJtCFkzANBgkqhkiG9w0BAQwFADBUMQsw
# CQYDVQQGEwJHQjEYMBYGA1UEChMPU2VjdGlnbyBMaW1pdGVkMSswKQYDVQQDEyJT
# ZWN0aWdvIFB1YmxpYyBDb2RlIFNpZ25pbmcgQ0EgUjM2MB4XDTIyMDgzMTAwMDAw
# MFoXDTIzMDgzMTIzNTk1OVowWjELMAkGA1UEBhMCQVIxFTATBgNVBAgMDEJ1ZW5v
# cyBBaXJlczEZMBcGA1UECgwQR2VyYXJkbyBHcmlnbm9saTEZMBcGA1UEAwwQR2Vy
# YXJkbyBHcmlnbm9saTCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBALf1
# uQ1SMmuXo3wQciXN92OzFggUJ4rK4unOcm7fNZT1D4N4vI32joOmDjhb5oUQ4DOP
# bE11it65I0R6wp1ilSg2/ItoYlOnR3OEWu56LIuYqZoSYL4SVS81sU8RMq44sQSK
# VJulyjwgK1q2p3FQFqLQoupMgf/kS9Di5V80HZMfGuP/VG9u1hL25+ruuvyHb/4Y
# qQ7h0qTPNEbrKVeydPER2yHWQVGNZFPYrY7P7gbAkkoXqr5byEM35dQpl79SrX4w
# aMB2EtqOjkJlcikT4PCvTt2cLy/LiTMMXznE++YZ933xMMZ2zrxxAXohPOdQPm1t
# SwXPsfvvANyumYRy5poSLIIPu+7o8fWfRM2jQQkrW6v0hzX0UIwRVkvo316H6aS0
# RHp1I8LgzXYB7G6XjYdGGYFLK4vCvNRCK5yDsMskHyJRmBS1nzcCEAE82a2pQP2d
# wSQBFtKyhl7vBbQcNkCkXI+ZAxN3hKFPMAGRFCaJVYAV3g1fYMoAID1b4va//Fjy
# YsmYABcxdM3frCHIuWg8iqrXPNnGX5UYubs3/npi2Mu4K7ZB12yfV3khfOyRiBz9
# q8iYmEG8ZgVeF6SOLpwDQFO0R3VypVhJZEOfDuT+KOEZVRnP2wJURJpe0ZeQ3v0/
# CqY4cUeVYZ03mTWsdhtA1Uw+1rG8Y9E5gJwErWTbAgMBAAGjggGoMIIBpDAfBgNV
# HSMEGDAWgBQPKssghyi47G9IritUpimqF6TNDDAdBgNVHQ4EFgQUFF6RicCkxiAG
# ELk00hac9X9mhSIwDgYDVR0PAQH/BAQDAgeAMAwGA1UdEwEB/wQCMAAwEwYDVR0l
# BAwwCgYIKwYBBQUHAwMwSgYDVR0gBEMwQTA1BgwrBgEEAbIxAQIBAwIwJTAjBggr
# BgEFBQcCARYXaHR0cHM6Ly9zZWN0aWdvLmNvbS9DUFMwCAYGZ4EMAQQBMEkGA1Ud
# HwRCMEAwPqA8oDqGOGh0dHA6Ly9jcmwuc2VjdGlnby5jb20vU2VjdGlnb1B1Ymxp
# Y0NvZGVTaWduaW5nQ0FSMzYuY3JsMHkGCCsGAQUFBwEBBG0wazBEBggrBgEFBQcw
# AoY4aHR0cDovL2NydC5zZWN0aWdvLmNvbS9TZWN0aWdvUHVibGljQ29kZVNpZ25p
# bmdDQVIzNi5jcnQwIwYIKwYBBQUHMAGGF2h0dHA6Ly9vY3NwLnNlY3RpZ28uY29t
# MB0GA1UdEQQWMBSBEmdlcmFyZG9nQGdtYWlsLmNvbTANBgkqhkiG9w0BAQwFAAOC
# AYEAcYE+UdbFGDM9uwzeC5wAcEP9hfPWOkOd0oH0jJsPFe00k1CdEIC71/b7LCQX
# lLNRZHiKlxgfpM+r7td763raFyPyrEYz7/UBYLl2yznseXQgTy9fHY9df/T6z8I9
# X09hyKHRYj0uUhtdWMvMZd+/OVb4IxP2smsB9isJITXLbrXckJQx/pNggQjNREp9
# jKgcL2ejb+Trq+C9J8xlvoIEnvYmZNfHioyjHC5vKC3jMpfXfCGiandFSGNKfp2Y
# n20u4q6WR8hUv18OTAnasMmkAUnLTfPZGkCR/kQTr8wwmQoKwI4jQnVX72C3zhRF
# wsiJfdX+2s+JlW1trdfjTMAkKMoryPYvKR/US380N4tWnqoxVHULcDqFfixjKimn
# Y3tdBWbmsW4gfsHfkHSOBnxsLfX7kAV48yXoNJbfpingwHwc12/372dx5PWsjTV3
# ZzL6zfrs/LfGdX5n7UzzfilSVLcIDV5IZtejI9OQGczLwNHHK5J10S8H9MMqP/HF
# alHJMIIGrjCCBJagAwIBAgIQBzY3tyRUfNhHrP0oZipeWzANBgkqhkiG9w0BAQsF
# ADBiMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQL
# ExB3d3cuZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdpQ2VydCBUcnVzdGVkIFJv
# b3QgRzQwHhcNMjIwMzIzMDAwMDAwWhcNMzcwMzIyMjM1OTU5WjBjMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xOzA5BgNVBAMTMkRpZ2lDZXJ0
# IFRydXN0ZWQgRzQgUlNBNDA5NiBTSEEyNTYgVGltZVN0YW1waW5nIENBMIICIjAN
# BgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAxoY1BkmzwT1ySVFVxyUDxPKRN6mX
# UaHW0oPRnkyibaCwzIP5WvYRoUQVQl+kiPNo+n3znIkLf50fng8zH1ATCyZzlm34
# V6gCff1DtITaEfFzsbPuK4CEiiIY3+vaPcQXf6sZKz5C3GeO6lE98NZW1OcoLevT
# sbV15x8GZY2UKdPZ7Gnf2ZCHRgB720RBidx8ald68Dd5n12sy+iEZLRS8nZH92GD
# Gd1ftFQLIWhuNyG7QKxfst5Kfc71ORJn7w6lY2zkpsUdzTYNXNXmG6jBZHRAp8By
# xbpOH7G1WE15/tePc5OsLDnipUjW8LAxE6lXKZYnLvWHpo9OdhVVJnCYJn+gGkcg
# Q+NDY4B7dW4nJZCYOjgRs/b2nuY7W+yB3iIU2YIqx5K/oN7jPqJz+ucfWmyU8lKV
# EStYdEAoq3NDzt9KoRxrOMUp88qqlnNCaJ+2RrOdOqPVA+C/8KI8ykLcGEh/FDTP
# 0kyr75s9/g64ZCr6dSgkQe1CvwWcZklSUPRR8zZJTYsg0ixXNXkrqPNFYLwjjVj3
# 3GHek/45wPmyMKVM1+mYSlg+0wOI/rOP015LdhJRk8mMDDtbiiKowSYI+RQQEgN9
# XyO7ZONj4KbhPvbCdLI/Hgl27KtdRnXiYKNYCQEoAA6EVO7O6V3IXjASvUaetdN2
# udIOa5kM0jO0zbECAwEAAaOCAV0wggFZMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYD
# VR0OBBYEFLoW2W1NhS9zKXaaL3WMaiCPnshvMB8GA1UdIwQYMBaAFOzX44LScV1k
# TN8uZz/nupiuHA9PMA4GA1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcD
# CDB3BggrBgEFBQcBAQRrMGkwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2lj
# ZXJ0LmNvbTBBBggrBgEFBQcwAoY1aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29t
# L0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcnQwQwYDVR0fBDwwOjA4oDagNIYyaHR0
# cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZFJvb3RHNC5jcmww
# IAYDVR0gBBkwFzAIBgZngQwBBAIwCwYJYIZIAYb9bAcBMA0GCSqGSIb3DQEBCwUA
# A4ICAQB9WY7Ak7ZvmKlEIgF+ZtbYIULhsBguEE0TzzBTzr8Y+8dQXeJLKftwig2q
# KWn8acHPHQfpPmDI2AvlXFvXbYf6hCAlNDFnzbYSlm/EUExiHQwIgqgWvalWzxVz
# jQEiJc6VaT9Hd/tydBTX/6tPiix6q4XNQ1/tYLaqT5Fmniye4Iqs5f2MvGQmh2yS
# vZ180HAKfO+ovHVPulr3qRCyXen/KFSJ8NWKcXZl2szwcqMj+sAngkSumScbqyQe
# JsG33irr9p6xeZmBo1aGqwpFyd/EjaDnmPv7pp1yr8THwcFqcdnGE4AJxLafzYeH
# JLtPo0m5d2aR8XKc6UsCUqc3fpNTrDsdCEkPlM05et3/JWOZJyw9P2un8WbDQc1P
# tkCbISFA0LcTJM3cHXg65J6t5TRxktcma+Q4c6umAU+9Pzt4rUyt+8SVe+0KXzM5
# h0F4ejjpnOHdI/0dKNPH+ejxmF/7K9h+8kaddSweJywm228Vex4Ziza4k9Tm8heZ
# Wcpw8De/mADfIBZPJ/tgZxahZrrdVcA6KYawmKAr7ZVBtzrVFZgxtGIJDwq9gdkT
# /r+k0fNX2bwE+oLeMt8EifAAzV3C+dAjfwAL5HYCJtnwZXZCpimHCUcr5n8apIUP
# /JiW9lVUKx+A+sDyDivl1vupL0QVSucTDh3bNzgaoSv27dZ8/DCCBsYwggSuoAMC
# AQICEAp6SoieyZlCkAZjOE2Gl50wDQYJKoZIhvcNAQELBQAwYzELMAkGA1UEBhMC
# VVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMTswOQYDVQQDEzJEaWdpQ2VydCBU
# cnVzdGVkIEc0IFJTQTQwOTYgU0hBMjU2IFRpbWVTdGFtcGluZyBDQTAeFw0yMjAz
# MjkwMDAwMDBaFw0zMzAzMTQyMzU5NTlaMEwxCzAJBgNVBAYTAlVTMRcwFQYDVQQK
# Ew5EaWdpQ2VydCwgSW5jLjEkMCIGA1UEAxMbRGlnaUNlcnQgVGltZXN0YW1wIDIw
# MjIgLSAyMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAuSqWI6ZcvF/W
# SfAVghj0M+7MXGzj4CUu0jHkPECu+6vE43hdflw26vUljUOjges4Y/k8iGnePNIw
# UQ0xB7pGbumjS0joiUF/DbLW+YTxmD4LvwqEEnFsoWImAdPOw2z9rDt+3Cocqb0w
# xhbY2rzrsvGD0Z/NCcW5QWpFQiNBWvhg02UsPn5evZan8Pyx9PQoz0J5HzvHkwdo
# aOVENFJfD1De1FksRHTAMkcZW+KYLo/Qyj//xmfPPJOVToTpdhiYmREUxSsMoDPb
# TSSF6IKU4S8D7n+FAsmG4dUYFLcERfPgOL2ivXpxmOwV5/0u7NKbAIqsHY07gGj+
# 0FmYJs7g7a5/KC7CnuALS8gI0TK7g/ojPNn/0oy790Mj3+fDWgVifnAs5SuyPWPq
# yK6BIGtDich+X7Aa3Rm9n3RBCq+5jgnTdKEvsFR2wZBPlOyGYf/bES+SAzDOMLeL
# D11Es0MdI1DNkdcvnfv8zbHBp8QOxO9APhk6AtQxqWmgSfl14ZvoaORqDI/r5LEh
# e4ZnWH5/H+gr5BSyFtaBocraMJBr7m91wLA2JrIIO/+9vn9sExjfxm2keUmti39h
# hwVo99Rw40KV6J67m0uy4rZBPeevpxooya1hsKBBGBlO7UebYZXtPgthWuo+epiS
# Uc0/yUTngIspQnL3ebLdhOon7v59emsCAwEAAaOCAYswggGHMA4GA1UdDwEB/wQE
# AwIHgDAMBgNVHRMBAf8EAjAAMBYGA1UdJQEB/wQMMAoGCCsGAQUFBwMIMCAGA1Ud
# IAQZMBcwCAYGZ4EMAQQCMAsGCWCGSAGG/WwHATAfBgNVHSMEGDAWgBS6FtltTYUv
# cyl2mi91jGogj57IbzAdBgNVHQ4EFgQUjWS3iSH+VlhEhGGn6m8cNo/drw0wWgYD
# VR0fBFMwUTBPoE2gS4ZJaHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0
# VHJ1c3RlZEc0UlNBNDA5NlNIQTI1NlRpbWVTdGFtcGluZ0NBLmNybDCBkAYIKwYB
# BQUHAQEEgYMwgYAwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNv
# bTBYBggrBgEFBQcwAoZMaHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lD
# ZXJ0VHJ1c3RlZEc0UlNBNDA5NlNIQTI1NlRpbWVTdGFtcGluZ0NBLmNydDANBgkq
# hkiG9w0BAQsFAAOCAgEADS0jdKbR9fjqS5k/AeT2DOSvFp3Zs4yXgimcQ28BLas4
# tXARv4QZiz9d5YZPvpM63io5WjlO2IRZpbwbmKrobO/RSGkZOFvPiTkdcHDZTt8j
# ImzV3/ZZy6HC6kx2yqHcoSuWuJtVqRprfdH1AglPgtalc4jEmIDf7kmVt7PMxafu
# DuHvHjiKn+8RyTFKWLbfOHzL+lz35FO/bgp8ftfemNUpZYkPopzAZfQBImXH6l50
# pls1klB89Bemh2RPPkaJFmMga8vye9A140pwSKm25x1gvQQiFSVwBnKpRDtpRxHT
# 7unHoD5PELkwNuTzqmkJqIt+ZKJllBH7bjLx9bs4rc3AkxHVMnhKSzcqTPNc3LaF
# wLtwMFV41pj+VG1/calIGnjdRncuG3rAM4r4SiiMEqhzzy350yPynhngDZQooOvb
# GlGglYKOKGukzp123qlzqkhqWUOuX+r4DwZCnd8GaJb+KqB0W2Nm3mssuHiqTXBt
# 8CzxBxV+NbTmtQyimaXXFWs1DoXW4CzM4AwkuHxSCx6ZfO/IyMWMWGmvqz3hz8x9
# Fa4Uv4px38qXsdhH6hyF4EVOEhwUKVjMb9N/y77BDkpvIJyu2XMyWQjnLZKhGhH+
# MpimXSuX4IvTnMxttQ2uR2M4RxdbbxPaahBuH0m3RFu0CAqHWlkEdhGhp3cCExwx
# ggY2MIIGMgIBATBpMFQxCzAJBgNVBAYTAkdCMRgwFgYDVQQKEw9TZWN0aWdvIExp
# bWl0ZWQxKzApBgNVBAMTIlNlY3RpZ28gUHVibGljIENvZGUgU2lnbmluZyBDQSBS
# MzYCEQCtJIe3WXaUQQN8Zywm0IWTMA0GCWCGSAFlAwQCAQUAoHwwEAYKKwYBBAGC
# NwIBDDECMAAwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYBBAGCNwIB
# CzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIEIKbRYfAOpQj2dsAW0md1
# H8dVs1w6FXo3/hOuf5bMzbFYMA0GCSqGSIb3DQEBAQUABIICAJsrMSkkp6JRCdUQ
# 3u4aevwn4KLAQ+Ez8h0T23Yd4l8ABJXYgaOjWoEltyxfdxCK/XKbMfouzW9ddGn6
# HbEPwpP7ghIAZtPF0LTJnBJ8ZhZ1yyDOcfmWmfFn6dOAQ5iOb0Cnt49YbzJQ33jU
# dNh4IQNtHMhJFfqXRtSiuFRX8xoP5fV+VsjtlCL6403HOpCZ2ExFZiR38BEB7KJC
# cPSO7GKocQtlknqV7wsGqGg/iFfUXXMFX+x4E16sQgtHeRkCVbrRCMLnZt/OihKG
# nrwPjhLY8yBiJwhEw+iaO5w8NBptp8oGQ5+zQCYsn0Qf7PAhJaBcHnMffg/o7Dvf
# dtC8rn3tRcEnzjKP4WnB4nwur8RQ5oKkAJePdLLgKHauG8GYyJI8RYn0O0VaQvWV
# mynPvYCGzgdEJdyqVPc3w3ImHRD8DlUaru2gSVL5MI8KtYYOMJgh4T8+77m8A4sO
# vuFL0iQbhsXNVpAAsu2DXMNp5Koz+Jjp49/iKlIZIoxscBjyms8o4AqaoKtKPueE
# mGIZXFT5IU++UaSSC4NzxBiW0UD6eOhsfpYwV7lV7HjK8z1i3yzGakJr3u98+kae
# YgTHbMgaZ3ViIAONjYjNypKMwIMF7jPM10XBPwnaF9AqYH87gi70JFEm7OwFp3vQ
# Tj/3fYQ/5ZHw2az8Zrs+bzW2GRPEoYIDIDCCAxwGCSqGSIb3DQEJBjGCAw0wggMJ
# AgEBMHcwYzELMAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMTsw
# OQYDVQQDEzJEaWdpQ2VydCBUcnVzdGVkIEc0IFJTQTQwOTYgU0hBMjU2IFRpbWVT
# dGFtcGluZyBDQQIQCnpKiJ7JmUKQBmM4TYaXnTANBglghkgBZQMEAgEFAKBpMBgG
# CSqGSIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTIyMDkwNDAx
# NTk1MlowLwYJKoZIhvcNAQkEMSIEIHha+Qg4NHiLmnFURbaNwZ6X3A6w4SkLQksV
# 87n+5OSgMA0GCSqGSIb3DQEBAQUABIICAFPjwxg1MG9r3OTJBo2r6EcILnTdnHqr
# EmN9818xB39B5IkS4zBvRawAQrCX1hZJkH1J1ikjYh/Wq71G9nXHcbPJulZj+ia+
# QndmloiDSLh63DqYUEwMx289qQHZr1QhgDNkDotRKmacndmBXj3qlsUOF8rbRXXe
# IqLCz40xRSSs/iYpFkfmXP2UT/xyO0ZmYQ3YMqrtFNpEgXxEOloR49aDksQ49Ych
# IoJZQLkEh+GkbaxTsMwmoD22XcDWRuXo9tWZP5dvWtMHGgT5S4ZJgNw0ZJjOw9Jj
# T7774dysUc49F6TluVE6vzCTe3uQpAE119hwZTaTgulHg1Kue9nqAmFZNQqZPK8N
# KwuuCOzmOrZMeec2GptnN975B97/TlnGX0ftH5vfYM9JziuSEcAKjDfKfEidbSyq
# PVFlyok7+DONlwVRM6m8cWJUA7/c8RKhCa/NdxPbDPREcYLU0lVtgWaZKqsCAhGV
# 9usLADZCSnJRW6wNAg4iRQX3v8vJfu1VO8tyIi5aIXTq6EMAs0UHt5TTWMTihlkx
# AyoQ1yp8Q8Ql5kDq8MsDEZQ17GxS+wzul6xOFhBjr0XlNQVicu1C2wy7yAsDeiSL
# kxMs04uDWXTz7uSi6tESuMr2WDiqQvgkZ+szmucWjB3g5FZ8X1CEFTBRK88AQj4+
# khLmN+ee/xTH
# SIG # End signature block
