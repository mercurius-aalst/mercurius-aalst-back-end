# Requirements: Mercurius API Domain Restructuring (v1)

**Defined:** 2026-04-29  
**Core Value:** Admins can create a game and generate matches in the chosen bracket format (single or double elimination) with a simple, reliable model.

## v1 Requirements

### Platform Baseline

- [x] **PLAT-01**: Backend targets .NET 10 across application, tests, CI, and container build/runtime definitions.
- [ ] **PLAT-02**: .NET 10 features are adopted only where they produce a clear backend improvement compared to current behavior.

### Identity & Participation Model

- [ ] **IDTY-01**: User is the canonical person identity used in participation flows.
- [ ] **IDTY-02**: Player is removed or fully collapsed into the user-based model in API and service contracts.
- [ ] **IDTY-03**: Participant as a generic standalone abstraction is removed or replaced by explicit mode-aware participation structures.

### Competition Mode Invariants

- [ ] **MODE-01**: Game creation requires choosing either team-based mode or individual-based mode.
- [ ] **MODE-02**: A game cannot hold team and individual participants simultaneously.
- [ ] **MODE-03**: Match participation follows the same mode constraint as its game.

### Match Generation & Lifecycle

- [ ] **BRKT-01**: Single-elimination match generation remains functional after restructuring.
- [ ] **BRKT-02**: Double-elimination match generation remains functional after restructuring.
- [ ] **BRKT-03**: Generated match graph progression remains valid under the new participant model.

### Historical Resilience

- [ ] **HIST-01**: Removing a user must not break existing games, matches, or teams.
- [ ] **HIST-02**: Historical records preserve readable participant display data even when live links no longer exist.

### API Contracts

- [ ] **API-01**: API DTOs and endpoints are restructured to match the simplified domain model.
- [ ] **API-02**: Breaking changes are allowed under explicit v1 contract semantics.
- [ ] **API-03**: Contract documentation and response behavior remain consistent across updated endpoints.

### Quality & Validation

- [ ] **QLTY-01**: Request validation behavior is explicit and consistent for updated endpoints.
- [ ] **QLTY-02**: Regression tests cover bracket generation and mode invariants under the restructured model.
- [ ] **QLTY-03**: Regression tests cover deletion resilience and key contract behavior changes.

## v2 Requirements

### Extended Tournament Functionality

- **V2BR-01**: Revisit additional tournament formats beyond single and double elimination.
- **V2RB-01**: Revisit ranking/rating logic redesign when domain restructuring stabilizes.

## Out of Scope

| Feature | Reason |
|---------|--------|
| UI redesign or frontend scope | This milestone is backend domain and contract restructuring only |
| Ranking logic redesign | Explicitly excluded by project direction |
| Authentication/authorization redesign | Existing role-based auth remains in place for this milestone |
| Production data migration optimization | No active dataset constraint; simplicity is prioritized |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PLAT-01 | Phase 1 | Complete via 01-01 |
| PLAT-02 | Phase 1 | Pending |
| IDTY-01 | Phase 2 | Pending |
| IDTY-02 | Phase 2 | Pending |
| IDTY-03 | Phase 2 | Pending |
| MODE-01 | Phase 2 | Pending |
| MODE-02 | Phase 2 | Pending |
| MODE-03 | Phase 3 | Pending |
| BRKT-01 | Phase 3 | Pending |
| BRKT-02 | Phase 3 | Pending |
| BRKT-03 | Phase 3 | Pending |
| HIST-01 | Phase 4 | Pending |
| HIST-02 | Phase 4 | Pending |
| API-01 | Phase 5 | Pending |
| API-02 | Phase 5 | Pending |
| API-03 | Phase 5 | Pending |
| QLTY-01 | Phase 6 | Pending |
| QLTY-02 | Phase 6 | Pending |
| QLTY-03 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 19 total
- Mapped to phases: 19
- Unmapped: 0

---
*Requirements defined: 2026-04-29*  
*Last updated: 2026-04-29 after initial definition*
