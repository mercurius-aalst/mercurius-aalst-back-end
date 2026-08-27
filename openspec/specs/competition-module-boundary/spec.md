## Purpose

Define Tournament module ownership, explicit module dependencies, event publication, and HTTP stability.

## Requirements

### Requirement: Tournament owns tournament lifecycle implementation
The Tournament module MUST own tournament, match, placement, tournament registration, roster member,
and bracket implementation types and MUST expose only intentional contracts and composition
extensions publicly.

#### Scenario: API composes Tournament
- **WHEN** the API application starts
- **THEN** it registers Tournament services through `AddTournamentModule`
- **AND** maps all existing tournament, match, and registration routes through `MapTournamentModule`

#### Scenario: Implementation types remain internal
- **WHEN** another module references Tournament
- **THEN** Tournament entities, EF adapters, bracket moderators, and application services are not
  available as public API

### Requirement: Tournament uses explicit module contracts
The Tournament module MUST use Teams, Identity, Sponsorship, and Media contracts for facts owned by
those capabilities and MUST NOT reference their implementation projects, EF entities, DbContexts,
repositories, or internal services.

#### Scenario: Team registration eligibility is evaluated
- **WHEN** Tournament validates a team roster
- **THEN** it obtains team authority and roster data through `Teams.Contracts`
- **AND** stores only external IDs and historical snapshots needed by Tournament

#### Scenario: User display data is required
- **WHEN** Tournament builds registration or placement read models
- **THEN** it obtains current privacy-safe user data through bounded `Identity.Contracts` lookups
- **AND** does not load Identity EF entities

### Requirement: Tournament publishes lifecycle events
Tournament mutations MUST publish Tournament-owned integration events through the durable module
eventing boundary in the same transaction as their persisted state.

#### Scenario: Tournament lifecycle changes
- **WHEN** a tournament is created, updated, started, reset, completed, or canceled
- **THEN** the corresponding Tournament integration event is added to the durable outbox

#### Scenario: Registration or roster state changes
- **WHEN** a registration is created or canceled or a roster member is confirmed
- **THEN** a Tournament-owned integration event is added to the durable outbox

### Requirement: Existing HTTP contracts remain stable
The extraction MUST preserve current Tournament route templates, authorization metadata,
antiforgery metadata, request and response JSON shapes, and validation outcomes.

#### Scenario: Existing client uses Tournament endpoints
- **WHEN** a client calls an existing tournament, match, or tournament-registration endpoint
- **THEN** the same route and authorization requirement apply
- **AND** the response JSON contract remains unchanged
