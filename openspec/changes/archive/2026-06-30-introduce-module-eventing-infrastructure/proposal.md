## Why

Modules need reliable synchronization before later phases duplicate data into Identity, Teams, Competition, and Discovery projections. SignalR realtime notifications are already separated from module synchronization, but the application still has no durable outbox, inbox, retry, or versioned integration event path.

## What Changes

- Add Platform-owned durable module eventing infrastructure with an outbox, shared inbox/idempotency table, dispatcher, retry state, and handler resolution.
- Add module-owned durable event payload records without requiring module contract assemblies to reference Platform.
- Add versioned Teams integration events and publish Teams lifecycle facts transactionally with the same database commit as the Teams mutation.
- Keep existing public API routes, authorization, DTO/JSON shapes, SignalR group names, SignalR client method names, and realtime payload timing unchanged.
- Keep invite-status and roster-confirmation notifications realtime-only in this phase unless a future OpenSpec change explicitly expands durable event scope.

## Capabilities

### New Capabilities

- `module-eventing`: Durable in-process module integration event publishing, outbox dispatch, inbox idempotency, retries, and versioned event handling.

### Modified Capabilities

- None.

## Impact

- Adds Platform eventing contracts and infrastructure.
- Adds `platform.outbox_messages` and shared `platform.inbox_messages` persistence through the current `MercuriusDBContext`.
- Adds Teams integration event payload records and a persisted Teams version source.
- Updates Teams write paths to enqueue durable integration events while preserving existing realtime behavior.
- Adds eventing, migration/model, Teams publication, and reliability tests.
