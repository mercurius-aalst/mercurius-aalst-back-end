# Current team registration context

## Purpose

Defines the authenticated read model used by the tournament registration UI to explain a user's
current team ownership and roster state.

## ADDED Requirements

### Requirement: Authenticated state includes current team registration context

The authenticated current-user tournament-registration response MUST expose a
`CurrentTeamRegistration` value when the caller belongs to a team roster registration for that
tournament, regardless of whether the registration is pending confirmation or active.

#### Scenario: Confirmed member remains on a pending team roster

- **WHEN** a roster contains member A and member B, member A confirms, and member B remains pending
- **THEN** the current-user response for member A MUST include the team registration in
  `CurrentTeamRegistration`
- **AND** the value MUST include the pending registration status, team identity, and privacy-safe
  roster confirmation state
- **AND** `ActiveTeamRegistration` MAY remain null until the full roster becomes active

#### Scenario: Active team member receives current context

- **WHEN** the caller belongs to an active team registration
- **THEN** `CurrentTeamRegistration` MUST include that active team registration
- **AND** `ActiveTeamRegistration` MUST retain its existing active-registration value

#### Scenario: Caller has no team registration

- **WHEN** the caller is not a roster member of a pending or active team registration
- **THEN** `CurrentTeamRegistration` MUST be null

### Requirement: Current team context remains authorization- and privacy-safe

`CurrentTeamRegistration` MUST be returned only from the authenticated current-user registration
endpoint and MUST use the existing privacy-safe registration and public-user DTO mappings.

#### Scenario: Public tournament projection remains active-only

- **WHEN** an anonymous or public client reads tournament details
- **THEN** pending team registrations MUST remain absent from the public registrations collection
- **AND** the private `CurrentTeamRegistration` field MUST NOT be added to the public projection
