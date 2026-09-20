# Tasks: Managed storage terminal

## MVP scope

US1 and US2 form the first testable manager slice: a finite named network with
deposit, withdrawal and physical compaction. The delivered change also includes
US3–US6; the manager slice alone does not close this change.

## Dependencies

Root owns shared contracts, plugin/project/configuration, all existing-feature
adapters and OpenSpec. Deep owns new backend/runtime implementation and core test
behavior. Designer owns only new terminal piece/window/text. Each writer preserves
others' edits. No child delegates. The shared contract and offline harness exist
before independent lanes begin. Root serializes any subsequent contract change.

T001 -> T002 -> T003 -> T004 precede Group P1. Within the backend lane each test
task is a vertical red/green cycle with its following implementation task; add one
observable scenario at a time, not all tests before all implementation. UI work
uses the fixed facade and does not consume unfinished backend outputs. After the
P1 barrier, root integrates and verifies station adapters against the real runtime.
Native capacity is three independent lanes including root. If capacity is
unavailable, execute the same lanes sequentially with the same ownership/barrier.

## Shared setup

Execution note: T001–T004 compile against local references; the offline harness
restored locally and compiled the production contracts as a library before its
test entry point exists. This is wiring evidence, not a passing behavior suite.
The native backend dispatch succeeded; fresh designer dispatch failed with
`agent thread limit reached`. Root therefore owns T017–T020 as the documented
sequential fallback; no UI peer is writing. Final fresh verification is still due.
The initial backend owner completed its bounded context with effect recovery gaps;
fresh deep owners completed Core/Runtime and Program.cs through convergence.
Root-owned consumers used the actual contract/adapter outputs during construction.
ProcessorOutput, native guards
and direct hotkey queue are explicit additional root surfaces T040–T042.
Round-1 evidence: combined net48 build succeeds with the three baseline CS0436
publicizer warnings on a full compile (incremental build: zero warnings);
the production contract executable has passed 18 scenarios, also independently
rerun by the fresh verification Oracle. Metadata-only inspection resolves all
28 explicit Harmony targets and their named hook parameters in local game DLLs.
SC-008/SC-009 have no live game observations.

Verification round 1 failed; its original package contained only the
plugin, manifest, icon, README and changelog; SHA256 at this review boundary is
`94A5E8BADC779C773354F47D85AE39C2F161E3249A3395EFE29213776ADD8B86`.
That package was superseded after convergence; its hash remains audit history.

Round-2 candidate: deep convergence returned terminal completion evidence for
V1-01 through V1-06. Root reran the composed harness (27/27), documented patch
check (28 explicit targets), and package script (incremental net48 build with
zero warnings/errors). The five-entry 0.7.0 ZIP has SHA256
`876C1555779AF52B67EE697D4166E62572A731EFB2667B050C6DA0B0C449E5F1`.
At the round-2 boundary, fresh `/root/oracle_terminal_verify_round2` reviewed the frozen production
candidate. This is executed evidence, not final approval; SC-008/SC-009 remain
unobserved. No package has been deployed or published.

- [x] T001 Fix nested test source glob and offline build wiring for FR-018 in `SmartCraftStorage.csproj` | Verify: baseline plugin compiles without duplicate assembly attributes using local references.
- [x] T002 Freeze domain contracts for FR-004, FR-011, FR-013 in `Storage/Contracts.cs` | Verify: immutable snapshots and explicit partial/pending results compile without Unity.
- [x] T003 Freeze runtime consumer/effect contracts for FR-008, FR-009, FR-010, FR-012 in `Storage/Runtime/StorageFacade.cs` | Verify: UI and station callers compile against one unavailable-safe service interface.
- [x] T004 Wire offline public contract harness for FR-018 in `tests/Storage.Contracts.Tests/Storage.Contracts.Tests.csproj` | Verify: executable harness references actual production pure code without network restore.

## Backend: US1, US2 and US5

- [x] T005 [P] [US1] Drive query, partial transfer and exact identity scenarios for FR-004, FR-005, FR-006, SC-001, SC-002 in `tests/Storage.Contracts.Tests/Program.cs` | Verify: each scenario fails for its intended missing behavior then passes with the next planner increment.
- [x] T006 [P] [US1] Implement finite deterministic allocation and query deduplication for FR-004, FR-005, FR-006 in `Storage/Core/StoragePlanner.cs` | Verify: literal expected quantities and metadata remain conserved across partial transfers.
- [x] T007 [P] [US2] Drive compaction cases for FR-007, SC-003 in `tests/Storage.Contracts.Tests/Program.cs` | Verify: four ten-slot members become three occupied members and repeat requires zero changes.
- [x] T008 [P] [US2] Implement compact mixed-item layouts for FR-007, FR-011 in `Storage/Core/StoragePlanner.cs` | Verify: no category slots or excess capacity appear and incompatible metadata never merges.
- [x] T009 [P] [US5] Drive commit/recovery/concurrency scenarios for FR-011, FR-012, FR-013, SC-006, SC-007 in `tests/Storage.Contracts.Tests/Program.cs` | Verify: duplicate, lost, reordered and restarted operations preserve exact totals without replaying effects.
- [x] T010 [P] [US5] Implement durable coordinator through boundary ports for FR-011, FR-012, FR-013 in `Storage/Core/StorageTransactions.cs` | Verify: production coordinator rejects stale/conflicting plans and resumes the same committed operation after acknowledgement loss.
- [x] T011 [P] [US1] Implement game payload and snapshot adapter for FR-005, FR-006, FR-011 in `Storage/Runtime/GameInventoryAdapter.cs` | Verify: serialized ItemData preserves properties and placement respects strict compatible identities and real slots.
- [x] T012 [P] [US5] Implement actor privacy/ward validation for FR-003, FR-016 in `Storage/Runtime/StorageAccess.cs` | Verify: peer identity and remote actor permissions are checked at preparation and commit.
- [x] T013 [P] [US1] Implement named loaded discovery and independent contexts for FR-002, FR-003, FR-004, FR-014, FR-017 in `Storage/Runtime/StorageDiscovery.cs` | Verify: deduplicated queries exclude wrong-name/range/permission members and never need an open GUI.
- [x] T014 [P] [US5] Implement world/profile journals and receipts for FR-013, FR-017 in `Storage/Runtime/StorageJournal.cs` | Verify: active escrow survives restart and old operation IDs cannot be reapplied after receipt compaction.
- [x] T015 [P] [US5] Implement owner-aware authenticated protocol and participant guards for FR-003, FR-012, FR-013, FR-016 in `Storage/Runtime/StorageRpc.cs` | Verify: stale owners, changed permissions, open chests and duplicate effects are rejected or reconciled without loss.
- [x] T016 [P] [US1] Implement shared facade, request queues and persistent effect execution for FR-001, FR-005, FR-006, FR-008, FR-009, FR-010, FR-017 in `Storage/Runtime/StorageService.cs` | Verify: real UI/station commands report confirmed partial results or recovery state and callbacks cannot bypass persistent effect receipts.

## Terminal presentation: US1, US2 and US6

- [x] T017 [P] [US1] Register finite-capacity manager piece for FR-001, FR-002 in `Storage/UI/TerminalRegistration.cs` | Verify: the buildable prefab has a manager interaction and no independent Container inventory.
- [x] T018 [P] [US1] Add terminal/chest naming interactions for FR-001, FR-002, FR-003 in `Storage/UI/StorageTerminal.cs` | Verify: accessible idle objects can be named and busy/unauthorized changes are refused.
- [x] T019 [P] [US1] Implement searchable pooled management window for FR-001, FR-005, FR-006, FR-007, FR-017 in `Storage/UI/StorageTerminalUi.cs` | Verify: capacity, filtering, quantity controls, view sorting, deposit, withdrawal and Organize call the facade and show honest pending results.
- [x] T020 [P] [US6] Add terminal feedback and accessible controls for FR-001, FR-017 in `Storage/UI/TerminalTranslations.cs` | Verify: EN/ES labels, empty/full/busy/recovery messages and keyboard closure are present with English fallback.

## Root integration foundation

- [x] T021 [US6] Bind synced terminal settings for FR-002, FR-016, FR-017 in `Config/StorageConfig.cs` | Verify: enabled, radius and bounded-work settings have finite validation and server authority.
- [x] T022 [US6] Initialize runtime/UI and enforce compatible installation for FR-016 in `Plugin.cs` | Verify: actual service and effect handlers initialize on the appropriate peers before consumers submit operations.
- [x] T023 [US3] Drive observable cost/processor scope contracts for FR-008, FR-009, FR-010, FR-015, SC-004, SC-005, SC-007 in `tests/Storage.Contracts.Tests/StationScenarios.cs` | Verify: red cases describe network-only single-ingredient quality, complete-payment before native effects, input capacity, duplicate ticks, unknown/partial output and direct-only boundaries at production public seams.

## Station and existing-feature integration

- [x] T024 [US3] Drive and implement production craft/processor controllers for FR-008, FR-009, FR-010, FR-012, SC-004, SC-005 in `Storage/Integration/StationOperationController.cs` | Verify: actual Harmony consumers use the tested payment-before-result, selected-ingredient quality, duplicate-tick, changed-capacity and remainder decisions.

- [x] T025 [US3] Gate native crafting and upgrade effects for FR-008, FR-011, FR-012, SC-004 in `Storage/Integration/CraftingStoragePatch.cs` | Verify: result creation runs once only after full cost, and canceled/changed crafts preserve accounted resources.
- [x] T026 [US3] Adapt displayed counts and prepared-cost consumption for FR-004, FR-008, FR-015 in `CraftingChestAccess/InventoryChestPatches.cs` | Verify: crafting includes networks once, GetFirstRequiredItem exposes the real selected escrow item even for network-only stock, hammer placement stays direct and replay never initiates another payment.
- [x] T027 [US4] Implement persistent machine input/output adapter for FR-009, FR-010, FR-011, FR-012, SC-005 in `Storage/Integration/ProcessorStorage.cs` | Verify: changed queue capacity refuses consumption, eight of ten output units store with two accounted remainders and unknown results remain pending.
- [x] T028 [US4] Integrate smelter/kiln automation for FR-009, FR-010, FR-014 in `Stations/SmelterPatches.cs` | Verify: fuel, ore, coal caps, coal routing and production honor shared operations with the terminal closed.
- [x] T029 [US4] Integrate fermentation automation for FR-009, FR-010 in `Stations/FermenterPatches.cs` | Verify: input requires confirmed payment and tapped output retains producer-specific remainder behavior.
- [x] T030 [US4] Integrate cooking automation for FR-009, FR-010 in `Stations/CookingStationPatches.cs` | Verify: slots/fuel reserve capacity and finished food delivery cannot duplicate a late accepted output.
- [x] T031 [US4] Integrate fireplace fuel for FR-009 in `Stations/FireplacePatch.cs` | Verify: owner applies a confirmed fuel quantity once within real capacity.
- [x] T032 [US4] Integrate honey collection for FR-010 in `Stations/BeehivePatches.cs` | Verify: partial acceptance preserves exact remaining honey without phantom success.
- [x] T033 Respect common write coordination and direct query boundaries for FR-012, FR-015, SC-007 in `Shared/NearbyContainers.cs` | Verify: direct features cannot mutate a prepared participant or acquire terminal-expanded reach.
- [x] T034 Keep cache contexts distinct for FR-004, FR-012 in `Shared/ChestCountCache.cs` | Verify: actor/origin/context changes do not reuse expanded counts and confirmed mutation invalidates stale values.
- [x] T035 Adapt direct quick-stack coordination for FR-012, FR-015 in `QuickStack/QuickStackService.cs` | Verify: Shift+E preserves direct reach and cannot compete with an active reservation.
- [x] T036 Adapt direct restock coordination for FR-012, FR-015 in `Restock/RestockService.cs` | Verify: marked-item replenishment keeps its direct scope and conserves concurrent quantities.
- [x] T037 Adapt direct animal writer coordination for FR-012, FR-015 in `AnimalFeeder/AnimalFeederPatch.cs` | Verify: only direct eligible food is spent and reserved quantities cannot feed twice.
- [x] T038 Adapt direct harvest writer coordination for FR-012, FR-015 in `PlantHarvest/PlantHarvestPatch.cs` | Verify: direct deposits retain exact accepted/remainder behavior under contention.
- [x] T039 Preserve optional integration's direct scope and safe writes for FR-012, FR-015 in `Integrations/EpicLootProvider.cs` | Verify: synchronous providers cannot advertise or consume resources reserved by terminal operations.

## Additional adapter surfaces

- [x] T040 [US4] Capture production and deliver exact remainders for FR-010, FR-011, SC-005 in `Storage/Integration/ProcessorOutput.cs` | Verify: each native producer grants durable custody before suppressing output and only confirmed remainders are dropped.
- [x] T041 [US5] Coordinate native inventory access for FR-012, FR-013 in `Storage/Integration/StorageMutationGuards.cs` | Verify: open, move, take-all and destruction honor reservations while authorized effect replay remains possible.
- [x] T042 Preserve direct hotkey behavior for FR-015, SC-007 in `Storage/Integration/DirectPlayerTransfers.cs` | Verify: quick-stack targets matching physical chests, restock uses direct range, and pending identities survive closure/reconnect.

## Verification and delivery

- [x] T043 Simplify changed production code without changing FR-001 through FR-018 behavior in `Storage/Runtime/StorageService.cs` | Verify: focused contract tests remain passing and no redundant permanent inventory or optimistic confirmation remains.
- [x] T044 Document setup, limits, recovery and live acceptance for FR-018 in `README.md` | Verify: client/server installation, naming, automation, capacity and two-client scenarios match the implementation.
- [x] T045 Build and inspect distributable for FR-016, FR-018 in `scripts/package.ps1` | Verify: package contains matching plugin metadata/docs and no game DLLs or transient journals.
- [x] T046 Record fresh independent verification for all FR and SC in `openspec/changes/storage-terminal-manager/verify-report.md` | Verify: Oracle returns PASS or actionable FAIL, automated evidence is reproducible and unobserved SC-008/SC-009 outcomes are explicit residual risks.
- [x] T047 Archive declared durable deltas after PASS in `openspec/changes/storage-terminal-manager/archive-report.md` | Verify: closeout validation passes and canonical capability specs match the verified change.

## Parallel execution

### Group P1

- Lane L1: T005 -> T006 -> T007 -> T008 -> T009 -> T010 -> T011 -> T012 -> T013 -> T014 -> T015 -> T016 | Owner: deep
- Lane L2: T017 -> T018 -> T019 -> T020 | Owner: designer
- Prerequisites: T001, T002, T003, T004
- Barrier: T025
- Rationale: Lane path sets are disjoint: backend/core plus Program.cs versus new UI files. Both lanes consume only the frozen contracts, so there is no cross-lane output dependency during construction. Root owns separate config/plugin and StationScenarios.cs tasks T021–T023 and may construct them while both agents run, postponing composed execution until the barrier and serializing shared-contract repairs. Dispatch both admitted lanes before waiting; terminal completion evidence, not timeouts, releases the barrier.

## Final verification

Run the offline public-contract executable and net48 plugin build/package with
the local cached game references. Fresh Oracle reviews every FR and buildable SC
against actual code/evidence. SC-008 and SC-009 require live game observations;
if unavailable, explicitly record RISK with the exact remaining acceptance steps.
Neither build success nor pure model tests substitute for live visual/multiplayer
evidence. Plan approval and implementation authorization do not approve results.

## Convergence — verification round 1

- [x] T048 Repair V1-01 (contradicts, Critical), FR-014/FR-016, SC-006/SC-007 through supporting RPC/discovery/access runtime files. Owner: fresh deep backend specialist in `Storage/Runtime/StorageService.cs` | Verify: authenticated remote operations and owner recovery never require server-local remote Player/Container/colliders; production authority policy tests run without scene objects.
- [x] T049 Repair V1-02 (partial, Critical), FR-013, SC-006 through supporting Core/RPC/service protocol files. Owner: same deep backend specialist in `Storage/Runtime/StorageJournal.cs` | Verify: durable identity, persisted requests, reconnect/status, lost/duplicate receipt and safe old-message rejection through production protocol seams.
- [x] T050 Repair V1-03 (partial, High), FR-001/FR-005/FR-006/FR-008, SC-002/SC-004 through supporting effect/service/backend tests. Owner: same deep backend specialist in `Storage/Core/StorageTransactions.cs` | Verify: delayed 8-of-10 results preserve exact counts; payment alone never reports final craft success.
- [x] T051 Repair V1-04 (partial, High), FR-011/FR-012/FR-013, SC-004/SC-005/SC-006 through the processor/output/guard adapters. Owner: root for Integration and native-effect tests; deep for Core/Runtime and backend tests after freezing shared handler contract in `Storage/Integration/CraftingStoragePatch.cs` | Verify: durable started receipts precede every native mutation; reconciliation after mutation-before-ack never repeats or refunds an ambiguous effect; unknown capture remains accounted and resumable.
- [x] T052 Repair V1-05 (missing, High), FR-003/FR-012/FR-016, SC-007 through supporting authority/reservation tests. Owner: same deep backend specialist in `Storage/Runtime/StorageAccess.cs` | Verify: synchronized range/feature policy and actor/target checks; terminal anchor rename/destruction/ward changes conflict with active plans.
- [x] T053 Repair V1-06 (contradicts, High), FR-017/FR-013, SC-006 through supporting Core/runtime/backend tests. Owner: same deep backend specialist in `Storage/Runtime/StorageJournal.cs` | Verify: durable pending admission, bounded status/receipt lifecycle and incremental persistence preserve rejection of old retries.
- [x] T054 Repair V1-07 (contradicts, Medium), FR-018 and operating documentation. Owner: root in `scripts/check-patches.ps1` | Verify: documented default invocation works; documentation states only verified recovery/limits. Root reruns package after integration and records a new candidate hash.

All original task history remains intact. Runtime/native receipt contract is an upstream dependency for the two implementation lanes. A fresh Oracle round follows the completed remediations; round-1 FAIL remains audit evidence.

Round-1 implementation evidence so far: native state reconciliation tests were
observed red (missing production method), then green; the combined harness reached
24/24 after transaction quantity/release and replay scenarios. The documented
patch-check default now passes without an explicit plugin path. Native crafting
stages its result before installation, and NativeDropPlan assigns stable object
identities before remainder publication. TerminalRegistration supplies the real
invisible SCS_StorageJournal prefab. These are implementation checks, not final
approval. An optional parallel native-boundary Oracle could not be spawned
(`agent thread limit reached`); its surface remains part of the required fresh
composed verification after the backend writer completes.

During convergence, root found that installing a player inventory could replace
ItemData references still held by equipment and the bulk-deposit queue. Backend
ownership includes preserving unchanged equipped instances in ApplyLayout; root
owns Storage/UI/DepositSelection.cs and the terminal queue, which resolve each
selected stack by exact identity and slot and cap it at the originally selected
quantity. The added production-helper regression was observed red, then the
combined harness passed 26/26. This remains implementation evidence pending the
fresh composed Oracle.

## Convergence — verification round 2

- [x] T055 Repair V2-03 (contradicts, Critical), FR-010/FR-011/FR-012/FR-013/FR-017 and SC-005/SC-006. Owner: root with supporting request/recovery tests and processor adapters in `Storage/Runtime/StorageService.cs` | Verify: host/remote share durable custody before suppression; uncertain capture resumes under the same ID; require-all re-queries capacity after space is freed without locking unrelated player actions.
- [x] T056 Repair V2-01 (partial, High), FR-001/FR-005/FR-006/FR-013/FR-017 and SC-002/SC-006. Owner: root with shared status projection and public status tests in `Storage/Core/StorageTransactions.cs` | Verify: retained confirmed 8-of-10 reconstructs requested 10, accepted 8, remaining 2 through the runtime's shared status seam.
- [x] T057 Repair V2-02 (missing, High), FR-004/FR-017 and SC-001/SC-005/SC-006. Owner: root with projection and request error handling in `Storage/Runtime/StorageDiscovery.cs` | Verify: deduplicated direct-plus-terminal union has a global bound; oversized and parsed malformed requests respond explicitly instead of hanging.

The backend writer completed and root owns these bounded coupled remediations.
Production was frozen during round 2. Its FAIL is verify-report.round2.md; round 1 is
preserved separately. A new fresh Oracle must pass before archive.

Round-2 remediation evidence: T056/T057 production seams were observed red, then
green. Root added StorageCaptureFlow (shared host/remote capture ordering),
StorageRequestIntent (immutable intent plus fresh per-attempt discovery), and
StorageEffects public status projection/reordered-response protection. The
combined executable passes 32/32. Parsed malformed requests now reply, final
status can be queried without a loaded anchor, terminal proofs are selected from
actual contributions, and pending processor output does not lock unrelated player
inventory. Dead host capture/context-recovery paths were removed during simplify.
The rebuilt five-entry candidate has SHA256
`DC944728D5496FA7EAD2E0CFE94532E87993037887742DABF83ED9BCCB4CF551`;
net48 build passes with the three known publicizer warnings, and metadata checks
pass 28 explicit targets. Fresh round-3 verification is the next gate; no live
outcome or release approval is inferred from this evidence.

## Final closeout evidence

Fresh /root/oracle_terminal_verify_round3 returned PASS for completeness,
correctness and coherence, with no deterministic V3-xx finding. The independent
review reran 32/32 scenarios, Release build, 28 explicit metadata targets and
whitespace checks, inspected the five-entry package and verified that its DLL
matches the fresh build. verify-report.md records its exact verdict and complete
FR/SC disposition; prior FAIL reports are retained as round1 and round2 history.
The reviewed ZIP remains DC944728D5496FA7EAD2E0CFE94532E87993037887742DABF83ED9BCCB4CF551.
SC-008/R3-SC008 and SC-009/R3-SC009 remain explicit unobserved live outcomes.
T005–T046 and T048–T057 are complete on this independent evidence. T047 is
finalized by successful closeout and transactional archive; the execution wrapper
restores pending state if either operation fails. No live validation, deployment,
publication or commit is implied by these completion markers.
