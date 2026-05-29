## Why

The redesigned front-end includes a global navigation search that must query the real API and navigate to public user profiles, team profiles, and game detail pages. The back-end currently has no normalized public search endpoint.

## What Changes

- Add `GET /v1/lan/search?query={query}` as an anonymous public endpoint.
- Return normalized search results for users, teams, and games with only the navigation field relevant to each result type.
- Validate and normalize search input, including short-query behavior and length limits.
- Exclude deleted/incomplete users and private user fields.
- Bound result counts and use normalized/indexed fields where available.
- Add tests for result shape, matching, privacy, and edge cases.

## Capabilities

### New Capabilities
- `public-global-search`: Public search across users, teams, and games with privacy-safe normalized result DTOs.

### Modified Capabilities
- None.

## Impact

- New search endpoint, DTOs, and service.
- `User` normalized username queries, `Team` normalized name queries once available, and `Game` name queries.
- `Program.cs` endpoint registration and dependency registration.
- Tests for public search behavior and privacy.
