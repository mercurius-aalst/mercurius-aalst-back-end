## Context

Pending roster selections already exist as durable roster-member rows and are uniquely scoped by tournament and user. The backend emits a realtime event when a captain creates a pending selection, but it exposes neither a global current-user query nor a decline transition. Reusing the roster rows avoids a second notification store that could become stale.

## Decisions

### Decline removes only the pending roster-member row

The selected user may delete their own pending roster-member resource. The team registration remains pending and becomes an incomplete, repairable roster. Other roster members and their responses remain intact. A missing, non-owned, or already-removed resource returns success without revealing whether it exists, which also makes retries idempotent.

### Replacement preserves unchanged responses

When the captain submits the repaired exact-size roster, confirmation state and timestamps are carried forward for unchanged members. Newly selected non-captains are pending. Removed pending selections receive withdrawal events.

### Pending rows are the notification read model

The current-user notification endpoint pages pending roster rows joined to their scheduled tournament and team registration snapshots. It returns an envelope with total count so clients can represent the complete actionable count and fetch additional pages.

### Mutations retain the exact-size activation invariant

Confirmation may activate a registration only when its current roster has the tournament's exact required size and every member is confirmed or auto-confirmed. Decline, confirmation, and roster replacement use the existing transaction and persistence coordination. PostgreSQL's native `xmin` row version protects the registration aggregate against lost concurrent updates without adding a schema column; stale writers receive a conflict response.

### Member-response realtime delivery is best effort after commit

Confirm and decline persist and commit before attempting realtime delivery. A publisher failure is logged and does not turn a committed member response into an HTTP failure. Clients recover authoritative actionable state from the paged pending-selection query on reload or reconnect.

## Risks

- Concurrent captain replacement and member response can race. Activation rechecks the tracked current roster and persistence remains transactional.
- Realtime delivery happens after commit and is advisory. Reloading the paged pending-selection query remains authoritative.
