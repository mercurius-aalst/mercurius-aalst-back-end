## MODIFIED Requirements

### Requirement: Captain member removal
The API MUST allow a team captain to remove a non-captain member from their team only when the removal is not blocked by the member's pending or active tournament roster participation.

#### Scenario: Captain removes member
- **WHEN** the current user is the team captain
- **AND** the target user is a non-captain member of the team
- **AND** the target user is not pending confirmation or confirmed on a protected team tournament roster for that team
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

#### Scenario: Pending roster confirmation blocks member removal
- **WHEN** the target member has a pending roster confirmation for this team in a scheduled team tournament
- **THEN** the API rejects team member removal until the captain edits or unregisters the tournament roster

#### Scenario: Confirmed scheduled roster blocks member removal
- **WHEN** the target member is confirmed on an active roster for this team in a scheduled team tournament
- **THEN** the API rejects team member removal until the captain edits or unregisters the tournament roster

#### Scenario: In-progress tournament roster blocks removal
- **WHEN** the target member is confirmed on a roster for this team in an in-progress team tournament
- **THEN** the API rejects member removal and preserves team membership

#### Scenario: Completed or canceled tournament roster allows removal
- **WHEN** the target member is referenced only by completed or canceled tournament registration history where historical roster preservation does not require active team membership
- **THEN** the API allows removal when no other removal rule blocks it

### Requirement: Team member leave rules
The API MUST allow a team member to leave only when leaving is not blocked by captainship or protected tournament roster rules.

#### Scenario: Non-captain member leaves safe team
- **WHEN** a non-captain member leaves a team that is not blocked by pending or active tournament roster rules
- **THEN** the API removes the user from the team members

#### Scenario: Captain cannot leave while still captain
- **WHEN** the current team captain attempts to leave the team without transferring captainship
- **THEN** the API rejects the request and leaves the captain as a member

#### Scenario: Pending roster confirmation blocks team leave
- **WHEN** a team member attempts to leave while they have a pending roster confirmation for the team in a scheduled tournament
- **THEN** the API rejects the request and requires the captain to edit or unregister the tournament roster first

#### Scenario: Confirmed scheduled roster blocks team leave
- **WHEN** a team member attempts to leave while they are confirmed on an active roster for the team in a scheduled tournament
- **THEN** the API rejects the request and requires the captain to edit or unregister the tournament roster first

#### Scenario: In-progress tournament roster blocks leave
- **WHEN** a team member attempts to leave while they are confirmed on a roster for the team in an InProgress tournament registration
- **THEN** the API rejects the request and preserves team membership

#### Scenario: Completed tournament roster does not require active team membership
- **WHEN** a team member attempts to leave while they are referenced only by Completed tournament registration history
- **THEN** the API allows the member to leave when no other leave rule blocks it

#### Scenario: Canceled tournament roster does not require active team membership
- **WHEN** a team member attempts to leave while they are referenced only by Canceled tournament registration history
- **THEN** the API allows the member to leave when no other leave rule blocks it

#### Scenario: Unprotected tournament state allows leave
- **WHEN** a team member leaves and no pending or active protected registration roster references the member for that team
- **THEN** the API allows the member to leave when no other leave rule blocks it

### Requirement: Efficient team management queries
The API MUST query team membership, invite, and roster state efficiently for user-owned team management flows.

#### Scenario: Team and invite listing avoids N+1 loading
- **WHEN** a user requests team and invite projections
- **THEN** the API retrieves the data using bounded projections or includes instead of per-row follow-up queries

#### Scenario: Roster blocking check is targeted
- **WHEN** the API checks whether a member can leave or be removed from a team
- **THEN** it queries only the pending and active tournament registration, confirmation, and roster state needed to decide whether the action is allowed

#### Scenario: Registration summaries use bounded projections
- **WHEN** the API includes team tournament registration summaries in current-user team management views
- **THEN** it retrieves pending confirmations, active registrations, full active rosters, and roster counts using bounded projections rather than per-team follow-up queries

### Requirement: Captain-owned team deletion
The API MUST allow a team captain to delete their team only when deletion preserves historical data and does not disrupt pending or active participation.

#### Scenario: Captain deletes inactive team
- **WHEN** the current user is the captain of a team that is not pending or actively participating in team games or tournaments
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

#### Scenario: Pending tournament registration blocks delete
- **WHEN** a team has a pending team tournament registration or pending roster confirmations
- **THEN** the API rejects deletion and preserves the team as active

#### Scenario: Active participation blocks delete
- **WHEN** a team has an active Scheduled or InProgress team tournament registration
- **THEN** the API rejects deletion and preserves the team as active

#### Scenario: Completed or canceled participation allows delete
- **WHEN** a team is referenced only by Completed or Canceled team games, tournaments, matches, placements, or registration history
- **THEN** the API allows deletion when no other deletion rule blocks it

#### Scenario: Deleted team hidden from active team surfaces
- **WHEN** a team has been deleted
- **THEN** active team listing, lookup, team-name search, public profile, and current-user team-management projections MUST exclude the team

#### Scenario: Historical deleted team references remain readable
- **WHEN** a deleted team is referenced by completed or canceled games, matches, placements, or registrations
- **THEN** those historical records MAY continue to reference the deleted team row without exposing the original team name, logo, captain, members, invite data, confirmation data, or private registration metadata
