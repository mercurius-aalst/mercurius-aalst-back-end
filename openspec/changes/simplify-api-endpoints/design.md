## Context

The Phase 15 integration branch has stable module endpoint mapping and baseline route, authorization, and OpenAPI coverage. A small set of v1 routes still encode commands in their path, including game lifecycle transitions, team leave/invite/logo operations, tournament registration operations, and current-user profile and Identity notifications.

This phase changes endpoint contracts only. The existing module services remain the owners of validation, authorization decisions, side effects, events, and persistence. There is no `/v2` route set and no compatibility mapping for removed routes.

## Goals / Non-Goals

**Goals:**

- Expose resource-oriented v1 routes for the remaining action-style operations.
- Preserve the existing domain operations, authorization requirements, response DTO JSON shapes, error behavior, events, and database schema.
- Make each canonical route and every intentionally removed predecessor explicit in route, authorization, and OpenAPI tests.

**Non-Goals:**

- Altering game lifecycle rules, registration eligibility, team invite or membership rules, identity-provider behavior, persistence ownership, or module dependencies.
- Adding compatibility aliases, a `/v2` API, deprecation redirects, or DTO-shape changes to existing responses.
- Redesigning routes that already model a resource, such as sponsor CRUD, game sponsor placement replacement, team captain replacement, and team-logo deletion.

## Decisions

### Use one lifecycle-state resource for game transitions

`PUT /v1/lan/games/{gameId}/lifecycle-state` accepts an `UpdateGameLifecycleStateRequest` whose state is one of `Scheduled`, `InProgress`, `Completed`, or `Canceled`. The endpoint dispatches to the existing reset, start, complete, and cancel service operations respectively. The former `POST /start`, `/reset`, `/complete`, and `/cancel` routes are removed.

The successful response behavior remains transition-specific: completion continues to return the existing placement response; the other transitions retain their existing empty successful response. This avoids inventing a second game read model solely for this route cleanup.

Alternative considered: separate action routes or a generic `PATCH /games/{id}`. Separate actions fail the phase goal, while a generic game patch would blur a protected lifecycle transition with ordinary game metadata updates.

### Model teams as memberships, invites, and logos

- `DELETE /v1/lan/teams/{teamId}/members/me` replaces `POST /teams/{teamId}/leave`.
- `POST /v1/lan/teams/{teamId}/invites` accepts the recipient user id in a new request body and replaces the path-parameter creation route.
- `PATCH /v1/lan/team-invites/{inviteId}` replaces `PUT /teams/invites/{inviteId}` for the recipient's acceptance or decline update.
- `PUT /v1/lan/teams/{teamId}/logo` replaces logo-upload `POST` while retaining multipart form data and the current response.

Invite cancellation remains nested beneath its owning team because the captain-scoped cancellation operation needs the team context. Captain transfer, member removal, and logo removal already express resource replacement or deletion and remain unchanged.

Alternative considered: retain every invite route below `/teams/{teamId}`. A top-level invite update is chosen because an invited recipient acts on an invite identified independently of the team and no team identifier is required by the existing service operation.

### Model registrations, eligibility, and roster confirmation as resources

- `PUT /v1/lan/games/{gameId}/registrations/individual/me` replaces the individual-registration `POST` route.
- `GET /v1/lan/games/{gameId}/registrations/individual/eligibility` and `GET /v1/lan/games/{gameId}/registrations/teams/{teamId}/eligibility` replace the eligibility-prefixed routes.
- `POST /v1/lan/games/{gameId}/registrations/teams/{teamId}/roster/eligibility` replaces `POST /eligibility/teams/{teamId}/roster` and retains the existing proposed-roster eligibility calculation.
- `PUT /v1/lan/games/{gameId}/registrations/teams/{teamId}/roster` remains the team-roster submission and replacement route.
- `PATCH /v1/lan/games/{gameId}/registrations/roster-members/{rosterMemberId}` accepts a request that sets the member confirmation state to `Confirmed`, replacing the confirmation action route.

The roster confirmation request accepts only the supported confirmed state. Requests for any other state are rejected rather than implying an unimplemented unconfirm operation. The game id stays in the route so the URI identifies the owning registration collection; the existing service keeps authorization and ownership validation.

Alternative considered: route confirmation through a registration id and user id. The current API exposes and authorizes a roster-member id; retaining it avoids an unnecessary public identifier change while still making the updated resource explicit.

### Make profile and Identity-triggered email operations resource requests

- `PATCH /v1/lan/users/me` is the sole update route; `POST /users/me/complete-profile` is removed.
- `POST /v1/lan/users/me/email-verification-requests` replaces resend-verification-email.
- `POST /v1/lan/users/me/password-reset-requests` replaces password-reset.

The two request resources retain the existing generic `UserActionResponse` and authenticated behavior. They represent requests to the external Identity provider, not persisted local entities.

### Test contracts from module route maps and generated OpenAPI

Focused module route tests will assert the canonical verb, raw template, and authorization metadata. OpenAPI tests will assert canonical paths exist and removed action paths do not exist. Existing serialization tests remain the source of truth for unchanged response JSON.

## Risks / Trade-offs

- [Breaking client route changes] → Document every removed-to-replacement mapping in the OpenSpec and PR, make no duplicate mappings, and assert removed paths are absent.
- [Single lifecycle endpoint has transition-specific success bodies] → Retain existing behavior and make the behavior explicit in the route specification rather than changing service outputs during a route-only phase.
- [New request DTOs could accept unsupported values] → Use narrow request types and validate unsupported lifecycle or roster-confirmation values before dispatch.
- [Route refactoring may accidentally broaden authorization] → Preserve the existing endpoint-group authorization and add focused metadata tests for each new route.
- [OpenAPI path drift] → Run generated OpenAPI route assertions and the full validation suite before the PR.
