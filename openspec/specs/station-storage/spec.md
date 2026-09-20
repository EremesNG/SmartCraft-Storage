# Station Storage Specification

## Purpose

Durable behavioral contract for `station-storage`.

## Requirements

### Requirement: Crafting source integration

Crafting and upgrades MUST discover nearby terminals using the current player-based crafting radius, query their eligible members and consume a complete valid recipe exactly once before granting the normal result.

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

### Requirement: Processor source integration

Enabled smelter/kiln, cooking, fireplace and fermenter input/fuel automation MUST consider terminals within the corresponding machine radius and coordinate consumption with machine input capacity and existing material/production restrictions.

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

### Requirement: Processor sink integration

Enabled smelter/kiln, cooking, fermenter and beehive output collection MUST consider nearby terminals as destinations and let the network choose physical receiving chests while preserving each producer's confirmed-remainder policy.

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

### Requirement: Direct-feature boundaries

Quick-stack, restock, hammer building, animal feeding, plant harvest and existing optional integrations MUST retain their direct physical search scopes unless explicitly part of this change, while respecting shared-operation coordination for the physical chests they touch.

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
