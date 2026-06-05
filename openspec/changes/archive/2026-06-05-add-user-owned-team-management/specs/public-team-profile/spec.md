## ADDED Requirements

### Requirement: Public team logo projection
The public team profile endpoint MUST expose only a safe public team logo reference when a team has an active logo.

#### Scenario: Team with logo returns safe logo reference
- **WHEN** a public client requests a team profile for a team with an active logo
- **THEN** the response includes a safe public logo reference and no storage-private path details

#### Scenario: Team without logo omits or nulls logo reference
- **WHEN** a public client requests a team profile for a team without an active logo
- **THEN** the response omits the logo reference or returns it as null according to the DTO contract

#### Scenario: Invite and private data remain omitted
- **WHEN** a public team profile includes a logo reference
- **THEN** the response still omits invite data, email, first name, last name, platform IDs, deleted state, Auth0 IDs, and timestamps
