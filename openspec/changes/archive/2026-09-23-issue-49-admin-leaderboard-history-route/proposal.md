# Specify the leaderboard attempt-history route

## Why

The admin attempt-history read endpoint should use the same `/leaderboard/attempts` collection resource as attempt creation. The current `/leaderboard/admin` suffix describes the caller rather than the data, while the existing admin role policy already provides the privacy boundary.

## What Changes

- Specify `GET /v1/lan/tournaments/{tournamentId}/leaderboard/attempts` as the admin-only read route for the existing attempt-history projection.
- Retire `GET /v1/lan/tournaments/{tournamentId}/leaderboard/admin` without a compatibility alias.
- Preserve the anonymous live-ranking endpoint and tournament-detail finalized placements.

## Impact

The backend route contract and its API consumers are affected. Authorization, response shape, attempt behavior, public rankings, and finalized placements remain unchanged.
