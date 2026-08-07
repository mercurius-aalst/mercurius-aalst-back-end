## Purpose

Define Competition module ownership, explicit module dependencies, event publication, and HTTP stability.

## Requirements

### Requirement: Competition owns tournament lifecycle implementation
The Competition module MUST own game, match, placement, tournament registration, roster member,
and bracket implementation types and MUST expose only intentional contracts and composition
extensions publicly.

#### Scenario: API composes Competition
- **WHEN** the API application starts
- **THEN** it registers Competition services through `AddCompetitionModule`
- **AND** maps all existing game, match, and registration routes through `MapCompetitionModule`

#### Scenario: Implementation types remain internal
- **WHEN** another module references Competition
- **THEN** Competition entities, EF adapters, bracket moderators, and application services are not
  available as public API

### Requirement: Competition uses explicit module contracts
The Competition module MUST use Teams, Identity, Sponsorship, and Media contracts for facts owned by
those capabilities and MUST NOT reference their implementation projects, EF entities, DbContexts,
repositories, or internal services.

#### Scenario: Team registration eligibility is evaluated
- **WHEN** Competition validates a team roster
- **THEN** it obtains team authority and roster data through `Teams.Contracts`
- **AND** stores only external IDs and historical snapshots needed by Competition

#### Scenario: User display data is required
- **WHEN** Competition builds registration or placement read models
- **THEN** it obtains current privacy-safe user data through bounded `Identity.Contracts` lookups
- **AND** does not load Identity EF entities

### Requirement: Competition publishes lifecycle events
Competition mutations MUST publish Competition-owned integration events through the durable module
eventing boundary in the same transaction as their persisted state.

#### Scenario: Tournament lifecycle changes
- **WHEN** a game is created, updated, started, reset, completed, or canceled
- **THEN** the corresponding Competition integration event is added to the durable outbox

#### Scenario: Registration or roster state changes
- **WHEN** a registration is created or canceled or a roster member is confirmed
- **THEN** a Competition-owned integration event is added to the durable outbox

### Requirement: Existing HTTP contracts remain stable
The extraction MUST preserve current Competition route templates, authorization metadata,
antiforgery metadata, request and response JSON shapes, and validation outcomes.

#### Scenario: Existing client uses Competition endpoints
- **WHEN** a client calls an existing game, match, or tournament-registration endpoint
- **THEN** the same route and authorization requirement apply
- **AND** the response JSON contract remains unchanged
