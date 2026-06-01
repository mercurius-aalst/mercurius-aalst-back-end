## Why

The redesigned front-end adds public user profile pages at `/users/{username}`, but the back-end only exposes current-user and admin user APIs. A public profile endpoint is needed that can show safe identity data without leaking private account fields.

## What Changes

- Add `GET /v1/lan/public/users/{username}` as a public username-based profile endpoint.
- Return username, first name, and last name for valid public profiles.
- Include Discord, Steam, and Riot IDs only for authenticated callers.
- Exclude email, email verification state, Auth0 ID, deleted state, and timestamps.
- Use normalized username lookup and exclude deleted/incomplete profiles.
- Add tests for anonymous/authenticated visibility, lookup, not found cases, and privacy regressions.

## Capabilities

### New Capabilities
- `public-user-profile`: Privacy-aware public user profile lookup by username.

### Modified Capabilities
- None.

## Impact

- New public user profile DTO and service method.
- `UserEndpoints` or a new public route group under `v{version}/lan/public/users`.
- Normalized username lookup behavior in `UserService`.
- Tests in `tests/MercuriusAPI.Tests`.
