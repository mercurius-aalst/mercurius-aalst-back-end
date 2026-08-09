## ADDED Requirements

### Requirement: Game lifecycle state resource
The API MUST expose an admin-authorized `PUT /v1/lan/games/{gameId}/lifecycle-state` endpoint. The request MUST set `state` to one of `Scheduled`, `InProgress`, `Completed`, or `Canceled`, and the API MUST apply the existing lifecycle operation for that state.

#### Scenario: Administrator starts a game through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/games/{gameId}/lifecycle-state` with `state` set to `InProgress`
- **THEN** the API MUST apply the existing game-start rules and return the existing successful start response

#### Scenario: Administrator completes a game through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/games/{gameId}/lifecycle-state` with `state` set to `Completed`
- **THEN** the API MUST apply the existing completion rules and return the existing placement response JSON shape

#### Scenario: Administrator resets or cancels a game through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/games/{gameId}/lifecycle-state` with `state` set to `Scheduled` or `Canceled`
- **THEN** the API MUST apply the existing reset or cancellation rules respectively and retain the corresponding existing successful response behavior

#### Scenario: Unsupported lifecycle state is rejected
- **WHEN** a client sends a lifecycle-state request with a value other than `Scheduled`, `InProgress`, `Completed`, or `Canceled`
- **THEN** the API MUST reject the request without changing game state

#### Scenario: Game lifecycle action routes are absent
- **WHEN** a client calls `POST /v1/lan/games/{gameId}/start`, `POST /v1/lan/games/{gameId}/reset`, `POST /v1/lan/games/{gameId}/complete`, or `POST /v1/lan/games/{gameId}/cancel`
- **THEN** the API MUST NOT expose those routes
