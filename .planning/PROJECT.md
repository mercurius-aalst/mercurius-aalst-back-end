# Mercurius API Domain Restructuring (v1)

## What This Is

This is a backend-first restructuring of the Mercurius LAN API domain model and API contracts. The goal is to simplify core competition modeling by unifying user/player concepts and reducing brittle entity coupling across games, matches, and teams. It serves both tournament admins and participants, with admin behavior defined by roles.

## Core Value

Admins can create a game and generate matches in the chosen bracket format (single or double elimination) with a simple, reliable model.

## Requirements

### Validated

- ✓ Authentication and role-based authorization are operational (`src/MercuriusAPI/Endpoints/AuthEndpoints.cs`, `src/MercuriusAPI/Endpoints/UserEndpoints.cs`) — existing
- ✓ Admin/API flows for creating and managing games are operational (`src/MercuriusAPI/Endpoints/GameEndpoints.cs`, `src/MercuriusAPI/Services/GameServices/GameServices.cs`) — existing
- ✓ Match generation logic exists for single and double elimination (`src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs`) — existing
- ✓ Team and participant-management endpoints/services exist (`src/MercuriusAPI/Endpoints/TeamEndpoints.cs`, `src/MercuriusAPI/Endpoints/PlayerEndpoints.cs`) — existing

### Active

- [ ] Unify `Player` into `User` as a single participant identity model
- [ ] Remove `Participant` as a standalone entity and simplify participation modeling
- [ ] Ensure each game is either team-based or individual-based, never both simultaneously
- [ ] Apply the same team-vs-individual separation approach consistently to match participation
- [ ] Restructure API contracts to match the simplified model (breaking changes allowed for v1)
- [ ] Decouple historical match/game/team records from hard user references to avoid deletion breakage
- [ ] Preserve historical competition readability via archived display fields when user links are removed

### Out of Scope

- UI changes — backend restructuring milestone only
- Ranking logic rewrite — explicitly excluded from this phase
- Authentication/authorization redesign — existing auth model remains as-is
- Data migration optimization for existing production-scale datasets — no active dataset constraint for this milestone

## Context

- Repository: ASP.NET Core Minimal API with EF Core + PostgreSQL (`src/MercuriusAPI/Program.cs`, `src/MercuriusAPI/Data/MercuriusDBContext.cs`)
- Existing bracket logic already supports single and double elimination in dedicated moderators
- Open requirements source: GitHub issues `#47` and `#50` (both opened on 2026-04-28)
- Current domain model is considered over-complex, especially around player/participant and game/match relations
- This milestone prioritizes model clarity and maintainability over backward compatibility and migration concerns

## Constraints

- **Tech stack**: Keep EF Core + current PostgreSQL provider unchanged — minimize platform churn during restructuring
- **API versioning**: Breaking contracts are acceptable; may start from v1 API semantics — enables clean domain/API redesign
- **Domain rule**: A game must be either team-based or individual-based, never mixed — core invariant for model simplification
- **Scope**: No UI, ranking, or auth redesign in this milestone — focus on domain and API restructuring only

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Accept breaking API contracts for this restructure | Clean model boundaries are preferred over compatibility with old contracts | — Pending |
| Keep EF Core and current DB provider | Reduce moving parts; focus effort on domain simplification | — Pending |
| Preserve historical records using archived display fields and optional live user links | Prevent deletion cascades from breaking match/game history while keeping history readable | — Pending |
| Treat admins as role-based users, with both admins and participants in v1 audience | Aligns with existing auth model and target usage | — Pending |
| Keep single and double elimination as core supported competitive formats | Matches current proven value and implementation focus | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `$gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `$gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-29 after initialization*
