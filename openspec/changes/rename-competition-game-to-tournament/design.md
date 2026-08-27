## Context

The modular monolith currently calls its tournament aggregate `Game`. Competition, Sponsorship,
Teams, Discovery, durable eventing, EF mappings, tests, and the API therefore expose a mixture of
aggregate-level `Game` names and genuine single-game concepts. Phase 20 is a cross-module,
contract-visible rename on the existing `refactor/phase-20-tournament-naming` branch. The current
database uses module schemas introduced by earlier phases, and the current EF snapshot reflects
those schemas. Durable outbox rows may contain CLR type names and JSON payload keys from the old
model.

The implementation MUST preserve lifecycle rules, authorization, privacy filtering, validation,
paging, search ranking/cursors, response semantics, and relationship behavior. It MUST preserve
genuine single-game terminology, notably `GameFormat` and `AverageGameDurationMinutes`.

## Goals / Non-Goals

**Goals:**

- Rename the aggregate and all aggregate-level contracts, implementations, namespaces, projects,
  composition methods, tests, route templates, and serialized identifiers to Tournament.
- Make the Tournament module, `tournament` schema, `tournaments` resource table, and
  `tournamentId` the canonical names.
- Rename Discovery's source type/navigation and Sponsorship's placement references consistently.
- Provide a reversible, data-preserving PostgreSQL migration and update only the current EF model
  snapshot; historical migrations and their designers MUST remain unchanged.
- Rewrite pending, non-dead-lettered durable outbox messages to the new CLR type names and
  `tournamentId` payload keys in the reversible migration, while retaining only focused resolver
  handling for historical/dead-lettered or rollback rows that cannot be rewritten safely.
- Update main specs, architecture docs, tests, and the active OpenSpec task checklist.

**Non-Goals:**

- Changing tournament lifecycle rules, authorization policies, privacy rules, validation semantics,
  paging algorithms, search relevance, or JSON fields unrelated to the aggregate rename.
- Renaming genuine one-match/single-game concepts such as `GameFormat`,
  `AverageGameDurationMinutes`, match game counts, or format enum values.
- Adding route aliases, compatibility `/v2` routes, a parallel aggregate, or unnecessary wrapper
  types.
- Rewriting historical migrations or historical migration designer files.
- Creating or claiming a Phase 20 pull request; the progress ledger will contain a coordinator
  placeholder until a real PR URL exists.

## Decisions

### 1. Perform a semantic rename, not a text-only replacement

Every `Game` occurrence MUST be classified as aggregate-level or genuine single-game language.
Aggregate-level types become `Tournament` equivalents, including `GameId` → `TournamentId`,
`GameStatus` → `TournamentStatus`, summaries, DTOs, commands, queries, services, events, sponsor
placements, and search documents. `GameFormat` and `AverageGameDurationMinutes` remain unchanged.

**Alternative considered:** Keep `Game` internally and add Tournament wrappers. This would preserve
the ambiguous domain model and create dual public concepts, so it is rejected.

### 2. Rename module and API resources in place

The Competition project, contracts, implementation namespaces, solution folders, registration
methods, and tests MUST become Tournament equivalents. The API MUST expose
`/v1/lan/tournaments` routes with `tournamentId` fields and MUST remove the corresponding
`/v1/lan/games` routes. No aliases or `/v2` compatibility strategy is permitted.

**Alternative considered:** Retain old routes as aliases for clients. This contradicts the locked
in-place cleanup and would leave the ambiguity in the public contract, so it is rejected.

### 3. Preserve data by renaming PostgreSQL identifiers in one reversible migration

The hand-authored migration MUST rename the `competition` schema to `tournament`, rename the
`games` resource table to `tournaments`, rename aggregate-level columns/constraints/indexes/FKs
from `GameId` to `TournamentId`, rename sponsorship placement identifiers, and update existing
Discovery rows from `game` to `tournament`. `Down` MUST reverse each operation in dependency-safe
order. The current model snapshot MUST be updated to match the new model; historical migrations
and designers MUST not be edited.

**Alternative considered:** Drop/recreate tables or introduce a new table and copy data. Renaming
preserves keys, data, indexes, and foreign keys more directly and makes rollback safer, so it is
rejected.

### 4. Rewrite durable events in the migration and keep a narrow safety boundary

The migration MUST rewrite pending, non-dead-lettered outbox rows: legacy Competition/Game CLR
type strings become Tournament names and aggregate-level `gameId` payload keys become
`tournamentId`. `Down` MUST reverse those transformations for pending, non-dead-lettered rows.
The dispatcher/type catalog MAY retain a focused alias and payload normalization path only for
historical, dead-lettered, or rollback rows that still contain legacy representations. This path
MUST NOT be used as a broad permanent compatibility shim, and public HTTP aliases and old
contracts MUST NOT be retained. Newly published events MUST use the new names and payload shape.

**Alternative considered:** Rely only on dispatch-time compatibility. That would leave pending
rows in legacy storage indefinitely and would not satisfy the reversible data migration, so it is
rejected.

### 5. Update specifications and tests as contract evidence

The six affected main capabilities MUST receive complete delta requirements with exact scenario
headers. Route/OpenAPI/DTO/privacy tests MUST assert the new canonical names and absence of old
routes. Migration tests MUST verify reversible renames, data preservation, FK/index names, and
Discovery/outbox compatibility where local infrastructure permits.

## Risks / Trade-offs

- [Risk] A broad semantic rename can accidentally change genuine single-game terminology. → Use a
  classification pass, preserve `GameFormat` and `AverageGameDurationMinutes`, and search for
  residual aggregate-level names after compilation.
- [Risk] Existing databases can have dependent FKs, indexes, or constraints whose generated names
  differ across environments. → Use explicit migration operations/SQL with dependency-aware
  ordering and verify both `Up` and `Down` against the local PostgreSQL test database when
  available.
- [Risk] Pending outbox messages can use assembly-qualified CLR names or old JSON keys. → Keep a
  focused legacy type-name map and tolerant payload normalization in the durable dispatcher, and
  add regression tests for both forms.
- [Risk] Search documents may be stale or contain old type values. → Migrate existing rows in place,
  rebuild only where necessary, and keep version-safe projection behavior unchanged.
- [Risk] Renaming project files and solution entries can create missed references in test projects
  or CI scripts. → Update project references, solution metadata, namespaces, and all test assets,
  then run restore/build/test/format/package validation.

## Migration Plan

1. Complete and validate the OpenSpec artifacts before implementation.
2. Rename project/file/namespace/type references in disjoint semantic groups, preserving unrelated
   user changes and historical migrations.
3. Add the reversible hand-authored EF migration and synchronize only the current model snapshot.
4. Add durable-event compatibility and migration/API/search regression tests.
5. Run `openspec validate rename-competition-game-to-tournament`, restore, build, test, format,
   vulnerability listing, and feasible migration/API/OpenAPI checks.
6. If deployment fails, stop before applying the migration and correct the code. If rollback is
   required after application, run the migration `Down` operation, deploy the prior application,
   and let `Down` restore pending rows while the focused resolver retains safety for historical or
   dead-lettered rows.

## Open Questions

- The exact PostgreSQL constraint/index names and durable event registrations MUST be confirmed from
  the current model before the migration is written.
- Local PostgreSQL/Docker availability may determine whether full `Up`/`Down` and API startup smoke
  checks can run; limitations MUST be reported exactly at handoff.
- The coordinator MUST add the Phase 20 PR URL and check the progress ledger only after opening the
  real PR.
