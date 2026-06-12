## MODIFIED Requirements

### Requirement: Anonymous participant privacy
Anonymous public API responses that embed participants MUST expose only fields required for public display and navigation.

#### Scenario: Anonymous game detail participants
- **WHEN** an anonymous client reads a game detail response
- **THEN** embedded users, team registrations, and team roster members omit email, first name, last name, Auth0 IDs, deleted state, timestamps, confirmation tokens, notification identifiers, and private registration metadata

#### Scenario: Anonymous placement participants
- **WHEN** an anonymous client reads placement data
- **THEN** placement users and teams use privacy-safe participant DTOs

#### Scenario: Anonymous team data
- **WHEN** an anonymous client reads public team data
- **THEN** the response omits pending invites, declined invites, invite history, pending roster confirmation state, inactive tournament roster history, and private member fields

#### Scenario: Anonymous registration roster participants
- **WHEN** an anonymous client reads public tournament registration or roster data
- **THEN** roster users and team members are represented with privacy-safe public fields only
- **AND** pending confirmation tokens, withdrawn notification state, removal metadata, and admin-only registration details are omitted

### Requirement: Shared participant privacy
Shared participant responses MUST remain privacy-safe for anonymous and authenticated callers while including platform identifiers declared public by the website privacy policy.

#### Scenario: Authenticated shared participant response
- **WHEN** an authenticated client reads a game, placement, team, registration, or roster response
- **THEN** embedded participants include Discord, Steam, and Riot IDs when those fields are part of the shared public participant contract but still omit private account fields, confirmation tokens, notification identifiers, and admin-only registration metadata

#### Scenario: Authorized profile response
- **WHEN** an authorized profile workflow needs private user data or actionable confirmation notifications
- **THEN** it uses the dedicated current-user or notification API rather than an embedded participant or public registration response
