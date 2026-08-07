## ADDED Requirements

### Requirement: Historical registration display snapshots
Competition MUST persist the username and team-name display facts captured when an individual or
team roster registration is created, while retaining the external user and team IDs as references.

#### Scenario: Individual registration captures user display data
- **WHEN** an authenticated user registers for an individual tournament
- **THEN** Competition stores the user's ID and username-at-registration snapshot

#### Scenario: Team roster captures historical display data
- **WHEN** a captain submits a valid team roster
- **THEN** Competition stores the team ID and team-name-at-registration snapshot
- **AND** stores each selected user ID and username-at-registration snapshot

#### Scenario: Existing registrations are migrated
- **WHEN** the Phase 11 migration is applied to an existing database
- **THEN** existing registration and roster rows are backfilled from their referenced user and team
  records where those records still exist
- **AND** no existing registration row is deleted

#### Scenario: Public registration JSON remains stable
- **WHEN** a client reads tournament registration data after the snapshot migration
- **THEN** the existing registration and roster JSON property names and privacy rules remain
  unchanged
