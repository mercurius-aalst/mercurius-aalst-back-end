## Why

The API-wide HTTP rate limiter protects SignalR negotiation and transport setup but does not run for hub invocations received over an established WebSocket. An authenticated caller can repeatedly invoke the team subscription methods, causing repeated authorization queries and process-local realtime group work.

## What Changes

- Add a process-local fixed-window limit for the authenticated caller's `JoinTeam` and `LeaveTeam` invocations on the team-management hub.
- Reject an invocation that exceeds the limit before it reaches the hub method, using a stable SignalR `HubException` retry message.
- Preserve the hub route, authorization, group names, payloads, client methods, connection lifecycle, and post-commit revocation behavior.

## Capabilities

### New Capabilities

- `team-realtime-invocation-throttling`: Limits expensive client-invoked team subscription changes on established SignalR connections.

### Modified Capabilities

None.

## Impact

- Affects the API host's SignalR registration, one API-local hub filter, and focused API tests.
- Adds no route, DTO, JSON, persistence, configuration, package, distributed-rate-limit, or SignalR-backplane change.
- The limit is process-local and applies independently in each API process.
