## 1. Persisted Claim State

- [x] 1.1 Add claim token and lease expiry properties to the outbox persistence model and EF configuration, including an eligibility-supporting index.
- [x] 1.2 Add an additive migration and synchronize the EF Core model snapshot for the claim columns and index.

## 2. Atomic Dispatch Ownership

- [x] 2.1 Atomically claim eligible messages with a unique token before loading payloads or invoking handlers.
- [x] 2.2 Clear claims on successful completion and recorded failure, and prevent an expired owner from writing terminal state after ownership changes.
- [x] 2.3 Preserve deterministic batching, retry/dead-letter behavior, cancellation propagation, and automatic recovery after lease expiry.

## 3. Regression Coverage

- [x] 3.1 Add a concurrency regression test proving overlapping dispatchers invoke handlers only once for one active claim.
- [x] 3.2 Add focused tests for claim release, expired-lease recovery, and stale-owner terminal-write protection.

## 4. Validation

- [x] 4.1 Run strict OpenSpec validation for the change and all repository specs.
- [x] 4.2 Run the focused Platform eventing tests, then repository restore, build, full tests, and formatting verification.
