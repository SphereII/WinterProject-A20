param([string]$solutionPath, [string]$projectPath, [string]$configurationName, [string]$gameFolder, [switch]$autoStartGame, [switch]$deleteModsFolder);

$stb = New-Object -TypeName "System.Text.StringBuilder"
$stb.Capacity = 10000
Try
{
	$modsFolder = "$gameFolder\Mods"

	[void]$stb.AppendLine($modsFolder)

	# Delete everything from mods folder first
	if($deleteModsFolder.IsPresent)
	{
		if (Test-Path -Path $modsFolder) 
		{
			Remove-Item -path $modsFolder -Recurse -Force
		}
	}

	foreach($folder in Get-ChildItem $solutionPath)
	{
		$sourceFolder = $solutionPath + $folder.Name
		$modInfo = $sourceFolder + '\ModInfo.xml'

		if (Test-Path -Path $modInfo -PathType Leaf) 
		{
			$destinationFolder = "$gameFolder\Mods\$($folder.Name)"

			# Copy Modinfo.xml
			$source = $modInfo
			$destination = "$destinationFolder\ModInfo.xml"
			New-Item -ItemType File -Path $destination -Force
			Copy-Item $source $destination -Force

			# Copy dll
			$sourceDll = "$sourceFolder\bin\$configurationName\$($folder.Name).dll"
			$destinationDll = "$destinationFolder\$($folder.Name).dll"
			if ((Get-ChildItem -Path $sourceFolder -Filter *.cs -Recurse -ErrorAction SilentlyContinue -Force).count -gt 1)
			{
				if (Test-Path -Path $sourceDll -PathType Leaf) 
				{
					New-Item -ItemType File -Path $destinationDll -Force
					Copy-Item $sourceDll $destinationDll -Force
				}
			}

			# Copy config folder and files
			$sourceConfigs = "$sourceFolder\Config"
			$destinationConfigs = "$destinationFolder\Config"
			if (Test-Path -Path $sourceConfigs) 
			{
				Copy-Item  $sourceConfigs $destinationConfigs -Force -Recurse
			}

			[void]$stb.AppendLine($destinationDll)
		}
	}

	if($autoStartGame.IsPresent)
	{
		Start-Process -FilePath "$gameFolder\7DaysToDie.exe"
	}
}
Catch
{
	[void]$stb.AppendLine($_.Exception.Message)
}

$logFile = $projectPath + 'deploylog.txt'
$stb.ToString() | Out-File $logFile