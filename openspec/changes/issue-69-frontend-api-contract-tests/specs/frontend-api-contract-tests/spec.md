## ADDED Requirements

### Requirement: Front-end API contract coverage
The test suite MUST contain explicit contract tests for each redesigned front-end API dependency.

#### Scenario: Game and schedule contracts covered
- **WHEN** the test suite runs
- **THEN** tests verify game list/detail response shape, tournament schedule fields, generated match schedule fields, and match detail response shape

#### Scenario: Sponsor and placement contracts covered
- **WHEN** the test suite runs
- **THEN** tests verify sponsor list/detail and game sponsor placement response shapes

#### Scenario: Search and profile contracts covered
- **WHEN** the test suite runs
- **THEN** tests verify public global search, public user profile, and public team profile response shapes

#### Scenario: Current-user and admin contracts covered
- **WHEN** the test suite runs
- **THEN** tests verify current-user profile completion/update/username availability flows and admin username deletion route compatibility

### Requirement: Public privacy regression coverage
The test suite MUST fail if anonymous public responses expose private user or account data.

#### Scenario: Anonymous public fields checked
- **WHEN** public game, placement, search, user profile, or team profile responses are serialized in tests
- **THEN** assertions prove they omit email, Auth0 IDs, deleted state, timestamps, invite data, and platform IDs unless explicitly allowed

#### Scenario: Deleted and incomplete users excluded
- **WHEN** deleted or incomplete users exist in test data
- **THEN** public search and profile tests prove those users are excluded

### Requirement: Security and compatibility coverage
The test suite MUST verify authorization and route compatibility for sensitive operations.

#### Scenario: Admin-only endpoints reject unauthorized clients
- **WHEN** anonymous or non-admin clients call admin-only routes
- **THEN** tests assert those requests are rejected

#### Scenario: Admin delete route compatibility
- **WHEN** the redesigned front-end calls `DELETE /v1/lan/users/{username}`
- **THEN** tests prove the route maps to the safe admin delete behavior and does not conflict with GUID routes

### Requirement: Bounded public response coverage
The test suite MUST include practical checks that prevent obvious performance regressions in public API contracts.

#### Scenario: Search result count bounded
- **WHEN** search has more matches than the configured result limit
- **THEN** tests assert the response remains bounded

#### Scenario: Public list responses avoid private payloads
- **WHEN** public list/detail responses are created
- **THEN** tests assert unnecessary private user fields are not materialized into the serialized response
