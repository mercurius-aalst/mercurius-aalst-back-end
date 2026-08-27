# resource-oriented-api-routes Specification

## Purpose

Define the resource-oriented v1 route for protected tournament lifecycle transitions.

## Requirements

### Requirement: Tournament lifecycle state resource
The API MUST expose an admin-authorized `PUT /v1/lan/tournaments/{tournamentId}/lifecycle-state` endpoint. The request MUST set `state` to one of `Scheduled`, `InProgress`, `Completed`, or `Canceled`, and the API MUST apply the existing lifecycle operation for that state.

#### Scenario: Administrator starts a tournament through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/tournaments/{tournamentId}/lifecycle-state` with `state` set to `InProgress`
- **THEN** the API MUST apply the existing tournament-start rules and return the existing successful start response

#### Scenario: Administrator completes a tournament through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/tournaments/{tournamentId}/lifecycle-state` with `state` set to `Completed`
- **THEN** the API MUST apply the existing completion rules and return the existing placement response JSON shape

#### Scenario: Administrator resets or cancels a tournament through its lifecycle-state resource
- **WHEN** an administrator sends `PUT /v1/lan/tournaments/{tournamentId}/lifecycle-state` with `state` set to `Scheduled` or `Canceled`
- **THEN** the API MUST apply the existing reset or cancellation rules respectively and retain the corresponding existing successful response behavior

#### Scenario: Unsupported lifecycle state is rejected
- **WHEN** a client sends a lifecycle-state request with a value other than `Scheduled`, `InProgress`, `Completed`, or `Canceled`
- **THEN** the API MUST reject the request without changing tournament state

#### Scenario: Tournament lifecycle action routes are absent
- **WHEN** a client calls `POST /v1/lan/tournaments/{tournamentId}/start`, `POST /v1/lan/tournaments/{tournamentId}/reset`, `POST /v1/lan/tournaments/{tournamentId}/complete`, or `POST /v1/lan/tournaments/{tournamentId}/cancel`
- **THEN** the API MUST NOT expose those routes
