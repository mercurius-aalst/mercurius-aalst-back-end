## Why

User identity fields are used for login linkage, profile lookup, and admin/user workflows. The database currently enforces uniqueness for Auth0 IDs and active normalized usernames, but it does not explicitly enforce uniqueness for active usernames or email snapshots.

## What Changes

- Add database-level uniqueness guarantees for active user usernames, active normalized usernames, active user emails, and Auth0 user IDs.
- Keep `Id` as the primary key for users.
- Preserve soft-deletion/anonymization behavior by excluding deleted users and null optional values from optional-field uniqueness checks.
- Add migration and test coverage for the user identity indexes.

## Capabilities

### New Capabilities
- `user-identity-uniqueness`: Defines database-backed uniqueness for user identity fields.

### Modified Capabilities

## Impact

- Affected code: `MercuriusDBContext`, EF Core migrations, model snapshot, and focused migration tests.
- APIs: No response shape changes.
- Dependencies: No new packages.
- Data: Existing duplicate active usernames or emails must be resolved before applying the migration in an environment with conflicting data.
