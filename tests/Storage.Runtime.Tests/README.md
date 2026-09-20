# Runtime regression fixture

Run offline with `dotnet run --project tests/Storage.Runtime.Tests --no-restore`
after restoring the dependency-free net10.0 project from the installed SDK/cache.

This executable compiles the **production** StorageJournal and GameInventoryAdapter
sources. The external game boundary is deliberately small: manager replacement
invalidates old native IDs, ZDO objects may be reused, and a temporary native
inventory load has the incomplete item behavior observed in the local game source.
The fixture exercises persistence APIs and item snapshot/display/layout APIs.

The item envelope, prefab hydration and layout application run in production
code. The fake legacy inventory loader does not validate Valheim's binary format;
that compatibility branch is inspected against the local native Inventory.Load
source and still requires a game test. Unity rendering, Harmony invocation and
multiplayer transport are not executed by this fixture.
