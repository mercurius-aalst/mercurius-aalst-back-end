# team-realtime-invocation-throttling Specification

## Purpose
TBD - created by archiving change limit-team-hub-invocations. Update Purpose after archive.
## Requirements
### Requirement: Authenticated team subscription invocations are process-locally throttled
The API MUST apply one process-local fixed window of twenty invocations per sixty seconds, with no queue, to the combined `JoinTeam` and `LeaveTeam` calls from each authenticated team-management-hub subject. The API MUST acquire a permit before executing either hub method body, and all connections for the same subject MUST share the same window.

#### Scenario: Shared subject exhausts subscription window across connections
- **WHEN** one authenticated subject invokes `JoinTeam` and `LeaveTeam` across one or more hub connections more than twenty times in sixty seconds
- **THEN** the API MUST execute only the first twenty covered invocations in that window and MUST reject each subsequent covered invocation before hub-method work begins

#### Scenario: Different authenticated subjects have independent windows
- **WHEN** separate authenticated subjects invoke covered team subscription methods
- **THEN** each subject MUST receive an independent process-local invocation window

### Requirement: SignalR throttling rejection preserves realtime access behavior
The API MUST reject a covered invocation without a permit by throwing a `HubException` with a stable retry message containing the ceiling of the available retry-after duration. The rejection MUST keep the existing SignalR connection open and MUST NOT change the hub route, authorization requirement, group names, client method names, payload shapes, connection lifecycle callbacks, or post-commit revocation behavior.

#### Scenario: Covered invocation is rejected
- **WHEN** an authenticated subject invokes `JoinTeam` or `LeaveTeam` after exhausting its window
- **THEN** the caller MUST receive the stable SignalR invocation error and the API MUST NOT enter the hub method, authorization query, access gate, or group operation for that invocation

#### Scenario: Lifecycle and revocation work continues unthrottled
- **WHEN** a team-management hub connection is created, disconnected, or revoked after the caller has exhausted its subscription window
- **THEN** the API MUST execute the existing lifecycle and revocation behavior without consuming or requiring a subscription-invocation permit
