## Why

Multiple API instances can currently select and dispatch the same eligible outbox message concurrently because selection does not reserve ownership before handlers run. This can duplicate handler side effects even though completed deliveries are recorded in the inbox.

## What Changes

- Atomically claim each eligible outbox message before invoking handlers.
- Attach an expiring lease to each claim so interrupted dispatches become eligible again without manual recovery.
- Restrict completion and failure updates to the dispatcher that owns the active claim.
- Add migration, model, and concurrency regression coverage for overlapping dispatchers.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `module-eventing`: Require exclusive, recoverable ownership while an outbox message is being dispatched.

## Impact

- Platform outbox persistence and dispatch logic.
- The shared EF Core model, model snapshot, and an additive database migration.
- Platform eventing reliability tests.
- No public HTTP route, authorization rule, request/response JSON shape, or event payload shape changes.
