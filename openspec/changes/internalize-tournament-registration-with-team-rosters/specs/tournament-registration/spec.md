## ADDED Requirements

### Requirement: Internal individual registration
The API MUST allow authenticated users to register and unregister themselves for individual tournaments through internal registration endpoints.

#### Scenario: Authenticated user registers self
- **WHEN** an authenticated user confirms registration for a scheduled individual tournament
- **THEN** the API creates an active registration for that user
- **AND** the registration response identifies the tournament and the registered user using privacy-safe fields

#### Scenario: Anonymous user cannot register
- **WHEN** an anonymous client attempts to register for a tournament
- **THEN** the API rejects the request as unauthorized

#### Scenario: Duplicate individual registration blocked
- **WHEN** a user already has active or pending participation for a tournament
- **THEN** the API rejects individual registration for the same tournament without creating a duplicate

#### Scenario: User unregisters before start
- **WHEN** an authenticated user has an active individual registration for a scheduled tournament
- **AND** tournament state allows self-unregistration
- **THEN** the API marks the registration inactive or removed for active participation

#### Scenario: User cannot unregister after start
- **WHEN** an authenticated user attempts to unregister from an in-progress or completed tournament
- **THEN** the API rejects the request unless an admin removes the user from the tournament

### Requirement: Team roster submission and confirmation
The API MUST allow team captains to submit exact-size rosters for teams they captain, require selected non-captain members to confirm, and automatically activate the team registration when all required confirmations are complete.

#### Scenario: Captain submits exact roster
- **WHEN** an authenticated captain submits a roster for a scheduled team tournament
- **AND** the roster contains exactly the configured tournament team size
- **AND** the roster includes the captain and only current team members
- **THEN** the API creates or updates a pending team registration
- **AND** marks the captain as confirmed automatically
- **AND** sends confirmation notifications to each selected non-captain roster member through the existing notification delivery infrastructure

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
- **WHEN** a selected non-captain roster member confirms a valid pending roster notification
- **THEN** the API marks that member's roster selection as confirmed

#### Scenario: Team activates when all members confirm
- **WHEN** all selected non-captain roster members have confirmed their valid pending roster notifications
- **THEN** the API automatically changes the team registration to active
- **AND** the team is added to active tournament participation

#### Scenario: Confirmation rechecks eligibility
- **WHEN** a selected member confirms a pending roster notification
- **THEN** the API rechecks tournament, team, roster, and duplicate-participation eligibility before accepting the confirmation

#### Scenario: Confirming withdrawn notification rejected
- **WHEN** a user attempts to confirm a roster notification that was deleted or withdrawn by a roster edit, team unregistration, or admin removal
- **THEN** the API rejects the confirmation without changing tournament participation

### Requirement: Captain roster control
The API MUST allow only the captain to edit a team roster before tournament start, and confirmed members MUST NOT be able to leave the roster on their own.

#### Scenario: Captain edits roster before start
- **WHEN** a captain updates a pending or active team roster before the tournament starts
- **THEN** the API validates the new exact-size roster
- **AND** removes or withdraws previous pending confirmations for replaced or changed roster entries
- **AND** sends new confirmation notifications to selected non-captain members who need confirmation

#### Scenario: Captain cannot edit roster after start
- **WHEN** a captain attempts to edit the roster after the tournament has started
- **THEN** the API rejects the request

#### Scenario: Confirmed member cannot leave roster
- **WHEN** a confirmed roster member attempts to remove themselves from a team tournament roster
- **THEN** the API rejects the request without changing roster or registration state

#### Scenario: Pending selected member cannot remove themselves directly
- **WHEN** a selected member with a pending roster confirmation attempts to remove themselves through a roster leave endpoint
- **THEN** the API rejects the request and requires the captain to edit the roster or unregister the team

#### Scenario: Captain unregisters team before start
- **WHEN** a captain unregisters their pending or active team registration before the tournament starts
- **THEN** the API removes the team from tournament participation
- **AND** removes related pending roster confirmations and confirmation notifications for that team registration

### Requirement: Exact tournament team size configuration
Admins MUST configure an exact roster size for team tournaments.

#### Scenario: Admin configures exact team size
- **WHEN** an admin creates or updates a team tournament with a valid team size
- **THEN** the API stores the team size as the exact required roster size for that tournament

#### Scenario: Individual tournament does not require team size
- **WHEN** an admin creates or updates an individual tournament
- **THEN** the API does not require a team size for registration eligibility

#### Scenario: Invalid team size rejected
- **WHEN** an admin creates or updates a team tournament with a missing, zero, or negative team size
- **THEN** the API rejects the request with validation feedback

#### Scenario: Team size locked after registration or match generation
- **WHEN** a team tournament already has pending registrations, active registrations, or generated matches
- **THEN** the API rejects team size changes

### Requirement: Duplicate participation prevention
The API MUST prevent a user from pending or active participation more than once in the same tournament across individual registrations, captain participation, and team roster membership.

#### Scenario: Team member already registered individually
- **WHEN** a captain selects a roster member who already has an active individual registration for the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Member already pending on another roster
- **WHEN** a captain selects a roster member who has a pending roster confirmation for another team in the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Member already active on another roster
- **WHEN** a captain selects a roster member who is already active on another team registration for the same tournament
- **THEN** the API rejects the team roster change

#### Scenario: Captain already participating through another team
- **WHEN** a captain attempts to submit a team roster while already pending or actively participating in the same tournament through another team registration
- **THEN** the API rejects the team registration

#### Scenario: Individual registration blocked by pending roster
- **WHEN** a user attempts to register individually while they have a pending roster confirmation in the same tournament
- **THEN** the API rejects the individual registration

#### Scenario: Individual registration blocked by active roster
- **WHEN** a user attempts to register individually while actively participating through a team roster in the same tournament
- **THEN** the API rejects the individual registration

#### Scenario: Concurrent duplicate requests safe
- **WHEN** concurrent requests attempt to create duplicate pending or active participation for the same tournament and user
- **THEN** at most one request succeeds
- **AND** the API preserves a single pending or active participation record for that tournament and user

### Requirement: Eligibility check endpoints
The API MUST expose REST endpoints that let the front-end quickly validate tournament eligibility before attempting registration or roster mutations.

#### Scenario: Current user individual eligibility checked
- **WHEN** an authenticated user requests their eligibility for an individual tournament
- **THEN** the API returns whether the user is eligible
- **AND** returns machine-readable reason codes when the user is not eligible

#### Scenario: Team registration eligibility checked
- **WHEN** a captain requests eligibility for registering a team in a team tournament
- **THEN** the API returns whether the team can submit a roster
- **AND** returns machine-readable reason codes for tournament state, captain authority, deleted team, exact team-size, or duplicate participation failures

#### Scenario: Roster candidate eligibility checked
- **WHEN** a captain requests eligibility for proposed roster members
- **THEN** the API returns per-user eligibility results using privacy-safe user identifiers
- **AND** identifies duplicate participation, non-membership, deleted user, and exact-size validation failures

#### Scenario: Eligibility endpoint does not replace mutation validation
- **WHEN** a client performs a registration, confirmation, roster edit, or removal after calling an eligibility endpoint
- **THEN** the API revalidates all eligibility and authorization rules during the mutation

### Requirement: Admin registration management
Admins MUST be able to inspect registrations and remove users or teams from tournaments, but MUST NOT add users, add teams, swap roster members, or force confirmations.

#### Scenario: Admin lists registrations
- **WHEN** an admin requests tournament registrations for a tournament
- **THEN** the API returns current individual registrations, pending team registrations, active team registrations, roster confirmation state, and registration state

#### Scenario: Admin removes individual user from individual tournament
- **WHEN** an admin removes a user from an individual tournament
- **THEN** the API removes the user's individual registration

#### Scenario: Admin cannot remove single pending roster member
- **WHEN** an admin attempts to remove one selected member from a pending team tournament roster
- **THEN** the API does not expose an endpoint for that operation
- **AND** the admin must remove the pending team registration if the roster must be invalidated

#### Scenario: Admin removes team from tournament
- **WHEN** an admin removes a team from a tournament
- **THEN** the API removes the team's pending or active registration
- **AND** removes related pending roster confirmations and confirmation notifications for that team registration

#### Scenario: Admin cannot add or swap roster members
- **WHEN** an admin attempts to add a user, add a team, swap roster members, or force-confirm a roster member
- **THEN** the API does not expose an endpoint for that operation

#### Scenario: Non-admin cannot use admin removals
- **WHEN** a non-admin client calls an admin registration removal endpoint
- **THEN** the API rejects the request as forbidden

### Requirement: External registration URL removal
The API MUST remove external registration URL behavior from the primary tournament registration model.

#### Scenario: Tournament creation does not accept registration URL
- **WHEN** an admin creates a tournament
- **THEN** the API does not require or accept an external registration URL as part of the tournament registration model

#### Scenario: Tournament update does not preserve registration URL
- **WHEN** an admin updates a tournament
- **THEN** the API does not require, accept, or return an external registration URL as registration metadata

#### Scenario: Registration does not depend on URL
- **WHEN** an authenticated user or captain registers for a tournament
- **THEN** the API evaluates the internal registration request without reading external registration URL data

### Requirement: Registration projections and privacy
The API MUST expose registration views that are efficient and privacy-safe for the caller's authorization level.

#### Scenario: Public registration projection omits private user fields
- **WHEN** an anonymous or public client reads tournament details with registrations
- **THEN** embedded users and roster members omit email, Auth0 IDs, deleted state, timestamps, confirmation tokens, notification identifiers, and private account metadata

#### Scenario: Current user registration state returned
- **WHEN** an authenticated user reads registration state for a tournament
- **THEN** the API identifies whether the current user is registered individually, has a pending roster confirmation, is confirmed on an active roster, can unregister, can confirm, or can manage a captained team registration

#### Scenario: Registration list avoids N+1 queries
- **WHEN** the API returns tournament registration lists, eligibility responses, or public registration projections
- **THEN** it retrieves registration, confirmation, team, and roster data using bounded projections or includes instead of per-row follow-up queries

#### Scenario: Public active rosters returned
- **WHEN** a public client reads tournament details with active team registrations
- **THEN** the API returns the full active roster for each active team registration using privacy-safe member fields

### Requirement: Transient registration data cleanup
The API MUST physically remove transient pending registration, roster confirmation, and notification data when it is no longer actionable.

#### Scenario: Team unregister cleanup
- **WHEN** a captain unregisters a pending team registration before tournament start
- **THEN** the API deletes the pending team registration, roster member confirmation rows, and related roster confirmation notifications

#### Scenario: Roster replacement cleanup
- **WHEN** a captain replaces a pending roster before tournament start
- **THEN** the API deletes or withdraws obsolete roster member confirmation rows and related notification records before creating the new pending confirmation set

#### Scenario: Admin pending team removal cleanup
- **WHEN** an admin removes a pending team registration
- **THEN** the API deletes the pending team registration, roster member confirmation rows, and related roster confirmation notifications

#### Scenario: Cleanup keeps registration queries bounded
- **WHEN** registration, eligibility, or current-user notification queries run
- **THEN** they filter by tournament, user, and active or pending state using indexed predicates
- **AND** they do not scan stale withdrawn confirmation records

### Requirement: Legacy admin route removal
The API MUST remove unused legacy admin participant mutation routes that bypass internal registration and confirmation rules.

#### Scenario: Legacy user registration route removed
- **WHEN** a client calls the previous admin route for directly adding or removing a user from a game's registered users
- **THEN** the API no longer exposes that route

#### Scenario: Legacy team registration route removed
- **WHEN** a client calls the previous admin route for directly adding or removing a team from a game's registered teams
- **THEN** the API no longer exposes that route
