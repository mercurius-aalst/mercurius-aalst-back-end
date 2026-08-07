## 1. Data Model

- [x] 1.1 Add filtered unique indexes for active non-null `Username` and `Email`.
- [x] 1.2 Preserve existing primary key, Auth0 ID, and normalized username uniqueness.

## 2. Migration

- [x] 2.1 Add an EF Core migration for the new unique user identity indexes.
- [x] 2.2 Update the EF Core model snapshot.

## 3. Verification

- [x] 3.1 Add focused tests for the user identity uniqueness indexes.
- [x] 3.2 Run the backend test suite.
