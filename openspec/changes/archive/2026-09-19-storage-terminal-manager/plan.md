# Implementation Plan: Managed storage terminal

## Technical context

SCS currently targets net48 and is client-driven. InventoryChestPatches extends synchronous player inventory queries/removals; station patches directly remove/add chest items and immediately invoke machine actions or drop remainders. NearbyContainers shares discovery/access checks, but claiming ownership is not a lock. Jotunn already provides configuration, piece registration and GUI helpers. README currently says client-only and must change with the new protocol.

The current branch is storage-net at 805e79c. The prior reference report is exa-results/storage-terminal-2026-09-19/research.md. No canonical capability specs existed before this change. Offline compilation succeeds using cached game assemblies with nested generated test sources excluded; three existing publicizer type-conflict warnings remain. Local game API inspection confirms Container.Save persists inventory bytes in ZDOVars.s_items, native chest opening transfers ownership, and InventoryGui.DoCrafting can create the result before ConsumeResources. Therefore adding asynchronous calls to the old RemoveItem hook alone is insufficient.

## Constitution Check (pre-design)

- **User-value first**: PASS — US1–US6 describe the approved pooled manager and bidirectional station behavior.
- **Simplicity and bounded scope**: PASS — Full was selected explicitly; one-hop named loaded networks and direct-only exclusions bound discovery and compatibility.
- **Testable contracts**: PASS — Query, movement, compaction, complete recipe payment, processing and recovery are the public observable seams agreed through the accepted workflows.
- **Independent assurance**: PASS — Planning offers a fresh Oracle review after ready; a different fresh Oracle verifies final artifacts and implementation. Root does not self-approve.
- **Traceable delivery**: PASS — Spec contains sequential FR/SC and acceptance examples; this plan and tasks bind them to ownership and evidence.

## Design

### Architecture and ownership

Use a pure storage model/planner and transaction state machine, with a game adapter for snapshots, owner RPCs, persistence, actor checks and inventory/machine effects. A common runtime facade supplies direct or terminal-expanded queries and queued operations. A Unity GUI consumes only the facade. No second permanent network inventory is introduced; actual chests remain the store, with explicitly recorded temporary escrow only for in-flight transfers or pending production.

Root owns shared contracts, existing-feature adapters, project/package changes and every OpenSpec artifact. One deep implementation owner owns the pure backend, RPC/persistence runtime and its tests. One designer owns new terminal piece/UI and terminal-specific text. They work only after the shared contract exists; no implementation owner approves its own work. Root resolves shared-contract changes. UI and backend have disjoint files and depend on the shared contract rather than each other's unfinished implementation.

Execution capability adjustment: native dispatch accepted the backend lane but
rejected the fresh designer with `agent thread limit reached`. No designer owns
any files. Root takes sole ownership of the UI lane and executes it sequentially
with its other work while the disjoint backend continues. This preserves the
frozen interface and changes no product behavior. Final fresh Oracle remains
required; if the runtime limit persists, record that capability gap rather than
reusing a completed reviewer or claiming self-approval.

Files are organized as follows:

- Storage/Contracts.cs: Unity-independent item, chest, operation and state DTOs/public storage contracts; root owns this shared surface.
- Storage/Core/StoragePlanner.cs and Storage/Core/StorageTransactions.cs: deterministic query/allocation/compaction and replay-safe coordination; deep owns.
- Storage/Runtime/StorageService.cs, Storage/Runtime/StorageDiscovery.cs, Storage/Runtime/StorageRpc.cs, Storage/Runtime/StorageJournal.cs, Storage/Runtime/GameInventoryAdapter.cs, Storage/Runtime/StorageAccess.cs: game-facing implementation; deep owns.
- Storage/Runtime/StorageFacade.cs: the game-facing interface consumed by UI and existing feature adapters; root owns the interface, deep owns the injected implementation in StorageService.
- Storage/UI/StorageTerminal.cs, Storage/UI/StorageTerminalUi.cs, Storage/UI/TerminalRegistration.cs, Storage/UI/TerminalTranslations.cs: piece, linking/naming and management controls; designer owns.
- Storage/Integration/StationOperationController.cs: root owns Unity-independent production craft/processor decision controllers, directly invoked by the Harmony adapters and included in the offline harness.
- Storage/Integration/CraftingStoragePatch.cs, Storage/Integration/ProcessorStorage.cs, Storage/Integration/ProcessorOutput.cs and existing feature files: root owns consumer/producer adaptation.
- Storage/Integration/StorageMutationGuards.cs and Storage/Integration/DirectPlayerTransfers.cs: root owns native mutation guards and coordinated direct hotkey commands. They use the same facade and never expand Direct scope.
- tests/Storage.Contracts.Tests/Storage.Contracts.Tests.csproj and Program.cs: executable offline public-contract harness over real production core with fake transport/time/persistence only at system boundaries; deep owns test behavior and root owns project wiring.

### Discovery, identity and permissions

Keep direct physical discovery available separately from an expanded station query. Expansion takes an explicit context and authorizing player: TerminalWindow, Crafting, ProcessorInput or ProcessorOutput. Direct contexts for quick-stack, restock, building, animals, plants and optional integrations cannot expand terminal networks.

Register loaded ordinary chests and terminal pieces. Persist a trimmed, case-normalized network name in SCS ZDO metadata; display the player's chosen label. Empty means unlinked. Eligible backing chests are stationary player-built Containers, excluding graves, carts/ships and terminals. Add an explicit naming/linking interaction to chests; do not infer membership from contents or chest type. The terminal window sets the terminal name and displays membership/capacity. Initial terminal recipe clones the vanilla wooden chest appearance, removes its Container component and registers a Jotunn piece using Wood and Bronze; the manager component supplies interaction and no private storage capacity.

Crafting keeps its current player-position origin and configured crafting radius. Machines keep their current position/radius. A terminal found inside that radius expands to its named members within TerminalRadius. Deduplicate the resulting physical inventories by ZDOID, including overlaps between terminals and direct discovery. Never recurse through a terminal. Unloaded, invalid, inaccessible or unavailable resources are excluded or reported as pending; changing radius cannot force a zone to load.

The authoritative operation boundary derives player identity from the connected peer (or the local host) rather than accepting an arbitrary player ID. Validate consumer/terminal/member geometry, network name and container privacy. Evaluate wards for the supplied actor using creator/permitted membership, preserving current SCS access semantics instead of calling a local-player-only helper on a remote owner. Recheck the authorization and topology before commit. Committed decisions settle under the authorization granted at commit; later permission changes must not cause blind replay or expose a second copy.

### Planning and capacity

Item identity includes prefab/type, quality, variant, world level, durability, crafter identity/name, cheated flag and sorted custom data. A display label is not a stacking key. Core views use immutable identity/payload descriptors; the game adapter round-trips original ItemData via Inventory serialization.

Deposit first fills compatible partial stacks, then available slots in occupied members, then empty members. Withdraw chooses real stacks deterministically and respects player capacity. Organize calculates a stable compact layout over eligible finite slots, preferring larger backing capacities when this frees more chests, combining compatible partial stacks and minimizing avoidable relocation. Different item identities can share one chest. The four-chest example and zero-move repeat are required tests. Sorting the visible list only changes UI ordering.

Availability subtracts reserved quantities and capacity. Cache keys include actor/context, origin and network membership/content revision. Do not reuse the existing frame-only ChestCountCache across different expanded resource sets without invalidation/context separation. Kiln caps use the same deduplicated resource projection.

### Transaction protocol and recovery

Use SCS-owned operation identities and owner-aware RPCs rather than treating MUC's queued request count as a transaction. MultiUserChest remains optional and must coexist with ordinary chests; its installed presence does not substitute for SCS coordination. No external mod source is modified.

The coordinator serializes conflicting participants at the server. A request specifies operation kind, authenticated actor/context, exact physical participants and quantities; client totals and arbitrary inventory replacement are not accepted as authority. Participant owners return validated current snapshots. Prepared participants reserve resources/capacity and guard native chest opening and SCS writes while the operation owns the reservation. If a participant is open, unavailable or conflicting, preparing waits/rejects without consuming anything. Owners record the active operation in persistent world metadata so an ownership handoff cannot silently forget the reservation.

The protocol states are Requested, Preparing, Prepared, CommitDecided, Applying, Confirmed, Aborted and RecoveryPending:

1. Gather authoritative snapshots, validate access/range/context and construct a complete finite plan. Reserve every participant needed by that operation in a stable order; reject cycles/conflicts before effects.
2. Persist the prepared intent and the commit decision before issuing any participant effect. Before a decision, confirmed abort releases reservations without effects. After a commit decision, recovery drives the same operation to completion rather than starting a replacement operation.
3. Owners apply the planned debit/credit under a guard and record an operation receipt with the inventory/state transition. A repeated message returns the existing receipt; it never applies arithmetic twice. Source payloads in transit remain in accounted escrow until destination effects are confirmed.
4. Confirm all required participant receipts, reconcile authoritative state and release reservations. Missing/late responses produce RecoveryPending, not inferred success/failure. Reconnect and owner changes query the existing operation/receipts and resume it. Pending journals are never evicted merely to satisfy a history limit; completed acknowledged receipt history can be bounded.

The pure coordinator is tested with an in-memory implementation of the same durable participant/transport contract, including interruption between commit decision and effects, and after effect before acknowledgement. The runtime persists world-side intent/receipts in versioned SCS world metadata/journal and player-side escrow/receipt data in character custom data. Normal save/reconnect recovery is supported; restoring world and character files from different historical saves is explicitly outside crash-atomic guarantees.

Machine participants include input capacity and pending-output escrow. Output capture records produced items before suppressing the original drop. A confirmed unaccepted remainder follows the producer's existing fallback exactly once; an unknown remainder remains recoverable. Reservations and existing open-chest/machine mutations must share the same coordinator/guards; only checking a stale local busy bit is insufficient. Raw third-party inventory writes outside supported adapters are not promised compatible and must not be advertised as such.

### Crafting and processing adapters

Crafting gates InventoryGui.DoCrafting before Vanilla result creation. Capture recipe, upgrade identity, quality, multiplier, variant and station context. Prepare the complete resource cost in accounted escrow, including player contribution when needed, and revalidate context/output space before replaying the native craft once. During the replay, the inventory hooks expose/consume only the prepared cost and never issue a second network withdrawal. Native no-cost and upgrade rules remain intact. If the craft cannot run, refund or retain recoverable escrow rather than paying a partial recipe.

For recipes requiring only one ingredient, adapt Player.GetFirstRequiredItem as well as counts. Select a real eligible item descriptor from the combined projection in the native requirement/quality order, preserving its actual quality, required amount and extra output amount. During prepared replay return the matching actual ItemData from escrow, not a nonexistent player-inventory object or a prefab's default quality. Freeze that selection across the async payment and consume the exact selected requirement once, including the multi-craft multiplier. Test a recipe whose chosen ingredient exists only in a linked chest; Recipe.GetAmount must receive a non-null selected item before calculating its quality-dependent output.

StationOperationController contains the production decision state machines for craft payment/replay and processor capacity/delivery. Harmony patches translate game state and invoke these controllers rather than duplicating their decisions. The offline harness runs those exact production controllers with external native-effect, transport and persistence boundary adapters: it proves payment precedes the native callback, selected-item quality reaches replay, changed input capacity prevents consumption, duplicate ticks retain one operation, and unknown/partial output follows each producer's actual remainder policy. Compile checks cover game API binding; final review checks each Harmony path is wired to the tested controller. This explicitly avoids claiming an unused pure model verifies game integration.

Processor adapters submit descriptors for ingredient, fuel or output work. Reserve queue/fuel space and record one pending operation per producer/work kind so subsequent ticks do not enqueue duplicates. Invoke the native effect only on its owner after payment is confirmed, with a receipt that prevents repeated application. Preserve all existing toggles, regular-wood restrictions, coal caps, processing timing and cheating metadata. Smelter/kiln, cooking, fireplace and fermenter inputs expand to networks; smelter/kiln, cooking, fermenter and beehive outputs expand to network sinks.

All other SCS writers retain direct queries but observe shared reservations and authoritative write access. Quick-stack/restock may queue completion while a physical participant is busy, without gaining access to distant terminal members. Building and optional synchronous providers must never advertise resources that cannot safely be spent in their current synchronous context; their compatibility limits and retry/busy behavior are explicit and covered by regression checks rather than silently expanding their range.

### UI and configuration

Use Jotunn/Valheim panel elements, icons and input blocking. The terminal window displays network name, member count, occupied/total slots, search, sorted item rows, quantity selection, deposit/withdraw and Organize preview/result. A pending action displays its state and prevents duplicate submissions; closing the window does not cancel or forget a committed operation. Preserve equipped/locked items for bulk deposit. Provide clear empty/full/busy/unavailable/recovering feedback and keyboard navigation/escape closure. Network naming is reachable from the terminal and from a targeted ordinary chest with a non-conflicting interaction modifier, checked against existing hotkeys.

Add server-synchronized enabled/radius/budget configuration and enforce matching plugin versions with Jotunn compatibility checks. Keep existing per-feature toggles authoritative. Add EN/ES terminal text with a safe English fallback. Package only the plugin and normal metadata/docs, never game assemblies or reference binaries.

### Requirement mapping

| Requirement | Technical decision | Files/interfaces | Verification seam |
| --- | --- | --- | --- |
| FR-001 | Buildable manager piece and unified GUI | Storage/UI/TerminalRegistration.cs, Storage/UI/StorageTerminalUi.cs | Terminal facade and live UI outcome |
| FR-002 | Named loaded stationary members | Storage/Runtime/StorageDiscovery.cs | Query membership boundary |
| FR-003 | Peer-bound actor and per-participant access | Storage/Runtime/StorageAccess.cs | Denied actor/changed ward before commit |
| FR-004 | ZDO deduplication and exact item identity | Storage/Core/StoragePlanner.cs | Overlapping direct/terminal queries |
| FR-005 | Compatible stack/slot deposit allocation | Storage/Core/StoragePlanner.cs | Partial deposit and metadata conservation |
| FR-006 | Exact finite player withdrawal | Storage/Runtime/StorageService.cs | Requested/accepted/remainder quantities |
| FR-007 | Deterministic compact layout | Storage/Core/StoragePlanner.cs | Four-chest example and idempotence |
| FR-008 | Complete payment before native replay | Storage/Integration/CraftingStoragePatch.cs | Recipe payment/abort/cost-once |
| FR-009 | Confirmed payment plus input reservation | Storage/Integration/ProcessorStorage.cs | Machine queue/fuel contract |
| FR-010 | Produced-output escrow and sink routing | Storage/Integration/ProcessorStorage.cs | Output delivery/producer fallback |
| FR-011 | Finite reservations and payload conservation | Storage/Core/StorageTransactions.cs | Public operation conservation |
| FR-012 | Common conflicting-participant coordinator | Storage/Runtime/StorageRpc.cs | Concurrent operations/direct writers |
| FR-013 | Persistent decisions and idempotent receipts | Storage/Runtime/StorageJournal.cs | Restart/lost acknowledgement/replay |
| FR-014 | World registry independent of open GUI | Storage/Runtime/StorageDiscovery.cs | Closed-window query and unavailable members |
| FR-015 | Explicit direct vs expanded contexts | Shared/NearbyContainers.cs, existing feature adapters | Scope regression checks |
| FR-016 | Server/client version and authenticated RPC | Plugin.cs, Storage/Runtime/StorageRpc.cs | Peer/actor rejection and build API checks |
| FR-017 | Bounded queues/caches and honest status | Storage/Runtime/StorageService.cs | Retry/work budgets and status transitions |
| FR-018 | Offline harness and installable package | SmartCraftStorage.csproj, scripts/package.ps1, README.md | Build/package inspection |

## Optional support artifacts

- Existing research report is sufficient background; do not create a second general research document.
- contracts/storage-api.md is required to freeze the shared domain/runtime/UI seam before parallel implementation.
- No separate data-model or blueprint; this plan and the contract are canonical design inputs.
- Installation and live acceptance instructions belong in README.md and the verification report rather than a duplicate quickstart.

## Risks and migrations

Verification round 1 rejected the candidate at the production authority/recovery
boundary (V1-01–V1-07). Convergence preserves the accepted feature scope. Server
planning must use authenticated peer IDs and world ZDO metadata, with current
owners providing validated inventory snapshots/reservations/effects. It must not
depend on instantiated remote players, colliders or chests on a dedicated server.
The journal must use a verified world-persistence boundary, with durable request
identities/status and bounded replay-safe compaction. Terminal anchors participate
in reservations even though they have no inventory. Root owns native effect
reconciliation and tooling/docs; a fresh deep specialist owns the backend protocol
and its tests. The shared effect-reconciliation seam is frozen before those lanes
write implementation. Unknown native effects retain custody and cannot be replayed
or refunded until an operation-specific before/after receipt resolves them.

Native reconciliation stores a Started before-state before invocation. Crafting
runs the normal game method against a detached inventory, restores the player
inventory reference in a finally block, and saves the exact native result before
installation. This preserves random bonuses and upgrader outcomes across replay.
Processor adapters compare queue, slot, content or fuel transitions; completed
output remainders use recorded world-object identities and ItemDrop.SaveToZDO
before publishing their prefab. The normal scene then instantiates those existing
objects. A real invisible SCS_StorageJournal prefab, registered on every peer,
supports ordinary world save/load without relying on a ZoneSystem component or
an invalid/missing prefab. Player operation streams use persisted monotonic IDs.

Round-2 convergence removes the separate host output path: host and remote
callers share durable capture and authenticated request handling. Recovery keeps
immutable intent while rebuilding discovery snapshots for unplanned captured
output. Admitted payment/delivery transactions resume recorded plans. Processor
escrow does not reserve unrelated player inventory. One status projection
reconstructs persisted quantities at every public boundary, and the final
deduplicated member union enforces the global operation bound. Parsed rejected
requests receive a status response. Root owns this repair and focused tests.

Implementation reconciliation: the initial deep backend lane delivered the
planner, transaction core and game adapters, but effect recovery was incomplete.
A fresh deep owner, `/root/deep_terminal_effect_recovery`, owns completion of
Core/Runtime (except the root-owned facade) and Program.cs. Root constructs
consumer adapters against the existing frozen seam concurrently; composed final
verification waits for that lane's terminal evidence. The attempted fresh UI
designer lane was unavailable (`agent thread limit reached`), so root owns UI.
No completed specialist is reused across phases and no live game result is claimed.

Output capture hooks are Smelter.Spawn, CookingStation.RPC_RemoveDoneItem,
Fermenter.RPC_Tap and Beehive.RPC_Extract (automatic honey calls the same adapter
directly). Capturing the entire selected cooking slot avoids duplicated bonus
yield, and capturing fermentation before its volatile delayed callback preserves
the output across reconnect. The source transition and durable custody precede
suppression of the native output. Recipe selection uses actual quantity-bearing
query rows, and native ItemCheated is patched with its explicit resource-array
overload to avoid an ambiguous Harmony target.

- Network concurrency is the primary correctness risk. Pure state-machine tests are necessary but not a substitute for two-client gameplay; record unobserved live outcomes as RISK and never claim a game test occurred.
- Synchronous Vanilla crafting creates output before resource consumption. The outer craft gate and prepared-cost replay are required; a CountItems-only adapter is rejected.
- Existing direct writers and native open requests must respect reservations. Optional MUC compatibility requires testing/guarding its mutation boundary when installed; generic third-party compatibility is out of scope.
- A persisted RecoveryPending operation may keep a participant unavailable until its owner/context returns. Report recovery state and preserve escrow; do not automatically drop/refund uncertain deliveries.
- New versioned SCS metadata leaves ordinary chest inventory serialization intact. A terminal holds no permanent items; unlinking returns an idle chest to ordinary use. Refuse unsafe unlink/dismantle while unresolved work depends on the participant and explain why.
- Initial build setup must exclude nested tests from plugin compilation and support an explicit local Managed reference path. Do not delete old ignored artifacts to fix compilation.
- Server/client requirement is an intentional installation change approved by the user. Update package/readme/version checks together.
- Rollback requires resolving pending operations, unlinking networks and removing empty terminal pieces before downgrading; do not discard journals or modify external mod installations.

## Constitution Check (post-design)

- **User-value first**: PASS — Every component maps to FR-001–FR-018 and the approved manager/station outcomes.
- **Simplicity and bounded scope**: PASS — One-hop loaded networks, ordinary physical persistence, one shared coordinator and explicit direct-only contexts avoid infinite storage or hidden scope expansion.
- **Testable contracts**: PASS — Core behavior runs through public request/query/recovery seams with transport/time/persistence boundary doubles; runtime compilation and explicit gameplay outcomes remain separate evidence.
- **Independent assurance**: PASS — Implementation surfaces have one writer, optional plan review remains distinct, and final verification requires a fresh Oracle over artifacts and code.
- **Traceable delivery**: PASS — Exact paths, requirement mapping, operation states, risks, migrations and rollback define inputs for ordered tasks and verification.
