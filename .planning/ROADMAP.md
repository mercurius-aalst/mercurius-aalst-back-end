# Roadmap: Mercurius API Domain Restructuring (v1)

**Date:** 2026-04-29  
**Mode:** YOLO  
**Granularity:** Standard  
**Execution Preference:** Parallel plans where dependencies allow

## Summary

- **Phases:** 6
- **v1 Requirements:** 19
- **Mapped Requirements:** 19/19
- **Coverage:** 100%

| # | Phase | Goal | Requirements |
|---|-------|------|--------------|
| 1 | .NET 10 Baseline | Upgrade runtime/tooling baseline safely before domain refactor | PLAT-01, PLAT-02 |
| 2 | Identity + Game Mode Model | Simplify participant identity and enforce game mode invariants | IDTY-01, IDTY-02, IDTY-03, MODE-01, MODE-02 |
| 3 | Match Model + Bracket Compatibility | Align matches to new mode model while preserving generation behavior | MODE-03, BRKT-01, BRKT-02, BRKT-03 |
| 4 | Historical Resilience | Make historical records deletion-safe and readable | HIST-01, HIST-02 |
| 5 | API Contract Restructure (v1) | Align DTOs/endpoints with new domain contracts | API-01, API-02, API-03 |
| 6 | Validation + Regression Hardening | Lock in reliable behavior with validation and test coverage | QLTY-01, QLTY-02, QLTY-03 |

## Phase Details

## Phase 1: .NET 10 Baseline

**Goal:** Upgrade the project baseline to .NET 10 without introducing unnecessary churn.

**Requirements:** `PLAT-01`, `PLAT-02`

**Success criteria:**
1. App and test projects target .NET 10.
2. CI workflow and container build/runtime use .NET 10 images/tooling.
3. At least one .NET 10 feature with clear backend value is selected and planned (for example Minimal API built-in validation).
4. No nonessential platform rewrites are introduced.

**UI hint:** no

**Progress:** Plan 01 complete. PLAT-01 delivered across project files, CI, and Docker. PLAT-02 remains for Plan 02.

## Phase 2: Identity + Game Mode Model

**Goal:** Replace legacy participant complexity with a simpler identity model and explicit game mode invariants.

**Requirements:** `IDTY-01`, `IDTY-02`, `IDTY-03`, `MODE-01`, `MODE-02`

**Success criteria:**
1. User is the canonical identity in participation flows.
2. Legacy Player/Participant abstractions are removed or collapsed from active contracts.
3. Game creation explicitly sets team or individual mode.
4. Mixed-mode participation in a single game is rejected by domain/service logic.

**UI hint:** no

## Phase 3: Match Model + Bracket Compatibility

**Goal:** Ensure match behavior and bracket generation remain correct under the new model.

**Requirements:** `MODE-03`, `BRKT-01`, `BRKT-02`, `BRKT-03`

**Success criteria:**
1. Match participation obeys game mode invariants.
2. Single elimination generation still produces valid graph output.
3. Double elimination generation still produces valid graph output.
4. Winner propagation and progression stay consistent for generated matches.

**UI hint:** no

## Phase 4: Historical Resilience

**Goal:** Prevent user removal from breaking competition history.

**Requirements:** `HIST-01`, `HIST-02`

**Success criteria:**
1. Historical games/matches remain queryable when user links are removed.
2. Historical participant display data remains readable from archived fields.
3. Delete behavior avoids destructive cascades into historical competition records.

**UI hint:** no

## Phase 5: API Contract Restructure (v1)

**Goal:** Deliver explicit v1 contracts aligned to the simplified backend model.

**Requirements:** `API-01`, `API-02`, `API-03`

**Success criteria:**
1. Updated DTOs and route handlers represent new domain shape unambiguously.
2. Breaking contract decisions are explicit and documented as v1 semantics.
3. OpenAPI output reflects current request/response behavior accurately.

**UI hint:** no

## Phase 6: Validation + Regression Hardening

**Goal:** Stabilize behavior with consistent validation and targeted regression tests.

**Requirements:** `QLTY-01`, `QLTY-02`, `QLTY-03`

**Success criteria:**
1. Request validation behavior is consistent across restructured endpoints.
2. Regression tests cover single/double elimination and mode invariants.
3. Regression tests cover user deletion resilience and contract-critical paths.
4. Test suite provides clear pass/fail signal for rollout readiness.

**UI hint:** no

---
*Roadmap generated: 2026-04-29*
