## Purpose

Define Sponsorship module ownership, boundaries, persistence, HTTP composition, and lifecycle facts.

## Requirements

### Requirement: Sponsorship owns sponsor and placement implementation
The Sponsorship module SHALL own the Sponsor and GameSponsorPlacement implementation types,
application services, validation, endpoint mapping, and persistence configuration. Its public
surface MUST be limited to intentional contracts and composition extensions.

#### Scenario: API composes Sponsorship
- **WHEN** the API application starts
- **THEN** it MUST register Sponsorship through `AddSponsorshipModule`
- **AND** map the existing sponsor routes through `MapSponsorshipModule`

#### Scenario: Sponsorship implementation remains internal
- **WHEN** another module references Sponsorship
- **THEN** Sponsor entities, EF adapters, CRUD services, and endpoint implementation types MUST
  NOT be available through the Sponsorship contracts project

### Requirement: Sponsorship uses explicit module dependencies
Sponsorship SHALL consume Competition facts only through external game identifiers and SHALL
consume image storage only through `Media.Contracts`. It MUST NOT reference Competition or Media
implementation projects, their EF entities, DbContexts, repositories, or internal services.

#### Scenario: Sponsor placement is changed for a game
- **WHEN** Competition calls `ISponsorshipModule` to replace a game's sponsor placement
- **THEN** Sponsorship MUST store the supplied game identifier as an external reference
- **AND** it MUST NOT load or navigate to a Competition Game entity

#### Scenario: Sponsor logo is stored
- **WHEN** a sponsor is created or its logo is replaced
- **THEN** Sponsorship MUST store the media URL returned through `IMediaModule`
- **AND** it MUST NOT invoke a Media implementation service directly

### Requirement: Sponsorship owns game sponsor placement behavior
Sponsorship SHALL own the one-to-one placement for each game, including sponsor identity, context,
headline, support line, and display order. A placement change MUST validate a non-null sponsor
before persisting it and MUST preserve the existing not-found validation outcome.

#### Scenario: Placement is created or replaced
- **WHEN** a valid `SponsorPlacementInput` is supplied for a game identifier
- **THEN** Sponsorship MUST create or update that game's single placement with the supplied values
- **AND** the corresponding Competition read model MUST receive the placement through
  `ISponsorshipModule`

#### Scenario: Placement is removed
- **WHEN** `null` is supplied as the placement for a game identifier
- **THEN** Sponsorship MUST remove that game's existing placement
- **AND** subsequent Sponsorship and Competition reads MUST report no placement

### Requirement: Sponsorship read models are bounded and persistence-compatible
Sponsorship read operations SHALL use no-tracking projections and SHALL provide a bounded batched
placement lookup for a supplied set of game identifiers. It MUST retain the existing `Sponsors` and
`GameSponsorPlacements` tables, scalar mapping, unique game placement constraint, and cascade
relationships without a schema migration.

#### Scenario: Competition enriches multiple games
- **WHEN** Competition requests placements for multiple game identifiers
- **THEN** Sponsorship MUST return the matching placement summaries from a bounded query
- **AND** it MUST NOT issue one placement query per game identifier

#### Scenario: Existing database is composed
- **WHEN** the shared EF model is built after the extraction
- **THEN** Sponsorship MUST map the existing Sponsor and GameSponsorPlacement schema and preserve
  the game-to-placement and sponsor-to-placement cascade behavior

### Requirement: Existing Sponsorship HTTP contracts remain stable
The extraction MUST preserve sponsor route templates, route names and tags, API version metadata,
authorization metadata, antiforgery metadata, form request binding, validation outcomes, response
JSON shapes, and anonymous read access.

#### Scenario: Existing client uses a sponsor endpoint
- **WHEN** a client calls an existing sponsor list, detail, create, update, or delete endpoint
- **THEN** the same route, authorization requirement, request shape, and response JSON contract
  MUST apply

#### Scenario: Existing client uses the game sponsor placement endpoint
- **WHEN** a client replaces a game's sponsor placement through the existing Competition endpoint
- **THEN** the existing route, authorization, validation, and game response JSON MUST remain
  unchanged

### Requirement: Sponsorship publishes lifecycle facts
Sponsorship mutations SHALL publish typed integration-event contracts in the `Contracts.V1`
namespace without exposing EF entities. Events MUST describe sponsor creation, update, deletion,
and game sponsor placement changes, including the relevant SponsorId and GameId or PlacementId
facts needed by later consumers.

#### Scenario: Sponsor metadata changes
- **WHEN** a sponsor is created, updated, or deleted
- **THEN** Sponsorship MUST publish the matching `Contracts.V1.SponsorCreated`,
  `Contracts.V1.SponsorUpdated`, or `Contracts.V1.SponsorDeleted` event through the durable module
  eventing boundary

#### Scenario: Game placement changes
- **WHEN** a game's sponsor placement is created, replaced, or removed
- **THEN** Sponsorship MUST publish a `Contracts.V1.GameSponsorPlacementChanged` event that
  identifies the game
- **AND** the event MUST represent either the current placement facts or the removal state
