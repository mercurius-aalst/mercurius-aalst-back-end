# Modular Monolith Migration Progress

Last updated: 2026-06-19

This is the handoff tracker for the modular monolith migration. Update it whenever a phase PR is opened, merged, blocked, or split.

## Current Status

- Integration branch: `refactor/modular-monolith`
- Current branch: `refactor/phase-1-progress-tracker`
- Current PR: pending
- PR target: `refactor/modular-monolith`
- State: progress tracker follow-up ready to open
- Next action: merge this progress tracker follow-up, then update local `refactor/modular-monolith` and start Phase 2 from it.

## Resume Checklist

After the current phase PR is merged:

1. Fetch the latest remote refs.
2. Switch to `refactor/modular-monolith`.
3. Fast-forward it from `origin/refactor/modular-monolith`.
4. Create the next phase branch from that updated integration branch.
5. Update this file with the new phase branch and PR status.

Do not start the next phase from an unmerged phase branch.

## Phase Queue

| Phase | Branch | Status | PR | Notes |
| --- | --- | --- | --- | --- |
| 1. AGENTS.md guardrails and refactor branch setup | `refactor/phase-1` | Merged | [#94](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/94) | Documentation/guardrails only. No behavior change. |
| 1 follow-up. Progress tracker | `refactor/phase-1-progress-tracker` | In progress | | Adds this repo-local handoff tracker. No behavior change. |
| 2. Baseline safety net and contract freeze | `refactor/phase-2` | Pending | | Add route/security/OpenAPI/DTO/privacy behavior tests. No production behavior change. |
| 3. Monolith code quality and boundary preparation | `refactor/phase-3` | Pending | | Prepare existing monolith code before physical moves. |
| 4. Platform extraction | `refactor/phase-4` | Pending | | Extract host/platform infrastructure without route or persistence redesign. |
| 5. Solution and project skeleton | `refactor/phase-5` | Pending | | Create module/project skeletons. |
| 6. Contracts before implementations | `refactor/phase-6` | Pending | | Introduce contracts without leaking EF or implementation details. |
| 7. Teams extraction | `refactor/phase-7` | Pending | | Move Teams into its module while preserving routes and JSON. |
| 8. Realtime split | `refactor/phase-8` | Pending | | Separate realtime concerns from Teams/business logic. |
| 9. Eventing/outbox/inbox | `refactor/phase-9` | Pending | | Add reliable cross-module integration plumbing. |
| 10. Identity extraction | `refactor/phase-10` | Pending | | Move user/profile/Auth0 ownership into Identity. |
| 11. Competition extraction | `refactor/phase-11` | Pending | | Move games, matches, registrations, rosters, and placements. |
| 12. Sponsorship extraction | `refactor/phase-12` | Pending | | Move sponsor ownership and document placement ownership. |
| 13. Media extraction | `refactor/phase-13` | Pending | | Move file/image storage concerns. |
| 14. Discovery/Search extraction | `refactor/phase-14` | Pending | | Move search into projections and Discovery. |
| 15. Persistence boundary tightening | `refactor/phase-15` | Pending | | Move mappings and tighten persistence ownership. |
| 16. Endpoint simplification in place | `refactor/phase-16` | Pending | | Resource-oriented cleanup with OpenSpec. No `/v2`. |
| 17. Tighten internals and public surface | `refactor/phase-17` | Pending | | Make module implementation details internal and add architecture tests. |
| 18. Test suite reshaping | `refactor/phase-18` | Pending | | Reshape tests around API, module behavior, events, and architecture. |
| 19. Remove transitional adapters and clean up | `refactor/phase-19` | Pending | | Delete old paths/adapters and run final architecture checks. |

## Phase 1 Handoff Notes

What changed:

- Rewrote `AGENTS.md` into durable engineering guardrails.
- Added `docs/architecture/modular-monolith-guardrails.md`.
- Added this progress tracker.
- Applied formatter-only whitespace cleanup in three existing C# files so format verification passes.

What did not change:

- No production behavior changed.
- No routes changed.
- No authorization behavior changed.
- No DTO or JSON shape changed.
- No database schema or persistence behavior changed.
- No OpenSpec change was required.

Validation for Phase 1:

- `dotnet restore` passed.
- `dotnet build` passed with existing warnings in `SecurityTrimming.cs` and `DoubleEliminationMatchModerator.cs`.
- `dotnet test` passed with 235 tests.
- `dotnet format --verify-no-changes` passed after formatter-only whitespace cleanup.

Validation limitations:

- API startup/OpenAPI smoke check was not run because no `Mercurius.LAN.API_ConnectionStrings__MercuriusDB` value was configured in the shell.
- PostgreSQL migration/database update validation was not run because the known local PostgreSQL test container was absent.

## Handoff Rules

- Keep this tracker in the phase PR when its status changes.
- Include the current branch, PR number, validation results, blockers, and next action.
- If a phase is split, add rows using suffixes such as `refactor/phase-7a-teams-contract-adapters`.
- Do not mark a phase complete in this file until its PR has merged into `refactor/modular-monolith`.
