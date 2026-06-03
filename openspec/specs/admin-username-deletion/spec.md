## Purpose

Define the admin-only username-based user deletion contract and its coexistence with GUID-based user deletion routes.

## Requirements

### Requirement: Admin username delete route
The API MUST expose `DELETE /v1/lan/users/{username}` for admins and perform the existing safe username-based delete behavior.

#### Scenario: Admin deletes by username
- **WHEN** an admin deletes an existing user through `DELETE /v1/lan/users/{username}`
- **THEN** the user is anonymized or safely deleted using the existing domain behavior

#### Scenario: Existing compatibility route remains
- **WHEN** an admin deletes through `DELETE /v1/lan/users/{username}/account`
- **THEN** the route continues to perform the same safe delete behavior

#### Scenario: Missing username
- **WHEN** an admin deletes a username that does not exist
- **THEN** the API returns 404

### Requirement: Username delete authorization
The username delete route MUST be admin-only.

#### Scenario: Anonymous request rejected
- **WHEN** an anonymous client calls `DELETE /v1/lan/users/{username}`
- **THEN** the request is rejected by authorization

#### Scenario: Non-admin request rejected
- **WHEN** an authenticated non-admin calls `DELETE /v1/lan/users/{username}`
- **THEN** the request is rejected by authorization

### Requirement: User delete route precedence
The new username route MUST NOT conflict with GUID-based user routes.

#### Scenario: GUID delete route still works
- **WHEN** an admin calls `DELETE /v1/lan/users/{id}` with a GUID value
- **THEN** the GUID-based delete route handles the request

#### Scenario: Username delete route handles non-GUID usernames
- **WHEN** an admin calls `DELETE /v1/lan/users/{username}` with a non-GUID username
- **THEN** the username-based delete route handles the request
