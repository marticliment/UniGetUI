<#
.SYNOPSIS
  Download file with choco internals

.DESCRIPTION
  This function will download a file from specified url and return it as a string.
  This command should be a replacement for ubiquitous WebClient in install scripts.

  The benefit of using this command instead of WebClient is that it correctly handles
  system or explicit proxy.

.EXAMPLE
  PS C:\> $s = Get-WebContent "http://example.com"
  PS C:\> $s -match 'Example Domain'
  True

  First command downloads html content from http://example.com and stores it in $s.
  Now you can parse and match it as a string.

.EXAMPLE
  PS C:\> $opts = @{ Headers = @{ Referer = 'http://google.com' } }
  PS C:\> $s = Get-WebContent -url "http://example.com" -options $opts

  You can set headers for http request this way.

.INPUTS
  None

.OUTPUTS
  System.String

.NOTES
  This function can only return string content.
  If you want to download a binary content, please use Get-WebFile.

.LINK
  Get-WebFile
#>
function Get-WebContent {
    [CmdletBinding()]
    param(
        # Url to download
        [string]$Url,

        # Additional options for http request.For now only Headers property supported.
        [hashtable]$Options,

        # Allows splatting with arguments that do not apply and future expansion. Do not use directly.
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $IgnoredArguments
    )

    $filePath =  get_temp_filepath
    Get-WebFile -Url $Url -FileName $filePath -Options $Options 3>$null

    $fileContent = Get-Content $filePath -ReadCount 0 | Out-String
    Remove-Item $filePath

    $fileContent
}

function get_temp_filepath() {
    $tempDir = Get-PackageCacheLocation
    $fileName = [System.IO.Path]::GetRandomFileName()
    Join-Path $tempDir $fileName
}

