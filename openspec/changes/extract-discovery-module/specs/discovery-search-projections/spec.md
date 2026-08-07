## ADDED Requirements

### Requirement: Discovery-owned public search projection
The system MUST persist privacy-safe Discovery search documents in the `discovery.search_documents` table for User, Team, Game, and Sponsor source entities. Each document MUST have a unique entity type and entity ID, normalized searchable text, a source version, an explicit deleted state, and the UTC time of its latest accepted update.

#### Scenario: Public entities are projected
- **WHEN** a complete active user, active team, game, or sponsor changes
- **THEN** Discovery stores or updates the corresponding document with only its allowed search/display metadata

#### Scenario: Ineligible users are not publicly searchable
- **WHEN** a user is deleted, incomplete, or has no usable username
- **THEN** Discovery marks its document deleted or does not expose it to public search

#### Scenario: Deleted source entities are retained as deleted documents
- **WHEN** a user, team, game, or sponsor is deleted
- **THEN** Discovery marks the matching document deleted without exposing its prior metadata through public search

### Requirement: Version-safe projection consumption
Discovery MUST consume the durable lifecycle events published by Identity, Teams, Competition, and Sponsorship and MUST process at-least-once delivery without duplicating projection effects. Discovery MUST NOT replace a document with an event whose source version is older than the stored source version.

#### Scenario: Duplicate delivery is ignored
- **WHEN** the durable dispatcher delivers the same source event more than once
- **THEN** Discovery applies the event once and records the existing inbox completion semantics

#### Scenario: Stale event cannot overwrite a newer document
- **WHEN** an earlier source event is retried after Discovery has applied a later event for the same document
- **THEN** Discovery keeps the newer document state and source version

#### Scenario: Game cancellation preserves current search visibility
- **WHEN** a game cancellation event is processed
- **THEN** Discovery retains the game document for public search unless a separate deletion event removes it

### Requirement: Projection-backed public search
The existing public search endpoint MUST obtain its User, Team, and Game results only from active Discovery search documents at request time. Its route, authorization, rate-limit policy, JSON response shape, privacy filtering, case-insensitive matching, relevance ordering, and keyset cursor behavior MUST remain equivalent to the current documented public-search behavior.

#### Scenario: Search does not query source live tables
- **WHEN** a client requests public global search
- **THEN** Discovery evaluates the request against its search-document projection without querying Identity, Teams, Competition, or Sponsorship source tables

#### Scenario: Existing public result types remain stable
- **WHEN** a client requests public global search after Discovery is enabled
- **THEN** the response includes only the documented User, Team, and Game result types with the existing navigation fields

### Requirement: Admin search-index rebuild jobs
The API MUST expose `POST /internal/discovery/search-index-rebuild-jobs` and `GET /internal/discovery/search-index-rebuild-jobs/{jobId}` as admin-only internal endpoints. Discovery MUST persist the status of each rebuild job and MUST coalesce a new request with an already pending or running rebuild.

#### Scenario: Admin requests a rebuild
- **WHEN** an admin posts a search-index rebuild request
- **THEN** the API returns an observable pending or running rebuild job identifier

#### Scenario: Rebuild reconstructs current documents
- **WHEN** Discovery processes a pending rebuild job
- **THEN** it obtains privacy-safe source snapshots through module contracts and upserts current documents without requiring public search requests to query source tables

#### Scenario: Non-admin access is rejected
- **WHEN** a caller without the admin role requests or reads a rebuild job
- **THEN** the API rejects the request according to the existing authorization policy

#### Scenario: Failed rebuild is observable
- **WHEN** a rebuild cannot complete
- **THEN** Discovery persists a failed terminal status and a bounded diagnostic message for the job
