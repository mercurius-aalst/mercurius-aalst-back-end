## Context

The no-query administrative user list and the administrative tournament-registration list currently return every matching database row. Both already expose raw JSON arrays and stable routes that clients use. User search on the same route is keyset/cursor based when the `query` key exists and is intentionally outside this change.

## Goals / Non-Goals

**Goals:**

- Bound the two administrative collection reads at their HTTP boundary and database query.
- Preserve raw-array response JSON, routes, authorization, current search behavior, and cancellation propagation.
- Make page membership deterministic and avoid integer offset overflow.

**Non-Goals:**

- Add a general paging abstraction, response metadata, totals, cursors for the admin lists, client changes, or persistence changes.
- Change public collections, search semantics, or any non-admin registration behavior.

## Decisions

### Validate and normalize paging in each scoped endpoint

Each endpoint accepts nullable `page` and `pageSize`, returns `ValidationProblem` for values less than one before calling its service, defaults omitted values to 1 and 20, and caps a supplied positive size at 50. This keeps validation at the transport boundary while preserving existing service behavior for direct callers.

Alternative considered: a shared paging helper. The two endpoint groups have distinct existing patterns and only two call sites; a new cross-module abstraction would add indirection without reducing meaningful complexity.

### Keep user search as a separate path

The user endpoint selects the search path solely by `request.Query.ContainsKey("query")`, exactly as today. It continues to use `SearchRequest` validation and cursor semantics; `page` is ignored. Only the branch that already requires the admin role receives offset paging.

Alternative considered: applying page parameters to search. That would change the established cursor-search contract and is out of scope.

### Page ordered queries before mapping

The user query orders by `NormalizedUsername` then `Id`; registrations order by `Kind`, `Status`, `CreatedAtUtc`, then `Id`. Each calculates `(long)(page - 1) * pageSize`, returns an empty collection above `Int32.MaxValue`, otherwise applies `Skip`/`Take` before materialization and any registration enrichment. All calls accept and pass the request cancellation token.

Alternative considered: materialize then page. That retains unbounded database and enrichment cost, defeating the objective.

## Risks / Trade-offs

- [Offset pages shift when data changes concurrently] → deterministic ordering provides stable tie-breaking; snapshot consistency is outside this compatible extension.
- [Existing UI may expect a full raw array] → raw array shape remains intact, but clients must request subsequent pages to traverse beyond 20 items.
- [Very large page values can overflow EF Core's integer offset] → calculate in `long` and return an empty page before `Skip` when unsupported.

## Migration Plan

Deploy as an API-only change with no database migration. Roll back by restoring the previous endpoint/service signatures; no persisted state is affected.

## Open Questions

None.
