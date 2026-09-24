## Why

PR review identified avoidable full-aggregate reads, attempt rules split between the service and domain, an unclear shared concurrency revision, and an inaccurate conflict code for non-leaderboard lifecycle saves.

## What Changes

Public and admin leaderboard read models are projected from queries, attempt validation is owned by the tournament aggregate, and the existing revision token is documented as applying to the whole aggregate. Leaderboard lifecycle conflicts retain their current code and message; non-leaderboard lifecycle conflicts receive an accurate tournament-specific code and message.

## Non-goals

- No response shape or authorization changes.
- No database schema change or revision increment change.
