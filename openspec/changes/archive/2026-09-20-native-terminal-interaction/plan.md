# Implementation Plan: Native terminal interaction and reliable linking

## Technical context

The user tested only single player. The current 0.7.0 terminal is a custom
1040x700 two-list overlay with explicit quantity/transfer buttons. The user
requires native chest gestures. The local OdinStorage 0.9.2 DLL also uses custom
rows, so its search/aggregation ideas inform the design while the explicit
native requirement controls the implementation.

Confirmed runtime evidence: ZRoutedRpc.InvokeRoutedRPC executes local handlers
synchronously. StorageService.HandleServerRequest publishes RecoveryPending
AFTER ApplyName can synchronously publish Confirmed, then Remember overwrites
that final result. The UI's global pending key can carry the stuck ID into
unrelated naming dialogs. Live Player.log also reports ArgumentOutOfRangeException
from ZDOID.GetUserID/GetHashCode through StorageJournal.WorldZdo during world
loading. The journal retains a ZDO reference across ZDOMan replacement and looks
it up before checking the manager generation. The plugin/service outlive worlds.

Live inspection of the reopened 1920x1080 game captured the old terminal with
network general, one linked chest and one used slot, but blank item lists and
pending feedback. Player.log additionally reports StorageTerminalUi.Name throwing
because a deserialized ItemData has null m_shared, and Unity rejects the custom
Start(operation, kind) method as an invalid lifecycle message. Native
Inventory(bool) marks a temporary inventory; AddTempItem reconstructs bare
ItemData without m_shared and omits m_cheated. GameInventoryAdapter currently
uses that path for DeserializeItem. Correct payload hydration, including the
cheated flag and prefab shared data, is required before the native UI is usable.

Cached game assemblies and ILSpy support offline inspection/build. Current
contract tests are pure production seams; a focused runtime-journal harness is
needed to reproduce obsolete native-context references. Preserve all existing
uncommitted implementation and the prior archived review history.

## Constitution Check (pre-design)

- **User-value first**: PASS — native gestures and functioning linking directly address the user's first live acceptance report.
- **Simplicity and bounded scope**: PASS — reuse InventoryGui and the existing transaction backend; no atomic swap, alternate storage model or station-policy expansion.
- **Testable contracts**: PASS — agreed transfer/query/status/gesture seams gain red-first naming, destination-slot and world-reload regressions; visual results remain separate.
- **Independent assurance**: PASS — a fresh designer provided bounded read-only discovery; fresh Oracle review and final verification use independent agents. A failed explorer dispatch is handled by root read-only investigation.
- **Traceable delivery**: PASS — this new correction change references the archived initial implementation and modifies exact existing canonical requirement titles.

## Design

### Remote send saturation correction (0.7.8)

Live finding NET-001 contradicts FR-007 bounded retries and leaves SC-008 unmet:
the matching 0.7.7 client/server floods Steam's outgoing queue after spawning,
without input. The user confirms the earlier local-merge DLL on both ends removes
the error. The saved profile has two pending processor cost intents; Tick resumes
them every frame and reconstructs all inventory snapshots. This is same-intent
convergence for FR-003/FR-007/FR-008 and SC-005/SC-006/SC-008/SC-009.

Owner: root, retaining loaded request/recovery context and a single ordered writer
for runtime integration. A bounded deep retry-policy lane was attempted but the
native agent thread limit prevented dispatch; use the truthful sequential fallback.
Mutable surface: Storage/Core/StorageRetrySchedule.cs, Storage/Runtime/StorageService.cs,
Storage/Runtime/StorageRpc.cs, focused contract/network tests, candidate metadata,
README/CHANGELOG and this audit. Preserve native UI, permissions, IDs, durable records,
item custody and existing transaction outcomes. No installed game/server/save changes.

Use monotonic elapsed-time retries (initial immediate, then 0.5/1/2/4/8 seconds,
capped at 8), shared by Tick and all explicit Resume callers. Only authenticated
new progress resets an operation's timer; context replacement clears transient timing.
After server admission, poll small status requests instead of repeating inventories.
An unknown server record or output needing a fresh delivery plan explicitly requests
fresh intent; naming retains its small retryable command. Bound retryable payload
dispatch using the actual routed connection's send queue; acknowledgements/results
and effect-release messages retain delivery so flow control cannot strand custody.

Verification uses the accepted transaction/status seams plus external clock and
socket boundaries: red-first dense-frame/delayed-response tests, lost/late acknowledgement
recovery, independent operations, backpressure and reconnection. Exercise production
runtime wiring in an isolated fixture where feasible; do not claim the live multiplayer
failure fixed solely from pure policy tests. Run storage suites, affected native fixtures,
Release build, patch metadata and exact package inspection. Keep source storage-net
isolated and merge the correction into local-merge after validation. Request a fresh
Oracle final review; capability failure is recorded without asserting approval.

### First-view scrollbar and padded cells correction (0.7.7)

The user accepts the improved 0.7.6 visual design but shows a native chest with
its scrollbar near the center after terminal use. A second screenshot shows a
full 30/30-slot network with 16 grouped entries and 16 misleading empty cells.
Owner: root, preserving the ordered diagnosis and already-loaded native UI
context. Surface: NativeTerminalInventory, NativeTerminalLayout, the existing
real Unity native-grid fixture, candidate metadata/docs and this audit. No
storage, transactions, hotkeys or station changes. Requirements: FR-001/FR-008,
US2 scenarios 9-11 and SC-006/SC-007. This is same-intent convergence.

Native Valheim Plus LayoutContainerScrollbar lazily caches panel width, viewport
inset and scrollbar anchored X on its first container-grid UpdateGui. SCS 0.7.6
changes those coordinates before that first call; its right-anchored -16 offset
is later replayed onto the restored center-anchored native scrollbar. The prior
fixture restored RectTransforms but did not reproduce this cross-open cache.
Reproduce the exact first-use cache contract in the existing Unity seam before
fixing baseline initialization. Keep native geometry available to layout hooks
before terminal changes, without editing another mod's private static fields.

Show only actual grouped result entries, without visible rectangular filler.
Size the view to the needed rows (one empty deposit row through four visible
rows); preserve native pointer/drop handling over invisible padding, search,
scrolling and physical capacity counts. Verify native input/raycast behavior,
ordinary-cell restoration and first-view/repeated-view layout transitions.
Record the prior check gap honestly; synthetic cache replay is not the complete
Valheim Plus plugin. Package 0.7.7 separately, retaining 0.7.6 and the installed
game/config/saves. Re-run affected ready checks, focused suites, build/metadata/
ZIP checks and request a fresh Oracle final review; SC-007 stays observational.

### Live 0.7.2 blank-grid correction

The user supplied a screenshot showing network GENERAL, two chests and 4/30
occupied slots, with no rendered cells. Installed DLL hash matches the delivered
0.7.2 candidate. Current BepInEx logs contain no terminal exception and confirm
Valheim Plus 0.10.2.0 Inventory enabled. Its installed
InventoryGrid_UpdateGui_Patch restores grid.offsetMax.x (a native right inset)
and a cached panel width. SCS BuildToolbar instead anchors both horizontal edges
of that grid to the left through the generic Position helper. Restoring a
negative right inset with those anchors can make the viewport width negative,
which explains a populated query with entirely clipped cells. ExtraSlots'
inspected layout prefix only handles the player grid; EpicLoot's socket prefix
returns normally when its overlay is closed.

Owner: root, keeping the short ordered diagnosis/layout/check chain together.
Scope: FR-001/FR-008 and SC-003/SC-006/SC-007; one writer for
Storage/UI/NativeTerminalInventory.cs and a small production native-layout
helper, tests/Storage.Unity.Tests, correction metadata/docs and this audit.
First extract the existing layout unchanged and reproduce the viewport failure
using actual Unity RectTransforms and InventoryGrid in an isolated headless
runtime with external right-inset reapplication. Then preserve native horizontal
stretch/inset semantics and native panel width, verifying positive viewport size,
visible native cells and normal chest layout restoration. The agreed native UI
seam includes the rendered grid; no new transaction seam or storage behavior is
introduced. Validate both existing suites, build, patch metadata and ZIP. Keep
the user's installation, config and saves untouched; use a separate temporary
runtime and quit before loading a world. Actual full-mod visual acceptance and
independent Oracle review remain distinct from the isolated Unity check.

### Runtime correction

A fresh deep specialist owns Storage/Runtime/StorageService.cs, StorageRpc.cs,
StorageJournal.cs, GameInventoryAdapter.cs, Storage/Core/StoragePlanner.cs and runtime regression harnesses
after root freezes StorageFacade.cs and test wiring. If native capacity is
unavailable, root implements this same bounded lane as a documented fallback.
Publish a naming request's pending status BEFORE dispatch, and preserve final
outcomes when synchronous or delayed statuses are observed. Name operations
remain idempotent under the same ID and owner/ward checks. Do not clear escrow
or generic pending state to unblock a naming dialog.

Use a small production naming-dispatch seam (Storage/Core/StorageNameFlow.cs)
if needed to exercise immediate, delayed and rejected callbacks without Unity;
it must be called by the real server path, not be a parallel reference model.
GetPendingOperation(target, kind) must also find durable pending request intents
for the current anchor and kind, restoring profile requests as appropriate.
UI state must not infer a new pending transfer from an unknown legacy global ID.

GameInventoryAdapter.DeserializeItem must hydrate a real usable item from its
prefab and complete serialized state, not return Inventory(true)'s bare temporary
ItemData. Use the inspected native normal-inventory load or equivalent faithful
prefab hydration, preserving identity, cheated flag, custom data and dimensions.
Add a public serialization/display/layout round-trip regression that exposes
the temporary-inventory behavior and requires non-null shared name/icon metadata
and exact identity. Do not hide unresolved items by skipping rows or adding a
UI-only null check. The runtime test harness may link this actual adapter against
documented external game-boundary fakes where Unity cannot run headlessly; final
native source inspection and live inventory checks remain necessary.

Before any cached ZDO lookup, validate current object-manager/session/world
identity; invalidate transient journal references and reload persisted records
when it changes. StorageService similarly scopes transient result/request/member
caches to the current manager/session/world/actor, preserving effect handlers and
persisted world/profile records. Cover manager replacement with the same world
UID and pooled/reused ZDO references; verify cached root/record metadata before
reuse. Never delete durable journals to recover from this bug.

The new journal regression executable links the actual StorageJournal source
against small external game-boundary fakes. Its fake object-ID table rejects
old-generation lookups exactly as the observed native stack trace does. Tests
use Load/Save/LoadEffect rather than private methods; the code under test is the
production journal. Source inspection still validates the native lifecycle link.

### Frozen cross-lane API

The only new transfer parameter is:

    Withdraw(StorageContext context, string identity, int amount,
        string operationKey = null, int destinationSlot = -1)

Root first updates the facade, unavailable implementation and host/remote service
signatures so the UI can compile. -1 retains existing automatic placement.
A nonnegative slot is bounded by the real player's dimensions. The planner
accepts only that exact empty slot or a strictly identical partial stack and
returns zero for an incompatible/full/invalid slot; it never silently falls back.
Current player snapshots and reservation checks prevent stale-slot overwrite.
UI rejects incompatible targets with a localized explanation and cancels drag.

Carry the optional slot through the withdrawal RPC body. Persisted 0.7.0
withdraw requests have no trailing slot: read absent data as -1. Matching new
client/server versions remain required. No incompatible-item Exchange is added.

### Native presentation

Fresh designer implementation owns Storage/UI/StorageTerminalUi.cs,
Storage/UI/NativeTerminalInventory.cs, Storage/UI/TerminalInteractionModel.cs,
Storage/UI/TerminalTranslations.cs, Storage/UI/StorageTerminal.cs,
Storage/Integration/StorageMutationGuards.cs and
 tests/Storage.Contracts.Tests/NativeUiScenarios.cs. Root owns test-project wiring.
The designer must preserve unrelated changes and never write root runtime files.
If native dispatch is unavailable, root performs this same bounded lane
sequentially and records the fallback.

Replace only the main terminal screen with InventoryGui.Show(null), preserving
its real player grid and presenting the finite network in its existing container
pane. The projection is an ordinary in-memory Inventory used only to render
cloned ItemData. It is never saved, bound to a real Container, or submitted as a
transaction participant. One cell represents one strict identity; m_stack is
clamped to a legal stack and an identity map supplies aggregate amount labels.
Use the native five-column grid with bounded visible rows/scrolling, a compact
search/header/capacity strip, view sorting and Organize. Render identities rather
than all physical slots. Capture and restore modified layout, listeners, button
labels and controls on close so ordinary chests return to native behavior.

Intercept terminal-mode UpdateContainer, IsContainerOpen, OnSelectedItem,
OnReleasedItem, OnRightClickItem, OnDropOutside, OnTakeAll and OnStackAll and any
additional proven native callback that can bypass those guards. Reuse native
SetupDragItem and ShowSplitDialog, tooltips, quality/durability elements and
controller groups. Player-to-player actions remain vanilla. Network-to-player
uses the exact clicked slot; player-to-network accepts anywhere in the virtual
pane. Network-to-network is a no-op, projection right-click cannot use/equip, and
outside drop cancels instead of creating an item. Do not globally block native
InventoryGui with GUIManager.BlockInput(true).

Do not rebuild the identity map during a projected drag or split. Defer refresh
or cancel the drag explicitly when authority/target validity changes. Search
and cross-grid writes are disabled for a pending submitted operation. Native
quantity controls are legal-stack bounded; quick transfer moves one stack.
Take All handles full aggregate rows, with each next step resolving fresh
capacity only after prior confirmation. Stack All only deposits original
unlocked/unequipped matching identities, re-resolving source slot plus identity
using the existing DepositSelection rules. Closing discards only unsubmitted
queue entries. Resume the same durable ID when reopening.

Avoid parameterized MonoBehaviour lifecycle names: rename the old Start helper
to BeginOperation (or another non-message name), and check all new components'
Awake/Start/Update/OnDestroy signatures during metadata verification.

Keep ordinary-chest naming a small native-styled separate dialog, with per-world,
per-target draft/pending state. Use GetPendingOperation(target, "name") rather
than the global terminal pending flag. Retain draft text through pending/rejected
close/reopen, but only show a saved network name after authoritative confirmation.

### Requirement mapping

| Requirement | Technical decision | Files/interfaces | Verification seam |
| --- | --- | --- | --- |
| FR-001 | Native InventoryGui/ContainerGrid with compact search/network actions | Storage/UI/NativeTerminalInventory.cs, StorageTerminalUi.cs, TerminalTranslations.cs | Native gesture model plus live native UI observation |
| FR-002 | Pending-before-dispatch and per-target naming draft/state | Storage/Core/StorageNameFlow.cs, StorageService.SetNetworkName/GetPendingOperation, StorageTerminalUi.cs | Naming submission/result and reopen |
| FR-003 | Monotonic public final status and context-scoped caches | StorageService.Remember/Tick, StorageJournal.Load/Save | Synchronous callback and real journal source across manager replacement |
| FR-004 | Gesture deposit and single-flight bulk intents | NativeTerminalInventory, TerminalInteractionModel, DepositSelection | Confirmed transfer counts and queue submission order |
| FR-005 | Optional exact player destination end-to-end | StorageFacade.Withdraw, StorageService, StoragePlanner.Withdraw | Directed/automatic planner and request compatibility |
| FR-006 | Faithful item payload hydration and clone projection with all mutating native exits guarded | GameInventoryAdapter, NativeTerminalInventory, StorageMutationGuards | Serialization/display/layout round-trip, projection action routing and native hook inspection |
| FR-007 | Target-scoped pending/reopen; frozen drag map; explicit results | GetPendingOperation, TerminalInteractionModel, StorageTerminalUi | Delayed/final/reopen state and unsubmitted queue cancellation |
| FR-008 | Versioned correction package and regression evidence | Plugin.cs, manifest.json, README.md, CHANGELOG.md, scripts/check-patches.ps1 | Both harnesses, build, metadata, package and fresh Oracle |

## Optional support artifacts

### Compact native terminal and configurable Alt+N (0.7.6)

User requests an imagegen-led redesign using their BottomlessChest and current
SCS screenshots, followed by implementation. The built-in imagegen concept is
retained as evidence/terminal-compact-concept-final.png; source references and prompt
are retained alongside it. The concept is design guidance, not a game screenshot
or a raster replacement for interactive widgets. Keep native skin/fonts/items,
eight columns, a compact header with network capacity, real aggregate weight in
the existing native chest weight indicator in its usual location (explicit user
correction; the superseded first concept placed it in the header),
search beside ordering, a scrollbar confined to the grid, and one footer row for
Take all/Stack all/Organize. Omit the bottom drag/Shift/Ctrl help line as explicitly
requested; use the header metadata row for concise operation status. Preserve native gestures, focus/click corrections,
finite capacity and ordinary-chest restoration. Compute network weight from
unfiltered StorageView rows using native ItemData.GetWeight(row.Amount), so
aggregate stacks and quality-scaled weights remain accurate. Reuse the existing
Unity geometry harness to check scrollbar/footer separation, width use and
restoration, including the inspected Valheim Plus inset behavior. Render the
implemented geometry for visual inspection when the isolated runtime permits;
keep any full-mod/live outcome separate.

Ownership: a fresh designer is requested for Storage/UI/NativeTerminalInventory.cs,
NativeTerminalLayout.cs, TerminalTranslations.cs and tests/Storage.Unity.Tests
only. The now-complete image concept is its upstream input. Root concurrently
owns Hotkeys/HotkeyConfig.cs, any focused config checks, metadata/docs/artifacts
and packaging. These surfaces do not overlap. If native agent capacity rejects
dispatch, root takes the UI surface sequentially and records that capability
gap. The bounded follow-up preserves the approved storage/network contracts.
Dispatch of designer_compact_terminal_076 returned agent thread limit reached;
root therefore owns both implementation lanes sequentially. No child was admitted.

Hotkey: keep Hotkeys.NetworkNameShortcut client-editable, default LeftAlt+N.
Inspect existing configuration behavior before choosing a minimal one-time
migration of the former exact default; custom shortcuts and deliberate later
rebinding must survive restarts. Do not edit the user's installed config during
development. No unverified claim about all other mods' shortcut conflicts.
Requirements: FR-001/FR-002/FR-008, US2 scenario 9, SC-006/SC-007. Final checks:
existing focused suites, real Unity layout checks, build, Harmony/package/version
inspection, corrected image review and fresh independent Oracle. Package 0.7.6
separately and leave game/config/saves untouched.

### Toolbar click correction (0.7.5)

The user initially associated Organize with opening backing chests, then
confirmed that contents were already visible and repeated clicks alone make
Organize work. Failed clicks show no operation status and buttons blink.
NativeTerminalInventory.Interacting includes the left mouse button, and the
200 ms Refresh uses it to disable all toolbar controls. A refresh between
pointer-down and pointer-up therefore makes Unity Button ignore its click.
Opening backing chests is not needed to reproduce this input timing fault.
Owner: root; the small ordered diagnosis/correction chain benefits from the
already-loaded UI context. Surface: NativeTerminalInventory, version metadata,
README/CHANGELOG and this audit. Requirements: FR-001/FR-007/FR-008, US2 scenario
8 and SC-006/SC-007. Keep raw pointer-down only in the projection-refresh freeze;
action availability remains gated by actual item dragging, split dialogs,
pending operations and queued work. Search keeps its existing focus gate.
The user's repeated-click/blinking reproduction supplies the pre-fix evidence;
inspect native Unity Button.Press, run existing focused suites, build and
Harmony/package checks, and retain live acceptance separately. This reversible
UI-only correction does not add a new test harness or alter any storage,
ownership or range algorithm. Package separately as 0.7.5; keep prior ZIPs and
the user's game/config/saves intact. Independent final review remains required.

### Search keyboard focus refinement (0.7.4)

The user confirms the terminal now looks correct, but typing WASD in search
still moves the character. Native PlayerController.TakeInput permits keyboard
movement with the inventory open and independently checks TextInput.IsVisible;
the existing InventoryGui.Update prefix only suppresses inventory shortcuts.
Owner: root, preserving continuity for this small ordered correction. Surface:
Storage/UI/NativeTerminalInventory.cs, version metadata/docs and this audit.
Requirements: FR-001/FR-008, US2 scenario 7 and SC-006/SC-007. Report actual
search focus through the native TextInput.IsVisible gate and reuse that same
focus predicate for inventory shortcuts. Only add a visible result; preserve
other dialogs' results. Derive blocking from the live field and terminal state
so closing, disabling or losing focus cannot leave a latched input block.
Native PlayerController then clears movement and action controls itself. No
transaction, layout, hotkey, membership or dependency changes. Verify by native
source inspection, existing focused suites, Release build, Harmony target and
versioned package inspection. This reversible input-only adjustment does not
add implementation-mirroring tests. Actual typing with the user's full mod set
remains a live acceptance step. Package separately as 0.7.4 without installing
it or altering the previous candidate; final independent review remains open.

### Naming shortcut refinement (0.7.2)

Owner: root; this bounded follow-up reuses the already-inspected input and naming
paths, so another implementation owner adds rediscovery overhead. Surface:
`Hotkeys/HotkeyConfig.cs`, `Hotkeys/HotkeyPatch.cs`,
`Storage/UI/StorageTerminal.cs`, `Storage/UI/TerminalTranslations.cs`, version
metadata and operating documentation. Requirements: FR-001/FR-002/FR-008,
SC-001/SC-006. Add `NetworkNameShortcut` with default LeftAlt+T and dispatch it
from the existing local-player Update hook using the game's hover target.
Remove the Container.Interact naming prefix; terminal Use opens the inventory.
Retain range/eligibility/access/busy checks, suppress naming while a UI is open,
and derive hover help from the configured shortcut. No inventory transaction,
station behavior or persisted membership changes. Verify the existing focused
suites, Release build, Harmony metadata and versioned ZIP; inspect native hover
range and input gating. This reversible input-only refinement does not add
constant-mirroring tests; actual keyboard/mod interaction remains a live check.
Package separately as 0.7.2, preserving the earlier candidates and the pending
independent-verification status.

- research.md: not needed; confirmed root causes, local reference findings and native anchors are recorded in this plan.
- data-model.md: not needed; the existing transfer model is retained and only an optional destination slot and transient UI projection are added.
- contracts/: not needed; the complete cross-lane signature, compatibility rule and ownership are frozen above.
- quickstart.md: not needed; README will contain the new native gestures and acceptance steps.

## Risks and migrations

### Implementation evidence and refinements

Both fresh implementation dispatches were attempted after T001. The native tool
returned `agent thread limit reached` for deep_native_terminal_runtime and
designer_native_terminal_implementation. Root therefore implements both declared
lanes sequentially with the same ownership boundaries. No child writer was admitted.

Native ItemData.Save also rounds durability to hundredths and omits a crafter
name when the crafter ID is zero. To preserve strict transfer identity, new item
payloads use a versioned scs-item:1 binary envelope with exact serialized fields
and prefab-cloned shared data. Persisted 0.7.0 native inventory payloads still load
through the fully initialized native inventory constructor. The runtime fixture
tests the production adapter; its minimal external game fakes are not a Unity test.

Directed remote withdrawals also retain the clicked slot's identity/amount (or
empty state) in their immutable intent. A fresh player snapshot after retry or
reconnect must match that expectation. Old automatic requests have no trailing
slot and retain -1 placement. StorageSlotExpectation is the tested production
policy; name dispatch/owner receipt ordering uses StorageNameFlow. Naming retries
reuse the existing bounded owner receipt storage instead of overwriting a later name.

StorageRequestIntent.MatchesTarget is used for current-world/anchor/kind pending
lookup. The removed global scs.terminal.pending.v1 flag is never a naming lock.
Native inventory gestures additionally guard InventoryGrid.DropItem before its
incompatible-swap branch can remove a real player item. Grid dimensions are
invalidated on opening/closing so native elements rebuild at the restored size.

The user chose to close Valheim and play the previous version while this correction
is prepared, then install/test the candidate locally. No corrected build has been
installed into that game session and no 0.7.1 screenshot/gameplay PASS is claimed.

- The old global scs.terminal.pending.v1 value may refer to a completed or missing request. Ignore it as a universal naming lock; preserve and reconcile actual durable intents/escrow instead of deleting them.
- Keep existing persistent journal/name/request keys. Manager replacement resets only process caches; loading current world records remains authoritative.
- The projection is a clone: any unguarded native use/drop/write path can duplicate items. Inspect selected, released, right-click, split, outside, bulk and controller/touch entry points and verify Harmony priority relative to mutation guards.
- Search must not rebind an active drag to another identity; snapshots are frozen or explicitly canceled before replacement.
- Optional inventory UI mods can alter hierarchy and callbacks. Fail closed with an actionable status if required controls are unavailable, and document live compatibility limits.
- Player-slot swaps are explicitly excluded; incompatible drops change nothing. Backing-slot placement remains manager-controlled.
- Package 0.7.1 updates matching-version metadata. Build before proposing any game installation/restart; no server deployment or publication is authorized.
- Live observation is attempted with the available single-player session. The desktop provider initially rejected an ultrawide screenshot; the user was asked optionally to use a smaller window. Do not claim visual or multiplayer PASS without actual evidence.

## Constitution Check (post-design)

### BUSY-001 convergence (0.7.9)

The matching 0.7.8 local test leaves ordinary chests and cooking stations busy.
The independent diagnostic review finds that every recovery attempt rebinds
remote participants while their acknowledgements are stored only in the replaced
instances. Host processor outputs use this same path. Root owns bounded runtime
and native fixture implementation because the ordered diagnosis/test context is
already loaded; Oracle independently reviews and the isolated save inspector has
a separate ignored-only diagnostic surface.

First reproduce complete output recovery via StorageService Resume/GetOperation/
IsBusy with native routed RPCs, real inventories and a pending effect/transaction.
Then preserve acknowledgement progress across binding lifetimes, scoped to its
operation, participant, actor and owner; completed phases must remain idempotent.
Check local inline acknowledgements and delayed remote replies, interleaved
operations, changed owners, already-applied receipts and reservation release.
Do not clear locks by timeout, discard pending work, change saves, or remove
permissions. Preserve 0.7.8 retry/backpressure behavior. Follow with fresh Oracle
verification and matching source/combined 0.7.9 artifacts; live acceptance stays
separate. Same-intent FR-003/007/008 correction; no new product choice or route.

Readonly native save inspection also establishes that current Valheim world
loads remap ZDO IDs while persisted SCS strings remain unchanged. The old
capture and prepare markers survive, but their journals are absent. Delegate
the disjoint journal/service persistence surface to a fresh deep specialist:
gate native mutation on durable journal availability, persist stable object
references, and recover legacy captured outputs only from matching profile and
world custody evidence. Root retains RPC, fixture orchestration, artifact and
integration ownership. No inspector or implementation may write live saves.
Identity stamping must not invalidate the first operation's revision snapshot.
Recovery must not reuse an old delivery ID or clear unrelated reservations.
Missing or conflicting proof stays pending with no capture replay. Final Oracle
must assess these additions together with the acknowledgement fix.

Independent review round 1 failed BUSY-079-001/002/003. Root owns journal legacy
reference provenance and runtime tests; a fresh deep specialist owns service
cleanup restart safety, actor/world source proof and native orphan tests.
Legacy producer-derived operation IDs contain no actor: never infer one from
their numeric prefix. Missing independent host profile or actor-bound source
proof must stop remote legacy recovery. Reconstruct interrupted cleanup only
from persisted verified custody and exact operation markers, checking all known
receipts before any new delivery. Missing reference trailers must never bless
a raw ID that now points to another object. Obtain a fresh final Oracle verdict
after these regressions and implementation corrections.

- **User-value first**: PASS — the design removes select-plus-button transfers and directly repairs confirmed local acknowledgement and lifecycle failure paths.
- **Simplicity and bounded scope**: PASS — it reuses native controls and existing finite transactions with one backward-compatible destination parameter; incompatible swap remains excluded.
- **Testable contracts**: PASS — production planner, naming dispatch, journal APIs and native action/state seams have explicit red-first scenarios; live acceptance stays separate.
- **Independent assurance**: PASS — deep/runtime and designer/UI own disjoint mutable surfaces after the root-frozen facade, with an explicit root fallback for unavailable capacity; a fresh Oracle reviews the composed change, including capability limits.
- **Traceable delivery**: PASS — every FR maps to concrete files and tests; versioned package, prior failures and residual live risks will be retained in the audit trail.

### Combined integration ownership after source PASS

The target branch independently moved to 70ea14d and now includes issue-8
ALT-lock input/crafting/Epic Loot protections alongside cart/overlay fixes.
Initial ownership was root. The actual merge exposed coupled station/crafting
lock policies and obsolete direct-station test boundaries. Root assigned
`deep_local_merge_lock_integration` the target-only product C# and focused test
reconciliation (T031): specialist ownership isolates that dependency chain while
root verifies the source package and owns docs, artifacts and commits. The
delegation envelope records the bounded surface, pre-fix native protection
tests, local/remote cost and view requirements, and no game/save writes.
Preserve every unrelated protection and storage conservation/busy guard.
Product conflict resolution requires a fresh Oracle integration review; the
source PASS does not approve merge choices.
