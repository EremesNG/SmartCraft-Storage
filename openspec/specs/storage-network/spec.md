# Storage Network Specification

## Purpose

Durable behavioral contract for `storage-network`.

## Requirements

### Requirement: Named membership

The system MUST assign and persist normalized network names on eligible ordinary chests and terminals with reliable local and remote confirmation, preserve an unconfirmed naming draft, and select members by matching name, terminal radius, access and loaded-world state while excluding terminals, graves and transport inventories as backing members.

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

### Requirement: Access preservation

The system MUST validate access to the terminal and each physical participant for the authorizing actor, preserve wards/private-chest restrictions and revalidate before effects; network ownership alone MUST NOT confer access.

#### Scenario: US1 - Manage one inventory across linked chests 1

- **GIVEN** accessible loaded chests with the terminal's network name within its configured radius
- **WHEN** the network is queried
- **THEN** the combined quantities and finite slot capacity are shown without opening any backing chest

#### Scenario: US1 - Manage one inventory across linked chests 2

- **GIVEN** a same-named chest outside the radius or inaccessible through a ward
- **WHEN** the player opens the terminal
- **THEN** its contents and capacity are excluded

#### Scenario: US1 - Manage one inventory across linked chests 3

- **GIVEN** compatible partial stacks and free slots across members
- **WHEN** the player deposits or withdraws a quantity
- **THEN** only confirmed quantities move and any unaccepted quantity remains accounted for

#### Scenario: US1 - Manage one inventory across linked chests 4

- **GIVEN** items with different quality, variant or custom metadata
- **WHEN** the terminal groups or moves them
- **THEN** their identity and metadata remain distinguishable and incompatible stacks are never fused

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

### Requirement: Unique resource projection

The system MUST count quantities and capacity from each physical chest once across direct and terminal routes, preserve distinct item identities and prohibit recursive terminal expansion.

#### Scenario: US1 - Manage one inventory across linked chests 1

- **GIVEN** accessible loaded chests with the terminal's network name within its configured radius
- **WHEN** the network is queried
- **THEN** the combined quantities and finite slot capacity are shown without opening any backing chest

#### Scenario: US1 - Manage one inventory across linked chests 2

- **GIVEN** a same-named chest outside the radius or inaccessible through a ward
- **WHEN** the player opens the terminal
- **THEN** its contents and capacity are excluded

#### Scenario: US1 - Manage one inventory across linked chests 3

- **GIVEN** compatible partial stacks and free slots across members
- **WHEN** the player deposits or withdraws a quantity
- **THEN** only confirmed quantities move and any unaccepted quantity remains accounted for

#### Scenario: US1 - Manage one inventory across linked chests 4

- **GIVEN** items with different quality, variant or custom metadata
- **WHEN** the terminal groups or moves them
- **THEN** their identity and metadata remain distinguishable and incompatible stacks are never fused

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

### Requirement: Closed-window availability

Eligible networks MUST remain usable by supported stations with the terminal window closed; the system MUST respect loaded-world and current player/automation constraints without forcing distant zones to load.

#### Scenario: US1 - Manage one inventory across linked chests 1

- **GIVEN** accessible loaded chests with the terminal's network name within its configured radius
- **WHEN** the network is queried
- **THEN** the combined quantities and finite slot capacity are shown without opening any backing chest

#### Scenario: US1 - Manage one inventory across linked chests 2

- **GIVEN** a same-named chest outside the radius or inaccessible through a ward
- **WHEN** the player opens the terminal
- **THEN** its contents and capacity are excluded

#### Scenario: US1 - Manage one inventory across linked chests 3

- **GIVEN** compatible partial stacks and free slots across members
- **WHEN** the player deposits or withdraws a quantity
- **THEN** only confirmed quantities move and any unaccepted quantity remains accounted for

#### Scenario: US1 - Manage one inventory across linked chests 4

- **GIVEN** items with different quality, variant or custom metadata
- **WHEN** the terminal groups or moves them
- **THEN** their identity and metadata remain distinguishable and incompatible stacks are never fused

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
