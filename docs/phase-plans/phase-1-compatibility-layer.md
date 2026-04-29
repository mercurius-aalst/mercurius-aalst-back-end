# Phase 1 — Compatibility Layer (Bridge Release)

## 1) Purpose
Decouple callers from `Player` directly and introduce a stable user-centric resolution layer.

## 2) Inputs
- Phase 0 relation/traversal inventory.
- Existing game/match team orchestration services.

## 3) Detailed deliverables
1. **Identity Resolution Abstraction**
   - Interface for resolving competition identities without exposing legacy storage model.
2. **Dual-path Adapter**
   - Supports legacy player-backed and new user-backed lookup paths.
3. **Incremental Service Adoption**
   - High-traffic game/match endpoints switched first.
4. **Contract Guard Tests**
   - Ensure no endpoint response regression during compatibility period.

## 4) Work breakdown
- Define resolver contracts and DTO shapes.
- Implement adapter with telemetry for path usage.
- Replace direct player lookups in prioritized services.
- Add tests for mixed data states (legacy + transitioned).

## 5) Acceptance criteria
- Critical flows resolve identities through abstraction only.
- No outward API shape drift introduced in this phase.
- Telemetry/logging can detect unresolved or ambiguous mappings.

## 6) Risks and mitigations
- **Risk:** Ambiguous identity mapping during dual-mode operation.
  - **Mitigation:** Deterministic precedence and conflict logging.
- **Risk:** Broad refactor too early.
  - **Mitigation:** Limit adoption to highest-value paths first.

## 7) Output artifacts
- New resolver interface and implementation.
- Service updates in selected game/match flows.
- Unit tests for compatibility behavior.
