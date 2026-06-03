## Purpose
Provide anonymous, privacy-safe global search across public users, teams, and games so clients can navigate to matching public pages without exposing private account data.

## Requirements

### Requirement: Public global search endpoint
The API MUST expose `GET /v1/lan/search?query={query}` for anonymous public search.

#### Scenario: Search across supported entities
- **WHEN** a client searches with a valid query of at least three trimmed characters
- **THEN** the response includes matching users, teams, and games in a stable normalized result shape

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
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `username`, and does not include team or game navigation fields

#### Scenario: Team result shape
- **WHEN** a team result is returned
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `teamName`, and does not include user or game navigation fields

#### Scenario: Game result shape
- **WHEN** a game result is returned
- **THEN** it includes `type`, `displayLabel`, `supportingText`, and `gameId`, and does not include user or team navigation fields

### Requirement: Search privacy and filtering
Search MUST return only public-safe data.

#### Scenario: Deleted or incomplete users excluded
- **WHEN** deleted users, incomplete users, or users without usernames match the query
- **THEN** they are excluded from search results

#### Scenario: Private fields omitted
- **WHEN** search returns user results
- **THEN** the response does not expose email, first name, last name, platform IDs, Auth0 IDs, deleted state, or timestamps

#### Scenario: Case-insensitive matching
- **WHEN** a client searches using different casing than stored usernames, team names, or game names
- **THEN** matching is case-insensitive
