# Verification Report: Native terminal interaction and reliable linking

**Reviewer**: oracle<br>
**Independent from implementer**: Yes<br>
**Verdict**: PASS

Oracle instance: `oracle_storage_combined_079`.

## Review dimensions

- **Completeness**: PASS — both source recovery and target reconciliation cover every accepted FR and buildable SC.
- **Correctness**: PASS — qualified recovery/protection failures converge; 171 focused checks and exact package equality support the independent verdict.
- **Coherence**: PASS — source/target version 0.7.9, merge parents, documented protections and test limitations agree.

## Compliance matrix

| Requirement | Implementation evidence | Executed check | Result |
| --- | --- | --- | --- |
| FR-001 | NativeTerminalInventory and prior restore-grid evidence | Prior 27 native UI checks retained; contract/native checks and independent source review | PASS |
| FR-002 | StorageNameFlow, service naming and configured shortcut | 40 contract checks and independent source review | PASS |
| FR-003 | StorageJournal stable references and StorageRpc durable recovery | 7 runtime and 73 native checks, busy-combined-runtime.log and busy-combined-network.log | PASS |
| FR-004 | Sequential terminal deposits and exact protection | 40 contract checks, native projection/queue evidence retained | PASS |
| FR-005 | StoragePlanner directed and automatic withdrawal | 40 contract checks include exact-slot cases | PASS |
| FR-006 | Custody proof, mutation guards and protected chest cost predicates | 73 native, 7 runtime and 4 protected input checks | PASS |
| FR-007 | Retry/backpressure and target-specific pending states | Native timing, authenticated progress, reconnect and exact-release checks | PASS |
| FR-008 | Versioned source and combined packages, documented limitations | Exact five-entry ZIP/embedded DLL equality and version inspections | PASS |
| SC-001 | Reliable naming and owner acknowledgement | Contract naming cases and independent source review | PASS |
| SC-002 | Journal world/manager lifetime and stable identity | 7 runtime and native rebind/restart cases | PASS |
| SC-003 | Native projection/hydration and display mutation guards | 40 contracts, runtime hydration and prior 27 UI checks | PASS |
| SC-004 | Immutable slot expectation and withdrawal plan | Contract exact/partial/incompatible/changed-slot scenarios | PASS |
| SC-005 | Finite protected costs, recovery and single-flight work | 73 native conservation/release/recovery/lock cases and 40 contracts | PASS |
| SC-006 | Compiled integration and exact distribution contents | 171 focused checks, Release0errors/3knownwarnings,39Harmony/5lifecycle, package inspection | PASS |
| SC-007 | Corrected local Steam/UI and full live kiln remain unobserved | User manual local test is the next observation | RISK |
| SC-008 | Matching-client Steam and remote lock propagation remain unobserved | Two-client and remote legacy custody acceptance remain manual | RISK |
| SC-009 | Bounded polling/retries and admission progress | Native clock/socket/backpressure/reconnect cases retained | PASS |

## Findings

No unresolved blocking finding. BUSY-079-001/002/003 are independently closed;
the historical findings and exact verdicts remain below and in the evidence.

## Residual risks

- SC-007: No corrected live local Steam/UI or full kiln run; test ordinary chest/station access, terminal gestures, processing and conservation after rejoin.
- SC-008: No matching-client Steam acceptance or new end-to-end remote locked-cost replay; verify two clients, owner changes and pending recovery. Legacy remote captures without independent actor proof remain pending.

## Final independent combined review

`oracle_storage_combined_079` returns PASS for merge commit
`a406c53f5b2532086656959a1eda81fb8dd82aa7`: completeness, correctness and
coherence all pass, with no actionable blocker. Its exact returned judgment is
preserved in `evidence/busy-combined-oracle.md`. The reviewed-source matrix below
remains applicable; the combined review additionally verifies that owner/access,
ALT-lock protections, cart discovery, overlays and Epic Loot survive reconciliation.

FR-001 through FR-008 and buildable SC-001 through SC-006/SC-009 pass. SC-007
remains RISK for corrected local Steam/UI and complete live kiln behavior;
SC-008 remains RISK for matching-client Steam behavior and remote lock propagation.
The cap accumulator is tested directly; remote cost protection shares the local
predicate but was not newly replayed end-to-end. These are explicit residual
outcome risks, not invented observed acceptance.

The committed target passes 171 focused checks: 73 native, 40 contracts,
7 runtime, 15 cart, 18 overlays, 4 protected inputs and 14 Epic Loot variants.
Release has 0 errors/3 known warnings; 39 Harmony targets and 5 lifecycle
signatures resolve. The exact five-entry 0.7.9 ZIP contains the built DLL and
matching versions. Both source and combined package inspections are retained.
Prior ZIPs and the Desktop backup remain unchanged. No game/config/save writes
or corrected live-game acceptance are claimed.

Archive/provenance-only commits may require a final rebuild; compare production
sources with the reviewed commits and record final distribution hashes in
BUILD-INFO.txt. Any later product-code change requires fresh independent review.

## Source independent review: BUSY-001 round 2

Fresh read-only Oracle `oracle_storage_busy_079_round2` returned PASS for
completeness, correctness and coherence. BUSY-079-001/002/003 are closed:
cleanup is durable before recovered custody and reconstructs after interruption;
legacy actor custody comes from independently verified host profile or exact
operation/world/actor source authority; unproven native references become
unresolved before migration can stamp a different object. No blocking product
finding remains in the reviewed source.

| Requirement | Independent result | Evidence |
| --- | --- | --- |
| FR-001 | PASS | Unchanged native terminal and prior focused presentation checks |
| FR-002 | PASS | Naming path and contract checks |
| FR-003 | PASS | Durable identity, cleanup, receipts, rebinds and journal gating |
| FR-004 | PASS | Finite sequential deposit behavior retained |
| FR-005 | PASS | Directed and automatic withdrawal contracts retained |
| FR-006 | PASS | Exact custody, conservation, no replay and unresolved legacy references |
| FR-007 | PASS | Bounded retries retained; pending, busy and final states distinguished |
| FR-008 | PASS for source | Version/build/docs agree; final merged package equality pending |
| SC-001 | PASS | Naming checks |
| SC-002 | PASS | Stable references and world/manager replacement checks |
| SC-003 | PASS | Projection and hydration guards |
| SC-004 | PASS | Exact-slot checks |
| SC-005 | PASS | 67 native checks include interruption, duplicate prevention and exact release |
| SC-006 | PASS for source | 40 contracts, 7 runtime, 67 native checks; Release 0 errors/3 known warnings; 39 Harmony targets/5 lifecycle signatures |
| SC-007 | RISK | Corrected 0.7.9 live local Steam/UI acceptance is unobserved |
| SC-008 | RISK | Matching two-client Steam acceptance and live owner propagation are unobserved |
| SC-009 | PASS | Timing, status polling, backpressure, authenticated progress and reconnect checks |

The actual protected profile/world diagnostic confirms all three local orphan
captures satisfy the reviewed host proof. This is readonly custody evidence,
not observation of the corrected game. Remote legacy captures without actor-bound
source proof intentionally remain pending. Native owner serialization preserves
the newer identity marker; live replication remains an explicit SC-008 risk.

Distribution still requires a clean merge, combined checks and exact package
inspection. During review `local-merge` was independently reset/rebuilt at
`70ea14db5e5b09e2cf348f8ee6ca71d9d2b5f937` with additional ALT-lock protections.
Root must preserve those changes. Any product conflict resolution requires a
fresh independent integration review rather than treating this as a mechanical
merge. No candidate was installed and no live save was modified.

## Historical independent review: BUSY-001 round 1

Completeness and correctness FAIL; artifact/code coherence PASS. Native checks
61/61, existing contracts 40/40 and Release 0 errors/3 known warnings establish
the successful paths but do not close these independent findings:

| ID | Severity | Dimension | Evidence | Remediation anchor |
| --- | --- | --- | --- | --- |
| BUSY-079-001 | HIGH | Restart safety | Recovered effect may persist without its cleanup record; LegacyCleanupComplete returns false forever | T028, service legacy cleanup |
| BUSY-079-002 | HIGH | Authorization | RPC-supplied actor/world fields do not bind the original legacy actor; actual producer-derived IDs contain no actor | T029, independent host profile or source proof |
| BUSY-079-003 | CRITICAL | Conservation/authorization | A missing reference trailer leaves stale raw IDs intact; legacy migration may stamp the wrong current object | T030, journal reference provenance |

FR-003/FR-007 and SC-005 FAIL on 001/003; FR-006 FAIL on 003; permission boundary
FAIL on 002. SC-009 and prior traffic limits PASS at the focused native seam.
FR-008/SC-006, combined integration and final packaging remain pending. SC-007
live UI and SC-008 Steam/two-client acceptance remain explicit RISK. Native
owner snapshot deserialization preserves the newer server identity marker;
the earlier hypothesized deletion is not supported by current evidence.

The independent next action is to converge all three findings, add failure/
restart, wrong-actor and missing-trailer/stale-valid-ID tests, rerun focused
checks and obtain a fresh Oracle verdict. No candidate was installed or offered
as independently passing.

## Historical verification and capacity limits

The fresh oracle_native_terminal_final dispatch returned `agent thread limit
reached`. There is no Oracle result to persist. Root does not substitute its own
implementation checks for the mandatory independent verdict. The prior plan
[OKAY] and archived 0.7.0 PASS do not approve this candidate.

## Review dimensions

- **Completeness**: implementation and operating instructions are present; independent assessment pending.
- **Correctness**: focused checks below passed; independent review and actual game acceptance remain pending.
- **Coherence**: root checked source/package versions and requirement links; independent assessment pending.

## Compliance evidence awaiting independent judgment

| Requirement | Implementation evidence | Executed check | Result |
| --- | --- | --- | --- |
| FR-001 | NativeTerminalInventory, StorageTerminalUi, TerminalInteractionModel | Projection/count/filter and bulk scenarios; Harmony target inspection | Checks passed; Oracle pending |
| FR-002 | StorageNameFlow, StorageService, StorageRpc, naming dialog | Inline/delayed/rejected name flow and duplicate owner receipt cases; target pending predicate | Checks passed; Oracle pending |
| FR-003 | StorageJournal, service runtime context, status merge | Manager replacement, pooled objects, world return, final-status cases | Checks passed; Oracle pending |
| FR-004 | Native bulk adapter, DepositSelection, interaction model | Replaced instance/protected-source adapter inspection, sequential/unknown/close/rejection cases | Checks passed; Oracle pending |
| FR-005 | StoragePlanner, StorageSlotExpectation, withdrawal RPC | Exact empty/partial/incompatible/invalid/changed slot and automatic placement; old trailing-body source inspection | Checks passed; Oracle pending |
| FR-006 | GameInventoryAdapter, display-only registry, StorageMutationGuards | Metadata/display/layout/equipment cases; native pre-remove DropItem and item/Inventory write guards inspected | Checks passed; Oracle pending |
| FR-007 | MatchesTarget, pending lookup, native state model | World/target/kind matching, frozen mapping and single-flight cases | Checks passed; Oracle pending |
| FR-008 | README, CHANGELOG, Plugin, manifest, csproj, package | Build/version/ZIP and reproducible focused test logs | Checks passed; Oracle pending |
| SC-001 buildable | Name dispatch and owner receipt integration | Contract name cases; authenticated ZDO write and callback chain inspected | Checks passed; Oracle pending |
| SC-002 buildable | Journal and service lifecycle | Production journal fixture and service cache reset inspection | Checks passed; Oracle pending |
| SC-003 buildable | Projection, hydration and mutation guards | Contract/runtime fixtures and native callback inspection | Checks passed; Oracle pending |
| SC-004 buildable | Directed withdrawal and immutable slot expectation | Contract cases; backward-compatible request parse inspected | Checks passed; Oracle pending |
| SC-005 buildable | Per-target pending and sequential queue | Target predicate, pending, rejection, unknown and close cases | Checks passed; Oracle pending |
| SC-006 buildable | Composed build and delivery | 40/40 contracts; 4/4 runtime; 39 Harmony targets; 5 lifecycle methods; exact ZIP DLL/version | Checks passed; Oracle pending |
| SC-007 outcome | Local native UI/link/re-entry acceptance | User observed a blank grid in 0.7.2; isolated Unity correction checks pass, full-mod 0.7.3 acceptance pending | RISK |
| SC-008 outcome | Two-client acceptance | Not executed | RISK |

## Findings and capability gaps

| ID | Severity | Dimension | Evidence | Remediation anchor |
| --- | --- | --- | --- | --- |
| CAP-001 | Blocking independent closeout | Verification capability | Fresh native Oracle spawn failed: agent thread limit reached | Run a fresh read-only Oracle when agent capacity is available; keep T014/T015 open |
| UI-001 | Historical; corrected | Correctness / FR-001, SC-007 | User's 0.7.2 blank-grid screenshot led to T017; later user screenshots show rendered entries | Preserve horizontal stretch; newer regressions are tracked under T022 |
| UI-002 | Corrected in candidate; live acceptance pending | Correctness / FR-001, SC-007 | 0.7.6 normal scrollbar shifts after terminal-first cache initialization; native-first sequence remains correct | T022 / 0.7.7: prime native layout before terminal changes, then repeat first-terminal test in game |
| UI-003 | Corrected in candidate; live acceptance pending | Coherence / FR-001, SC-007 | Full 30/30 network shows sixteen occupied entries plus sixteen misleading empty cells | T022 / 0.7.7: adaptive occupied-entry display; verify blank-space deposits and physical capacity label in game |

No product PASS/FAIL findings are attributed to an uncreated Oracle. See
evidence/implementation-checks.md for root evidence and the boundaries of the
external game fakes. The first package inspection found the default 1.0.0.0
assembly file version; setting csproj Version 0.7.1 and rebuilding resolved it.

## Executed checks

- `dotnet run --project tests/Storage.Contracts.Tests --no-restore`: 40/40, evidence/contracts.log.
- `dotnet run --project tests/Storage.Runtime.Tests --no-restore`: 4/4, evidence/runtime.log.
- `scripts/package.ps1 -ValheimManagedDir C:/Users/EremesNG/AppData/Local/Temp/SCS-game-references-20260919 -NoRestore`: Release build 0 errors, 3 known CS0436 warnings, evidence/package.log.
- `scripts/check-patches.ps1` against the same references: 39 explicit Harmony targets/parameters and 5 Unity lifecycle signatures, evidence/patches.log.
- ZIP manifest, exact file set, file version 0.7.1.0 and matching embedded DLL: evidence/package-inspection.json.
- SDD validator through ready: valid, conditional checklist not activated. Closeout was not attempted without independent PASS.

Prior local candidate: dist/SmartCraftStorage-0.7.1.zip.
ZIP SHA256: F70DCE0506EC63800CA2976010FC083C3A93D84A15911F77743D267FE78FA747.
DLL SHA256: 4ECC2E6FEDDD38613E0331759967A090892AB61898CA71452935182CDAA6D2A6.

## Prior local candidate: 0.7.2 naming shortcut refinement

The user requested `Alt + T` to replace `Alt + E`. T016 changes only the naming
entry point, its configurable hint, UI focus gating and package instructions.
The prior root checks remain applicable to the unchanged inventory/runtime
implementation. Re-executed checks for 0.7.2:

- Contract/runtime suites: 40/40 and 4/4; evidence/alt-t-contracts.log and alt-t-runtime.log.
- Release package: 0 errors, 3 existing CS0436 warnings; evidence/alt-t-package.log.
- Metadata: 38 explicit Harmony targets and 5 Unity lifecycle methods; evidence/alt-t-patches.log. The removed target is the old Container.Interact naming prefix.
- Exact ZIP contents, matching embedded DLL, manifest 0.7.2 and file version 0.7.2.0; evidence/alt-t-package-inspection.json. Previous 0.7.1 ZIP remains unchanged.
- Ready validator valid; evidence/alt-t-ready.json. Conditional checklist remains unactivated; this bounded shortcut refinement adds no storage contract risk.
- Source inspection: native Player.Update refreshes the hover target, GetHoverObject uses interaction distance, TakeInput suppresses inventory/chat/console/menu input, and the additional IsOpen guard suppresses custom naming input. Chest eligibility, privacy, ward and busy checks are preserved; terminal wards remain checked. Quick-stack/restock retain precedence. Configured None hides the naming hint and disables the shortcut.
- Simplify: reused the existing shortcut detector, consolidated chest eligibility and configured help, and removed the obsolete interaction interception. No transaction or persistence behavior changed.

Prior package: dist/SmartCraftStorage-0.7.2.zip.
ZIP SHA256: 4132AFC86EBFCBFC130A2D4C5EC68A381288A737835366AEF8DCA9A971799C67.
DLL SHA256: D1B8E0B12EC04F7156E8E759EA0F94F53E3DC41C149B6E98DC4E6804526015B3.
These are root execution checks, not an independent approval. Alt+T, custom
rebinding and ExtraSlots coexistence still require observation inside Valheim
(SC-007).

Fresh dispatch `oracle_native_terminal_072_final` also returned `agent thread
limit reached`. CAP-001 therefore still applies to this candidate. No reviewer
was created, no PASS is inferred, and T014/T015 remain open. The installed
thoth-sdd skill requires that "materially risky Direct work and every
Accelerated or Full final verify require a fresh read-only Oracle"; local
packaging can be delivered for user testing, while independent closeout stays
pending.

## Prior local candidate: 0.7.3 native viewport correction

UI-001 diagnosis: user screenshot reports 2 members and 4/30 occupied physical
slots, but no grid cells. At diagnosis the installed DLL matched 0.7.2. BepInEx
confirmed Valheim Plus Inventory enabled and no terminal exceptions. Its local
InventoryGrid_UpdateGui_Patch reapplies a right inset via offsetMax.x and restores
cached panel width. SCS had changed the grid to fixed left anchors. The tested
combination produces width -40 and clips the grid despite ten native elements.
NativeTerminalLayout now keeps horizontal stretch and the existing panel width;
the same external inset operation yields width 560 with all ten cells visible.

Executed evidence (root checks, no independent approval):

- Actual Unity/Valheim InventoryGrid: 5/5 assertions, evidence/grid-layout-green.log. Valid baseline control and old-layout failure: grid-layout-red.log. Empty cells, external insets, four icons/counts and ordinary-chest restoration covered. Only the external setter is simulated; no full-mod scene or pixel screenshot PASS is implied.
- Existing contracts 40/40 and runtime 4/4: grid-contracts.log and grid-runtime.log.
- Package/Release build: 0 errors, 3 known CS0436 warnings: grid-package.log. Initial test dependency candidate-resolution warning was eliminated before this final build; product dependencies are unchanged.
- Metadata: 38 Harmony targets and 5 Unity lifecycle methods: grid-patches.log.
- Exact five ZIP entries, matching embedded DLL, manifest 0.7.3 and file version 0.7.3.0: grid-package-inspection.json. Previous 0.7.2 ZIP is unchanged; the test plugin is excluded.
- Ready validator valid, conditional checklist not activated: grid-ready.json. Task-format validation was corrected before completion.
- Simplify: one helper owns native grid geometry and the existing reversible RectState; production BuildToolbar uses that helper. No storage/network/gesture protocol changes.

Prior package: dist/SmartCraftStorage-0.7.3.zip.
ZIP SHA256: 2F935A422CF79D564364C6151F62B16BCBF4FDC010E21E118592C13D01FA9182.
DLL SHA256: 62FDC0C0DF9FC3F4963C0E42284C1F1E22B60EF4BC023A795B1B7F993F07C78C.

Fresh `oracle_native_terminal_073_final` dispatch returned `agent thread limit
reached`; CAP-001 persists and no final approval is issued. UI-001 is corrected
in source and passes the isolated engine regression, with the user's full-mod
observation of 0.7.3 still required for SC-007. SC-008 remains unobserved.
The test ran in its own temporary process with its own plugin/config/save paths
and suppressed startup; it loaded no world or character. Root did not install a
DLL in the user's game or modify game configuration/saves. The installed DLL
hash changed outside this task after the initial 0.7.2 check; no attempt was made
to overwrite that external change.

## Prior local candidate: 0.7.4 search input focus

User follow-up: "Todo se ve bien, por ahora" confirms the visible terminal
improvement after the viewport correction. This is limited user observation,
not a blanket pass for all SC-007 scenarios. New finding UI-002: typing WASD in
the search field still moves the character. Native PlayerController.TakeInput
permits keyboard movement while inventory is open and independently checks
TextInput.IsVisible. The existing terminal prefix only guarded InventoryGui.Update.

Correction: the TextInput.IsVisible postfix adds true only while the terminal's
active search field is focused. It never replaces another dialog's true result
with false. Native PlayerController.FixedUpdate then supplies zero movement and
false action controls when input is blocked. SearchHasFocus is also used by the
inventory shortcut prefix. Inactive/destroyed fields, blur and the terminal's
existing close path remove this gate without a counter or delayed refresh.
Enter/Escape retain the existing field-deactivation behavior.

Executed evidence (root checks, not independent approval):

- Existing contract/runtime suites: 40/40 and 4/4, evidence/search-focus-contracts.log and search-focus-runtime.log. These are regression checks and do not simulate typing.
- Release/package: 0 errors, 3 known CS0436 warnings, search-focus-package.log.
- Metadata: 39 explicit Harmony targets and 5 Unity lifecycle methods, search-focus-patches.log. The new target is native TextInput.IsVisible.
- Exact five ZIP entries, embedded DLL hash match, manifest 0.7.4 and assembly/file version 0.7.4.0, search-focus-package-inspection.json. The 0.7.3 ZIP is unchanged.
- Ready validator valid; search-focus-ready.json. Conditional checklist stays unactivated for this bounded input correction.
- Source inspection: cached PlayerController.decompiled.cs lines 74-84, 222-240 and 257-266; production NativeTerminalInventory.SearchHasFocus, SearchInput.Postfix and TextInput.Prefix. No Unity test of typing or full-mod control blocking is claimed.
- Simplify: shared the live focus predicate between both native input paths; no mutable input-block ownership/counter, new helper abstraction, dependency or unrelated cleanup. Existing layout, gestures and transaction behavior are preserved.

Prior package: dist/SmartCraftStorage-0.7.4.zip.
ZIP SHA256: 5E69AC8EE5A5B984183681F6E12930DD55CE9DB2F09FE8E12F8489A1FAC8D968.
DLL SHA256: BAA8A00BAE21DE5D38D61A848DBB86346620F656018695404C018F482534A746.

Fresh oracle_native_terminal_074_final dispatch returned agent thread limit
reached. CAP-001 remains and the verdict is still NOT RUN; T014/T015 stay open.
UI-002 is corrected in source and packaged for the user's local typing check;
SC-007 remains RISK until that behavior is observed. No game installation,
configuration or save was modified.

## Current local candidate: 0.7.5 toolbar click correction

UI-003: the user initially associated successful organizing with manually
opening backing chests. Clarification establishes that the terminal already
showed correct contents. Failed clicks produced no status and made buttons
blink; repeated Organize clicks alone eventually worked. Their reproduction:
put objects directly in a linked chest, deposit/withdraw through the terminal,
close/reopen it, and click Organize. This is an input dispatch fault rather
than evidence of a required inventory-open dependency.

The source cause is deterministic for a click overlapping refresh: Interacting
included ZInput.GetMouseButton(0), and the 200 ms Refresh used Interacting to
set all buttons' interactable flags false. Native Unity Button.OnPointerClick
calls Press, which invokes onClick only when IsActive and IsInteractable are
true. The callback could therefore be discarded before Organize ever ran.

Correction: Interacting now covers only actual item dragging and a split
dialog. Raw mouse-down stays in the projection-freeze condition to preserve
stable item identities, while idle controls remain interactable through their
own clicks. Search uses the busy/item-gesture gate instead of the projection
freeze. CanAct still checks closing, pending/queued work, available view and
teleporting. Existing drag, split, operation and text-focus guards remain.

Executed evidence (root checks, no independent approval):

- User reproduction above plus native UnityEngine.UI.Button source lines 37-51 in the local SCS-organize-20260919 inspection directory. No new automated test was added for this reversible three-line UI condition correction.
- Existing contracts 40/40 and runtime 4/4: evidence/toolbar-click-contracts.log and toolbar-click-runtime.log. These preserve inventory/runtime behavior and do not simulate mouse timing.
- Release/package: 0 errors, 3 known CS0436 warnings: toolbar-click-package.log.
- Metadata: 39 Harmony targets, 5 Unity lifecycle methods: toolbar-click-patches.log.
- Exact five ZIP entries, matching embedded DLL, manifest 0.7.5 and assembly/file version 0.7.5.0; previous 0.7.4 ZIP unchanged: toolbar-click-package-inspection.json.
- Ready validator valid, conditional checklist unactivated: toolbar-click-ready.json.
- Simplify: separated stable-projection timing from control eligibility using the existing fields and predicates. No new abstraction, dependency, backend change or unrelated cleanup.

Prior package: dist/SmartCraftStorage-0.7.5.zip.
ZIP SHA256: 8BAD29518BC0331F5A4CFB408DF6B9772A16FF486D1A329AE7E60509020580EE.
DLL SHA256: A68EC0F6DD50D368AED4757F6FFFD062D83D366E67D6358E88AC103C65ED1A36.

Fresh oracle_native_terminal_075_final dispatch returned agent thread limit
reached. CAP-001 persists; verdict remains NOT RUN and T014/T015 stay open.
SC-007 still needs live 0.7.5 validation with both quick clicks and a held press
spanning refresh, without first opening backing chests. The user's local config
was read and shows TerminalRadius = 32, matching the default; allowed config
range remains 1-100 meters. Root did not install the candidate or modify game,
configuration or save data. The latest available game log identified 0.6.0,
so it was not treated as evidence of this candidate's runtime behavior.

## Prior local candidate: 0.7.6 compact terminal and configurable Alt+N

The user requested an imagegen-guided native layout with dense use of space,
weight in the existing chest indicator, a configurable Alt+N default and no
drag/Shift/Ctrl footer. T020/T021 implement that same-intent refinement. Root
took both bounded lanes sequentially after the fresh designer dispatch hit
the native agent thread limit.

The selected built-in imagegen concept is
evidence/terminal-compact-concept-final.png; prompts and superseded corrections
are in evidence/terminal-compact-imagegen-prompts.md. It is a concept, not an
implemented-game capture. Product widgets remain native.

Executed checks and limitations are detailed in evidence/compact-implementation-checks.md:

- Compact geometry red: 24 visible cells instead of the requested 32.
- Shortcut red: fresh default and legacy-default migration failed as expected.
- Final isolated Unity/native-grid/BepInEx fixture: 18/18, zero captured engine
  errors, including scrolling/restoration and persisted custom/disabled choices.
- Production contract/runtime suites: 40/40 and 4/4.
- Release package: 0 errors and 3 existing CS0436 warnings.
- Metadata: 39 Harmony targets/arguments and 5 lifecycle signatures.
- Exact five ZIP entries, matching DLL and version 0.7.6 / file version 0.7.6.0;
  previous 0.7.5 ZIP unchanged.
- Full ready validator valid; existing conditional checklist stays unactivated.
- Simplify consolidated native geometry/restoration and removed unused help
  text and the premature view reset, preserving storage and input contracts.

Prior package: dist/SmartCraftStorage-0.7.6.zip.
ZIP SHA256: 2EE617A25BE0ABFBA3995C04E025204AEC682D16B17E3941312D4894898CAB95.
DLL SHA256: 57003F9575C6A20FD1BB99F22A242EC449AA7ACFE8217F7960971C789219CB38.

These checks do not establish full-skin/live rendering or multiplayer acceptance.
The native weight assignment uses unfiltered StorageView rows and the native
quality-aware weight method; observing its placement/value under the installed
UI mods is part of SC-007. No game/config/save installation writes were made.
Independent final verification is still required; T014/T015 remain open until
a fresh Oracle can issue a result.

Fresh `oracle_native_terminal_076_final` dispatch returned `agent thread limit
reached`. No reviewer was created and no independent verdict exists. CAP-001
continues to block final closeout; it does not invalidate the executed root
checks or prevent providing the local candidate for user testing. The installed
thoth-sdd SKILL.md:72 requires "a fresh read-only Oracle" for Full final verify.
Root therefore leaves the verdict NOT RUN and archive deferred.

## Prior local candidate: 0.7.7 native baseline and grouped-entry cells

The user's screenshots establish two live 0.7.6 regressions: a normal chest's
scrollbar near the center and a full 30/30 network appearing to have extra
empty slots. Restarting and opening a normal chest first avoids the scrollbar
issue. The 0.7.6 synthetic fixture missed the layout hook's first-use cache.

T022 corrects the baseline initialization and replaces minimum-row padding
with occupied-entry display and adaptive height. Root evidence and fixture
limits: evidence/restore-grid-implementation-checks.md. Checks passed:

- First-terminal cache and padded-display regressions reproduced before fixes.
- Final Unity/config fixture: 27/27, including real transparent-cell raycasting,
  native deposit selection and restoration of normal empty cells; no engine errors.
- Existing contract/runtime suites: 40/40 and 4/4.
- Release/package: 0 errors, 3 known CS0436 warnings; 39 Harmony targets and 5 lifecycles.
- Exact five-entry ZIP, matching DLL/versions; 0.7.6 remains unchanged.
- Full ready validator valid; existing conditional checklist unactivated.

Current package: dist/SmartCraftStorage-0.7.7.zip.
ZIP SHA256: A67691D1CE238658FEED14A6821C2C739AAF64C59EBC13E195338B55A2850BF8.
DLL SHA256: 492340703120C83CF6B67531ED4A37435974558A24FCACBAC904B1240009C016.

SC-007 still needs live 0.7.7 acceptance: restart, open terminal first, then normal
chests; inspect grouped entries/height and deposit over blank space. The user's
new normal-chest-first test is diagnostic evidence for 0.7.6, not corrected
0.7.7 acceptance. SC-008 remains a multiplayer risk. No installed game/config/
save writes were made. Fresh independent final verification is still required.

Fresh `oracle_native_terminal_077_final` dispatch returned `agent thread limit
reached`. No reviewer was created, CAP-001 persists, the verdict remains NOT RUN
and T014/T015 stay open. The installed thoth-sdd SKILL.md:72 requires "a fresh
read-only Oracle"; root does not replace that verdict with its own checks.

## Current local candidate: 0.7.8 bounded network recovery

NET-001 is a same-intent FR-003/FR-007/FR-008 correction. The user confirms that
matching 0.7.7 floods the remote connection while idle, while the pre-storage
backup on both endpoints does not. The real production service reproduces 402
packets for two pending processor requests in an immediate Tick/Resume burst;
the corrected service sends two and waits. An additional reply-feedback case
reproduced 3,600 sends in a simulated minute and now sends 11.

See evidence/network-implementation-checks.md and network-*.log for qualified
red/green evidence and fixture limitations. Current source checks pass:

- 33 actual-Unity network checks; 40 existing contracts; 4 runtime checks.
- Release build: zero errors, three known CS0436 publicizer warnings.
- 39 Harmony targets/arguments and 5 Unity lifecycle signatures.
- Full ready validator and whitespace checks; existing checklist warning only.

The fix preserves pending intent/custody, uses status polling after admission,
limits retries by time on client and server, and defers retryable payloads while
the routed socket is congested. Fresh-intent control replies preserve output
custody and backoff. UI and station resource semantics are unchanged.

Fresh oracle_storage_network_078_final could not start: native agent thread
limit reached. Independent verdict is NOT RUN; CAP-001 and T014/T015 remain open.
T023 has implementation evidence; T024 remains in progress for the independent
judgment. Root completed the integration checks on local-merge: 33 network,
15 cart-discovery and 18 overlay assertions pass. Together with the unchanged
source contract/runtime checks, this supplies 110 focused passing assertions.
The UI and shortcut source is unchanged from the prior tested 0.7.7 candidate;
the previous 27 UI checks were not rerun or counted in this total.

Implementation commit dc50935 was merged as 4c4c757, retaining cart/overlay
compatibility. The only conflict was the changelog; both local additions and
0.7.8 notes were preserved. Source and combined 0.7.8 packaging and 39/5 metadata
checks passed; previous 0.7.7 ZIPs remain unchanged. A following documentation-only
commit records these results; final DLL/ZIP hashes and commit metadata are kept
in dist/local-merge/BUILD-INFO.txt in the combined worktree. Final packaging
rebuilds from that worktree's committed head. No installed game/config/server/
save writes or real Steam acceptance are claimed.

## Residual risks

- SC-007: the user accepts the compact design but reported the 0.7.6 native scrollbar and filler-cell regressions. Corrected 0.7.7 first-terminal restoration, adaptive display, blank-space deposits, native weight, label fit/UI scale, search/control release, gestures, naming/re-entry, gamepad/touch and installed-mod interactions still need observation. Source inspection, cache replay and synthetic Unity UI are not full-game verification.
- SC-008: remote owner response delays, multiple clients, dedicated server and MultiUserChest simultaneous use need matching-version multiplayer acceptance.
- The runtime fixture does not execute Unity or the actual native legacy inventory loader. New exact item payloads are exercised through the production adapter; persisted legacy payload loading still requires an in-game observation.
- No automatic installation, server deployment, publication or alteration of world/character saves was performed. Local source/merge commits preserve the feature branch for a future upstream PR.
