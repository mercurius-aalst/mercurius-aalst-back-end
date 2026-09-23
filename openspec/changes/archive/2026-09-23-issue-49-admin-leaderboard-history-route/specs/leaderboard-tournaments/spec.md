## ADDED Requirements

### Requirement: Leaderboard attempt-history route
The API MUST expose `GET /v1/lan/tournaments/{tournamentId}/leaderboard/attempts` to authenticated admins as the read route for the existing leaderboard attempt-history projection. It MUST preserve the existing admin response shape and MUST NOT expose the former `GET /v1/lan/tournaments/{tournamentId}/leaderboard/admin` route as an alias. Anonymous `GET /v1/lan/tournaments/{tournamentId}/leaderboard` MUST continue to expose live rankings without attempt history, and tournament details MUST continue to expose finalized placements.

#### Scenario: Admin reads leaderboard attempt history
- **WHEN** an authenticated admin requests `GET /v1/lan/tournaments/{tournamentId}/leaderboard/attempts`
- **THEN** the API MUST return the existing participant and attempt-history projection

#### Scenario: Anonymous caller reads the public leaderboard
- **WHEN** an anonymous caller requests `GET /v1/lan/tournaments/{tournamentId}/leaderboard`
- **THEN** the API MUST return live rankings without attempt history, and finalized placements MUST remain available through tournament details

#### Scenario: Former admin-history route is unavailable
- **WHEN** a client requests `GET /v1/lan/tournaments/{tournamentId}/leaderboard/admin`
- **THEN** the API MUST NOT expose that route as an alias
