# Feature Specification: Managed storage terminal

**Change ID**: `storage-terminal-manager`<br>
**Route**: Full<br>
**Status**: Draft

## Intent and scope

**Why**: Players need one finite, unified store over ordinary chests, with transparent allocation and compaction instead of walking between chests. Nearby crafting and processing must consume and return materials through the terminal even after physical storage is reorganized.<br>
**Impact**: Add a buildable terminal, named networks, an aggregated management window and coordinated storage operations. Extend crafting/upgrades and supported processing input/output to nearby networks. Require matching SCS on server and clients. Existing direct quick-stack, restock and building radii remain direct.<br>
**Affected capabilities**: `storage-terminal`, `storage-network`, `station-storage`, `storage-transactions`

## User stories

### US1 - Manage one inventory across linked chests (Priority: P1)

As a player, I can name a terminal and nearby backing chests, search their combined contents and deposit or withdraw at the terminal without knowing the physical location.

**Independent test**: Link differently sized physical chests to a named network, query it with the terminal window closed, deposit a partial stack, and withdraw a requested quantity.

**Covers**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-014, SC-001, SC-002, SC-008

**Acceptance scenarios**:

1. **Given** accessible loaded chests with the terminal's network name within its configured radius, **When** the network is queried, **Then** the combined quantities and finite slot capacity are shown without opening any backing chest.
2. **Given** a same-named chest outside the radius or inaccessible through a ward, **When** the player opens the terminal, **Then** its contents and capacity are excluded.
3. **Given** compatible partial stacks and free slots across members, **When** the player deposits or withdraws a quantity, **Then** only confirmed quantities move and any unaccepted quantity remains accounted for.
4. **Given** items with different quality, variant or custom metadata, **When** the terminal groups or moves them, **Then** their identity and metadata remain distinguishable and incompatible stacks are never fused.

### US2 - Compact physical storage (Priority: P1)

As a player, I can organize the network so compatible stacks consolidate and fewer backing chests remain occupied without permanently assigning a chest to an item category.

**Independent test**: Four ten-slot chests containing 10 A, 5 A, 10 B and 5 B full stacks become three occupied chests with identical quantities; a repeated operation makes no further moves.

**Covers**: FR-007, FR-011, FR-012, SC-003

**Acceptance scenarios**:

1. **Given** compatible partial stacks spread across members, **When** Organize is selected, **Then** stacks combine within real maximum sizes and no item properties or quantities change.
2. **Given** the four-chest example, **When** Organize completes, **Then** one chest is empty and mixed materials may share a chest.
3. **Given** an unchanged organized network, **When** Organize is selected again, **Then** no physical moves are planned.
4. **Given** a visual name/category/quantity sort, **When** it changes, **Then** the physical contents do not move.

### US3 - Craft and upgrade through the terminal (Priority: P1)

As a player using a workbench, forge or other supported crafting station, I can use network materials through a nearby terminal using the normal crafting interface.

**Independent test**: A recipe requires several materials distributed beyond direct crafting range but within a reachable terminal network; perform a successful craft and a competing insufficient-material attempt.

**Covers**: FR-008, FR-012, FR-015, SC-004

**Acceptance scenarios**:

1. **Given** a reachable terminal and eligible resources in its members, **When** the recipe is displayed, **Then** available quantities include each physical resource once.
2. **Given** a complete recipe can be paid, **When** the player crafts or upgrades, **Then** the exact cost is consumed once and the normal result is produced once.
3. **Given** another operation consumes a required ingredient first, **When** the recipe attempts to complete, **Then** it produces no unpaid result and does not leave a partial irreversible cost.
4. **Given** the player enters hammer placement mode, **When** building requirements are queried, **Then** only the existing direct chest behavior applies.

### US4 - Feed processors and store their production (Priority: P1)

As a player, I can place a terminal near a supported processor so it pulls materials or fuel and distributes completed production into the backing network automatically.

**Independent test**: A smelter obtains ore/fuel through a terminal, stores output remotely, and encounters partial capacity and delayed acknowledgements.

**Covers**: FR-009, FR-010, FR-011, FR-012, FR-014, SC-005, SC-006

**Acceptance scenarios**:

1. **Given** enabled automation and machine input space, **When** the processor requests an ingredient or fuel, **Then** confirmed network resources are delivered exactly once and repeated ticks do not overfill its queue.
2. **Given** production of ten units and space for eight, **When** storage completes, **Then** eight are stored and the confirmed remainder follows that producer's existing fallback policy.
3. **Given** a deposit response is delayed or lost, **When** the machine ticks again, **Then** the same pending output is neither regenerated nor also dropped as if delivery failed.
4. **Given** Organize moves material between valid members, **When** a nearby processor requests it, **Then** the physical member's distance from the processor does not remove terminal-mediated access.

### US5 - Share the store without ambiguous transfers (Priority: P1)

As players on a remote server, we can operate the same storage concurrently while permissions, finite capacity and recoverable in-flight state are preserved.

**Independent test**: Exercise two actors, reordered/duplicate responses, a changed owner, rejection, lost acknowledgement and disconnect/reconnect through the public storage-operation interface with deterministic transport and persistence boundaries.

**Covers**: FR-003, FR-004, FR-011, FR-012, FR-013, FR-016, FR-017, SC-001, SC-006, SC-007, SC-009

**Acceptance scenarios**:

1. **Given** a chest appears directly and behind two terminals, **When** resources or capacity are requested, **Then** that physical chest contributes once.
2. **Given** simultaneous requests for the same stack or free slot, **When** operations execute, **Then** reservations prevent overspending and overfilling.
3. **Given** an operation's response is unknown, **When** it is retried or the session reconnects, **Then** its stable identity is reconciled before further effects and a completed operation is not applied twice.
4. **Given** access or membership changes before an operation commits, **When** the pending operation proceeds, **Then** it is rejected or recovered without bypassing permissions or losing accounted items.

### US6 - Install and operate the feature (Priority: P2)

As a server operator, I can install the produced package, configure network radii and identify a pending or unavailable operation without ambiguous success messages.

**Independent test**: Compile and package offline using local game references, inspect package contents, and follow the documented client/server and terminal setup flow.

**Covers**: FR-001, FR-016, FR-017, FR-018, SC-008, SC-009

**Acceptance scenarios**:

1. **Given** matching server and client installations, **When** a player builds a terminal and names a network, **Then** the controls and status explain its member capacity and available actions.
2. **Given** incompatible client/server versions or an unavailable member, **When** the player attempts an operation, **Then** the feature reports an actionable state rather than optimistic success.

## Edge cases

- Empty network, all members full, only partial compatible stack space, mixed chest sizes, non-stackable gear and modded custom data.
- Overlapping terminal ranges, same-name networks at distant bases, direct-plus-network discovery and case/whitespace normalization of the group name.
- Locked/equipped player items during bulk deposit, full player inventory and changed recipe/upgrade selection while waiting.
- Simultaneous Organize, direct quick-stack/restock, ordinary chest access, crafting and processing against the same physical chest.
- Actor or ward changes; member destroyed, renamed, unloaded or changing owner while work is pending; terminal destroyed while an operation has not settled.
- Partial, rejected, duplicated, late and missing responses; replay after controlled disconnect/reconnect; bounded journals and operation queues.
- Processor input becoming full, output already committed but response unknown, kiln coal caps and producer-specific remainder policies.
- Game/player-save rollback to different historical points is a Vanilla persistence limitation, not a promise of cross-file crash-atomic saves.

## Functional requirements

- **FR-001 — Terminal interaction**: `[ADDED storage-terminal]` The system MUST provide a buildable terminal with searchable combined contents, quantities, finite capacity, network naming, deposit, withdrawal, view sorting and Organize controls with clear pending/result states.
- **FR-002 — Named membership**: `[ADDED storage-network]` The system MUST support assigning a network name to eligible ordinary physical chests and select members by normalized matching name, terminal radius and valid loaded world state, excluding terminals, graves and transport inventories as backing members.
- **FR-003 — Access preservation**: `[ADDED storage-network]` The system MUST validate access to the terminal and each physical participant for the authorizing actor, preserve wards/private-chest restrictions and revalidate before effects; network ownership alone MUST NOT confer access.
- **FR-004 — Unique resource projection**: `[ADDED storage-network]` The system MUST count quantities and capacity from each physical chest once across direct and terminal routes, preserve distinct item identities and prohibit recursive terminal expansion.
- **FR-005 — Distributed deposit**: `[ADDED storage-terminal]` The system MUST distribute deposits into compatible partial stacks and finite member slots, preserving metadata and reporting confirmed partial acceptance; bulk deposit MUST respect existing protected/equipped player-item rules.
- **FR-006 — Distributed withdrawal**: `[ADDED storage-terminal]` The system MUST withdraw selected compatible quantities into available player space without requiring physical chest navigation, returning confirmed quantities and preserving any remainder.
- **FR-007 — Network compaction**: `[ADDED storage-terminal]` Organize MUST consolidate compatible stacks and occupy fewer chests when capacity permits, allow mixed materials, avoid permanent category reservations and produce no moves when an unchanged network is already organized; visual sorting MUST NOT relocate items.
- **FR-008 — Crafting source integration**: `[ADDED station-storage]` Crafting and upgrades MUST discover nearby terminals using the current player-based crafting radius, query their eligible members and consume a complete valid recipe exactly once before granting the normal result.
- **FR-009 — Processor source integration**: `[ADDED station-storage]` Enabled smelter/kiln, cooking, fireplace and fermenter input/fuel automation MUST consider terminals within the corresponding machine radius and coordinate consumption with machine input capacity and existing material/production restrictions.
- **FR-010 — Processor sink integration**: `[ADDED station-storage]` Enabled smelter/kiln, cooking, fermenter and beehive output collection MUST consider nearby terminals as destinations and let the network choose physical receiving chests while preserving each producer's confirmed-remainder policy.
- **FR-011 — Conservation and exact capacity**: `[ADDED storage-transactions]` Operations MUST preserve quantities and item metadata across inventories and explicitly accounted in-flight state, respect maximum stack sizes, reserve finite destination capacity and never treat an unknown response as confirmed failure or success.
- **FR-012 — Coordinated effects**: `[ADDED storage-transactions]` Reads, reservations and effects MUST use one consistent resource model; competing operations and existing SCS writers MUST NOT spend the same resource or slot twice, and recipe or processor side effects MUST be gated on their required confirmed resources.
- **FR-013 — Recoverable operation identity**: `[ADDED storage-transactions]` Network operations MUST have stable identities and recoverable progress sufficient to reconcile duplicate requests, lost acknowledgements, controlled reconnects and ownership changes without replaying completed effects or silently discarding pending products.
- **FR-014 — Closed-window availability**: `[ADDED storage-network]` Eligible networks MUST remain usable by supported stations with the terminal window closed; the system MUST respect loaded-world and current player/automation constraints without forcing distant zones to load.
- **FR-015 — Direct-feature boundaries**: `[ADDED station-storage]` Quick-stack, restock, hammer building, animal feeding, plant harvest and existing optional integrations MUST retain their direct physical search scopes unless explicitly part of this change, while respecting shared-operation coordination for the physical chests they touch.
- **FR-016 — Deployment and authority**: `[ADDED storage-transactions]` The feature MUST require compatible SCS server/client installations, synchronize gameplay configuration and validate operation actor/context at the authoritative effect boundary rather than trusting client-provided inventory totals.
- **FR-017 — Bounded work and honest status**: `[ADDED storage-transactions]` Discovery, planning, execution and retry work MUST be bounded and cached by relevant network state; unavailable/busy/pending/rejected/confirmed outcomes MUST remain distinguishable to callers and players.
- **FR-018 — Deliverable and operating instructions**: `[INTERNAL]` The change MUST provide a compilable plugin package, focused automated contract tests and instructions for linking, capacity, permissions, automation, recovery limitations and two-client acceptance checks.

## Success criteria

- **SC-001** `[buildable]`: Public network queries return exactly 1 contribution per physical chest and exclude wrong-name, out-of-range, unauthorized and unavailable members in deterministic boundary tests.
- **SC-002** `[buildable]`: Deposit/withdrawal scenarios with partial capacity and metadata-distinct items preserve exact independent expected quantities and payloads at the public operation boundary.
- **SC-003** `[buildable]`: The four ten-slot chest example compacts to three occupied chests, compatible partial stacks merge, and a second unchanged Organize produces zero moves.
- **SC-004** `[buildable]`: Craft/upgrade integration gates the normal result on exact complete payment; insufficient or conflicting payment produces 0 results and 0 lost accounted materials; hammer mode excludes network expansion.
- **SC-005** `[buildable]`: All supported processor adapters cover input/fuel and output, including 8-of-10 capacity, changing input capacity, kiln caps and producer-specific remainder behavior.
- **SC-006** `[buildable]`: Deterministic transport/persistence scenarios cover duplicate/reordered/lost responses, rejection, ownership change and reconnect with 0 duplicate effects and 0 unaccounted quantity loss.
- **SC-007** `[buildable]`: Competing operations and permission/membership changes leave 0 overspent resources and 0 overfilled slots; direct-only features retain their search boundary in regression checks.
- **SC-008** `[outcome]`: In Valheim, a player can build/name a terminal, link 4 chests, search/deposit/withdraw/organize through its window and craft/process through it without opening backing chests.
- **SC-009** `[outcome]`: 2 clients on a matching server complete competing terminal and station operations, including an interrupted connection, with expected quantities and usable status; any unobserved live scenario is reported explicitly as residual risk.

## Assumptions

- The user authorized implementation and explicitly selected Full. Planning/review does not revoke that authorization.
- Network naming is explicit SCS metadata on ordinary chests; vanilla does not expose a suitable per-chest rename flow today, so SCS supplies that interaction. Empty names mean unlinked.
- Initial operation scope is loaded eligible chests and the current authorizing player context already used by SCS automation. Offline/unloaded simulation is not added.
- Recipe output remains in the player inventory. Automatic processor output uses the newly supported sinks.
- Public test seams are network query, deposit/withdrawal/compaction, complete recipe payment, processor input/output and operation recovery: these correspond to the user-approved observable workflows, not private implementation methods.
- Existing source targets net48. Local cached game assemblies and NuGet packages can support offline compilation; baseline compilation requires excluding stale nested test build artifacts.

## Dependencies

- Existing BepInEx and Jotunn dependencies, matching game assemblies, installed .NET SDK and local SDD/TDD tooling.
- MultiUserChest is an allowed integration aid, not a requirement to use an insufficient transaction primitive. No changes to BottomlessChest or another mod's source are required by this feature.
- No canonical capability specs exist yet; all durable requirement deltas are additions.

## Out of scope

- Infinite storage, fake permanent duplicate inventories, cross-base/global access, terminal-to-terminal relays and permanent per-item chest assignments.
- Extending Shift+E/restock/building/animal/plant/third-party integration reach through terminals.
- Unloaded-world automation, arbitrary third-party raw-inventory compatibility, or changing recipes, normal processing times and stack limits.
- Publishing, deploying into a live world, modifying external mods, or claiming multiplayer runtime validation without observing it.
