## ADDED Requirements

### Requirement: User-owned team-name normalization
User-created and captain-renamed teams MUST use the same team-name validation, normalization, and case-insensitive uniqueness behavior as admin-managed teams.

#### Scenario: User-created team stores normalized name
- **WHEN** an authenticated user creates a team with a valid display name
- **THEN** the API stores the normalized team-name value for lookup and uniqueness checks

#### Scenario: User-created duplicate rejected
- **WHEN** an authenticated user creates a team whose name collides case-insensitively with an active existing team
- **THEN** the API rejects the duplicate name

#### Scenario: Captain rename validates name
- **WHEN** a captain renames a team
- **THEN** the API applies the existing team-name validation, normalization, and uniqueness rules
