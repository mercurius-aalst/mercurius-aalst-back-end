# Coding Conventions

**Analysis Date:** 2026-04-29

## Naming Patterns

**Files:**
- Use PascalCase for C# source files and match the primary type name: `src/MercuriusAPI/Models/Team.cs`, `src/MercuriusAPI/Services/TeamServices/TeamService.cs`, `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`.
- Use `I`-prefixed interface names beside implementations: `src/MercuriusAPI/Services/TeamServices/ITeamService.cs` with `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
- Use feature-suffixed DTO filenames under feature folders: `src/MercuriusAPI/DTOs/TeamDTOs/CreateTeamDTO.cs`, `src/MercuriusAPI/DTOs/PlayerDTOs/UpdatePlayerDTO.cs`, `src/MercuriusAPI/DTOs/Auth/LoginRequest.cs`.
- Test files are named by domain model or behavior area: `tests/MercuriusAPI.Tests/TeamTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`, `tests/MercuriusAPI.Tests/GameTests.cs`.

**Functions:**
- Use PascalCase for methods and append `Async` to asynchronous service methods: `CreateTeamAsync`, `GetTeamByIdAsync`, `DeleteTeamAsync` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
- Use `Map{Feature}Endpoints` for minimal API route registration extension methods: `MapTeamEndpoints` in `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`.
- Use action-oriented domain method names on models: `Update`, `RemovePlayer`, `InvitePlayer` in `src/MercuriusAPI/Models/Team.cs`; `Start`, `Complete`, `Reset` in `src/MercuriusAPI/Models/Game.cs`.
- Use helper methods with clear creation intent in tests: `CreateGame`, `CreateMatch`, `CreatePlayer`, and `GetFixture` in `tests/MercuriusAPI.Tests/GameTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`, and `tests/MercuriusAPI.Tests/TeamTests.cs`.

**Variables:**
- Use camelCase for locals and parameters: `teamDTO`, `captain`, `playerId`, and `inviteResendCooldownDays` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
- Use `_camelCase` for private readonly fields: `_dbContext` and `_inviteResendCooldownDays` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`; `_next` in `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`.
- Use descriptive domain names rather than abbreviations: `playerToInvite`, `lastDeclinedInvite`, `daysSinceDeclined`, and `jwtSettings` in `src/MercuriusAPI/Models/Team.cs` and `src/MercuriusAPI/Program.cs`.

**Types:**
- Use PascalCase for classes, records, enums, and interfaces: `Team`, `TeamInvite`, `TeamInviteStatus`, `ITeamService`, `ExceptionHandlingMiddleware`.
- Use suffixes that communicate role: `Service`, `DTO`, `Request`, `Response`, `Exception`, `Middleware`, `Factory`, `Customization`, as seen in `src/MercuriusAPI/Services/Auth/AuthService.cs`, `src/MercuriusAPI/DTOs/Auth/AuthTokenResponse.cs`, and `tests/MercuriusAPI.Tests/Customizations/MatchParticipantCustomization.cs`.
- Keep namespaces aligned with folder structure under `Mercurius.LAN.API`: `Mercurius.LAN.API.Services.TeamServices` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`; `Mercurius.LAN.API.Tests` in `tests/MercuriusAPI.Tests/TeamTests.cs`.

## Code Style

**Formatting:**
- Formatting is standard C#/.NET SDK style with file-scoped namespaces: `namespace Mercurius.LAN.API.Models;` in `src/MercuriusAPI/Models/Team.cs`.
- Nullable reference types and implicit usings are enabled in `src/MercuriusAPI/Mercurius.LAN.API.csproj` and `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`; write new code with nullable annotations such as `string?` and `int?`.
- Prefer expression-bodied members only for very small pass-through code. The codebase mostly uses block-bodied methods for services and domain behavior, as in `src/MercuriusAPI/Services/PlayerServices/PlayerService.cs`.
- Use modern collection expressions where already present: `IEnumerable<GetTeamPlayerDTO> Players { get; set; } = [];` in `src/MercuriusAPI/DTOs/TeamDTOs/GetTeamDTO.cs`.
- No `.editorconfig`, `Directory.Build.props`, or analyzer ruleset is present. Match surrounding file formatting when adding code.

**Linting:**
- Not detected. There is no repository-level `.editorconfig`, Roslyn analyzer configuration, StyleCop configuration, or lint command in the solution.
- XML documentation output is enabled in `src/MercuriusAPI/Mercurius.LAN.API.csproj`, but warnings `CS1591`, `CS8602`, and `CS8618` are suppressed there. Do not rely on missing-doc or nullable initialization warnings to catch issues.

## Import Organization

**Order:**
1. Project namespaces first, usually `Mercurius.LAN.API.*`: `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
2. Microsoft framework namespaces next: `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Authorization`, `Microsoft.IdentityModel.Tokens`.
3. System namespaces last when present: `System.Text`, `System.Text.Json.Serialization`, `System.Net`.

**Path Aliases:**
- No C# path aliases are used. Use normal namespaces rooted at `Mercurius.LAN.API` and `Mercurius.LAN.API.Tests`.
- Use project references rather than package-style imports for test access: `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj` references `src/MercuriusAPI/Mercurius.LAN.API.csproj`.

## Error Handling

**Patterns:**
- Throw domain-specific exceptions for expected user and entity failures: `ValidationException` and `NotFoundException` in `src/MercuriusAPI/Exceptions/ValidationException.cs` and `src/MercuriusAPI/Exceptions/NotFoundException.cs`.
- Services validate persistence and business preconditions, then throw exceptions instead of returning null or status objects: `GetPlayerByIdAsync` and `CreatePlayerAsync` in `src/MercuriusAPI/Services/PlayerServices/PlayerService.cs`; `CreateTeamAsync` and `DeleteTeamAsync` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
- Domain models enforce invariant rules directly: `Team.RemovePlayer` and `Team.InvitePlayer` in `src/MercuriusAPI/Models/Team.cs`; `Game.Start`, `Game.Complete`, and `Game.Reset` in `src/MercuriusAPI/Models/Game.cs`.
- Minimal API exceptions are converted to JSON responses by `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`. Use the existing exception types so the middleware maps failures to 400, 401, 404, or 423 consistently.
- `src/MercuriusAPI/Exceptions/ExceptionFilter.cs` contains the same mapping pattern for MVC action filters, but the active `Program.cs` path registers `ExceptionHandlingMiddleware`; new minimal API code should follow the middleware path.

## Logging

**Framework:** Not detected

**Patterns:**
- No `ILogger<T>`, `Console.WriteLine`, Serilog, Application Insights, or other logging pattern is used in the C# source files.
- Do not introduce ad hoc console logging in services or models. If logging is needed, add `ILogger<T>` through dependency injection and keep messages at endpoint/service boundaries such as `src/MercuriusAPI/Services/Auth/AuthService.cs` or `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`.

## Comments

**When to Comment:**
- Use comments sparingly for non-obvious infrastructure or workaround context. Existing examples include JWT setup, migration seeding, Imageflow middleware setup, and cache notes in `src/MercuriusAPI/Program.cs`.
- Keep business-rule code self-describing through method and variable names. Model methods in `src/MercuriusAPI/Models/Team.cs` and `src/MercuriusAPI/Models/Game.cs` generally do not need inline comments.
- In tests, use `// Arrange`, `// Act`, and `// Assert` sections for longer tests, as in `tests/MercuriusAPI.Tests/TeamTests.cs` and `tests/MercuriusAPI.Tests/MatchTests.cs`.
- Use short comments when test setup must compensate for EF Core behavior, such as the manual navigation assignment comment in `tests/MercuriusAPI.Tests/TeamTests.cs`.

**JSDoc/TSDoc:**
- Not applicable for C#.
- XML documentation is generated by `src/MercuriusAPI/Mercurius.LAN.API.csproj`, but public members do not consistently include XML comments and missing-comment warnings are suppressed.

## Function Design

**Size:** Keep endpoint handlers thin and delegate domain work to services. `src/MercuriusAPI/Endpoints/TeamEndpoints.cs` resolves route inputs and services, then calls `ITeamService` or `IPlayerService`.

**Parameters:** Pass DTOs for request bodies and primitive route values separately. Examples: `CreateTeamDTO createTeamDTO` plus injected services in `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`; `UpdatePlayerDTO playerDTO` plus `int id` in `src/MercuriusAPI/Services/PlayerServices/PlayerService.cs`.

**Return Values:** Return DTOs from service methods that create or update API-facing entities, and return domain models only when another service layer needs the entity. `TeamService.CreateTeamAsync` returns `GetTeamDTO`, while `TeamService.GetTeamByIdAsync` returns `Team` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.

## Module Design

**Exports:** Use public classes and interfaces per file. Endpoint modules are public static classes with extension methods, such as `src/MercuriusAPI/Endpoints/GameEndpoints.cs` and `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`.

**Barrel Files:** Not used. There are no C# aggregate export files; register cross-cutting dependencies in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

**Dependency Injection:** Register service interfaces and decorators centrally in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`. Use `AddTransient` for request-scoped business services, Scrutor `Decorate` for validation decorators, and `AddSingleton` for stateless/shared services such as token and login attempt services.

**Endpoint Layout:** Add new HTTP routes as extension methods under `src/MercuriusAPI/Endpoints/`, call them from `src/MercuriusAPI/Program.cs`, and keep authorization/tag configuration at the route group level.

**DTO Layout:** Add request/response DTOs under the matching `src/MercuriusAPI/DTOs/{Feature}DTOs/` folder. Map domain models to DTOs in constructors when following `src/MercuriusAPI/DTOs/TeamDTOs/GetTeamDTO.cs`.

---

*Convention analysis: 2026-04-29*
