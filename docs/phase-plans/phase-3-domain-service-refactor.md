# Phase 3 — Domain and Service Refactor

## 1) Purpose
Switch runtime logic to explicit team-vs-individual strategies and remove active dependency on Player/Participant abstractions.

## 2) Inputs
- Completed data transitions from Phase 2.
- Compatibility adapter and service touchpoints from Phase 1.

## 3) Detailed deliverables
1. **Typed Strategy Layer**
   - Distinct handling for team and individual game/match operations.
2. **Service rewrite for core flows**
   - Create/read/update logic based on user-centric relations.
3. **Mapping overhaul**
   - DTO/view-model mapping resilient to missing/deactivated users.
4. **Error behavior standardization**
   - Consistent not-found/inactive handling.

## 4) Work breakdown
- Implement strategy-dispatch for game/match operations.
- Refactor services to eliminate participant traversal paths.
- Update endpoint mappers and response builders.
- Add regression tests for both team and individual modes.

## 5) Acceptance criteria
- No active business path requires Player/Participant runtime resolution.
- Both competition modes pass functional tests.
- Endpoint responses remain deterministic in edge cases.

## 6) Risks and mitigations
- **Risk:** Behavioral drift in bracket logic.
  - **Mitigation:** Golden regression tests for bracket scenarios.
- **Risk:** Performance regressions from lookup changes.
  - **Mitigation:** Benchmark hot endpoints and optimize query includes/indexes.

## 7) Output artifacts
- Updated services and strategy modules.
- Regression suite updates for mode coverage.
- API behavior notes for any intentional changes.
