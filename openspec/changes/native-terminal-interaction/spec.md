# Feature Specification: Native terminal interaction and reliable linking

**Change ID**: `native-terminal-interaction`<br>
**Route**: Full<br>
**Status**: Local candidate packaged; independent verification and live acceptance pending

## Intent and scope

**Why**: The first live single-player acceptance rejected the custom list/button UI and exposed linking that remains pending. Players need familiar chest gestures and reliable named membership.<br>
**Impact**: Replace terminal presentation with the game's native inventory experience, correct local name acknowledgement and world/session cache handling, preserve finite network management and station source/sink behavior.<br>
**Affected capabilities**: `storage-terminal`, `storage-network`, `storage-transactions`

## User stories

### US1 - Link a chest and use it after reloading (Priority: P1)

As a player, I can name a terminal or eligible chest and see the confirmed name after reopening, without a stuck transfer message.

**Independent test**: Name an accessible chest in a local session, reopen it, then reload the same world; repeat with immediate and delayed owner acknowledgements.

**Covers**: FR-002, FR-003, FR-007, SC-001, SC-002, SC-005, SC-007

**Acceptance scenarios**:

1. **Given** an accessible idle chest in single player, **When** its name is submitted and the local acknowledgement arrives immediately, **Then** the final confirmed status and stored name survive return from the submission call and reopening.
2. **Given** a name request waiting for its owner, **When** the same dialog is reopened, **Then** the current request and draft remain visible and a second request is not created.
3. **Given** a pending operation for another chest or another world, **When** a naming dialog is opened, **Then** the unrelated marker does not disable linking and its durable inventory custody is not erased.
4. **Given** a previous game session's cached object IDs, **When** the same world is reopened with a new object manager, **Then** the runtime discards obsolete references before looking them up and reloads durable state without invalid-ID exceptions or cross-session results.
5. **Given** a denied or changed target, **When** naming completes, **Then** rejection is explicit, its draft is retained and no name or inventory is changed without authorization.
6. **Given** an eligible chest or terminal under the player's interaction cursor, **When** the player presses the configurable naming shortcut (default `Alt + N`), **Then** the naming dialog opens subject to the existing access checks; `Alt + E` no longer opens that dialog, and typing in a UI cannot trigger the shortcut.

### US2 - Move items using familiar inventory gestures (Priority: P1)

As a player, I can interact with the terminal as a chest inventory, with my existing player inventory beside it, instead of selecting rows and pressing transfer buttons.

**Independent test**: Open a terminal, search, Ctrl-click, split and drag stacks in both directions; inspect exact destination quantities before and after confirmation.

**Covers**: FR-001, FR-004, FR-005, FR-006, SC-003, SC-004, SC-005, SC-007

**Acceptance scenarios**:

1. **Given** a named terminal with loaded accessible members, **When** it opens, **Then** native player and chest grids, item icons/tooltips and a search field are shown; ordinary deposit and withdrawal do not need a separate action button.
   The terminal grid MUST keep a usable viewport when the installed Valheim Plus inventory layout reapplies native horizontal insets; the reported member/slot count alone does not satisfy this scenario.
2. **Given** one pooled identity with more than a maximum stack, **When** it is clicked, split or quick-transferred, **Then** the drag/split quantity is bounded to a legal stack while the display reports the aggregate total.
3. **Given** a network stack and an empty or compatible player slot, **When** the stack is dropped into that slot, **Then** only the confirmed amount is placed in that exact slot, preserving finite capacity and metadata.
4. **Given** an incompatible occupied or changed player slot, **When** a network stack is dropped there, **Then** the operation is rejected without moving either item or silently choosing another slot.
5. **Given** an item dragged from the player, **When** it is dropped anywhere in the network grid, **Then** the manager distributes the selected quantity to eligible physical capacity without assigning physical meaning to visual slots.
6. **Given** a projected network item, **When** it is dragged outside, right-clicked or rearranged inside the projection, **Then** no projected copy is dropped, consumed, equipped or written into real storage.
7. **Given** the terminal search field has keyboard focus, **When** the player types movement or action keys, **Then** those keys edit the search without controlling the character; clicking away, Enter, Escape or closing the terminal releases this text-input gate without overriding another open text dialog.
8. **Given** an idle terminal, **When** a mouse press on Organize or another toolbar control spans a periodic refresh, **Then** the control remains enabled and releasing the click invokes its action once; active item drags, split dialogs and pending transfers still prevent conflicting actions, and the item projection stays stable while the mouse is held.
9. **Given** the terminal window, **When** it renders, **Then** eight native columns use the available width, up to four visible rows have their own bounded scrollbar, search and ordering share one row, and the three transfer/organize actions occupy a separate footer row without overlapping scroll controls. Header metadata reports physical capacity. The existing native chest weight indicator, in its normal location rather than the header, reports the total weight of all accessible network contents independent of filtering. Ordinary-chest layout is restored on close.
10. **Given** a fresh game UI where the terminal is the first container view, **When** it closes and a normal chest opens, **Then** inventory layout mods retain native baseline coordinates and the normal scrollbar stays at the right edge, including after repeated terminal/chest alternation.
11. **Given** grouped network contents or a search result, **When** the grid renders, **Then** only occupied entries have visible cells and its height uses the required rows up to four, with one blank deposit row for an empty result. Rectangular padding remains able to receive native deposits without representing free physical capacity. A full 30-slot network with 16 grouped entries displays 16 cells in two rows, while the counter still reports 30/30.

### US3 - Keep network extras and asynchronous safety (Priority: P2)

As a player, I can search, organize and use bulk actions while confirmed inventory state and existing automation remain trustworthy.

**Independent test**: Filter a pooled view, organize it, run transfer-all queues with delayed confirmation, close/reopen, and confirm exact quantities and unchanged automation scope.

**Covers**: FR-001, FR-003, FR-004, FR-006, FR-007, FR-008, SC-003, SC-005, SC-006, SC-007, SC-008, SC-009

**Acceptance scenarios**:

1. **Given** active search or view sorting, **When** the view changes, **Then** only presentation changes, all accessible stock remains available to stations and bulk actions use their documented full scope.
2. **Given** a pending cross-grid request, **When** more gestures or refreshes occur, **Then** no second mutation is submitted, drag identity remains stable and the authoritative inventories refresh after the same operation completes.
3. **Given** a bulk transfer, **When** one step is pending or the UI closes, **Then** only the submitted operation persists and unsubmitted entries are not moved; later steps require confirmed preceding results and fresh capacity.
4. **Given** ordinary chests, crafting, processing and direct quick-stack, **When** the terminal UI is used or closed, **Then** prior finite-network, permission, compaction and direct-feature boundaries remain intact.
5. **Given** delayed or lost remote replies, **When** automatic stations and UI updates resume pending work, **Then** retries use elapsed time instead of frame rate, admitted requests poll status without repeating inventory snapshots, and a congested connection defers retryable payloads while preserving operation identity and custody.
6. **Given** an output delivery with inline or delayed owner acknowledgements, **When** recovery rebuilds its participants, **Then** authenticated phase receipts survive that rebind and a confirmed delivery releases both chest and source reservations exactly once.
7. **Given** a saved world whose native ZDO identifiers change during load, **When** pending work resumes, **Then** durable participant references reconnect to the same objects; legacy captured output may be recovered only from matching world, actor, pending intent, escrow and native custody proofs. Missing or conflicting evidence must not clear reservations or replay capture.
8. **Given** that the durable operation journal is unavailable, **When** a new mutation is requested, **Then** no native capture or participant reservation starts with only an in-memory record.

## Edge cases

- Single player, host-owned and remote-owned participants; immediate, delayed, duplicate and rejected responses.
- Restart/rejoin of the same world, change of world or character, stale global UI markers and durable in-flight items.
- Empty/unnamed/full network, different metadata, protected/equipped player items, target slot changes, partial stack capacity.
- Search while dragging or awaiting a result, split dialog focus, Escape, death, range/ward changes, ordinary chest opening after terminal closure.
- UI mods, ultrawide screens, small resolutions and controller navigation; report unobserved integrations honestly.

## Functional requirements

- **FR-001 — Terminal interaction**: `[MODIFIED storage-terminal]` The system MUST provide a buildable manager through the game's native player/chest inventory grids, familiar click/drag/split/quick-transfer gestures, item tooltips, searchable combined contents, aggregate quantities, finite capacity, naming, view sorting and Organize controls with clear result states; ordinary transfers MUST NOT require selecting a row and then pressing Deposit or Withdraw.
- **FR-002 — Named membership**: `[MODIFIED storage-network]` The system MUST assign and persist normalized network names on eligible ordinary chests and terminals with reliable local and remote confirmation, preserve an unconfirmed naming draft, and select members by matching name, terminal radius, access and loaded-world state while excluding terminals, graves and transport inventories as backing members.
- **FR-003 — Recoverable operation identity**: `[MODIFIED storage-transactions]` Network operations MUST retain stable identities and recoverable progress across duplicate requests, lost acknowledgements, controlled reconnects and ownership changes without replaying completed effects or discarding pending products; final outcomes MUST NOT regress under synchronous or late replies, and cached object references and transient operation results MUST be scoped to the current world/session/actor.
- **FR-004 — Distributed deposit**: `[MODIFIED storage-terminal]` The system MUST distribute gesture-driven and bulk deposits into compatible partial stacks and finite member slots, preserving metadata and reporting confirmed partial acceptance; protected/equipped items MUST remain protected, and bulk work MUST be sequential, refresh source identities and stop submitting when its UI closes or a preceding step fails.
- **FR-005 — Distributed withdrawal**: `[MODIFIED storage-terminal]` The system MUST withdraw selected compatible quantities without visiting backing chests, preserve remainders and respect player space; dragging to an empty or compatible player slot MUST use that exact slot, incompatible or changed slots MUST reject without a fake swap, and quick-transfer MAY choose available player space automatically.
- **FR-006 — Conservation and exact capacity**: `[MODIFIED storage-transactions]` Operations MUST preserve quantities and item metadata across real inventories and accounted in-flight state, obey stack sizes and reserved finite capacity and never treat unknown responses as final; projected inventory entries MUST be display-only and MUST NOT be directly moved, consumed, equipped, dropped or persisted by native inventory paths.
- **FR-007 — Bounded work and honest status**: `[MODIFIED storage-transactions]` Discovery, planning, execution and retries MUST remain bounded and cached by relevant state; unavailable, busy, pending, rejected and confirmed outcomes MUST remain distinguishable, pending UI state MUST refer to the applicable target/operation, and search/drag/refresh or duplicate gestures MUST NOT submit duplicate writes or hide the authoritative result.
- **FR-008 — Regression evidence and package**: `[INTERNAL]` The correction MUST provide regression evidence for the observed single-player failures, focused native-interaction tests, a compilable versioned package and updated operating instructions, distinguishing actual game observations from automated or source-inspection evidence.

Naming shortcut refinement: the default for FR-002 is now **Alt + N** because
Valheim uses T for emotes. `Hotkeys.NetworkNameShortcut` MUST remain editable by
the player, including custom combinations or disabling it. Upgrades must not
repeatedly reset a customized binding. Earlier Alt+T acceptance records describe
the previous default, not the current one.

Presentation refinement: no persistent drag/Shift/Ctrl instruction line below
the action buttons. Necessary operation status remains visible in the header.

## Success criteria

- **SC-001** `[buildable]`: All local immediate and delayed naming scenarios finish with the correct persisted name/result; rejected and duplicate acknowledgements never produce false success or reopen a completed request.
- **SC-002** `[buildable]`: A world/session manager replacement and same-world reload preserve durable records but trigger zero lookups through obsolete IDs and zero cross-session transient results.
- **SC-003** `[buildable]`: Projection/filter/gesture scenarios preserve exact identities and aggregate totals, bound individual drag/split quantities, and produce zero direct inventory mutations from projected items.
- **SC-004** `[buildable]`: A directed withdrawal lands in the requested empty/compatible slot; changed/incompatible slots move zero items; automatic withdrawal and persisted older requests retain their existing behavior.
- **SC-005** `[buildable]`: Delayed/reopened operations, unrelated pending markers, protected items and sequential bulk queues preserve exact quantities with zero duplicate submissions.
- **SC-006** `[buildable]`: Existing contract tests, new focused regressions, plugin build and patch metadata checks pass and the versioned package matches the verified code.
- **SC-007** `[outcome]`: All live single-player naming/reopening/reloading, native search, transfer, split, Organize and ordinary-chest restoration checks succeed at the available display resolution.
- **SC-008** `[outcome]`: 2 matching clients confirm the same terminal gestures with delayed owner responses and no duplicated or lost items; unobserved game/integration scenarios remain explicit residual risks.
- **SC-009** `[buildable]`: Repeated updates during delayed responses produce bounded retries independent of frame rate; authenticated progress permits the next step, queued transport pressure defers retryable sends, and reconnect/status recovery preserves pending work and exact quantities.

## Assumptions

- The user explicitly requested correction of both the rejected UI and linking failure and confirms only single-player testing so far. The previously selected Full route and authorization to implement this terminal continue to apply; this correction has a new audit trail without overwriting the archived initial change.
- OdinStorage 0.9.2 is an available local design reference, but the user's explicit native-chest requirement takes precedence over its own custom list implementation.
- Existing accepted public seams remain network query, transfers, operation status/recovery and user inventory gestures. Regression tests target those observable boundaries, including naming and session reopening identified by the user.
- The user's follow-up changes naming from `Alt + E` to `Alt + T` because of another mod's shortcut. This is a same-intent interaction refinement; existing quick-stack/restock precedence and normal interaction remain unchanged.
- Dragging to an incompatible occupied slot is rejected rather than implementing a two-operation imitation of a native swap; an atomic Exchange feature is outside this correction.

## Dependencies

- Existing BepInEx/Jotunn, cached Valheim assemblies and offline build tools. No network or change to another mod is required.
- Live desktop inspection is available but its first ultrawide screenshot exceeded the provider payload cap; visual verification requires a usable capture or explicit user observations.

## Out of scope

- Infinite/offline/global storage, replacing the network backend, changing station search policies, adding terminal reach to Shift+E or physically assigning visual grid slots to backing chests.
- Atomic exchange with an incompatible occupied player slot, using or dropping a virtual network item directly, guaranteed arbitrary inventory-mod compatibility.
- Publication, live-server deployment, deleting journals/escrow to hide failures, or treating previous automated PASS as proof of live gameplay.
