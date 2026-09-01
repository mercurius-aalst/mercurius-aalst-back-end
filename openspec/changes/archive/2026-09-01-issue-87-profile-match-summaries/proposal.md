## Why

Public profile responses currently contain participant identity and team tournament links but no
efficient way to show the participant's latest completed match or next scheduled match. A client
that loads every tournament and match would create an N+1 pattern and would risk exposing private
match lifecycle data.

## What Changes

- Add dedicated anonymous match-summary reads for public user and team profiles.
- Select at most one previous and one upcoming match per active tournament registration.
- Return public tournament/match identifiers, labels, opponent display data, schedule, result, and
  lifecycle state without private reports or account metadata.
- Use set-based bounded queries with deterministic ordering, preferring current publicly resolvable
  user/team values and falling back to retained registration snapshots when those sources are
  unavailable.

## Non-goals

- No match mutations, registration changes, private report access, or new persistence entities.
- No loading all matches or issuing per-tournament/per-opponent queries.

## Dependency and stacking

This branch stacks on BE PR #121 (`codex/issue-84-match-result-lifecycle`, `ca23a96`) because the
summary contract returns the authoritative Completed, Forfeited, and Reversed lifecycle states.
Merge PR #121 before this change's PR. The FE counterpart stacks on FE PR #55.
