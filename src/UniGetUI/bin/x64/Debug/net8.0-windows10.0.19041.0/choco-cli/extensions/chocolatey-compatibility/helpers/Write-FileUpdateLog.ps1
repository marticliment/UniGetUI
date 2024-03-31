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

function Write-FileUpdateLog {
  <#
  .SYNOPSIS
  DEPRECATED - DO NOT USE. This function was removed in Chocolatey v1.

  .DESCRIPTION
  Original: Monitors a location and writes changes to a log file.
  Present: Will output a warning about the cmdlet should not be used.

  .NOTES
  DEPRECATED - Functionality was removed in Chocolatey v1.
  As the recommendation is to no longer use this function, no updates
  will be accepted to fix any problems.

  .PARAMETER IgnoredArguments
  Allows splatting with arguments that do not apply. Do not use directly.
  #>
  param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [object[]] $IgnoredArguments
  )

  Write-Warning "Write-FileUpdateLog was removed in Chocolatey CLI v1, and have no functionality any more. If you are the maintainer, please remove it from from your package file."
  Write-Warning "If you are not the maintainer, please contact the maintainer to update the $env:ChocolateyPackageName package."
}
