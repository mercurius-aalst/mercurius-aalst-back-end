## Why

The redesigned front-end adds public team profile pages at `/teams/{teamName}`, but the back-end currently exposes ID-based team APIs and anonymous team DTOs that can include more data than a public profile needs.

## What Changes

- Add `GET /v1/lan/public/teams/{teamName}` as a public team-name profile endpoint.
- Return a privacy-safe team profile with team name, captain username, member usernames, and registered tournaments.
- Use case-insensitive team-name lookup and integrate with normalized team-name semantics.
- Exclude invites, private member fields, and internal IDs except public `gameId` for tournament navigation.
- Add tests for successful lookup, casing, not found, member privacy, invite privacy, and tournament projection.

## Capabilities

### New Capabilities
- `public-team-profile`: Public team profile lookup by team name with privacy-safe member and tournament data.

### Modified Capabilities
- None.

## Impact

- New public team profile DTOs and service method.
- `TeamEndpoints` or a new public route group under `v{version}/lan/public/teams`.
- Team-name normalization dependency for efficient lookup.
- Tests in `tests/MercuriusAPI.Tests`.
