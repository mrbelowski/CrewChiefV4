# Patches CC install directory with newest Release files.  Not complete, can be grown and improved to cleanup stuff, settings, sounds etc.
# For some unknown reason does not work from git powershell prompt, so either run from Explorer, ISE or elevated PS prompt.
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {   
    $arguments = "& '" + $myinvocation.mycommand.definition + "'"
    Start-Process powershell -Verb RunAs -ArgumentList $arguments
    break
}

function GetScriptDirectory {
  $invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $invocation.MyCommand.Path
}

function CopyFile($from, $to) {
    Copy-Item $from -Destination $to -Force -Verbose -Recurse
    echo ""
}

$toolsPath = GetScriptDirectory
$releaseBinPath = $toolsPath + "\..\bin\Release\"
$rootPath = $toolsPath + "\..\"
$ccLayoutMainPath = ${env:ProgramFiles(x86)} + "\Britton IT Ltd\CrewChiefV4\"

echo "Patching main CC install"
echo ""

CopyFile $releaseBinPath\"CrewChiefV4.exe" $ccLayoutMainPath
CopyFile $releaseBinPath\"CrewChiefV4.exe.config" $ccLayoutMainPath
CopyFile $rootPath\"ui_text.txt" $ccLayoutMainPath
CopyFile $rootPath\"carClassData.json" $ccLayoutMainPath
CopyFile $rootPath\"trackLandmarksData.json" $ccLayoutMainPath
CopyFile $rootPath\"plugins" $ccLayoutMainPath

echo "Press any key to finish..."

Read-Host