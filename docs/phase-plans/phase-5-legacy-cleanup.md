# Phase 5 — Legacy Removal and Cleanup

## 1) Purpose
Complete the transition by removing deprecated models, fallback code, and obsolete schema artifacts.

## 2) Inputs
- Stable production-like validation from Phases 3 and 4.
- Verification window confirming no legacy dependency.

## 3) Detailed deliverables
1. **Code cleanup**
   - Remove `Player` and `Participant` artifacts and compatibility shims.
2. **Schema cleanup migration**
   - Drop deprecated columns/tables and stale constraints.
3. **Documentation finalization**
   - Update architecture docs, API notes, and changelog.
4. **Post-migration verification report**
   - Confirm no lingering references and no behavior regressions.

## 4) Work breakdown
- Delete legacy domain files and dead mappings.
- Remove fallback branches from resolver/strategy code.
- Run full regression and migration validation suite.
- Publish final model diagram and maintenance notes.

## 5) Acceptance criteria
- Repository compiles/tests pass without legacy entities.
- Schema is free of deprecated #47/#50 transitional artifacts.
- Documentation reflects final, canonical model only.

## 6) Risks and mitigations
- **Risk:** Hidden runtime dependency on legacy objects.
  - **Mitigation:** Static search gates and startup self-checks.
- **Risk:** Documentation drift.
  - **Mitigation:** Require docs update checklist in PR template.

## 7) Output artifacts
- Cleanup PRs and final migration.
- Finalized architecture/API docs.
- Closure report for both issues.
