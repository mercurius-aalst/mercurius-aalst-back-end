## Context

The anonymous game and team collection endpoints currently return raw arrays backed by unbounded EF Core queries. Game ordering is already deterministic, but team ordering is incidental. Both list paths already batch their cross-module enrichment, so the missing control is a database-level page bound rather than a mapping redesign.

Roster eligibility and submission share `SubmitTeamRosterDTO`, but validation starts inside `TournamentRegistrationService` after identity, game, team, and database work; submission also starts a transaction first. Duplicate IDs are currently silently normalized with `Distinct()`, and an empty GUID reaches downstream candidate lookup. Team tournaments accept every positive configured team size, so an HTTP roster cap must be paired with the same domain cap.

## Goals / Non-Goals

**Goals:**

- Bound roster payload cost before any service or downstream invocation.
- Keep every supported configured team roster submit-able.
- Make both public collections bounded, deterministic, and navigable by page.
- Preserve existing routes, raw-array responses, item DTOs, cancellation, and batched enrichment.
- Reuse the shared default page size of 20 and maximum page size of 50.

**Non-Goals:**

- Introduce total counts, cursors, response envelopes, or new route versions.
- Redesign tournament registration business rules or DTO shapes.
- Add persistence schema, indexes, or migrations.
- Change single-item game or team endpoints.

## Decisions

### Validate transport-level roster structure in the endpoint

Both roster handlers will run the same small validation routine before reading claims or invoking `ITournamentRegistrationService`. Missing `userIds`, more than 50 entries, `Guid.Empty`, and duplicates return `Results.ValidationProblem` keyed to `userIds`. The service retains exact-size, captain, membership, lifecycle, and participation checks as domain defense and will no longer use deduplication as a substitute for valid input.

Alternative considered: validate only in `TournamentRegistrationService`. That cannot satisfy the requirement to reject before transaction, database, Identity, or Teams work and would leave an expensive public application boundary.

### Use page/pageSize with raw arrays

Both collection endpoints accept optional `page` and `pageSize`. Missing values resolve to page 1 and the shared default of 20; non-positive values return validation problems; positive page sizes above the shared maximum are capped at 50. The services receive normalized positive values, apply deterministic ordering, calculate the offset in `long`, and return an empty page when the offset exceeds `Int32.MaxValue`; otherwise they call `Skip` then `Take` in SQL.

Alternative considered: cursor paging or a metadata envelope. Either would provide stronger large-dataset traversal or counts, but would change the established top-level JSON contract. Numbered raw-array pages are the smallest fully navigable compatible extension.

### Preserve and complete stable ordering

Games remain ordered by planned start time, name, and ID. Teams are ordered by name and ID. The ID tie-breakers guarantee stable page membership when business ordering values collide. `Skip` and `Take` occur before materialization and before existing batched module enrichment.

### Align configured team size with roster input capacity

The `Game` domain will accept team-mode sizes from 1 through 50 and retain the existing behavior of clearing team size for individual tournaments. Create and update already flow through the domain constructor/update method, so one invariant protects both request paths without changing their DTOs.

## Risks / Trade-offs

- [Clients assumed collection requests returned every row] → Preserve the route and array shape, document page defaults, and expose page parameters through OpenAPI so clients can traverse subsequent pages.
- [Offset paging can shift under concurrent inserts or updates] → Deterministic ordering prevents duplicates caused by ambiguous ordering, but snapshot consistency is intentionally outside this minimal contract.
- [Existing persisted team size above 50] → Current repository tests and configuration contain only sizes at or below 5; deployment should check production data before using create/update on such rows. No migration rewrites existing data.
- [Very large page numbers overflow an `int` offset] → Calculate with `long` and return an empty page when EF Core's `int` offset range is exceeded.
- [Validation exists at both transport and business layers] → Endpoint validation owns only cheap structural checks; the service remains the owner of tournament-specific rules.
