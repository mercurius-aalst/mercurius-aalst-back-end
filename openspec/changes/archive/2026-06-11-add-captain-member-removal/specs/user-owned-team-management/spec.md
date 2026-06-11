## ADDED Requirements

### Requirement: Captain member removal
The API MUST allow a team captain to remove a non-captain member from their team when the team is not registered in an in-progress team tournament.

#### Scenario: Captain removes member
- **WHEN** the current user is the team captain
- **AND** the target user is a non-captain member of the team
- **AND** the team is not registered in an in-progress team tournament
- **THEN** the API removes the target user from the team members
- **AND** publishes a privacy-safe membership removal event

#### Scenario: Anonymous user cannot remove member
- **WHEN** an anonymous client attempts to remove a team member
- **THEN** the API rejects the request as unauthorized

#### Scenario: Non-captain cannot remove member
- **WHEN** the current user is not the team captain and attempts to remove a team member
- **THEN** the API rejects the request without changing team state

#### Scenario: Captain cannot be removed
- **WHEN** the captain attempts to remove the current captain from the team
- **THEN** the API rejects the request and leaves the captain as a member

#### Scenario: Missing member rejected
- **WHEN** the captain attempts to remove a user who is not a current member
- **THEN** the API rejects the request without changing team state

#### Scenario: In-progress tournament roster blocks removal
- **WHEN** the team is registered in an in-progress team tournament
- **THEN** the API rejects member removal and preserves team membership

#### Scenario: Completed or canceled tournament roster allows removal
- **WHEN** the team is registered only in completed or canceled team tournaments
- **THEN** the API allows member removal when no other removal rule blocks it

#### Scenario: Scheduled tournament roster allows removal
- **WHEN** the team is registered only in scheduled team tournaments
- **THEN** the API allows member removal when no other removal rule blocks it

## MODIFIED Requirements

### Requirement: Captain-only team mutations
The API MUST allow only the current team captain to invite users, cancel team invitations, remove team members, transfer captainship, and manage the team logo.

#### Scenario: Non-captain member removal is rejected
- **WHEN** an authenticated user who is not the team captain attempts to remove a team member
- **THEN** the API rejects the request without changing team membership
