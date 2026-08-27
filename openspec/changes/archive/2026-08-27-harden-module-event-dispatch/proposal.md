## Why

The durable module outbox loads a tracked batch and clears the entire change tracker after a handler failure. That detaches later messages in the batch, so their handlers can run without their successful `ProcessedAtUtc` transition being persisted. Failed messages are also selected again on every poll and can permanently occupy the oldest batch positions.

## What Changes

- Select a deterministic bounded list of eligible message identifiers without tracking, then load and process each message independently.
- Schedule failed messages on a small deterministic capped retry delay and move them to a terminal dead-letter state after five failed attempts.
- Exclude deferred and dead-lettered messages from selection so later healthy messages continue across batch boundaries.
- Preserve the existing single hosted worker, per-handler inbox idempotency, and at-least-once delivery contract.
- Retain terminal outbox rows and inbox markers for diagnosis; operational inspection and replay remain manual concerns.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `module-eventing`: Isolate tracked dispatch attempts and add scheduled bounded retries, dead-letter handling, and poison-message non-starvation.

## Impact

- Platform event dispatcher, hosted-worker registration, persistence entity/configuration, and focused tests.
- `platform.outbox_messages` gains nullable `next_attempt_at_utc` and `dead_lettered_at_utc` columns plus one pending-dispatch index.
- Routes, authorization, DTOs, JSON payloads, module event contracts, and application configuration remain unchanged.
