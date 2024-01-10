<#
.SYNOPSIS
    Register application in the system

.DESCRIPTION
    The function will register application in the system using App Paths registry key so that you
    can start it by typing its registred name in the Windows Start menu on using run dialog (Win + R).

    To make applications available in the shell, add to your profile Set-AppKeyAliases function.

.EXAMPLE
    Register-Application 'c:\windows\notepad.exe'

    Register application using name derived from its file name.

.EXAMPLE
    Register-Application 'c:\windows\notepad.exe' -Name ntp

    Register application using explicit name.

.LINK
   Set-AppKeyAliases        - https://github.com/majkinetor/posh/blob/master/MM_Admin/Set-AppKeyAliases.ps1
   Application Registration - https://msdn.microsoft.com/en-us/library/windows/desktop/ee872121(v=vs.85).aspx

#>
function Register-Application{
    [CmdletBinding()]
    param(
        # Full path of the executable to register.
        [Parameter(Mandatory=$true)]
        [string]$ExePath,

        # Optional name to register with. By default exe name will be used.
        [string]$Name,

        # Register application only for the current user. By default registration is for the machine.
        [switch]$User,

        # Allows splatting with arguments that do not apply and future expansion. Do not use directly.
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $IgnoredArguments
    )

    if (!(Test-Path $ExePath)) { throw "Path doesn't exist: $ExePath" }
    if (!$Name) { $Name = Split-Path $ExePath -Leaf } else { $Name = $Name + '.exe' }

    $appPathKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\$Name"
    if ($User) { $appPathKey = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\$Name" }

    If (!(Test-Path $AppPathKey)) { New-Item "$AppPathKey" | Out-Null }
    Set-ItemProperty -Path $AppPathKey -Name "(Default)" -Value $ExePath
}
