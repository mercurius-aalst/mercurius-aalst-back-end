# Mercurius Backend Modular Monolith Migration Plan — Codex Handoff

## Codex objective

Migrate `mercurius-aalst/mercurius-aalst-back-end` from the current intertwined ASP.NET Core API project into a stricter modular monolith while keeping the API runnable and the test suite green after every phase.

This is not a mechanical file move. Codex must actively improve the code while migrating it:

- improve names where they are unclear or misleading;
- remove accidental coupling;
- replace duplicated procedural logic with focused domain/application methods;
- introduce design patterns only when they reduce complexity or make module boundaries clearer;
- improve query performance and avoid unnecessary data loading;
- keep public behavior stable unless the phase explicitly includes a behavior change backed by OpenSpec;
- keep each phase small enough to review.

The migration must happen through a long-lived refactor integration branch and one PR per phase.

This document is intended to be handed to Codex once as the complete migration assignment. Codex should execute the phases sequentially, create the required phase branches and PRs itself, and stop at each PR review checkpoint. The human reviewer should only need to review and merge each phase PR, then let Codex continue from the updated integration branch.

---

## Codex execution model

Codex owns the operational flow of the migration:

```text
1. Create or verify the long-lived refactor/modular-monolith integration branch.
2. Create the phase branch for the current phase from the latest refactor/modular-monolith.
3. Implement only that phase.
4. Run the required validation.
5. Open a PR from the phase branch into refactor/modular-monolith.
6. Include a complete PR description with validation results, behavior impact, risks, and follow-up notes.
7. Stop and wait for human review/merge before starting the next phase.
8. After the phase PR is merged, create the next phase branch from the updated refactor/modular-monolith.
```

Do not expect a separate prompt for every phase. The phase sections below are the execution queue.

Codex may split a phase into smaller PRs only if the phase proves too large or risky. If it does, use suffixes such as:

```text
refactor/phase-7a-teams-contract-adapters
refactor/phase-7b-teams-endpoints
```

Each split PR must still target `refactor/modular-monolith`, preserve a green build, and explain why the split was necessary.

---

## Branch workflow

### Starting point

Start from:

```text
bugfix/endpoint-simplification-and-clarification
```

### Integration branch

Create one long-lived integration branch:

```text
refactor/modular-monolith
```

This branch is the base for every phase PR.

### Phase branches

For every phase, create a dedicated branch from the latest `refactor/modular-monolith`:

```text
refactor/phase-1
refactor/phase-2
refactor/phase-3
...
```

A descriptive suffix is allowed when useful:

```text
refactor/phase-4-platform-extraction
```

Every phase branch must be PR'ed into:

```text
refactor/modular-monolith
```

Do not PR phase branches directly into `main`, `develop`, or the original bugfix branch.

After a phase PR is merged into `refactor/modular-monolith`, create the next phase branch from the updated `refactor/modular-monolith`.

Codex must not start the next phase from an unmerged previous phase branch. The integration branch is the only base for phase work.

### Final PR

Only after all phases are complete and the full validation suite passes should `refactor/modular-monolith` be PR'ed into the normal target branch.

---

## Global execution rules for Codex

### Keep the API working after every phase

Every phase must end with:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

If PostgreSQL is available:

```bash
dotnet ef migrations list
dotnet ef database update --connection "<test connection>"
```

Also validate that:

```text
- the API starts locally;
- OpenAPI document generation succeeds;
- existing public routes keep their expected metadata;
- existing response JSON shapes remain stable unless explicitly changed;
- public route and DTO changes are intentional, OpenSpec-backed, and covered by route/security/OpenAPI/serialization tests.
```

### Do not combine risky change types

A phase must not combine more than one of these change categories:

```text
- physical project/file moves;
- endpoint route changes;
- persistence/schema ownership changes;
- public DTO/contract changes;
- module eventing/projection behavior changes.
```

If two categories seem necessary, split the phase.

### No v2 endpoint strategy

Do not introduce `/v2` endpoints.

There is no compatibility layer based on parallel v1/v2 route sets.

Route simplification must be done in-place only when:

```text
- the relevant OpenSpec change exists;
- endpoint cleanup is intentionally backend-led and resource-oriented, not blocked by backward-compatibility concerns;
- route/security/OpenAPI tests are updated;
- intentionally removed routes are tested as absent;
- Codex does not change route shape as part of physical module extraction.
```

The backend API should be actively cleaned up toward resource-based REST semantics. Do not preserve malformed/action-style endpoints solely for backward compatibility. Removed or renamed routes must be intentional, documented in OpenSpec, and tested as absent.

During module extraction, preserve current route patterns and authorization behavior.

### OpenSpec rule

OpenSpec is required for any functional or externally observable behavior change, including:

```text
- route shape changes;
- request/response DTO changes;
- validation behavior changes;
- authorization or privacy behavior changes;
- persistence behavior changes;
- event/projection behavior that changes visible results;
- search behavior changes;
- lifecycle or domain-rule changes.
```

OpenSpec is not required for purely internal refactoring that preserves behavior, but the PR must still explain why behavior is unchanged.

### Code quality refactor policy

Codex should clean up code inside the current phase scope. Do not perform large unrelated rewrites.

Allowed and expected improvements:

```text
- clearer names for methods, variables, DTOs, and domain concepts;
- smaller methods with explicit responsibilities;
- guard clauses for invalid state;
- extraction of cohesive private methods or application services;
- replacing duplicated logic with a shared internal method;
- reducing service methods that know too many unrelated concepts;
- moving domain rules closer to the domain model where appropriate;
- replacing magic strings/numbers with named constants or value objects;
- making read models explicit instead of returning EF entities;
- using cancellation tokens consistently;
- using async EF APIs consistently;
- using AsNoTracking for read-only EF queries;
- projecting only required fields instead of loading large graphs;
- avoiding N+1 queries;
- preserving query semantics while improving query shape;
- adding indexes only in persistence phases where migrations are expected.
```

Patterns are allowed only when they fit the problem:

```text
- Facade: public module API to hide implementation internals.
- Specification/Query Object: reusable query predicates or read models when repeated.
- Strategy: interchangeable policies such as validation/eligibility decisions.
- Domain Service: domain logic that does not belong to one aggregate.
- Application Service/Use Case Handler: orchestration of one command/query.
- Outbox/Inbox: reliable cross-module integration events.
- Adapter: temporary compatibility from old services/endpoints to new module internals.
```

Avoid pattern theater. Do not introduce abstractions that only wrap a single line or obscure simple code.
To validate code quality run the dotnet-clean-code-audit skill focussed on the made changes and implement the suggested changes made by this skill untill no more high and medium severity issues remain.

### Performance policy

For every touched query/service, Codex must check:

```text
- is this read-only? Use AsNoTracking.
- can this be projected instead of Include-heavy loading?
- can multiple queries be combined or batched?
- could this create an N+1 query?
- is ordering/pagination deterministic?
- are large blobs or navigation graphs being loaded unnecessarily?
- are cancellation tokens passed through?
- will this query still work efficiently after module boundaries are introduced?
```

Do not change behavior for performance unless tests and OpenSpec allow it. To validate performance, run dotnet-performance-audit skill focussed on the made changes. Implement the suggested changes made by this skill untill no more high and medium severity issues remain.

### Public contract policy

Do not expose these from module contracts or public module APIs:

```text
- EF entities;
- DbContext;
- repositories;
- IQueryable;
- navigation properties;
- internal application services;
- validators;
- EF configuration classes.
```

Contracts may expose:

```text
- command records;
- query records;
- DTO/read models;
- snapshots;
- integration events;
- typed IDs;
- facade interfaces.
```

### Internals policy

Implementation classes should become internal once they have moved into a module:

```text
- domain entities;
- EF configurations;
- repositories;
- use-case handlers;
- validators;
- endpoint classes;
- infrastructure services;
- application services.
```

Allowed public surface:

```text
- AddXModule(...);
- MapXModule(...);
- contracts;
- integration events;
- typed IDs;
- module facade interfaces when needed.
```

Use `InternalsVisibleTo` only for the matching test assembly. Never use it to let one module access another module's internals.

---

## Target architecture

The intended end state is:

```text
Mercurius.Api

Modules.Shared
Platform

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

The API host should eventually compose the application roughly like this:

```csharp
builder.Services.AddPlatform(builder.Configuration);

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

The API host should not directly know about:

```text
TeamService
GameService
MercuriusDBContext internals
EF entities
module repositories
domain entities
internal validators
internal use cases
```

It should only know public registration and endpoint mapping extensions plus public contracts.

---

## Module ownership

### Identity

Owns:

```text
- user identity/profile;
- Auth0 user binding;
- username/email uniqueness;
- user deletion/anonymization state;
- user profile contracts and events.
```

### Teams

Owns:

```text
- Team;
- Team membership;
- Team invites;
- captain transfer;
- team logo reference;
- membership rules not tied to a competition lifecycle.
```

### Competition

Owns:

```text
- Game;
- TournamentRegistration;
- TournamentRegistrationRosterMember;
- Match;
- Placement;
- bracket/game lifecycle;
- tournament registration and roster confirmation rules.
```

### Sponsorship

Owns:

```text
- Sponsor;
- sponsor tier/context;
- sponsor display metadata;
- sponsor placement assignment when not part of competition lifecycle decisions.
```

If sponsor placement affects competition lifecycle behavior, document the ownership decision before moving it.

### Discovery

Owns:

```text
- search projections;
- public searchable documents;
- search endpoint behavior;
- projection rebuild jobs.
```

### Media

Owns:

```text
- physical file/image storage;
- upload validation;
- storage key management;
- generated media references.
```

Image serving middleware remains host/platform infrastructure.

### Platform

Owns:

```text
- auth wiring;
- Swagger/OpenAPI;
- validation plumbing;
- rate limiting;
- exception handling;
- CORS;
- SignalR/realtime infrastructure;
- outbox/inbox infrastructure;
- migrations/startup plumbing;
- route constraints;
- host-level HTTP middleware.
```

---

## Dependency rules

Allowed:

```text
Competition -> Teams.Contracts
Competition -> Identity.Contracts
Discovery -> Teams.Contracts
Discovery -> Competition.Contracts
Discovery -> Sponsorship.Contracts
Discovery -> Identity.Contracts
API Host -> module implementation projects
Module implementation -> own Contracts
Module implementation -> Modules.Shared
Module implementation -> Platform only where required
Module contracts -> Modules.Shared only
```

Not allowed:

```text
Competition -> Teams implementation
Competition -> Teams.Domain.Team
Teams -> Identity implementation internals
Discovery -> MercuriusDBContext directly
Any module -> another module's DbContext
Any module -> another module's repository
Any module -> another module's EF entity
Any module contract -> EF entity
Any module contract -> IQueryable
```

---

## Data duplication rules

For every duplicated field, explicitly classify it:

```text
Reference
- Stores only another module's ID.
- No sync needed.

Snapshot
- Historical copy of data at a specific moment.
- Should not stay synchronized.

Projection
- Read/search/display model.
- Synchronized through integration events.

Cached decision data
- Local copy used to make decisions.
- Synchronized through versioned events and reconciled carefully.
```

One fact has one owner. Other modules may reference, snapshot, project, or cache it intentionally.

Do not try to keep every duplicate perfectly synchronized.

---

# Phase 1 — AGENTS.md Guardrails and Refactor Branch Setup

## Branch

```text
refactor/phase-1
```

Base:

```text
refactor/modular-monolith
```

PR target:

```text
refactor/modular-monolith
```

## Goal

Update repository guidance so Codex can safely perform the modular monolith migration without being constrained by stale file-specific metadata.

This phase changes guidance and baseline documentation only. It should not change production behavior.

## Required AGENTS.md rewrite

Rewrite `AGENTS.md` so it contains durable engineering rules and guardrails instead of specific project/file metadata that will become stale during migration.

The updated `AGENTS.md` should include guidance like:

```md
# Engineering Guidelines

## Core principles

- Keep behavior stable unless the task explicitly requests a functional change.
- Prefer small, reviewable changes with clear validation.
- Use OpenSpec for externally observable behavior changes.
- Preserve public API JSON shapes unless the spec says otherwise.
- Treat API route and DTO changes as intentional contract changes only when OpenSpec explicitly requires them.
- Do not combine route changes, persistence changes, and physical project moves in one PR.

## Code quality

- Improve naming when touching unclear code.
- Prefer cohesive methods and explicit domain/application concepts.
- Remove duplication inside the touched scope.
- Use design patterns only when they clarify responsibilities.
- Keep abstractions purposeful.
- Avoid leaking EF entities outside implementation boundaries.
- Avoid exposing IQueryable across module boundaries.

## Performance

- Use AsNoTracking for read-only EF queries.
- Prefer projections over Include-heavy loading when only read models are needed.
- Avoid N+1 queries.
- Pass cancellation tokens through async flows.
- Keep pagination and ordering deterministic.
- Do not load large graphs or files unless required.

## Modular monolith boundaries

- Modules own business capabilities.
- Module contracts may expose DTOs, commands, queries, events, snapshots, typed IDs, and facades.
- Module contracts must not expose EF entities, DbContext, repositories, IQueryable, or navigation properties.
- Implementation classes should be internal after extraction.
- Use InternalsVisibleTo only for the matching test project.

## Branching

- Perform modular migration on refactor/modular-monolith.
- Use one phase branch per phase.
- Each phase branch must PR into refactor/modular-monolith.
```

Do not keep lists that assume the current source layout will remain true throughout the migration.

## Required migration documentation

Add or update an architecture note that records:

```text
- module ownership rules;
- dependency rules;
- public versus internal rules;
- no EF entity leakage rule;
- one fact has one owner;
- reference versus snapshot versus projection versus cache;
- OpenSpec requirements;
- no v2 endpoint strategy;
- branch workflow;
- per-phase validation commands.
```

## Phase exit criteria

```text
- AGENTS.md is durable and no longer depends on soon-to-be-stale file paths.
- Branch workflow is documented.
- Modular guardrails are documented.
- No production behavior changed.
- dotnet restore passes.
- dotnet build passes.
- dotnet test passes.
- dotnet format --verify-no-changes passes.
```

---

# Phase 2 — Baseline Safety Net and Contract Freeze

## Branch

```text
refactor/phase-2
```

## Goal

Freeze current external behavior before moving code or cleaning routes.

This phase should add tests and observability around current behavior. Production behavior should not change.

## Tasks

### 2.1 Add route/security tests

Add or strengthen tests for:

```text
- anonymous users can access public read endpoints;
- authenticated users are required for normal mutations;
- admin-only endpoints require the admin role;
- intentionally unavailable routes stay unavailable;
- SignalR hub endpoint remains mapped;
- endpoint route names/tags/metadata remain stable where relevant.
```

### 2.2 Add OpenAPI generation test

Add a test that generates the OpenAPI document successfully.

If feasible, snapshot relevant path/method metadata without snapshotting noisy generated content.

### 2.3 Add DTO serialization tests

For public response DTOs, add serialization-shape tests for representative responses.

Focus first on DTOs around:

```text
- teams;
- team invites;
- games;
- tournament registrations;
- roster members;
- matches;
- sponsors;
- search results;
- user profiles.
```

### 2.4 Add privacy/visibility tests

Add black-box tests for behavior that can easily regress during refactoring:

```text
- deleted/anonymized users;
- public versus private user fields;
- team membership visibility;
- game visibility;
- search visibility;
- sponsor visibility.
```

### 2.5 Add query-behavior tests before performance cleanup

Where tests are currently too implementation-coupled, add behavior-first tests before rewriting the implementation.

## Code quality allowance

Only small test-support cleanup is allowed in this phase. Do not perform broad production refactoring yet.

## Phase exit criteria

```text
- Current external API behavior is covered by higher-level tests.
- OpenAPI generation succeeds in tests.
- DTO JSON shape is protected for key public contracts.
- No production behavior changed.
- Full validation passes.
```

---

# Phase 3 — Monolith Code Quality and Boundary Preparation

## Branch

```text
refactor/phase-3
```

## Goal

Prepare the current monolith for extraction by reducing obvious coupling and improving code quality without moving code into new projects yet.

This phase makes extraction safer by improving names, method boundaries, query shape, and service contracts inside the existing structure.

## Tasks

### 3.1 Remove public EF entity leaks from service interfaces

Find service interfaces that return domain/EF entities publicly, such as methods that return `Team`, `Game`, `User`, or other EF models.

Replace public-facing use with narrower read models or DTOs:

```text
TeamSummary
TeamRosterSnapshot
GameSummary
UserProfileSummary
TournamentConfiguration
RegistrationEligibility
```

Temporary internal methods may still return entities where required, but they should not become module contract boundaries.

### 3.2 Introduce internal read models

Create internal read models where endpoints currently depend on large entity graphs.

Read models should:

```text
- contain only fields needed by the caller;
- avoid navigation properties;
- be easy to move into contract assemblies later when appropriate;
- preserve current JSON output at the endpoint boundary.
```

### 3.3 Improve query performance in touched services

For touched queries:

```text
- add AsNoTracking for read-only flows;
- use projection instead of Include when possible;
- avoid loading members/games/users when only IDs or names are needed;
- keep ordering deterministic;
- pass cancellation tokens.
```

### 3.4 Extract repeated eligibility/guard logic

Move repeated decision logic into clearly named methods or small internal services.

Examples:

```text
CanUserLeaveTeam
CanCaptainRemoveMember
CanTeamRegisterForTournament
CanRosterMemberBeSelected
CanRosterMemberConfirm
```

These should be names of business decisions, not names of database queries.

### 3.5 Improve naming

Rename unclear local variables, private methods, and internal methods in the touched scope.

Do not rename public DTO properties or route parameters unless the phase has an OpenSpec-backed behavior change.

## Phase exit criteria

```text
- Public service interfaces are less dependent on EF entities.
- Touched queries are cleaner and more efficient.
- Business rules have clearer names.
- No route or JSON shape changed.
- Tests pass.
- OpenAPI generation still passes.
```

---

# Phase 4 — Extract Platform Concerns from the API Host

## Branch

```text
refactor/phase-4
```

## Goal

Reduce API host complexity before moving business modules.

This is a pure refactor. Do not alter runtime behavior.

## Tasks

### 4.1 Create Platform

Create:

```text
src/Platform
```

Move cross-cutting setup into focused extension methods.

Service registration examples:

```csharp
builder.Services.AddValidation();
builder.Services.AddVersionedSwagger(...);
builder.Services.AddAuth0JwtAuthentication(...);
builder.Services.AddApiProblemDetails<TExceptionHandler>();
builder.Services.AddFixedWindowRateLimiting(...);
builder.Services.AddWildcardSubdomainCors(...);
builder.Services.AddRealtime();
```

Pipeline examples:

```csharp
app.UseApiExceptionHandling();
app.UseVersionedSwaggerUI();
app.UseImageflowWithCaching(...);
app.UseSecurityPipeline();
```

### 4.2 Preserve behavior exactly

Do not alter:

```text
- JWT/Auth0 validation semantics;
- authorization policies;
- CORS origins;
- rate limit rules;
- Swagger/OpenAPI metadata;
- Imageflow/cache behavior;
- exception response shape;
- route constraints;
- SignalR mapping;
- startup migration behavior.
```

### 4.3 Hide migration startup behind a platform method

If the app currently applies pending migrations at startup, keep the same behavior but move it behind:

```csharp
app.ApplyMigrations<TDbContext>();
```

Do not decide in this phase whether production should auto-migrate.

### 4.4 Improve names and cohesion while moving

Use this phase to make extension methods cohesive and clearly named.

Do not introduce vague names like:

```text
AddInfrastructure
AddCommonServices
UseEverything
```

Prefer names that explain the platform concern.

## Phase exit criteria

```text
- Program.cs is thinner.
- Runtime behavior unchanged.
- Auth/security tests pass.
- Swagger/OpenAPI tests pass.
- Image serving behavior still works.
- SignalR mapping still works.
- Full validation passes.
```

---

# Phase 5 — Introduce Solution and Project Skeleton

## Branch

```text
refactor/phase-5
```

## Goal

Create the class library structure and project references while keeping current behavior running through the existing API project.

Do not move business code in this phase.

## Tasks

### 5.1 Create projects

Create:

```text
src/Modules.Shared

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

### 5.2 Add references carefully

Initial reference direction:

```text
Mercurius.Api
  -> all module implementation projects
  -> Platform
  -> Modules.Shared

Module implementation
  -> its own Contracts
  -> Modules.Shared
  -> Platform only where required

Module contracts
  -> Modules.Shared only
```

Avoid implementation-to-implementation module references.

### 5.3 Add empty module configuration extensions

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

At this phase they can be empty.

### 5.4 Add architecture tests

Use plain xUnit/reflection/project-file checks before adding dependencies.

Enforce:

```text
- Contracts projects do not reference implementation projects.
- Module implementation projects do not reference other module implementation projects.
- API host may reference module implementations.
- Module contracts do not expose EF types.
- Module contracts do not expose IQueryable.
```

Only add an architecture-test package if plain tests become unreasonable, and document why.

## Phase exit criteria

```text
- New projects exist.
- Existing API still runs through old registrations.
- Architecture tests pass.
- No endpoint behavior changed.
- Full validation passes.
```

---

# Phase 6 — Create Contracts Before Moving Implementations

## Branch

```text
refactor/phase-6
```

## Goal

Define module contracts so implementations can become internal later.

This phase should introduce contracts and adapters while preserving current behavior.

## Tasks

### 6.1 Define typed IDs

Examples:

```csharp
public readonly record struct UserId(Guid Value);
public readonly record struct TeamId(Guid Value);
public readonly record struct GameId(Guid Value);
public readonly record struct SponsorId(Guid Value);
public readonly record struct TournamentRegistrationId(Guid Value);
```

Typed IDs can live in module contracts or `Modules.Shared`. Prefer the location that minimizes cross-module coupling.

### 6.2 Define module facades

Examples:

```csharp
public interface ITeamsModule
{
    Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken ct);

    Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(
        TeamId teamId,
        CancellationToken ct);

    Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
        TeamId teamId,
        UserId requestedBy,
        GameId gameId,
        CancellationToken ct);

    Task<MembershipMutationGuard> CanMutateMembershipAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken ct);
}

public interface IIdentityModule
{
    Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken ct);

    Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(
        string auth0UserId,
        CancellationToken ct);

    Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken ct);
}

public interface ICompetitionModule
{
    Task<GameSummary?> GetGameSummaryAsync(GameId gameId, CancellationToken ct);

    Task<bool> IsRegistrationOpenAsync(GameId gameId, CancellationToken ct);
}
```

### 6.3 Define snapshots deliberately

Examples:

```text
TeamRosterSnapshot
TeamMemberSnapshot
UserProfileSummary
GameSummary
TournamentConfiguration
RegistrationEligibility
```

Snapshots must not contain EF navigation properties.

### 6.4 Keep old interfaces temporarily as adapters

Temporary shape:

```text
Old endpoint -> old service interface -> internal implementation/adapters
```

Target shape:

```text
Endpoint -> command/query/use case -> internal module implementation
```

Do not rewrite every endpoint at once.

### 6.5 Preserve JSON shape

Introducing contracts must not change public response JSON unless backed by OpenSpec.

## Phase exit criteria

```text
- Contract projects compile.
- Old API behavior still works.
- Existing services still registered.
- New contract interfaces exist.
- No public contract exposes EF entities, DbContext, repositories, IQueryable, or navigation properties.
- Tests pass.
```

---

# Phase 7 — Move Teams into Its Module

## Branch

```text
refactor/phase-7
```

## Goal

Extract Teams as the first complete business module.

Teams is the first extraction because it has clear ownership, visible endpoint behavior, and significant coupling that should be addressed before Competition moves.

## Tasks

### 7.1 Move Teams files

Move into `Mercurius.Modules.Teams`:

```text
Domain
- Team
- TeamInvite
- invite/member/captain domain behavior

Application
- Team service/use-case handlers
- eligibility/guard services
- validation/decorator behavior

Infrastructure
- EF mappings related to Teams
- repositories if introduced
- Team-specific realtime adapters only if still needed

Endpoints
- Team endpoints

Contracts
- TeamSummary
- PublicTeamProfile
- TeamInviteSummary
- TeamRosterSnapshot
- TeamMemberSnapshot
- TeamRegistrationEligibility
- MembershipMutationGuard
- Team integration events
```

### 7.2 Keep current routes

Do not redesign Teams routes in this phase.

Make this work with the same route patterns and security behavior:

```csharp
builder.Services.AddTeamsModule(builder.Configuration);
app.MapTeamsModule();
```

### 7.3 Make implementation classes internal

These should become internal once inside the module:

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
Team integration events
```

### 7.4 Move DI registrations

Remove Teams registrations from central DI and move them into:

```csharp
builder.Services.AddTeamsModule(configuration);
```

Preserve decorator behavior and lifetimes.

### 7.5 Rewrite for cleaner design inside the module

While moving, improve Teams internals:

```text
- give membership/captain/invite operations explicit method names;
- reduce endpoint orchestration logic;
- keep business decisions out of endpoint lambdas where possible;
- use read models for queries;
- use AsNoTracking for read-only reads;
- avoid loading full member graphs unless needed;
- pass cancellation tokens.
```

### 7.6 Update tests

Move or rewrite Teams tests so they target:

```text
- Teams domain behavior;
- Teams application/use-case behavior;
- Teams route/security behavior through MapTeamsModule;
- Teams facade behavior.
```

## Phase exit criteria

```text
- Teams module is a separate class library.
- API host maps Teams through AddTeamsModule/MapTeamsModule.
- Current Teams endpoint behavior still works.
- Team route/security tests pass.
- Other modules still use old code or contracts.
- No endpoint route redesign occurred.
- Full validation passes.
```

---

# Phase 8 — Split Realtime from Module Synchronization

## Branch

```text
refactor/phase-8
```

## Goal

Prevent SignalR from acting as the general module synchronization mechanism.

This phase separates realtime delivery from integration events before introducing outbox/inbox.

## Tasks

### 8.1 Introduce realtime abstraction

Create a platform-level realtime abstraction, for example:

```csharp
public interface IRealtimePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct);
}
```

Keep SignalR as an implementation detail behind Platform/Realtime.

### 8.2 Replace team-specific SignalR publisher coupling

Where services currently publish team or roster events directly through a team-specific SignalR publisher, route that through a clearer realtime abstraction or a module event adapter.

Do not yet introduce durable outbox behavior unless this phase is explicitly expanded.

### 8.3 Move hub authorization behind a module contract

SignalR hub logic should not query `MercuriusDBContext` directly for Teams membership once Teams has a contract/facade.

Use `ITeamsModule` or a dedicated Teams realtime authorization contract.

### 8.4 Preserve current realtime behavior

Do not change:

```text
- group names;
- client message names;
- authorization semantics;
- payload shapes;
- when notifications are sent.
```

## Phase exit criteria

```text
- SignalR remains functional.
- Realtime publishing is separated from durable integration events.
- Hub authorization no longer depends directly on Teams internals where avoidable.
- Current notification tests pass or are added.
- Full validation passes.
```

---

# Phase 9 — Introduce Module Eventing Infrastructure

## Branch

```text
refactor/phase-9
```

## Goal

Add reliable module synchronization infrastructure before extracting modules that need duplicated data.

## Tasks

### 9.1 Add event bus contracts in Platform

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

### 9.2 Add outbox table

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

The dispatcher can be in-process because this remains one deployable application.

### 9.3 Add inbox/idempotency

Either shared:

```text
platform.inbox_messages
- module_name
- message_id
- processed_at_utc
```

or module-local inbox tables.

Choose one and document why.

### 9.4 Add versioned events

Teams should publish versioned events such as:

```csharp
public sealed record TeamCreated(
    Guid MessageId,
    Guid TeamId,
    string Name,
    string NormalizedName,
    Guid CaptainUserId,
    long Version,
    DateTime OccurredAtUtc) : IIntegrationEvent;

public sealed record TeamRenamed(...);
public sealed record TeamDeleted(...);
public sealed record TeamMemberAdded(...);
public sealed record TeamMemberRemoved(...);
public sealed record CaptainTransferred(...);
```

### 9.5 Transactionality

Event publishing must be transactionally safe.

A domain/application action and the outbox message should commit together where required.

### 9.6 Tests

Add tests for:

```text
- event saved to outbox;
- dispatcher invokes handler;
- duplicate event is ignored;
- retry does not duplicate side effects;
- stale-version event does not overwrite newer projection data.
```

## Phase exit criteria

```text
- Outbox/inbox tables exist.
- Event publishing can happen transactionally.
- At least one Teams event is saved and dispatched in tests.
- Existing API behavior unchanged.
- Full validation passes.
```

---

# Phase 10 — Move Identity/Profile into Its Module

## Branch

```text
refactor/phase-10
```

## Goal

Extract user/profile ownership and reduce direct user-domain coupling from Teams and Competition.

## Tasks

### 10.1 Move user-related code

Move into `Mercurius.Modules.Identity`:

```text
User entity
User service/use cases
User validation behavior
Auth0 profile sync/management-facing code where appropriate
User DTOs/contracts
User endpoints
User EF configuration
```

### 10.2 Add Identity contracts

Expose only narrow contracts:

```csharp
public interface IIdentityModule
{
    Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken ct);

    Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(
        string auth0UserId,
        CancellationToken ct);

    Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken ct);
}
```

### 10.3 Replace direct user entity usage in Teams

Teams should use:

```text
UserId
UserProfileSummary
TeamMemberSnapshot
```

instead of depending on the Identity implementation.

Do not necessarily remove every database FK yet. First reduce code-level coupling.

### 10.4 Publish user profile events

Identity publishes events such as:

```text
UserProfileChanged
UsernameChanged
UserDeleted
UserAnonymized
```

Teams and Discovery consume these where needed.

### 10.5 Clean and optimize touched code

```text
- keep Auth0 concerns isolated;
- keep profile display rules explicit;
- avoid exposing private user fields;
- use read models for user profile queries;
- use AsNoTracking for reads;
- avoid loading full user graphs unnecessarily.
```

## Phase exit criteria

```text
- Identity is a separate class library.
- User endpoints still work with current routes.
- Teams no longer needs direct access to UserService implementation.
- Public user/profile DTO shape remains stable unless OpenSpec says otherwise.
- Tests pass.
```

---

# Phase 11 — Extract Competition Module

## Branch

```text
refactor/phase-11
```

## Goal

Move Games, TournamentRegistration, Matches, Placements, and bracket logic together.

Do not split these too early. They form one coherent tournament lifecycle.

## Tasks

### 11.1 Move competition code

Move into `Mercurius.Modules.Competition`:

```text
Game service/use cases
Match service/use cases
TournamentRegistration service/use cases
bracket moderators
Game endpoints
Match endpoints
TournamentRegistration endpoints
Game/Match/Placement/Registration models
DTOs/contracts
EF configurations
```

### 11.2 Replace Teams/User entity dependencies

Competition should depend on:

```text
Teams.Contracts
Identity.Contracts
```

not Teams/Identity implementations.

Example registration flow:

```csharp
var eligibility = await teamsModule.GetRegistrationEligibilityAsync(
    teamId,
    requestedBy,
    gameId,
    ct);
```

Then Competition stores:

```text
team_id
registered_by_user_id
roster snapshot
historical display names where needed
```

### 11.3 Convert roster display data to snapshots

Target roster model:

```text
registration_id
game_id
user_id
team_id
username_at_registration
team_name_at_registration
selection_status
```

Historical display fields should not sync after registration unless an explicit product rule says they should.

### 11.4 Publish competition events

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

### 11.5 Preserve current routes

Do not introduce `/v2`.

Do not redesign competition routes in this phase.

### 11.6 Clean and optimize touched code

```text
- isolate tournament lifecycle state transitions;
- replace action-heavy method names with business decision names;
- make registration and roster confirmation rules explicit;
- avoid Include-heavy reads where projections are enough;
- add deterministic ordering to list results;
- pass cancellation tokens.
```

## Phase exit criteria

```text
- Competition is a separate class library.
- Game/match/registration endpoints map through MapCompetitionModule.
- Competition no longer references Teams/Identity implementation projects.
- Existing game/registration/match behavior passes tests.
- No endpoint route redesign occurred.
- Full validation passes.
```

---

# Phase 12 — Extract Sponsorship Module

## Branch

```text
refactor/phase-12
```

## Goal

Separate sponsor ownership from game/tournament lifecycle.

## Tasks

### 12.1 Move sponsor code

Move into `Mercurius.Modules.Sponsorship`:

```text
Sponsor service/use cases
Sponsor endpoints
Sponsor entity
Sponsor DTOs/contracts
Sponsor EF configuration
Sponsor validation behavior
```

### 12.2 Decide ownership of game sponsor placement

Before moving placement code, document whether ownership is:

```text
Sponsorship owns:
- sponsor;
- sponsor placement assignment;
- display order/context/tier for sponsor placement.

Competition owns:
- game;
- game lifecycle;
- tournament behavior affected by sponsorship, if any.
```

Sponsorship may reference `game_id`, but it must not own the game.

### 12.3 Publish placement events

```text
SponsorCreated
SponsorUpdated
SponsorDeleted
GameSponsorPlacementChanged
```

Discovery may consume these for search/display.

### 12.4 Preserve current routes and JSON

Do not redesign sponsor endpoints in this phase.

### 12.5 Clean and optimize touched code

```text
- separate upload/media concerns from sponsor metadata;
- avoid loading games when only game IDs are needed;
- keep sponsor placement rules explicit;
- use read models for sponsor listing/detail queries.
```

## Phase exit criteria

```text
- Sponsorship is a separate class library.
- Sponsor endpoints still work with current routes.
- Game sponsor placement behavior still works.
- Competition does not import Sponsorship implementation.
- Tests pass.
```

---

# Phase 13 — Extract Media Module

## Branch

```text
refactor/phase-13
```

## Goal

Move file/image storage concerns out of business modules and platform-adjacent code.

## Tasks

### 13.1 Move file service

Move into `Mercurius.Modules.Media`:

```text
IFileService or replacement media facade
FileService
FileValidationService
file/image validation
storage abstractions
storage key generation
```

### 13.2 Define Media contracts

Example:

```csharp
public interface IMediaModule
{
    Task<StoredImage> StoreTeamLogoAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task<StoredImage> StoreGameImageAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task<StoredImage> StoreSponsorImageAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct);

    Task DeleteAsync(string storageKey, CancellationToken ct);
}
```

### 13.3 Remove direct file handling from business modules

Target flow:

```text
Media stores image.
Media returns MediaId/LogoUrl/storage key.
Owning module stores returned reference.
Owning module publishes changed event if needed.
```

### 13.4 Keep Imageflow middleware in Platform/API host

Image serving remains host/platform infrastructure because it is HTTP middleware.

### 13.5 Clean and optimize touched code

```text
- centralize upload validation;
- avoid duplicated image type/size checks;
- make storage key naming deterministic and safe;
- stream file contents instead of buffering unnecessarily where possible;
- keep deletion idempotent.
```

## Phase exit criteria

```text
- Media is a separate class library.
- Team logo upload/remove behavior still works.
- Game image behavior still works.
- Sponsor image behavior still works.
- File validation tests pass.
- Image serving still works.
```

---

# Phase 14 — Extract Discovery/Search Module

## Branch

```text
refactor/phase-14
```

## Goal

Stop global search from depending on everyone’s live tables.

## Tasks

### 14.1 Move Search into Discovery

Move into `Mercurius.Modules.Discovery`:

```text
Search service/use cases
Search endpoints
Search DTOs/contracts
Search projection handlers
```

### 14.2 Introduce search projection table

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

### 14.3 Feed Search via events

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

### 14.4 Add rebuild job

Because projections can drift, add a manual/admin rebuild mechanism.

No `/v2` endpoints.

Use current internal/admin route conventions. If a new route is externally visible, add OpenSpec.

Resource-oriented shape:

```text
POST /internal/discovery/search-index-rebuild-jobs
GET  /internal/discovery/search-index-rebuild-jobs/{jobId}
```

### 14.5 Preserve search behavior unless explicitly changed

Before replacing live queries with projections, tests must cover:

```text
- ordering;
- pagination/cursor behavior;
- deleted/private users;
- teams;
- games;
- sponsors;
- route values returned in search results;
- stale projection handling.
```

### 14.6 Clean and optimize touched code

```text
- normalize search text consistently;
- avoid querying multiple live tables per search request after projection exists;
- keep rebuild job idempotent;
- avoid stale-version event overwrites;
- make deleted entity handling explicit.
```

## Phase exit criteria

```text
- Discovery is a separate class library.
- Search endpoint keeps equivalent behavior unless OpenSpec says otherwise.
- Search is fed by projections.
- Projection rebuild test passes.
- Duplicate/stale event tests pass.
- Tests pass.
```

---

# Phase 15 — Persistence Boundary Tightening

## Branch

```text
refactor/phase-15
```

## Goal

Move from one all-knowing EF model toward module-owned persistence.

Do this incrementally.

## Tasks

### 15.1 Move EF configurations into modules

Move mappings into module-owned infrastructure areas:

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

### 15.2 Keep one physical DbContext first

First target:

```text
one physical MercuriusDbContext
module-owned EF configurations
module schemas/table prefixes where appropriate
```

Later target:

```text
IdentityDbContext
TeamsDbContext
CompetitionDbContext
SponsorshipDbContext
DiscoveryDbContext
```

Do not jump directly to many DbContexts while also moving projects and changing routes.

### 15.3 Introduce schemas only as a persistence phase

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

Schema changes require migrations and careful database validation.

### 15.4 Reduce cross-module EF navigation

Target:

```text
Inside module:
- normal EF relationships and FKs are fine.

Across modules:
- store IDs.
- avoid EF navigation properties.
- avoid direct DbSet access.
```

### 15.5 Performance review

When moving mappings, review:

```text
- indexes used by lookup paths;
- uniqueness constraints;
- cascade delete behavior;
- query filters;
- foreign key delete behavior;
- large Include-heavy query paths.
```

Do not add indexes casually. Add them when they support known query paths.

## Phase exit criteria

```text
- EF mappings live with modules.
- DbContext still works.
- Migrations generate cleanly.
- Database update succeeds in a test/dev database.
- Tests pass.
- No module imports another module's EF entities.
```

---

# Phase 16 — Endpoint Simplification In Place

## Branch

```text
refactor/phase-16
```

## Goal

Simplify and clarify endpoints in the existing API version only.

Do not introduce `/v2`.

This phase is required for this refactor goal: actively replace malformed/action-style endpoints with resource-oriented REST endpoints in-place.

## Mandatory preconditions

Before this phase:

```text
- module boundaries are stable enough that route changes are isolated;
- OpenSpec changes exist for every externally visible route change;
- endpoint cleanup is intentionally backend-led and not blocked by backward-compatibility concerns;
- route/security/OpenAPI tests exist;
- old route removal is intentional and tested.
```

## Route design principles

Use resource-oriented route shapes in the existing route version/convention.

Avoid action routes such as:

```text
POST /games/{id}/start
POST /games/{id}/complete
POST /games/{id}/cancel
POST /teams/{id}/leave
```

Prefer resource state updates where appropriate.

Examples:

```text
PUT/PATCH /games/{gameId}/lifecycle-state
DELETE    /teams/{teamId}/members/me
PUT       /teams/{teamId}/captain
PUT       /teams/{teamId}/logo
DELETE    /teams/{teamId}/logo
PATCH     /team-invites/{inviteId}
PATCH     /games/{gameId}/registrations/{registrationId}/roster-members/{userId}
```

These are examples only. Codex must align the final shape with OpenSpec and the existing route conventions.

## No compatibility route duplication

Do not keep both action routes and resource routes unless the OpenSpec explicitly requires temporary compatibility.

If compatibility is required, document when old routes will be removed.

## Phase exit criteria

```text
- No /v2 endpoints were introduced.
- Endpoint changes match OpenSpec.
- Endpoint cleanup is resource-oriented and OpenSpec-backed, without adding compatibility route duplication unless explicitly required.
- Route/security/OpenAPI tests pass.
- Removed routes are tested as absent.
- Tests pass.
```

---

# Phase 17 — Tighten Internals and Public Surface

## Branch

```text
refactor/phase-17
```

## Goal

Make modular boundaries real.

## Tasks

### 17.1 Make implementation types internal

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

### 17.2 Use InternalsVisibleTo only for tests

Example:

```csharp
[assembly: InternalsVisibleTo("Mercurius.Modules.Teams.Tests")]
```

Do not use broad `InternalsVisibleTo` between modules.

### 17.3 Add public API approval/reflection tests

Add tests that fail when a module accidentally exposes:

```text
Team
Game
User
DbContext
Repository
internal service
IQueryable
EF configuration
```

from an implementation assembly.

Prefer reflection tests first. Avoid adding dependencies unless justified.

### 17.4 Remove or shrink old centralized DI

Delete or shrink old central service registration once registrations have moved.

### 17.5 Clean naming/public surface

Review public names for clarity:

```text
- module configuration extensions;
- contract DTOs;
- command/query records;
- integration event names;
- typed ID names;
- facade method names.
```

Names should reflect business language, not implementation mechanics.

## Phase exit criteria

```text
- Most implementation classes are internal.
- API host cannot directly inject old services.
- Public API approval/reflection tests pass.
- Architecture tests pass.
- Functional tests pass.
```

---

# Phase 18 — Test Suite Reshaping

## Branch

```text
refactor/phase-18
```

## Goal

Move tests from current implementation coupling to module behavior.

## Tasks

### 18.1 Split tests by module

Create:

```text
tests/Mercurius.Api.Tests
tests/Platform.Tests
tests/Mercurius.Modules.Identity.Tests
tests/Mercurius.Modules.Teams.Tests
tests/Mercurius.Modules.Competition.Tests
tests/Mercurius.Modules.Sponsorship.Tests
tests/Mercurius.Modules.Discovery.Tests
tests/Mercurius.Modules.Media.Tests
```

Only split what is useful. Do not create empty projects just to satisfy the list.

### 18.2 Test categories

Use these layers:

```text
Unit tests:
- domain rules
- use-case handlers
- validation

Module integration tests:
- module DbContext/configuration behavior
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

### 18.3 Rewrite route tests around intended API

Update route tests to assert:

```text
- current intended routes exist;
- removed routes are absent when removal is intentional;
- auth metadata is correct;
- public endpoints are anonymous;
- mutation endpoints are authenticated/admin as intended;
- OpenAPI contains expected paths.
```

Do not introduce `/v2` assertions.

### 18.4 Add event sync tests

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

## Phase exit criteria

```text
- Tests reflect the new architecture.
- Old implementation-coupled tests are removed or rewritten.
- Module tests pass.
- API tests pass.
- Architecture tests pass.
- Full validation passes.
```

---

# Phase 19 — Remove Transitional Adapters and Clean Up

## Branch

```text
refactor/phase-19
```

## Goal

Finish the migration by deleting old paths, old services, and transitional adapters.

## Tasks

### 19.1 Remove old service interfaces

Delete transitional interfaces like old `ITeamService` and `IGameService` once no endpoint uses them.

These should not remain as public module boundaries if they expose mixed DTO/domain contracts.

### 19.2 Remove old namespaces

Clean up or empty old areas such as:

```text
Mercurius.LAN.API.Endpoints
Mercurius.LAN.API.Services
Mercurius.LAN.API.DTOs
Mercurius.LAN.API.Models
```

Leave only host-specific types in the API project.

### 19.3 Remove obsolete routes only if OpenSpec says so

No `/v2` migration strategy exists.

Remove old action-style routes only when:

```text
- the in-place replacement exists;
- the OpenSpec marks compatibility as unnecessary or explicitly removes the old route;
- route tests assert the old route is absent;
- OpenSpec marks the removal intentional.
```

### 19.4 Final dependency check

Ensure:

```text
- no module references another module's implementation project;
- no module exposes EF entities;
- no API endpoint injects internal services directly;
- no obsolete routes remain unless intentionally kept;
- no public contract returns EF entities;
- no public contract exposes IQueryable;
- no broad InternalsVisibleTo exists.
```

### 19.5 Final cleanup pass

Within the touched migration scope:

```text
- remove dead code;
- remove unused registrations;
- remove unused DTOs;
- remove unused tests;
- improve final naming;
- ensure XML/docs/comments explain non-obvious boundaries;
- remove comments that only describe old transitional behavior.
```

## Phase exit criteria

```text
- Transitional adapters removed.
- Architecture tests pass.
- Full test suite passes.
- API runs with module registration only.
- OpenAPI generation succeeds.
- Database validation succeeds.
```

---

# Recommended phase order

```text
1. AGENTS.md guardrails and refactor branch setup
2. Baseline safety net and contract freeze
3. Monolith code quality and boundary preparation
4. Platform extraction
5. Solution and project skeleton
6. Contracts before implementations
7. Teams extraction
8. Realtime split
9. Eventing/outbox/inbox
10. Identity extraction
11. Competition extraction
12. Sponsorship extraction
13. Media extraction
14. Discovery/Search extraction
15. Persistence boundary tightening
16. Endpoint simplification in place
17. Internals/public surface hardening
18. Test suite reshaping
19. Transitional cleanup
```

Endpoint simplification can move earlier only if it is the explicit product priority, but it must still avoid `/v2` and must not be combined with physical module extraction or persistence changes.

---

# Global Definition of Done per phase

Every phase must include a PR description with:

```text
- phase number;
- source branch;
- target branch;
- summary of changes;
- behavior change: yes/no;
- OpenSpec impact: none/updated/not required and why;
- route impact: none/changed/removed;
- DTO/JSON impact: none/changed;
- database impact: none/migration required;
- validation commands run;
- known risks;
- follow-up phases.
```

Every phase must pass:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Every phase must also verify:

```text
- OpenAPI generation succeeds;
- API starts locally;
- no accidental route changes;
- no accidental DTO JSON changes;
- no module implementation references another module implementation;
- no public contract leaks EF entities or IQueryable.
```

After eventing is introduced:

```text
- outbox dispatcher test passes;
- inbox/idempotency test passes;
- no stale-version event overwrites newer projection data.
```

After module extraction begins:

```text
- no module endpoint injects another module's internal service;
- API host composes modules only through AddXModule/MapXModule;
- implementation classes are internal unless intentionally public.
```
---

# Final sequencing rule

Do not combine these in one phase:

```text
- moving code into a new class library;
- changing endpoint routes;
- changing persistence ownership.
```

Pick one main kind of change per phase.

This keeps the API working after each phase and makes failures easy to locate.
