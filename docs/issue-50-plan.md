# Issue #50 Plan — Code simplification

## Issue context
Issue #50 (opened April 28, 2026) asks to simplify the codebase, with focus on **Game** and **Match** structures. It specifically requests minimizing hard references/relations and ensuring that deleting one user/player does not break connected matches/games/teams.

## Goals
- Reduce tight coupling between `Game`, `Match`, `Team`, `Participant`, and `Player`.
- Prevent entity deletions from causing cascading breakage or invalid object graphs.
- Keep current API behavior stable where possible while making relation handling safer.

## Non-goals
- Full domain rewrite.
- Changing bracket business logic behavior unless required by decoupling.
- Introducing breaking API changes without migration/compatibility path.

## Proposed approach

### 1) Baseline mapping of current coupling
- Inventory all direct entity references and navigation chains in:
  - `Models/*` (especially `Game`, `Match`, `Participant`, `Team`, `Player`)
  - `MercuriusDBContext` relationship configuration
  - Services that eagerly traverse those relations
- Create a quick dependency matrix: **who owns who**, **who can be null**, **what deletes cascade**.

### 2) Define safe ownership boundaries
- Treat `Game` and `Match` as aggregate roots with stable identifiers.
- Replace hard object dependencies where possible with ID-based references and guarded lookups at service boundaries.
- Introduce explicit "inactive/orphan-safe" behavior for removed users/players (e.g., soft deletion marker or placeholder participant semantics).

### 3) Refactor deletion semantics
- Update EF Core relationship rules to avoid dangerous cascade paths:
  - Prefer `Restrict`/`SetNull` for user/player links that should not destroy historical match/game records.
- Add service-layer guard rails:
  - Deleting a player/user should trigger controlled detachment/reassignment workflow.
  - Ensure teams/matches/games remain queryable and valid post-delete.

### 4) Simplify service logic around Game/Match
- Move relation resolution into clearly scoped helper methods.
- Remove implicit assumptions that every participant always maps to an active player record.
- Normalize error handling for missing related entities (consistent `NotFound`/validation behavior).

### 5) API contract hardening
- Ensure endpoint responses remain stable even when related user/player is missing.
- Return deterministic fallback payload values instead of null-reference failures.
- Document any response-shape changes in Swagger annotations/changelog if unavoidable.

## Work breakdown
1. **Discovery PR**
   - Add tests that reproduce current failure modes when deleting linked player/user.
   - Add relationship matrix notes (internal doc/comment).
2. **Data-model PR**
   - Adjust EF relationships and migration(s).
   - Implement deletion-safe entity configuration.
3. **Service-layer PR**
   - Refactor Game/Match flows to use safe lookup patterns and defensive mapping.
4. **Endpoint stabilization PR**
   - Ensure outward API behavior consistency and update docs.
5. **Cleanup PR**
   - Remove dead code, simplify duplicated relation traversal, finalize naming.

## Testing plan
- Unit tests:
  - Delete player/user linked to team/match/game should not break retrieval or updates.
  - Match generation/update still functions with inactive/missing linked player.
- Integration tests:
  - End-to-end delete scenario followed by reads on game, match, team endpoints.
  - Regression tests for bracket flows.
- Migration validation:
  - Apply migration on existing dev DB snapshot and verify no destructive data loss.

## Acceptance criteria
- Deleting a user/player does not break reads or writes for related matches/games/teams.
- Game/Match service code has reduced direct relation traversal and fewer hard object dependencies.
- Tests cover deletion safety scenarios and pass in CI.
- No unintended breaking API changes; any required changes are documented.

## Risks and mitigations
- **Risk:** Hidden assumptions in bracket/moderator logic.
  - **Mitigation:** Add focused regression tests per bracket type before refactor.
- **Risk:** Migration side effects on existing relation constraints.
  - **Mitigation:** Validate migration on seeded data and document rollback steps.
- **Risk:** Performance impact from replacing navigations with lookups.
  - **Mitigation:** Profile key endpoints and add targeted includes/indexes where needed.

## Delivery estimate
- Discovery + failure-case tests: 0.5–1 day
- Data model + migration: 0.5 day
- Service refactor + endpoint hardening: 1–2 days
- Regression testing + cleanup: 0.5–1 day

**Total:** ~2.5 to 4.5 engineering days


## Estimation basis
- The delivery estimate above is based on **one human engineer** working in this codebase with normal review/CI loops.
- It assumes selective AI assistance (drafting/refactor support), **not** a fully autonomous agent shipping directly to production.
- If executed by a highly capable autonomous agent with reliable repo context and test-fix loops, a best-case range could be lower (roughly **1 to 2.5 days**), but operational risk and verification overhead typically still require human sign-off.
