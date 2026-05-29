## 1. Test Infrastructure

- [x] 1.1 Add reusable fixture builders for users, teams, games, matches, sponsors, and placements.
- [x] 1.2 Add serialized response assertion helpers for required and forbidden fields.
- [x] 1.3 Add a minimal route-level test harness if needed for authorization, status codes, and route precedence.

## 2. Contract Coverage

- [x] 2.1 Add tests for game list/detail responses and tournament/match schedule fields.
- [x] 2.2 Add tests for match score update and match detail responses.
- [x] 2.3 Add tests for sponsor list/detail and game sponsor placement responses.
- [x] 2.4 Add tests for public search result shape, limits, and privacy.
- [x] 2.5 Add tests for public user profile and public team profile response shapes.
- [x] 2.6 Add tests for current-user profile completion/update/username availability flows.
- [x] 2.7 Add tests for admin username deletion route compatibility.

## 3. Privacy, Security, and Verification

- [x] 3.1 Add anonymous privacy regression tests across public endpoints.
- [x] 3.2 Add deleted/incomplete user exclusion tests for public search and profile endpoints.
- [x] 3.3 Add anonymous and non-admin rejection tests for admin-only endpoints.
- [x] 3.4 Add bounded search result tests and practical projection/payload assertions.
- [x] 3.5 Run `dotnet test` for the solution.
