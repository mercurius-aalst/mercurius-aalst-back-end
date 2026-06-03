## Why

The redesigned front-end API client calls `DELETE /v1/lan/users/{username}`, while the back-end currently exposes username-based admin deletion at `DELETE /v1/lan/users/{username}/account`. This mismatch will break integration once mock data is removed.

## What Changes

- Add a compatible admin-only `DELETE /v1/lan/users/{username}` route.
- Reuse the existing username-based safe delete/anonymize service method.
- Keep the existing `/account` route for temporary backwards compatibility.
- Preserve route precedence with `/{id:guid}` routes.
- Add tests for admin success, anonymous rejection, non-admin rejection, missing users, and route precedence.

## Capabilities

### New Capabilities
- `admin-username-deletion`: Admin username-based user deletion route compatible with the redesigned front-end contract.

### Modified Capabilities
- None.

## Impact

- `UserEndpoints` route mapping.
- `IUserService`/`UserService.DeleteUserAsync` reuse and validation behavior.
- Authorization tests and route precedence coverage.
