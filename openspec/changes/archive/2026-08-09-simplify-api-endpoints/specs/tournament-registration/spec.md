## ADDED Requirements

### Requirement: Resource-oriented tournament registration routes
The API MUST expose authenticated resource-oriented routes for individual registration, registration eligibility, team roster submission, and roster-member confirmation while preserving the existing registration rules and response DTO JSON shapes.

#### Scenario: Current user creates an individual registration resource
- **WHEN** an authenticated user sends `PUT /v1/lan/games/{gameId}/registrations/individual/me`
- **THEN** the API MUST apply the existing individual-registration rules and return the existing registration response JSON shape

#### Scenario: Client reads individual or team registration eligibility
- **WHEN** an authenticated client sends `GET /v1/lan/games/{gameId}/registrations/individual/eligibility` or `GET /v1/lan/games/{gameId}/registrations/teams/{teamId}/eligibility`
- **THEN** the API MUST return the existing eligibility result for the requested registration resource

#### Scenario: Captain calculates proposed team roster eligibility
- **WHEN** an authenticated team captain sends `POST /v1/lan/games/{gameId}/registrations/teams/{teamId}/roster/eligibility` with proposed roster user ids
- **THEN** the API MUST return the existing proposed-roster eligibility result without changing registration or roster state

#### Scenario: Captain replaces the team registration roster
- **WHEN** an authenticated team captain sends `PUT /v1/lan/games/{gameId}/registrations/teams/{teamId}/roster`
- **THEN** the API MUST apply the existing roster submission, replacement, and validation rules and return the existing registration response JSON shape

#### Scenario: Roster member confirms their roster-member resource
- **WHEN** an authenticated selected roster member sends `PATCH /v1/lan/games/{gameId}/registrations/roster-members/{rosterMemberId}` with confirmation status `Confirmed`
- **THEN** the API MUST apply the existing roster-confirmation rules and return the existing registration response JSON shape

#### Scenario: Unsupported roster-member confirmation update is rejected
- **WHEN** a client sends a roster-member confirmation update with a status other than `Confirmed`
- **THEN** the API MUST reject the request without changing the roster member or registration state

#### Scenario: Registration action routes are absent
- **WHEN** a client calls `POST /v1/lan/games/{gameId}/registrations/individual`, `GET /v1/lan/games/{gameId}/registrations/eligibility/individual`, `GET /v1/lan/games/{gameId}/registrations/eligibility/teams/{teamId}`, `POST /v1/lan/games/{gameId}/registrations/eligibility/teams/{teamId}/roster`, or `POST /v1/lan/games/{gameId}/registrations/roster-confirmations/{rosterMemberId}/confirm`
- **THEN** the API MUST NOT expose those routes
