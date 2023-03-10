# Prints the installation directory of FEZ, and then exits.

$ProgramName = "FEZ"
$items = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
foreach ($entry in $items)
{
	$entryName = $entry.Name -replace 'HKEY_LOCAL_MACHINE', 'HKLM:'
	if ((Get-ItemProperty -Path $entryName -Name DisplayName -ErrorAction SilentlyContinue).DisplayName -like $ProgramName)
	{
		(Get-ItemProperty -Path $entryName -Name InstallLocation -ErrorAction SilentlyContinue).InstallLocation
	}
}

