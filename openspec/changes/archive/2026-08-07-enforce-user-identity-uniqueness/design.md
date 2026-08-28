## Context

`User.Id` is the backend primary key. `Auth0UserId` links local users to Auth0 identities, while `Username`, `NormalizedUsername`, and `Email` are optional snapshots used by profile and account workflows. The current EF model already has unique indexes for `Auth0UserId` and active non-null `NormalizedUsername`, but active `Username` and non-null active `Email` are not database-enforced.

## Goals / Non-Goals

**Goals:**
- Enforce user identity uniqueness at the database level.
- Keep soft-deleted users from blocking reuse of public-facing optional identity values.
- Keep null optional values valid for incomplete profiles.
- Keep `Id` as the sole user primary key.

**Non-Goals:**
- Change API response shapes or route behavior.
- Add new application-layer duplicate error mapping.
- Introduce case-insensitive email normalization beyond the stored email snapshot.

## Decisions

- Use EF Core unique indexes instead of only service-level duplicate checks.
  - Rationale: database constraints prevent race-condition duplicates and protect all write paths.
  - Alternative considered: service-only checks, rejected because concurrent writes can still race.
- Use filtered unique indexes for `Username`, `NormalizedUsername`, and `Email`.
  - Rationale: optional profile values may be null, and deleted users are anonymized/soft-deleted.
  - Alternative considered: unfiltered indexes, rejected because deleted or incomplete rows could block valid future values.
- Keep `Auth0UserId` unfiltered and required.
  - Rationale: every stored user must map to one Auth0 subject, including deleted users, and the field is already required.

## Risks / Trade-offs

- Existing active duplicate values could make the migration fail -> inspect and remediate duplicates before applying in shared environments.
- Email uniqueness follows the stored email string -> Auth0/provider email normalization remains outside this change.
- Optional filtered indexes preserve incomplete-profile support -> null emails/usernames can still appear on multiple rows.

## Migration Plan

- Add unique filtered indexes for `Users.Username` and `Users.Email`.
- Preserve the existing unique `Users.Auth0UserId` and filtered unique `Users.NormalizedUsername` indexes.
- Rollback drops only the newly added username and email indexes.

## Open Questions

- None.
