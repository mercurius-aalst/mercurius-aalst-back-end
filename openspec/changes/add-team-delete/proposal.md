# add-team-delete

## Summary
Add authenticated, captain-owned team deletion that preserves historical tournament data.

## Motivation
The front-end needs a way for a team captain to delete a team. A hard delete would break existing match, placement, and tournament registration references, so deletion must behave as an archival/soft-delete operation rather than removing the team row.

## Scope
- Add an authenticated `DELETE /v{version}/lan/teams/{id}` route.
- Allow only the current team's captain to delete the team.
- Soft-delete teams so historical references remain readable.
- Block deletion while the team is actively registered for scheduled or in-progress team tournaments/games.
- Exclude deleted teams from normal team lookup, search, public profile, and current-user team-management projections.
- Add tests for authorization, blocking rules, soft deletion, and query visibility.

## Out of Scope
- Admin force-delete or restore workflows.
- Physical deletion of teams, match history, placements, or registration history.
- Front-end DTO or client changes.

## Impact
- Adds soft-delete fields to `Team` and an EF Core migration.
- Reuses existing exception middleware behavior for unauthorized, not-found, and validation responses.
- No CORS, Auth0, or deployment configuration changes expected.
