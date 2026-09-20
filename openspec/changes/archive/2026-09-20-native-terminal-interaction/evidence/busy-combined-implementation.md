# Combined 0.7.9 integration evidence

Source commit: 5aa72dbb2b41b8fe0619e23e0ea1e1cb46e1d398.
Target parent: 70ea14db5e5b09e2cf348f8ee6ca71d9d2b5f937.
Reviewed merge candidate: a406c53f5b2532086656959a1eda81fb8dd82aa7.
Final integration Oracle: oracle_storage_combined_079 PASS; source round2 PASS is recorded separately.

## Preserved behavior

Target had independently returned to the pre-storage cart/overlay branch and
added issue-8 ALT-lock protections. The merge retains those changes and uses
the storage coordinator for processor inputs/outputs. Owner and lock gates are
combined for building and Epic Loot. Locked chest rows cannot pay local or remote
costs; player-carried ordinary crafting behavior remains unchanged.

Query retains full contents/capacity. Consumer filtering excludes locked inputs
from crafting and processor selection, while stored locked coal still counts
against the kiln output cap. The narrow consumer helpers and shared cost
predicate avoid changing terminal display or unrelated storage policies.

## Qualified evidence and limits

The initial combined build succeeded before two added native protection cases
failed: locked/free cost selection and locked-only rejection. Both pass after
the fix. Narrow filtering was then qualified with two consumer failures; the cap
accumulator already passed with the restored full query. The cap check exercises
the production accumulator, not a complete live kiln. A temporary player-cost
fixture used an invalid overlapping anchor; correcting the fixture topology
resolved that setup failure without a product change.

Four remaining protected-input checks cover building, Epic Loot callbacks,
unlocked inventory and ALT-lock persistence. Obsolete direct-station tests were
replaced by native coordinator checks. Cart boundary adapters from the prior
combined 0.7.8 commit were restored for the existing owner-only synchronous write
contract; old ownership-claim expectations are not treated as product failures.
Runtime test compilation now includes the real ItemFlags source used by the
combined adapter. All test-only boundaries and documentation are explicit.

The exact committed merge build passes 73 native Unity checks, 40 contracts,
7 runtime, 15 cart, 18 overlays, 4 protected inputs and 14 Epic Loot contract
variants: 171 total. Release has 0 errors and 3 known publicizer warnings.
39 Harmony targets and 5 lifecycle signatures resolve. The five-entry 0.7.9 ZIP
contains exactly the built DLL and matching manifest/file versions; hashes are
in busy-combined-package-inspection.json. Source package equality is recorded in
busy-source-package-inspection.json. Prior 0.7.8 ZIPs and the Desktop backup are
unchanged. No installed game/config/server/save was modified.

Remote protected-cost execution is structurally covered by the shared predicate,
not a new end-to-end remote request. SC-007 live local/UI and SC-008 live Steam
multiplayer remain risks. A later documentation/archive-only commit may change
assembly provenance and hashes on rebuild; production equivalence and final
hashes must then be recorded in the ignored distribution BUILD-INFO.txt.
