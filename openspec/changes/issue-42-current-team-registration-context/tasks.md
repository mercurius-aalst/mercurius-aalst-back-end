## 1. Contract and read model

- [x] 1.1 Define the backward-compatible `CurrentTeamRegistration` authenticated response field
      and preserve active-only `ActiveTeamRegistration` semantics.
- [x] 1.2 Populate current team context for any pending or active roster registration without
      changing public registration projections.

## 2. Validation

- [x] 2.1 Add service tests covering a confirmed member whose team remains pending because another
      roster member has not confirmed.
- [x] 2.2 Verify authorization/privacy boundaries and run backend build, tests, and OpenSpec
      validation.
