#Requires -Version 7
<#
.SYNOPSIS
    Publishes PStudio, pedit, pcfg, ppak and ppakz for Windows (win-x64, self-contained, no .NET needed) and builds the NSIS installer.

.DESCRIPTION
    The release workflow runs it on a Windows runner: only a build on Windows puts the icon and the version into
    pstudio.exe. It also runs on Linux (with makensis from the nsis package), e.g. to check the installer script;
    pstudio.exe then has the default icon.

.PARAMETER Version
    The version (e.g. 8.1.0 or 8.1.0-beta.1). Defaults to the version of the packages (src/Directory.Build.props).

.PARAMETER Output
    The folder of the installer. Defaults to artifacts/ in the repository.

.EXAMPLE
    tools/PStudio/packaging/windows/build-windows-installer.ps1 -Version 8.1.0
#>
[CmdletBinding()]
param(
    [string] $Version,
    [string] $Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$Projects = @('tools/PStudio/PStudio.csproj', 'tools/PEdit/PEdit.csproj', 'tools/PCfg/PCfg.csproj', 'tools/PPak/PPak.csproj', 'tools/PPakZ/PPakZ.csproj') | ForEach-Object { Join-Path $RepoRoot $_ }
$Script = Join-Path $PSScriptRoot 'pstudio.nsi'
$Publish = Join-Path $RepoRoot 'publish/pstudio-win-x64'
$Output = if ($Output) { [IO.Path]::GetFullPath($Output) } else { Join-Path $RepoRoot 'artifacts' }

if (-not $Version) {
    $Version = (& dotnet msbuild (Join-Path $RepoRoot 'src/Promptuarium/Promptuarium.csproj') -getProperty:Version).Trim()
}

# The file version has four numbers and no pre-release suffix: 8.1.0-beta.1 is 8.1.0.0
$Numbers = ($Version -split '[-+]')[0].Split('.')
$FileVersion = (@($Numbers) + @('0', '0', '0', '0'))[0..3] -join '.'

function Find-Makensis {
    $command = Get-Command makensis -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    foreach ($candidate in "${env:ProgramFiles(x86)}\NSIS\makensis.exe", "$env:ProgramFiles\NSIS\makensis.exe") {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }

    throw 'makensis was not found: install NSIS (https://nsis.sourceforge.io, choco install nsis, or apt install nsis).'
}

$Makensis = Find-Makensis

if (Test-Path $Publish) { Remove-Item -Recurse -Force $Publish }

# PStudio, pedit (its command line counterpart), pcfg, ppak and ppakz in the same folder: the files of the runtime are the
# same for all of them
foreach ($project in $Projects) {
    Write-Host "==> Publishing $(Split-Path -Leaf $project) $Version (win-x64, self-contained)"
    & dotnet publish $project -c Release -r win-x64 --self-contained true `
        -p:Version=$Version -p:FileVersion=$FileVersion -p:AssemblyVersion=$FileVersion -p:DebugType=none `
        -o $Publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)." }
}

# The native libraries bring their debug symbols (about 100 MB), which an installer does not need
Get-ChildItem $Publish -Filter *.pdb -Recurse | Remove-Item

Write-Host '==> Building the installer'
New-Item -ItemType Directory -Force $Output | Out-Null
$Installer = Join-Path $Output "pstudio-$Version-win-x64-setup.exe"

# -D works with makensis on Windows and on Linux; the script finds its other files relative to itself
Push-Location $PSScriptRoot
try {
    & $Makensis -V2 "-DAPP_VERSION=$Version" "-DVI_VERSION=$FileVersion" "-DPUBLISH_DIR=$Publish" "-DOUT_FILE=$Installer" $Script
    if ($LASTEXITCODE -ne 0) { throw "makensis failed ($LASTEXITCODE)." }
}
finally {
    Pop-Location
}

$Size = [math]::Round((Get-Item $Installer).Length / 1MB, 1)
Write-Host "==> $Installer ($Size MB)"
