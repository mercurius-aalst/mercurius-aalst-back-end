## Why

Authenticated team and invite reads currently trigger unbounded, system-wide invite expiry, retention deletes, and real-time event fan-out. A single user's ordinary request can therefore perform work proportional to every invite in the database, increasing latency and allowing unrelated users' data volume to affect the request.

## What Changes

- Remove invite expiry, retention cleanup, and expiry-event publication from current-user read paths while continuing to exclude due invites from actionable projections.
- Add a configurable scheduled maintenance worker that expires and deletes invites in deterministic, bounded batches.
- Publish expiry events only for invites persisted as expired by the bounded maintenance batch.
- Add database indexes for the global expiry and terminal-retention query shapes.
- Add regression coverage for read isolation, cancellation, bounded maintenance, event fan-out, configuration, and indexes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `user-owned-team-management`: Clarify that current-user reads MUST remain side-effect free for invite maintenance and that persisted expiry, retention cleanup, and expiry events run through bounded scheduled maintenance.

## Impact

The Teams module gains a scoped invite-maintenance service and hosted worker, configurable batch size and interval settings, and supporting invite indexes. Public routes and JSON response shapes remain unchanged; expired invites remain immediately absent from actionable read projections based on their expiration timestamp.
