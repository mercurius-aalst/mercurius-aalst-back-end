## Why

Several endpoints encode small action variants in the path even when the HTTP method, target resource, authorization boundary, and response shape are otherwise identical. Consolidating those variants behind explicit query parameters reduces route count and gives the front end a more regular contract for action-style operations.

## What Changes

- Add a consolidated game lifecycle action route: `POST /v{version}/lan/games/{id}?action=<action>`.
- Replace separate game lifecycle path actions for start, reset, complete, and cancel with an enum-backed action parameter accepted as a string.
- Add a consolidated current-user account action route: `POST /v{version}/lan/users/me?action=<action>`.
- Replace separate current-user resend-verification-email and password-reset path actions with an enum-backed action parameter accepted as a string.
- Add a consolidated current-user team invite route: `GET /v{version}/lan/teams/me/invites?sent=<bool>`.
- Replace separate current-user received and sent invite route definitions with the `sent` query parameter while preserving authorization and DTO shape.
- Keep JSON enum string conversion in use for action values.

## Capabilities

### New Capabilities
- `simplified-endpoint-routes`: Consolidated route contracts for endpoint variants that differ only by action or same-shape query intent.

### Modified Capabilities

## Impact

- Affected API endpoint mappings: game lifecycle actions, current-user account actions, and current-user team invite listing.
- Affected tests: endpoint route authorization and route-shape coverage.
- Front-end contract impact: clients should call query-parameterized routes instead of action-specific path routes.
- No persistence, migration, CORS, Auth0, or deployment configuration changes are expected.
