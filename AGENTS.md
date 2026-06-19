# Engineering Guidelines

## Scope

These instructions apply to the whole back-end repository.

## Core Principles

- Keep behavior stable unless the task explicitly requests a functional change.
- Prefer small, reviewable changes with clear validation.
- Use OpenSpec for externally observable behavior changes.
- Preserve public API JSON shapes unless the spec says otherwise.
- Treat route, authorization, validation, persistence, and DTO changes as intentional contract changes only when OpenSpec explicitly requires them.
- Do not combine route changes, persistence changes, and physical project moves in one PR.
- Keep anonymous, authenticated, and admin-only authorization boundaries explicit.
- Do not introduce `/v2` endpoints as a compatibility strategy.
- Do not keep old routes solely for backward compatibility when OpenSpec calls for an in-place cleanup.

## OpenSpec

- Inspect existing specs before changing externally observable behavior.
- Create or update an OpenSpec change before implementing route, request, response, validation, authorization, privacy, persistence, search, lifecycle, or event/projection behavior changes.
- Include `proposal.md`, `tasks.md`, and relevant spec deltas for new OpenSpec changes.
- Use RFC 2119 language in specs: MUST, SHOULD, MAY.
- Keep implementation, tests, and task checklists aligned with the active OpenSpec change.
- Pure refactoring, documentation, formatting, and mechanical integration plumbing do not require OpenSpec when behavior is unchanged.

## Code Quality

- Keep implementations straightforward and avoid unnecessary wrappers, indirection, or pattern use.
- Improve naming when touching unclear code.
- Prefer cohesive methods and explicit domain/application concepts.
- Remove duplication inside the touched scope, but avoid brittle shared abstractions.
- Use design patterns only when they clarify responsibilities, boundaries, performance, security, or testability.
- Keep one primary class, record, entity, DTO, endpoint group, or service per file unless an additional type is a small private nested detail.
- Reuse existing components, services, DTOs, options, tests, configuration, and validation patterns where possible.
- Keep feature-domain dependencies minimal and explicit.

## Performance

- Use `AsNoTracking` for read-only Entity Framework queries.
- Prefer projections over `Include`-heavy loading when only read models are needed.
- Avoid N+1 queries, repeated render-triggering work, redundant service calls, and unnecessary materialization.
- Pass cancellation tokens through async flows.
- Keep ordering and pagination deterministic.
- Do not load large graphs, files, or navigation trees unless required.
- Review touched queries for batching, filtering, indexes, and cross-module boundary impact.

## API And Contracts

- Preserve public JSON shapes unless OpenSpec explicitly requires a change.
- Do not expose private user data, internal auth fields, deletion state, or account metadata through public endpoints.
- Add or update tests for changed routes, DTO shapes, authorization rules, privacy rules, validation behavior, and OpenAPI metadata.
- Keep public endpoints privacy-safe and mutation endpoints authorization-safe.
- Do not expose EF entities, `DbContext`, repositories, `IQueryable`, or navigation properties through public APIs or module contracts.

## Modular Monolith Boundaries

- Modules own business capabilities and expose only intentional contracts.
- Module contracts may expose DTOs, command records, query records, read models, snapshots, integration events, typed IDs, and facade interfaces.
- Module contracts must not expose EF entities, `DbContext`, repositories, `IQueryable`, implementation services, validators, EF configurations, or navigation properties.
- Implementation classes should become `internal` after extraction.
- Use `InternalsVisibleTo` only for the matching test project.
- Do not add implementation-to-implementation module references.
- One business fact has one owner; other modules may store references, snapshots, projections, or cached decision data intentionally.

## Branching

- Perform the modular migration on `refactor/modular-monolith`.
- Use one phase branch per phase, created from the latest `refactor/modular-monolith`.
- Each phase branch must PR into `refactor/modular-monolith`.
- Do not PR phase branches directly into `main`, `develop`, or earlier bugfix branches.
- Stop after opening each phase PR and wait for human review and merge before starting the next phase.
- Keep `docs/architecture/modular-monolith-progress.md` current when a phase PR is opened, merged, blocked, or split.

## Validation

Run validation from the repository root before completing each phase:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Also note API startup, OpenAPI generation, route impact, DTO/JSON impact, database impact, configuration impact, and known risks in the PR description.
