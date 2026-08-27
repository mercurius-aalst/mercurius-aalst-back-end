## Context

The API maps one authenticated SignalR hub at `/v1/lan/team-events`. Browser WebSocket and Server-Sent Events clients provide bearer tokens through the SignalR `access_token` query convention, but the JWT bearer handler does not currently read that value. The hub authorizes a team join against current database state and then adds the connection to the stable `team:{id:N}` group, but it does not track active connections or remove subscriptions after access changes.

Teams and Identity already commit their state changes and durable integration events transactionally. Teams then publishes existing best-effort realtime payloads after the commit. The repository contains one API Dockerfile and local launch profiles, with no replica, backplane, distributed connection registry, or scale-out deployment configuration.

## Goals / Non-Goals

**Goals:**

- Authenticate browser SignalR transports only at the canonical team hub path without replacing header authentication or exposing token values.
- Close hub connections at authentication expiration.
- Track multiple process-local connections per user and their personal/team group memberships.
- Ensure a join cannot survive a concurrent membership, team, or account revocation.
- Revoke subscriptions only after the matching database transaction commits.
- Preserve current realtime and REST contracts.

**Non-Goals:**

- Distributed connection state, a Redis or Azure SignalR backplane, leases, or cross-instance revocation.
- Durable delivery or outbox handlers for process-local group removal.
- Changing routes, group names, client method names, payloads, DTOs, JSON, or persistence schema.

## Decisions

### Use one canonical hub route for mapping and JWT query-token handling

`TeamManagementHub.Route` will be the single route constant. The JWT bearer message-received callback will inspect `access_token` only when the request path equals that value exactly, the query contains exactly one nonblank value, no prior event token exists, and no `Authorization` header exists. It will assign the original value without trimming, decoding, or logging it. All other requests retain the framework's normal bearer-header flow.

The hub mapping will set `HttpConnectionDispatcherOptions.CloseOnAuthenticationExpiration` to `true`; this option belongs to the SignalR connection dispatcher rather than `JwtBearerOptions`.

### Use one process-local connection manager

Platform will expose one narrow `IRealtimeConnectionManager` contract and register one singleton generic SignalR implementation for the mapped hub. The implementation will own:

- a user-to-connection index;
- connection-to-user-and-group state;
- registration, group tracking, leave, and disconnect cleanup;
- removal of one user's connections from one group, all connections from one group, or all groups for one user;
- one async gate shared by authorization/add and revocation.

The API hub supplies the existing group names. Modules pass primitive user ids and group names through the Platform infrastructure boundary and do not depend on hub implementation details.

A single gate is intentionally used instead of a keyed-lock abstraction. Team subscription changes are short and infrequent, and the global gate avoids leaked per-key semaphores and lock-order complexity. Database identity and membership reads plus the matching SignalR group add are performed inside the gate; unrelated request work is not.

### Revalidate immediately before joining

`JoinTeam` will resolve the active local user and query current team authorization inside the connection manager gate immediately before adding and tracking the team group. Connection setup, explicit leave, disconnect cleanup, and revocation use the same gate.

If Join obtains the gate first, a later committed revocation waits and then removes the new subscription. If revocation obtains the gate first, Join's in-gate database reads observe the committed loss of access and reject the add. The same ordering covers team deletion and account deletion.

### Invoke revocation directly after commit

Teams will call the connection manager only after its transactional mutation/outbox helper has returned successfully. Member removal and leave remove all affected user's live connections from that team group before the existing membership broadcast. Team deletion removes every tracked live connection from that team group.

Identity will revoke all personal and team groups for the affected user only after a first successful active-to-deleted transition commits. Already-deleted no-op paths do not revoke again. Post-commit calls use `CancellationToken.None` so an aborted HTTP request cannot skip local cleanup.

No new outbox consumers will be added. Persisting process-local group-removal work would introduce dispatch delay but could not restore connections after restart; restart already terminates every active connection and clears its SignalR group state.

### Surface unexpected post-commit revocation failures

The default single-process SignalR lifetime manager performs group removal locally and has no external transport dependency. If an unexpected removal exception nevertheless occurs, it will propagate to the caller after the database transaction has committed. The mutation is not rolled back or reported as rolled back. Tests will prove this ordering. Operators can recycle the process to force all active connections closed; token expiration is the other bounded cleanup path.

## Risks / Trade-offs

- **[A deployment runs multiple API instances]** → Each instance sees only its own connections, so this design does not provide cross-instance revocation. Deployment must remain single-process or add an explicit shared SignalR/backplane design before scaling out.
- **[A post-commit local removal unexpectedly fails]** → The mutation remains committed and the exception is surfaced. Recycle the process to disconnect all clients; the implementation does not claim transactional rollback or durable retry.
- **[The process restarts]** → All SignalR connections and in-memory indexes are lost together. Clients reconnect and must pass current authentication and team authorization again.
- **[The global gate adds contention]** → Only short connection lifecycle, authorization, and revocation work uses it. Focused concurrency tests guard correctness; scale-out or high-volume evidence would justify revisiting granularity later.

## Migration Plan

Deploy the API change without a database migration. Existing clients retain the same route and payload contracts; browser clients using SignalR's access-token factory can authenticate transports at the existing hub route. Rollback removes process-local tracking and query-token support without data rollback.

## Open Questions

None. A future multi-instance deployment requires a separate design based on concrete hosting and backplane evidence.
