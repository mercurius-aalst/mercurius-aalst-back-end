## Implementation

- [x] Project public leaderboard ranking fields in the database query and apply deterministic ranking to the projected rows.
- [x] Project the admin participant and attempt history DTOs in the database query, preserving identity privacy and ordering.
- [x] Move add, update, and remove leaderboard attempt business validation into Tournament domain methods; keep services responsible for external identity lookup and persistence.
- [x] Document that LeaderboardRevision is the shared aggregate concurrency token without changing its increments or stored column.
- [x] Return leaderboard_changed with its existing message for leaderboard lifecycle conflicts and tournament_changed with an accurate message for non-leaderboard lifecycle conflicts.
- [x] Add focused tests for projection behavior, domain validation, and non-leaderboard lifecycle conflict details.

## Validation

- [x] Strictly validate this OpenSpec change before implementation.
- [x] Run the in-memory leaderboard projection and domain tests.
- [x] Attempt PostgreSQL-backed projection and concurrency tests; record that the local PostgreSQL service was unavailable on 127.0.0.1:5432.
- [x] Strictly validate the change after implementation and task synchronization.
