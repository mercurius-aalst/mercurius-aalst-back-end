# Codebase Structure

**Analysis Date:** 2026-04-29

## Directory Layout

```text
mercurius-aalst-back-end/
├── .github/                         # GitHub automation/configuration
├── .planning/                       # GSD planning and codebase intelligence
│   └── codebase/                    # Generated mapper documents
├── src/
│   └── MercuriusAPI/                # ASP.NET Core Web API project
│       ├── Controllers/             # Legacy/unused MVC controller location
│       ├── Data/                    # EF Core DbContext
│       ├── DTOs/                    # API request/response DTOs by feature
│       ├── Endpoints/               # Minimal API route group extension classes
│       ├── Exceptions/              # Custom exception types and filters
│       ├── Extensions/              # DI, Swagger, JWT, OpenAPI, utility extensions
│       ├── Middleware/              # ASP.NET Core custom middleware
│       ├── Migrations/              # EF Core migration history
│       ├── Models/                  # Domain and persistence entities
│       ├── Properties/              # Launch and service dependency metadata
│       ├── Services/                # Application services grouped by feature
│       ├── staticfiles/             # Swagger UI static customization assets
│       ├── Program.cs               # Application startup/composition root
│       ├── Mercurius.LAN.API.csproj # API project file
│       └── appsettings*.json        # Runtime configuration files
├── tests/
│   └── MercuriusAPI.Tests/          # xUnit test project
├── Dockerfile                       # Multi-stage .NET container build
└── LAN.API.sln                      # Visual Studio solution
```

## Directory Purposes

**`src/MercuriusAPI`:**
- Purpose: Main ASP.NET Core API application.
- Contains: Startup, Minimal API endpoints, DTOs, EF Core data access, domain models, services, middleware, migrations, and static Swagger customizations.
- Key files: `src/MercuriusAPI/Program.cs`, `src/MercuriusAPI/Mercurius.LAN.API.csproj`, `src/MercuriusAPI/appsettings.json`.

**`src/MercuriusAPI/Endpoints`:**
- Purpose: HTTP route definitions using Minimal API extension methods.
- Contains: One static `*Endpoints.cs` file per API area.
- Key files: `src/MercuriusAPI/Endpoints/GameEndpoints.cs`, `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`, `src/MercuriusAPI/Endpoints/UserEndpoints.cs`.

**`src/MercuriusAPI/Services`:**
- Purpose: Application use cases and infrastructure-adjacent operations.
- Contains: Feature subdirectories with service interfaces and implementations.
- Key files: `src/MercuriusAPI/Services/GameServices/IGameService.cs`, `src/MercuriusAPI/Services/GameServices/GameServices.cs`, `src/MercuriusAPI/Services/Auth/AuthService.cs`, `src/MercuriusAPI/Services/FileServices/FileService.cs`.

**`src/MercuriusAPI/Services/MatchServices`:**
- Purpose: Match lookup/update logic plus tournament bracket generation strategies.
- Contains: `IMatchService`, `MatchService`, `IMatchModerator`, `MatchModeratorFactory`, `BracketTypes`, and `Helpers`.
- Key files: `src/MercuriusAPI/Services/MatchServices/MatchService.cs`, `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs`.

**`src/MercuriusAPI/DTOs`:**
- Purpose: API contract classes grouped by feature and operation.
- Contains: Request DTOs such as `Create*DTO`, `Update*DTO`, auth request models, and response DTOs such as `Get*DTO`.
- Key files: `src/MercuriusAPI/DTOs/GameDTOs/CreateGameDTO.cs`, `src/MercuriusAPI/DTOs/GameDTOs/GetGameDTO.cs`, `src/MercuriusAPI/DTOs/TeamDTOs/TeamInviteDTO.cs`, `src/MercuriusAPI/DTOs/Auth/AuthTokenResponse.cs`.

**`src/MercuriusAPI/Models`:**
- Purpose: Domain entities and enums persisted by EF Core.
- Contains: Game/match/team/player/sponsor entities, participant hierarchy, tournament enums, and auth entities under `Models/Auth`.
- Key files: `src/MercuriusAPI/Models/Game.cs`, `src/MercuriusAPI/Models/Match.cs`, `src/MercuriusAPI/Models/Team.cs`, `src/MercuriusAPI/Models/Auth/User.cs`.

**`src/MercuriusAPI/Data`:**
- Purpose: EF Core data model and relationship mapping.
- Contains: `MercuriusDBContext`.
- Key files: `src/MercuriusAPI/Data/MercuriusDBContext.cs`.

**`src/MercuriusAPI/Migrations`:**
- Purpose: EF Core schema migration history.
- Contains: Timestamped migration classes and `MercuriusDBContextModelSnapshot`.
- Key files: `src/MercuriusAPI/Migrations/MercuriusDBContextModelSnapshot.cs`, `src/MercuriusAPI/Migrations/20260311212820_Remove_AcademicSeason.cs`.

**`src/MercuriusAPI/Extensions`:**
- Purpose: Extension methods and support classes for DI, Swagger, JWT, security trimming, enum schema generation, match helpers, and username normalization.
- Contains: Static extension classes and OpenAPI/JWT helpers.
- Key files: `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`, `src/MercuriusAPI/Extensions/JWTBuilder.cs`, `src/MercuriusAPI/Extensions/MatchExtensions.cs`.

**`src/MercuriusAPI/Exceptions`:**
- Purpose: Domain/application exception types and an MVC action filter.
- Contains: `ValidationException`, `NotFoundException`, auth exceptions, and `ExceptionFilter`.
- Key files: `src/MercuriusAPI/Exceptions/ValidationException.cs`, `src/MercuriusAPI/Exceptions/NotFoundException.cs`, `src/MercuriusAPI/Exceptions/AuthExceptions.cs`.

**`src/MercuriusAPI/Middleware`:**
- Purpose: ASP.NET Core middleware classes.
- Contains: Global exception-to-response middleware.
- Key files: `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`.

**`src/MercuriusAPI/staticfiles`:**
- Purpose: Static assets copied to output for Swagger UI customization.
- Contains: JavaScript injected by `UseSecuredSwaggerUI`.
- Key files: `src/MercuriusAPI/staticfiles/swagger-custom.js`.

**`tests/MercuriusAPI.Tests`:**
- Purpose: xUnit test project referencing the API project.
- Contains: Unit tests for games, teams, players, matches, and AutoFixture customizations.
- Key files: `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`, `tests/MercuriusAPI.Tests/GameTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`.

## Key File Locations

**Entry Points:**
- `src/MercuriusAPI/Program.cs`: ASP.NET Core composition root and runtime entry point.
- `src/MercuriusAPI/Endpoints/GameEndpoints.cs`: Game route group under `lan/games`.
- `src/MercuriusAPI/Endpoints/MatchEndpoints.cs`: Match route group under `lan/matches`.
- `src/MercuriusAPI/Endpoints/PlayerEndpoints.cs`: Player route group under `lan/players`.
- `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`: Team route group under `lan/teams`.
- `src/MercuriusAPI/Endpoints/SponsorEndpoints.cs`: Sponsor route group under `lan/sponsors`.
- `src/MercuriusAPI/Endpoints/UserEndpoints.cs`: User administration route group under `users`.
- `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`: Authentication route group under `auth`.

**Configuration:**
- `LAN.API.sln`: Solution containing `Mercurius.LAN.API` and `Mercurius.LAN.API.Tests`.
- `src/MercuriusAPI/Mercurius.LAN.API.csproj`: API project dependencies and build settings.
- `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`: Test project dependencies and API project reference.
- `src/MercuriusAPI/appsettings.json`: Logging, connection string, file storage, team invite, and JWT settings.
- `src/MercuriusAPI/appsettings.Development.json`: Development-specific configuration overlay.
- `src/MercuriusAPI/Properties/launchSettings.json`: Local launch profiles and development URLs.
- `Dockerfile`: Container build/publish/runtime definition.

**Core Logic:**
- `src/MercuriusAPI/Services/GameServices/GameServices.cs`: Game CRUD, lifecycle, participants, match generation, image handling.
- `src/MercuriusAPI/Services/MatchServices/MatchService.cs`: Match retrieval and score/winner updates.
- `src/MercuriusAPI/Services/TeamServices/TeamService.cs`: Team CRUD, player membership, invites.
- `src/MercuriusAPI/Services/Auth/AuthService.cs`: Registration, login, refresh token, revoke token workflows.
- `src/MercuriusAPI/Services/SponsorServices/SponsorService.cs`: Sponsor CRUD and logo handling.
- `src/MercuriusAPI/Services/UserServices/UserService.cs`: User administration and initial user seeding.
- `src/MercuriusAPI/Models/Game.cs`: Game lifecycle state transitions.
- `src/MercuriusAPI/Models/Match.cs`: Match scoring, winner/loser assignment, next-match propagation.
- `src/MercuriusAPI/Models/Team.cs`: Team captain/player/invite domain rules.

**Persistence:**
- `src/MercuriusAPI/Data/MercuriusDBContext.cs`: EF Core DbSets and model relationships.
- `src/MercuriusAPI/Migrations`: Schema migration files.

**Testing:**
- `tests/MercuriusAPI.Tests/GameTests.cs`: Game model/service behavior tests.
- `tests/MercuriusAPI.Tests/MatchTests.cs`: Match/bracket behavior tests.
- `tests/MercuriusAPI.Tests/TeamTests.cs`: Team behavior tests.
- `tests/MercuriusAPI.Tests/PlayerTests.cs`: Player behavior tests.
- `tests/MercuriusAPI.Tests/Customizations/MatchParticipantCustomization.cs`: AutoFixture customization for match participants.

## Naming Conventions

**Files:**
- Endpoint files use plural feature names ending in `Endpoints.cs`: `src/MercuriusAPI/Endpoints/GameEndpoints.cs`, `src/MercuriusAPI/Endpoints/TeamEndpoints.cs`.
- Service interfaces use `I{Name}Service.cs`: `src/MercuriusAPI/Services/PlayerServices/IPlayerService.cs`.
- Service implementations use `{Name}Service.cs`, except the game implementation file is named `GameServices.cs` while the class is `GameService`.
- Validation decorators use `{Feature}ValidationService.cs`: `src/MercuriusAPI/Services/Auth/AuthValidationService.cs`.
- DTOs use operation prefixes: `CreateGameDTO.cs`, `UpdateGameDTO.cs`, `GetGameDTO.cs`, `LoginRequest.cs`, `AuthTokenResponse.cs`.
- EF migrations use timestamped names under `src/MercuriusAPI/Migrations`.

**Directories:**
- Feature service folders use plural names ending in `Services`: `GameServices`, `TeamServices`, `PlayerServices`, `SponsorServices`, `UserServices`.
- DTO feature folders use plural names ending in `DTOs`: `GameDTOs`, `TeamDTOs`, `PlayerDTOs`, `SponsorDTOs`.
- Auth has nested subfeatures under `src/MercuriusAPI/Services/Auth/Login` and `src/MercuriusAPI/Services/Auth/Token`.
- Tournament-specific implementations live under `src/MercuriusAPI/Services/MatchServices/BracketTypes`.

## Where to Add New Code

**New HTTP Feature:**
- Primary endpoint: Add `src/MercuriusAPI/Endpoints/<Feature>Endpoints.cs`.
- Service contract: Add `src/MercuriusAPI/Services/<Feature>Services/I<Feature>Service.cs`.
- Service implementation: Add `src/MercuriusAPI/Services/<Feature>Services/<Feature>Service.cs`.
- DTOs: Add request/response contracts under `src/MercuriusAPI/DTOs/<Feature>DTOs`.
- Entity model: Add persisted entity under `src/MercuriusAPI/Models` or `src/MercuriusAPI/Models/Auth` for auth-specific entities.
- Persistence mapping: Add `DbSet<T>` and relationship mapping in `src/MercuriusAPI/Data/MercuriusDBContext.cs`.
- DI registration: Register interfaces and implementations in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Startup mapping: Call `app.Map<Feature>Endpoints()` from `src/MercuriusAPI/Program.cs`.
- Tests: Add focused tests under `tests/MercuriusAPI.Tests`.

**New Endpoint in Existing Feature:**
- Primary code: Add route mapping to the existing file under `src/MercuriusAPI/Endpoints`.
- Business logic: Add/extend methods on the matching `I*Service` and `*Service` files under `src/MercuriusAPI/Services`.
- DTOs: Add any new request/response DTOs under the existing `src/MercuriusAPI/DTOs/<Feature>DTOs` folder.
- Tests: Add or extend tests in `tests/MercuriusAPI.Tests`.

**New Tournament Bracket Type:**
- Enum/model: Add the enum value in `src/MercuriusAPI/Models/BracketType.cs`.
- Moderator: Add `src/MercuriusAPI/Services/MatchServices/BracketTypes/<BracketName>MatchModerator.cs` implementing `IMatchModerator`.
- Factory: Add the switch branch in `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs`.
- DI registration: Add registration in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Tests: Add bracket behavior tests in `tests/MercuriusAPI.Tests/MatchTests.cs` or a new focused test file.

**New Validation Wrapper:**
- Implementation: Add `<Feature>ValidationService.cs` beside the interface it decorates under `src/MercuriusAPI/Services/<Feature>Services`.
- Registration: Add `services.Decorate<I<Feature>Service, <Feature>ValidationService>()` in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Exceptions: Use `ValidationException` from `src/MercuriusAPI/Exceptions/ValidationException.cs`.

**Utilities:**
- General extension methods: Add to `src/MercuriusAPI/Extensions`.
- Match/bracket helpers: Add to `src/MercuriusAPI/Services/MatchServices/Helpers`.
- Auth helpers: Add to `src/MercuriusAPI/Services/Auth`.
- Shared exception types: Add to `src/MercuriusAPI/Exceptions`.

**Database Changes:**
- Model changes: Update files under `src/MercuriusAPI/Models`.
- Mapping changes: Update `src/MercuriusAPI/Data/MercuriusDBContext.cs`.
- Schema migration: Add EF Core migration under `src/MercuriusAPI/Migrations`.

## Special Directories

**`src/MercuriusAPI/Migrations`:**
- Purpose: EF Core schema migration history for PostgreSQL.
- Generated: Yes
- Committed: Yes

**`src/MercuriusAPI/staticfiles`:**
- Purpose: Static Swagger UI JavaScript copied to output.
- Generated: No
- Committed: Yes

**`src/MercuriusAPI/Properties`:**
- Purpose: Visual Studio launch profiles and service dependency metadata.
- Generated: Partially
- Committed: Yes

**`src/MercuriusAPI/.config`:**
- Purpose: Local .NET tool/config directory when present.
- Generated: Yes
- Committed: Not detected from listed files

**`tests/MercuriusAPI.Tests/Customizations`:**
- Purpose: AutoFixture customization helpers for tests.
- Generated: No
- Committed: Yes

**`.planning/codebase`:**
- Purpose: GSD codebase mapping documents for future planning/execution agents.
- Generated: Yes
- Committed: Project-dependent

---

*Structure analysis: 2026-04-29*
