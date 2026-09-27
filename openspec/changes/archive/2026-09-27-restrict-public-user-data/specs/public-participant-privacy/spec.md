## MODIFIED Requirements

### Requirement: Shared participant privacy
Public tournament, placement, registration, roster, and team responses MUST identify users by username only, for anonymous and authenticated callers. They MUST NOT expose users' first or last names, email, Discord ID, Steam ID, or Riot ID.

#### Scenario: Anonymous shared participant response
- **WHEN** an anonymous client reads tournament, placement, registration, roster, or public team data
- **THEN** each embedded user exposes the username only

#### Scenario: Authenticated shared participant response
- **WHEN** an authenticated non-administrator client reads tournament, placement, registration, roster, or public team data
- **THEN** each embedded user exposes the username only

#### Scenario: Private team management response
- **WHEN** an authenticated user reads their team management data
- **THEN** the API retains the fields required by that private workflow

### Requirement: Public user detail access
Shared participant responses MUST NOT act as a source of detailed user profiles.

#### Scenario: Match-scoped workflow reads opponent details
- **WHEN** a user needs their assigned opponent's details
- **THEN** the user reads them from the authorized match opponent profile endpoint
