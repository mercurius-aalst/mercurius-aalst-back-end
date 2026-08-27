## ADDED Requirements

### Requirement: Competition coordinates Game image lifecycle
Competition MUST compensate a newly stored Game image if the following Game mutation or commit
fails or is cancelled, and MUST retire replaced or deleted owned Game images only after the Game
state and durable lifecycle event commit succeeds.

#### Scenario: Game image mutation fails before commit
- **WHEN** Competition stores a Game image and the owning Game mutation or durable-event commit does not succeed
- **THEN** Competition attempts non-cancelled deletion of only the new image and preserves the original failure

#### Scenario: Game replacement or deletion commits
- **WHEN** a Game image is replaced or a Game is deleted successfully
- **THEN** Competition attempts eligible prior-image deletion after the Game state and durable event commit

### Requirement: Competition provides bounded historical Team-logo reference decisions
Competition MUST implement the existing Teams read-contract boundary with one cancellation-aware,
no-tracking existence query over all `TournamentRegistration.TeamLogoUrlAtRegistration` values.
It MUST NOT expose Competition entities, repositories, or queryables through the contract.

#### Scenario: Teams evaluates historical logo retention
- **WHEN** Teams asks whether a non-blank logo URL is retained by tournament history
- **THEN** Competition executes one bounded existence query over all tournament-registration snapshots
- **AND** returns whether an ordinally equal stored URL exists
