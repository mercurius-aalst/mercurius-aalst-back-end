## 1. Public DTOs and Mapping

- [x] 1.1 Add public participant DTOs for users, teams, team members, and placement participants.
- [x] 1.2 Update public game and placement mappings to use privacy-safe DTOs.
- [x] 1.3 Update public team mappings or split public team responses from admin team responses.
- [x] 1.4 Keep existing admin/current-user DTOs unchanged for authorized workflows.

## 2. Query and Route Behavior

- [x] 2.1 Review anonymous `AllowAnonymous` routes and ensure each selects the correct public DTO.
- [x] 2.2 Add explicit authenticated-public visibility handling for platform IDs where allowed.
- [ ] 2.3 Tighten public projections/includes to avoid loading unnecessary private user data.

## 3. Regression Coverage

- [x] 3.1 Add anonymous response-shape tests for games, placements, and teams.
- [x] 3.2 Add authenticated public response tests for allowed platform identifier visibility.
- [x] 3.3 Add admin/current-user response tests to prove full authorized data remains available.
- [x] 3.4 Run `dotnet test` for the solution.
