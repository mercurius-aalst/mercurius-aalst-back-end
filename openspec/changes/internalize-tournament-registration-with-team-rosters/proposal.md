## Why

Tournament registration is currently modeled as an external form URL plus admin-managed participant lists, which prevents the redesigned front-end from offering first-class self-service registration, team roster confirmation, and eligibility feedback. Internalizing registration now gives the API one authoritative place to enforce duplicate participation, captain authority, exact roster size, member confirmation, unregister timing, and privacy-safe registration views.

## What Changes

- Add internal individual tournament registration for authenticated users, including explicit self-registration, duplicate prevention, and self-unregister before tournament start when tournament state allows it.
- Add internal team tournament registration for captains with exact tournament team-size configuration and captain-selected rosters from current team members.
- Require selected non-captain roster members to confirm their roster selection through the existing notification infrastructure before the team is automatically added to the tournament.
- Treat the captain as always part of the roster and automatically confirmed.
- Allow captains to edit rosters before tournament start; roster changes after confirmation notifications are sent invalidate the previous pending confirmations as if they never existed.
- Prevent confirmed members from leaving the roster on their own; captain roster editing and admin removals are the allowed paths after confirmation.
- Expose fast REST eligibility checks so the front-end can validate whether a user or roster candidate can participate before attempting a mutation.
- Enforce participation eligibility server-side so a user cannot participate more than once in the same tournament through individual registration, captain participation, pending roster confirmation, or active team roster membership.
- Delete transient registration, roster, and confirmation notification data when it is no longer actionable, especially when a team unregisters or a roster is replaced, so stale data does not accumulate.
- Add admin visibility plus narrow admin removal flows for removing users or teams from tournaments.
- **BREAKING**: Remove external registration URL from the registration model instead of preserving it as compatibility metadata.
- **BREAKING**: Remove unused legacy admin registration routes instead of keeping wrappers.
- Preserve privacy-safe public registration responses that do not expose email, Auth0 identifiers, deletion state, or private account metadata.
- Require registration operations to be concurrency-safe and query-efficient.

## Capabilities

### New Capabilities

- `tournament-registration`: Internal individual and team tournament registration, exact roster selection, member confirmation notifications using existing notification delivery, eligibility checks, unregister/removal flows, transient data cleanup, narrow admin inspection/removal, and removal of external registration URL behavior.

### Modified Capabilities

- `user-owned-team-management`: Team member leave, captain member removal, and team deletion rules must account for pending and active tournament registration roster state rather than only legacy whole-team game registration.
- `public-participant-privacy`: Public game and registration projections must expose tournament participants, pending/active team registrations, and rosters using privacy-safe user/team shapes.

## Impact

- API endpoints under `src/MercuriusAPI/Endpoints/GameEndpoints.cs` and likely new registration-specific endpoint/service organization.
- Game, team, user, registration, roster-confirmation, and existing notification domain/service models under `src/MercuriusAPI/Models/` and `src/MercuriusAPI/Services/`.
- EF Core mappings and migrations in `src/MercuriusAPI/Data/MercuriusDBContext.cs` and `src/MercuriusAPI/Migrations/`.
- Game and registration DTOs under `src/MercuriusAPI/DTOs/`.
- Registration, eligibility, team-management, notification, match-generation, and admin service behavior.
- Tests in `tests/MercuriusAPI.Tests/` for authorization, validation, privacy, confirmation lifecycle, concurrency, migrations, service rules, and endpoint contract coverage.
- Front-end contract impact: clients must use internal registration and eligibility endpoints; `RegisterFormUrl` and unused legacy admin registration routes are removed.
