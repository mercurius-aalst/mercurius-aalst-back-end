## 1. Eventing Lifecycle Model And Configuration

- [x] 1.1 Add explicit next-attempt, lease-owner, lease-expiration, and dead-letter state to the outbox entity and EF configuration.
- [x] 1.2 Add validated `ModuleEventing` options and bind them in application composition with a configurable `TimeProvider`.
- [x] 1.3 Add an additive EF migration and model snapshot updates with claim and terminal-retention indexes.

## 2. Exclusive Dispatch And Failure Lifecycle

- [x] 2.1 Implement PostgreSQL atomic one-message claims with deterministic eligibility/order and a safe non-relational test-provider fallback.
- [x] 2.2 Renew active leases from an independent scope, prevent stale-owner transitions, recover expired claims, and release owned claims on cancellation.
- [x] 2.3 Implement deterministic capped retry scheduling, maximum-attempt dead-letter transitions, and continued eligible-message processing across poison batches.

## 3. Retention And Hosted Processing

- [x] 3.1 Implement deterministic bounded cleanup for expired successful and dead-lettered outbox records while deleting only their associated inbox markers atomically.
- [x] 3.2 Update the hosted worker to use configured polling, dispatch batch, cleanup interval, and cancellation behavior.

## 4. Focused Verification

- [x] 4.1 Add focused tests for atomic/concurrent claiming, lease renewal beyond the initial lease, abandoned lease recovery, and ownership-conditioned transitions.
- [x] 4.2 Add focused tests for backoff timing and capping, max-attempt dead-letter behavior, cross-batch poison non-starvation, and cancellation release.
- [x] 4.3 Add focused tests for bounded retention, associated inbox cleanup, DI/options validation, PostgreSQL claim SQL, and migration/runtime model indexes.
- [x] 4.4 Run focused tests, solution restore/build/test/format validation, strict OpenSpec validation, and verify implementation coherence against the change artifacts.
