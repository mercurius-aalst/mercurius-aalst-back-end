## Context

`UserService` already maintains `Username`, `NormalizedUsername`, names, platform IDs, and soft-deletion state. Existing user endpoints are either current-user authenticated routes or admin routes. The public endpoint should not reuse `GetUserDTO` because that DTO contains private fields such as email and account metadata.

## Goals / Non-Goals

**Goals:**
- Provide a public profile lookup by username.
- Return first name, last name, and platform IDs publicly.
- Return 404 for missing, deleted, incomplete, or username-less users.
- Use normalized username lookup and projection-only responses.

**Non-Goals:**
- Exposing email or Auth0 account data.
- Changing current-user or admin user response DTOs.
- Implementing front-end rendering in this back-end repository.

## Decisions

- Add a dedicated public DTO for public user profile responses rather than reusing `GetUserDTO`.
- Normalize route input with the existing username normalization helper and validate it before querying.
- Allow anonymous access to the endpoint and return a single public response shape for all callers.
- Query only active complete users with non-null username/normalized username.
- Keep not-found behavior uniform for missing, deleted, and incomplete profiles.

## Risks / Trade-offs

- First and last name are intentionally public for this endpoint, while other public participant responses may hide them. Tests should make that distinction explicit.
- Platform IDs are intentionally public for this endpoint, so tests should make that visibility explicit.
- Route grouping under `/lan/public` should be consistent with future public team profile endpoints.
