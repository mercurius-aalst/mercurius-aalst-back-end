## Purpose

Define authenticated self-service team ownership, membership, invites, captain transfer, team logo management, and related real-time update behavior.

## Requirements

### Requirement: Authenticated team creation
The API MUST allow an authenticated user to create a team and MUST make that user the team's captain and a team member.

#### Scenario: Current user creates team
- **WHEN** an authenticated user creates a team with a valid name
- **THEN** the API creates the team with the current user as captain and member

#### Scenario: Anonymous user cannot create team
- **WHEN** an anonymous client attempts to create a team
- **THEN** the API rejects the request as unauthorized

#### Scenario: Captain limit enforced
- **WHEN** a user already captains two teams and attempts to create another team
- **THEN** the API rejects the request without creating a third captained team

### Requirement: Captain-only team mutations
The API MUST allow only the current team captain to invite users, cancel team invitations, transfer captainship, and manage the team logo.

#### Scenario: Captain mutates own team
- **WHEN** the current user is the team captain and performs a captain-only mutation
- **THEN** the API applies the mutation when all validation rules pass

#### Scenario: Non-captain mutation rejected
- **WHEN** the current user is not the team captain and attempts a captain-only mutation
- **THEN** the API rejects the request without changing team state

#### Scenario: Missing team rejected
- **WHEN** the requested team does not exist
- **THEN** the API returns a not-found response

### Requirement: Team invitation lifecycle
The API MUST track team invitations with Pending, Accepted, Declined, Cancelled, and Expired states.

#### Scenario: Captain sends invite
- **WHEN** a captain invites an existing user who is not already a team member
- **THEN** the API creates a pending invite for that user and team

#### Scenario: Duplicate pending invite blocked
- **WHEN** a pending invite already exists for the same user and team
- **THEN** the API rejects another invite for that user and team

#### Scenario: Declined invite resend allowance not exhausted
- **WHEN** a user declined a team invite and the configured declined-invite resend allowance has not been exhausted
- **THEN** the API allows the captain to send another invite for that user and team when no pending invite exists

#### Scenario: Declined invite cooldown enforced after resend allowance
- **WHEN** a user has declined the configured number of resend attempts for a team and the resend cooldown has not elapsed
- **THEN** the API rejects a new invite for that user and team

#### Scenario: Captain cancels pending invite
- **WHEN** the team captain cancels a pending invite for their team
- **THEN** the API marks the invite as cancelled and excludes it from recipient pending-invite lists

#### Scenario: Accepted or declined invite cannot be cancelled
- **WHEN** a captain attempts to cancel an invite that is not pending
- **THEN** the API rejects the cancellation without changing the invite status

#### Scenario: Pending invite expires
- **WHEN** a pending invite reaches its configured expiration time
- **THEN** the API treats the invite as expired and excludes it from actionable pending invite lists

#### Scenario: Expired invite cannot be answered
- **WHEN** a user attempts to accept or decline an expired invite
- **THEN** the API rejects the request without changing membership

### Requirement: Invite retention
The API MUST retain invitation records only while needed for pending actions, anti-spam cooldown, or audit, and MUST provide a cleanup path for expired terminal invites.

#### Scenario: Terminal invite retained during cooldown
- **WHEN** an invite is declined, cancelled, accepted, or expired and is still within its configured retention or cooldown window
- **THEN** the API keeps the invite record for validation, audit, and anti-spam decisions

#### Scenario: Terminal invite eligible for cleanup
- **WHEN** an invite is no longer pending and its configured retention and cooldown windows have elapsed
- **THEN** the invite is eligible for cleanup and MUST no longer appear in user or captain invite projections

### Requirement: Invite recipient responses
The API MUST allow invited users to accept or decline only their own pending invitations.

#### Scenario: Recipient accepts invite
- **WHEN** the invite recipient accepts a pending invite
- **THEN** the API marks the invite accepted and adds the user as a team member

#### Scenario: Recipient declines invite
- **WHEN** the invite recipient declines a pending invite
- **THEN** the API marks the invite declined and does not add the user as a team member

#### Scenario: Different user cannot respond
- **WHEN** a user attempts to accept or decline an invite addressed to another user
- **THEN** the API rejects the request without changing the invite or membership

#### Scenario: Non-pending invite cannot be answered
- **WHEN** a user attempts to answer an invite that is accepted, declined, or cancelled
- **THEN** the API rejects the request without changing the invite or membership

### Requirement: Team member leave rules
The API MUST allow a team member to leave only when leaving is not blocked by captainship or protected tournament roster rules.

#### Scenario: Non-captain member leaves safe team
- **WHEN** a non-captain member leaves a team that is not blocked by ongoing tournament roster rules
- **THEN** the API removes the user from the team members

#### Scenario: Captain cannot leave while still captain
- **WHEN** the current team captain attempts to leave the team without transferring captainship
- **THEN** the API rejects the request and leaves the captain as a member

#### Scenario: In-progress tournament roster blocks leave
- **WHEN** a team member attempts to leave while the team is part of an InProgress tournament roster
- **THEN** the API rejects the request and preserves team membership

#### Scenario: Completed tournament roster blocks leave
- **WHEN** a team member attempts to leave while the team is part of a Completed tournament roster
- **THEN** the API rejects the request and preserves team membership

#### Scenario: Canceled tournament roster blocks leave
- **WHEN** a team member attempts to leave while the team is part of a Canceled tournament roster
- **THEN** the API rejects the request and preserves team membership

#### Scenario: Unprotected tournament state allows leave
- **WHEN** a team member leaves and no InProgress, Completed, or Canceled tournament registrations reference the team
- **THEN** the API allows the member to leave when no other leave rule blocks it

### Requirement: Captain transfer
The API MUST allow a team captain to transfer captainship to exactly one current team member.

#### Scenario: Captain transfers to member
- **WHEN** the captain transfers captainship to another current team member
- **THEN** the API updates the team captain to that member and keeps both users as team members

#### Scenario: Transfer to non-member rejected
- **WHEN** the captain attempts to transfer captainship to a user who is not a current team member
- **THEN** the API rejects the transfer without changing the captain

#### Scenario: Transfer preserves single captain
- **WHEN** a captain transfer succeeds
- **THEN** the team has exactly one captain after the change

#### Scenario: Recipient captain limit enforced
- **WHEN** the target member already captains two teams
- **THEN** the API rejects the captain transfer without changing the captain

### Requirement: Team logo management
The API MUST allow a team captain to upload, replace, and remove an optional team logo with server-side validation and safe serving behavior.

#### Scenario: Captain uploads valid logo
- **WHEN** the captain uploads a valid supported image within the configured size limit
- **THEN** the API stores the logo with a server-generated safe path or key and associates it with the team

#### Scenario: Captain replaces logo
- **WHEN** the captain uploads a new valid logo for a team that already has one
- **THEN** the API associates the new logo with the team and no longer exposes the previous logo as the active logo

#### Scenario: Captain removes logo
- **WHEN** the captain removes the team's logo
- **THEN** the API clears the team's active logo reference

#### Scenario: Invalid logo rejected
- **WHEN** the uploaded file has an unsupported type, unsafe content, unsafe storage path, or exceeds the configured size limit
- **THEN** the API rejects the upload without changing the team's active logo

#### Scenario: Logo served as inert image content
- **WHEN** a client retrieves a stored team logo
- **THEN** the API or static file configuration serves it as image content that cannot execute script or active content

### Requirement: Current-user team and invite projections
The API MUST expose authenticated current-user projections for captained teams, member teams, received pending invites, and pending invites sent for teams captained by the current user.

#### Scenario: Captained teams returned
- **WHEN** an authenticated user requests their team management summary
- **THEN** the response includes teams where the user is captain

#### Scenario: Member teams returned
- **WHEN** an authenticated user requests their team management summary
- **THEN** the response includes teams where the user is a member

#### Scenario: Received pending invites returned
- **WHEN** an authenticated user requests their team management summary
- **THEN** the response includes pending invites addressed to that user

#### Scenario: Sent pending invites returned for captained teams
- **WHEN** an authenticated captain requests their team management summary
- **THEN** the response includes pending invites sent for teams captained by that user

#### Scenario: Projection omits private user fields
- **WHEN** the API returns current-user team or invite projections
- **THEN** the response omits email, Auth0 IDs, deleted state, and private account metadata for other users

### Requirement: Real-time team management events
The API MUST publish privacy-safe real-time events for successful team invite, membership, and captain transfer changes.

#### Scenario: Invite event published
- **WHEN** an invite is created, cancelled, accepted, declined, or expired
- **THEN** authorized affected clients can receive a real-time event describing the invite change without private user fields

#### Scenario: Membership event published
- **WHEN** a member joins through invite acceptance or leaves a team
- **THEN** authorized affected clients can receive a real-time event describing the membership change without private user fields

#### Scenario: Captain transfer event published
- **WHEN** captainship is transferred successfully
- **THEN** authorized affected clients can receive a real-time event identifying the team and new captain by safe public fields

#### Scenario: Unauthorized client does not receive event
- **WHEN** a client is not authorized to know about a team or invite
- **THEN** the API does not send that client private team-management event details

### Requirement: Admin team endpoint retirement
The API MUST remove admin-only team management endpoints that duplicate user-owned team management flows.

#### Scenario: Admin-only create endpoint removed
- **WHEN** a client calls the previous admin-only team creation endpoint
- **THEN** the API no longer exposes that admin-only mutation route

#### Scenario: Admin-only member mutation endpoints removed
- **WHEN** a client calls previous admin-only team member or invite mutation endpoints
- **THEN** the API no longer exposes those admin-only mutation routes

### Requirement: Efficient team management queries
The API MUST query team membership, invite, and roster state efficiently for user-owned team management flows.

#### Scenario: Team and invite listing avoids N+1 loading
- **WHEN** a user requests team and invite projections
- **THEN** the API retrieves the data using bounded projections or includes instead of per-row follow-up queries

#### Scenario: Roster blocking check is targeted
- **WHEN** the API checks whether a member can leave a team
- **THEN** it queries only the tournament roster state needed to decide whether leaving is allowed
