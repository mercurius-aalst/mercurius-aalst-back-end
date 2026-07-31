## MODIFIED Requirements

### Requirement: Transactional event publication
The system SHALL save supported business mutations and their durable outbox messages in the same
database commit.

#### Scenario: Business change and event commit together
- **WHEN** a supported Teams lifecycle mutation succeeds
- **THEN** the Teams state change and its durable outbox message MUST both be committed

#### Scenario: Event enqueue failure prevents commit
- **WHEN** a supported Teams lifecycle mutation cannot enqueue its durable integration event
- **THEN** the Teams state change MUST NOT be committed

#### Scenario: Sponsorship change and event commit together
- **WHEN** Sponsorship creates, updates, or deletes a sponsor or changes a game sponsor placement
- **THEN** the Sponsorship state change and its matching durable outbox message MUST both be
  committed

### Requirement: Versioned integration events
The system SHALL use versioned durable event payloads for Teams and Sponsorship lifecycle facts.

#### Scenario: Teams publishes versioned lifecycle events
- **WHEN** Teams creates, renames, deletes, adds a member, removes a member, or transfers captain ownership
- **THEN** the corresponding durable integration event payload MUST include the Team id and current monotonic Team version

#### Scenario: Stale version does not overwrite newer projection
- **WHEN** a consumer receives a Teams integration event whose version is older than the stored projection version
- **THEN** the consumer MUST ignore the stale event and keep the newer projection data

#### Scenario: Sponsorship publishes versioned lifecycle events
- **WHEN** Sponsorship creates, updates, or deletes a sponsor or creates, replaces, or removes a
  game sponsor placement
- **THEN** it MUST publish the matching V1 event payload
- **AND** the payload MUST include the SponsorId for sponsor facts and the GameId plus current
  placement facts or removal state for placement facts
