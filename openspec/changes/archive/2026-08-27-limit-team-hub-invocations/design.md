## Context

The global HTTP limiter uses `HttpContext` middleware and therefore limits negotiation and transport setup, but not messages on an established SignalR WebSocket. The team-management hub has two public client-invoked subscription methods. `JoinTeam` performs local-user and team-membership reads before changing group state; both methods use the process-local realtime access gate.

## Goals / Non-Goals

**Goals:**

- Reject excessive authenticated `JoinTeam` and `LeaveTeam` calls before hub work begins.
- Share one fixed-window budget across all connections belonging to the same authenticated subject.
- Preserve existing authorization, access-gate ordering, group names, route, payloads, lifecycle callbacks, and post-commit revocation.

**Non-Goals:**

- Limiting unrelated or future hub methods, hub lifetime callbacks, or direct revocation calls.
- Configurable policies, distributed rate limits, a SignalR backplane, persistence, or changes to the HTTP limiter.
- Emitting an HTTP response for an already-established SignalR connection.

## Decisions

### Use a TeamManagementHub-local singleton hub filter

Register one `IHubFilter` only through `AddHubOptions<TeamManagementHub>`. The filter owns the small amount of shared process-local limiter state and is resolved as a singleton. This confines the policy to the hub that needs it without changing Platform's generic realtime registration.

Alternative considered: calling a limiter from each hub method. Rejected because a filter guarantees acquisition before every covered method body and avoids duplicating enforcement.

### Limit only the two current subscription methods with one shared subject partition

The filter recognizes `JoinTeam` and `LeaveTeam` only and uses one 20-per-60-second fixed-window, zero-queue partition keyed by the authenticated `sub` claim, with the hub's existing name-identifier fallback. The same subject shares the budget across all of its connections, so opening extra connections does not multiply the permitted subscription work.

Alternative considered: all client methods. Rejected because the finding is specific to subscription authorization and group work; extending the behavior to unrelated future methods would be an unrequested contract change. A per-connection partition was also rejected because it is bypassed by opening more connections.

### Use HubException rather than HTTP 429 for a rejected message

On an unavailable limiter lease, the filter does not call the next delegate and throws `HubException` with a stable retry message that includes the ceiling of the lease's retry-after duration. An established WebSocket cannot receive a new HTTP 429 response; SignalR propagates `HubException` as a failed invocation while keeping the connection open.

### Leave lifecycle and revocation paths outside the filter

The filter implements only invocation handling. It does not wrap `OnConnectedAsync` or `OnDisconnectedAsync`, and it does not change the realtime connection manager. Existing join/revoke serialization therefore remains the sole ordering control for group access and cleanup.

## Risks / Trade-offs

- [A deployment uses multiple API processes] → Each process has an independent subject window. The existing deployment scope is process-local; add a separate distributed design before claiming cross-instance protection.
- [Subject partitions accumulate for the process lifetime] → This matches the existing HTTP partitioned limiter. The service does not add a cleanup framework without evidence that the bounded process-local cost needs it.
- [A client is temporarily rejected during rapid navigation or reconnects] → Twenty combined calls per minute is deliberately above normal subscription activity; clients can use the supplied retry delay.

## Migration Plan

Deploy with no database migration or configuration change. Monitor client invocation failures for the stable limit message. Roll back by removing the local hub-filter registration and class; existing connections and persisted data need no migration.

## Open Questions

None.
