# Verification Report: Native terminal interaction and reliable linking

**Reviewer**: unavailable — native agent capacity<br>
**Independent from implementer**: independent review was not executed<br>
**Verdict**: NOT RUN — no final approval issued

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

## Current local candidate: 0.7.7 native baseline and grouped-entry cells

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

## Residual risks

- SC-007: the user accepts the compact design but reported the 0.7.6 native scrollbar and filler-cell regressions. Corrected 0.7.7 first-terminal restoration, adaptive display, blank-space deposits, native weight, label fit/UI scale, search/control release, gestures, naming/re-entry, gamepad/touch and installed-mod interactions still need observation. Source inspection, cache replay and synthetic Unity UI are not full-game verification.
- SC-008: remote owner response delays, multiple clients, dedicated server and MultiUserChest simultaneous use need matching-version multiplayer acceptance.
- The runtime fixture does not execute Unity or the actual native legacy inventory loader. New exact item payloads are exercised through the production adapter; persisted legacy payload loading still requires an in-game observation.
- No automatic installation, server deployment, publication, commit or alteration of world/character saves was performed.
