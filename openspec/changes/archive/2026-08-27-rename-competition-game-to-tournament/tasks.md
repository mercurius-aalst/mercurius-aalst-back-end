## 1. Inventory and OpenSpec baseline

- [x] 1.1 Inventory aggregate-level Game/Competition names, genuine single-game terminology, project references, routes, serialized fields, event registrations, migrations, and tests; preserve unrelated working-tree changes.
- [x] 1.2 Confirm the current EF model, constraint/index/FK names, durable event type catalog, and Discovery projection values that the migration and compatibility boundary must handle.
- [x] 1.3 Validate the completed proposal, design, delta specs, and task checklist with the OpenSpec CLI.

## 2. Tournament module and shared contracts

- [x] 2.1 Rename Competition module projects, solution entries/folders, namespaces, composition methods, and implementation/contract files to Tournament equivalents without retaining compatibility wrappers.
- [x] 2.2 Rename aggregate-level GameId, GameStatus, summaries, DTOs, commands, queries, services, events, sponsor-placement contracts, and search documents to Tournament equivalents while preserving GameFormat and AverageGameDurationMinutes.
- [x] 2.3 Update Teams, Sponsorship, Media, Discovery, Platform eventing, and API composition references to the new Tournament contracts and explicit module boundaries.

## 3. API routes and serialized contracts

- [x] 3.1 Rename all `/v1/lan/games` tournament-resource routes to `/v1/lan/tournaments`, including registrations, matches, sponsors, and lifecycle operations.
- [x] 3.2 Rename aggregate-level route/request/response identifiers from gameId/GameId to tournamentId/TournamentId and update route names, tags, OpenAPI metadata, and navigation paths.
- [x] 3.3 Remove old `/v1/lan/games` routes and action aliases without adding `/v2` routes; preserve authorization, antiforgery, privacy, validation, paging, lifecycle, and single-game fields.

## 4. Persistence and data migration

- [x] 4.1 Update module EF configurations, DbContext registration, current model metadata, table/schema names, keys, indexes, columns, and foreign keys to `tournament`/`tournaments` and TournamentId.
- [x] 4.2 Add a reversible hand-authored EF migration that renames competition to tournament, games to tournaments, all aggregate references and sponsorship placement identifiers, existing Discovery game rows and their canonical subtitle/route metadata for active and deleted documents, and pending outbox event type/payload representations without data loss.
- [x] 4.3 Update only the current EF model snapshot and migration/catalog registration; do not modify historical migration or designer files.
- [x] 4.4 Add migration regression coverage for data preservation, dependency-safe Up/Down behavior, schemas/tables, indexes/constraints/FKs, sponsorship, and Discovery rows including active/deleted and blank-metadata semantics.

## 5. Durable events and Discovery

- [x] 5.1 Update Tournament lifecycle, registration, sponsorship, and Discovery event contracts and handlers to publish/consume canonical Tournament names and tournamentId payloads.
- [x] 5.2 Add focused compatibility for historical/dead-lettered or rollback legacy Competition/Game CLR type strings and gameId JSON payload keys after the migration rewrites pending rows, with regression tests and no public route aliases.
- [x] 5.3 Rename Discovery entity types, source documents, result types, navigation URLs, rebuild snapshots, indexes, and projection tests from Game to Tournament while preserving search semantics and privacy.

## 6. Tests and documentation

- [x] 6.1 Rename and update module, API, OpenAPI, DTO serialization, migration, persistence-boundary, eventing, search, sponsorship, and route tests to assert canonical Tournament behavior and old-route absence.
- [x] 6.2 Update current main OpenSpec specs and architecture/implementation documentation to describe Tournament naming and migration behavior; preserve historical archived artifacts unless explicitly current.
- [x] 6.3 Add the Phase 20 progress-ledger placeholder/task for the coordinator without checking the phase or inventing a PR URL.

## 7. Validation and handoff

- [x] 7.1 Run `openspec validate rename-competition-game-to-tournament` and confirm all tasks are checked off.
- [x] 7.2 Run `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and `dotnet list package --vulnerable --include-transitive` from the repository root.
- [x] 7.3 Attempt migration Up/Down, API startup, and OpenAPI checks when local PostgreSQL/configuration permits; report exact limitations and remaining risks.
- [x] 7.4 Review git status/diff for unrelated changes, ensure no old aggregate routes or accidental single-game renames remain, and hand off without committing, pushing, or creating a PR.
