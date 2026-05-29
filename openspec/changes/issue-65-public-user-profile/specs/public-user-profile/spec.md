## ADDED Requirements

### Requirement: Public user profile endpoint
The API MUST expose `GET /v1/lan/public/users/{username}` for public username-based profile lookup.

#### Scenario: Anonymous public profile lookup
- **WHEN** an anonymous client requests a valid complete profile by username
- **THEN** the response includes username, first name, and last name

#### Scenario: Authenticated public profile lookup
- **WHEN** an authenticated client requests a valid complete profile by username
- **THEN** the response includes username, first name, last name, Discord ID, Steam ID, and Riot ID

#### Scenario: Case-insensitive lookup
- **WHEN** a client requests a username with different casing than stored
- **THEN** lookup succeeds using normalized username matching

### Requirement: Public user profile privacy
The public user profile endpoint MUST omit account-private fields.

#### Scenario: Anonymous private fields omitted
- **WHEN** an anonymous client reads a public user profile
- **THEN** the response omits email, email verification state, Auth0 ID, deleted state, timestamps, and platform IDs

#### Scenario: Authenticated private fields omitted
- **WHEN** an authenticated client reads a public user profile
- **THEN** the response still omits email, email verification state, Auth0 ID, deleted state, and timestamps

### Requirement: Public user profile not found behavior
The endpoint MUST return not found for profiles that are not public.

#### Scenario: Missing user
- **WHEN** the requested username does not exist
- **THEN** the endpoint returns 404

#### Scenario: Deleted or incomplete user
- **WHEN** the requested username belongs to a deleted or incomplete profile
- **THEN** the endpoint returns 404
