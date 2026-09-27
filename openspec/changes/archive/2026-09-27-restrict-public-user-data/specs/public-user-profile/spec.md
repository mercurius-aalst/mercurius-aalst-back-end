## MODIFIED Requirements

### Requirement: Public user profile endpoint
The API MUST expose `GET /v1/lan/public/users/{username}` for public username lookup, and its response MUST contain only the username.

#### Scenario: Anonymous public lookup
- **WHEN** an anonymous client requests a valid public username
- **THEN** the response contains the username only

#### Scenario: Authenticated public lookup
- **WHEN** an authenticated client requests a valid public username
- **THEN** the response contains the username only

#### Scenario: Case-insensitive lookup
- **WHEN** a client requests a username with different casing than stored
- **THEN** lookup succeeds using normalized username matching

### Requirement: Detailed user profile access
The API MUST expose detailed user profiles by username only to administrators.

#### Scenario: Administrator reads a detailed profile
- **WHEN** an administrator requests `GET /v1/lan/users/{username}` for an existing user
- **THEN** the response includes the existing detailed user profile fields

#### Scenario: Non-administrator cannot read a detailed profile
- **WHEN** an anonymous or non-administrator client requests `GET /v1/lan/users/{username}`
- **THEN** the request is denied

#### Scenario: Missing user
- **WHEN** an administrator requests a username that does not exist
- **THEN** the endpoint returns 404

#### Scenario: User match summaries require administrator access
- **WHEN** a client requests `GET /v1/lan/users/{username}/match-summaries`
- **THEN** only an administrator can read that user's match summaries
- **AND** the anonymous `GET /v1/lan/public/users/{username}/match-summaries` route is not exposed
