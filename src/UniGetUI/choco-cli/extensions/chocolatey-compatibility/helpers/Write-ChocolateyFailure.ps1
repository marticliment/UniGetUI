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

function Write-ChocolateyFailure {
  <#
  .SYNOPSIS
  DEPRECATED - DO NOT USE. This function was removed in Chocolatey v1.

  .DESCRIPTION
  Throws the error message as an error.

  .NOTES
  This has been deprecated and was no longer useful as of 0.9.9,
  in 1.0.0 this function was removed entirely from Chocolatey CLI. Instead
  please just use `throw $_.Exception` when catching errors. Although
  try/catch is no longer necessary unless you want to do some error
  handling.
  As the recommendation is to no longer use this function, no updates
  will be accepted to fix any problems.

  .INPUTS
  None

  .OUTPUTS
  None

  .PARAMETER PackageName
  The name of the package - while this is an arbitrary value, it's
  recommended that it matches the package id.

  .PARAMETER FailureMessage
  The message to throw an error with.

  .PARAMETER IgnoredArguments
  Allows splatting with arguments that do not apply. Do not use directly.

  .LINK
  Write-ChocolateySuccess
  #>
  param(
    [string] $packageName,
    [string] $failureMessage,
    [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
  )

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters
  Write-Warning "Write-ChocolateyFailure was removed in Chocolatey CLI v1. If you are the package maintainer, please use 'throw `$_.Exception' instead."
  Write-Warning "If you are not the maintainer, please contact the maintainer to update the $packageName package."

  $error | ForEach-Object { $_.Exception | Format-List * | Out-String }

  throw "$failureMessage"
}
