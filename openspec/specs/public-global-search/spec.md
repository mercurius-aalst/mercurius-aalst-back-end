## Purpose
Provide anonymous, privacy-safe global search across public users, teams, and tournaments so clients can navigate to matching public pages without exposing private account data.
## Requirements
### Requirement: Public global search endpoint
The API MUST expose `GET /v1/lan/search?query={query}` for anonymous public search.

#### Scenario: Search across supported entities
- **WHEN** a client searches with a valid query of at least three trimmed characters
- **THEN** the response includes matching users, teams, and tournaments in a stable normalized result shape

#### Scenario: Short query handling
- **WHEN** a client searches with fewer than three trimmed characters
- **THEN** the response returns an empty result list

#### Scenario: Bounded result count
- **WHEN** many entities match a query
- **THEN** each response page is limited to a configured maximum number of results
- **AND** results are returned in deterministic relevance order
- **AND** the response includes a continuation cursor when additional matches exist

### Requirement: Search response DTO shape
The search response MUST include a bounded `results` collection and pagination metadata that lets clients retrieve additional matching results without changing the query.

#### Scenario: Search response shape
- **WHEN** a valid search request is processed
- **THEN** the response includes `results`
- **AND** the response includes `nextCursor`
- **AND** the response includes total-count metadata or an equivalent way to indicate more matches exist

#### Scenario: Continue broad search
- **WHEN** more entities match a valid query than fit in the first response page
- **THEN** the client can use the returned continuation cursor to request the next page with the same query
- **AND** all matching entities are reachable through repeated continuation requests

### Requirement: Search result DTO shape
Each search result MUST identify its type, display label, supporting text, and exactly the navigation field relevant to that type.

#### Scenario: User result shape
- **WHEN** a user result is returned
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `username`, and does not include team or tournament navigation fields

#### Scenario: Team result shape
- **WHEN** a team result is returned
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `teamName`, and does not include user or tournament navigation fields

#### Scenario: Tournament result shape
- **WHEN** a tournament result is returned
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `tournamentId`, and does not include user or team navigation fields

### Requirement: Search privacy and filtering
Search MUST return only public-safe data.

#### Scenario: Deleted or incomplete users excluded
- **WHEN** deleted users, incomplete users, or users without usernames match the query
- **THEN** they are excluded from search results

#### Scenario: Private fields omitted
- **WHEN** search returns user results
- **THEN** the response does not expose email, first name, last name, platform IDs, Auth0 IDs, deleted state, or timestamps

#### Scenario: Case-insensitive matching
- **WHEN** a client searches using different casing than stored usernames, team names, or tournament names
- **THEN** matching is case-insensitive

### Requirement: Public search uses explicit eligibility and stable indexed ordering
Public global search MUST return only active User, Team, and Tournament search documents; Sponsor documents MUST NOT be returned. Searchability MUST be determined by the entity type itself and MUST NOT depend on `type_order`. Search MUST preserve exact-match, starts-with, then contains ranking and MUST page results in the deterministic order `(normalized_text, type_order, entity_id)` within each rank. `normalized_text` ordering MUST use PostgreSQL `C` collation, including the resulting byte/code-point order for non-ASCII labels.

#### Scenario: Sponsor and inactive documents stay out of results
- **WHEN** a matching search document is a Sponsor or is soft-deleted
- **THEN** public search MUST omit it regardless of its `type_order`

#### Scenario: Public entity eligibility does not depend on display order
- **WHEN** a matching active document is a User, Team, or Tournament
- **THEN** public search MUST consider it even if its ordering value changes
- **AND** an active Sponsor MUST remain ineligible even if its ordering value changes

#### Scenario: Search ranking and cursor order remain deterministic
- **WHEN** matching documents span exact, starts-with, and contains ranks
- **THEN** results MUST remain ordered exact before starts-with before contains
- **AND** each rank MUST order by normalized text, type order, then entity ID
- **AND** cursor paging MUST return each result once without skips

#### Scenario: Non-ASCII labels use C-collation ordering
- **WHEN** search results include `team zed` and `team émile`
- **THEN** `team zed` MUST precede `team émile`

### Requirement: Search projection indexes support exact, prefix, contains, and cursor queries
The database MUST provide one active-only B-tree over `(normalized_text, type_order, entity_id)` using `C` collation for `normalized_text`, and one active-only trigram GIN index over `normalized_text`. No Discovery search index predicate MUST encode entity-type names. The prefix pass MUST retain escaped `LIKE` matching and use parameterized, plain scalar lower and (when safely constructible) upper bounds on `normalized_text`; the `LIKE` predicate MUST remain the correctness guard. Cursor seeking MUST use one lexicographic comparison over the same ordered key and MUST not cast indexed columns.

#### Scenario: Prefix bounds remain indexable for reusable plans
- **WHEN** a starts-with query has a safely constructible upper bound
- **THEN** its SQL MUST compare `normalized_text >= lower` and `normalized_text < upper` directly
- **AND** both bounds MUST be parameters
- **AND** the query MUST retain escaped `LIKE` and exact-match exclusion

#### Scenario: Prefix upper bound cannot be safely incremented
- **WHEN** the prefix is empty or ends in a UTF-16 surrogate or the maximum code unit
- **THEN** the upper bound MUST be omitted
- **AND** the lower bound and `LIKE` predicate MUST still determine matching results

#### Scenario: Cursor seeks by the B-tree key
- **WHEN** a result page continues after a cursor
- **THEN** the query MUST compare `(normalized_text, type_order, entity_id)` lexicographically to the cursor key
- **AND** the indexed key columns MUST remain unwrapped and uncast

