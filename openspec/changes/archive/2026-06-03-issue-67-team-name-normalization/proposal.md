## Why

The redesigned front-end uses team names as public route identifiers, but the back-end currently stores team names as display strings and checks duplicates with case-sensitive service logic. Public routing needs stable, case-insensitive, and database-enforced team-name semantics.

## What Changes

- Add normalized team-name storage or an equivalent robust case-insensitive uniqueness mechanism.
- Backfill normalized team names for existing teams in a migration.
- Enforce database-level uniqueness for team names regardless of casing.
- Update create/update validation and service lookups to use normalized names.
- Update public team lookup and search semantics to use the normalized value.
- Add tests for create, update, lookup, search, duplicates, and migration/backfill behavior.

## Capabilities

### New Capabilities
- `team-name-normalization`: Case-insensitive team-name uniqueness, validation, lookup, and persistence guarantees.

### Modified Capabilities
- None.

## Impact

- `Team` model and `MercuriusDBContext` mapping.
- EF migration and model snapshot.
- `TeamService` create/update/lookup behavior.
- Public team profile and public search features.
- `TeamTests` and related service/search tests.
