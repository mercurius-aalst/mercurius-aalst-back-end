## Why

The Discovery rebuild worker currently requeues every `Running` job after a fixed 15-minute interval. A legitimate long rebuild can therefore be reported as pending while it is still executing, while an interrupted job cannot be retried promptly after the single worker restarts.

## What Changes

- Replace elapsed-time recovery with one-time recovery of persisted `Running` rebuild jobs during Discovery worker startup.
- Preserve a legitimately long-running job while the single worker remains active, including when an admin submits another rebuild request.
- Treat requested worker cancellation as recoverable interruption instead of a failed rebuild; preserve the existing bounded failure behavior for genuine errors.
- Explicitly retain the single Discovery worker per database deployment assumption; multi-instance claim coordination is out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `discovery-search-projections`: Replace threshold-based interrupted-job recovery with startup recovery and clarify cancellation behavior under the single-worker deployment assumption.

## Impact

- Affects Discovery rebuild-service and hosted-worker lifecycle handling and its focused tests.
- Does not change routes, authorization, DTOs, JSON, event contracts, configuration, or database schema.
- Does not introduce distributed locks, leases, heartbeats, raw SQL claims, or multi-instance worker coordination.
