
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://github.com/marticliment/ElevenClock/releases/download/4.0.1/ElevenClock.Installer.exe' 

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'exe' 
  url           = $url
  url64bit      = $url64

  softwareName  = 'elevenclock*'

  checksum      = 'c3bfabc3208aef9f9e4b4d9b90f26beb43b8b22e22ea73957774fa80cd5098f6'
  checksumType  = 'sha256'


  silentArgs    = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-"
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs
