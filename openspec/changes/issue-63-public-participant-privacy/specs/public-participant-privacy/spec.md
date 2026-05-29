## ADDED Requirements

### Requirement: Anonymous participant privacy
Anonymous public API responses that embed participants MUST expose only fields required for public display and navigation.

#### Scenario: Anonymous game detail participants
- **WHEN** an anonymous client reads a game detail response
- **THEN** embedded users and team members omit email, first name, last name, platform IDs, Auth0 IDs, deleted state, and timestamps

#### Scenario: Anonymous placement participants
- **WHEN** an anonymous client reads placement data
- **THEN** placement users and teams use privacy-safe participant DTOs

#### Scenario: Anonymous team data
- **WHEN** an anonymous client reads public team data
- **THEN** the response omits pending invites, declined invites, invite history, and private member fields

### Requirement: Authenticated public enrichment
Authenticated public responses MUST include linked platform IDs only for endpoints that explicitly allow them.

#### Scenario: Authenticated public participant response
- **WHEN** an authenticated client reads an endpoint that permits linked identity display
- **THEN** the response may include Discord, Steam, and Riot IDs but still omits email, Auth0 IDs, deleted state, and timestamps

#### Scenario: Anonymous platform ID suppression
- **WHEN** an anonymous client reads the same public endpoint
- **THEN** platform identifiers are not present in the response body

### Requirement: Admin data preservation
Authorized admin/current-user APIs MUST continue returning the full DTOs required by admin and profile workflows.

#### Scenario: Admin response shape retained
- **WHEN** an admin reads an admin user, team, game, or placement workflow that requires full data
- **THEN** authorized responses still include the fields needed by that workflow
