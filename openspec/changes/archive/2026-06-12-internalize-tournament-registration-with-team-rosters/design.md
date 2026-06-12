## Context

The API currently represents tournament participants through `Game.RegisteredUsers` and `Game.RegisteredTeams` many-to-many collections. Mutations are exposed on admin-authorized game routes, and tournament creation/update requires `RegisterFormUrl`, reflecting the older external-form registration model. Team membership management already protects teams that are registered in protected tournament states, but it has no concept of tournament-specific rosters or member confirmation.

Issue #83 requires registration to become a first-class authenticated workflow for individual users and team captains, with exact roster selection, selected-member confirmation, duplicate participation prevention, narrow admin removal, eligibility feedback, and privacy-safe public projections. The current many-to-many model cannot record pending confirmations, exact roster membership, per-registration state, or concurrency-safe uniqueness for a user participating through exactly one path.

## Goals / Non-Goals

**Goals:**

- Introduce an explicit registration model for scheduled tournaments that supports individual registrations and team registrations with exact-size rosters.
- Require selected non-captain roster members to confirm their selection before a team becomes actively registered.
- Automatically include and confirm the captain as part of every team roster.
- Enforce all eligibility, authority, duplicate participation, exact-size, confirmation, unregister, and removal rules server-side.
- Provide fast REST eligibility endpoints for front-end validation before registration mutations.
- Provide authenticated self-service endpoints for users and captains, plus admin endpoints limited to inspection and removal of users or teams from tournaments.
- Keep public game/detail responses privacy-safe.
- Remove external registration URL behavior and unused legacy admin registration routes.
- Add EF Core constraints and service transaction boundaries that make duplicate participation safe under concurrent requests.
- Align team management protections with pending and active tournament-specific roster state.

**Non-Goals:**

- Redesign match generation, bracket logic, placement calculation, or tournament scheduling beyond consuming active confirmed registration records.
- Build front-end UI flows.
- Replace or duplicate the existing team-invite notification delivery infrastructure.
- Import historical external form submissions.
- Preserve `RegisterFormUrl` as a registration compatibility mechanism.
- Allow admins to add users, swap roster users directly, edit roster size, or force-confirm roster members.

## Decisions

### Use explicit registration entities instead of enriching join tables

Introduce a registration aggregate tied to `Game`:

- `TournamentRegistration`: id, game id, registration kind, status (`PendingConfirmation`, `Active`), registered by user id, optional team id, and created/updated timestamps.
- `TournamentRegistrationRosterMember`: registration id, game id, user id, confirmation status (`AutoConfirmed`, `Pending`, `Confirmed`), confirmation notification reference metadata, and timestamps.

Individual tournaments store one active registration with the registering user. Team tournaments store a pending registration per team until all selected non-captain roster members confirm; once all required confirmations are complete, the service automatically transitions the team registration to active. The captain is always included and auto-confirmed. Existing `Game.RegisteredUsers` and `Game.RegisteredTeams` should be replaced by projections from active registration records, not maintained as primary state.

Alternative considered: add payload columns to the existing `GameUser` / `GameTeam` join tables. That would not model pending confirmations cleanly, would make exact roster state awkward, and would still leave duplicate participation across pending and active registrations difficult to constrain.

### Centralize eligibility and mutations in a registration service

Add a dedicated registration service rather than growing `GameService`. It should load the current Auth0-linked user, tournament, team, captain, roster candidates, pending confirmations, and active participation state in bounded queries, then apply mutations in a transaction. Endpoint authorization remains explicit:

- anonymous read routes can see privacy-safe registration projections;
- authenticated users can register/unregister themselves and confirm their own roster selection;
- captains can create team registrations, edit pending or pre-start rosters, and unregister teams before start;
- admins can inspect registration state and remove users or teams from tournaments.

Eligibility endpoints should use the same service rules and return fast, front-end-friendly responses with `eligible`, machine-readable reason codes, and privacy-safe details for individual users, team registration, and roster candidate validation.

Alternative considered: keep all registration behavior in `GameService`. That keeps fewer classes but blends admin tournament management, match scheduling, participant registration, eligibility checks, confirmation notifications, and participant registration into one service and makes authorization paths harder to reason about.

### Enforce duplicate participation across pending and active state

The service must reject duplicate participation before saving so callers receive clear validation errors. Database constraints should also protect against races:

- one active individual registration per game/user;
- one pending or active team registration per game/team;
- one pending or active roster membership per game/user across all registrations;
- one pending or active captain participation per game/user.

Because cross-table filtered uniqueness can be difficult in PostgreSQL through EF Core alone, implementation may use a denormalized `GameId` on roster members or a participation index table to enforce pending-or-active `GameId + UserId` uniqueness. The design prefers a denormalized key when it keeps queries and migrations understandable.

Alternative considered: reserve uniqueness only after confirmation. That would let the same user receive multiple roster confirmations for the same tournament, creating front-end confusion and race conditions when confirmations complete.

### Treat tournament team size as exact active roster size

Add team-size configuration to team-mode tournaments. The configured size is the exact roster size required for the tournament. Roster creation and roster edits must include exactly that many members, including the automatically confirmed captain. Every selected non-captain member must be an active team member, not deleted, and not already pending or actively participating in the same tournament.

Alternative considered: maximum-size semantics. The requested behavior now requires exact team size, so smaller rosters must be rejected.

### Keep roster confirmations separate from team membership invites

When a captain submits an exact-size roster, the service creates pending confirmation entries and sends dedicated roster-confirmation notifications to selected non-captain members. These notifications are separate from team membership invites: team invites add a user to a team, while roster confirmations add an existing team member to a tournament roster. If the captain edits the roster, unregisters the team, or an admin removes the pending team registration before all confirmations complete, pending confirmations and their notifications are deleted or withdrawn so the user experience behaves as if they never existed. Confirming a deleted, withdrawn, or expired notification must fail without changing participation.

Alternative considered: store roster confirmations as a team-invite purpose. That mixes two different domain concepts and lets generic team invite flows accidentally surface or mutate tournament roster confirmations, so roster confirmations use a separate table and service flow.

### Aggressively clean up transient registration data

Pending registrations, roster member confirmation rows, and roster confirmation notifications are transient coordination data. When a team unregisters before tournament start, when a captain replaces a pending roster, or when an admin removes a pending team registration before tournament start, the related pending registration, roster rows, and notifications should be physically deleted rather than retained as inactive history. Admins do not remove a single selected member from a pending team roster; if a pending team roster must be invalidated, the admin removes the pending team registration.

Active confirmed registrations should remain only while they are needed for current tournament participation, match generation, public projections, or completed tournament history. Queries must be designed as if the table could contain very large volumes of rows: filter by tournament and active state first, use supporting indexes, avoid unbounded scans, and do not retain stale withdrawn notification or pending roster records.

Alternative considered: keep invalidated/removed registration rows for audit. The clarified requirement prioritizes cleanup and performance; audit history for stale pre-start roster coordination is not useful enough to justify unbounded data growth.

### Limit admin mutation power to removals

Admins may inspect registrations and remove an individual registration from an individual tournament or remove a whole team registration from a team tournament. Admin removals hard-delete the affected registration aggregate and related pending confirmation notifications rather than retaining removed-state metadata. Admins must not add users, add teams, remove a single selected member from a pending team roster, swap roster users, force confirmations, or edit roster size. Removing an active team registration removes the whole team from the tournament.

Alternative considered: broad admin override. Narrow removal-only admin behavior is easier to reason about and matches the clarified scope.

### Preserve public privacy by projecting registration DTOs

Public game/detail responses should expose active registration status, participant counts, teams, and roster members using `PublicUserDTO`, `PublicTeamMemberDTO`, or equivalent privacy-safe DTOs. They must not expose pending registrations, pending confirmation state, email, Auth0 IDs, deletion state, admin removal notes, confirmation tokens, notification identifiers, or private account metadata.

Alternative considered: return internal registration entities directly to admin and public callers with conditional serialization. Separate DTOs make privacy and contract testing simpler.

### Remove external registration URL behavior

`RegisterFormUrl` should be removed from create/update contracts and public registration models as part of the migration to internal registration. Existing database values can be dropped or ignored by migration; new registration flows must not depend on external URLs.

Alternative considered: keep the property as optional metadata. The clarified requirement explicitly says not to preserve the legacy external registration URL.

## Risks / Trade-offs

- [Risk] Existing clients may still use `/games/{id}/users` and `/games/{id}/teams` admin registration routes. -> Mitigation: remove unused admin routes deliberately and cover removal/absence in contract tests.
- [Risk] Duplicate participation constraints are subtle when pending confirmations and active registrations share the same tournament. -> Mitigation: model pending and active participation explicitly, add database constraints, and include concurrent registration/confirmation tests.
- [Risk] Match generation currently reads `RegisteredUsers` / `RegisteredTeams`. -> Mitigation: update match generation input to use active confirmed registrations only.
- [Risk] A selected member may become ineligible between notification and confirmation. -> Mitigation: re-run eligibility checks during confirmation before transitioning the member or team to active state.
- [Risk] Admin removal from an active exact-size roster can leave the team invalid. -> Mitigation: admins remove the whole team registration from the tournament rather than removing a single roster member.
- [Risk] Notification deletion may leave stale client UI state. -> Mitigation: confirmation endpoints must return a clear not-found or no-longer-actionable result, and current-user registration state endpoints must exclude deleted or withdrawn confirmations.
- [Risk] Registration and notification tables could grow without bound if pending data is retained. -> Mitigation: physically delete transient pending registration, roster, and notification rows when they are replaced, unregistered, withdrawn, or no longer actionable; keep indexes aligned with active tournament queries.

## Migration Plan

1. Add registration, roster member, and confirmation-notification state entities, EF mappings, indexes, and a migration generated with the existing EF tooling, e.g. `dotnet ef migrations add <Name>`.
2. Add exact team size to team-mode tournaments and remove `RegisterFormUrl` from DTO contracts and database state.
3. Do not backfill existing `GameUser` or `GameTeam` rows; deployment assumes a clean database state for this change.
4. Route new self-service, eligibility, confirmation, captain, and admin removal endpoints to the registration service.
5. Update public game, team profile, match-generation, and team-management queries to read pending/active registration records as appropriate, with full active rosters shown in public active-registration projections.
6. Remove unused legacy admin registration routes and update OpenAPI/contract tests.
7. Rollback requires restoring the previous schema from backup or a down migration because external URL and legacy route behavior are intentionally removed.

## Open Questions

- None. Current decisions: no data migration/backfill is required because the database will have a clean state, and public registration projections show full active rosters.
