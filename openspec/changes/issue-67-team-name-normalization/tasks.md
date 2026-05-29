## 1. Persistence and Validation

- [ ] 1.1 Add `NormalizedName` or equivalent normalized lookup storage to `Team`.
- [ ] 1.2 Add EF mapping with max lengths and a unique index.
- [ ] 1.3 Create a migration that backfills normalized names and updates the snapshot.
- [ ] 1.4 Add team-name normalization and validation helpers.

## 2. Service Semantics

- [ ] 2.1 Update team creation to trim, validate, normalize, and reject duplicates.
- [ ] 2.2 Update team renaming to use normalized duplicate checks.
- [ ] 2.3 Catch database unique constraint violations and return safe validation errors.
- [ ] 2.4 Add normalized lookup helpers for public team profile and search features.

## 3. Regression Coverage

- [ ] 3.1 Add tests for duplicate create and update across casing.
- [ ] 3.2 Add tests for normalized lookup and search behavior.
- [ ] 3.3 Add tests for invalid names and migration/backfill expectations.
- [ ] 3.4 Run `dotnet test` for the solution.
