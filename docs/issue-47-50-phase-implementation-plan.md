# Issue #47 + #50 — Detailed Phase Implementation Plan

This document expands the unified strategy into concrete, execution-ready phase plans.

## Scope baseline
- Issue #47: unify players/users, remove `Player` and `Participant`, and move to explicit team/individual game-match modeling.
- Issue #50: simplify `Game`/`Match` coupling and enforce deletion-safe relationship behavior.

---

## Phase 0 — Discovery and failure reproduction

### Objective
Create a complete map of coupling points and lock in current failures with tests.

### Deliverables
1. **Coupling matrix document** (`docs/internal/relation-matrix.md`)
   - Entity-to-entity ownership and cardinality
   - Current delete behaviors
   - Service-layer deep traversal hotspots
2. **Failing regression tests** for current breakage scenarios
   - User/player deletion impacting game/match/team reads
   - Participant-dependent logic assumptions in match generation/read
3. **Refactor impact list**
   - Ranked by risk (high/medium/low)

### Implementation tasks
- Enumerate model and DB-context relations.
- Enumerate every service/controller path that reads `Player`/`Participant`.
- Add targeted tests that demonstrate current undesired outcomes.

### Exit criteria
- Coupling matrix reviewed and complete.
- At least one failing test per known failure class is present.
- Refactor hotspots prioritized.

---

## Phase 1 — Compatibility layer (bridge release)

### Objective
Introduce user-centric abstraction so downstream code can migrate incrementally.

### Deliverables
1. `ICompetitionIdentityResolver` (or equivalent) abstraction.
2. Adapter implementation that resolves identity via legacy player/user paths.
3. Service-level usage of resolver in high-traffic game/match flows.

### Implementation tasks
- Add resolver abstraction and default implementation.
- Refactor selected services to use resolver instead of direct `Player` assumptions.
- Add unit tests for resolver behavior (legacy + user-centric paths).

### Exit criteria
- Critical flows no longer directly depend on `Player` lookups.
- All new resolver tests pass.
- No API contract changes yet.

---

## Phase 2 — Data model transition (#47 core)

### Objective
Move schema/data toward user-first model and remove participant-centric storage.

### Deliverables
1. **Migration A: user profile consolidation**
   - Move required player data to user-owned structure.
2. **Migration B: team-user relation**
   - Introduce/upgrade join relation from team→user.
3. **Migration C: participant removal path**
   - Replace participant references with typed game/match ownership fields.
4. **Data backfill script**
   - Deterministic mapping and idempotent execution support.

### Implementation tasks
- Author additive migrations first (non-destructive).
- Backfill data and validate row counts/integrity constraints.
- Introduce deprecation markers on old columns/tables.

### Exit criteria
- All required player data is available through user-owned model.
- Team membership works with user references.
- Participant-dependent schema paths are deprecated and no longer required by new writes.

---

## Phase 3 — Domain/service refactor (#47 + #50 joint)

### Objective
Switch business logic to explicit team-vs-individual models and remove participant/player runtime dependency.

### Deliverables
1. Typed game/match handling strategy
   - `TeamGame` / `IndividualGame` handling path
   - `TeamMatch` / `IndividualMatch` handling path
2. Updated service orchestration for create/read/update flows.
3. Unified error-handling policy for missing/deactivated users.

### Implementation tasks
- Replace participant traversal branches with typed strategy resolution.
- Replace player-centric DTO mapping with user-centric mapping.
- Remove null-fragile relation walks in game/match services.

### Exit criteria
- Active flows do not require `Player` or `Participant`.
- Team and individual modes are both supported in tests.
- API responses remain deterministic for missing-linked-user scenarios.

---

## Phase 4 — Deletion semantics hardening (#50 core)

### Objective
Guarantee user deletion/deactivation safety across historical competition data.

### Deliverables
1. EF relationship rules updated for safe delete behavior.
2. Deletion workflow in service layer (detach/reassign/archive semantics).
3. Integration tests for delete-then-read/update across aggregates.

### Implementation tasks
- Replace unsafe cascades with `Restrict`/`SetNull` where appropriate.
- Implement guarded delete/deactivate command path.
- Add end-to-end tests against game/match/team endpoints.

### Exit criteria
- Deleting/deactivating a user does not break historical data retrieval.
- No referential-integrity exceptions in supported delete flows.
- Regression suite for deletion scenarios is green.

---

## Phase 5 — Legacy removal and cleanup

### Objective
Finalize migration by removing deprecated models, schema artifacts, and bridge code.

### Deliverables
1. Remove `Player` and `Participant` domain artifacts.
2. Drop deprecated DB columns/tables after verification window.
3. Remove compatibility resolver fallback logic no longer needed.
4. Update docs/changelog/swagger notes for finalized model.

### Implementation tasks
- Delete dead code and obsolete mappings.
- Run full regression suite and migration upgrade-from-snapshot tests.
- Validate no lingering compile/runtime references.

### Exit criteria
- Codebase compiles and tests pass without legacy entities.
- Schema is clean of deprecated artifacts.
- Documentation reflects final domain model.

---

## Cross-phase governance

### Required checkpoints (end of each phase)
- Architecture review sign-off
- Migration safety review (if schema touched)
- Test coverage delta review
- Rollback/readiness note

### Rollout strategy
- Prefer multiple small PRs per phase over one large PR.
- Keep migration PRs isolated from large service refactors when possible.
- Ship compatibility bridges before destructive schema removals.

### Suggested PR sequence
1. Phase 0 tests + relation matrix
2. Phase 1 compatibility abstraction
3. Phase 2 migrations/backfill
4. Phase 3 service/domain refactor
5. Phase 4 delete hardening
6. Phase 5 cleanup/removals

## Definition of done (program level)
- Issue #47 acceptance: no first-class `Player`/`Participant` dependency in active flows.
- Issue #50 acceptance: deletion-safe, simplified `Game`/`Match` relation behavior with stable APIs.
- Operational acceptance: upgrade + rollback paths validated on seeded snapshots.
