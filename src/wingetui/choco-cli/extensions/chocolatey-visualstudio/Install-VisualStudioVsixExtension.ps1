function Install-VisualStudioVsixExtension
{
<#
.SYNOPSIS
Installs or updates a Visual Studio VSIX extension.

.DESCRIPTION
This function installs a Visual Studio VSIX extension by invoking
the Visual Studio extension installer (VSIXInstaller.exe).
The latest installer version found on the machine is used.
The extension is installed in all Visual Studio instances present on the
machine the extension is compatible with.

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.
Alias: Name

.PARAMETER VsixUrl
The URL of the package to be installed.
Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.
Alias: Url

.PARAMETER Checksum
The checksum hash value of the Url resource. This allows a checksum to
be validated for files that are not local. The checksum type is covered
by ChecksumType.

.PARAMETER ChecksumType
The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.
MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.
The recommendation is to use at least SHA256.

.PARAMETER VsVersion
NOT USED. The newest available VSIXInstaller.exe program
will be used and the extension will be installed in all supported
Visual Studio products present on the machine.
Alias: VisualStudioVersion

.PARAMETER Options
Additional options passed to Get-ChocolateyWebFile when downloading
remote resources, such as custom HTTP headers.

.PARAMETER File
Same as VsixUrl, will be used if VsixUrl is empty. Provided for
compatibility reasons.
#>
    [CmdletBinding()]
    Param
    (
        [Alias('Name')] [string] $PackageName,
        [Alias('Url')] [string] $VsixUrl,
        [string] $Checksum,
        [string] $ChecksumType,
        [Alias('VisualStudioVersion')] [int] $VsVersion,
        [hashtable] $Options,
        [string] $File
    )
    if ($null -ne $Env:ChocolateyPackageDebug)
    {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
        Write-Warning "VerbosePreference and DebugPreference set to Continue due to the presence of ChocolateyPackageDebug environment variable"
    }
    Write-Debug "Running 'Install-VisualStudioVsixExtension' for $PackageName with VsixUrl:'$VsixUrl' Checksum:$Checksum ChecksumType:$ChecksumType VsVersion:$VsVersion Options:$Options File:$File";

    $packageParameters = Parse-Parameters $env:chocolateyPackageParameters

    if ($VsVersion -ne 0)
    {
        Write-Warning "VsVersion is not supported yet. The extension will be installed in all compatible Visual Studio instances present."
    }

    if ($VsixUrl -eq '')
    {
        $VsixUrl = $File
    }

    $vsixInstaller = Get-VisualStudioVsixInstaller -Latest
    Write-Verbose ('Found VSIXInstaller version {0}: {1}' -f $vsixInstaller.Version, $vsixInstaller.Path)

    $vsixPath = Get-VSWebFile `
        -PackageName $PackageName `
        -DefaultFileName "${PackageName}.vsix" `
        -FileDescription 'vsix file' `
        -Url $VsixUrl `
        -Checksum $Checksum `
        -ChecksumType $ChecksumType `
        -Options $Options

    $logFileName = 'VSIXInstaller_{0}_{1:yyyyMMddHHmmss}.log' -f $PackageName, (Get-Date)
    $argumentSet = @{
        'quiet' = $null
        'admin' = $null
        'logFile' = $logFileName
    }

    Merge-AdditionalArguments -Arguments $argumentSet -AdditionalArguments $packageParameters
    Remove-NegatedArguments -Arguments $argumentSet -RemoveNegativeSwitches
    $exeArgsString = ConvertTo-ArgumentString -Arguments $argumentSet -Syntax 'VSIXInstaller' -FinalUnstructuredArguments @($vsixPath)

    Write-Host ('Installing {0} using VSIXInstaller version {1}' -f $PackageName, $vsixInstaller.Version)
    $validExitCodes = @(0, 1001)
    $exitCode = Start-VSChocolateyProcessAsAdmin -statements $exeArgsString -exeToRun $vsixInstaller.Path -validExitCodes $validExitCodes
    if ($exitCode -eq 1001)
    {
        Write-Host "Visual Studio extension '${PackageName}' is already installed."
    }
    else
    {
        Write-Host "Visual Studio extension '${PackageName}' has been installed in all supported Visual Studio instances."
    }
}
