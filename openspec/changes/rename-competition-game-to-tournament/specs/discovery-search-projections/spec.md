## MODIFIED Requirements

### Requirement: Discovery-owned public search projection
The system MUST persist privacy-safe Discovery search documents in the `discovery.search_documents`
table for User, Team, Tournament, and Sponsor source entities. Each document MUST have a unique
entity type and entity ID, normalized searchable text, a source version, an explicit deleted state,
and the UTC time of its latest accepted update.

#### Scenario: Public entities are projected
- **WHEN** a complete active user, active team, tournament, or sponsor changes
- **THEN** Discovery stores or updates the corresponding document with only its allowed search/display metadata

#### Scenario: Ineligible users are not publicly searchable
- **WHEN** a user is deleted, incomplete, or has no usable username
- **THEN** Discovery marks its document deleted or does not expose it to public search

#### Scenario: Deleted source entities are retained as deleted documents
- **WHEN** a user, team, tournament, or sponsor is deleted
- **THEN** Discovery marks the matching document deleted without exposing its prior metadata through public search

### Requirement: Version-safe projection consumption
Discovery MUST consume the durable lifecycle events published by Identity, Teams, Tournament, and
Sponsorship and MUST process at-least-once delivery without duplicating projection effects. Discovery
MUST NOT replace a document with an event whose source version is older than the stored source version.

#### Scenario: Duplicate delivery is ignored
- **WHEN** the durable dispatcher delivers the same source event more than once
- **THEN** Discovery applies the event once and records the existing inbox completion semantics

#### Scenario: Stale event cannot overwrite a newer document
- **WHEN** an earlier source event is retried after Discovery has applied a later event for the same document
- **THEN** Discovery keeps the newer document state and source version

#### Scenario: Tournament cancellation preserves current search visibility
- **WHEN** a tournament cancellation event is processed
- **THEN** Discovery retains the tournament document for public search unless a separate deletion event removes it

### Requirement: Projection-backed public search
The existing public search endpoint MUST obtain its User, Team, and Tournament results only from
active Discovery search documents at request time. Its route, authorization, rate-limit policy, JSON
response shape, privacy filtering, case-insensitive matching, relevance ordering, and keyset cursor
behavior MUST remain equivalent to the current documented public-search behavior, with tournament
navigation using `/tournaments/{id}`.

#### Scenario: Search does not query source live tables
- **WHEN** a client requests public global search
- **THEN** Discovery evaluates the request against its search-document projection without querying Identity, Teams, Tournament, or Sponsorship source tables

#### Scenario: Existing public result types use canonical navigation
- **WHEN** a client requests public global search after Discovery is enabled
- **THEN** the response includes only the documented User, Team, and Tournament result types with the existing privacy-safe fields
- **AND** Tournament results navigate to `/tournaments/{id}`
