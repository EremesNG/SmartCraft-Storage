# Storage Transactions Specification

## Purpose

Durable behavioral contract for `storage-transactions`.

## Requirements

### Requirement: Conservation and exact capacity

Operations MUST preserve quantities and item metadata across real inventories and accounted in-flight state, obey stack sizes and reserved finite capacity and never treat unknown responses as final; projected inventory entries MUST be display-only and MUST NOT be directly moved, consumed, equipped, dropped or persisted by native inventory paths.

#### Scenario: US2 - Move items using familiar inventory gestures 1

- **GIVEN** a named terminal with loaded accessible members
- **WHEN** it opens
- **THEN** native player and chest grids, item icons/tooltips and a search field are shown; ordinary deposit and withdrawal do not need a separate action button. The terminal grid MUST keep a usable viewport when the installed Valheim Plus inventory layout reapplies native horizontal insets; the reported member/slot count alone does not satisfy this scenario

#### Scenario: US2 - Move items using familiar inventory gestures 2

- **GIVEN** one pooled identity with more than a maximum stack
- **WHEN** it is clicked, split or quick-transferred
- **THEN** the drag/split quantity is bounded to a legal stack while the display reports the aggregate total

#### Scenario: US2 - Move items using familiar inventory gestures 3

- **GIVEN** a network stack and an empty or compatible player slot
- **WHEN** the stack is dropped into that slot
- **THEN** only the confirmed amount is placed in that exact slot, preserving finite capacity and metadata

#### Scenario: US2 - Move items using familiar inventory gestures 4

- **GIVEN** an incompatible occupied or changed player slot
- **WHEN** a network stack is dropped there
- **THEN** the operation is rejected without moving either item or silently choosing another slot

#### Scenario: US2 - Move items using familiar inventory gestures 5

- **GIVEN** an item dragged from the player
- **WHEN** it is dropped anywhere in the network grid
- **THEN** the manager distributes the selected quantity to eligible physical capacity without assigning physical meaning to visual slots

#### Scenario: US2 - Move items using familiar inventory gestures 6

- **GIVEN** a projected network item
- **WHEN** it is dragged outside, right-clicked or rearranged inside the projection
- **THEN** no projected copy is dropped, consumed, equipped or written into real storage

#### Scenario: US2 - Move items using familiar inventory gestures 7

- **GIVEN** the terminal search field has keyboard focus
- **WHEN** the player types movement or action keys
- **THEN** those keys edit the search without controlling the character; clicking away, Enter, Escape or closing the terminal releases this text-input gate without overriding another open text dialog

#### Scenario: US2 - Move items using familiar inventory gestures 8

- **GIVEN** an idle terminal
- **WHEN** a mouse press on Organize or another toolbar control spans a periodic refresh
- **THEN** the control remains enabled and releasing the click invokes its action once; active item drags, split dialogs and pending transfers still prevent conflicting actions, and the item projection stays stable while the mouse is held

#### Scenario: US2 - Move items using familiar inventory gestures 9

- **GIVEN** the terminal window
- **WHEN** it renders
- **THEN** eight native columns use the available width, up to four visible rows have their own bounded scrollbar, search and ordering share one row, and the three transfer/organize actions occupy a separate footer row without overlapping scroll controls. Header metadata reports physical capacity. The existing native chest weight indicator, in its normal location rather than the header, reports the total weight of all accessible network contents independent of filtering. Ordinary-chest layout is restored on close

#### Scenario: US2 - Move items using familiar inventory gestures 10

- **GIVEN** a fresh game UI where the terminal is the first container view
- **WHEN** it closes and a normal chest opens
- **THEN** inventory layout mods retain native baseline coordinates and the normal scrollbar stays at the right edge, including after repeated terminal/chest alternation

#### Scenario: US2 - Move items using familiar inventory gestures 11

- **GIVEN** grouped network contents or a search result
- **WHEN** the grid renders
- **THEN** only occupied entries have visible cells and its height uses the required rows up to four, with one blank deposit row for an empty result. Rectangular padding remains able to receive native deposits without representing free physical capacity. A full 30-slot network with 16 grouped entries displays 16 cells in two rows, while the counter still reports 30/30

#### Scenario: US3 - Keep network extras and asynchronous safety 1

- **GIVEN** active search or view sorting
- **WHEN** the view changes
- **THEN** only presentation changes, all accessible stock remains available to stations and bulk actions use their documented full scope

#### Scenario: US3 - Keep network extras and asynchronous safety 2

- **GIVEN** a pending cross-grid request
- **WHEN** more gestures or refreshes occur
- **THEN** no second mutation is submitted, drag identity remains stable and the authoritative inventories refresh after the same operation completes

#### Scenario: US3 - Keep network extras and asynchronous safety 3

- **GIVEN** a bulk transfer
- **WHEN** one step is pending or the UI closes
- **THEN** only the submitted operation persists and unsubmitted entries are not moved; later steps require confirmed preceding results and fresh capacity

#### Scenario: US3 - Keep network extras and asynchronous safety 4

- **GIVEN** ordinary chests, crafting, processing and direct quick-stack
- **WHEN** the terminal UI is used or closed
- **THEN** prior finite-network, permission, compaction and direct-feature boundaries remain intact

#### Scenario: US3 - Keep network extras and asynchronous safety 5

- **GIVEN** delayed or lost remote replies
- **WHEN** automatic stations and UI updates resume pending work
- **THEN** retries use elapsed time instead of frame rate, admitted requests poll status without repeating inventory snapshots, and a congested connection defers retryable payloads while preserving operation identity and custody

#### Scenario: US3 - Keep network extras and asynchronous safety 6

- **GIVEN** an output delivery with inline or delayed owner acknowledgements
- **WHEN** recovery rebuilds its participants
- **THEN** authenticated phase receipts survive that rebind and a confirmed delivery releases both chest and source reservations exactly once

#### Scenario: US3 - Keep network extras and asynchronous safety 7

- **GIVEN** a saved world whose native ZDO identifiers change during load
- **WHEN** pending work resumes
- **THEN** durable participant references reconnect to the same objects; legacy captured output may be recovered only from matching world, actor, pending intent, escrow and native custody proofs. Missing or conflicting evidence must not clear reservations or replay capture

#### Scenario: US3 - Keep network extras and asynchronous safety 8

- **GIVEN** that the durable operation journal is unavailable
- **WHEN** a new mutation is requested
- **THEN** no native capture or participant reservation starts with only an in-memory record

### Requirement: Coordinated effects

Reads, reservations and effects MUST use one consistent resource model; competing operations and existing SCS writers MUST NOT spend the same resource or slot twice, and recipe or processor side effects MUST be gated on their required confirmed resources.

#### Scenario: US2 - Compact physical storage 1

- **GIVEN** compatible partial stacks spread across members
- **WHEN** Organize is selected
- **THEN** stacks combine within real maximum sizes and no item properties or quantities change

#### Scenario: US2 - Compact physical storage 2

- **GIVEN** the four-chest example
- **WHEN** Organize completes
- **THEN** one chest is empty and mixed materials may share a chest

#### Scenario: US2 - Compact physical storage 3

- **GIVEN** an unchanged organized network
- **WHEN** Organize is selected again
- **THEN** no physical moves are planned

#### Scenario: US2 - Compact physical storage 4

- **GIVEN** a visual name/category/quantity sort
- **WHEN** it changes
- **THEN** the physical contents do not move

#### Scenario: US3 - Craft and upgrade through the terminal 1

- **GIVEN** a reachable terminal and eligible resources in its members
- **WHEN** the recipe is displayed
- **THEN** available quantities include each physical resource once

#### Scenario: US3 - Craft and upgrade through the terminal 2

- **GIVEN** a complete recipe can be paid
- **WHEN** the player crafts or upgrades
- **THEN** the exact cost is consumed once and the normal result is produced once

#### Scenario: US3 - Craft and upgrade through the terminal 3

- **GIVEN** another operation consumes a required ingredient first
- **WHEN** the recipe attempts to complete
- **THEN** it produces no unpaid result and does not leave a partial irreversible cost

#### Scenario: US3 - Craft and upgrade through the terminal 4

- **GIVEN** the player enters hammer placement mode
- **WHEN** building requirements are queried
- **THEN** only the existing direct chest behavior applies

#### Scenario: US4 - Feed processors and store their production 1

- **GIVEN** enabled automation and machine input space
- **WHEN** the processor requests an ingredient or fuel
- **THEN** confirmed network resources are delivered exactly once and repeated ticks do not overfill its queue

#### Scenario: US4 - Feed processors and store their production 2

- **GIVEN** production of ten units and space for eight
- **WHEN** storage completes
- **THEN** eight are stored and the confirmed remainder follows that producer's existing fallback policy

#### Scenario: US4 - Feed processors and store their production 3

- **GIVEN** a deposit response is delayed or lost
- **WHEN** the machine ticks again
- **THEN** the same pending output is neither regenerated nor also dropped as if delivery failed

#### Scenario: US4 - Feed processors and store their production 4

- **GIVEN** Organize moves material between valid members
- **WHEN** a nearby processor requests it
- **THEN** the physical member's distance from the processor does not remove terminal-mediated access

#### Scenario: US5 - Share the store without ambiguous transfers 1

- **GIVEN** a chest appears directly and behind two terminals
- **WHEN** resources or capacity are requested
- **THEN** that physical chest contributes once

#### Scenario: US5 - Share the store without ambiguous transfers 2

- **GIVEN** simultaneous requests for the same stack or free slot
- **WHEN** operations execute
- **THEN** reservations prevent overspending and overfilling

#### Scenario: US5 - Share the store without ambiguous transfers 3

- **GIVEN** an operation's response is unknown
- **WHEN** it is retried or the session reconnects
- **THEN** its stable identity is reconciled before further effects and a completed operation is not applied twice

#### Scenario: US5 - Share the store without ambiguous transfers 4

- **GIVEN** access or membership changes before an operation commits
- **WHEN** the pending operation proceeds
- **THEN** it is rejected or recovered without bypassing permissions or losing accounted items

### Requirement: Recoverable operation identity

Network operations MUST retain stable identities and recoverable progress across duplicate requests, lost acknowledgements, controlled reconnects and ownership changes without replaying completed effects or discarding pending products; final outcomes MUST NOT regress under synchronous or late replies, and cached object references and transient operation results MUST be scoped to the current world/session/actor.

#### Scenario: US1 - Link a chest and use it after reloading 1

- **GIVEN** an accessible idle chest in single player
- **WHEN** its name is submitted and the local acknowledgement arrives immediately
- **THEN** the final confirmed status and stored name survive return from the submission call and reopening

#### Scenario: US1 - Link a chest and use it after reloading 2

- **GIVEN** a name request waiting for its owner
- **WHEN** the same dialog is reopened
- **THEN** the current request and draft remain visible and a second request is not created

#### Scenario: US1 - Link a chest and use it after reloading 3

- **GIVEN** a pending operation for another chest or another world
- **WHEN** a naming dialog is opened
- **THEN** the unrelated marker does not disable linking and its durable inventory custody is not erased

#### Scenario: US1 - Link a chest and use it after reloading 4

- **GIVEN** a previous game session's cached object IDs
- **WHEN** the same world is reopened with a new object manager
- **THEN** the runtime discards obsolete references before looking them up and reloads durable state without invalid-ID exceptions or cross-session results

#### Scenario: US1 - Link a chest and use it after reloading 5

- **GIVEN** a denied or changed target
- **WHEN** naming completes
- **THEN** rejection is explicit, its draft is retained and no name or inventory is changed without authorization

#### Scenario: US1 - Link a chest and use it after reloading 6

- **GIVEN** an eligible chest or terminal under the player's interaction cursor
- **WHEN** the player presses the configurable naming shortcut (default `Alt + N`)
- **THEN** the naming dialog opens subject to the existing access checks; `Alt + E` no longer opens that dialog, and typing in a UI cannot trigger the shortcut

#### Scenario: US3 - Keep network extras and asynchronous safety 1

- **GIVEN** active search or view sorting
- **WHEN** the view changes
- **THEN** only presentation changes, all accessible stock remains available to stations and bulk actions use their documented full scope

#### Scenario: US3 - Keep network extras and asynchronous safety 2

- **GIVEN** a pending cross-grid request
- **WHEN** more gestures or refreshes occur
- **THEN** no second mutation is submitted, drag identity remains stable and the authoritative inventories refresh after the same operation completes

#### Scenario: US3 - Keep network extras and asynchronous safety 3

- **GIVEN** a bulk transfer
- **WHEN** one step is pending or the UI closes
- **THEN** only the submitted operation persists and unsubmitted entries are not moved; later steps require confirmed preceding results and fresh capacity

#### Scenario: US3 - Keep network extras and asynchronous safety 4

- **GIVEN** ordinary chests, crafting, processing and direct quick-stack
- **WHEN** the terminal UI is used or closed
- **THEN** prior finite-network, permission, compaction and direct-feature boundaries remain intact

#### Scenario: US3 - Keep network extras and asynchronous safety 5

- **GIVEN** delayed or lost remote replies
- **WHEN** automatic stations and UI updates resume pending work
- **THEN** retries use elapsed time instead of frame rate, admitted requests poll status without repeating inventory snapshots, and a congested connection defers retryable payloads while preserving operation identity and custody

#### Scenario: US3 - Keep network extras and asynchronous safety 6

- **GIVEN** an output delivery with inline or delayed owner acknowledgements
- **WHEN** recovery rebuilds its participants
- **THEN** authenticated phase receipts survive that rebind and a confirmed delivery releases both chest and source reservations exactly once

#### Scenario: US3 - Keep network extras and asynchronous safety 7

- **GIVEN** a saved world whose native ZDO identifiers change during load
- **WHEN** pending work resumes
- **THEN** durable participant references reconnect to the same objects; legacy captured output may be recovered only from matching world, actor, pending intent, escrow and native custody proofs. Missing or conflicting evidence must not clear reservations or replay capture

#### Scenario: US3 - Keep network extras and asynchronous safety 8

- **GIVEN** that the durable operation journal is unavailable
- **WHEN** a new mutation is requested
- **THEN** no native capture or participant reservation starts with only an in-memory record

### Requirement: Deployment and authority

The feature MUST require compatible SCS server/client installations, synchronize gameplay configuration and validate operation actor/context at the authoritative effect boundary rather than trusting client-provided inventory totals.

#### Scenario: US5 - Share the store without ambiguous transfers 1

- **GIVEN** a chest appears directly and behind two terminals
- **WHEN** resources or capacity are requested
- **THEN** that physical chest contributes once

#### Scenario: US5 - Share the store without ambiguous transfers 2

- **GIVEN** simultaneous requests for the same stack or free slot
- **WHEN** operations execute
- **THEN** reservations prevent overspending and overfilling

#### Scenario: US5 - Share the store without ambiguous transfers 3

- **GIVEN** an operation's response is unknown
- **WHEN** it is retried or the session reconnects
- **THEN** its stable identity is reconciled before further effects and a completed operation is not applied twice

#### Scenario: US5 - Share the store without ambiguous transfers 4

- **GIVEN** access or membership changes before an operation commits
- **WHEN** the pending operation proceeds
- **THEN** it is rejected or recovered without bypassing permissions or losing accounted items

#### Scenario: US6 - Install and operate the feature 1

- **GIVEN** matching server and client installations
- **WHEN** a player builds a terminal and names a network
- **THEN** the controls and status explain its member capacity and available actions

#### Scenario: US6 - Install and operate the feature 2

- **GIVEN** incompatible client/server versions or an unavailable member
- **WHEN** the player attempts an operation
- **THEN** the feature reports an actionable state rather than optimistic success

### Requirement: Bounded work and honest status

Discovery, planning, execution and retries MUST remain bounded and cached by relevant state; unavailable, busy, pending, rejected and confirmed outcomes MUST remain distinguishable, pending UI state MUST refer to the applicable target/operation, and search/drag/refresh or duplicate gestures MUST NOT submit duplicate writes or hide the authoritative result.

#### Scenario: US1 - Link a chest and use it after reloading 1

- **GIVEN** an accessible idle chest in single player
- **WHEN** its name is submitted and the local acknowledgement arrives immediately
- **THEN** the final confirmed status and stored name survive return from the submission call and reopening

#### Scenario: US1 - Link a chest and use it after reloading 2

- **GIVEN** a name request waiting for its owner
- **WHEN** the same dialog is reopened
- **THEN** the current request and draft remain visible and a second request is not created

#### Scenario: US1 - Link a chest and use it after reloading 3

- **GIVEN** a pending operation for another chest or another world
- **WHEN** a naming dialog is opened
- **THEN** the unrelated marker does not disable linking and its durable inventory custody is not erased

#### Scenario: US1 - Link a chest and use it after reloading 4

- **GIVEN** a previous game session's cached object IDs
- **WHEN** the same world is reopened with a new object manager
- **THEN** the runtime discards obsolete references before looking them up and reloads durable state without invalid-ID exceptions or cross-session results

#### Scenario: US1 - Link a chest and use it after reloading 5

- **GIVEN** a denied or changed target
- **WHEN** naming completes
- **THEN** rejection is explicit, its draft is retained and no name or inventory is changed without authorization

#### Scenario: US1 - Link a chest and use it after reloading 6

- **GIVEN** an eligible chest or terminal under the player's interaction cursor
- **WHEN** the player presses the configurable naming shortcut (default `Alt + N`)
- **THEN** the naming dialog opens subject to the existing access checks; `Alt + E` no longer opens that dialog, and typing in a UI cannot trigger the shortcut

#### Scenario: US3 - Keep network extras and asynchronous safety 1

- **GIVEN** active search or view sorting
- **WHEN** the view changes
- **THEN** only presentation changes, all accessible stock remains available to stations and bulk actions use their documented full scope

#### Scenario: US3 - Keep network extras and asynchronous safety 2

- **GIVEN** a pending cross-grid request
- **WHEN** more gestures or refreshes occur
- **THEN** no second mutation is submitted, drag identity remains stable and the authoritative inventories refresh after the same operation completes

#### Scenario: US3 - Keep network extras and asynchronous safety 3

- **GIVEN** a bulk transfer
- **WHEN** one step is pending or the UI closes
- **THEN** only the submitted operation persists and unsubmitted entries are not moved; later steps require confirmed preceding results and fresh capacity

#### Scenario: US3 - Keep network extras and asynchronous safety 4

- **GIVEN** ordinary chests, crafting, processing and direct quick-stack
- **WHEN** the terminal UI is used or closed
- **THEN** prior finite-network, permission, compaction and direct-feature boundaries remain intact

#### Scenario: US3 - Keep network extras and asynchronous safety 5

- **GIVEN** delayed or lost remote replies
- **WHEN** automatic stations and UI updates resume pending work
- **THEN** retries use elapsed time instead of frame rate, admitted requests poll status without repeating inventory snapshots, and a congested connection defers retryable payloads while preserving operation identity and custody

#### Scenario: US3 - Keep network extras and asynchronous safety 6

- **GIVEN** an output delivery with inline or delayed owner acknowledgements
- **WHEN** recovery rebuilds its participants
- **THEN** authenticated phase receipts survive that rebind and a confirmed delivery releases both chest and source reservations exactly once

#### Scenario: US3 - Keep network extras and asynchronous safety 7

- **GIVEN** a saved world whose native ZDO identifiers change during load
- **WHEN** pending work resumes
- **THEN** durable participant references reconnect to the same objects; legacy captured output may be recovered only from matching world, actor, pending intent, escrow and native custody proofs. Missing or conflicting evidence must not clear reservations or replay capture

#### Scenario: US3 - Keep network extras and asynchronous safety 8

- **GIVEN** that the durable operation journal is unavailable
- **WHEN** a new mutation is requested
- **THEN** no native capture or participant reservation starts with only an in-memory record
