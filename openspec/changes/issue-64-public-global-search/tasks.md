## 1. Search API Contract

- [x] 1.1 Add search response and result DTOs with `results`, `nextCursor`, count/more metadata, type, labels, and navigation fields.
- [x] 1.2 Add a search service interface and implementation.
- [x] 1.3 Add `GET /v1/lan/search` endpoint and register it in the app.

## 2. Query Behavior

- [x] 2.1 Normalize and validate query input, including short-query and max-length behavior.
- [x] 2.2 Query complete, non-deleted users by normalized username.
- [x] 2.3 Query teams by normalized/case-insensitive name and align with the team-name normalization change.
- [x] 2.4 Query games by case-insensitive name.
- [x] 2.5 Apply bounded page size, deterministic relevance ordering, cursor continuation, and projection-only DTO mapping.

## 3. Regression Coverage

- [x] 3.1 Add tests for user, team, game, empty, short-query, case-insensitive results, relevance ordering, and cursor continuation.
- [x] 3.2 Add tests that deleted/incomplete users are excluded.
- [x] 3.3 Add privacy tests proving user search results do not contain private fields.
- [x] 3.4 Run `dotnet test` for the solution.
