## MODIFIED Requirements

### Requirement: Team invitation lifecycle
The API MUST track team invitations with Pending, Accepted, Declined, Cancelled, and Expired states, and team invite clients MAY use authenticated user search to discover stable recipient user ids before sending an invite.

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

#### Scenario: User search does not replace invite validation
- **WHEN** a client sends an invite using a user id discovered through authenticated user search
- **THEN** the existing invite endpoint still enforces captainship, membership, duplicate invite, expiration, and cooldown rules before creating the invite
