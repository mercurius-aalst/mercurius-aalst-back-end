## MODIFIED Requirements

### Requirement: Current user profile update
The API MUST expose authenticated `PATCH /v1/lan/users/me` as the canonical endpoint that updates an existing active profile for the authenticated Auth0 subject.

#### Scenario: Existing current user is updated
- **WHEN** an authenticated client requests `PATCH /v1/lan/users/me` with a valid profile payload and an active user exists for the token subject
- **THEN** the API updates the local profile fields for that user

#### Scenario: Missing current user is not created by update
- **WHEN** an authenticated client requests `PATCH /v1/lan/users/me` and no user exists for the token subject
- **THEN** the API returns 404
- **AND** no user record is created

#### Scenario: Profile-completion compatibility route is absent
- **WHEN** a client calls `POST /v1/lan/users/me/complete-profile`
- **THEN** the API MUST NOT expose that route
