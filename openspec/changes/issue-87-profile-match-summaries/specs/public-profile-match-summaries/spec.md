## Purpose

Define efficient, privacy-safe previous and upcoming match summaries for public player and team
profiles.

## ADDED Requirements

### Requirement: Public profile match-summary endpoints

The API MUST expose anonymous reads at
`GET /v1/lan/public/users/{username}/match-summaries` and
`GET /v1/lan/public/teams/{teamName}/match-summaries`.

#### Scenario: Active player registration
- **WHEN** an anonymous client requests a complete, non-deleted player profile's summaries
- **THEN** the response MUST contain at most one previous and one upcoming match per tournament
  with an active individual registration or an active team registration whose confirmed roster or
  captain snapshot includes that player

#### Scenario: Active team registration
- **WHEN** an anonymous client requests an existing, non-deleted team's summaries
- **THEN** the response MUST contain at most one previous and one upcoming match per tournament
  with an active team registration for that team

#### Scenario: Public profile not found
- **WHEN** the requested player is missing/incomplete/deleted or the team is missing/deleted
- **THEN** the endpoint MUST return 404 without revealing match data

### Requirement: Lifecycle-aware summary selection

The service MUST select summaries from the authoritative match lifecycle and MUST use deterministic
ordering.

#### Scenario: Previous selection
- **WHEN** multiple matches for one participant and tournament have an official result
- **THEN** the API MUST return only the latest `Completed` or `Forfeited` match, ordered by result
  completion time descending, then round, match number, and match ID

#### Scenario: Upcoming selection
- **WHEN** multiple unplayed matches for one participant and tournament exist
- **THEN** the API MUST return only the earliest non-BYE match ordered by estimated/scheduled start,
  then round, match number, and match ID

#### Scenario: Reversed/canceled/unresolved matches
- **WHEN** a match is `Reversed`, belongs to a canceled tournament, is already in progress, or is
  unresolved (`Disputed`/`AdminResolutionRequired`)
- **THEN** it MUST be excluded from both previous and upcoming projections

#### Scenario: BYE and TBD
- **WHEN** a match has a BYE slot
- **THEN** it MUST be excluded because no playable opponent exists
- **WHEN** an otherwise eligible upcoming match has only the profile participant assigned
- **THEN** the response MUST identify the opponent as TBD without exposing private identifiers

### Requirement: Privacy-safe response projection

Each summary MUST include only public tournament/match IDs and names, match ID, opponent display
label or TBD indication, public lifecycle/result state, participant-relative scores when present,
estimated and scheduled times when present, and safe round/bracket metadata. `EstimatedStartTime`
MUST take precedence over `ScheduledStartTime` for upcoming ordering. Missing or
`DateTime.MinValue` times MUST be represented as absent rather than serialized as a sentinel. It
MUST NOT include email, Auth0 IDs, private reports, admin assignment, deletion state, or private
account metadata.

#### Scenario: Stable navigation
- **WHEN** a summary is returned
- **THEN** tournament ID and match ID MUST be non-empty and stable for navigation

#### Scenario: No qualifying match
- **WHEN** a valid profile has no qualifying match in a category
- **THEN** that category MUST be represented by an empty array

### Requirement: Set-based bounded query behavior

The service MUST resolve active registrations and select one summary per tournament using a bounded
number of persistence queries. It MUST NOT load all profile matches for in-memory filtering and
MUST NOT issue N+1 queries per tournament, match, or opponent.

#### Scenario: Large profile history
- **WHEN** a participant has many registrations and matches
- **THEN** previous and upcoming selection MUST remain set-based, bounded, and deterministically
  ordered
