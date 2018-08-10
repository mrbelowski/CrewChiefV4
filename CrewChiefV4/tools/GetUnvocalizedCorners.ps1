# This script tries to detect which landmarks still lack mappings.  Also, prints partially matching landmarks to help reusing existing sounds.
# Passing "appdata" as param will make script search sounds from install location.  Might come handy for setup validation.
function GetScriptDirectory {
  $invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $invocation.MyCommand.Path
}

$toolsPath = GetScriptDirectory
if ($args -and $args[0].ToString().ToUpperInvariant() -eq "APPDATA") {
    echo "Reading vocalized landmarks from the setup (appdata)"
    $cornersContents = Get-ChildItem $env:LOCALAPPDATA\CrewChiefV4\sounds\voice\corners
}
else {
    echo "Reading vocalized landmarks from the git.  Pass APPDATA to read from APPDATA."
    $cornersPath = $toolsPath + "\..\sounds\voice\corners"
    $cornersContents = Get-ChildItem $cornersPath
}

[System.Collections.ArrayList]$vocalizedCorners = @()
foreach ($item in $cornersContents) {
    $vocalizedCorners.Add($item.Name.ToLowerInvariant()) > $null
}


$trackLandmarksFile = $toolsPath + "\..\trackLandmarksData.json"
$content = Get-Content $trackLandmarksFile

$regexLandmarkName = new-object System.Text.RegularExpressions.Regex('landmarkName\": \"(?<name>.*?)\"', [System.Text.RegularExpressions.RegexOptions]::Singleline)

$landmarks = $regexLandmarkName.Matches($content)

[System.Collections.ArrayList]$mappedCorners = @()
foreach ($landmark in $landmarks) {
    $landmarkName = $landmark.Groups["name"].Value.ToLowerInvariant()
    if (!$mappedCorners.Contains($landmarkName)) {
        $mappedCorners.Add($landmarkName) > $null
    }
}

[System.Collections.ArrayList]$unvocalizedCorners = @()
foreach ($mappedCorner in $mappedCorners) {
    if (!$vocalizedCorners.Contains($mappedCorner)) {
        $unvocalizedCorners.Add($mappedCorner) > $null
    }
}

echo ($mst = "Num unvocalized landmarks :" + $unvocalizedCorners.Count)
echo ""

echo "====== Unvocalized Landmarks ======="
echo $unvocalizedCorners
echo ""

echo "====== Partial matches: unvocalized in vocalized ======="
foreach ($vocalizedCorner in $vocalizedCorners) {
    foreach ($unvocalizedCorner in $unvocalizedCorners) {
        if ($unvocalizedCorner -eq "r") {
            continue
        }

        if ($vocalizedCorner.Contains($unvocalizedCorner)) {
            echo ($mst = "Vocalized: " + $vocalizedCorner + " Unvocalized: " + $unvocalizedCorner)
        }
    }
}
echo ""

echo "====== Partial matches: vocalized in unvocalized ======="
foreach ($unvocalizedCorner in $unvocalizedCorners) {
    foreach ($vocalizedCorner in $vocalizedCorners) {

        if ($vocalizedCorner.Length -gt 1 -and $unvocalizedCorner.Contains($vocalizedCorner)) {
            echo ($mst = "Unvocalized: " + $unvocalizedCorner + " Vocalized: " + $vocalizedCorner)
        }
    }
}
