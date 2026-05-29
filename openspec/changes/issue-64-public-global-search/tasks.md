## 1. Search API Contract

- [ ] 1.1 Add search result DTOs with type, labels, and navigation fields.
- [ ] 1.2 Add a search service interface and implementation.
- [ ] 1.3 Add `GET /v1/lan/search` endpoint and register it in the app.

## 2. Query Behavior

- [ ] 2.1 Normalize and validate query input, including short-query and max-length behavior.
- [ ] 2.2 Query complete, non-deleted users by normalized username.
- [ ] 2.3 Query teams by normalized/case-insensitive name and align with the team-name normalization change.
- [ ] 2.4 Query games by case-insensitive name.
- [ ] 2.5 Apply bounded result limits and projection-only DTO mapping.

## 3. Regression Coverage

- [ ] 3.1 Add tests for user, team, game, empty, short-query, and case-insensitive results.
- [ ] 3.2 Add tests that deleted/incomplete users are excluded.
- [ ] 3.3 Add privacy tests proving user search results do not contain private fields.
- [ ] 3.4 Run `dotnet test` for the solution.
