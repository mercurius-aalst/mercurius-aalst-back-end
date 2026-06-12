## ADDED Requirements

### Requirement: Current user profile read
The API MUST expose an authenticated `GET /v1/lan/users/me` endpoint that reads the active profile for the authenticated Auth0 subject without creating or updating database records.

#### Scenario: Existing current user returned
- **WHEN** an authenticated client requests `GET /v1/lan/users/me` and an active user exists for the token subject
- **THEN** the API returns the current profile response for that user

#### Scenario: Missing current user is not created
- **WHEN** an authenticated client requests `GET /v1/lan/users/me` and no user exists for the token subject
- **THEN** the API returns 404
- **AND** no user record is created

#### Scenario: Current user read does not refresh snapshots
- **WHEN** an authenticated client requests `GET /v1/lan/users/me` for an existing active user
- **THEN** the API MUST NOT update stored Auth0 email or verification snapshots as part of the read

### Requirement: Current user profile creation
The API MUST expose an authenticated `PUT /v1/lan/users/me` endpoint that creates the profile for the authenticated Auth0 subject when no user record exists.

#### Scenario: Missing current user is created
- **WHEN** an authenticated client requests `PUT /v1/lan/users/me` with a valid profile payload and no user exists for the token subject
- **THEN** the API creates a user linked to that Auth0 subject
- **AND** the API returns the created user profile

#### Scenario: Existing current user is not recreated
- **WHEN** an authenticated client requests `PUT /v1/lan/users/me` and a user already exists for the token subject
- **THEN** the API rejects the request
- **AND** the existing user profile is not overwritten

### Requirement: Current user profile update
The API MUST expose authenticated current-user profile update endpoints that update an existing active profile for the authenticated Auth0 subject.

#### Scenario: Existing current user is updated
- **WHEN** an authenticated client requests `PATCH /v1/lan/users/me` with a valid profile payload and an active user exists for the token subject
- **THEN** the API updates the local profile fields for that user

#### Scenario: Missing current user is not created by update
- **WHEN** an authenticated client requests `PATCH /v1/lan/users/me` and no user exists for the token subject
- **THEN** the API returns 404
- **AND** no user record is created

#### Scenario: Existing current user is completed through compatibility route
- **WHEN** an authenticated client requests `POST /v1/lan/users/me/complete-profile` with a valid profile payload and an active user exists for the token subject
- **THEN** the API updates the local profile fields for that user
- **AND** the API MUST NOT try to create a new user

#### Scenario: Missing current user is not created by compatibility route
- **WHEN** an authenticated client requests `POST /v1/lan/users/me/complete-profile` and no user exists for the token subject
- **THEN** the API returns 404
- **AND** no user record is created
