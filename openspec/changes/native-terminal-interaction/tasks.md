# Tasks: Native terminal interaction and reliable linking

## MVP scope

US1 is independently testable: name an accessible single-player chest, retain
its confirmed result and reopen the world without obsolete-ID lookups. Delivery
also includes US2/US3 native interaction; fixing linking alone does not close it.

## Dependencies

T001 precedes Group P1 and freezes the destination-slot facade and isolated test
wiring. A fresh deep specialist owns Core/Runtime and existing test bodies after
root freezes test entry/project wiring. Root retains all artifacts and shared
contracts. A fresh designer owns UI/native-action adapters and NativeUiScenarios only. Each
lane follows one failing observable scenario with its minimum implementation,
then repeats; test tasks denote those vertical cycles, not a batch of imaginary
tests written before any implementation. T002 -> T003 -> T004 -> T005 -> T006 ->
T007; T008 -> T009 -> T010 -> T011. Both lanes must return terminal evidence before
T012 -> T013 -> T014 -> T015. If dispatch is unavailable, root executes the same
owned surfaces sequentially and records that capability gap. Children never
spawn agents or mutate another lane's files.

## Shared setup

- [x] T001 Freeze backward-compatible destination-slot contract and wire separate runtime/UI test entry points for FR-005/FR-008 in `Storage/Runtime/StorageFacade.cs` | Verify: existing automatic callers still compile, named destinationSlot calls compile, and independent UI tests need no shared project edits.

## Runtime correction: US1 and US2

- [x] T002 [P] [US1] Reproduce stale world/session IDs and invalid item hydration through production adapter APIs for FR-003/FR-006 and SC-002/SC-003 in `tests/Storage.Runtime.Tests/Program.cs` | Verify: saved records survive manager replacement without obsolete-ID lookups and serialized items retain shared metadata, cheated flag and exact identity; each regression exposes the original failure before its paired fix.
- [x] T003 [P] [US1] Scope journal references and record caches to valid runtime context for FR-003 and SC-002 in `Storage/Runtime/StorageJournal.cs` | Verify: no old-generation ID is queried, pooled root metadata is checked, and persisted transactions/effects remain available after re-entry.
- [x] T004 [P] [US1] Drive immediate/delayed naming and directed withdrawal regressions for FR-002/FR-003/FR-005/FR-007 and SC-001/SC-004/SC-005 in `tests/Storage.Contracts.Tests/Program.cs` | Verify: each added public scenario is observed red before the corresponding production change and asserts exact final status, counts or slot identity.
- [x] T005 [P] [US1] Correct naming dispatch, final status, per-target pending lookup and transient context lifecycle for FR-002/FR-003/FR-007 and SC-001/SC-002/SC-005 in `Storage/Runtime/StorageService.cs` | Verify: synchronous replies cannot be overwritten, unrelated requests do not lock naming, re-entry reloads requests without clearing durable custody and request parsing accepts older automatic withdrawals.
- [x] T006 [P] [US2] Implement exact-slot withdrawal planning and supporting faithful game payload hydration for FR-005/FR-006 and SC-003/SC-004 in `Storage/Core/StoragePlanner.cs` | Verify: empty and compatible clicked slots receive exact accepted quantities, incompatible/invalid slots move zero, automatic placement still passes and GameInventoryAdapter reconstructs usable metadata-preserving items.
- [x] T007 [P] [US1] Compose authority/status/request integration for FR-002/FR-003/FR-005/FR-007 and SC-001/SC-002/SC-004 in `Storage/Runtime/StorageRpc.cs` | Verify: owner acknowledgements and current player snapshots remain authenticated; local/delayed and backward-compatible request paths match the tested contracts.

## Native presentation: US2 and US3

- [x] T008 [P] [US2] Drive production projection, gesture and single-flight state scenarios for FR-001/FR-004/FR-006/FR-007 and SC-003/SC-005 in `tests/Storage.Contracts.Tests/NativeUiScenarios.cs` | Verify: red-first cases cover exact identity, legal stack amounts, deferred refresh, one submitted operation and cancellation of unsubmitted bulk work.
- [x] T009 [P] [US2] Implement native inventory projection and gesture routing for FR-001/FR-004/FR-005/FR-006/FR-007 and SC-003/SC-004/SC-005 in `Storage/UI/NativeTerminalInventory.cs` | Verify: actual InventoryGui grids handle drag/split/quick transfer, directed withdrawals, search/sort/bulk/Organize and every projected use/drop/write exit is intercepted.
- [x] T010 [P] [US1] Replace the main overlay and preserve targeted naming drafts/status for FR-001/FR-002/FR-007 and SC-001/SC-005 in `Storage/UI/StorageTerminalUi.cs` | Verify: no select-plus-transfer-button UI remains, naming uses only its target's request, a final name survives reopen, ordinary chest layout is restored and MonoBehaviour lifecycle signatures are valid.
- [x] T011 [P] [US3] Coordinate native callbacks, localized feedback and lifecycle cleanup for FR-001/FR-004/FR-006/FR-007 and SC-003/SC-005 in `Storage/Integration/StorageMutationGuards.cs` | Verify: pending operations, right-click, outside drop, controller/touch entry and destruction guards cannot mutate projection copies or bypass real reservations.

## Verification and delivery

- [x] T012 Simplify and integrate both terminal-completed lanes for FR-001 through FR-008 and SC-001 through SC-006 in `Storage/UI/NativeTerminalInventory.cs` | Verify: no ownership overlap or unused compatibility path remains, both test executables and the composed plugin build pass.
- [x] T013 Update correction metadata, operating instructions and installable package for FR-008 and SC-006 in `README.md` | Verify: native gestures, incompatible-slot behavior, single-player regression steps and remaining live risks match version 0.7.1 code and package.
- [x] T014 Record fresh independent completeness/correctness/coherence verification for all FR and SC in `openspec/changes/native-terminal-interaction/verify-report.md` | Verify: every buildable criterion has actual evidence; live outcomes have observed PASS or explicit ID-matched RISK and actionable findings drive convergence.
- [ ] T015 Archive only the independently passing correction and declared deltas in `openspec/changes/native-terminal-interaction/archive-report.md` | Verify: closeout validation and canonical requirement synchronization succeed without altering the prior archived audit trail.
- [x] T016 [US1] Change network naming to configurable Alt+T for FR-001/FR-002/FR-008 and SC-001/SC-006 in `Hotkeys/HotkeyPatch.cs` | Verify: hovered eligible chest/terminal naming retains access gates, old Alt+E naming hook is absent, UI focus suppresses the action, configured help and the 0.7.2 package agree.
- [x] T017 [US2] Resolve live finding UI-001 (partial FR-001: query succeeds but native cells are hidden) with a Unity regression fixture for FR-001/FR-008 and SC-003/SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: reproduce native viewport collapse before the fix, render a usable native grid after external inset reapplication, preserve ordinary-chest layout on restoration, and package a separate correction without modifying the user's game installation.
- [x] T018 [US2] Block character controls while terminal search is focused for FR-001/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: native text-input visibility follows active search focus, preserves other dialogs, releases on blur or close, existing focused suites and Harmony metadata pass, and a separate 0.7.4 package is produced for live typing acceptance.
- [x] T019 [US2] Prevent held toolbar clicks from being canceled by refresh for FR-001/FR-007/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: only projection refresh freezes on raw mouse-down; idle controls remain enabled, drag/split/pending safeguards persist, existing suites/build/Harmony checks pass, and a separate 0.7.5 package is ready for the user's repeated-click scenario.
- [x] T020 [US2] Implement the imagegen-guided compact native terminal for FR-001/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: eight columns fill the pane, the scrollbar stays above the three-action footer, weight reflects the full unfiltered network, native gestures/focus/click guards survive, ordinary layout restores, and implemented geometry is inspected with the existing Unity fixture.
- [x] T021 [US1] Make Alt+N the default while retaining editable naming shortcuts for FR-002/FR-008 and SC-001/SC-006 in `Hotkeys/HotkeyConfig.cs` | Verify: new installs use Alt+N, existing custom or disabled bindings survive, a migrated default is not imposed again on later customizations, and instructions/package metadata agree.
- [x] T022 [US2] Correct first-terminal native scrollbar cache contamination and misleading empty cells for FR-001/FR-008 and SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: reproduce first-view cache failure before correcting it; alternate terminal/ordinary views without moving the native scrollbar; show only occupied grouped entries with adaptive rows; retain empty-area deposit events and exact physical capacity reporting.

## Convergence: NET-001 remote send saturation

- [x] T023 [US3] Correct NET-001 (contradicts FR-007, partial FR-003/FR-008) with root-owned elapsed-time retries, admitted-request status polling and transport backpressure for SC-005/SC-006/SC-009 in `Storage/Runtime/StorageService.cs` | Verify: observe red before the fix at the clock/socket seams, preserve pending requests and custody, allow new authenticated progress, and keep repeated-frame traffic bounded on client and server.
- [x] T024 [US3] Exercise NET-001 corrected runtime wiring and package the 0.7.8 source and local-merge candidates with root-owned tests/artifacts and fresh Oracle judgment for FR-008 and SC-006/SC-008/SC-009 in `openspec/changes/native-terminal-interaction/verify-report.md` | Verify: focused suites/build/Harmony/package checks pass, prior packages are retained, source branch remains isolated, remote live acceptance and unavailable independent review stay explicit.

NET-001 evidence: user reports no error with the prior local-merge DLL installed
on both client and server, unlike 0.7.7 with idle automatic processing. Diagnostic
logs and a read-only save-field inspection were retained under local-merge/dist/diagnostics/
send-limit-20260920. No source or player-save mutation occurred during diagnosis.
T023 passes 33 native network checks, including custody-preserving fresh-intent
polling and backoff under alternating replies. Existing 40 contract and 4 runtime
checks pass. T024 root integration passes 33 network, 15 cart and 18 overlay
checks, source/combined packaging and 39/5 Harmony/lifecycle checks; prior 0.7.7
ZIPs are retained. See evidence/network-implementation-checks.md. Fresh
oracle_storage_network_078_final failed to start (agent thread limit reached),
so T024's independent judgment and T014/T015 remain open, not self-approved.
The bounded deep implementation dispatch could not start (agent thread limit);
root owns the same declared surfaces sequentially. T014/T015 remain open.

## Convergence: BUSY-001 acknowledgements lost during recovery

- [x] T025 [US3] Correct BUSY-001 (contradicts FR-003/FR-007, partial FR-008) for SC-005/SC-006/SC-009 with root-owned runtime acknowledgement lifetime and recovery wiring in `Storage/Runtime/StorageRpc.cs` | Verify: reproduce a complete local output delivery remaining busy across service resumes, then finish exactly once and release chest/station reservations; retain authentication, concurrent-operation isolation, owner-change recovery and 0.7.8 backoff.
- [~] T026 [US3] Verify BUSY-001 against real native service/RPC participant lifecycles, existing suites, a fresh Oracle and source/local-merge 0.7.9 packages in `openspec/changes/native-terminal-interaction/verify-report.md` | Verify: preserve old packages and saves, distinguish synthetic execution from live acceptance, and obtain independent final findings before closeout.
- [x] T027 [US3] Correct BUSY-001 durable-reference and orphan-custody recovery for FR-003/FR-006/FR-007 and SC-005/SC-006/SC-009 with deep-owned journal and service changes in `Storage/Runtime/StorageJournal.cs` | Verify: native world ID remapping preserves participant identity; journal availability gates mutations; exact profile/world capture proof permits bounded recovery, conflicting or absent proof preserves custody; fresh operations remain valid after identity stamping.

T025 pre-fix evidence: actual native service Resume/GetOperation/IsBusy failed
four assertions with a Preparing delivery after 40 recovery attempts. The RPC
fix passes complete local delivery, conservation and release. A second qualified
red proves applied receipts were ignored after reservations were released;
that case now passes without replaying the inventory layout. Delayed replies,
rebinds, interleaved operations, changed owners, runtime/anchor participants and
the host player are exercised through native RPC dispatch. The current combined
network fixture has 53 passing checks; final candidate validation is pending.

T027 evidence: readonly native v41 parsing of copied Pruebas .46/.47 saves
finds three applied captures, three matching prepare-only delivery reservations,
and no matching journal records. Native Load remaps ZDO IDs; SCS string references
remain stale. A separate readonly profile decode proves exact world/actor,
pending output, profile escrow, source outbox and capture-data agreement for all
three operations. Root owns artifacts and RPC tests; the deep recovery writer
owns journal/service and unique durability fixtures. Diagnostic inspectors own
only ignored helper directories. See evidence/busy-implementation-checks.md.

User observes both chests and cooking stations permanently busy after entering
the local test world with the matching 0.7.8 DLL. Root owns production/tests and
artifacts for continuity. Independent oracle_storage_busy_local_diagnosis owns
read-only code diagnosis; deep_busy_world_inspection owns only ignored diagnostic
tooling and read-only saved-world evidence. Neither owns product edits.
Oracle identifies acknowledgement HashSets discarded when ResumeTransaction
rebinds participants; host output delivery also uses this remote RPC path.
Root first tests the public service Resume/GetOperation/IsBusy and native
inventory boundaries using a restored journal, not a mocked participant.

## Parallel execution

### Group P1

- Lane L1: T002 -> T003 -> T004 -> T005 -> T006 -> T007 | Owner: deep
- Lane L2: T008 -> T009 -> T010 -> T011 | Owner: designer
- Prerequisites: T001
- Barrier: T012
- Rationale: Runtime/Core and root harness paths are disjoint from UI/native-callback and dedicated NativeUiScenarios paths. Neither lane consumes unfinished peer output: both compile against the frozen facade and existing implementation; composed behavior waits at T012. Root exclusively owns test-project/entry wiring and artifacts. The designer cannot edit runtime or root tests. Dispatch admitted independent work before waiting; a thread-limit failure permits the documented sequential root fallback.

## Final verification

Run both focused test executables, Release build, explicit Harmony target/argument
checks and package inspection with cached local game references and no network.
Inspect the real native callback and host/remote service links beyond the pure
model tests. Attempt actual single-player naming/re-entry, native-grid gestures,
search, partial capacity, ordinary chest restoration and screenshot observation
with the user's available game session. SC-007/SC-008 are outcome targets, not
artificial implementation tasks; report all unobserved visual, controller,
mod-interaction and remote multiplayer cases explicitly. A fresh read-only Oracle
judges the final composed candidate independently; earlier plan or 0.7.0 approval
never substitutes for that verdict.

T001 evidence: optional destinationSlot facade and host signature compile; existing callers remain unchanged. Root wired NativeUiScenarios and the pure TerminalInteractionModel conditionally into the existing harness before assigning one writer per lane. Release build passed with 0 errors and the three known CS0436 publicizer warnings. The review is recorded in plan-review.md. The corrected local candidate is now packaged separately as 0.7.1.

## Implementation evidence

- Root executed both lanes sequentially after both independent dispatch attempts returned agent thread limit reached. This is the declared capacity fallback, not delegated completion.
- Red-first observations: stale manager lookup threw obsolete native ID table; display lacked shared item metadata; inline naming ended RecoveryPending after Confirmed; directed withdrawal accepted 15 instead of the clicked slot's 10; projection initially had zero expected cells; bulk initially did not submit, and an unknown reply incorrectly released its wait; changed-slot intent accepted altered contents; duplicate owner naming overwrote a later name. Each paired production fix now passes.
- Final focused suites: 40/40 contract cases and 4/4 production runtime fixture cases, logged in evidence/contracts.log and evidence/runtime.log. Fixture boundaries and limits are documented in tests/Storage.Runtime.Tests/README.md.
- Simplify review preserved native gestures, exact identity/amounts, final status, finite capacity and durable custody. Removed the obsolete list/selection/quantity workflow, reused native controls and existing receipt storage, retained only a separate naming dialog, and kept the interaction queue independent of Unity. No unrelated cleanup or dependency change.
- Release/package succeeded with 0 errors and the three pre-existing CS0436 warnings. Metadata checks pass 39 explicit Harmony targets and 5 Unity lifecycle signatures. ZIP entries and embedded DLL hash/version are checked in evidence/package-inspection.json.
- Candidate: dist/SmartCraftStorage-0.7.1.zip, SHA256 F70DCE0506EC63800CA2976010FC083C3A93D84A15911F77743D267FE78FA747. The prior 0.7.0 ZIP hash is unchanged. Package inspection caught the old default assembly file version; SmartCraftStorage.csproj now explicitly sets Version 0.7.1, the package was rebuilt, and the final embedded DLL/manifest/file-version inspection passes.
- User requested to play the prior version while the correction is prepared, then install and test the new candidate. No automatic installation, live server write or corrected-game observation occurred.
- T014 capacity gap: the fresh oracle_native_terminal_final spawn also returned agent thread limit reached. No independent verdict exists; verify-report.md records NOT RUN and the checked evidence without self-approval. T014 and T015 remain open, and archive-report.md records deferred closeout.

T016 follow-up evidence: user requested Alt+T to avoid an Alt+E conflict. Root
reused Player.Update input handling and native range-limited GetHoverObject,
removed the Container.Interact naming hook and terminal Alt+E branch, and kept
eligibility/privacy/ward/busy gates. Shared eligibility and configured hint
helpers remove duplication between actual naming and hover help. Existing
tests pass 40/40 + 4/4, package build has 0 errors and the same 3 warnings,
metadata resolves 38 Harmony targets (one old naming hook removed) and 5 Unity
lifecycle methods. See evidence/alt-t-*. The initial ready check caught T016
listed before T012; after ordering the identifiers it is valid. Version 0.7.2
is packaged separately and the 0.7.1 ZIP is unchanged. T016 is implemented and
checked; T014 remains the independent review barrier before T015. No live
keyboard/ExtraSlots observation is claimed.

T014 retry for the unchanged 0.7.2 candidate: fresh
`oracle_native_terminal_072_final` dispatch again returned `agent thread limit
reached`. This is an unavailable verification capability, not reviewer failure
or approval; T014/T015 remain open.

T017 evidence: screenshot retained as evidence/terminal-0.7.2-blank-grid.png.
Real headless Unity reproduction passed the native control and failed the old
terminal layout at viewport width -40. Preserving horizontal stretch and native
panel width passes all 5 engine assertions, including four native icons/counts
and restoration to 15 ordinary chest cells (grid-layout-red.log/green.log).
The fixture replays the inspected external inset setter; it does not load all
mods or a world. Its initial control setup and missing native drag predicate
were repaired before treating the fixture as evidence. Final existing suites
remain 40/40 and 4/4; package build has 0 errors and 3 known warnings, metadata
38 Harmony targets/5 lifecycle methods, exact versioned ZIP inspection passes.
Fixture dependencies initially entered SDK candidate assembly resolution;
excluding tests from root None items and disabling fixture copy-local resolved
the extra Cecil warning without changing product dependencies. Simplify reused
the existing RectState capture/restore in the tested layout helper; transaction
and hotkey behavior stay unchanged. Version 0.7.3 is ready for user installation
and live acceptance. Fresh oracle_native_terminal_073_final again failed with
agent thread limit reached; no independent verdict exists, T014/T015 stay open.

T018 evidence: user reports the terminal now looks correct, then reports WASD
moving the character during search. Native PlayerController.TakeInput checks
TextInput.IsVisible separately from inventory shortcuts. The new postfix only
adds true while the active search has focus; native FixedUpdate clears movement
and action controls, and blur/close cannot leave a stored block. The same focus
predicate guards inventory shortcuts. Existing suites remain 40/40 and 4/4;
Release/package has 0 errors and 3 known warnings, metadata resolves 39 Harmony
targets and 5 lifecycle methods, and the exact five-entry 0.7.4 ZIP matches its
DLL/versions. Evidence is search-focus-*. No new tests were added for this
reversible input-only patch; source inspection does not claim live typing PASS.
Simplify kept one focus predicate with no per-frame counter or persistent state.
No inventory, layout, transaction or shortcut semantics were otherwise changed.
The 0.7.3 ZIP remains intact. Fresh oracle_native_terminal_074_final also failed
with agent thread limit reached; T014/T015 remain open. The user installs and
tests the new candidate; no game installation/config/save write was performed.

T019 evidence: the user clarified that visible contents were correct and no
status appeared on failed Organize clicks; buttons blinked and repeated clicks
alone eventually worked. The raw mouse-down branch in Interacting disabled
controls on each 200 ms refresh. Native Unity Button.Press only dispatches when
IsInteractable is true. The correction keeps pointer-down freezing only the
projection, while drag/split and operation gates still protect actions. Search
also stays enabled through its own click and retains the 0.7.4 focus gate.
This user reproduction and native source inspection are pre-fix evidence, not
an automated click test or a claim that the corrected game was observed.
Existing suites pass 40/40 and 4/4; Release/package has 0 errors/3 known warnings,
metadata resolves 39 Harmony targets/5 lifecycle methods, and exact versioned
ZIP inspection passes. See evidence/toolbar-click-*. Simplify retained the
existing UI structure and separated projection freezing from control gating;
no additional abstraction, test harness, backend or range change was needed.
Version 0.7.5 is separately packaged and 0.7.4 remains unchanged. Fresh
oracle_native_terminal_075_final hit agent thread limit reached; T014/T015 are
still open. No game/config/save changes or corrected live-game PASS is claimed.

T020/T021 evidence: built-in imagegen produced the selected compact concept
with the native weight pouch and no help strip; final prompts/images are retained
under evidence/terminal-compact-*. Production uses native controls with eight
columns/four visible rows, a separate scrollbar, search/order above and three
actions below. Complete unfiltered network weight goes to m_containerWeight.
Rect/hierarchy/title restoration and the existing input/transaction guards remain.
The real Unity fixture first reproduced 24 instead of 32 visible cells and the
missing Alt+N default/migration, then passes 18 geometry/config assertions.
Synthetic GuiBar/platform fixture setup was corrected; no engine errors remain.
Weight and native widget assignment are inspected source evidence; no live 0.7.6
skin/rendering acceptance is claimed. Final contract/runtime checks pass 40/40
and 4/4; Release/package has 0 errors/3 known warnings, 39 Harmony targets and
5 lifecycles, exact five-entry ZIP inspection and ready validation pass. See
evidence/compact-implementation-checks.md and compact-*. Simplify centralizes
geometry and removes obsolete help/reset code without backend changes.
0.7.6 is separately packaged; 0.7.5 hash is unchanged. Fresh
oracle_native_terminal_076_final again hit agent thread limit reached; no
independent verdict exists, so T014/T015 remain open. User installs/tests this
candidate; development made no installed game/config/save changes.

T022 evidence: user screenshots show the native scrollbar near center and the
full network with padded cells. Their restarted native-chest-first test stays
correct, consistent with the lazy-cache cause. A new real Unity UpdateGui hook
replay reproduces terminal-first contamination (X=-16), then passes at native
X=284 after initialization precedes terminal geometry. Repeated alternation,
adaptive grouped-entry rows, hidden padding, real transparent-cell raycasting,
native deposit selection and ordinary empty-cell restoration pass. The fixture
was qualified for post-Canvas rendering and isolated scene startup before the
raycast result was accepted. Final checks: 27/27 Unity/config, 40/40 contracts,
4/4 runtime, Release 0 errors/3 known warnings, 39 Harmony targets/5 lifecycles,
exact matching 0.7.7 ZIP and ready validation. See restore-grid-implementation-checks.md
and associated evidence logs. Simplify shares clamped row/action positioning
and explicit native initialization without mutating external private state.
The 0.7.6 ZIP remains unchanged. Fresh oracle_native_terminal_077_final could
not start because of the native agent thread limit; T014/T015 remain open and
no independent verdict or corrected live-game PASS is claimed.

## Convergence: BUSY-001 independent review round 1

Fresh independent review `oracle_storage_busy_079_final` returned FAIL. The
61 native checks pass but do not close the following three gaps. These are
same-intent recovery corrections, not permission to broaden storage behavior.

- [x] T028 [US3] Correct BUSY-079-001 (contradicts FR-003/FR-007, SC-005) with deep-owned restart-safe legacy cleanup in `Storage/Runtime/StorageService.cs` | Verify: a durable recovered effect without its cleanup record resumes and releases only proven reservations after a failure/restart between writes.
- [x] T029 [US3] Correct BUSY-079-002 (contradicts FR-003/FR-006, SC-005) with deep-owned independently authenticated legacy custody in `Storage/Runtime/StorageService.cs` | Verify: host profile intent and escrow bind the actor independently of RPC fields; remote legacy claims without actor-bound source proof fail closed; future capture records persist actor/world proof.
- [x] T030 [US3] Correct BUSY-079-003 (contradicts FR-003/FR-006/FR-007, SC-005) with root-owned missing-reference provenance handling in `Storage/Runtime/StorageJournal.cs` | Verify: loading a legacy transaction/effect without a stable reference trailer never binds or stamps a different object occupying its stale raw ID; unavailable references stay unresolved.

T030 implementation evidence: two qualified behavior failures reproduce both
embedded-root migration and separate legacy records resolving a reused raw ID.
The corrected loader marks every unproven native reference unresolved before
migration can persist it, preserving effect identity/custody and leaving the
unrelated current object unstamped. Runtime suite now passes 7/7. See
busy-legacy-reference-red.log and busy-legacy-reference-green.log. The independent
finding remains subject to a fresh verification round; this checkbox records
completed implementation evidence, not root approval of the composed candidate.

T028/T029 implementation evidence: qualified native failures cover an incorrect
persisted actor, a recovered effect missing cleanup, and future source authority.
The service now records cleanup before reconstructed custody, reconstructs an
interrupted cleanup from exact durable source evidence, and checks old delivery
receipts beyond the active reservation set. Legacy host recovery compares the
independently read saved intent/body/escrow/descriptor; RPC claims are not proof.
New capture authority binds operation/world/actor in one fixed source field,
matching the single current capture-intent field and clearing only its own
operation. This avoids introducing a new per-output dictionary key. Root ran a
qualified red/green refinement for that bounded source field. Native suite is
67/67; Release has 0 errors/3 known warnings. Writer checks are complete;
a fresh independent verdict and combined package checks remain pending.

## Integration after independent source PASS

Fresh `oracle_storage_busy_079_round2` closes BUSY-079-001/002/003 and approves
all source FR/buildable SC, preserving live SC-007/008 risks. T014/T024/T025/T027
now have independent evidence; T026 distribution checks and T015 closeout remain.

- [~] T031 Preserve the independently updated local-merge ALT-lock, cart and overlay changes while integrating reviewed storage-net 0.7.9 for FR-006/FR-008 and SC-005/SC-006 in `Stations/CookingStationPatches.cs` | Verify: resolve any product conflicts without losing either protection, run storage/cart/overlay/protected-input/Epic Loot regressions and obtain a fresh independent judgment if product resolution is required.
