# Storage Network Specification

## Purpose

Durable behavioral contract for `storage-network`.

## Requirements

### Requirement: Named membership

The system MUST support assigning a network name to eligible ordinary physical chests and select members by normalized matching name, terminal radius and valid loaded world state, excluding terminals, graves and transport inventories as backing members.

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
