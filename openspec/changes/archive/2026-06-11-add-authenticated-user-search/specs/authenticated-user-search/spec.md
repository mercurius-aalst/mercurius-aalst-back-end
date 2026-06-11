## ADDED Requirements

### Requirement: Authenticated user search
The API MUST support authenticated filtering of the users collection with `GET /v1/lan/users?query={query}`.

#### Scenario: Authenticated caller searches users
- **WHEN** an authenticated client searches with a valid query
- **THEN** the response includes matching user search results
- **AND** each result includes the stable backend user id

#### Scenario: Anonymous caller rejected
- **WHEN** an anonymous client searches the users collection
- **THEN** the API rejects the request as unauthorized

### Requirement: User search request bounds
Authenticated user search MUST support a query string, continuation cursor, and bounded page size.

#### Scenario: Short query returns no candidates
- **WHEN** a client searches with fewer than three trimmed characters
- **THEN** the response returns an empty candidate list

#### Scenario: Result count is bounded
- **WHEN** more users match than the requested or default page size allows
- **THEN** the response contains no more than the bounded page size

#### Scenario: Continue user search
- **WHEN** more users match a valid query than fit in the first response page
- **THEN** the response includes `nextCursor`
- **AND** the client can request the next page with the same query and returned cursor

#### Scenario: Overlong query rejected
- **WHEN** a client searches with a query beyond the configured maximum search length
- **THEN** the API rejects the request with validation failure

#### Scenario: Search requests are rate limited
- **WHEN** an authenticated client searches the users collection
- **THEN** the request is subject to the API search rate limit policy

### Requirement: User search response shape
Authenticated user search results MUST include only the fields needed for user selection, plus pagination metadata aligned with global search.

#### Scenario: Search response envelope
- **WHEN** a user search request is processed
- **THEN** the response includes `results`
- **AND** the response includes `nextCursor`
- **AND** the response includes `hasMore`

#### Scenario: Candidate fields returned
- **WHEN** a user candidate is returned
- **THEN** it includes `id`, `type`, `username`, `displayLabel`, and `supportingText`

#### Scenario: Private fields omitted
- **WHEN** user candidates are returned
- **THEN** the response omits email, Auth0 id, roles, deletion state, first name, last name, platform ids, and timestamps

### Requirement: User search privacy filtering
Authenticated user search MUST return only privacy-safe active users with usable usernames.

#### Scenario: Deleted and username-less users excluded
- **WHEN** deleted users or users without usernames match the query
- **THEN** they are excluded from user search results

#### Scenario: Case-insensitive username matching
- **WHEN** a client searches using different casing than stored usernames
- **THEN** matching is case-insensitive

### Requirement: User search database indexes
Authenticated user search MUST use the existing database indexing strategy aligned with normalized username matching.

#### Scenario: Username matching index exists
- **WHEN** authenticated user search is deployed to PostgreSQL
- **THEN** the database includes a trigram index on active users' normalized usernames

#### Scenario: Dedicated authenticated-search migration not required
- **WHEN** authenticated user search is deployed alongside the existing public search indexes
- **THEN** the API does not require an additional authenticated-search-specific migration
