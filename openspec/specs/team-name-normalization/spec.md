## Purpose

Team-name normalization defines how team display names are validated, normalized, persisted, and looked up for reliable public routing and search.

## Requirements

### Requirement: Case-insensitive team-name uniqueness
The system MUST prevent active teams from sharing the same name when compared case-insensitively.

#### Scenario: Duplicate create rejected
- **WHEN** `Team Alpha` already exists and an admin creates `team alpha`
- **THEN** the API rejects the duplicate name

#### Scenario: Duplicate update rejected
- **WHEN** an admin updates a team name to collide with another team by casing only
- **THEN** the API rejects the update

#### Scenario: Database uniqueness enforced
- **WHEN** concurrent requests attempt to create case-insensitive duplicate team names
- **THEN** the database constraint prevents duplicate persistence

### Requirement: Team-name normalization
The system MUST normalize team names consistently for create, update, lookup, and search.

#### Scenario: Store normalized team name
- **WHEN** a team is created or renamed
- **THEN** the normalized team-name value is stored with the team

#### Scenario: Backfill existing teams
- **WHEN** the migration is applied to existing team records
- **THEN** normalized names are populated from existing display names

#### Scenario: Invalid name rejected
- **WHEN** a team name is empty, malformed, or exceeds the allowed length
- **THEN** the API rejects it with a safe validation response

### Requirement: Case-insensitive lookup
Public team lookup and search MUST use normalized or otherwise indexed case-insensitive matching.

#### Scenario: Public team lookup by casing
- **WHEN** a client requests a team profile with different casing than stored
- **THEN** lookup returns the matching team

#### Scenario: Search by casing
- **WHEN** a client searches for a team using different casing than stored
- **THEN** search returns the matching team
