## MODIFIED Requirements

### Requirement: Team roster submission and confirmation
The API MUST allow team captains to submit exact-size rosters for teams they captain, require selected non-captain members to act on their roster selection, and automatically activate the team registration when all required confirmations are complete.

#### Scenario: Captain submits exact roster
- **WHEN** an authenticated captain submits a roster for a scheduled team tournament
- **AND** the roster contains exactly the configured tournament team size
- **AND** the roster includes the captain and only current team members
- **THEN** the API creates or updates a pending team registration
- **AND** marks the captain as confirmed automatically
- **AND** sends dedicated roster-selection notifications to each selected non-captain roster member without creating team membership invites

#### Scenario: Captain omitted from roster rejected
- **WHEN** a captain submits a team tournament roster that does not include themselves
- **THEN** the API rejects the roster without creating or updating the registration

#### Scenario: Smaller roster rejected
- **WHEN** a captain submits fewer roster members than the configured tournament team size
- **THEN** the API rejects the roster without changing tournament participation

#### Scenario: Oversized roster rejected
- **WHEN** a captain submits more roster members than the configured tournament team size
- **THEN** the API rejects the roster without changing tournament participation

#### Scenario: Non-captain cannot submit team roster
- **WHEN** an authenticated user who is not the team captain attempts to submit a roster for that team
- **THEN** the API rejects the request without creating or updating a registration

#### Scenario: Roster member must belong to team
- **WHEN** a captain selects a user who is not a current member of the registered team
- **THEN** the API rejects the roster without creating or updating the registration

#### Scenario: Member confirms roster selection
- **WHEN** a selected non-captain roster member submits a `Confirm` action for a valid pending roster selection
- **THEN** the API marks that member's roster selection as confirmed
- **AND** removes the consumed roster-selection notification from actionable notification state

#### Scenario: Member declines roster selection
- **WHEN** a selected non-captain roster member submits a `Decline` action for a valid pending roster selection
- **THEN** the API removes the pending team registration and its pending roster selections from actionable state
- **AND** the declined selection no longer blocks that member from participating in the same tournament through another valid registration

#### Scenario: Team activates when all members confirm
- **WHEN** all selected non-captain roster members have confirmed their valid pending roster selections
- **THEN** the API automatically changes the team registration to active
- **AND** the team is added to active tournament participation

#### Scenario: Confirmation rechecks eligibility
- **WHEN** a selected member confirms a pending roster selection
- **THEN** the API rechecks tournament, team, roster, and duplicate-participation eligibility before accepting the confirmation

#### Scenario: Action on withdrawn selection rejected
- **WHEN** a user attempts to act on a roster selection that was deleted or withdrawn by a roster edit, team unregistration, admin removal, or decline
- **THEN** the API rejects the action without changing tournament participation

### Requirement: Duplicate participation prevention
The API MUST prevent a user from pending or active participation more than once in the same tournament across individual registrations, captain participation, and team roster membership, except that a user's pending roster selections for the tournament MUST be declined before they submit a valid roster for a team they captain.

#### Scenario: Team member already registered individually
- **WHEN** a captain selects a roster member who already has an active individual registration for the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Member already pending on another roster
- **WHEN** a captain selects a roster member who has a pending roster selection for another team in the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Member already active on another roster
- **WHEN** a captain selects a roster member who is already active on another team registration for the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Captain pending on another roster registers own team
- **WHEN** a captain submits a roster for their team while they have pending roster selections for another team in the same tournament
- **THEN** the API declines those pending roster selections before validating and creating the captain's team registration

#### Scenario: Captain already actively participating through another team
- **WHEN** a captain attempts to submit a team roster while actively participating in the same tournament through another team registration
- **THEN** the API rejects the team registration

#### Scenario: Individual registration blocked by pending roster
- **WHEN** a user attempts to register individually while they have a pending roster selection in the same tournament
- **THEN** the API rejects the individual registration

#### Scenario: Individual registration blocked by active roster
- **WHEN** a user attempts to register individually while actively participating through a team roster in the same tournament
- **THEN** the API rejects the individual registration

#### Scenario: Concurrent duplicate requests safe
- **WHEN** concurrent requests attempt to create duplicate pending or active participation for the same tournament and user
- **THEN** at most one request succeeds
- **AND** the API preserves a single pending or active participation record for that tournament and user
