# Storage network regression

This fixture loads the compiled SmartCraftStorage service and RPC implementation
inside an isolated Valheim/Unity process. It uses the game's real routed RPC
serialization, peer objects and inventories. Only the socket and monotonic clock
are controlled test boundaries; no Steam connection, gameplay world or user save
is opened. The installed plugins and game configuration are not changed.

Build the production project first, restore this test project once, then run:

```powershell
./scripts/test-storage-network.ps1 -ValheimInstall 'F:/Steam/steamapps/common/Valheim'
```

Use `-ValheimManagedDir` when reference assemblies are cached outside the game.
The runner requires the project's Jotunn 2.30.0 package in the NuGet cache and
BepInEx in the game install. Each run creates a separate temporary directory,
prints its location and preserves its report and logs. `-NoBuild` reuses the test
assembly; `-PluginAssembly` selects an alternate production DLL for comparisons.

Coverage includes restored processor requests, frame-independent retry timing,
authenticated admission and status polling, fresh-intent recovery, captured
output custody, reconnects, client/server congestion, duplicate participant
receipts, final outcomes, operation ownership and synchronous local RPCs.

The original 0.7.7 service sent 402 packets for two pending operations during
100 immediate Tick/Resume iterations. The corrected service sends two initial
packets and waits for its retry deadline. A separate 60-second clock simulation
produces 11 attempts per pending operation at 30, 60 and 144 FPS.

This fixture does not establish full-game or real Steam multiplayer acceptance.
Inventory conservation, transaction recovery and effect idempotency are also
covered by the existing Storage.Contracts.Tests and Storage.Runtime.Tests suites.
