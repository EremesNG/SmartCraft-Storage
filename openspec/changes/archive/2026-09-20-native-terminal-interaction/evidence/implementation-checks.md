# Root implementation checks — not independent approval

Candidate ZIP SHA256: F70DCE0506EC63800CA2976010FC083C3A93D84A15911F77743D267FE78FA747.
Embedded DLL SHA256: 4ECC2E6FEDDD38613E0331759967A090892AB61898CA71452935182CDAA6D2A6.
Plugin constant/manifest/assembly file version: 0.7.1 / 0.7.1 / 0.7.1.0.

- `dotnet run --project tests/Storage.Contracts.Tests --no-restore`: 40/40.
- `dotnet run --project tests/Storage.Runtime.Tests --no-restore`: 4/4.
- Release/package with cached SCS-game-references-20260919 and -NoRestore: 0 errors, 3 known CS0436 warnings.
- `scripts/check-patches.ps1`: 39 explicit Harmony targets/parameters and 5 Unity lifecycle signatures pass.
- ZIP inspection: exactly manifest, icon, README, CHANGELOG and plugins/SmartCraftStorage.dll; DLL matches built candidate.

## Requirement evidence for independent review

| Requirement / buildable criterion | Production path | Evidence |
| --- | --- | --- |
| FR-001, SC-003 | NativeTerminalInventory; StorageTerminalUi; TerminalInteractionModel | Native InventoryGui grid, selected/split/released/drop/right-click routes; projection/filter/amount and queue cases; Harmony metadata |
| FR-002, SC-001 | StorageNameFlow; StorageService; StorageRpc; naming dialog | Inline/delayed/rejected dispatch, duplicate owner receipt tests; native ZDO name write/ward/access inspected |
| FR-003, SC-002 | StorageJournal; StorageService.EnsureRuntimeContext | Actual journal APIs exercised across same-world manager reset, world switch and pooled ZDO reuse; service transient reset path inspected |
| FR-004, SC-005 | DepositSelection; TerminalInteractionModel; NativeTerminalInventory.Bulk | Existing replaced-item-instance selection case, single-flight/unknown/rejection/close cases; protected real player items checked before each submission |
| FR-005, SC-004 | StoragePlanner; StorageSlotExpectation; StorageService.Withdraw/HandleServerRequest | Exact/partial/invalid/incompatible/changed-slot tests, automatic-placement baseline; absent old trailing slot read as -1 inspected |
| FR-006, SC-003 | GameInventoryAdapter; StorageMutationGuards; NativeTerminalInventory | Hydration/exact metadata/layout tests; CWT display-only inventory/item guards, native DropItem pre-remove guard, no fake persistent Container |
| FR-007, SC-005 | StorageRequestIntent.MatchesTarget; service pending lookup; UI state model | Current world/target/kind assertions, stable cells during interaction, unknown replies do not release pending work |
| FR-008, SC-006 | Tests; README; CHANGELOG; csproj; Plugin; manifest; package | Logs and final package-inspection.json |

SC-007 RISK: no corrected-build game observation. The user opted to play the old
version and install/test this candidate later; baseline screenshot only shows the
faulty 0.7.0 window. Layout, actual gestures, re-entry and ordinary-chest restoration
require that local run. Controller/touch and other UI mods are not live-verified.

SC-008 RISK: no two-client/dedicated-server acceptance. Same matching mod version
remains required. The fakes do not execute Unity, native binary inventory loading,
Harmony application or transport. See tests/Storage.Runtime.Tests/README.md.

Implementation dispatch gap: both fresh specialist starts failed with native
agent thread limit reached; root followed the documented sequential fallback.
Prior plan [OKAY] is not final candidate approval. No publish, deployment, game
installation, commit, journal removal or save modification was performed.
