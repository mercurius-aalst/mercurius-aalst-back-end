## ADDED Requirements

### Requirement: Resource-oriented team membership, invitation, and logo routes
The API MUST expose authenticated resource-oriented routes for team leave, invitation creation, invitation response, and logo replacement while preserving the existing team authorization and lifecycle rules.

#### Scenario: Team member leaves through self membership deletion
- **WHEN** an authenticated team member sends `DELETE /v1/lan/teams/{teamId}/members/me`
- **THEN** the API MUST apply the existing team-leave rules and return the existing leave response JSON shape

#### Scenario: Captain creates an invitation in the team invitation collection
- **WHEN** a team captain sends `POST /v1/lan/teams/{teamId}/invites` with the recipient user id in the request body
- **THEN** the API MUST apply the existing invitation creation and validation rules and return the existing invite response JSON shape

#### Scenario: Invite recipient updates the invite resource
- **WHEN** an authenticated invite recipient sends `PATCH /v1/lan/team-invites/{inviteId}` with an accepted or declined response
- **THEN** the API MUST apply the existing invitation response rules and return the existing invite response JSON shape

#### Scenario: Captain replaces a team logo resource
- **WHEN** a team captain sends multipart form data to `PUT /v1/lan/teams/{teamId}/logo`
- **THEN** the API MUST apply the existing logo validation and replacement rules and return the existing logo response JSON shape

#### Scenario: Team action routes are absent
- **WHEN** a client calls `POST /v1/lan/teams/{teamId}/leave`, `POST /v1/lan/teams/{teamId}/invites/{userId}`, `PUT /v1/lan/teams/invites/{inviteId}`, or `POST /v1/lan/teams/{teamId}/logo`
- **THEN** the API MUST NOT expose those routes
