## ADDED Requirements

### Requirement: Sponsorship coordinates Sponsor logo lifecycle
Sponsorship MUST compensate a newly stored Sponsor logo if the following Sponsor mutation, outbox
write, or transaction commit fails or is cancelled. It MUST retire a replaced or deleted owned
Sponsor logo only after the Sponsor state and matching durable lifecycle event commit succeeds.

#### Scenario: Sponsor create or replacement fails before commit
- **WHEN** Sponsorship stores a Sponsor logo and its owning mutation or matching durable-event commit does not succeed
- **THEN** Sponsorship attempts non-cancelled deletion of only the new logo and preserves the original failure
- **AND** it does not delete the previously current logo

#### Scenario: Sponsor replacement commits
- **WHEN** a Sponsor update commits with a different owned logo URL
- **THEN** Sponsorship attempts to delete the previous logo after the state and SponsorUpdated event commit

#### Scenario: Sponsor deletion commits
- **WHEN** Sponsor deletion and its SponsorDeleted event commit
- **THEN** Sponsorship attempts to delete the former current logo with a non-cancelled token

#### Scenario: Sponsor post-commit cleanup fails
- **WHEN** Sponsor state and its lifecycle event are committed but logo deletion fails
- **THEN** Sponsorship logs the orphan and returns the committed mutation outcome
