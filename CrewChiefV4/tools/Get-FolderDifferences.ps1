param (
   [parameter(position = 0, mandatory = $true )]
   [string] $LeftTree,

   [parameter(position=1, mandatory = $true)]
   [string] $RightTree,
 
   [switch]
   [boolean] $IncludeSpotter = $false
)

$LeftTree = $LeftTree.TrimEnd("\")
$RightTree = $RightTree.TrimEnd("\")

$nonEmptyLeftDirs = gci $LeftTree -Directory -Recurse | where { (gci $_.FullName).Count -ne 0 } | select -ExpandProperty FullName

$nonEmptyLeftMap = New-Object "System.Collections.Generic.Dictionary[string,string]"
foreach ($dir in $nonEmptyLeftDirs) {
    if (!$IncludeSpotter -and ($dir.Contains("spotter") -or $dir.Contains("radio_check"))) {
        continue
    }

    $leafPart = $dir.Remove(0, $LeftTree.Length)
    $nonEmptyLeftMap.Add($leafPart, $dir)
}

$nonEmptyRightDirs = gci $RightTree -Directory -Recurse | where { (gci $_.FullName).Count -ne 0 } | select -ExpandProperty FullName

$nonEmptyRightMap = New-Object "System.Collections.Generic.Dictionary[string,string]"
foreach ($dir in $nonEmptyRightDirs) {
    if (!$IncludeSpotter -and ($dir.Contains("spotter") -or $dir.Contains("radio_check"))) {
        continue
    }

    $leafPart = $dir.Remove(0, $RightTree.Length)
    $nonEmptyRightMap.Add($leafPart, $dir)
}

# Print folders that are non empty in the left tree, but are absent in the right one.
Write-Host "-------------------------------------------------------------------------"
Write-Host "---FOLDERS NOT EMPTY IN THE LEFT TREE MISSING FROM THE RIGHT TREE--------"
Write-Host "-------------------------------------------------------------------------"
foreach ($leftNonEmpty in $nonEmptyLeftMap.Keys) {
    if (!$nonEmptyRightMap.ContainsKey($leftNonEmpty)) {
        Write-Host $leftNonEmpty
    }
}

# Print folders that are non empty in the right tree, but are absent in the left one.
Write-Host "-------------------------------------------------------------------------"
Write-Host "---FOLDERS EMPTY IN THE LEFT TREE PRESENT IN THE RIGHT TREE--------------"
Write-Host "-------------------------------------------------------------------------"
foreach ($rightNonEmpty in $nonEmptyRightMap.Keys) {
    if (!$nonEmptyLeftMap.ContainsKey($rightNonEmpty)) {
        Write-Host $rightNonEmpty
    }
}
