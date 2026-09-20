<# Runs the overlay regression inside an isolated copy of the Windows Unity player. #>
[CmdletBinding()]
param([string] $ValheimInstall = $env:VALHEIM_INSTALL)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($env:OS -ne 'Windows_NT') { throw 'These native Unity tests require the Windows Valheim player.' }
if (-not $ValheimInstall) { throw 'Pass -ValheimInstall or set VALHEIM_INSTALL to a Valheim installation with BepInEx.' }
$game = (Resolve-Path -LiteralPath $ValheimInstall).Path
foreach ($entry in @('valheim.exe', 'UnityPlayer.dll', 'winhttp.dll', 'BepInEx/core/BepInEx.Preloader.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $game $entry))) { throw "Missing test runtime dependency: $entry" }
}

$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'tests/OverlayRegression/OverlayRegression.csproj'
dotnet build $project -c Release "-p:ValheimInstallDir=$game"
if ($LASTEXITCODE -ne 0) { throw 'Overlay regression build failed.' }

# Only game data/runtime directories are shared. Plugins, config, logs and save
# paths belong to this unique temporary directory; the installed mod is not used.
$runtime = Join-Path ([System.IO.Path]::GetTempPath()) "smartcraft-overlay-tests-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path "$runtime/BepInEx/plugins", "$runtime/BepInEx/config" -Force | Out-Null
foreach ($file in @('valheim.exe', 'UnityPlayer.dll', 'winhttp.dll')) {
    Copy-Item -LiteralPath (Join-Path $game $file) -Destination $runtime
}
Copy-Item -LiteralPath (Join-Path $game 'BepInEx/core') -Destination "$runtime/BepInEx" -Recurse
foreach ($directory in @('valheim_Data', 'MonoBleedingEdge')) {
    New-Item -ItemType Junction -Path (Join-Path $runtime $directory) -Value (Join-Path $game $directory) | Out-Null
}
@'
[General]
enabled = true
target_assembly = BepInEx\core\BepInEx.Preloader.dll
'@ | Set-Content -LiteralPath "$runtime/doorstop_config.ini"
@'
[Logging.Console]
Enabled = false
[Logging.Disk]
Enabled = true
'@ | Set-Content -LiteralPath "$runtime/BepInEx/config/BepInEx.cfg"
Copy-Item -LiteralPath (Join-Path $repo 'tests/OverlayRegression/bin/Release/net48/SmartCraftStorage.OverlayTests.dll') -Destination "$runtime/BepInEx/plugins"

$arguments = @('-batchmode', '-nographics', '-savedir', "`"$runtime/saves`"", '-logFile', "`"$runtime/unity.log`"")
$process = Start-Process -FilePath "$runtime/valheim.exe" -WorkingDirectory $runtime -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test artifacts: $runtime"
if (-not $process.WaitForExit(30000)) {
    # This process was started above from the private runtime, never the user's game.
    $process.Kill()
    throw 'Overlay regression timed out; inspect unity.log in the test artifacts.'
}
$report = Get-Content -LiteralPath "$runtime/overlay-results.txt"
$report | Write-Output
if ($report -notcontains 'DONE' -or ($report | Where-Object { $_ -match '^(FAIL|ERROR) ' })) {
    throw "Overlay regression failed. See $runtime/overlay-results.txt"
}
