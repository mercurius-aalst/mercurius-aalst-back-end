## MODIFIED Requirements

### Requirement: Admin registration management
Admins MUST be able to inspect registrations and remove users or teams from tournaments, but MUST NOT add users, add teams, swap roster members, or force confirmations. The existing admin registration list MUST support optional `page` and `pageSize` query parameters, default omitted values to page 1 and page size 20, reject non-positive values before service invocation, cap positive page size at 50, and preserve its raw-array JSON response, route, and authorization.

#### Scenario: Admin lists registrations
- **WHEN** an admin requests tournament registrations for a tournament
- **THEN** the API returns the requested bounded page of current individual registrations, pending team registrations, active team registrations, roster confirmation state, and registration state

#### Scenario: Admin lists default registration page
- **WHEN** an admin requests tournament registrations without `page` or `pageSize`
- **THEN** the API returns at most the first 20 registrations in the existing raw JSON array

#### Scenario: Admin receives validation for invalid registration page
- **WHEN** an admin requests tournament registrations with `page` or `pageSize` less than one
- **THEN** the API returns a validation problem before invoking the registration service

#### Scenario: Admin removes individual user from individual tournament
- **WHEN** an admin removes a user from an individual tournament
- **THEN** the API hard-deletes the user's individual registration

#### Scenario: Admin cannot remove single pending roster member
- **WHEN** an admin attempts to remove one selected member from a pending team tournament roster
- **THEN** the API does not expose an endpoint for that operation
- **AND** the admin must remove the pending team registration if the roster must be invalidated

#### Scenario: Admin removes team from tournament
- **WHEN** an admin removes a team from a tournament
- **THEN** the API hard-deletes the team's pending or active registration
- **AND** removes related pending roster confirmations and confirmation notifications for that team registration

#### Scenario: Admin cannot add or swap roster members
- **WHEN** an admin attempts to add a user, add a team, swap roster members, or force-confirm a roster member
- **THEN** the API does not expose an endpoint for that operation

#### Scenario: Non-admin cannot use admin removals
- **WHEN** a non-admin client calls an admin registration removal endpoint
- **THEN** the API rejects the request as forbidden
