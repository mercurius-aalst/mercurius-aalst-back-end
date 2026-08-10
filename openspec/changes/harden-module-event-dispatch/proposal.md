## Why

The durable module outbox can currently be selected by multiple application instances at the same time, retries failed records immediately forever, and never removes terminal records. A failing batch can therefore duplicate handler execution, consume unbounded resources, and prevent later healthy events from making progress.

## What Changes

- Add database-atomic outbox claims with explicit owner and expiring lease state, plus safe abandoned-lease recovery.
- Schedule deterministic retries with capped backoff and move messages to a terminal dead-letter state after a configurable maximum attempt count.
- Select only currently eligible messages so delayed or poisoned records do not starve later healthy events.
- Delete successfully completed and dead-lettered outbox records in bounded retention batches.
- Add validated dispatch, lease, retry, and retention options while preserving transactional publication, handler behavior, and shared inbox idempotency.
- Define the delivery contract as at-least-once and preserve all public API and integration-event payload shapes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `module-eventing`: Strengthen in-process outbox dispatch with exclusive claims, recoverable leases, scheduled bounded retries, dead-letter handling, non-starvation, and bounded terminal-record retention.

## Impact

- Platform eventing dispatcher, hosted worker, persistence entities/configuration, and dependency injection options.
- `platform.outbox_messages` columns and indexes through an additive EF Core migration and updated model snapshot.
- Focused Platform eventing tests, including multi-dispatcher behavior and migration/model validation.
- Application configuration gains a `ModuleEventing` section; routes, authorization, DTOs, JSON payloads, and module event contracts remain unchanged.
