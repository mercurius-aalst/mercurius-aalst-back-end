# Phase 0 — Discovery and Failure Reproduction

## 1) Purpose
Create a factual baseline of current coupling and lock known failure modes into tests before refactoring.

## 2) Inputs
- Existing entities and EF relationship mappings.
- Existing service/controller paths for game/match/team workflows.
- Current issue requirements:
  - #47: remove Player/Participant abstractions.
  - #50: simplify and harden relation behavior.

## 3) Detailed deliverables
1. **Relation Matrix**
   - Every entity relation with cardinality, optionality, delete behavior, and ownership semantics.
   - "Historical data critical" marker for each relation.
2. **Traversal Inventory**
   - List of all deep relation walks in services/controllers.
   - Risk tags: null-fragile, performance-sensitive, deletion-sensitive.
3. **Failure Reproduction Tests**
   - One failing case per known breakage category.
4. **Refactor Impact Assessment**
   - High/medium/low classification with estimated change blast radius.

## 4) Work breakdown
- Extract model relationship metadata from DB context and model classes.
- Build a route-to-service-to-entity dependency map.
- Author failing tests for:
  - delete user/player then fetch game/match/team
  - participant-missing assumptions in match generation
- Document baseline API behavior and identify unstable contracts.

## 5) Acceptance criteria
- Matrix and traversal inventory reviewed by maintainers.
- Failing test suite captures all known bug classes discussed in #47/#50.
- Risks prioritized and linked to future phase tasks.

## 6) Risks and mitigations
- **Risk:** Incomplete dependency mapping.
  - **Mitigation:** Pair route inventory with code search on entity names.
- **Risk:** False negatives in failing tests.
  - **Mitigation:** Add fixture realism (teams, matches, historical records).

## 7) Output artifacts
- `docs/internal/relation-matrix.md`
- `docs/internal/traversal-inventory.md`
- New failing tests in service/integration test projects.
