## Why

The Competition module and its public API use `Game` for a tournament aggregate, which is
ambiguous with the genuine single-game concepts already present in match formats and schedule
estimation. Phase 20 establishes `Tournament` as the canonical aggregate name across module
boundaries, persistence, discovery, and HTTP contracts while preserving the existing behavior and
data.

## What Changes

- **BREAKING** Rename the Competition module and project/namespace surface to Tournament where
  the term represents the tournament aggregate.
- **BREAKING** Rename public `/v1/lan/games` resources and `gameId` route/JSON fields to
  `/v1/lan/tournaments` and `tournamentId`, including registrations, matches, sponsors, and
  lifecycle operations; do not retain aliases or introduce `/v2` routes.
- Rename aggregate contracts, entities, DTOs, services, commands, queries, integration events,
  sponsor-placement references, and tests from Game to Tournament while retaining genuine
  single-game concepts such as `GameFormat` and `AverageGameDurationMinutes`.
- Rename the Competition module/project and schema concept to Tournament while preserving the
  resource table name as plural `tournaments` and preserving existing authorization, privacy,
  validation, paging, lifecycle, and response semantics.
- Add a reversible, data-preserving migration from the `competition` schema and `games` table to
  the `tournament` schema and `tournaments` table, including aggregate references, sponsorship,
  and Discovery rows with canonical tournament type, subtitle, and route metadata.
- Rewrite pending, non-dead-lettered durable outbox messages to the new CLR type names and
  `tournamentId` payload key in the migration, reverse those rewrites in `Down`, and retain only
  narrow historical/dead-letter resolver safety where required.
- Update current specifications, architecture documentation, OpenAPI/route tests, module tests,
  and the Phase 20 ledger placeholder without inventing a PR URL.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `competition-module-boundary`: Rename the module's tournament aggregate and public composition
  surface while keeping explicit contract boundaries and lifecycle behavior.
- `tournament-registration`: Use tournament resources and `tournamentId` fields for all existing
  registration behavior.
- `tournament-schedule-estimation`: Clarify tournament responses while retaining single-game
  duration and format terminology.
- `discovery-search-projections`: Rename projected Game source/type/navigation concepts to
  Tournament and update persisted discovery data.
- `sponsorship-module-boundary`: Rename game sponsor placement references to tournament placement
  references and migrate their persisted foreign keys without changing placement behavior.
- `resource-oriented-api-routes`: Replace game lifecycle resource routes with tournament routes
  and remove the old game routes.

## Impact

The change affects the Competition/Tournament module project and namespace, all module contracts
and implementations that refer to the tournament aggregate, Teams and Sponsorship contracts,
Discovery projections and search navigation, the API host composition and `/v1/lan` routes, EF
configuration/model snapshot/migration history, durable event type compatibility, solution
project entries, and unit/API/integration tests. Existing databases require the reversible rename
migration; clients must move from game resource URLs and `gameId` fields to tournament resources
and `tournamentId` fields.
