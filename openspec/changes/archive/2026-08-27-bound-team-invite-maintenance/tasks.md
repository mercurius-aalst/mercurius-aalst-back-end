## 1. Request-path isolation

- [x] 1.1 Remove global expiry, cleanup, and event-candidate work from authenticated team and invite reads while retaining timestamp freshness filters and cancellation propagation.
- [x] 1.2 Restrict request-time invite expiry helpers and event scans to the invite creation team's recipient pair.

## 2. Bounded maintenance

- [x] 2.1 Add validated maintenance configuration, a scoped deterministic batch processor, PostgreSQL cross-instance ownership, bounded event concurrency, and efficient terminal deletes.
- [x] 2.2 Register the Teams hosted maintenance worker and production defaults.

## 3. Persistence

- [x] 3.1 Add pending-expiry and terminal-retention indexes for the maintenance query shapes.
- [x] 3.2 Generate and verify the EF Core migration and model snapshot changes.

## 4. Verification

- [x] 4.1 Add regression tests proving current-user reads do not mutate or publish events for related or unrelated due/terminal invites.
- [x] 4.2 Add tests for deterministic batch limits, event limits and rerun idempotence, cancellation, hosted-service/options registration, and maintenance indexes/migration.
- [x] 4.3 Run focused Teams tests, build, and formatting validation; reconcile the OpenSpec checklist with the verified implementation.
