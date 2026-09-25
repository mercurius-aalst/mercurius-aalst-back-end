## MODIFIED Requirements

### Requirement: Deterministic public ranking

The leaderboard MUST use only each participant's best attempt: maximum score for `HighestScore` and minimum duration for `FastestTime`. Equal best values MUST share competition ranks such as `1, 1, 3`. Stable ordering within a tie MUST use participant id without changing rank. Adding a worse attempt MUST NOT reduce the existing best result. Public responses MUST expose the metric, rank, participant id, display name, linked-user or guest kind, optional linked user id, and metric-specific best value, and MUST NOT expose attempt history or mutation metadata. When a new linked LAN participant is created, the participant's display name MUST be set to the user's public username snapshotted at that moment. If the user has no public username — including incomplete or otherwise non-public profiles — the generic "Incomplete profile" placeholder MUST be stored instead, and a real profile name MUST NOT be stored or exposed. All public leaderboard, administrator participant, and finalized placement projections MUST display that stored participant display name and MUST NOT perform a read-time identity lookup. Guest display names MUST remain the name entered for that guest. The public username lookup MUST complete before the participant and its first attempt are persisted, so a failed lookup MUST NOT leave a committed participant or attempt.

#### Scenario: Worse attempt preserves best result
- **WHEN** a participant records an attempt worse than their current best
- **THEN** the public best result and rank MUST continue to use the better attempt

#### Scenario: Tie uses competition ranking
- **WHEN** two participants share the best value and another follows
- **THEN** their ranks MUST be `1`, `1`, and `3` in deterministic order

#### Scenario: Linked username is snapshotted at creation
- **WHEN** a new linked LAN participant is created for a user with a public username
- **THEN** the stored participant display name MUST be that user's public username
- **AND** public leaderboard, administrator participant, and finalized placement projections MUST display that stored name without a read-time identity lookup

#### Scenario: Linked user without a public username falls back to a generic placeholder
- **WHEN** a new linked LAN participant is created for a user without a public username
- **THEN** the stored participant display name MUST be the generic "Incomplete profile" placeholder
- **AND** the user's real profile name MUST NOT appear in the stored value or any public projection

#### Scenario: Guest participants keep their entered name
- **WHEN** a guest participant appears in any leaderboard projection
- **THEN** the displayed name MUST remain the name entered for that guest

#### Scenario: Failed username lookup leaves nothing committed
- **WHEN** the public username lookup fails while creating a new linked participant
- **THEN** the request MUST fail without persisting the participant or its attempt
- **AND** a later successful request for the same user MUST create the participant with the username as its display name
