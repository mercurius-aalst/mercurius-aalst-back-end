## ADDED Requirements

### Requirement: Public game and team collections are bounded and navigable
The API MUST page the existing anonymous game and team collection routes using optional positive `page` and `pageSize` query parameters while preserving their routes and raw JSON array response shapes.

#### Scenario: Default collection page
- **WHEN** a client requests `GET /v1/lan/games` or `GET /v1/lan/teams` without paging parameters
- **THEN** the API returns the first page with at most 20 existing item objects in a raw JSON array
- **AND** the response does not add a total count or pagination envelope

#### Scenario: Custom collection page
- **WHEN** a client supplies positive `page` and `pageSize` values
- **THEN** the API skips the preceding pages and returns the requested page in the existing raw JSON array shape

#### Scenario: Oversized page size is capped
- **WHEN** a client supplies a positive `pageSize` greater than 50
- **THEN** the API applies a page size of 50

#### Scenario: Invalid page is rejected
- **WHEN** a client supplies a `page` value less than or equal to zero
- **THEN** the API returns a validation-problem response without invoking the collection query service

#### Scenario: Invalid page size is rejected
- **WHEN** a client supplies a `pageSize` value less than or equal to zero
- **THEN** the API returns a validation-problem response without invoking the collection query service

#### Scenario: Later pages remain addressable
- **WHEN** more items exist after a returned page
- **THEN** the client can increment `page` using the same `pageSize` to request the next ordered raw-array page

### Requirement: Public collection page ordering is deterministic and efficient
The API MUST apply stable ordering and database-level bounds before materializing public game and team pages, MUST pass request cancellation through the query and enrichment flow, and MUST retain batched cross-module enrichment.

#### Scenario: Game page ordering
- **WHEN** a public game page is queried
- **THEN** games are ordered by planned start time, then name, then ID before paging

#### Scenario: Team page ordering
- **WHEN** a public team page is queried
- **THEN** active teams are ordered by name, then ID before paging

#### Scenario: Page enrichment remains batched
- **WHEN** a game or team page contains multiple items requiring cross-module display data
- **THEN** the API enriches the bounded page through batch calls rather than one call per item

#### Scenario: Overflowing page offset is safe
- **WHEN** a positive page and page size produce an offset outside the supported EF Core integer offset range
- **THEN** the API returns an empty raw JSON array without integer overflow
