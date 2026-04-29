<!-- refreshed: 2026-04-29 -->
# Architecture

**Analysis Date:** 2026-04-29

## System Overview

```text
┌─────────────────────────────────────────────────────────────┐
│                     ASP.NET Core Web API                    │
│                  `src/MercuriusAPI/Program.cs`              │
├──────────────────┬──────────────────┬───────────────────────┤
│ Minimal endpoints│ Auth/JWT/Swagger  │ Middleware/static IO  │
│ `src/MercuriusAPI│ `src/MercuriusAPI │ `src/MercuriusAPI/    │
│ /Endpoints`      │ /Extensions`      │ Middleware`           │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  Application service layer                  │
│                  `src/MercuriusAPI/Services`                │
│ Interfaces, implementations, decorators, tournament logic    │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│             Domain models + EF Core persistence             │
│ `src/MercuriusAPI/Models` + `src/MercuriusAPI/Data`          │
│ PostgreSQL via `MercuriusDBContext`, file storage for images │
└─────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| Program startup | Builds configuration, registers EF Core, auth, Swagger, CORS, middleware, Imageflow, endpoint groups, migrations, and initial user seed. | `src/MercuriusAPI/Program.cs` |
| Minimal API endpoints | Own HTTP route groups, authorization metadata, request binding, DTO response shaping, and service calls. | `src/MercuriusAPI/Endpoints/GameEndpoints.cs`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs` |
| Dependency configuration | Registers service interfaces, Scrutor decorators, token/login singletons, and tournament moderator implementations. | `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs` |
| Service layer | Coordinates EF Core queries, persistence, DTO creation, file IO, auth token generation, and calls into domain model behavior. | `src/MercuriusAPI/Services/GameServices/GameServices.cs`, `src/MercuriusAPI/Services/TeamServices/TeamService.cs` |
| Domain models | Encapsulate state transitions and invariants for games, matches, teams, invites, users, and participants. | `src/MercuriusAPI/Models/Game.cs`, `src/MercuriusAPI/Models/Match.cs`, `src/MercuriusAPI/Models/Team.cs` |
| Persistence | Defines DbSets and EF Core relationship mapping for participants, games, matches, roles, refresh tokens, sponsors, and placements. | `src/MercuriusAPI/Data/MercuriusDBContext.cs` |
| DTO layer | Defines API request and response contracts grouped by feature. | `src/MercuriusAPI/DTOs/GameDTOs/CreateGameDTO.cs`, `src/MercuriusAPI/DTOs/Auth/LoginRequest.cs` |
| Exception middleware | Converts domain/application exceptions into HTTP status responses. | `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs` |
| Tournament moderators | Generate bracket match graphs and placements for supported bracket types. | `src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs` |
| Static/image pipeline | Stores uploaded images as WebP and serves `/images` through Imageflow. | `src/MercuriusAPI/Services/FileServices/FileService.cs`, `src/MercuriusAPI/Program.cs` |

## Pattern Overview

**Overall:** Modular Minimal API with service-per-feature, EF Core persistence, DTO contracts, and domain-model methods for core state transitions.

**Key Characteristics:**
- Use endpoint extension classes named `*Endpoints` to map feature route groups from `Program.cs`.
- Keep HTTP handlers thin: bind route/body/form inputs, resolve services via dependency injection, call service methods, and return DTOs.
- Register application services behind interfaces in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Use EF Core `MercuriusDBContext` directly inside services instead of repository classes.
- Use Scrutor decorators for validation where a cross-cutting interface wrapper exists.
- Put tournament bracket algorithms behind `IMatchModerator` and select implementations with `IMatchModeratorFactory`.

## Layers

**Host/Composition Layer:**
- Purpose: Compose the web application, middleware pipeline, configuration, authentication, Swagger, Imageflow, and endpoints.
- Location: `src/MercuriusAPI/Program.cs`
- Contains: `WebApplicationBuilder`, service registration, JWT bearer setup, CORS policy, migration execution, static/image middleware, endpoint mapping.
- Depends on: `src/MercuriusAPI/Data/MercuriusDBContext.cs`, `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`, `src/MercuriusAPI/Endpoints/*.cs`.
- Used by: ASP.NET Core runtime and `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj` via project reference.

**API Endpoint Layer:**
- Purpose: Define HTTP surface area and authorization policy per route group.
- Location: `src/MercuriusAPI/Endpoints`
- Contains: Static extension classes such as `GameEndpoints`, `TeamEndpoints`, `PlayerEndpoints`, `MatchEndpoints`, `SponsorEndpoints`, `UserEndpoints`, and `AuthEndpoints`.
- Depends on: Feature service interfaces in `src/MercuriusAPI/Services`, DTOs in `src/MercuriusAPI/DTOs`, ASP.NET Core authorization attributes.
- Used by: `Program.cs` calls `app.MapGameEndpoints()`, `app.MapMatchEndpoints()`, `app.MapPlayerEndpoints()`, `app.MapTeamEndpoints()`, `app.MapSponsorEndpoints()`, `app.MapUserEndpoints()`, and `app.MapAuthEndpoints()`.

**Application Service Layer:**
- Purpose: Implement use cases, database queries, persistence, external-adjacent operations, and DTO conversion.
- Location: `src/MercuriusAPI/Services`
- Contains: Interface/implementation pairs such as `IGameService`/`GameService`, `ITeamService`/`TeamService`, `IPlayerService`/`PlayerService`, `IAuthService`/`AuthService`, and `IFileService`/`FileService`.
- Depends on: `MercuriusDBContext`, domain models, DTOs, `IConfiguration`, Imageflow, token/login helpers.
- Used by: Endpoint handlers and decorated validation services.

**Validation Decorator Layer:**
- Purpose: Validate inputs before delegating to service implementations for interfaces with decorator registration.
- Location: `src/MercuriusAPI/Services/Auth/AuthValidationService.cs`, `src/MercuriusAPI/Services/UserServices/UserValidationService.cs`, `src/MercuriusAPI/Services/FileServices/FileValidationService.cs`
- Contains: Classes wrapping `IAuthService`, `IUserService`, and `IFileService`.
- Depends on: Service interfaces and custom exceptions.
- Used by: DI registrations in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

**Domain Model Layer:**
- Purpose: Represent persisted entities and enforce entity-level state transitions.
- Location: `src/MercuriusAPI/Models`
- Contains: `Game`, `Match`, `Participant`, `Player`, `Team`, `TeamInvite`, `Placement`, `Sponsor`, enums, and auth models.
- Depends on: Custom exceptions for invariant failures.
- Used by: EF Core context, services, DTO constructors, tournament moderators.

**Persistence Layer:**
- Purpose: Map models to PostgreSQL tables and relationships through EF Core.
- Location: `src/MercuriusAPI/Data/MercuriusDBContext.cs`, `src/MercuriusAPI/Migrations`
- Contains: `DbSet<T>` declarations and `OnModelCreating` relationship configuration.
- Depends on: `src/MercuriusAPI/Models`, `Microsoft.EntityFrameworkCore`.
- Used by: Services through constructor-injected `MercuriusDBContext`; startup migration code in `Program.cs`.

**Infrastructure/Integration Layer:**
- Purpose: Provide supporting concerns: JWT tokens, login lockout state, image processing, secured Swagger, enum schema filters, static file hosting.
- Location: `src/MercuriusAPI/Extensions`, `src/MercuriusAPI/Services/Auth`, `src/MercuriusAPI/Services/FileServices`, `src/MercuriusAPI/staticfiles`
- Contains: `JWTBuilder`, `TokenService`, `LoginAttemptService`, `FileService`, `SecurityTrimming`, Swagger configuration.
- Depends on: ASP.NET Core, Imageflow, JWT libraries, configuration values.
- Used by: Startup and feature services.

## Data Flow

### Primary Request Path

1. ASP.NET Core starts in `Program.Main`, loads `appsettings.json`, environment variables with the `Mercurius.LAN.API_` prefix, registers `MercuriusDBContext`, services, JWT, CORS, Swagger, and middleware (`src/MercuriusAPI/Program.cs:17`).
2. Startup applies pending EF Core migrations and seeds the initial user through `IUserService.SeedInitialUserAsync` (`src/MercuriusAPI/Program.cs:80`).
3. Route groups are mapped from endpoint extension classes, for example `app.MapGameEndpoints()` (`src/MercuriusAPI/Program.cs:115`).
4. A request hits a Minimal API handler, such as `POST lan/games/` binding `[FromForm] CreateGameDTO` and resolving `IGameService` (`src/MercuriusAPI/Endpoints/GameEndpoints.cs:30`).
5. The service performs validation/business workflow and calls domain methods, such as `GameService.CreateGameAsync` creating `Game`, saving an image through `IFileService`, adding it to `MercuriusDBContext.Games`, and calling `SaveChangesAsync` (`src/MercuriusAPI/Services/GameServices/GameServices.cs:24`).
6. EF Core persists data through `MercuriusDBContext` DbSets and mappings (`src/MercuriusAPI/Data/MercuriusDBContext.cs:12`).
7. The service returns API DTOs, such as `GetGameDTO`, to the endpoint response (`src/MercuriusAPI/Services/GameServices/GameServices.cs:40`).
8. Exceptions thrown from services or models are translated by `ExceptionHandlingMiddleware` into JSON responses and status codes (`src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs:13`).

### Tournament Start Flow

1. `POST lan/games/{id}/start` calls `IGameService.StartGameAsync` (`src/MercuriusAPI/Endpoints/GameEndpoints.cs:57`).
2. `GameService.StartGameAsync` loads the game with participants, matches, and placements, then calls `game.Start()` to enforce scheduled status and participant count (`src/MercuriusAPI/Services/GameServices/GameServices.cs:91`, `src/MercuriusAPI/Models/Game.cs:62`).
3. `GameService` asks `IMatchModeratorFactory` for an implementation matching `game.BracketType` (`src/MercuriusAPI/Services/GameServices/GameServices.cs:96`).
4. `MatchModeratorFactory` returns `SingleEliminationMatchModerator`, `DoubleEliminationMatchModerator`, or `RoundRobinMatchModerator`; unsupported bracket types throw `NotSupportedException` (`src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs:13`).
5. The moderator generates linked `Match` entities and BYE assignments, then `GameService` attaches them to `game.Matches` and persists the graph (`src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs:69`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:97`).

### Authentication Flow

1. `POST auth/login` allows anonymous access and calls `IAuthService.LoginAsync` (`src/MercuriusAPI/Endpoints/AuthEndpoints.cs:21`).
2. `AuthValidationService` validates request shape and username format before delegating (`src/MercuriusAPI/Services/Auth/AuthValidationService.cs:32`).
3. `AuthService` normalizes the username, checks lockout state with `ILoginAttemptService`, verifies the password hash, generates JWT and refresh tokens through `ITokenService`, removes expired refresh tokens, and persists changes (`src/MercuriusAPI/Services/Auth/AuthService.cs:38`).
4. JWT bearer middleware validates future authenticated requests using the `Jwt` configuration section (`src/MercuriusAPI/Program.cs:34`).

### Image Upload and Serving Flow

1. `GameEndpoints` and `SponsorEndpoints` bind form DTOs with uploaded images (`src/MercuriusAPI/Endpoints/GameEndpoints.cs:30`, `src/MercuriusAPI/Endpoints/SponsorEndpoints.cs:28`).
2. `GameService` or `SponsorService` calls `IFileService.SaveImageAsync` (`src/MercuriusAPI/Services/GameServices/GameServices.cs:34`, `src/MercuriusAPI/Services/SponsorServices/SponsorService.cs:40`).
3. `FileValidationService` validates null, empty, and max-size rules from `FileStorage:MaxFileSizeInMB` (`src/MercuriusAPI/Services/FileServices/FileValidationService.cs:20`).
4. `FileService` creates the configured file storage directory, converts uploads to WebP with Imageflow, and returns a relative `images/...` URL (`src/MercuriusAPI/Services/FileServices/FileService.cs:13`).
5. `Program.cs` maps `/images` to the configured `FileStorage:Location` through Imageflow middleware (`src/MercuriusAPI/Program.cs:98`).

**State Management:**
- Request state is per HTTP request and resolved through ASP.NET Core DI.
- Database state lives in PostgreSQL through EF Core `MercuriusDBContext`.
- Login lockout state lives in singleton `LoginAttemptService`, registered in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Uploaded image files live outside the database at `FileStorage:Location`, served under `/images`.
- Domain state transitions happen on model methods such as `Game.Start`, `Game.Complete`, `Game.Reset`, `Match.SetScoresAndWinner`, and `Team.InvitePlayer`.

## Key Abstractions

**Endpoint Mapping Extensions:**
- Purpose: Keep route registration modular by feature.
- Examples: `src/MercuriusAPI/Endpoints/GameEndpoints.cs`, `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`
- Pattern: Static class with `Map*Endpoints(this WebApplication app)` returning `RouteGroupBuilder`.

**Service Interfaces:**
- Purpose: Define use-case contracts for DI and tests.
- Examples: `src/MercuriusAPI/Services/GameServices/IGameService.cs`, `src/MercuriusAPI/Services/TeamServices/ITeamService.cs`, `src/MercuriusAPI/Services/Auth/IAuthService.cs`
- Pattern: Interface beside implementation in the same feature folder.

**Validation Decorators:**
- Purpose: Wrap service interfaces to apply validation before business logic.
- Examples: `src/MercuriusAPI/Services/Auth/AuthValidationService.cs`, `src/MercuriusAPI/Services/FileServices/FileValidationService.cs`, `src/MercuriusAPI/Services/UserServices/UserValidationService.cs`
- Pattern: Scrutor `services.Decorate<TInterface, TDecorator>()` in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

**Tournament Moderator Strategy:**
- Purpose: Generate matches and placements differently for each bracket type.
- Examples: `src/MercuriusAPI/Services/MatchServices/IMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs`
- Pattern: Strategy-like interface selected by factory from `BracketType`.

**Entity DTO Constructors:**
- Purpose: Shape API output from domain models.
- Examples: `src/MercuriusAPI/DTOs/GameDTOs/GetGameDTO.cs`, `src/MercuriusAPI/DTOs/TeamDTOs/GetTeamDTO.cs`, `src/MercuriusAPI/DTOs/MatchDTOs/GetMatchDTO.cs`
- Pattern: Response DTOs accept model instances and project selected fields.

**EF Core DbContext:**
- Purpose: Unit-of-work and object graph mapping.
- Examples: `src/MercuriusAPI/Data/MercuriusDBContext.cs`
- Pattern: Services inject `MercuriusDBContext` and call `SaveChangesAsync` after mutating aggregates.

## Entry Points

**Application Host:**
- Location: `src/MercuriusAPI/Program.cs`
- Triggers: `dotnet run`, published container entrypoint, ASP.NET Core runtime.
- Responsibilities: Configuration, DI, middleware, migrations, seeding, and endpoint mapping.

**Game API:**
- Location: `src/MercuriusAPI/Endpoints/GameEndpoints.cs`
- Triggers: HTTP requests under `lan/games`.
- Responsibilities: CRUD games, manage participants, start/reset/complete/cancel games.

**Match API:**
- Location: `src/MercuriusAPI/Endpoints/MatchEndpoints.cs`
- Triggers: HTTP requests under `lan/matches`.
- Responsibilities: Read a match and update scores/winners.

**Player API:**
- Location: `src/MercuriusAPI/Endpoints/PlayerEndpoints.cs`
- Triggers: HTTP requests under `lan/players`.
- Responsibilities: CRUD players.

**Team API:**
- Location: `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`
- Triggers: HTTP requests under `lan/teams`.
- Responsibilities: CRUD teams, manage team players and invites.

**Sponsor API:**
- Location: `src/MercuriusAPI/Endpoints/SponsorEndpoints.cs`
- Triggers: HTTP requests under `lan/sponsors`.
- Responsibilities: CRUD sponsors and sponsor logo uploads.

**User API:**
- Location: `src/MercuriusAPI/Endpoints/UserEndpoints.cs`
- Triggers: HTTP requests under `users`.
- Responsibilities: Admin user listing/deletion/role management and authenticated password changes.

**Auth API:**
- Location: `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`
- Triggers: HTTP requests under `auth`.
- Responsibilities: Register, login, refresh tokens, and revoke refresh tokens.

**Container Entrypoint:**
- Location: `Dockerfile`
- Triggers: Container runtime.
- Responsibilities: Run the published ASP.NET application through `dotnet MercuriusAPI.dll`.

## Architectural Constraints

- **Threading:** ASP.NET Core request handling is asynchronous; services use `async` EF Core calls for most database operations. Singleton services such as `LoginAttemptService` and `TokenService` must remain safe for concurrent requests.
- **Global state:** `LoginAttemptService` is registered as a singleton with in-memory lockout tracking in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`; this state is process-local.
- **Persistence:** Services depend directly on `MercuriusDBContext`; add new persistence behavior in services and `src/MercuriusAPI/Data/MercuriusDBContext.cs`, not through a repository layer.
- **Migrations:** Startup always calls `dbContext.Database.Migrate()` in `src/MercuriusAPI/Program.cs`; schema changes require EF Core migrations in `src/MercuriusAPI/Migrations`.
- **Configuration:** Runtime settings come from `src/MercuriusAPI/appsettings.json` plus environment variables prefixed with `Mercurius.LAN.API_`.
- **Authorization:** Most route groups require `Roles = "admin"` by default, with selected `.AllowAnonymous()` calls for public reads/auth.
- **Image storage:** Image URLs are relative paths under `images/`; physical storage is configured by `FileStorage:Location`.
- **Circular imports:** No explicit circular dependency chain is defined by project files; C# project structure is a single API assembly, so namespace cycles are possible but not separated by assembly boundaries.

## Anti-Patterns

### DbContext in Endpoint Handlers

**What happens:** Endpoint handlers call service interfaces, and services own EF Core queries and persistence.
**Why it's wrong:** Injecting `MercuriusDBContext` into endpoints would bypass the established service layer and duplicate validation/business workflow.
**Do this instead:** Add methods to the relevant service interface and implementation, then call the service from an endpoint, following `src/MercuriusAPI/Endpoints/GameEndpoints.cs` and `src/MercuriusAPI/Services/GameServices/GameServices.cs`.

### New Feature Without DI Registration

**What happens:** Services are resolved from interfaces registered in `AddServiceDependencies`.
**Why it's wrong:** Endpoint handlers depend on DI-resolved interfaces; missing registrations produce runtime failures.
**Do this instead:** Add new `I*Service`/`*Service` pairs under `src/MercuriusAPI/Services/<Feature>Services` and register them in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

### Validation Inside Every Endpoint

**What happens:** Some validation lives in decorators such as `AuthValidationService`, `UserValidationService`, and `FileValidationService`.
**Why it's wrong:** Repeating interface-level validation in endpoint lambdas scatters behavior and makes it inconsistent across routes.
**Do this instead:** Put reusable validation in a decorator and register it with Scrutor, following `src/MercuriusAPI/Services/Auth/AuthValidationService.cs` and `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

### Unsupported Bracket Type Fallthrough

**What happens:** `MatchModeratorFactory` explicitly switches on `BracketType`.
**Why it's wrong:** Adding a new enum value without a moderator registration leaves runtime paths unsupported.
**Do this instead:** Implement `IMatchModerator` under `src/MercuriusAPI/Services/MatchServices/BracketTypes`, register it in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`, and add the factory branch in `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs`.

## Error Handling

**Strategy:** Application code throws custom/domain exceptions; `ExceptionHandlingMiddleware` converts known exceptions to JSON error responses.

**Patterns:**
- Throw `NotFoundException` for missing entities in services, for example `src/MercuriusAPI/Services/PlayerServices/PlayerService.cs`.
- Throw `ValidationException` for invalid state transitions or invalid input in models/services, for example `src/MercuriusAPI/Models/Game.cs`.
- Throw `InvalidCredentialsException` and `LockoutException` for auth failures in `src/MercuriusAPI/Services/Auth/AuthService.cs`.
- Middleware maps `NotFoundException` to 404, `ValidationException` to 400, `InvalidCredentialsException` to 401, `LockoutException` to 423, and `UnauthorizedAccessException` to 401 in `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`.

## Cross-Cutting Concerns

**Logging:** Uses ASP.NET Core built-in logging configuration from `src/MercuriusAPI/appsettings.json`; no custom logger abstraction is present.
**Validation:** Mix of endpoint binding, service decorators, service-level checks, and domain model invariants. Use decorators for reusable interface-wide validation and model methods for state rules.
**Authentication:** JWT bearer auth is configured in `src/MercuriusAPI/Program.cs`; tokens are generated by `src/MercuriusAPI/Services/Auth/Token/TokenService.cs`; route groups apply role-based authorization.
**Swagger:** Swagger and secured UI customization live in `src/MercuriusAPI/Extensions` and `src/MercuriusAPI/staticfiles/swagger-custom.js`.
**File handling:** Image upload validation and conversion are behind `IFileService`; serving uses Imageflow middleware in `src/MercuriusAPI/Program.cs`.
**Database schema:** EF Core migrations are committed under `src/MercuriusAPI/Migrations`; `MercuriusDBContext` controls model mapping.

---

*Architecture analysis: 2026-04-29*
