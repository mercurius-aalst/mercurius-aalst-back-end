# Design

Use `GET /v1/lan/tournaments/{tournamentId}/leaderboard/attempts` for the admin history projection. This shares a resource path with the existing `POST /attempts` operation while keeping the read and write methods distinct. The route keeps the existing admin role requirement and response projection; no query or response fields change.

The former `GET /leaderboard/admin` route is removed, with no alias. Anonymous `GET /leaderboard` continues to return only the live ranking. Finalized placements continue to be exposed through tournament details. The archived issue-49 design remains unchanged as a historical record; this follow-up specifies the route correction.
