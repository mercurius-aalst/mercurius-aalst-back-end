## Purpose

Define privacy-safe public access to team profile data and tournament navigation.

## Requirements

### Requirement: Public team profile endpoint
The API MUST expose `GET /v1/lan/public/teams/{teamName}` for public team-name profile lookup.

#### Scenario: Successful team profile lookup
- **WHEN** a client requests an existing team by team name
- **THEN** the response includes `teamName`, `captainUsername`, `members`, and `tournaments`

#### Scenario: Case-insensitive lookup
- **WHEN** a client requests the team name using different casing than stored
- **THEN** lookup succeeds

#### Scenario: Missing team
- **WHEN** the requested team does not exist
- **THEN** the endpoint returns 404

### Requirement: Public team profile privacy
The public team profile endpoint MUST return only public-safe team data.

#### Scenario: Members contain usernames only
- **WHEN** a team profile response includes members
- **THEN** each member object contains username only

#### Scenario: Invite data omitted
- **WHEN** a team has pending, declined, or historical invites
- **THEN** the public profile response omits all invite data

#### Scenario: Private member fields omitted
- **WHEN** a team profile response includes captain or member data
- **THEN** it omits email, first name, last name, platform IDs, deleted state, Auth0 IDs, and timestamps

### Requirement: Public team tournament projection
The endpoint MUST include public tournament navigation data for tournaments where the team is registered.

#### Scenario: Registered tournaments returned
- **WHEN** a team is registered for tournaments
- **THEN** the response includes tournaments with only `gameId` and tournament name

#### Scenario: Stable ordering
- **WHEN** members or tournaments are returned
- **THEN** they are sorted predictably
