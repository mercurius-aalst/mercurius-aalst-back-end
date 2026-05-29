## Why

The redesigned front-end depends on real API contracts across public pages, admin pages, authentication/profile flows, search, schedule data, and privacy-sensitive response shapes. Without explicit contract tests, regressions can silently reintroduce mock-data assumptions or private-field leaks.

## What Changes

- Add integration/contract tests for API shapes consumed by the redesigned front-end.
- Cover game, match, schedule, sponsor placement, public search, public user/team profile, current-user profile, and admin deletion contracts.
- Add privacy assertions that public anonymous responses do not expose private user/account fields.
- Add security assertions for admin-only endpoints and deleted/incomplete user filtering.
- Add lightweight performance-oriented assertions where practical, such as bounded search results and avoiding unnecessary private data loading.

## Capabilities

### New Capabilities
- `frontend-api-contract-tests`: Contract and privacy regression tests for redesigned front-end API dependencies.

### Modified Capabilities
- None.

## Impact

- `tests/MercuriusAPI.Tests` test structure and helpers.
- API DTOs and services indirectly through contract coverage.
- Potential addition of HTTP-style test harnesses if route-level authorization/status checks cannot be covered through existing service tests.
