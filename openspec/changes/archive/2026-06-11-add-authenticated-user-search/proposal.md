## Why

Authenticated app workflows need a stable backend user id when selecting users, but public global search intentionally returns navigation-only fields and omits user ids. Filtering the authenticated users collection keeps global search privacy semantics intact while providing a general user lookup contract.

## What Changes

- Add authenticated search/filtering to the existing users collection route using the existing search query, cursor, and page-size conventions.
- Return a paged privacy-safe user search response with stable `id`, `type`, `username`, `displayLabel`, and optional `supportingText` results.
- Exclude deleted users and users without usable usernames.
- Preserve existing admin list behavior for `GET /v1/lan/users` when no `query` parameter is supplied.
- Keep workflow-specific validation, such as team invite rules, in the existing workflow endpoints.

## Capabilities

### New Capabilities
- `authenticated-user-search`: Authenticated privacy-safe user collection filtering for app workflows that need stable user ids.

### Modified Capabilities
- `user-owned-team-management`: Team invite flows can use authenticated user search to obtain recipient ids without changing invite mutation rules.

## Impact

- API: enhanced `GET /v1/lan/users?query={query}&cursor={cursor}&pageSize={pageSize}` collection route.
- DTOs: new privacy-safe authenticated user search result shape.
- Services: user service search method with query, cursor, and page-size bounds.
- Tests: endpoint authorization/route coverage, service filtering/DTO privacy coverage.
- Database: reuses the existing PostgreSQL trigram normalized-username search index; no new migration is required for authenticated user search.
- No CORS, Auth0 configuration, or deployment variable changes expected.
