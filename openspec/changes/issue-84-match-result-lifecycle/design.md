# Design

## Lifecycle

Match stores ended confirmations for each side, each side's latest score report, server UTC deadlines, and a MatchLifecycleState. The state transitions are performed by domain methods. A single score report starts the five-minute confirmation deadline; a matching report completes the match, while a mismatch enters a five-minute correction window. An expired correction window becomes AdminResolutionRequired and cannot be changed by players.

## Commands

- POST /v1/lan/matches/{matchId}/confirm-ended confirms the authenticated participant's side.
- PUT /v1/lan/matches/{matchId}/score submits the authenticated participant's score report.
- POST /v1/lan/matches/{matchId}/forfeit forfeits the authenticated participant's side; an admin MAY select either side.
- POST /v1/lan/matches/{matchId}/resolve lets an admin resolve a dispute with a final score.
- POST /v1/lan/matches/{matchId}/reverse lets an admin reverse a completed or forfeited result when no linked next match has a result.

All commands reload the match with the linked next matches, revalidate ownership/role and the current server time, then save in one transaction. A hosted deadline processor also applies expired windows and writes completion/escalation events through the platform outbox, so expiry does not depend on an open browser. The tournament stores an optional assigned-admin subject as the primary recipient for resolution-required events and as the private-report visibility scope; when it is null, the notification uses the global-admin fallback. Any authenticated global admin may resolve, force-forfeit, or reverse while role, tournament-state, participant/lifecycle, and downstream-graph rules pass, regardless of assignment, and no reassignment prerequisite applies. `MatchResolutionRequiredIntegrationEvent` exposes a typed assigned-admin/global-admin recipient contract. The registered tournament consumer persists each notification in `match_resolution_notifications` using the platform message id as its idempotency key, so inbox/outbox retries cannot create duplicate notifications. A reversal clears only downstream participant slots that originated from the reverted match and is rejected if any linked next match has a submitted result.

## Compatibility and privacy

The migration backfills legacy matches that already have a winner and score as Completed/Score results before new lifecycle commands are used. It also derives each populated downstream slot's source from its incoming winner/loser edge and exact participant identity, updating only unique same-tournament candidates and incrementing the affected row's result version. Ambiguous or inconsistent legacy assignments remain unprovenanced; reversal fails closed rather than clearing an assignment without proof. The existing anonymous GET /matches/{matchId} projection keeps its route and existing fields while redacting private score reports. The protected action projection returns both reports to an eligible participant or team captain, or to the assigned tournament admin; when no assignment is configured, the global admin role is the fallback viewer. An unrelated authenticated caller, including an unassigned global admin for an assigned tournament, receives neither report, but that global admin retains eligible administrative action capabilities. New lifecycle fields are status/deadline/score metadata only. The existing admin PUT /matches/{matchId} remains a compatibility route but is subject to the same global-admin role, tournament-state, participant, and lifecycle validation as explicit resolution; assignment is not an additional gate. API clients should treat unknown enum values as unavailable and refresh after each command.

Public and action reads use a match-targeted query and do not hydrate the tournament's sibling matches. Deadline-triggered reads load only directly linked matches when bracket advancement is required; reversal uses bounded downstream traversal.
