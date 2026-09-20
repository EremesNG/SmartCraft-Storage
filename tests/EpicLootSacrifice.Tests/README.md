# Epic Loot sacrifice regression fixture

This .NET Framework executable source-links the production sacrifice integration
and item flags. It uses real HarmonyX 2.7.0 for patch dispatch and small external
Valheim, Unity, BepInEx and Epic Loot boundaries. The executable is named EpicLoot
so production optional-type resolution runs unchanged. It must never be copied
into a game profile or distributed with the plugin.

The external shapes and ordering match the locally inspected Epic Loot 0.13.0.0:
the sacrifice API filters candidate/reward calculation, but selected items are
removed and sockets reclaimed without a final API check. Tests observe candidate
identity/order, unlock refresh, inventory totals, rewards, sockets and cancellation
through that boundary. The fixture contains no copied SmartCraft filtering policy.

On Windows, restore from the existing local NuGet cache and run:

```pwsh
dotnet restore tests/EpicLootSacrifice.Tests/EpicLootSacrifice.Tests.csproj `
  --source "$env:USERPROFILE/.nuget/packages" `
  -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false

foreach ($contract in 'Supported', 'MissingFilter', 'MissingSelection', 'WrongUnregister') {
  dotnet run --no-restore `
    --project tests/EpicLootSacrifice.Tests/EpicLootSacrifice.Tests.csproj `
    -p:EpicLootContract=$contract
  if ($LASTEXITCODE -ne 0) { throw "Failed contract: $contract" }
}
```

Supported runs 11 cases. Each incompatible contract variant runs one check that
initialization logs the unsupported integration and leaves no partial registration
or execution patch. The patch-failure case passes a missing Harmony instance to
exercise registration rollback; it does not simulate a partially completed native
patch installation. Real Unity UI/focus, multiplayer and other mods' patches still
require an in-game check.
