# AGENTS.md

## Scope
These instructions apply to the whole back-end repository.

## Project
Mercurius LAN party back-end API. The API source lives in `src/MercuriusAPI`, with tests in `tests/MercuriusAPI.Tests`.

## Stack
- ASP.NET Core Web API / minimal API endpoint mapping.
- Target framework: `net10.0`.
- Database: PostgreSQL via Entity Framework Core.
- Auth: Auth0-backed JWT bearer authentication.
- API versioning and Swagger are configured.
- CORS policy currently targets `https://*.mercurius-aalst.be`.
- Main solution: `LAN.API.sln`.

## OpenSpec requirements
This repository uses OpenSpec. Any functional behavior change must be accompanied by an OpenSpec change unless the task is explicitly limited to investigation, refactoring with no behavior change, documentation, formatting, or mechanical integration plumbing.

Before implementing a functional change:
1. Inspect existing specs in `openspec/specs/`.
2. If no suitable active change exists, create a new OpenSpec change under `openspec/changes/<change-id>/`.
3. Include at minimum:
   - `proposal.md`
   - `tasks.md`
   - spec deltas under `openspec/changes/<change-id>/specs/<capability>/spec.md`
4. Use RFC 2119 language in specs: MUST, SHOULD, MAY.
5. Keep implementation and tests aligned with the OpenSpec tasks.
6. When completing work, update the OpenSpec task checklist.

Integration-analysis-only work does not need a new OpenSpec change. Actual functional changes discovered during integration do need one.

## Common commands
Run commands from the repository root.

```bash
dotnet restore LAN.API.sln
dotnet build LAN.API.sln
dotnet test LAN.API.sln
dotnet run --project src/MercuriusAPI/Mercurius.LAN.API.csproj
```

## Important files and directories
- `src/MercuriusAPI/Program.cs`: application composition, auth, CORS, Swagger, middleware, endpoint registration.
- `src/MercuriusAPI/Endpoints/`: minimal API route definitions.
- `src/MercuriusAPI/DTOs/`: request/response contracts. Keep these aligned with the front-end `ILANClient`.
- `src/MercuriusAPI/Services/`: domain and application logic.
- `src/MercuriusAPI/Data/MercuriusDBContext.cs`: EF Core database context.
- `src/MercuriusAPI/Migrations/`: EF Core migrations.
- `tests/MercuriusAPI.Tests/`: automated tests.
- `openspec/`: requirements/specification source. Functional behavior changes require spec coverage.

## Front-end integration context
The redesigned front-end expects API support for:
- global search across users, teams, games, and tournaments;
- privacy-safe public user profile lookup by username;
- privacy-safe public team profile lookup by team name;
- sponsor tier, placement, context, and game-detail sponsor presentation;
- Auth0-backed current-user profile flows;
- admin username deletion compatibility;
- game, match, participant, placement, and schedule responses that match the front-end DTOs.

Several backend PRs already address these areas, including public global search, public user profiles, public team profiles, team-name normalization, privacy-safe participant projections, Auth0-backed profile migration, schedule estimation, and front-end contract tests.

## Integration rules
- Do not change response JSON shapes casually. Check the front-end `ILANClient`, DTOs, and mock data first.
- Public endpoints must not leak private user fields such as email, internal auth fields, deletion state, or private account metadata.
- Keep anonymous, authenticated, and admin-only authorization boundaries explicit.
- Add or update tests for every changed route, DTO shape, authorization rule, privacy rule, and validation behavior.
- Prefer minimal API endpoints consistent with the current endpoint organization.
- Prefer small, reviewable changes by endpoint/feature.
- Do not introduce new packages unless the existing stack cannot reasonably solve the task.
- If a required integration change alters API behavior, validation rules, auth behavior, privacy behavior, persistence behavior, or error handling, add or update OpenSpec coverage first.

## Code design rules
- Keep implementations straightforward and avoid unnecessary abstractions, wrapper methods, wrapper classes, and indirection that do not add clear readability, testability, performance, or maintainability value.
- Avoid code duplication, but also avoid unnecessary code de-duplication that creates brittle shared abstractions or hides feature-specific behavior.
- Reuse existing endpoints, services, DTOs, EF Core mappings, tests, and validation patterns where possible, but verify behavior to avoid regression failures.
- Keep one primary class, record, entity, DTO, endpoint group, or service per file unless the additional type is a small private nested implementation detail.
- Apply industry-standard design patterns only where they are appropriate and necessary for code cleanliness, performance, or long-term maintainability.
- Avoid N+1 database queries, repeated service calls, unnecessary materialization, redundant method invocations, and other performance bottlenecks caused by query or code invocation patterns.
- Keep dependencies between feature domains minimal. Prefer explicit contracts at boundaries rather than cross-domain coupling or shared mutable state.

## Before completing work
- Run `dotnet test LAN.API.sln`.
- Verify the relevant OpenSpec change exists for functional behavior changes.
- Update the relevant OpenSpec task checklist.
- Note any migration impact.
- Note any front-end contract impact.
- Note any CORS, Auth0, environment variable, or deployment configuration changes.
