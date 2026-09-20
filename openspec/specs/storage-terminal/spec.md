# Storage Terminal Specification

## Purpose

Durable behavioral contract for `storage-terminal`.

## Requirements

### Requirement: Terminal interaction

The system MUST provide a buildable terminal with searchable combined contents, quantities, finite capacity, network naming, deposit, withdrawal, view sorting and Organize controls with clear pending/result states.

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

#### Scenario: US6 - Install and operate the feature 1

- **GIVEN** matching server and client installations
- **WHEN** a player builds a terminal and names a network
- **THEN** the controls and status explain its member capacity and available actions

#### Scenario: US6 - Install and operate the feature 2

- **GIVEN** incompatible client/server versions or an unavailable member
- **WHEN** the player attempts an operation
- **THEN** the feature reports an actionable state rather than optimistic success

### Requirement: Distributed deposit

The system MUST distribute deposits into compatible partial stacks and finite member slots, preserving metadata and reporting confirmed partial acceptance; bulk deposit MUST respect existing protected/equipped player-item rules.

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

### Requirement: Distributed withdrawal

The system MUST withdraw selected compatible quantities into available player space without requiring physical chest navigation, returning confirmed quantities and preserving any remainder.

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
