# Storage Transactions Specification

## Purpose

Durable behavioral contract for `storage-transactions`.

## Requirements

### Requirement: Conservation and exact capacity

Operations MUST preserve quantities and item metadata across inventories and explicitly accounted in-flight state, respect maximum stack sizes, reserve finite destination capacity and never treat an unknown response as confirmed failure or success.

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

Network operations MUST have stable identities and recoverable progress sufficient to reconcile duplicate requests, lost acknowledgements, controlled reconnects and ownership changes without replaying completed effects or silently discarding pending products.

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

Discovery, planning, execution and retry work MUST be bounded and cached by relevant network state; unavailable/busy/pending/rejected/confirmed outcomes MUST remain distinguishable to callers and players.

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
