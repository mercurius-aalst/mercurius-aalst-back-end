## Context

`Team` currently has only `Name`, and `TeamService.CheckIfTeamNameExistsAsync` compares `t.Name.Equals(name)`. That does not provide reliable case-insensitive uniqueness and cannot support efficient public route lookup by team name. Users already have a normalized username pattern that can guide team-name implementation.

## Goals / Non-Goals

**Goals:**
- Normalize team names consistently on create and update.
- Enforce case-insensitive uniqueness at the database level.
- Support efficient case-insensitive team lookup for public routes and search.
- Validate malformed, empty, and excessively long names safely.

**Non-Goals:**
- Adding team deletion soft-state unless required by current domain rules.
- Changing team display names beyond trimming/normalization rules.
- Implementing public team profile or search beyond enabling their lookup semantics.

## Decisions

- Add a `NormalizedName` column to `Team`, using a helper similar to `UserProfileValidationHelper.NormalizeUsername`.
- Keep `Name` as the display value and use `NormalizedName` for uniqueness and lookup.
- Add max length constraints for both display and normalized team names.
- Add a unique index on `NormalizedName`. If teams later gain soft deletion, the index can become filtered like usernames.
- Backfill normalized values in the migration from existing names and fail safely if duplicate historical data needs manual resolution.
- Update create/update checks to use normalized names and catch database unique constraint violations.

## Risks / Trade-offs

- Existing duplicate team names that differ only by casing will block a strict migration unless cleaned or resolved.
- PostgreSQL collation/citext could solve case-insensitivity, but an explicit normalized column aligns with the existing username approach and keeps route matching portable.
- Public search may be implemented before this change; service helpers should make the transition to indexed lookup straightforward.
