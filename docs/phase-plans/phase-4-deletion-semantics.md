# Phase 4 — Deletion Semantics Hardening

## 1) Purpose
Guarantee safe user deletion/deactivation without invalidating historical competition records.

## 2) Inputs
- Refactored user-centric service and schema model.
- Existing deletion behavior and relation constraints.

## 3) Detailed deliverables
1. **Relationship rule hardening**
   - Replace unsafe cascades with `Restrict` or `SetNull` where history must persist.
2. **Deletion/deactivation workflow**
   - Explicit command flow with validation, detach/reassign/archive policies.
3. **End-to-end deletion regression tests**
   - Delete/deactivate then read/update game, match, team resources.
4. **Operational runbook snippet**
   - Safe deletion procedures and troubleshooting notes.

## 4) Work breakdown
- Update EF relation config and generate migration if needed.
- Implement service-level guards and deterministic outcomes.
- Verify historical records remain readable and consistent.
- Add integration tests for multiple role/team membership scenarios.

## 5) Acceptance criteria
- No destructive cascade into historical game/match data from user deletion paths.
- Supported deletion/deactivation flows complete without referential errors.
- Regression tests pass for delete-then-read/update scenarios.

## 6) Risks and mitigations
- **Risk:** Orphaned links with unclear presentation.
  - **Mitigation:** Standard fallback payload fields and status flags.
- **Risk:** Unexpected constraint failures in legacy data.
  - **Mitigation:** Data cleanup pre-check and phased rollout.

## 7) Output artifacts
- EF configuration/migration updates.
- Deletion workflow service code.
- Integration test coverage and runbook notes.
