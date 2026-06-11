## ADDED Requirements

### Requirement: Captain-owned team deletion
The API MUST allow a team captain to delete their team only when deletion preserves historical data and does not disrupt active participation.

#### Scenario: Captain deletes inactive team
- **WHEN** the current user is the captain of a team that is not actively participating in team games or tournaments
- **THEN** the API marks the team as deleted without physically removing the team row
- **AND** anonymizes the team name to a generated deleted-team value
- **AND** clears the team logo, captain association, members, and invitations
- **AND** historical match, placement, and completed or canceled registration references to the team remain intact

#### Scenario: Deleted team name can be reused
- **WHEN** a team has been deleted
- **THEN** another active team MAY be created with the deleted team's previous name when no other active team uses that name

#### Scenario: Non-captain delete rejected
- **WHEN** the current user is not the team captain and attempts to delete the team
- **THEN** the API rejects the request without changing team state

#### Scenario: Active participation blocks delete
- **WHEN** a team is registered for a Scheduled or InProgress team game or tournament
- **THEN** the API rejects deletion and preserves the team as active

#### Scenario: Completed or canceled participation allows delete
- **WHEN** a team is registered only for Completed or Canceled team games or tournaments
- **THEN** the API allows deletion when no other deletion rule blocks it

#### Scenario: Deleted team hidden from active team surfaces
- **WHEN** a team has been deleted
- **THEN** active team listing, lookup, team-name search, public profile, and current-user team-management projections MUST exclude the team

#### Scenario: Historical deleted team references remain readable
- **WHEN** a deleted team is referenced by completed or canceled games, matches, or placements
- **THEN** those historical records MAY continue to reference the deleted team row without exposing the original team name, logo, captain, members, or invite data
