## ADDED Requirements

### Requirement: Tournament roster request structure is bounded before downstream work
The API MUST validate the `userIds` collection for team-roster eligibility and submission before invoking an application service, module contract, transaction, or database operation.

#### Scenario: Missing roster collection rejected early
- **WHEN** a roster eligibility or submission request has a null or missing `userIds` collection
- **THEN** the API returns a validation-problem response keyed to `userIds`
- **AND** no roster application service or downstream dependency is invoked

#### Scenario: Oversized roster collection rejected early
- **WHEN** a roster eligibility or submission request contains more than 50 user IDs
- **THEN** the API returns a validation-problem response keyed to `userIds`
- **AND** no roster application service or downstream dependency is invoked

#### Scenario: Empty user ID rejected early
- **WHEN** a roster eligibility or submission request contains `Guid.Empty`
- **THEN** the API returns a validation-problem response keyed to `userIds`
- **AND** no roster application service or downstream dependency is invoked

#### Scenario: Duplicate roster user ID rejected early
- **WHEN** a roster eligibility or submission request contains the same user ID more than once
- **THEN** the API returns a validation-problem response keyed to `userIds` instead of silently deduplicating the roster
- **AND** no roster application service or downstream dependency is invoked

#### Scenario: Structurally valid roster reaches business validation
- **WHEN** a roster eligibility or submission request contains at most 50 unique, non-empty user IDs
- **THEN** the existing exact-size, captain, membership, lifecycle, and duplicate-participation rules remain authoritative

## MODIFIED Requirements

### Requirement: Exact tournament team size configuration
Admins MUST configure an exact roster size from 1 through 50 for team tournaments.

#### Scenario: Admin configures exact team size
- **WHEN** an admin creates or updates a team tournament with a valid team size from 1 through 50
- **THEN** the API stores the team size as the exact required roster size for that tournament

#### Scenario: Individual tournament does not require team size
- **WHEN** an admin creates or updates an individual tournament
- **THEN** the API does not require a team size for registration eligibility

#### Scenario: Invalid team size rejected
- **WHEN** an admin creates or updates a team tournament with a missing, zero, negative, or greater-than-50 team size
- **THEN** the API rejects the request with validation feedback

#### Scenario: Team size locked after registration or match generation
- **WHEN** a team tournament already has pending registrations, active registrations, or generated matches
- **THEN** the API rejects team size changes
