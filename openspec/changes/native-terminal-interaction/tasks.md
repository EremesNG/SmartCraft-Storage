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
- [~] T014 Record fresh independent completeness/correctness/coherence verification for all FR and SC in `openspec/changes/native-terminal-interaction/verify-report.md` | Verify: every buildable criterion has actual evidence; live outcomes have observed PASS or explicit ID-matched RISK and actionable findings drive convergence.
- [ ] T015 Archive only the independently passing correction and declared deltas in `openspec/changes/native-terminal-interaction/archive-report.md` | Verify: closeout validation and canonical requirement synchronization succeed without altering the prior archived audit trail.
- [x] T016 [US1] Change network naming to configurable Alt+T for FR-001/FR-002/FR-008 and SC-001/SC-006 in `Hotkeys/HotkeyPatch.cs` | Verify: hovered eligible chest/terminal naming retains access gates, old Alt+E naming hook is absent, UI focus suppresses the action, configured help and the 0.7.2 package agree.
- [x] T017 [US2] Resolve live finding UI-001 (partial FR-001: query succeeds but native cells are hidden) with a Unity regression fixture for FR-001/FR-008 and SC-003/SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: reproduce native viewport collapse before the fix, render a usable native grid after external inset reapplication, preserve ordinary-chest layout on restoration, and package a separate correction without modifying the user's game installation.
- [x] T018 [US2] Block character controls while terminal search is focused for FR-001/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: native text-input visibility follows active search focus, preserves other dialogs, releases on blur or close, existing focused suites and Harmony metadata pass, and a separate 0.7.4 package is produced for live typing acceptance.
- [x] T019 [US2] Prevent held toolbar clicks from being canceled by refresh for FR-001/FR-007/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: only projection refresh freezes on raw mouse-down; idle controls remain enabled, drag/split/pending safeguards persist, existing suites/build/Harmony checks pass, and a separate 0.7.5 package is ready for the user's repeated-click scenario.
- [x] T020 [US2] Implement the imagegen-guided compact native terminal for FR-001/FR-008 and SC-006/SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: eight columns fill the pane, the scrollbar stays above the three-action footer, weight reflects the full unfiltered network, native gestures/focus/click guards survive, ordinary layout restores, and implemented geometry is inspected with the existing Unity fixture.
- [x] T021 [US1] Make Alt+N the default while retaining editable naming shortcuts for FR-002/FR-008 and SC-001/SC-006 in `Hotkeys/HotkeyConfig.cs` | Verify: new installs use Alt+N, existing custom or disabled bindings survive, a migrated default is not imposed again on later customizations, and instructions/package metadata agree.
- [x] T022 [US2] Correct first-terminal native scrollbar cache contamination and misleading empty cells for FR-001/FR-008 and SC-007 in `Storage/UI/NativeTerminalInventory.cs` | Verify: reproduce first-view cache failure before correcting it; alternate terminal/ordinary views without moving the native scrollbar; show only occupied grouped entries with adaptive rows; retain empty-area deposit events and exact physical capacity reporting.

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
