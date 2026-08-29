# Design

## Lifecycle

Match stores ended confirmations for each side, each side's latest score report, server UTC deadlines, and a MatchLifecycleState. The state transitions are performed by domain methods. A single score report starts the five-minute confirmation deadline; a matching report completes the match, while a mismatch enters a five-minute correction window. An expired correction window becomes AdminResolutionRequired and cannot be changed by players.

## Commands

- POST /v1/lan/matches/{matchId}/confirm-ended confirms the authenticated participant's side.
- PUT /v1/lan/matches/{matchId}/score submits the authenticated participant's score report.
- POST /v1/lan/matches/{matchId}/forfeit forfeits the authenticated participant's side; an admin MAY select either side.
- POST /v1/lan/matches/{matchId}/resolve lets an admin resolve a dispute with a final score.
- POST /v1/lan/matches/{matchId}/reverse lets an admin reverse a completed or forfeited result when no linked next match has a result.

All commands reload the match with the linked next matches, revalidate ownership/role and the current server time, then save in one transaction. A hosted deadline processor also applies expired windows and writes completion/escalation events through the platform outbox, so expiry does not depend on an open browser. The tournament stores an optional assigned-admin subject for routing resolution-required events; when it is null, the existing global admin role is the fallback authority. Dispute resolution rejects a signed-in admin whose subject does not match a configured assignment with the machine-readable reason admin_not_assigned. A reversal clears only downstream participant slots that originated from the reverted match and is rejected if any linked next match has a submitted result.

## Compatibility and privacy

The migration backfills legacy matches that already have a winner and score as Completed/Score results before new lifecycle commands are used. The existing anonymous GET /matches/{matchId} projection keeps its route and existing fields. New lifecycle fields are status/deadline/score metadata only. The existing admin PUT /matches/{matchId} remains a force-score compatibility route. API clients should treat unknown enum values as unavailable and refresh after each command.
