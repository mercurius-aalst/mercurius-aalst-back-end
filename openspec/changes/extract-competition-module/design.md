## Context

Competition endpoints and DTOs can be moved mechanically, but the remaining services and EF
entities currently navigate directly to Identity users, Teams, and Sponsors owned outside the
Competition capability. The application uses one physical `MercuriusDBContext`, so the module
must own its model configuration without creating a second database or changing table names.

## Goals / Non-Goals

**Goals:**

- Make Competition the code owner of games, registrations, roster members, matches, placements,
  brackets, and their EF configuration.
- Restrict cross-module access to Identity, Teams, Sponsorship, Media, and Platform contracts.
- Persist display snapshots for new and existing registration rows.
- Keep the current database tables, routes, authorization metadata, validation behavior, and JSON.
- Publish Competition-owned durable lifecycle events in the same transaction as mutations.

**Non-Goals:**

- Route cleanup or `/v2` endpoints.
- Moving Sponsor or physical media ownership into Competition.
- Splitting the shared physical database.
- Updating `MercuriusDBContextModelSnapshot.cs`; Phase 11 uses a reviewed hand-authored migration.

## Decisions

- Competition uses a generic `ICompetitionDbContext` adapter over the host DbContext, matching the
  existing module-composition pattern. The API host calls the module's model-builder extension.
- Competition entities store external IDs instead of EF navigation properties to User, Team, or
  Sponsor entities. Generic model configuration preserves current foreign keys without introducing
  implementation project references.
- Registrations store username and team-name snapshots captured from Identity/Teams contracts.
  Public DTOs remain unchanged and can still be enriched in bounded batches where platform IDs or
  current team roster details are required.
- Sponsor placement and image storage remain with their owning capabilities. Competition calls
  `Sponsorship.Contracts` and `Media.Contracts` through adapters registered by the composition root.
- Competition lifecycle events are records in `Competition.Contracts` and are published through
  Platform's durable module-event publisher before the transaction commits.
- The migration is hand-authored and idempotent for deployment ordering. Per explicit project
  direction, the EF model snapshot is intentionally left unchanged.

## Risks / Trade-offs

- [Snapshot columns are nullable during rolling deployment] → The migration backfills existing rows,
  and application mapping has deterministic fallback values until all data is populated.
- [Removing cross-module navigations requires more explicit enrichment] → Batch Identity lookups and
  bounded Teams snapshot calls prevent N+1 behavior.
- [The unchanged EF model snapshot can cause a later generated migration to rediscover these
  columns] → Document the exception in the migration and require the next migration author to
  reconcile it explicitly.
- [Lifecycle event publication can expose transactional gaps] → Publish to the existing outbox
  before `SaveChangesAsync`/commit and cover the behavior with integration tests.
