## Context

Phase 6 introduced `Mercurius.Modules.Sponsorship.Contracts`, and Phase 11 moved Competition to
that contract for sponsor reads and placement replacement. The contract is currently backed by
`LegacySponsorshipModuleAdapter` in the API host, while sponsor CRUD, endpoints, entity types, and
EF mappings remain in `MercuriusAPI`. The existing shared database already contains `Sponsors` and
`GameSponsorPlacements`; Competition configures the foreign key from a game to the placement via a
generic placement type.

The extraction MUST preserve the current route templates, authorization and antiforgery metadata,
form request shapes, response JSON, tables, columns, and delete behavior. Phase 13 owns moving the
physical media implementation, so this phase can depend only on `Media.Contracts` and retain the
host's temporary Media adapter.

## Goals / Non-Goals

**Goals:**

- Make Sponsorship the code owner of Sponsor and GameSponsorPlacement state, sponsor CRUD and
  placement application services, endpoint mapping, validation, and EF configuration.
- Keep Competition dependent only on `Sponsorship.Contracts`; a placement retains a `GameId` value
  but does not reference or own a Competition entity.
- Preserve the one-to-one game placement relationship and all existing cascade semantics in the
  shared physical database.
- Publish Sponsorship lifecycle facts to the existing transactional outbox with successful state
  mutations.
- Keep file uploads outside Sponsorship ownership by using `IMediaModule`.

**Non-Goals:**

- Redesigning sponsor routes, authorization, validation rules, or JSON contracts.
- Moving the physical file-storage implementation, image processing pipeline, or media endpoints;
  those are Phase 13 work.
- Splitting the shared database, renaming tables or columns, or adding a new database migration.
- Moving game lifecycle behavior, adding cross-module EF navigations, or changing Competition's
  current sponsorship contract.
- Adding event consumers or Discovery projections; later phases may consume the published facts.

## Decisions

### Sponsorship owns the entity and application implementation

Move the existing Sponsor and GameSponsorPlacement entity types, sponsor DTOs, service, and
endpoint group into `Mercurius.Modules.Sponsorship`. `AddSponsorshipModule<TDbContext>` will
register an internal DbContext adapter, the Sponsor CRUD service, and a module facade implementing
the existing public `ISponsorshipModule` contract. `MapSponsorshipModule` will map the current
`v{version:apiVersion}/lan/sponsors` group with its current version, tags, authorization, anonymous
read access, and antiforgery settings. The API composition root will call these extensions and
remove the legacy sponsor registrations and adapter.

This follows the established Competition pattern: the host may reference module implementation for
composition, but Competition and other modules reference only `Sponsorship.Contracts`. Keeping the
endpoint group in the owning implementation avoids a permanent API-host facade. Keeping the CRUD
service internal avoids exposing implementation services as a module contract.

### Sponsorship retains placement ownership through an external game ID

Sponsorship will own the placement entity, its Sponsor relationship, its context/headline/support
line/display-order rules, and the existing unique `GameId` placement constraint. It will hold the
game identifier as a scalar external reference and MUST NOT introduce a Game navigation or a
Competition implementation reference. The existing generic Competition model configuration will
continue to be supplied the Sponsorship placement type by the host so the database's game-to-
placement cascade remains unchanged.

The existing `ISponsorshipModule` remains the Competition boundary. Its single and batched read
methods use no-tracking projections, and replacement validates sponsor existence before changing a
placement. This preserves the bounded batch enrichment that Phase 11 established and avoids N+1
queries. A direct Competition-to-Sponsorship contract is preferred over routing a game mutation
through the host because it keeps ownership explicit without adding an unnecessary mediator.

### Sponsorship supplies its own shared-database model configuration

`ApplySponsorshipModelConfiguration` will configure the existing entity keys, scalar constraints,
enum-to-string conversions, Sponsor-to-placement cascade, unique placement `GameId` index, and
unchanged table names. The host `MercuriusDBContext` will compose the physical Game-to-placement
foreign key by EF entity name because both implementation types remain internal; this keeps the
cross-module database link at the composition root without an implementation-project reference.
The host remains the one physical database composition root.

An internal generic DbContext adapter will provide only Sponsorship entity sets and save operations
to module services. This avoids exposing the host DbContext from a module contract and avoids a
second context or repository abstraction. The alternative of leaving the entity configuration and
legacy adapter in the API host would retain the boundary violation and is rejected.

### Sponsor metadata consumes Media only through contracts

For create and logo replacement, the module will adapt the existing form upload to the
`Media.Contracts` upload input and call `IMediaModule.SaveImageAsync`. It will retain the returned
URL as sponsor metadata. This separates sponsorship ownership from image validation and storage
while retaining `LegacyMediaModuleAdapter` until Media is extracted. Existing delete behavior will
remain unchanged; this phase MUST NOT start deleting stored files as a side effect.

### Mutations publish versioned Sponsorship facts in the shared transaction

`Sponsorship.Contracts.V1` will define distinct integration-event records for create, update,
delete, and game-placement changes. The module will use `IModuleEventPublisher` to enqueue the
appropriate record in the same database transaction as the mutation. Because Sponsor and placement
identifiers are database-generated, a create flow saves the entity to generate its identifier,
enqueues the event, saves the outbox row, and commits the enclosing transaction only after both
states are durable. Event payloads will contain the identifiers and placement/sponsor facts needed
by later projections without exposing EF entities.

The existing Platform outbox owns serialization, retries, dispatch, and inbox idempotency. The
Sponsorship module will not implement an outbox or consumer of its own. Publishing records in an
explicit V1 namespace is preferred to reusing untyped audit messages because event type names are durable and
later Discovery consumers need stable factual payloads.

## Risks / Trade-offs

- [A physical entity move can accidentally change EF model metadata] → Reuse the existing table,
  column, index, conversion, and cascade configuration; compare generated model behavior through
  focused EF tests and do not create a migration.
- [The shared context could become a leaked module dependency] → Limit the adapter to the
  Sponsorship internal surface and publish only contracts, DTOs, and composition extensions.
- [Media remains temporarily host-backed] → Depend solely on `Media.Contracts`; Phase 13 can
  replace the host adapter without touching Sponsorship logic.
- [Event publication could be omitted from one mutation path] → Cover create, update, delete, and
  placement replacement with outbox tests that verify state and event are saved together.
- [Read-model relocation could reintroduce N+1 queries] → Keep the current placement batch query
  and projection-based Sponsor reads, with regression coverage for Competition listing/detail.

## Migration Plan

1. Deploy the extraction as a code-only change against the existing schema; no data backfill or
   migration is required because table and column mappings are unchanged.
2. At startup, compose Sponsorship through its module extension and map its existing endpoint
   routes; remove the legacy Sponsorship adapter registration only after the module facade is
   registered.
3. Verify sponsor CRUD, game placement, outbox records, OpenAPI metadata, and Competition sponsor
   enrichment before rollout.
4. Roll back by restoring the previous code version. The schema is unchanged; already persisted
   Sponsorship outbox messages remain durable and may be dispatched once the new version is active
   again.

## Open Questions

None. Phase 13 will replace the temporary host Media adapter, and later Discovery work will decide
which Sponsorship events to consume.
