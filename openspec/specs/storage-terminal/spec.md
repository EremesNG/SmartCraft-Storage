# Storage Terminal Specification

## Purpose

Durable behavioral contract for `storage-terminal`.

## Requirements

### Requirement: Terminal interaction

The system MUST provide a buildable manager through the game's native player/chest inventory grids, familiar click/drag/split/quick-transfer gestures, item tooltips, searchable combined contents, aggregate quantities, finite capacity, naming, view sorting and Organize controls with clear result states; ordinary transfers MUST NOT require selecting a row and then pressing Deposit or Withdraw.

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

### Requirement: Distributed deposit

The system MUST distribute gesture-driven and bulk deposits into compatible partial stacks and finite member slots, preserving metadata and reporting confirmed partial acceptance; protected/equipped items MUST remain protected, and bulk work MUST be sequential, refresh source identities and stop submitting when its UI closes or a preceding step fails.

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

### Requirement: Distributed withdrawal

The system MUST withdraw selected compatible quantities without visiting backing chests, preserve remainders and respect player space; dragging to an empty or compatible player slot MUST use that exact slot, incompatible or changed slots MUST reject without a fake swap, and quick-transfer MAY choose available player space automatically.

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

### Requirement: Network compaction

Organize MUST consolidate compatible stacks and occupy fewer chests when capacity permits, allow mixed materials, avoid permanent category reservations and produce no moves when an unchanged network is already organized; visual sorting MUST NOT relocate items.

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
