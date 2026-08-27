## Context

`TeamService` currently calls invite expiry without a user/team filter from three authenticated read methods, and the summary read also deletes every terminal invite older than the retention cutoff. `TeamEventPublishingDecorator` surrounds those reads by preloading every due invite and then publishing one SignalR event per persisted transition. The projection query already applies `Status == Pending` and `ExpiresAt > now`, so request-time writes are not needed to keep actionable results fresh.

The production database is PostgreSQL, the Teams module owns the invite entity and configuration, and the module already registers hosted services through dependency injection. Invite expiry events are privacy-safe realtime notifications rather than durable integration events.

## Goals / Non-Goals

**Goals:**

- Make current-user team and invite reads side-effect free and scoped entirely to the current recipient or captain.
- Move persisted expiry and retention cleanup to a deterministic scheduled workflow with hard per-run limits.
- Ensure concurrent application instances do not process the same expiry transition and do not duplicate its realtime event during normal operation.
- Keep maintenance queries indexed, cancellation-aware, and efficient.
- Preserve routes, JSON shapes, invite lifetime rules, and immediate exclusion of due invites from actionable projections.

**Non-Goals:**

- Introduce a general-purpose job framework or a new public maintenance endpoint.
- Make transient SignalR delivery durable across a process crash.
- Change invite expiration, cooldown, or retention durations.

## Decisions

### Use one scoped maintenance service behind a hosted worker

The Teams module will register a small `BackgroundService` that creates a dependency-injection scope and invokes one maintenance batch, then waits for the configured interval. Each cycle processes at most the configured batch size of due pending invites and at most the configured batch size of terminal retention candidates. It will not loop until the backlog is empty, so a cycle has a hard database-write and event-fan-out bound.

This keeps scheduling separate from the scoped EF Core context without introducing a generic job abstraction. Running maintenance from reads was rejected because it couples user latency and authorization-scoped traffic to global work. Draining the whole backlog in one hosted-service cycle was rejected because it recreates the unbounded load outside the request.

### Keep reads fresh through timestamp predicates

Current-user projections will continue to require `Status == Pending` and `ExpiresAt > now`, with the recipient or captain predicate composed into the same server-side query and every async operation receiving the caller's cancellation token. The read methods and their event decorator will perform no expiry update, cleanup delete, or expiry-event scan.

The invite-creation path will retain its existing team-and-user-scoped expiry transition because it must release the unique pending-invite constraint before creating a replacement. Its event scan remains bounded to that same team/user pair.

### Serialize PostgreSQL maintenance ownership and make transitions idempotent

A maintenance batch will open a transaction and acquire a PostgreSQL transaction-scoped advisory lock using a stable Teams-owned key. If another instance owns the lock, the cycle exits without work. The batch deterministically selects due invites by `ExpiresAt`, then `Id`, transitions only pending rows, persists them, and commits before publishing their events. Re-running the service cannot transition or republish those rows because they are no longer pending.

For non-PostgreSQL relational test/development providers, a serializable transaction provides the closest provider-neutral fallback; the in-memory provider runs without a transaction. A general distributed-lock dependency was rejected as unnecessary for a single PostgreSQL-backed application.

### Bound realtime publication concurrency

Only invites transitioned by the committed batch are event candidates. Their expiry events are dispatched with a configured maximum degree of parallelism no larger than the maintenance batch, and cancellation is propagated. A successful batch therefore attempts exactly one event per transitioned invite and never more than the batch size; a subsequent cycle does not repeat those events.

### Delete terminal records by bounded identifiers

Retention candidates are loaded through three fixed status-specific server-side queries for responded, cancelled, and expired timestamps. Each query is indexed, deterministically ordered, and limited to the configured batch size; at most three bounded lists are merged in memory to select the globally oldest batch. Relational providers delete the selected identifiers with `ExecuteDeleteAsync`; the in-memory test provider uses a tracked bounded fallback.

Partial indexes will support pending expiry and the three terminal timestamp query families. The migration will backfill a missing status-specific terminal timestamp from `CreatedAt` before those indexed predicates become authoritative. Existing recipient/team invite indexes remain for authenticated projections and scoped invite creation.

## Risks / Trade-offs

- [A large existing backlog takes multiple intervals to drain] → The configurable batch size and interval allow controlled catch-up without making any cycle unbounded.
- [A process can stop after committing expiry but before sending SignalR] → Actionable reads still derive freshness from `ExpiresAt`; clients refresh authoritative state on reconnect. Durable realtime delivery is explicitly outside this change.
- [PostgreSQL advisory-lock keys must remain stable] → Keep one named constant in the maintenance service and cover repeated-run idempotence in tests.
- [Configuration could create excessive load] → Validate interval, batch size, retention, and event-concurrency ranges during startup.

## Migration Plan

1. Deploy the migration that backfills missing terminal timestamps and adds partial maintenance indexes.
2. Deploy the hosted worker and configuration values with conservative defaults.
3. Monitor maintenance errors, batch duration, and invite backlog while the bounded cycles catch up.
4. Roll back by disabling the worker code and removing the added indexes; public API data remains compatible because no columns or JSON contracts change.

## Open Questions

None.
