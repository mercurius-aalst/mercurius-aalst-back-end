## Context

Platform eventing stores durable integration events in PostgreSQL and dispatches them in-process through scoped handlers. The dispatcher currently reads pending rows without claiming them, retries every failure on the next poll, has no terminal failure state, and retains every outbox row indefinitely. Shared inbox markers already make completed database consumers idempotent and MUST remain intact.

The application can run multiple instances, so an in-memory mutex cannot provide production ownership. Handler execution is intentionally outside the business transaction that originally created the outbox row; the resulting contract is at-least-once delivery, not exactly-once delivery.

## Goals / Non-Goals

**Goals:**

- Give one dispatcher exclusive, database-visible ownership before it enters a message handler.
- Recover work after an owning process stops while preventing a normally running handler from losing its lease.
- Bound failure retries with deterministic capped backoff and an explicit dead-letter state.
- Keep later eligible messages moving when older messages are delayed or poisoned.
- Remove old successful and dead-lettered outbox rows in bounded batches.
- Preserve transactional publication, shared inbox idempotency, registered event types, handler ordering, and public API/event payload shapes.

**Non-Goals:**

- Claim exactly-once delivery or make arbitrary external handler side effects transactional.
- Add a message broker, administrative replay endpoint, or dead-letter management UI.
- Delete shared inbox markers or change consumer names.
- Change module event contracts, routes, authorization, DTOs, or JSON payloads.

## Decisions

- **Claim one message at a time with a PostgreSQL atomic update.** A single common-table-expression statement selects the oldest eligible row with `FOR UPDATE SKIP LOCKED`, updates its owner and lease expiration, and returns its id. Claiming and ownership publication therefore commit before handler entry without a select/update race. One-at-a-time claiming avoids a batch waiting behind a slow earlier handler until its lease expires. EF's non-relational test provider uses a process-local serialized fallback only for focused unit tests; PostgreSQL remains the production concurrency authority. Alternatives considered: an application mutex does not coordinate instances, and a long database transaction around an entire batch would retain locks while user code runs.

- **Use renewable leases and owner-conditioned state transitions.** A claim stores a random owner id and expiration. While a handler is running, a heartbeat from an independent dependency-injection scope extends the lease before it expires. Completion, retry, dead-letter, and cancellation release updates all require the current owner id. A dispatcher that loses ownership cancels the handler token and cannot finalize the row. Expired leases are eligible for a new atomic claim, which recovers abandoned work. Alternatives considered: a fixed lease alone can overlap a handler that runs longer than the lease, while holding a row lock throughout arbitrary handler code creates long-running transactions and couples every handler to one database transaction.

- **Schedule deterministic bounded retries.** `retry_count` remains the number of failed attempts. Failure `n` schedules `baseDelay * 2^(n-1)`, capped at the configured maximum delay. When `n` reaches the configured maximum attempt count, the dispatcher records `dead_lettered_at_utc`, keeps the truncated last error, clears claim fields, and never selects the row automatically again. There is no jitter so timing is predictable and testable. Selection excludes future retry times and terminal rows, allowing later eligible messages to pass delayed or dead-lettered records.

- **Treat caller cancellation as control flow, not a delivery failure.** Cancellation propagates to handlers and database operations. If the dispatcher still owns the row, it releases the lease without incrementing the retry count, then rethrows cancellation. A later dispatcher can resume the at-least-once attempt. Ownership cleanup uses a non-cancelled token after the caller token has fired because it is best-effort lease release during shutdown.

- **Run bounded retention from the existing worker.** The worker periodically deletes at most the configured cleanup batch size of successful rows older than the success retention period and dead-lettered rows older than the dead-letter retention period, ordered deterministically by terminal timestamp and id. It deletes inbox markers only for those same terminal message ids and in the same transaction before removing the outbox rows; markers for pending or retained outbox work are never age-deleted independently. Partial PostgreSQL indexes support eligible claims, matching inbox cleanup, and each terminal cleanup path.

- **Bind and validate one `ModuleEventing` options section.** Batch size, polling interval, lease duration, maximum attempts, retry delays, retention periods, cleanup interval, and cleanup batch size have conservative defaults and startup validation, including cross-field delay and heartbeat constraints. A `TimeProvider` dependency supplies timestamps and delays to keep scheduling testable.

## Risks / Trade-offs

- **[A process is paused longer than an entire renewed lease]** → Heartbeats renew well before expiration and ownership checks cancel a stale dispatcher and prevent it from committing a terminal transition. Arbitrary non-transactional external side effects still remain at-least-once and handlers MUST be idempotent where required.
- **[A poison message consumes repeated attempts]** → Each failure moves its next eligible time forward; maximum attempts end in dead-letter state, and the dispatcher continues claiming other eligible rows in the same batch loop.
- **[Atomic claim SQL is PostgreSQL-specific]** → Keep the SQL isolated in Platform eventing, validate the required `SKIP LOCKED`/owner predicates in tests, and use the existing EF provider fallback only outside production.
- **[Retention removes diagnostic payloads and idempotency markers]** → Use separate configurable success and dead-letter retention periods, retain last error and inbox markers for the full outbox retention window, and delete matching markers only when their terminal outbox row is deleted in the same transaction.
- **[Extra heartbeat writes increase database load]** → Claim only one active message per scoped dispatcher and renew at a fraction of the lease duration; handlers that finish quickly stop the heartbeat before its first write.

## Migration Plan

1. Add nullable lease owner, lease expiration, next-attempt, and dead-letter timestamp columns so existing pending rows remain immediately eligible without a destructive backfill.
2. Replace the old pending index with partial claim and terminal-retention indexes, and add an inbox message-id index for associated cleanup.
3. Deploy the migration before or with application instances using the new dispatcher. Mixed-version deployment is not supported because the new dispatcher requires the new columns.
4. Rollback removes the new indexes and columns and restores the former pending index; dead-letter scheduling metadata is lost on rollback, but event payloads and inbox markers are unaffected.

## Open Questions

None. Operational dead-letter inspection and manual replay can be added as a separate explicitly specified change if needed.
