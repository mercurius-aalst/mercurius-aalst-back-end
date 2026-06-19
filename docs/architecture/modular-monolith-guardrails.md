# Modular Monolith Guardrails

This note records the durable migration rules for moving the back-end toward a modular monolith. It is intentionally independent of the current source layout so it can remain useful while projects, folders, and namespaces change.

## Target Module Ownership

- Identity owns user identity, profile data, Auth0 binding, username and email uniqueness, deletion/anonymization state, and user profile events.
- Teams owns teams, memberships, invites, captain transfer, team logo references, and membership rules that are not tied to a competition lifecycle.
- Competition owns games, tournament registrations, roster members, matches, placements, brackets, and tournament lifecycle rules.
- Sponsorship owns sponsors, sponsor tiers and contexts, display metadata, and sponsor placement assignment when it is not part of competition lifecycle decisions.
- Discovery owns search projections, searchable documents, search endpoint behavior, and projection rebuild jobs.
- Media owns physical file/image storage, upload validation, storage key management, and generated media references.
- Platform owns host-level infrastructure such as auth wiring, Swagger/OpenAPI, validation plumbing, rate limiting, exception handling, CORS, SignalR/realtime infrastructure, outbox/inbox infrastructure, migrations/startup plumbing, route constraints, and HTTP middleware.

## Dependency Rules

Allowed dependencies:

- API host may reference module implementation projects for composition.
- Module implementations may reference their own contracts.
- Module implementations may reference SharedKernel.
- Module implementations may reference Platform only when infrastructure concerns require it.
- Module contracts may reference SharedKernel.
- Competition may reference Teams.Contracts and Identity.Contracts.
- Discovery may reference Teams.Contracts, Competition.Contracts, Sponsorship.Contracts, and Identity.Contracts.

Forbidden dependencies:

- A module must not reference another module's implementation project.
- A module must not reference another module's `DbContext`, repository, EF entity, or internal service.
- Discovery must not query every module's live tables directly once projection ownership exists.
- Module contracts must not reference EF entities, `DbContext`, repositories, `IQueryable`, validators, EF configurations, or navigation properties.
- `InternalsVisibleTo` must not be used to let one module access another module's internals.

## Public Versus Internal Surface

Public module surface should be limited to:

- `AddXModule(...)` registration extensions;
- `MapXModule(...)` endpoint mapping extensions;
- contracts, read models, DTOs, commands, queries, typed IDs, snapshots, integration events, and facade interfaces.

Implementation details should become internal after extraction:

- domain entities;
- EF configurations;
- repositories;
- use-case handlers;
- validators;
- endpoint classes;
- infrastructure services;
- application services.

The API host should compose modules through registration and mapping extensions. It should not directly inject module internals or depend on EF model details.

## Data Ownership And Duplication

One business fact has one owner. Other modules may duplicate data only with an explicit classification:

- Reference: stores only another module's ID and does not need synchronization.
- Snapshot: stores a historical copy captured at a moment in time and should not automatically synchronize.
- Projection: stores a read/search/display model synchronized through integration events.
- Cached decision data: stores local data needed for decisions and must be synchronized through versioned events with reconciliation rules.

Do not try to keep every duplicate perfectly synchronized. Document the ownership and synchronization model where duplication exists.

## OpenSpec Requirements

OpenSpec is required for externally observable behavior changes, including:

- route shape changes;
- request or response DTO changes;
- validation behavior changes;
- authorization or privacy behavior changes;
- persistence behavior changes;
- event or projection behavior that changes visible results;
- search behavior changes;
- lifecycle or domain-rule changes.

OpenSpec is not required for internal refactoring that preserves behavior, but the PR must explain why behavior is unchanged.

## Endpoint Strategy

- Do not introduce `/v2` endpoints.
- Route simplification happens in place only when OpenSpec covers the intended route change.
- Do not combine route changes with physical module extraction or persistence ownership changes.
- Do not preserve malformed or action-style routes solely for backward compatibility when OpenSpec calls for removal.
- Removed or renamed routes must be documented and tested as absent.
- During module extraction phases, preserve current route patterns and authorization behavior.

## Branch Workflow

- Maintain `refactor/modular-monolith` as the long-lived integration branch.
- Create each phase branch from the latest `refactor/modular-monolith`.
- Use phase branch names such as `refactor/phase-1`, `refactor/phase-2`, or a phase-suffixed split name when a phase is too large or risky.
- Open each phase PR into `refactor/modular-monolith`.
- Do not start the next phase from an unmerged phase branch.
- Do not PR phase branches directly into `main`, `develop`, or an earlier bugfix branch.
- Keep `modular-monolith-progress.md` updated so another agent can resume the migration without relying on chat history.

## Per-Phase Validation

Run these commands from the repository root for every phase:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Each phase PR description should include:

- phase number;
- source branch and target branch;
- summary of changes;
- behavior change: yes or no;
- OpenSpec impact;
- route impact;
- DTO/JSON impact;
- database impact;
- validation commands run;
- known risks;
- follow-up phases.

Where relevant, also verify:

- the API starts locally;
- OpenAPI document generation succeeds;
- public routes and metadata did not change accidentally;
- response JSON shapes remain stable;
- no public contract leaks EF entities or `IQueryable`;
- no module implementation references another module implementation.
