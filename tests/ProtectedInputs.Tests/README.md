# Protected input regression fixture

This package compiles the touched production sources directly and supplies
small boundaries for Valheim, Unity, Harmony, configuration, and Epic Loot.
It exercises issue 8's protected crafting counts and removal, station input/fuel
routes, Epic Loot material callbacks, and lock persistence/cache invalidation.
Epic Loot Sacrifice has a separate fixture in `tests/EpicLootSacrifice.Tests`.

Run offline from the repository root:

```pwsh
dotnet run --project tests/ProtectedInputs.Tests/ProtectedInputs.Tests.csproj `
  -p:RestoreSources=C:/Users/EremesNG/.nuget/packages `
  -p:RestoreAdditionalProjectSources= -p:NuGetAudit=false
```

The eight cases verify deterministic local protection of locked stacks while
unlocked stacks remain consumable. Live Unity UI, relogs, multiplayer and other
mods' behavior still require an in-game check.
