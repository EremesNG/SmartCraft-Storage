# Native grid regression in Unity

This test plugin links production `Storage/UI/NativeTerminalLayout.cs` and `Hotkeys/HotkeyConfig.cs` and runs
real Unity RectTransforms plus Valheim's InventoryGrid. It creates a synthetic
UI and test items, checks native cell visibility and restores ordinary chest
geometry. External padding reapplication matches the inspected Valheim Plus
layout operation; the complete mod is not loaded by this fixture. The 0.7.7
regression also replays its lazy first-container cache, before and after normal
chest restoration. It verifies adaptive rows, hidden padding, a real UGUI raycast
onto a transparent native cell and the native selection callback used for deposits.

The 0.7.2 layout produced ten cells inside a viewport with width -40 after a
native right inset was reapplied. The corrected layout retains visible cells.
The 0.7.6 compact layout checks cover eight columns/four complete rows, scrollbar
separation from the footer, scrolling to the last row, repeated external inset
changes without drift, and restoring the ordinary chest and scrollbar hierarchy.
Real BepInEx config files in the isolated runtime verify the Alt+N default,
one-time migration of Alt+T, preservation of custom/disabled bindings, and a later
explicit Alt+T choice surviving reload. Engine errors fail the fixture.

Build against local game references and BepInEx core assemblies:

```powershell
dotnet restore tests/Storage.Unity.Tests/Storage.Unity.Tests.csproj -p:RestoreSources=C:/Users/EremesNG/.nuget/packages -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false --ignore-failed-sources
dotnet build tests/Storage.Unity.Tests/Storage.Unity.Tests.csproj -c Release --no-restore -p:ValheimManagedDir=C:/Users/EremesNG/AppData/Local/Temp/SCS-game-references-20260919 -p:BepInExCoreDir=F:/Steam/steamapps/common/Valheim/BepInEx/core
```

Run with a graphics device enabled so Canvas can assign drawable depths for
the pointer check; omit `-nographics`. It waits two frames for real Canvas layout.
Run the built test DLL as the **only plugin in an isolated runtime**, never in
the normal game installation: its boot hooks suppress platform/scene startup and FejdStartup.Awake and
quits the process after testing. Use a separate directory with copies of
valheim.exe, UnityPlayer.dll, winhttp.dll, doorstop_config.ini and BepInEx/core;
valheim_Data and MonoBleedingEdge may be read-only source junctions. Disable the
BepInEx console in this fixture and use a separate empty save directory.

For the prepared local fixture:

```powershell
$gridRuntime = 'C:/Users/EremesNG/AppData/Local/Temp/SCS-terminal-grid-runtime-20260919'
Copy-Item -LiteralPath tests/Storage.Unity.Tests/bin/Release/net48/Storage.Unity.Tests.dll -Destination (Join-Path $gridRuntime 'BepInEx/plugins/Storage.Unity.Tests.dll')
Copy-Item -LiteralPath C:/Users/EremesNG/.nuget/packages/jotunnlib/2.30.0/lib/net462/Jotunn.dll -Destination (Join-Path $gridRuntime 'BepInEx/core/Jotunn.dll')
$gridProcess = Start-Process -FilePath (Join-Path $gridRuntime 'valheim.exe') -WorkingDirectory $gridRuntime -WindowStyle Hidden -PassThru -ArgumentList @('-batchmode', '-noaudio', '-savedir', (Join-Path $gridRuntime 'saves'), '-logFile', (Join-Path $gridRuntime 'unity.log'))
$gridProcess.WaitForExit(10000)
Get-Content -LiteralPath (Join-Path $gridRuntime 'native-grid-results.txt')
```

`DONE failures=0` is required. Evidence is retained in the active change's
`evidence/grid-layout-red.log` and `grid-layout-green.log`, plus the 0.7.6
`compact-layout-red.log`, `compact-shortcut-red.log` and
`compact-grid-shortcut-green.log`. This is actual engine
execution. The 0.7.7 cache/display/input evidence is in `native-scrollbar-cache-*.log`,
`terminal-padding-red.log` and `terminal-padding-input.log`. This is native
geometry/native-grid execution, not a screenshot or full-game acceptance. The
complete terminal query/transfer loop, mods, real prefab skin, resolution and
controller behavior still require live acceptance. No world or character is
loaded by this fixture, and it is excluded from the distributable ZIP.
