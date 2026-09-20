[CmdletBinding()]
param(
    [string] $ValheimInstall = $env:VALHEIM_INSTALL,
    [string] $ValheimManagedDir,
    [string] $PluginAssembly,
    [switch] $NoBuild
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path -Parent $PSScriptRoot
$game = (Resolve-Path -LiteralPath $ValheimInstall).Path
if (-not $ValheimManagedDir) { $ValheimManagedDir = Join-Path $game 'valheim_Data/Managed' }
if (-not $PluginAssembly) { $PluginAssembly = Join-Path $repo 'bin/Release/net48/SmartCraftStorage.dll' }
if (-not $NoBuild) {
    dotnet build (Join-Path $repo 'tests/Storage.Network.Tests/Storage.Network.Tests.csproj') -c Release --no-restore "-p:ValheimManagedDir=$ValheimManagedDir" "-p:BepInExCoreDir=$game/BepInEx/core" -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Network fixture build failed.' }
}
$runtime = Join-Path ([IO.Path]::GetTempPath()) ('scs-network-tests-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path "$runtime/BepInEx/plugins", "$runtime/BepInEx/config" -Force | Out-Null
foreach ($file in @('valheim.exe', 'UnityPlayer.dll', 'winhttp.dll')) {
    Copy-Item -LiteralPath (Join-Path $game $file) -Destination $runtime
}
Copy-Item -LiteralPath "$game/BepInEx/core" -Destination "$runtime/BepInEx" -Recurse
Copy-Item -LiteralPath $PluginAssembly -Destination "$runtime/BepInEx/core/SmartCraftStorage.dll"
$jotunn = Join-Path $env:USERPROFILE '.nuget/packages/jotunnlib/2.30.0/lib/net462/Jotunn.dll'
Copy-Item -LiteralPath $jotunn -Destination "$runtime/BepInEx/core/Jotunn.dll"
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
Copy-Item -LiteralPath (Join-Path $repo 'tests/Storage.Network.Tests/bin/Release/net48/Storage.Network.Tests.dll') -Destination "$runtime/BepInEx/plugins"
$arguments = @('-batchmode', '-nographics', '-noaudio', '-savedir', "`"$runtime/saves`"", '-logFile', "`"$runtime/unity.log`"")
$process = Start-Process -FilePath "$runtime/valheim.exe" -WorkingDirectory $runtime -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Test artifacts: $runtime"
if (-not $process.WaitForExit(30000)) {
    $process.Kill()
    throw 'Isolated network fixture timed out.'
}
$report = Get-Content -LiteralPath "$runtime/network-results.txt"
$report | Write-Output
if ($report -notcontains 'DONE failures=0' -or ($report | Where-Object { $_ -match '^(FAIL|ERROR) ' })) {
    throw "Network regression failed. See $runtime/network-results.txt"
}
