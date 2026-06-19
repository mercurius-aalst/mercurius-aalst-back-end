# Modular Monolith Migration Plan

This document describes a phased migration plan for evolving the Mercurius backend from its current intertwined ASP.NET Core API into a stricter modular monolith using separate class libraries, internal implementation types, public contracts, module registration extensions, event-driven synchronization, and more REST-oriented resource routes.

The plan assumes the API must remain fully working at the end of every phase. Each phase should leave the application buildable, testable, runnable, and deployable.

## Target architecture

The intended end state is a single deployable ASP.NET Core API host backed by separate module class libraries.

```text
Mercurius.Api

Mercurius.SharedKernel
Mercurius.Platform

Mercurius.Modules.Identity
Mercurius.Modules.Identity.Contracts

Mercurius.Modules.Teams
Mercurius.Modules.Teams.Contracts

Mercurius.Modules.Competition
Mercurius.Modules.Competition.Contracts

Mercurius.Modules.Sponsorship
Mercurius.Modules.Sponsorship.Contracts

Mercurius.Modules.Discovery
Mercurius.Modules.Discovery.Contracts

Mercurius.Modules.Media
Mercurius.Modules.Media.Contracts
```

The API host should eventually be responsible only for composition:

```csharp
builder.Services.AddMercuriusPlatform(builder.Configuration);

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddTeamsModule(builder.Configuration);
builder.Services.AddCompetitionModule(builder.Configuration);
builder.Services.AddSponsorshipModule(builder.Configuration);
builder.Services.AddDiscoveryModule(builder.Configuration);
builder.Services.AddMediaModule(builder.Configuration);

app.MapIdentityModule();
app.MapTeamsModule();
app.MapCompetitionModule();
app.MapSponsorshipModule();
app.MapDiscoveryModule();
app.MapMediaModule();
```

The API host should not know about module internals such as services, EF entities, repositories, validators, DbContexts, or use-case handlers.

## Core architectural rules

1. A module owns its own business facts.
2. A module implementation should be `internal` by default.
3. Other modules may depend on contract assemblies, not implementation assemblies.
4. Public contracts may expose IDs, commands, queries, DTOs, snapshots, and integration events.
5. Public contracts must not expose EF entities, repositories, `DbContext`, `DbSet`, `IQueryable`, or internal services.
6. Cross-module references should use IDs, not object graphs.
7. Historical data should be stored as snapshots and should not be force-synchronized.
8. Read/search data should be stored as projections and synchronized through events.
9. Cached decision data should be versioned, idempotent, and reconciliable.
10. Direct module queries are allowed only when immediate consistency is needed.
11. Event handlers must be idempotent.
12. Every phase must end with a fully working API and passing validations.

## Suggested module ownership

```text
Identity
- User identity/profile
- Auth0 user binding
- username/email uniqueness
- user deletion/anonymization state

Teams
- Team
- Team membership
- Team invites
- captain transfer
- team logo reference

Competition
- Game
- TournamentRegistration
- TournamentRegistrationRosterMember
- Match
- Placement
- game lifecycle
- bracket/moderator behavior

Sponsorship
- Sponsor
- sponsor tier/context
- sponsor placement decisions

Discovery
- Search projections
- public searchable documents

Media
- physical file/image storage
- upload validation
- URL generation

Platform
- auth wiring
- Swagger/OpenAPI
- exception handling
- rate limiting
- CORS
- SignalR/realtime plumbing
- outbox/inbox infrastructure
- migrations/startup plumbing

SharedKernel
- stable primitives
- typed IDs if shared globally
- result/error primitives
- clock abstractions
- value objects that are truly generic
```

## Definition of done for every phase

Each phase should end with:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Where possible, also verify:

```bash
dotnet ef migrations list
dotnet ef database update --connection "<test connection>"
```

The API should also:

```text
- start locally
- generate OpenAPI successfully
- pass route/security tests
- pass architecture tests once introduced
- preserve existing behavior unless the phase explicitly introduces versioned route changes
```

After eventing is introduced, every phase should additionally verify:

```text
- outbox dispatcher tests pass
- inbox/idempotency tests pass
- stale-version events cannot overwrite newer projection data
```

## Phase 0 — Baseline safety net

### Goal

Create a reliable baseline before moving code. This phase should change very little production behavior.

### Steps

#### 0.1 Standardize validation commands

Document the exact commands that must pass after every phase:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Add database validation if CI has PostgreSQL available.

#### 0.2 Add architecture decision notes

Create a short architecture document explaining:

```text
- module ownership rules
- internal implementation rule
- contract assembly rule
- no EF entities across module boundaries
- one fact has one owner
- references vs snapshots vs projections vs caches
- direct query vs integration event usage
```

#### 0.3 Freeze existing endpoint behavior in tests

Keep useful existing route/security tests, but add broader tests that are less coupled to current implementation details:

```text
- anonymous users can access public read endpoints
- authenticated users are required for mutations
- admin-only endpoints require admin role
- removed admin routes remain unavailable
- OpenAPI document generation succeeds
```

### Phase exit criteria

```text
- Build passes.
- Existing tests pass.
- New high-level route/security tests pass.
- No production behavior changed yet.
```

## Phase 1 — Introduce solution/project structure without moving behavior

### Goal

Create the class library structure and references while keeping current runtime behavior intact.

### Steps

#### 1.1 Create new projects

Create:

```text
src/Mercurius.SharedKernel
src/Mercurius.Platform

src/Modules/Identity/Mercurius.Modules.Identity
src/Modules/Identity/Mercurius.Modules.Identity.Contracts

src/Modules/Teams/Mercurius.Modules.Teams
src/Modules/Teams/Mercurius.Modules.Teams.Contracts

src/Modules/Competition/Mercurius.Modules.Competition
src/Modules/Competition/Mercurius.Modules.Competition.Contracts

src/Modules/Sponsorship/Mercurius.Modules.Sponsorship
src/Modules/Sponsorship/Mercurius.Modules.Sponsorship.Contracts

src/Modules/Discovery/Mercurius.Modules.Discovery
src/Modules/Discovery/Mercurius.Modules.Discovery.Contracts

src/Modules/Media/Mercurius.Modules.Media
src/Modules/Media/Mercurius.Modules.Media.Contracts
```

Keep the current API project as the host.

#### 1.2 Add references carefully

Initial reference direction:

```text
API host
  -> all module implementation projects
  -> Platform
  -> SharedKernel

Module implementation
  -> its own Contracts
  -> SharedKernel
  -> Platform only where required

Module contracts
  -> SharedKernel only
```

Allowed examples:

```text
Competition -> Teams.Contracts
Competition -> Identity.Contracts
Discovery -> Teams.Contracts
Discovery -> Competition.Contracts
```

Forbidden examples:

```text
Competition -> Teams implementation
Teams -> Identity.Domain
Discovery -> MercuriusDBContext directly
```

#### 1.3 Add empty module configuration extensions

Example:

```csharp
namespace Mercurius.Modules.Teams;

public static class TeamsModuleConfiguration
{
    public static IServiceCollection AddTeamsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTeamsModule(
        this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
```

#### 1.4 Add architecture tests

At minimum, enforce:

```text
- Contracts projects do not reference implementation projects.
- Module implementation projects do not reference other module implementation projects.
- API host may reference module implementations.
- Module public surface remains intentionally small.
```

### Phase exit criteria

```text
- Build passes.
- Existing API still runs through old registrations.
- Architecture tests pass.
- No endpoint behavior changed.
```

## Phase 2 — Extract Platform concerns from the API host

### Goal

Reduce host complexity before moving business modules.

### Steps

#### 2.1 Move cross-cutting setup into `Mercurius.Platform`

Create service registration extensions:

```csharp
builder.Services.AddMercuriusValidation();
builder.Services.AddMercuriusSwagger();
builder.Services.AddMercuriusAuth(configuration);
builder.Services.AddMercuriusProblemDetails();
builder.Services.AddMercuriusRateLimiting(configuration);
builder.Services.AddMercuriusCors(configuration);
builder.Services.AddMercuriusRealtime();
```

Create app pipeline extensions:

```csharp
app.UseMercuriusExceptionHandling();
app.UseMercuriusSwaggerUI();
app.UseMercuriusImages();
app.UseMercuriusSecurityPipeline();
```

#### 2.2 Keep behavior identical

Do not alter:

```text
- JWT validation semantics
- CORS origins
- rate limit rules
- Swagger/OpenAPI versioning
- Imageflow cache behavior
- exception response shape
```

#### 2.3 Move route constraint setup

Move shared route constraints, such as a non-GUID route constraint, into Platform.

#### 2.4 Keep startup migration behavior temporarily

If the app currently applies migrations at startup, keep that behavior unchanged, but hide the implementation behind:

```csharp
app.ApplyMercuriusMigrations();
```

Later, decide separately whether production should auto-migrate on startup.

### Phase exit criteria

```text
- Program.cs is thinner.
- Runtime behavior unchanged.
- Existing endpoint tests pass.
- Auth/security tests pass.
- OpenAPI generation still works.
```

## Phase 3 — Create contracts before moving implementations

### Goal

Define public module contracts so implementations can later become `internal`.

### Steps

#### 3.1 Define module ownership

Confirm and document which module owns each business fact. For example:

```text
Identity owns user profile truth.
Teams owns team membership truth.
Competition owns tournament/game/match truth.
Sponsorship owns sponsor truth.
Discovery owns search projection truth.
Media owns stored file truth.
```

#### 3.2 Define IDs and small DTOs in contract projects

Examples:

```csharp
public readonly record struct UserId(Guid Value);
public readonly record struct TeamId(Guid Value);
public readonly record struct GameId(Guid Value);
public readonly record struct SponsorId(Guid Value);
```

Keep these in contracts or SharedKernel, depending on how widely they are used.

#### 3.3 Replace domain-returning public interfaces

Avoid public interfaces returning domain/EF models such as `Team`, `Game`, or `User`.

Prefer contracts like:

```csharp
public interface ITeamsModule
{
    Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken ct);

    Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
        TeamId teamId,
        UserId requestedBy,
        CancellationToken ct);
}

public interface ICompetitionModule
{
    Task<GameSummary?> GetGameSummaryAsync(GameId gameId, CancellationToken ct);
    Task<bool> IsRegistrationOpenAsync(GameId gameId, CancellationToken ct);
}
```

#### 3.4 Keep old interfaces temporarily as adapters

Do not rewrite everything at once.

Use transitional adapters where needed:

```text
Old endpoint -> old service interface -> internal module implementation
```

Gradually invert this to:

```text
Endpoint -> module contract/use case -> internal implementation
```

### Phase exit criteria

```text
- Contract projects compile.
- Old API behavior still works.
- Existing services still registered.
- New contract interfaces exist but are not yet mandatory everywhere.
- Tests pass.
```

## Phase 4 — Move Teams into its module

### Goal

Extract one meaningful module first. Teams is a good first module because it has clear ownership and clear endpoints.

### Steps

#### 4.1 Move Teams files

Move into `Mercurius.Modules.Teams`:

```text
Domain
- Team
- TeamInvite
- invite/member/captain domain behavior

Application
- TeamService or use-case handlers
- validation/decorator behavior

Infrastructure
- EF mappings
- repositories if introduced
- SignalR event adapter only if still team-specific

Endpoints
- Team endpoints

Contracts
- TeamSummary
- PublicTeamProfile
- TeamInviteSummary
- TeamRegistrationEligibility
- Team events
```

#### 4.2 Make implementation classes internal

These should become `internal`:

```text
TeamService
Team entity
TeamInvite entity
Team validators
Team EF configurations
Team repositories
Team endpoint class
```

Public surface should be limited to:

```text
TeamsModuleConfiguration
ITeamsModule
Teams contract records
Team events
```

#### 4.3 Keep current routes initially

Do not redesign routes during physical extraction.

First make this work:

```csharp
app.MapTeamsModule();
```

with the same route patterns and security behavior.

#### 4.4 Update tests to reference module mapping

Change endpoint tests from old direct mapping methods to:

```csharp
app.MapTeamsModule();
```

#### 4.5 Move Teams registration out of the central DI method

Replace central registrations with:

```csharp
builder.Services.AddTeamsModule(configuration);
```

### Phase exit criteria

```text
- Teams module is a separate class library.
- API host maps Teams through AddTeamsModule/MapTeamsModule.
- Current Teams endpoint behavior still works.
- Team route/security tests pass.
- Other modules still use old code.
```

## Phase 5 — Introduce module eventing infrastructure

### Goal

Add the synchronization mechanism before extracting modules that need duplicated data.

### Steps

#### 5.1 Add internal event bus contracts

In `Mercurius.Platform`:

```csharp
public interface IIntegrationEvent
{
    Guid MessageId { get; }
    DateTime OccurredAtUtc { get; }
}

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IIntegrationEvent;
}

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
```

#### 5.2 Add an outbox table

Start with one platform outbox table:

```text
platform.outbox_messages
- id
- type
- payload
- occurred_at_utc
- processed_at_utc
- retry_count
- last_error
```

The dispatcher can initially be in-process because this is still one deployable application.

#### 5.3 Add inbox/idempotency tracking

Either shared:

```text
platform.inbox_messages
- module_name
- message_id
- processed_at_utc
```

or module-local inbox tables.

#### 5.4 Add versioned events

Teams should publish events such as:

```csharp
public sealed record TeamCreated(
    Guid MessageId,
    Guid TeamId,
    string Name,
    string NormalizedName,
    Guid CaptainUserId,
    long Version,
    DateTime OccurredAtUtc) : IIntegrationEvent;
```

Add similar events for:

```text
TeamRenamed
TeamDeleted
TeamMemberAdded
TeamMemberRemoved
CaptainTransferred
```

#### 5.5 Keep SignalR separate from module eventing

Realtime delivery should be a consumer of integration events or an adapter, not the general module integration mechanism.

### Phase exit criteria

```text
- Outbox/inbox tables exist.
- Event publishing can happen transactionally.
- At least one Teams event is saved and dispatched in tests.
- Existing API behavior unchanged.
- Tests pass.
```

## Phase 6 — Move Identity/Profile into its module

### Goal

Extract user/profile ownership and reduce direct user-domain coupling from Teams and Competition.

### Steps

#### 6.1 Move user-related code

Move into `Mercurius.Modules.Identity`:

```text
User entity
UserService
UserValidationService
Auth0 profile sync/management-facing code where appropriate
User DTOs/contracts
User endpoints
```

#### 6.2 Add Identity contracts

Example:

```csharp
public interface IIdentityModule
{
    Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken ct);

    Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(
        string auth0UserId,
        CancellationToken ct);

    Task<IReadOnlyList<UserProfileSummary>> GetUsersByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken ct);
}
```

#### 6.3 Replace direct user entity usage in Teams

Start replacing code-level usage of the `User` entity with:

```text
UserId
UserProfileSummary
TeamMemberSnapshot
```

It is acceptable if some database relationships still exist temporarily, but module code should stop relying on user internals.

#### 6.4 Publish user profile events

Identity publishes:

```text
UserProfileChanged
UsernameChanged
UserDeleted
UserAnonymized
```

Teams and Discovery consume these where needed.

### Phase exit criteria

```text
- Identity is a separate class library.
- User endpoints still work.
- Teams no longer needs direct access to the UserService implementation.
- Tests pass.
```

## Phase 7 — REST endpoint redesign, preferably as v2

### Goal

Move away from action-style routes into resource-based routes.

Because the API already supports versioning, prefer introducing clean routes as v2 while keeping v1 compatibility temporarily.

### Steps

#### 7.1 Add v2 route groups per module

Example:

```csharp
app.MapGroup("v{version:apiVersion}/lan/teams")
   .MapToApiVersion(new ApiVersion(2, 0));
```

#### 7.2 Teams REST route redesign

Recommended v2 routes:

```text
GET    /v2/lan/teams
POST   /v2/lan/teams
GET    /v2/lan/teams/{teamId}
DELETE /v2/lan/teams/{teamId}

GET    /v2/lan/users/me/team
GET    /v2/lan/users/me/team-invites

GET    /v2/lan/teams/{teamId}/members
PUT    /v2/lan/teams/{teamId}/members/{userId}
DELETE /v2/lan/teams/{teamId}/members/{userId}
DELETE /v2/lan/teams/{teamId}/members/me

GET    /v2/lan/teams/{teamId}/invites
POST   /v2/lan/teams/{teamId}/invites
DELETE /v2/lan/teams/{teamId}/invites/{inviteId}

PATCH  /v2/lan/team-invites/{inviteId}
```

For invite response, use a resource state update:

```json
{
  "status": "accepted"
}
```

For captain transfer, model captain as a subresource:

```text
PUT /v2/lan/teams/{teamId}/captain
```

with body:

```json
{
  "userId": "..."
}
```

For team logo:

```text
PUT    /v2/lan/teams/{teamId}/logo
DELETE /v2/lan/teams/{teamId}/logo
```

Use `PUT`, not `POST`, because the team has one logo resource.

#### 7.3 Games REST route redesign

Recommended v2 routes:

```text
GET    /v2/lan/games
POST   /v2/lan/games
GET    /v2/lan/games/{gameId}
PATCH  /v2/lan/games/{gameId}
DELETE /v2/lan/games/{gameId}

PUT    /v2/lan/games/{gameId}/sponsor-placements
PUT    /v2/lan/games/{gameId}/lifecycle-state
GET    /v2/lan/games/{gameId}/placements
```

Lifecycle state update body:

```json
{
  "state": "started"
}
```

or:

```json
{
  "state": "completed"
}
```

Avoid action routes like:

```text
POST /games/{id}/start
POST /games/{id}/complete
POST /games/{id}/cancel
```

#### 7.4 Tournament registration route redesign

Recommended v2 routes:

```text
GET    /v2/lan/games/{gameId}/registrations
POST   /v2/lan/games/{gameId}/registrations
GET    /v2/lan/games/{gameId}/registrations/{registrationId}
DELETE /v2/lan/games/{gameId}/registrations/{registrationId}

GET    /v2/lan/games/{gameId}/registrations/{registrationId}/roster-members
PUT    /v2/lan/games/{gameId}/registrations/{registrationId}/roster-members/{userId}
DELETE /v2/lan/games/{gameId}/registrations/{registrationId}/roster-members/{userId}
PATCH  /v2/lan/games/{gameId}/registrations/{registrationId}/roster-members/{userId}
```

Use `PATCH` for confirmation state:

```json
{
  "selectionStatus": "confirmed"
}
```

#### 7.5 Update endpoint tests around behavior, not exact old patterns

Tests should assert:

```text
- v2 route exists
- v2 route uses the correct HTTP method
- v2 route has correct authorization metadata
- old action routes either still exist as v1 compatibility or are explicitly absent
- OpenAPI v2 document has expected tags and paths
```

### Phase exit criteria

```text
- v2 REST routes exist.
- v1 either remains compatible or is intentionally removed.
- All route/security tests are updated and pass.
- OpenAPI generation passes.
- API behavior is equivalent or intentionally versioned.
```

## Phase 8 — Extract Competition module

### Goal

Move Games, TournamentRegistration, Matches, Placements, and bracket logic together.

Do not split these too early. They are part of one tournament/game lifecycle.

### Steps

#### 8.1 Move competition code

Move into `Mercurius.Modules.Competition`:

```text
GameService
IGameService equivalent/use cases
MatchService
TournamentRegistrationService
bracket moderators
Game endpoints
Match endpoints
TournamentRegistration endpoints
Game/Match/Placement/Registration models
DTOs/contracts
EF configurations
```

#### 8.2 Replace Teams/User entity dependencies

Competition should depend on:

```text
Teams.Contracts
Identity.Contracts
```

not Teams or Identity implementation projects.

During registration:

```csharp
var eligibility = await teamsModule.GetRegistrationEligibilityAsync(
    teamId,
    requestedBy,
    ct);
```

Then Competition stores:

```text
team_id
registered_by_user_id
roster snapshot
historical display names where appropriate
```

#### 8.3 Convert roster to snapshots

Target roster member shape:

```text
registration_id
game_id
user_id
team_id
username_at_registration
team_name_at_registration
selection_status
```

Historical display fields should not necessarily sync after registration.

#### 8.4 Publish competition events

Examples:

```text
GameCreated
GameUpdated
GameStarted
GameReset
GameCompleted
GameCanceled
TournamentRegistrationCreated
TournamentRegistrationCanceled
RosterMemberConfirmed
MatchCompleted
PlacementAssigned
```

Discovery and Realtime can consume these.

### Phase exit criteria

```text
- Competition is a separate class library.
- Game/match/registration endpoints map through MapCompetitionModule.
- Competition no longer references Teams/Identity implementation projects.
- Existing game/registration/match behavior passes tests.
- New v2 REST route tests pass.
```

## Phase 9 — Extract Sponsorship module

### Goal

Separate sponsor ownership from game/tournament lifecycle.

### Steps

#### 9.1 Move sponsor code

Move into `Mercurius.Modules.Sponsorship`:

```text
SponsorService
Sponsor endpoints
Sponsor entity
Sponsor DTOs/contracts
Sponsor EF configuration
```

#### 9.2 Decide ownership of game sponsor placement

Recommended ownership:

```text
Sponsorship owns:
- Sponsor
- Sponsor placement assignment

Competition owns:
- Game
- Game lifecycle
```

Sponsorship stores:

```text
game_id
sponsor_id
placement_context
tier
display_order
```

It references `game_id` but does not own the game.

#### 9.3 Publish placement events

Examples:

```text
SponsorCreated
SponsorUpdated
SponsorDeleted
GameSponsorPlacementChanged
```

Competition may consume if it needs display copies; Discovery consumes for search/display.

### Phase exit criteria

```text
- Sponsorship is a separate class library.
- Sponsor endpoints still work.
- Game sponsor placement behavior still works.
- Competition does not import Sponsorship implementation.
- Tests pass.
```

## Phase 10 — Extract Media module

### Goal

Move file/image concerns out of Teams and platform-adjacent code.

### Steps

#### 10.1 Move file service

Move into `Mercurius.Modules.Media`:

```text
FileService
FileValidationService
file/image DTOs
storage abstractions
upload validation
```

#### 10.2 Define Media contracts

Example:

```csharp
public interface IMediaModule
{
    Task<StoredImage> StoreTeamLogoAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
```

#### 10.3 Remove direct file handling from Teams

Teams should request:

```text
Media stores image
Teams stores returned LogoUrl/MediaId
Teams publishes TeamLogoChanged
```

#### 10.4 Keep Imageflow middleware in Platform/API host

Imageflow/static image serving remains host/platform infrastructure because it is HTTP middleware.

### Phase exit criteria

```text
- Media is a separate class library.
- Team logo upload/remove behavior still works.
- File validation tests pass.
- Image serving still works.
```

## Phase 11 — Extract Discovery/Search module

### Goal

Stop global search from depending on everyone’s live tables.

### Steps

#### 11.1 Move Search into Discovery

Move into `Mercurius.Modules.Discovery`:

```text
SearchService
Search endpoints
Search DTOs/contracts
```

#### 11.2 Introduce search projection table

Create:

```text
discovery.search_documents
- id
- entity_type
- entity_id
- title
- subtitle
- image_url
- route
- normalized_text
- source_version
- is_deleted
- updated_at_utc
```

#### 11.3 Feed Search via events

Discovery consumes:

```text
UserProfileChanged
TeamCreated
TeamRenamed
TeamDeleted
GameCreated
GameUpdated
GameCanceled
SponsorCreated
SponsorUpdated
SponsorDeleted
```

#### 11.4 Add rebuild job

Because projections can drift, add a manual/admin rebuild mechanism.

Preferred resource-oriented shape:

```text
POST /internal/discovery/search-index-rebuild-jobs
GET  /internal/discovery/search-index-rebuild-jobs/{jobId}
```

### Phase exit criteria

```text
- Discovery is a separate class library.
- Search endpoint returns equivalent results.
- Search is fed by projections.
- Projection rebuild test passes.
- Tests pass.
```

## Phase 12 — Split persistence boundaries

### Goal

Move from one all-knowing EF model to module-owned persistence.

### Steps

#### 12.1 Move EF configurations into modules

Move mappings into:

```text
Identity.Infrastructure.UserConfiguration
Teams.Infrastructure.TeamConfiguration
Teams.Infrastructure.TeamInviteConfiguration
Competition.Infrastructure.GameConfiguration
Competition.Infrastructure.MatchConfiguration
Competition.Infrastructure.TournamentRegistrationConfiguration
Sponsorship.Infrastructure.SponsorConfiguration
Discovery.Infrastructure.SearchDocumentConfiguration
```

#### 12.2 Decide between one DbContext and module DbContexts

Recommended two-step approach:

First:

```text
one physical MercuriusDbContext
module-owned EF configurations
module schemas/table prefixes
```

Later:

```text
IdentityDbContext
TeamsDbContext
CompetitionDbContext
SponsorshipDbContext
DiscoveryDbContext
```

Do not combine project extraction, route redesign, and many DbContexts in the same phase.

#### 12.3 Use database schemas

Recommended schemas:

```text
identity.users
teams.teams
teams.team_invites
teams.team_members
competition.games
competition.matches
competition.tournament_registrations
competition.roster_members
competition.placements
sponsorship.sponsors
sponsorship.game_sponsor_placements
discovery.search_documents
platform.outbox_messages
platform.inbox_messages
```

#### 12.4 Reduce cross-module EF navigation

Target rule:

```text
Inside a module:
- normal EF relationships and FKs are fine.

Across modules:
- store IDs.
- avoid EF navigation properties.
- avoid direct DbSet access.
```

### Phase exit criteria

```text
- EF mappings live with modules.
- DbContext still works.
- Migrations generate cleanly.
- Tests pass.
- No module imports another module's EF entities.
```

## Phase 13 — Tighten internals and public surface

### Goal

Make the modular boundaries real.

### Steps

#### 13.1 Make implementation types internal

For each module:

```text
internal:
- entities
- EF configurations
- repositories
- use-case handlers
- validators
- endpoints
- services

public:
- AddXModule
- MapXModule
- contracts
- event contracts
- module facade interfaces
```

#### 13.2 Add `InternalsVisibleTo` only for tests

Example:

```csharp
[assembly: InternalsVisibleTo("Mercurius.Modules.Teams.Tests")]
```

Do not use broad `InternalsVisibleTo` between modules.

#### 13.3 Add public API approval tests

Fail the build if a module accidentally exposes:

```text
Team
Game
User
DbContext
Repository
internal service
IQueryable
```

from a module implementation assembly.

#### 13.4 Remove old centralized DI

Delete or shrink the old central service registration method once all registrations have moved into modules.

### Phase exit criteria

```text
- Most implementation classes are internal.
- API host cannot directly inject old services.
- Public API approval tests pass.
- Architecture tests pass.
- Functional tests pass.
```

## Phase 14 — Test suite reshaping

### Goal

Move tests away from current implementation coupling and toward module behavior.

### Steps

#### 14.1 Split tests by module

Create:

```text
tests/Mercurius.Api.Tests
tests/Mercurius.Platform.Tests
tests/Mercurius.Modules.Identity.Tests
tests/Mercurius.Modules.Teams.Tests
tests/Mercurius.Modules.Competition.Tests
tests/Mercurius.Modules.Sponsorship.Tests
tests/Mercurius.Modules.Discovery.Tests
tests/Mercurius.Modules.Media.Tests
```

#### 14.2 Use layered test categories

```text
Unit tests:
- domain rules
- use-case handlers
- validation

Module integration tests:
- module DbContext
- module facade
- event handlers
- outbox/inbox behavior

API tests:
- routes
- auth metadata
- status codes
- request/response contracts
- OpenAPI generation

Architecture tests:
- forbidden references
- public surface
- no EF leakage
```

#### 14.3 Rewrite route tests around intended API

Tests should assert:

```text
- resource routes exist
- action routes are absent or v1-only
- auth metadata is correct
- public endpoints are anonymous
- mutation endpoints are authenticated/admin as intended
```

#### 14.4 Add event synchronization tests

Examples:

```text
TeamRenamed updates Discovery search document.
UserProfileChanged updates Team member display projection.
GameUpdated updates Discovery search document.
SponsorUpdated updates Discovery search document.
Duplicate event is ignored.
Older version event is ignored.
Outbox retry does not duplicate side effects.
```

### Phase exit criteria

```text
- Tests reflect the new architecture.
- Old implementation-coupled tests are removed or rewritten.
- Module tests pass.
- API tests pass.
- Architecture tests pass.
```

## Phase 15 — Remove compatibility and clean up

### Goal

Finish the migration by deleting old paths, old services, and transitional adapters.

### Steps

#### 15.1 Remove old endpoint groups

Once clients/tests are on v2, remove action-style routes.

Examples:

```text
POST /teams/{id}/leave
POST /games/{id} with lifecycle action body
```

#### 15.2 Remove old service interfaces

Delete transitional interfaces once no endpoint uses them.

Especially remove old public service interfaces that expose domain/EF models.

#### 15.3 Remove old namespaces

Clean up or empty old host-level folders such as:

```text
Endpoints
Services
DTOs
Models
```

Only host-specific types should remain in the API project.

#### 15.4 Final dependency check

Verify:

```text
- no module references another module's implementation project
- no module exposes EF entities publicly
- no API endpoint injects another module's internal service directly
- all module implementation classes are internal where possible
- all route compatibility decisions are intentional
```

### Phase exit criteria

```text
- No obsolete routes remain unless intentionally versioned.
- Transitional adapters removed.
- Architecture tests pass.
- Full test suite passes.
- API runs with module registration only.
```

## Recommended phase order

```text
0. Baseline safety net
1. Solution/project skeleton
2. Platform extraction
3. Contracts first
4. Teams extraction
5. Eventing/outbox/inbox
6. Identity extraction
7. REST v2 routes
8. Competition extraction
9. Sponsorship extraction
10. Media extraction
11. Discovery/Search extraction
12. Persistence boundary tightening
13. Internal/public surface hardening
14. Test suite reshaping
15. Compatibility removal and cleanup
```

## Important sequencing rule

Avoid combining these three changes in one phase:

```text
- moving code into a new class library
- changing endpoint routes
- changing persistence ownership
```

Pick one main kind of change per phase. This keeps failures easy to locate and keeps the API working throughout the migration.

## Synchronization strategy for duplicated data

For every duplicated field, classify it first:

```text
Reference:
- Store only the ID.
- No synchronization needed.

Snapshot:
- Store historical data as it looked at that moment.
- Do not synchronize after the fact.

Projection:
- Store read/search/display data.
- Synchronize through integration events.

Cached decision data:
- Store only when a module needs local decisions.
- Synchronize through versioned events.
- Add idempotency and reconciliation.
```

Examples:

```text
team_id on a tournament registration
=> reference

username_at_registration on a roster member
=> snapshot

team name in search_documents
=> projection

team registration eligibility cached in Competition
=> cached decision data
```

Only projections and cached decision data should be kept in sync.

## Eventing rules

Integration events should:

```text
- be public contracts
- contain only stable contract data
- never expose EF/domain entities
- have a unique MessageId
- have OccurredAtUtc
- include a source Version for mutable aggregates
- be handled idempotently
```

Consumers should:

```text
- ignore duplicate messages
- ignore stale versions
- be retry-safe
- avoid publishing loops
- update only data they own
```

## REST route guidelines

Prefer resource-oriented routes:

```text
PUT /teams/{teamId}/logo
DELETE /teams/{teamId}/logo
PUT /teams/{teamId}/captain
PATCH /team-invites/{inviteId}
PUT /games/{gameId}/lifecycle-state
GET /games/{gameId}/placements
```

Avoid action-oriented routes:

```text
POST /teams/{teamId}/leave
POST /games/{gameId}/start
POST /games/{gameId}/complete
POST /games/{gameId}/cancel
POST /games/{gameId} with an action enum body
```

A route may contain a noun-like subresource such as `captain`, `logo`, `members`, `registrations`, `roster-members`, `placements`, or `lifecycle-state`. It should avoid verbs such as `start`, `complete`, `cancel`, `leave`, `accept`, `reject`, or `transfer`.
