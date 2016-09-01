Add-Type -Assembly System.IO.Compression.FileSystem
$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
function CreateZip( $zipfilename )
{
	$mode = "Create"
	if (Test-Path $zipfilename) {
		Write-Host "Trying to remove zip file"
		Remove-Item $zipfilename
	}
	return [System.IO.Compression.ZipFile]::Open($zipfilename, $mode)
}
function AddToZip( $zip )
{
	foreach ($filetoadd in $input) {
		$full = Join-Path $pwd $filetoadd
		#$file = $null
		#if ($full -match "[(.*\.cs)|(.*\.xaml)]") {
		#	# namespace mangling for source files
		#	$file = New-Object System.IO.FileInfo($full)
		#	$basenameext = $file.BaseName+$file.Extension
		#	$full = Join-Path $pwd $basenameext
		#	gc $filetoadd | %{ $_ -replace 'namespace.*', 'namespace $safeprojectname$' } > $full
		#}
		[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $full, $filetoadd, $compressionlevel)
		#if ($file)
		#{
		#	Remove-Item $full
		#}
	}
}
function PathExplode( $path = $pwd, [string[]]$exclude )
{
	$excludeList = @("bin","obj*")
	if (test-path $path -PathType Container) {
		Get-ChildItem -Recurse $path -Exclude *.dll,*.pdb,*.exe | % {
			$wholepath = $_.FullName.substring($pwd.path.Length + 1)
			$pathParts = $wholepath.split("\");
			if ( ! ($excludeList | where { $pathParts -like $_ } | where {!$_.PsisContainer}) )
			{
				if (-not(test-path $wholepath -PathType Container)) {
					$wholepath
				}
			}
		}
	}
	else
	{
		$path
	}
}
function ZipMultiple( $zipfilename )
{
	$zip = CreateZip($zipfilename)
	if (-not($zip))
	{
		Write-Host "FAILED TO CREATE ZIP FILE!"
		return
	}
	foreach ($nextdeeper in $input) {
		PathExplode $nextdeeper | AddToZip $zip
	}
	$zip.dispose()
	$zip = $null
}
$wpf_window_template_contents = "ROS.NET WPF Application","ROS_Comm","XmlRpc_Wrapper","YAMLParser","ROS.NET_WPF_Application_Root.vstemplate","TemplateIcon.png","PreviewImage.png"
$wpf_window_template_contents | ZipMultiple("ROS.NET_WPF_Application.zip")
$console_template_contents = "ROS.NET Console Application","ROS_Comm","XmlRpc_Wrapper","YAMLParser","ROS.NET_Console_Application_Root.vstemplate","TemplateIcon.png","PreviewImage.png"
$console_template_contents | ZipMultiple("ROS.NET_Console_Application.zip")