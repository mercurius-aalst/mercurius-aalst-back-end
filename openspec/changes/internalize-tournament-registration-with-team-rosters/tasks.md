## 1. Data Model and Migration

- [x] 1.1 Add explicit tournament registration and roster member state models with pending/active confirmation state, timestamps, and references to reusable notification records.
- [x] 1.2 Add exact team-size configuration to the game/tournament model and remove external registration URL from create/update DTOs, public DTOs, domain model usage, and database state.
- [x] 1.3 Configure EF Core mappings, relationships, delete behavior, indexes, and concurrency-safe uniqueness for pending-or-active game/user participation, game/team registration, roster membership, and captain participation.
- [x] 1.4 Use the existing EF migration tooling, e.g. `dotnet ef migrations add <Name>`, to create a migration that adds registration/roster/confirmation tables, exact team size, and removes registration URL state without backfilling existing `GameUser` / `GameTeam` participation data.
- [x] 1.5 Update domain model helpers so active registered participant counts come from confirmed active registration records only.

## 2. Registration, Confirmation, and Eligibility Services

- [x] 2.1 Add a registration service interface and implementation for individual self-registration, individual self-unregistration, team roster submission, roster confirmation, captain roster edits, team unregistration, admin listing, and admin removals.
- [x] 2.2 Implement shared eligibility checks for tournament state, participation mode, duplicate pending/active participation, current-user identity, captain authority, team membership, deleted users/teams, exact roster size, confirmation state, and admin authorization.
- [x] 2.3 Add REST eligibility-check service methods and DTOs for individual user eligibility, team registration eligibility, and roster candidate eligibility with fast machine-readable reason codes.
- [x] 2.4 Wrap registration and confirmation mutations in transactions and translate database uniqueness conflicts into validation errors.
- [x] 2.5 Reuse or extend the existing team-invite notification infrastructure to publish roster confirmation notifications for selected non-captain members and auto-confirm captains.
- [x] 2.6 Physically delete transient pending registrations, roster rows, and roster confirmation notifications when a team unregisters, a captain replaces a roster, or an admin removes a pending team registration so stale data does not accumulate.
- [x] 2.7 Update match-generation inputs to use active confirmed internal registrations and exact team rosters without changing bracket behavior.

## 3. API Contracts and Endpoints

- [x] 3.1 Add request/response DTOs for registration state, eligibility checks, individual registration, team roster submission, roster confirmation, captain roster edits, admin registration lists, and admin removals.
- [x] 3.2 Add authenticated current-user endpoints for registration state, individual self-register/unregister, roster confirmation, captain team roster submission, captain roster edit, and captain team unregistration.
- [x] 3.3 Add REST eligibility endpoints for front-end pre-validation of individual participation, team participation, and roster candidates.
- [x] 3.4 Add admin endpoints for registration inspection, removing a user from a tournament, and removing a team from a tournament with explicit admin authorization.
- [x] 3.5 Remove unused legacy admin `/games/{id}/users` and `/games/{id}/teams` registration routes and update OpenAPI/contract expectations.
- [x] 3.6 Update tournament create/update DTO validation so exact team size is required for team tournaments and `RegisterFormUrl` is no longer accepted or returned.

## 4. Projections, Privacy, and Team Management

- [x] 4.1 Update public game/detail projections to include internal pending/active registration data and full active rosters using privacy-safe user/team/roster DTOs without confirmation tokens or notification identifiers.
- [x] 4.2 Update authenticated current-user registration projections to expose individual registration state, pending roster confirmations, confirmation eligibility, and captain-managed team registration state.
- [x] 4.3 Update admin registration projections to include pending/active/removed roster state and removal metadata without enabling admin add/swap/force-confirm behavior.
- [x] 4.4 Update team member leave, captain removal, and team deletion checks to use pending and active tournament registration roster state.
- [x] 4.5 Ensure eligibility, registration, roster, team-management, cleanup, and public participant queries avoid N+1 loading and unbounded scans with bounded projections, includes, and indexes that start from tournament/status filters.

## 5. Tests and Verification

- [ ] 5.1 Add domain/service tests for individual registration, duplicate prevention, self-unregistration timing, eligibility endpoints, and concurrent duplicate participation attempts.
- [ ] 5.2 Add domain/service tests for captain team roster submission, exact roster size validation, roster member team membership, captain auto-confirmation, member confirmation, automatic team activation, captain roster edits, and team unregistration.
- [ ] 5.3 Add tests proving pending confirmations and related notifications are removed or withdrawn when a captain edits the roster, unregisters the team, or an admin removes a pending team registration.
- [ ] 5.4 Add tests proving confirmed members cannot leave the roster on their own and only captain edits or admin removals can change roster participation before tournament start.
- [ ] 5.5 Add authorization tests for anonymous, authenticated non-captain, captain, selected member, confirmed member, and admin registration flows.
- [ ] 5.6 Add privacy/contract tests for public registration projections, current-user registration state, eligibility responses, confirmation notifications, and admin registration lists.
- [ ] 5.7 Add team-management regression tests for member leave, captain removal, and team deletion with pending, active, in-progress, completed, and canceled registration roster states.
- [x] 5.8 Add migration verification for the `dotnet ef migrations add` generated migration, removal of registration URL state, and the no-backfill clean-database assumption.
- [x] 5.9 Run `dotnet test LAN.API.sln` and update this checklist as implementation tasks complete.
