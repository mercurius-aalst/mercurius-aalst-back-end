## 1. Public contract

- [x] 1.1 Add public-safe summary records and response DTOs to the intentional module contracts.
- [x] 1.2 Add anonymous versioned user/team summary routes with profile validation and 404/empty
      behavior.

## 2. Projection implementation

- [x] 2.1 Query active individual/team registrations and candidate matches set-wise, selecting one
      previous/upcoming row per tournament with deterministic lifecycle-aware tie-breaks.
- [x] 2.2 Resolve opponent display labels from public registration snapshots in bounded work and
      preserve participant-relative scores/times without private lifecycle reports.
- [x] 2.3 Handle single/double formats, team/individual matches, BYE/TBD, canceled tournaments,
      forfeits, reversals, and unresolved states consistently.

## 3. Tests and validation

- [x] 3.1 Add projection tests for user/team active registrations, result ordering/tie-breaks,
      schedule ordering, lifecycle exclusions, BYE/TBD, and privacy shape.
- [x] 3.2 Add endpoint/OpenAPI/serialization tests and query-count/performance regressions.
- [x] 3.3 Run OpenSpec strict, build, test, and formatting validation; document any baseline-only
      formatting diagnostics.
