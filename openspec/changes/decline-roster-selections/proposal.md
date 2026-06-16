## Why

Selected team roster members can currently only confirm a pending roster selection. They need an explicit way to decline, and the API path and event contract should use roster-selection language.

## What Changes

- **BREAKING** Replace the confirm-only roster member route with `PUT /teams/{teamId}/roster/members/{rosterMemberId}`.
- Add a request DTO for roster selection actions so callers can submit `Confirm` or `Decline`.
- Rename related SignalR events, publisher methods, and DTO/model fields to roster selection terminology.
- Allow the selected user to decline a pending roster selection, making that pending team registration no longer actionable.
- Automatically decline a user's pending roster selections for the same tournament when they submit a roster for a team they captain.

## Capabilities

### New Capabilities

### Modified Capabilities
- `tournament-registration`: Adds roster selection action semantics, decline behavior, and auto-decline during captain team registration.

## Impact

- Affected code: tournament registration endpoints, registration DTOs, `ITournamentRegistrationService`, `TournamentRegistrationService`, and focused route/service tests.
- APIs: Breaking route/method change for roster selection confirmation and a new body contract.
- Dependencies: No new packages.
- Data: No schema migration expected; declined pending selections remain transient and are removed with the pending registration.
