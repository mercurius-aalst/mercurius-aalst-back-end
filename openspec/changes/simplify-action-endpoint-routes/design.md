## Context

The API currently exposes multiple routes for operations that share the same resource, HTTP method, authorization boundary, and service-level behavior. Game lifecycle operations are split across action-specific POST paths, while current-user team invite lists are split across received and sent paths even though both return `TeamInviteSummaryDTO` projections.

Global HTTP JSON options already include `JsonStringEnumConverter`, and minimal API query binding accepts enum names as strings, so enum-backed action values can be exchanged without introducing a new package or formatter.

## Goals / Non-Goals

**Goals:**
- Consolidate game lifecycle POST routes behind a single enum-backed `action` query parameter.
- Consolidate current-user Auth0 account POST action routes behind a single enum-backed `action` query parameter.
- Consolidate current-user invite list routes behind a single `sent` query parameter.
- Preserve existing authorization boundaries and service methods.
- Keep route changes small and testable through endpoint metadata tests.

**Non-Goals:**
- Change game lifecycle domain rules or placement calculation.
- Change team invite DTO contents, invite status filtering, or real-time behavior.
- Add compatibility aliases for removed route shapes unless tests or consumers explicitly require them.

## Decisions

- Use `POST /v{version}/lan/games/{id}?action=<action>` with a `GameAction` enum parameter.
  - Rationale: a route-level action enum keeps allowed values explicit and lets model binding reject unsupported action values before service dispatch.
  - Alternative considered: accepting a raw string and switching on normalized text. This would be less discoverable in Swagger and easier to drift from valid action names.

- Use `POST /v{version}/lan/users/me?action=<action>` with a `CurrentUserAccountAction` enum parameter for resend verification email and password reset.
  - Rationale: both operations target the authenticated user's Auth0-backed account, share the same HTTP method, authorization boundary, and `UserActionResponse` shape.
  - Alternative considered: keeping separate paths because the action names are descriptive. The consolidated route better matches the action-parameter pattern requested for repeated same-resource operations.

- Return `Results.Ok(...)` for the `complete` game action and `Results.Ok()` for start, reset, and cancel.
  - Rationale: this preserves the existing behavior where completion returns placements and the other lifecycle actions only acknowledge success.
  - Alternative considered: normalizing all lifecycle actions to a common response envelope. That would be a larger front-end contract change.

- Use `GET /v{version}/lan/teams/me/invites?sent=<bool>` where `sent=false` or omitted returns received invites.
  - Rationale: received invites are the existing canonical `/me/invites` behavior, so omission keeps the shorter path intuitive while allowing sent invites with an explicit query parameter.
  - Alternative considered: an `InviteDirection` enum. The requested `sent=true` pattern is simpler and maps directly to the existing two-way split.

## Risks / Trade-offs

- Removed path-specific routes may require front-end updates -> The final change summary will call out the exact route replacements.
- Minimal API enum binding behavior can vary between route, query, and JSON binding -> Add route tests that verify the consolidated endpoint is mapped and obsolete action paths are absent.
- Consolidating routes reduces endpoint count but adds dispatch logic inside endpoint mapping -> Keep the dispatch as a direct switch expression over the enum to avoid hidden indirection.
