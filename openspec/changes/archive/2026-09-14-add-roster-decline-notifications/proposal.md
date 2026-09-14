# Change: Add roster decline and notification actions

## Why

Selected tournament roster members can confirm a place but cannot decline it, and pending selections are not discoverable in the notification bell after a reload. Captains also need a repairable pending registration when a selected member declines without losing other members' responses.

## What Changes

- Add an authenticated, idempotent decline action for a user's own pending roster selection.
- Keep the affected team registration pending and preserve other roster members' responses after a decline.
- Preserve unchanged confirmations when the captain submits a replacement roster.
- Add a paged current-user query for actionable pending roster selections.
- Publish realtime roster changes for confirm and decline outcomes so connected clients can refresh notifications and registration state.

## Impact

- Affected spec: `tournament-registration`
- Affected module: Tournament registration endpoints, service, read model, DTOs, and tests
- Persistence: no schema migration; pending roster rows remain the authoritative notification source
