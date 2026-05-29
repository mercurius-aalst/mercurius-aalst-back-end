## 1. Public Team Contract

- [ ] 1.1 Add public team profile, public team member, and public team tournament DTOs.
- [ ] 1.2 Add a team service method for public lookup by team name.
- [ ] 1.3 Add `GET /v1/lan/public/teams/{teamName}` with anonymous access.

## 2. Lookup and Projection

- [ ] 2.1 Validate and normalize team-name route input.
- [ ] 2.2 Use normalized/case-insensitive lookup and align with the team-name normalization change.
- [ ] 2.3 Project members to username-only data and omit invalid usernames.
- [ ] 2.4 Project registered tournaments to game id and name only.
- [ ] 2.5 Exclude all invite and private user data from the response.

## 3. Regression Coverage

- [ ] 3.1 Add tests for successful and case-insensitive lookup.
- [ ] 3.2 Add tests for missing team behavior.
- [ ] 3.3 Add tests for username-only members, invite omission, and private-field omission.
- [ ] 3.4 Add tests for tournament projection and stable ordering.
- [ ] 3.5 Run `dotnet test` for the solution.
