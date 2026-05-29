## Context

The current test project focuses on model and service behavior with EF in-memory helpers. The redesigned front-end needs tests that assert API-facing DTO contracts, route compatibility, privacy rules, and authorization behavior. Some coverage can remain service-level, but route/status/authorization assertions may need a small HTTP test harness.

## Goals / Non-Goals

**Goals:**
- Test every redesigned front-end API dependency explicitly.
- Fail when anonymous/public responses leak private fields.
- Fail when required public routes or admin compatibility routes are missing.
- Keep tests runnable locally in the existing test project.

**Non-Goals:**
- Browser or end-to-end front-end testing.
- Load testing beyond lightweight bounded-result or query-shape assertions.
- Rewriting all existing service tests.

## Decisions

- Organize contract tests by API area: games/schedule, matches, sponsors, search, public profiles, current user, admin users, and privacy.
- Add reusable fixture builders for users, teams, games, sponsors, and matches to keep response-shape tests readable.
- Use DTO/service-level tests where the behavior is pure mapping or projection.
- Add a minimal route-level test harness only where HTTP status, route precedence, or authorization must be proven.
- Assert absence of private fields through serialized JSON for public responses so regressions in DTO properties are caught.
- Include bounded-result tests for search and projection tests that discourage loading or returning unnecessary private data.

## Risks / Trade-offs

- Route-level tests may require adding packages such as `Microsoft.AspNetCore.Mvc.Testing`, which should be kept scoped to the test project.
- EF in-memory tests do not prove PostgreSQL query plans, so performance checks should focus on contracts that can be enforced locally.
- These tests depend on the preceding public API capabilities; they should be written to document expected contracts even if some implementation work lands in parallel.
