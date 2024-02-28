# Copyright © 2017 - 2021 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Install-ChocolateyDesktopLink {
  <#
  .SYNOPSIS
  DEPRECATED - This adds a shortcut on the desktop to the specified file path.

  .DESCRIPTION
  Determines the desktop folder and creates a shortcut to the specified
  file path. Will not throw errors if it fails.
  It is recommended you use `Install-ChocolateyShortcut` instead of this
  method as this has been removed upstream.

  .NOTES
  This function was removed and deprecated in Chocolatey CLI in favor of using
  https://docs.chocolatey.org/en-us/create/functions/install-chocolateyshortcut.
  As the recommendation is to no longer use this function, no updates
  will be accepted to fix any problems.
  Compared to original function that was available in Chocolatey CLI, this
  implementation do not include any error handling, nor any promise that shortcuts
  will be created in the same way as Chocolatey CLI did it.

  .INPUTS
  None

  .OUTPUTS
  None

  .PARAMETER TargetFilePath
  This is the location to the application/executable file that you want to
  add a shortcut to on the desktop.  This is mandatory.

  .PARAMETER IgnoredArguments
  Allows splatting with arguments that do not apply. Do not use directly.

  .LINK
  Install-ChocolateyShortcut
#>
  param(
    [parameter(Mandatory = $true, Position = 0)][string] $targetFilePath,
    [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
  )
  Write-Warning "Install-ChocolateyDesktopLink was removed in Chocolatey CLI v1. If you are the package maintainer, please use Install-ChocolateyShortcut instead."
  Write-Warning "If you are not the maintainer, please contact the maintainer to update the $env:ChocolateyPackageName package."
  Write-Warning "There is no guarantee that this function works as expected compared to original function in Chocolatey CLI pre 1.0.0"

  if (!$targetFilePath) {
    throw "Install-ChocolateyDesktopLink - `$targetFilePath can not be null."
  }

  if (!(Test-Path($targetFilePath))) {
    Write-Warning "'$targetFilePath' does not exist. If it is not created the shortcut will not be valid."
  }

  Write-Debug "Creating Shortcut..."

  try {
    $desktop = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
    if (!(Test-Path($desktop))) {
      [System.IO.Directory]::CreateDirectory($desktop) | Out-Null
    }
    $link = Join-Path $desktop "$([System.IO.Path]::GetFileName($targetFilePath)).lnk"
    $workingDirectory = $([System.IO.Path]::GetDirectoryName($targetFilePath))

    $wshshell = New-Object -ComObject WScript.Shell
    $lnk = $wshshell.CreateShortcut($link)
    $lnk.TargetPath = $targetFilePath
    $lnk.WorkingDirectory = $workingDirectory
    $lnk.Save()

    Write-Host "Desktop Shortcut created pointing at `'$targetFilePath`'."
  }
  catch {
    Write-Warning "Unable to create desktop link. Error captured was $($_.Exception.Message)."
  }
}
