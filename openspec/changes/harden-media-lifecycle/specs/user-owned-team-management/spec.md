## ADDED Requirements

### Requirement: Team logo retirement preserves current and historical references
The API MUST retire a replaced, removed, or soft-deleted Team logo only after the Team mutation and
its durable business event commit, and MUST retain the file while any active Team or any tournament
registration snapshot references the same logo URL. Reference-check failure MUST retain the file.

#### Scenario: Unreferenced Team logo is replaced or removed
- **WHEN** a Team logo replacement or removal commits and no active Team or registration snapshot references the previous different logo URL
- **THEN** the API attempts to delete the previous logo after commit with a non-cancelled token

#### Scenario: Historical tournament snapshot references Team logo
- **WHEN** any tournament registration stores the candidate URL in `TeamLogoUrlAtRegistration`
- **THEN** Team logo replacement, removal, or soft deletion retains the physical logo file

#### Scenario: Current Team references candidate logo
- **WHEN** an active Team still uses the candidate logo URL
- **THEN** the API retains the physical logo file

#### Scenario: Team logo reference check fails
- **WHEN** a current or historical Team-logo reference query fails
- **THEN** the API logs the failure and retains the physical logo file

#### Scenario: Team soft deletion commits
- **WHEN** a Team soft deletion and its durable TeamDeleted event commit
- **THEN** the existing SignalR group access is revoked in its current post-commit order
- **AND** only then does the API attempt eligible logo retirement without changing the deletion response
