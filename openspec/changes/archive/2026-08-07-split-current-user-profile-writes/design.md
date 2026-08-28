## Context

The current-user profile flow is Auth0-backed. The API extracts the current Auth0 subject from the authenticated principal, stores that subject on `User.Auth0UserId`, and keeps local profile fields such as username, first name, last name, and platform IDs in the Mercurius database. `GET /v1/lan/users/me` currently uses a helper that fetches Auth0 profile metadata, creates a database row when none exists, and updates stored email verification snapshots for existing users.

## Goals / Non-Goals

**Goals:**
- Make `GET /me` read-only at the service layer.
- Provide an explicit authenticated create route at `PUT /me`.
- Preserve `PATCH /me` as an update-only route for existing current-user rows.
- Preserve `POST /me/complete-profile` as an update-only compatibility route for existing incomplete current-user rows.
- Keep existing DTO shapes for current-user profile responses and profile write requests.

**Non-Goals:**
- Changing the `User` table schema or adding migrations.
- Changing public user profile behavior.
- Changing Auth0 JWT validation, role mapping, or CORS configuration.

## Decisions

- Use a new `CreateCurrentUserAsync(auth0UserId, request)` service method for `PUT /me`.
  - Rationale: current-user creation uses the authenticated subject, not a client-supplied Auth0 ID, while admin creation still uses `CreateUserProfileRequest`.
  - Alternative considered: reuse admin `CreateUserAsync`; rejected because it trusts a payload Auth0 ID and is scoped to admin operations.

- Make `GetCurrentUserAsync` use the existing required-user lookup and skip Auth0 Management API synchronization.
  - Rationale: the endpoint must not create or update database objects, and refreshing stored email snapshots is a write.
  - Alternative considered: keep email snapshot refresh on read; rejected because it violates safe read semantics.

- Treat `PUT /me` against an existing profile as a validation error.
  - Rationale: the requested split assigns creation to PUT and updates to PATCH, so existing profiles should use PATCH.
  - Alternative considered: make PUT idempotently replace the full profile; rejected because the requested behavior says PUT is for the missing-user case.

- Keep `POST /me/complete-profile` separate from `CreateCurrentUserAsync`.
  - Rationale: legacy complete-profile clients call this route after a local user row exists, so it should complete/update that row instead of trying to create it.
  - Alternative considered: route it to `PUT /me`; rejected because that fails for existing users and duplicates create semantics.

## Risks / Trade-offs

- Existing clients that relied on `GET /me` to bootstrap a profile will now receive 404 until they call `PUT /me` with profile data. Mitigation: keep the response DTO shape unchanged and add explicit route/test coverage for the new create path.
- Stored Auth0 email snapshots will no longer refresh on every current-user read. Mitigation: write-oriented flows such as password reset can still refresh Auth0 profile data where needed.
