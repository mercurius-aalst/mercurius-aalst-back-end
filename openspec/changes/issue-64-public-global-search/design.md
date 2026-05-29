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
- Add a result DTO with `type`, `displayLabel`, `supportingText`, and one navigation field: `username`, `teamName`, or `gameId`.
- Trim and normalize the query once. For queries shorter than three characters, return an empty result list instead of leaking validation details.
- Apply a maximum input length and a maximum total result count, with optional per-type limits for predictable UI behavior.
- Query users by normalized username and completion/deleted filters. Query teams by normalized name once the team normalization capability exists; until then, keep implementation structured so it can be switched to the indexed column.
- Project directly into DTOs to avoid loading full entities and private fields.

## Risks / Trade-offs

- Team search is less efficient until normalized team names exist.
- Returning empty results for short queries is simpler for the front-end but should be documented so clients do not treat it as an error.
- Search tests should assert absent private fields, not just positive matches.
