$scriptDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
. (Join-Path -Path $scriptDirectory -ChildPath 'Get-DefaultChocolateyLocalFilePath.ps1')
. (Join-Path -Path $scriptDirectory -ChildPath 'Get-NativeInstallerExitCode.ps1')
. (Join-Path -Path $scriptDirectory -ChildPath 'Install-ChocolateyInstallPackageAndHandleExitCode.ps1')
. (Join-Path -Path $scriptDirectory -ChildPath 'Set-PowerShellExitCode.ps1')

$ERROR_SUCCESS = 0
$ERROR_SUCCESS_REBOOT_REQUIRED = 3010
$STATUS_ACCESS_VIOLATION = 0xC0000005

function Get-SafeLogPath
{
    $logPath = $Env:TEMP
    if ($logPath -like '\\*')
    {
        # .NET installer does not like logging to a network share (https://github.com/jberezanski/ChocolateyPackages/issues/15)
        $candidates = @("$Env:LOCALAPPDATA\Temp\chocolatey", "$Env:LOCALAPPDATA\Temp", "$Env:USERPROFILE\AppData\Local\Temp\chocolatey", "$Env:USERPROFILE\AppData\Local\Temp", "$Env:SystemRoot\TEMP", $scriptDirectory)
        foreach ($candidate in $candidates)
        {
            if ((Test-Path -Path $candidate) -and $candidate -notlike '\\*')
            {
                Write-Verbose "Using '$candidate' as log path because `$Env:TEMP points to a network share, which may cause the installation to fail"
                $logPath = $candidate
                break
            }
        }
    }

    return $logPath
}

function Test-Installed
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [int] $Release
    )

    $props = Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue
    return $null -ne $props -and $props.Release -ge $Release
}

function Invoke-CommandWithTempPath
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $TempPath,
        [Parameter(Mandatory = $true)] [scriptblock] $ScriptBlock
    )

    $oldTemp = $Env:TEMP
    if ($Env:TEMP -ne $TempPath)
    {
        Write-Debug "Changing `$Env:TEMP from '$oldTemp' to '$TempPath'"
        $Env:TEMP = $TempPath
    }

    try
    {
        & $ScriptBlock
    }
    finally
    {
        if ($Env:TEMP -ne $oldTemp)
        {
            Write-Debug "Changing `$Env:TEMP back to '$oldTemp'"
            $Env:TEMP = $oldTemp
        }
    }
}

function Install-DotNetFrameworkOrDevPack
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $PackageName,
        [Parameter(Mandatory = $true)] [string] $Version,
        [Parameter(Mandatory = $true)] [string] $Url,
        [Parameter(Mandatory = $true)] [string] $Checksum,
        [Parameter(Mandatory = $true)] [string] $ChecksumType,
        [Parameter(Mandatory = $true)] [scriptblock] $ExitCodeHandler
    )

    $originalFileName = Split-Path -Leaf -Path ([uri]$Url).LocalPath
    $downloadFilePath = Get-DefaultChocolateyLocalFilePath -OriginalFileName $originalFileName
    $downloadArguments = @{
        packageName = $PackageName
        fileFullPath = $downloadFilePath
        url = $Url
        checksum = $Checksum
        checksumType = $ChecksumType
        url64 = $Url
        checksum64 = $Checksum
        checksumType64 = $ChecksumType
    }

    Get-ChocolateyWebFile @downloadArguments | Out-Null

    $safeLogPath = Get-SafeLogPath
    $installerExeArguments = @{
        packageName = $PackageName
        fileType = 'exe'
        file = $downloadFilePath
        silentArgs = ('/Quiet /NoRestart /Log "{0}\{1}_{2}_{3:yyyyMMddHHmmss}.log"' -f $safeLogPath, $PackageName, $Version, (Get-Date))
        validExitCodes = @(
            $ERROR_SUCCESS # success
            $ERROR_SUCCESS_REBOOT_REQUIRED # success, restart required
        )
    }

    Invoke-CommandWithTempPath -TempPath $safeLogPath -ScriptBlock { Install-ChocolateyInstallPackageAndHandleExitCode @installerExeArguments -ExitCodeHandler $ExitCodeHandler }
}

function Install-DotNetFramework
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $PackageName, # = 'netfx-4.8'
        [Parameter(Mandatory = $true)] [int] $Release, # = 528040
        [Parameter(Mandatory = $true)] [string] $Version, # = '4.8'
        [Parameter(Mandatory = $true)] [string] $ProductNameWithVersion, # = "Microsoft .NET Framework $version"
        [Parameter(Mandatory = $true)] [string] $Url, # = 'https://download.visualstudio.microsoft.com/download/pr/7afca223-55d2-470a-8edc-6a1739ae3252/abd170b4b0ec15ad0222a809b761a036/NDP48-x86-x64-AllOS-ENU.exe'
        [Parameter(Mandatory = $true)] [string] $Checksum, # = '95889D6DE3F2070C07790AD6CF2000D33D9A1BDFC6A381725AB82AB1C314FD53'
        [Parameter(Mandatory = $true)] [string] $ChecksumType # = 'sha256'
    )

    if (Test-Installed -Release $Release) {
        Write-Host "$ProductNameWithVersion or later is already installed."
        return
    }

    $exitCodeHandler = {
        $installResult = $_
        $exitCode = $installResult.ExitCode
        if ($exitCode -eq $ERROR_SUCCESS_REBOOT_REQUIRED)
        {
            Write-Warning "$ProductNameWithVersion has been installed, but a reboot is required to finalize the installation. Until the computer is rebooted, dependent packages may fail to install or function properly."
        }
        elseif ($exitCode -eq $ERROR_SUCCESS)
        {
            Write-Verbose "$ProductNameWithVersion has been installed successfully, a reboot is not required."
        }
        elseif ($null -eq $exitCode)
        {
            Write-Warning "Package installation has finished, but this Chocolatey version does not provide the installer exit code. A restart may be required to finalize $productNameWithVersion installation."
        }
    }

    $innerArgs = New-Object System.Collections.Hashtable -ArgumentList @($PSBoundParameters)
    [void]$innerArgs.Remove('Release')
    [void]$innerArgs.Remove('ProductNameWithVersion')
    Install-DotNetFrameworkOrDevPack @innerArgs -ExitCodeHandler $exitCodeHandler
}

function Install-DotNetDevPack
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory = $true)] [string] $PackageName, # = 'netfx-4.8-devpack'
        [Parameter(Mandatory = $true)] [string] $Version, # = '4.8'
        [Parameter(Mandatory = $true)] [string] $ProductNameWithVersion, # = "Microsoft .NET Framework $version Developer Pack early access build 3745"
        [Parameter(Mandatory = $true)] [string] $Url, # = 'https://download.visualstudio.microsoft.com/download/pr/9854b5f2-2341-4136-ad7d-1d881ab8d603/e3a011f2a41a59b086f78d64e1c7a3fc/NDP48-DevPack-ENU.exe'
        [Parameter(Mandatory = $true)] [string] $Checksum, # = '67979C8FBA2CD244712A31A7FE323FD8BD69AA7971F152F8233CB109A7260F06'
        [Parameter(Mandatory = $true)] [string] $ChecksumType # = 'sha256'
    )

    $exitCodeHandler = {
        $installResult = $_
        $exitCode = $installResult.ExitCode
        if ($exitCode -eq $ERROR_SUCCESS_REBOOT_REQUIRED)
        {
            Write-Warning "$ProductNameWithVersion has been installed, but a reboot is required to finalize the installation. Until the computer is rebooted, dependent packages may fail to install or function properly."
        }
        elseif ($exitCode -eq $ERROR_SUCCESS)
        {
            Write-Verbose "$ProductNameWithVersion has been installed successfully, a reboot is not required."
        }
        elseif ($null -eq $exitCode)
        {
            Write-Warning "Package installation has finished, but this Chocolatey version does not provide the installer exit code. A restart may be required to finalize $productNameWithVersion installation."
        }
        elseif ($exitCode -eq $STATUS_ACCESS_VIOLATION)
        {
            # installer crash (access violation), but may occur at the very end, after the devpack is installed
            if (Test-Path -Path 'Env:\ProgramFiles(x86)')
            {
                $programFiles32 = ${Env:ProgramFiles(x86)}
            }
            else
            {
                $programFiles32 = ${Env:ProgramFiles}
            }

            $mscorlibPath = "$programFiles32\Reference Assemblies\Microsoft\Framework\.NETFramework\v${version}\mscorlib.dll"
            Write-Warning "The native installer crashed, checking if it managed to install the devpack before the crash"
            Write-Debug "Testing existence of $mscorlibPath"
            if (Test-Path -Path $mscorlibPath)
            {
                Write-Verbose "mscorlib.dll found: $mscorlibPath"
                Write-Verbose 'This probably means the devpack got installed successfully, despite the installer crash'
                $installResult.ShouldFailInstallation = $false
                $installResult.ExitCode = $ERROR_SUCCESS # to avoid triggering failure detection in choco.exe
            }
            else
            {
                Write-Verbose "mscorlib.dll not found in expected location: $mscorlibPath"
                Write-Verbose 'This probably means the installer crashed before it could fully install the devpack'
            }
        }
    }

    $innerArgs = New-Object System.Collections.Hashtable -ArgumentList @($PSBoundParameters)
    [void]$innerArgs.Remove('ProductNameWithVersion')
    Install-DotNetFrameworkOrDevPack @innerArgs -ExitCodeHandler $exitCodeHandler
}
