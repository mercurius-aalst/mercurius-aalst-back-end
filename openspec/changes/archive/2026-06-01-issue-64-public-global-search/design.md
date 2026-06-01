## Context

The API has separate game, team, and user route groups but no cross-entity search route. Users already have `NormalizedUsername`; teams currently do not have a normalized name column, so search should integrate with the team-name normalization change when available and avoid an expensive pattern that will block indexing later.

## Goals / Non-Goals

**Goals:**
- Provide a public, anonymous search endpoint for users, teams, and games.
- Return a compact normalized DTO that front-end navigation can consume directly.
- Exclude private user data and incomplete/deleted users.
- Keep query execution bounded and predictable.

**Non-Goals:**
- Full-text search, fuzzy matching, or ranking beyond stable prefix/name matching.
- Searching private/admin-only entities.
- Returning detailed user/team/game DTOs from search.

## Decisions

- Add a dedicated `SearchEndpoints` route group at `v{version}/lan/search`.
- Add a search response DTO with `results`, `nextCursor`, and `totalCount` or equivalent count metadata. Each result has `type`, `displayLabel`, `supportingText`, and one navigation field: `username`, `teamName`, or `gameId`.
- Trim and normalize the query once. For queries shorter than three characters, return an empty result list instead of leaking validation details.
- Apply a maximum input length and a maximum page size for predictable performance. Return results in deterministic relevance order: exact matches first, prefix matches next, then a stable display-label and stable tie-breaker ordering.
- Support keyset cursor-based continuation so clients can load matching result pages without offset drift when searchable rows change.
- Query users by normalized username and completion/deleted filters. Use PostgreSQL trigram indexes for user, team, and game substring matching until normalized team and game name columns are available.
- Project directly into DTOs to avoid loading full entities and private fields.
- Apply API-wide fixed-window rate limiting and a stricter per-client policy for the anonymous search endpoint.

## Risks / Trade-offs

- Team search is less efficient until normalized team names exist.
- Returning empty results for short queries is simpler for the front-end but should be documented so clients do not treat it as an error.
- Search tests should assert absent private fields, not just positive matches.
