Set-StrictMode -Off;

if(!$args) { "Bundled version 0.2020.1.26"; exit 0 }

$powershellExe = Get-Process -Id $pid | Select-Object -ExpandProperty Path
$commandPrefix = ''
if ($host.Version.Major -gt 5) {
    $commandPrefix = '-Command'
}

function is_admin {
	return ([System.Security.Principal.WindowsIdentity]::GetCurrent().UserClaims | ? { $_.Value -eq 'S-1-5-32-544'})
}

function sudo_do($parent_pid, $dir, $cmd) {
	$src = 'using System.Runtime.InteropServices;
	public class Kernel {
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		public static extern bool FreeConsole();
	}'

	$kernel = add-type $src -passthru

	$kernel::freeconsole()
	$kernel::attachconsole($parent_pid)

	$p = new-object diagnostics.process; $start = $p.startinfo
	$start.filename = "$powershellExe"
	$start.arguments = "-noprofile $commandPrefix $cmd`nexit `$lastexitcode"
	$start.useshellexecute = $false
	$start.workingdirectory = $dir
	$p.start()
	$p.waitforexit()
	return $p.exitcode
}

function serialize($a, $escape) {
	if($a -is [string] -and $a -match '\s') { return "'$a'" }
	if($a -is [array]) {
		return $a | % { (serialize $_ $escape) -join ', ' }
	}
	if($escape) { return $a -replace '[>&]', '`$0' }
	return $a
}

if($args[0] -eq '-do') {
	$null, $dir, $parent_pid, $cmd = $args
	$exit_code = sudo_do $parent_pid $dir (serialize $cmd)
	exit $exit_code
}

if(!(is_admin)) {
	[console]::error.writeline("sudo: you must be an administrator to run sudo")
	exit 1
}

$a = if ($args[0] -eq '-please' -or $args[0] -eq '-plz') {
	Get-History -Count 1 | Select-Object -ExpandProperty CommandLine
} else {
	serialize $args $true
}

$wd = serialize (convert-path $pwd) # convert-path in case pwd is a PSDrive

$savetitle = $host.ui.rawui.windowtitle
$p = new-object diagnostics.process; $start = $p.startinfo
$start.filename = "$powershellExe"
$start.arguments = "-noprofile $commandPrefix & '$pscommandpath' -do $wd $pid $a`nexit `$lastexitcode"
$start.verb = 'runas'
$start.UseShellExecute = $true
$start.windowstyle = 'hidden'
try { $null = $p.start() }
catch { exit 1 } # user didn't provide consent
$p.waitforexit()
$host.ui.rawui.windowtitle = $savetitle

exit $p.exitcode
