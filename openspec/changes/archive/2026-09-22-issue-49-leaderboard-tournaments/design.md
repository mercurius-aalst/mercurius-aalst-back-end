# Design

## Configuration and values

`Leaderboard` is a new bracket type. Its required `rankingMetric` is either `HighestScore` or `FastestTime`; non-leaderboard tournaments reject a metric. Leaderboards are always individual. Match format and scheduling fields remain accepted for multipart contract compatibility but are ignored by leaderboard execution. The metric can be edited only while scheduled.

Scores are nonnegative decimal values stored as `numeric(18,6)`. Durations are positive whole milliseconds stored as `bigint`. Requests and responses expose the two representations as nullable `score` and `durationMilliseconds` fields and require exactly the field selected by the tournament metric.

## Persistence and concurrency

Leaderboard participants and attempts are tournament-owned rows. A participant contains either a unique linked user id or a guest display name; guest rows have independent generated ids, so equal names never merge. Attempts carry one metric-specific value and a concurrency token. A unique `(tournamentId, linkedUserId)` index prevents concurrent duplicate linked participants. Result mutations and lifecycle complete/reset are serialized by optimistic concurrency on the shared tournament row. Every one of those paths loads the tournament with its leaderboard rows, changes them, and increments `LeaderboardRevision`, which is registered as a concurrency token next to `Status`. Attempt mutations run inside an explicit transaction; lifecycle completion and reset rely on the revision guard together with the implicit `SaveChanges` transaction. The writer that saves second updates zero rows and fails with `leaderboard_changed`, and an explicit transaction rolls back. Attempt row versions reject stale correction/deletion, and every route scopes attempt ids to the tournament.

Final placements reference a leaderboard participant, allowing linked users and guests to share the existing placement model without inventing user ids for guests. Reset cascades through participants, attempts, and their placements.

## API

- `GET /v1/lan/tournaments/{tournamentId}/leaderboard` is anonymous and returns the metric and deterministic ranked rows.
- `GET /v1/lan/tournaments/{tournamentId}/leaderboard/admin` requires the admin role and returns participants with complete attempt history.
- `POST /v1/lan/tournaments/{tournamentId}/leaderboard/attempts` requires the admin role and accepts exactly one of `participantId`, `linkedUserId`, or `guestDisplayName`, plus the metric-specific result field.
- `PUT /v1/lan/tournaments/{tournamentId}/leaderboard/attempts/{attemptId}` requires the admin role and corrects the metric-specific value with `rowVersion`.
- `DELETE /v1/lan/tournaments/{tournamentId}/leaderboard/attempts/{attemptId}?rowVersion=...` requires the admin role and removes an attempt.

Public rows contain `rank`, `participantId`, `displayName`, `participantKind`, optional `linkedUserId`, and the metric-specific best value. Admin history additionally contains attempt ids, values, creation/update times, and row versions.

## Ranking and lifecycle

Ranking uses the maximum score or minimum duration per participant. Equal best values share competition ranks (`1, 1, 3`); tied rows are ordered by participant id only for stable serialization. Start bypasses registration minimums and generates no matches. Completion requires a ranked row, freezes current ranks into placements, and rejects later attempt mutations. Reset deletes leaderboard data and placements. Cancellation retains data until reset, matching existing lifecycle behavior.
