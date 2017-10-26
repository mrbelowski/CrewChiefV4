# This script detects driver names that need vocalizations. It reads driver names from the file supplied and checks if vocalization already exists.
function GetScriptDirectory {
  $invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $invocation.MyCommand.Path
}

echo $args
if ($args -and $args[0].ToString() -ne "") {
    Write-Host "Will be reading driver names from:" -NoNewline
    echo $args[0].ToString()
    $newDriversFile = $args[0].ToString()
}
else
{
    echo "Error: No new drivers list specified."
    exit
}

$toolsPath = GetScriptDirectory

echo "Reading vocalized driver names from from the git"
$driversPath = $toolsPath + "\..\sounds\driver_names"
$driversVocalized = ((Get-ChildItem $driversPath).Basename).ToLowerInvariant()

$newDriverNames = (Get-Content $newDriversFile).ToLowerInvariant()

[System.Collections.ArrayList]$uniqueUnvocalized = @()
echo "Driver names needing vocalization:"
foreach ($newName in $newDriverNames) {
    if(!$driversVocalized.Contains($newName))
    {
        if (!$uniqueUnvocalized.Contains($newName))
        {
            $uniqueUnvocalized.Add($newName) > $null
            echo $newName
        }
    }
}
