## ADDED Requirements

### Requirement: Captain-owned team deletion
The API MUST allow a team captain to delete their team only when deletion preserves historical data and does not disrupt active participation.

#### Scenario: Captain deletes inactive team
- **WHEN** the current user is the captain of a team that is not actively participating in team games or tournaments
- **THEN** the API marks the team as deleted without physically removing the team row
- **AND** historical match, placement, and completed or canceled registration references to the team remain intact

#### Scenario: Non-captain delete rejected
- **WHEN** the current user is not the team captain and attempts to delete the team
- **THEN** the API rejects the request without changing team state

#### Scenario: Active participation blocks delete
- **WHEN** a team is registered for a Scheduled or InProgress team game or tournament
- **THEN** the API rejects deletion and preserves the team as active

#### Scenario: Deleted team hidden from active team surfaces
- **WHEN** a team has been deleted
- **THEN** active team listing, lookup, search, public profile, and current-user team-management projections MUST exclude the team

#### Scenario: Historical deleted team references remain readable
- **WHEN** a deleted team is referenced by completed or canceled games, matches, or placements
- **THEN** those historical records MAY continue to include the deleted team's privacy-safe display data
