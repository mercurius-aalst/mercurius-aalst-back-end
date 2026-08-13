## Context

Platform eventing stores durable integration events and dispatches them through one in-process hosted worker. Each completed handler writes an inbox marker, allowing later at-least-once attempts to skip that consumer.

The former dispatcher loaded a tracked batch. When one message failed, it cleared the shared change tracker before reloading that message to persist failure state. Every later message was therefore detached: its handlers could complete while its `ProcessedAtUtc` update was never saved. Persistent poison rows were selected on every poll and could repeatedly fill the oldest batch positions.

There is no repository evidence that this API is deployed as multiple concurrent worker instances. Distributed ownership and terminal-record retention are outside this correction.

## Goals / Non-Goals

**Goals:**

- Isolate each tracked message attempt so failure recovery cannot detach later candidates.
- Delay retries deterministically, stop automatic attempts after five failures, and keep later eligible work moving.
- Preserve the single hosted worker, handler ordering, inbox idempotency, and at-least-once delivery.
- Keep the implementation and schema small and provider-independent.

**Non-Goals:**

- Coordinate multiple application instances or claim exclusive distributed ownership.
- Provide exactly-once delivery or transactional external side effects.
- Delete terminal outbox rows or inbox markers.
- Add dead-letter inspection, replay APIs, a management UI, or configurable retry policies.
- Change public routes, authorization, DTOs, JSON, or event payload contracts.

## Decisions

- **Select identifiers, then process one row at a time.** The dispatcher selects at most the requested batch size of eligible IDs with `AsNoTracking`, ordered by occurrence time and ID. It then loads each row separately. Clearing the tracker after one failed attempt cannot detach unprocessed candidate IDs because those IDs are ordinary values, not tracked entities.

- **Use a small internal retry policy.** Failed attempt `n` records `RetryCount`, `LastAttemptAtUtc`, and truncated `LastError`. Attempts below five schedule `NextAttemptAtUtc` with deterministic exponential delay from five seconds, capped at five minutes. The fifth failure sets `DeadLetteredAtUtc` and clears `NextAttemptAtUtc`. These operational constants are not public configuration.

- **Filter before taking a batch.** Selection requires `ProcessedAtUtc` and `DeadLetteredAtUtc` to be null and `NextAttemptAtUtc` to be null or due. Deferred poison rows are absent from the next bounded candidate list, allowing healthy rows beyond the previous batch boundary to progress.

- **Keep time deterministic without expanding configuration.** `TimeProvider` supplies dispatcher timestamps for focused retry tests. Application composition registers the system provider; the hosted worker keeps its fixed two-second idle delay and existing default batch size.

- **Retain diagnostic state.** Successful, failed, and dead-lettered outbox rows and all inbox markers remain stored. Retention and replay require separate operational design.

## Risks / Trade-offs

- **[The application is deployed with multiple active workers]** → This design does not coordinate them. Delivery remains at least once, and inbox or handler idempotency remains required. Distributed dispatch MUST be specified separately if deployment topology changes.
- **[A process stops after a handler side effect but before completion is saved]** → A later attempt MAY repeat the handler; inbox markers protect completed database consumers, while external effects require handler-level idempotency.
- **[A row reaches five failures]** → It remains stored and is no longer selected automatically. Operations must inspect and replay or repair it manually.

## Migration Plan

1. Add nullable `next_attempt_at_utc` and `dead_lettered_at_utc` columns so existing pending rows remain immediately eligible.
2. Replace the former pending index with one partial pending-dispatch index covering due-time filtering and deterministic order.
3. Rollback removes the new index and columns and restores the prior pending index. Existing payloads and inbox markers are unchanged.

## Open Questions

None. Multi-instance coordination, automated retention, and operational dead-letter replay require separate evidence and specifications.
