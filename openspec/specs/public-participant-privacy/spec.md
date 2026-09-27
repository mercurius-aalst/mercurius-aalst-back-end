# public-participant-privacy Specification

## Purpose

Define the privacy-safe user shape embedded in shared tournament, placement, and team API responses.

## Requirements

### Requirement: Anonymous participant privacy
Anonymous public API responses that embed participants MUST expose only fields required for public display and navigation.

#### Scenario: Anonymous tournament detail participants
- **WHEN** an anonymous client reads a tournament detail response
- **THEN** embedded users, active team registrations, and active team roster members omit email, first name, last name, Auth0 IDs, deleted state, timestamps, pending confirmation state, confirmation tokens, notification identifiers, and private registration metadata

#### Scenario: Anonymous placement participants
- **WHEN** an anonymous client reads placement data
- **THEN** placement users and teams use privacy-safe participant DTOs

#### Scenario: Anonymous team data
- **WHEN** an anonymous client reads public team data
- **THEN** the response omits pending invites, declined invites, invite history, pending roster confirmation state, inactive tournament roster history, and private member fields

#### Scenario: Anonymous registration roster participants
- **WHEN** an anonymous client reads public tournament registration or roster data
- **THEN** roster users and team members are represented with privacy-safe public fields only
- **AND** pending registrations, pending confirmation state, confirmation tokens, withdrawn notification state, and admin-only registration details are omitted

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

#### Scenario: Authorized profile response
- **WHEN** an authorized profile workflow needs private user data or actionable confirmation notifications
- **THEN** it uses the dedicated current-user or notification API rather than an embedded participant or public registration response

### Requirement: Public user detail access
Shared participant responses MUST NOT act as a source of detailed user profiles.

#### Scenario: Match-scoped workflow reads opponent details
- **WHEN** a user needs their assigned opponent's details
- **THEN** the user reads them from the authorized match opponent profile endpoint

### Requirement: Admin data preservation
Authorized admin/current-user APIs MUST continue returning the full DTOs required by admin and profile workflows.

#### Scenario: Admin user response shape retained
- **WHEN** an admin or current user reads an authorized user workflow
- **THEN** the response still includes the fields needed by that workflow
