## ADDED Requirements

### Requirement: Game lifecycle actions use a consolidated route
The API MUST expose game lifecycle operations through `POST /v{version}/lan/games/{id}?action=<action>` using an enum-backed action value accepted as a string.

#### Scenario: Supported lifecycle action is dispatched
- **WHEN** an admin sends `POST /v1/lan/games/{id}?action=start`, `reset`, `complete`, or `cancel`
- **THEN** the API MUST dispatch to the matching game lifecycle behavior for that game id

#### Scenario: Completion keeps its existing response
- **WHEN** an admin sends `POST /v1/lan/games/{id}?action=complete`
- **THEN** the API MUST return the placement DTO response produced by completing the game

#### Scenario: Non-completion lifecycle actions acknowledge success
- **WHEN** an admin sends `POST /v1/lan/games/{id}?action=start`, `reset`, or `cancel`
- **THEN** the API MUST return a successful empty response after the matching action completes

#### Scenario: Action-specific game paths are not mapped
- **WHEN** endpoint routes are registered
- **THEN** the API MUST NOT map separate `POST` routes for `/games/{id}/start`, `/games/{id}/reset`, `/games/{id}/complete`, or `/games/{id}/cancel`

### Requirement: Current-user account actions use a consolidated route
The API MUST expose current-user Auth0 account actions through `POST /v{version}/lan/users/me?action=<action>` using an enum-backed action value accepted as a string.

#### Scenario: Resend verification email is requested
- **WHEN** an authenticated user sends `POST /v1/lan/users/me?action=resendVerificationEmail`
- **THEN** the API MUST dispatch to the current-user resend verification email behavior

#### Scenario: Password reset is requested
- **WHEN** an authenticated user sends `POST /v1/lan/users/me?action=passwordReset`
- **THEN** the API MUST dispatch to the current-user password reset email behavior

#### Scenario: Action-specific current-user account paths are not mapped
- **WHEN** endpoint routes are registered
- **THEN** the API MUST NOT map separate `POST` routes for `/users/me/resend-verification-email` or `/users/me/password-reset`

### Requirement: Current-user team invite direction uses a query parameter
The API MUST expose current-user team invites through `GET /v{version}/lan/teams/me/invites` with a `sent` query parameter that selects received or sent invite summaries.

#### Scenario: Received invites are requested
- **WHEN** an authenticated user sends `GET /v1/lan/teams/me/invites` or `GET /v1/lan/teams/me/invites?sent=false`
- **THEN** the API MUST return the current user's received pending invite summaries

#### Scenario: Sent invites are requested
- **WHEN** an authenticated user sends `GET /v1/lan/teams/me/invites?sent=true`
- **THEN** the API MUST return the current user's sent pending invite summaries

#### Scenario: Sent invite path is not mapped
- **WHEN** endpoint routes are registered
- **THEN** the API MUST NOT map a separate `GET` route for `/teams/me/sent-invites`
