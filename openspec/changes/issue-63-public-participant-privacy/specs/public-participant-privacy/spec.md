## ADDED Requirements

### Requirement: Anonymous participant privacy
Anonymous public API responses that embed participants MUST expose only fields required for public display and navigation.

#### Scenario: Anonymous game detail participants
- **WHEN** an anonymous client reads a game detail response
- **THEN** embedded users and team members omit email, first name, last name, Auth0 IDs, deleted state, and timestamps

#### Scenario: Anonymous placement participants
- **WHEN** an anonymous client reads placement data
- **THEN** placement users and teams use privacy-safe participant DTOs

#### Scenario: Anonymous team data
- **WHEN** an anonymous client reads public team data
- **THEN** the response omits pending invites, declined invites, invite history, and private member fields

### Requirement: Shared participant privacy
Shared participant responses MUST remain privacy-safe for anonymous and authenticated callers while including platform identifiers declared public by the website privacy policy.

#### Scenario: Authenticated shared participant response
- **WHEN** an authenticated client reads a game, placement, or team response
- **THEN** embedded participants include Discord, Steam, and Riot IDs but still omit private account fields

#### Scenario: Authorized profile response
- **WHEN** an authorized profile workflow needs private user data
- **THEN** it uses the dedicated user API rather than an embedded participant response

### Requirement: Admin data preservation
Authorized admin/current-user APIs MUST continue returning the full DTOs required by admin and profile workflows.

#### Scenario: Admin user response shape retained
- **WHEN** an admin or current user reads an authorized user workflow
- **THEN** the response still includes the fields needed by that workflow
