# Modular Monolith Migration Progress

Last updated: 2026-06-23

This tracker is the repo-local handoff ledger for the modular monolith migration. It records which phase PRs have been created. GitHub remains the source of truth for whether those PRs are open, closed, or merged.

Full phase instructions live in `docs/architecture/modular-monolith-implementation-plan.md`.

## Checkbox Semantics

- `[x]` means the phase PR has been created.
- `[ ]` means the phase PR has not been created yet.
- A checked phase is not automatically complete. Always verify the linked PR state on GitHub.
- Do not update a checkbox only because a PR merged. The checkbox already records the durable handoff event: PR created.
- Update this file when opening a new phase PR, splitting a phase into multiple PRs, adding a blocker note, or correcting the phase queue.

## Resume Protocol

Before starting work:

1. Fetch the latest remote refs.
2. Find the last checked item in the phase PR ledger.
3. Open the linked GitHub PR for that last checked item.
4. Verify that the PR targets `refactor/modular-monolith`.
5. If the PR is not merged, stop and wait for human review/merge.
6. If the PR is merged, switch to `refactor/modular-monolith` and fast-forward it from `origin/refactor/modular-monolith`.
7. Start the next unchecked phase from the updated `refactor/modular-monolith`.
8. When the next phase PR is opened, check that phase and add the PR link in this file.

Do not start the next phase from an unmerged previous phase branch.

## Phase PR Ledger

- [x] Phase 1 - AGENTS.md guardrails and refactor branch setup
  - Branch: `refactor/phase-1`
  - PR: [#94 Phase 1: document modular monolith guardrails](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/94)
  - Notes: Documentation/guardrails only. No behavior change.

- [x] Phase 1 follow-up - progress tracker
  - Branch: `refactor/phase-1-progress-tracker`
  - PR: [#95 Add modular monolith progress tracker](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/95)
  - Notes: Adds this repo-local handoff tracker. No behavior change.

- [x] Phase 1 follow-up - implementation plan
  - Branch: `refactor/phase-1-implementation-plan`
  - PR: [#96 Add modular monolith implementation plan](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/96)
  - Notes: Adds the full phase-by-phase implementation plan to the repository. No behavior change.

- [x] Phase 2 - Baseline safety net and contract freeze
  - Branch: `refactor/phase-2`
  - PR: [#97 Phase 2: add baseline contract safety tests](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/97)
  - Notes: Add route/security/OpenAPI/DTO/privacy behavior tests. No production behavior change.

- [x] Phase 3 - Monolith code quality and boundary preparation
  - Branch: `refactor/phase-3`
  - PR: [#98 Phase 3: prepare monolith service boundaries](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/98)
  - Notes: Prepare existing monolith code before physical moves.

- [x] Phase 4 - Platform extraction
  - Branch: `refactor/phase-4`
  - PR: [#99 Phase 4: extract platform infrastructure](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/99)
  - Notes: Extract host/platform infrastructure without route or persistence redesign.

- [x] Phase 5 - Solution and project skeleton
  - Branch: `refactor/phase-5`
  - PR: [#100 Phase 5: introduce module project skeleton](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/100)
  - Notes: Create `Modules.Shared` plus empty module implementation/contracts project skeletons. No behavior change.

- [x] Phase 6 - Contracts before implementations
  - Branch: `refactor/phase-6`
  - PR: [#101 Phase 6: introduce module contracts](https://github.com/mercurius-aalst/mercurius-aalst-back-end/pull/101)
  - Notes: Introduce contracts without leaking EF or implementation details.

- [ ] Phase 7 - Teams extraction
  - Branch: `refactor/phase-7`
  - PR: pending
  - Notes: Move Teams into its module while preserving routes and JSON.

- [ ] Phase 8 - Realtime split
  - Branch: `refactor/phase-8`
  - PR: pending
  - Notes: Separate realtime concerns from Teams/business logic.

- [ ] Phase 9 - Eventing/outbox/inbox
  - Branch: `refactor/phase-9`
  - PR: pending
  - Notes: Add reliable cross-module integration plumbing.

- [ ] Phase 10 - Identity extraction
  - Branch: `refactor/phase-10`
  - PR: pending
  - Notes: Move user/profile/Auth0 ownership into Identity.

- [ ] Phase 11 - Competition extraction
  - Branch: `refactor/phase-11`
  - PR: pending
  - Notes: Move games, matches, registrations, rosters, and placements.

- [ ] Phase 12 - Sponsorship extraction
  - Branch: `refactor/phase-12`
  - PR: pending
  - Notes: Move sponsor ownership and document placement ownership.

- [ ] Phase 13 - Media extraction
  - Branch: `refactor/phase-13`
  - PR: pending
  - Notes: Move file/image storage concerns.

- [ ] Phase 14 - Discovery/Search extraction
  - Branch: `refactor/phase-14`
  - PR: pending
  - Notes: Move search into projections and Discovery.

- [ ] Phase 15 - Persistence boundary tightening
  - Branch: `refactor/phase-15`
  - PR: pending
  - Notes: Move mappings and tighten persistence ownership.

- [ ] Phase 16 - Endpoint simplification in place
  - Branch: `refactor/phase-16`
  - PR: pending
  - Notes: Resource-oriented cleanup with OpenSpec. No `/v2`.

- [ ] Phase 17 - Tighten internals and public surface
  - Branch: `refactor/phase-17`
  - PR: pending
  - Notes: Make module implementation details internal and add architecture tests.

- [ ] Phase 18 - Test suite reshaping
  - Branch: `refactor/phase-18`
  - PR: pending
  - Notes: Reshape tests around API, module behavior, events, and architecture.

- [ ] Phase 19 - Remove transitional adapters and clean up
  - Branch: `refactor/phase-19`
  - PR: pending
  - Notes: Delete old paths/adapters and run final architecture checks.

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

- The last checked item is the PR that must be verified before any new phase starts.
- If that PR is open, wait for human review/merge.
- If that PR is closed without merge, ask for human direction before continuing.
- If that PR is merged, start the next unchecked phase from the updated `refactor/modular-monolith`.
- If a phase is split, add the split PRs as separate checked items when each PR is created.
