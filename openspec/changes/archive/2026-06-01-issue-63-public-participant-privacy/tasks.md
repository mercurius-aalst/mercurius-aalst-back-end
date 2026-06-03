## 1. Public DTOs and Mapping

- [x] 1.1 Add one privacy-safe public user DTO for embedded participants.
- [x] 1.2 Update game and placement mappings to use the privacy-safe user DTO.
- [x] 1.3 Update team member mappings and omit invite collections from shared team responses.
- [x] 1.4 Keep existing admin/current-user DTOs unchanged for authorized workflows.

## 2. Query and Route Behavior

- [x] 2.1 Review anonymous `AllowAnonymous` routes and ensure each selects the correct public DTO.
- [x] 2.2 Include privacy-policy-approved Discord, Steam, and Riot IDs in shared participant responses.
- [x] 2.3 Preserve dedicated authorized endpoints for workflows that require private data.

## 3. Regression Coverage

- [x] 3.1 Add anonymous response-shape tests for games, placements, and teams.
- [x] 3.2 Add regression tests proving public platform identifiers remain available in embedded participant responses.
- [x] 3.3 Add admin/current-user response tests to prove full authorized data remains available.
- [x] 3.4 Run `dotnet test` for the solution.
