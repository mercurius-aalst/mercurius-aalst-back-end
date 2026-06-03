## 1. Public Team Contract

- [x] 1.1 Add public team profile, public team member, and public team tournament DTOs.
- [x] 1.2 Add a team service method for public lookup by team name.
- [x] 1.3 Add `GET /v1/lan/public/teams/{teamName}` with anonymous access.

## 2. Lookup and Projection

- [x] 2.1 Validate and normalize team-name route input.
- [x] 2.2 Use normalized/case-insensitive lookup and align with the team-name normalization change.
- [x] 2.3 Project members to username-only data and omit invalid usernames.
- [x] 2.4 Project registered tournaments to game id and name only.
- [x] 2.5 Exclude all invite and private user data from the response.

## 3. Regression Coverage

- [x] 3.1 Add tests for successful and case-insensitive lookup.
- [x] 3.2 Add tests for missing team behavior.
- [x] 3.3 Add tests for username-only members, invite omission, and private-field omission.
- [x] 3.4 Add tests for tournament projection and stable ordering.
- [x] 3.5 Run `dotnet test` for the solution.
