## Why

`GET /v1/lan/users/me` currently creates missing current-user rows and refreshes stored Auth0 profile snapshots. Read endpoints should be safe to call without mutating database state, and current-user creation/update behavior should be explicit.

## What Changes

- Change `GET /v1/lan/users/me` to return the existing active current-user profile only.
- Add `PUT /v1/lan/users/me` to create the current-user profile when it does not already exist.
- Keep `PATCH /v1/lan/users/me` as the update path for an existing current-user profile.
- Keep `POST /v1/lan/users/me/complete-profile` as a compatibility update path for an existing current-user profile.
- Make missing-profile behavior explicit: `GET` and `PATCH` return not found when the current user row does not exist.
- Prevent current-user creation from overwriting an existing user row.

## Capabilities

### New Capabilities
- `current-user-profile`: Authenticated current-user profile lookup, creation, and update semantics.

### Modified Capabilities
- None.

## Impact

- `UserEndpoints` route mapping for authenticated current-user profile routes.
- `IUserService`, `UserService`, and `UserValidationService` current-user profile methods.
- Tests under `tests/MercuriusAPI.Tests` for route registration, read/write separation, not found behavior, and create/update boundaries.
- No database schema, migration, CORS, Auth0 configuration, environment variable, or deployment configuration changes are expected.
