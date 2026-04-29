# Issue #47 + #50 Unified Implementation Plan — Player/User unification and relation simplification

## Source issues
- **Issue #47: "Unification of players and users"** (opened **April 28, 2026**).
  - `Player` should no longer be a separate entity.
  - `Participant` should be removed.
  - Player attributes (email, socials) should belong to `User`.
  - Teams should contain `User` references, not players.
  - On `Game` and `Match` levels, replace participant-based modeling with specialized forms for team-vs-individual logic.
- **Issue #50: "Code simplification"** (opened **April 28, 2026**).
  - Simplify `Game`/`Match` relations and reduce fragile hard references.
  - Ensure user deletion does not break related game/match/team data.

## Why these must be implemented together
Issue #47 changes core domain shape (`Player`/`Participant` removal), while issue #50 changes relation safety and traversal strategy. Implementing one without the other would likely create duplicate migrations, incompatible service assumptions, and temporary API instability.

## Target domain model (combined end state)

### Entity-level changes
1. **Remove `Player` as first-class domain entity**.
   - Migrate required player profile fields into `User` (or linked user-profile value object/table).
2. **Remove `Participant` entity**.
   - Replace with explicit game/match polymorphism:
     - `TeamGame` and `IndividualGame` (derived/specialized representations)
     - `TeamMatch` and `IndividualMatch` (or equivalent typed strategy)
3. **Team membership points to `User`**.
   - Team-user link table/relation replaces team-player relation.

### Relation-safety changes
1. Convert risky delete cascades from user links to safer behaviors (`Restrict`/`SetNull` where history is required).
2. Preserve historical competition records even after a user deletion/deactivation.
3. Replace deep navigation assumptions with explicit ID-based loading and guarded mapping.

## Implementation plan

### Phase 0 — Discovery and safety net
- Map current dependencies across:
  - `Models/*` (especially `Game`, `Match`, `Participant`, `Team`, `Player`, `User`)
  - DB context relationship configuration
  - Services/controllers assuming `Player`/`Participant`
- Add failing tests that reproduce:
  - user/player delete breakages
  - participant-dependent flows in game/match generation/read paths

### Phase 1 — Introduce compatibility layer
- Add transitional mapping so services can resolve user-centric identity while legacy entities still exist.
- Add adapter/helpers that encapsulate "current player source" to limit touching all call-sites at once.

### Phase 2 — Data model transition (#47 core)
- Migration set A:
  - copy/merge required player attributes into user-owned structure
  - create/upgrade team-user link
- Migration set B:
  - remove participant links in favor of typed game/match relations
- Keep reversible migration path with explicit rollback notes.

### Phase 3 — Service refactor (#47 + #50 joint)
- Replace participant traversal with typed game/match resolution paths.
- Replace player-centric service logic with user-centric logic.
- Normalize not-found/inactive-user behavior so responses are deterministic.

### Phase 4 — Deletion semantics hardening (#50 core)
- Enforce deletion workflows that detach/reassign links instead of breaking aggregates.
- Ensure read endpoints for game/match/team still return stable payloads after user deletion/deactivation.

### Phase 5 — Remove legacy artifacts
- Drop obsolete `Player` and `Participant` code paths and schema remnants.
- Remove temporary compatibility helpers.
- Final cleanup for duplicate traversal and naming.

## Test plan
- **Unit tests**
  - Team membership and game/match creation using `User` only.
  - Individual and team game/match flows without participant entity.
  - Deleting/deactivating user preserves historical game/match/team integrity.
- **Integration tests**
  - Full flow: create users/teams/games/matches → delete/deactivate user → re-read/update dependent resources.
  - Regression tests for bracket and result update flows.
- **Migration tests**
  - Upgrade from pre-#47 schema snapshot and verify data preservation.
  - Verify rollback path in non-production DB.

## Acceptance criteria
- No runtime dependency remains on `Player` or `Participant` in active domain flows.
- Teams reference users directly.
- Game/match logic supports explicit team vs individual modes without participant abstraction.
- Deleting/deactivating a user does not corrupt or invalidate historical competition records.
- API behavior remains stable, with documented and intentional response changes only.

## Delivery estimate
- Discovery + failing tests: 0.5–1 day
- Compatibility layer + migrations: 1–1.5 days
- Service refactor + deletion hardening: 1–2 days
- Cleanup + regression stabilization: 0.5–1 day

**Total:** ~3 to 5.5 engineering days
