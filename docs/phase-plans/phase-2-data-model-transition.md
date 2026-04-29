# Phase 2 — Data Model Transition

## 1) Purpose
Move persistence model from Player/Participant-centric storage to User-centric and typed game/match representation.

## 2) Inputs
- Compatibility abstraction from Phase 1.
- Existing production-like seeded snapshots.

## 3) Detailed deliverables
1. **Migration A — Profile consolidation**
   - Move required Player profile fields to User-owned storage.
2. **Migration B — Team membership update**
   - Replace team-player with team-user relation.
3. **Migration C — Participant transition**
   - Introduce typed references for team/individual match and game modeling.
4. **Backfill + Validation scripts**
   - Idempotent scripts with row-count and constraint checks.
5. **Rollback guidance**
   - Explicit steps and preconditions for rollback.

## 4) Work breakdown
- Design additive-first migration sequence.
- Run dry-run migration on snapshot data.
- Backfill and verify field-level correctness.
- Mark old structures deprecated but still readable until Phase 5.

## 5) Acceptance criteria
- Required player-owned data available under user ownership.
- Team membership fully operational with users.
- New writes no longer depend on participant-centric schema.

## 6) Risks and mitigations
- **Risk:** Data loss during consolidation.
  - **Mitigation:** Pre/post migration audits + backup snapshot.
- **Risk:** Non-idempotent backfill behavior.
  - **Mitigation:** Safe upsert semantics and repeat-run checks.

## 7) Output artifacts
- Migration scripts and metadata.
- Backfill tooling + validation report.
- Schema change notes for reviewers.
