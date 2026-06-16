## Context

Tournament team registration currently creates transient pending roster member rows for selected non-captain members. The existing mutation only confirms a pending row and activates the team once all selected members are confirmed.

## Goals / Non-Goals

**Goals:**
- Replace the confirm-only route with an action-based, team-scoped roster member route.
- Let selected users explicitly confirm or decline pending selections.
- Remove same-tournament pending selections for a user before they submit a roster for a team they captain.
- Use roster-selection naming for the related SignalR event, publisher method, payload record, and registration-state DTO fields.
- Preserve the existing transient cleanup model for pending team registrations.

**Non-Goals:**
- Add long-lived declined roster member history.
- Change public registration projections.
- Introduce notification persistence beyond the existing SignalR event publisher.

## Decisions

- Use `PUT /teams/{teamId}/roster/members/{rosterMemberId}` with a body enum action. This keeps the route resource-oriented, scopes the roster member to its team, and lets the same endpoint express `Confirm` and `Decline`.
- Treat declined selections as transient cleanup. A declined member makes the pending team registration invalid because exact-size roster acceptance can no longer complete, so the service deletes that pending team registration and publishes declined events for affected pending selections.
- Before a captain submits their own roster, delete any pending roster selections for that user in the same tournament. This clears duplicate-participation state before normal team and roster validation, while active participation remains protected by the existing duplicate checks.

## Risks / Trade-offs

- Breaking route and event-name changes -> Update route and SignalR tests, and keep the new DTO explicit so clients can migrate cleanly.
- Deleting a pending registration on decline removes other members' pending selections too -> Publish declined events for all affected pending selections so current clients can clear actionable state.
- Auto-decline before roster validation could remove pending selections even if the new roster is later invalid -> This matches the user's explicit intent to register their own team and avoids leaving stale actionable selections for the same tournament.
