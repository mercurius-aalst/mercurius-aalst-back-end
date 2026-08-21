## Why

Browser SignalR clients cannot currently authenticate WebSocket or Server-Sent Events connections with their JWT, and an already-authorized connection can remain subscribed to a team after the underlying membership, team, or account access is revoked. The realtime boundary needs to authenticate the intended hub transport and remove stale subscriptions promptly without changing existing routes or client contracts.

## What Changes

- Accept one nonblank `access_token` query value only on the exact authenticated team-management hub route while preserving normal bearer-header processing.
- Close authenticated hub connections when their JWT authentication expires.
- Track active user connections and their realtime groups in the API process so multiple connections can be removed after committed access changes.
- Serialize team subscription authorization with revocation and revalidate the active user and current membership immediately before joining the group.
- Remove affected subscriptions only after successful member removal, team leave, team deletion, or account deletion commits.
- Preserve the existing hub route, group names, client method names, payloads, REST routes, and JSON contracts.
- Explicitly keep distributed connection tracking and scale-out SignalR revocation outside this change.

## Capabilities

### New Capabilities

- `team-realtime-access`: Defines browser JWT authentication, authentication-expiration closure, serialized team subscription authorization, and post-commit single-process subscription revocation.

### Modified Capabilities

None.

## Impact

- Affects Platform JWT and SignalR registration, the API team-management hub and mapping, and narrowly scoped Teams and Identity post-commit integration points.
- Adds process-local connection state only; no database schema, durable event payload, package, backplane, route, DTO, or JSON change is required.
- Adds focused authentication, mapping, hub lifecycle, concurrency, and post-commit regression tests.
