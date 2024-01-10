# Generates customizations file. Returns its path
function Generate-AdminFile
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [hashtable] $Parameters,
        [Parameter(Mandatory = $true)]
        [string] $DefaultAdminFile,
        [Parameter(Mandatory = $true)]
        [string] $PackageName,
        [Parameter(Mandatory = $true)]
        [PSObject] $InstallSourceInfo,
        [string] $Url,
        [string] $Checksum,
        [string] $ChecksumType
    )
    Write-Debug "Running 'Generate-AdminFile' with Parameters:'$Parameters', DefaultAdminFile:'$DefaultAdminFile', PackageName:'$PackageName', InstallSourceInfo:'$InstallSourceinfo', Url:'$Url', Checksum:'$Checksum', ChecksumType:'$ChecksumType'";
    $adminFile = $Parameters['AdminFile']
    $features = $Parameters['Features']
    $regenerateAdminFile = $Parameters.ContainsKey('RegenerateAdminFile')
    if ($adminFile -eq '' -and !$features)
    {
        return $null
    }

    $localAdminFile = Join-Path $Env:TEMP "${PackageName}_AdminDeployment.xml"
    if (Test-Path $localAdminFile)
    {
        Remove-Item $localAdminFile
    }

    if ($adminFile)
    {
        if (Test-Path $adminFile)
        {
            Copy-Item $adminFile $localAdminFile -force
        }
        else
        {
            if ($null -ne ($adminFile -as [System.URI]).AbsoluteURI)
            {
                Get-ChocolateyWebFile 'adminFile' $localAdminFile $adminFile
            }
            else
            {
                throw 'Invalid AdminFile setting.'
            }
        }
        Write-Verbose "Using provided AdminFile: $adminFile"
    }
    elseif ($features)
    {
        if (-not $regenerateAdminFile)
        {
            Copy-Item $DefaultAdminFile $localAdminFile -force
        }
        else
        {
            Write-Host "Generating a new admin file using the VS installer"
            $regeneratedAdminFile = $DefaultAdminFile -replace '\.xml$', '.regenerated.xml'
            $logFilePath = Join-Path $Env:TEMP ('{0}_{1:yyyyMMddHHmmss}.log' -f $PackageName, (Get-Date))
            $silentArgs = "/Quiet /NoRestart /Log ""$logFilePath"" /CreateAdminFile ""$regeneratedAdminFile"""
            $arguments = @{
                packageName = $PackageName
                silentArgs = $silentArgs
                url = $Url
                checksum = $Checksum
                checksumType = $ChecksumType
                logFilePath = $logFilePath
                assumeNewVS2017Installer = $false
                installerFilePath = $installSourceInfo.InstallerFilePath
            }
            $argumentsDump = ($arguments.GetEnumerator() | ForEach-Object { '-{0}:''{1}''' -f $_.Key,"$($_.Value)" }) -join ' '
            Write-Debug "Install-VSChocolateyPackage $argumentsDump"
            Install-VSChocolateyPackage @arguments

            Copy-Item $regeneratedAdminFile $localAdminFile -force
        }
    }

    return $localAdminFile
}
