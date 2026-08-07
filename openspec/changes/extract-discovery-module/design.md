## Context

The public search endpoint currently combines live queries over Identity and Teams with a call into Competition. That query fan-out couples the API host to module implementation data and makes search results depend on several synchronous reads. The Discovery projects and their initial contracts already exist, and the Platform outbox/inbox dispatcher provides durable, at-least-once delivery with a message timestamp.

Phase 14 introduces a persisted Discovery projection. It also adds the first internal operational API in the module: an admin-only rebuild job that handles projection drift. Existing public search behavior is contract-frozen by `public-global-search` and remains unchanged.

## Goals / Non-Goals

**Goals:**

- Route public search through `IDiscoveryModule` and query only Discovery-owned documents at request time.
- Preserve the current public route, anonymous authorization, rate-limit policy, response JSON, privacy filtering, case-insensitive matching, relevance ordering, and keyset cursor behavior.
- Persist one privacy-safe search document per source entity with a source version and soft-deletion state.
- Project Identity, Teams, Competition, and Sponsorship lifecycle events with duplicate delivery and stale-event protection.
- Provide an idempotent, observable, admin-only rebuild job.

**Non-Goals:**

- Adding sponsors to the current public search result set; sponsor documents are maintained for Discovery lifecycle completeness and future Discovery views only.
- Redesigning public endpoint routes, response DTOs, authorization, or rate limiting.
- Splitting the physical EF `DbContext`, moving every existing EF mapping, or introducing separate module databases; those are Phase 15 work.
- Adding a general-purpose job platform or real-time projection consistency guarantee.

## Decisions

### Store a Discovery search-document projection

`discovery.search_documents` holds a unique `(entity_type, entity_id)` document with `title`, `subtitle`, `image_url`, `route`, `normalized_text`, `source_version`, `is_deleted`, and `updated_at_utc`. A partial trigram index on active `normalized_text` supports the existing contains-search behavior, and a deterministic b-tree ordering index supports keyset pagination.

Discovery owns the document entity, model configuration, and a narrow DbContext adapter. It does not receive `MercuriusDBContext`, source entities, repositories, or `IQueryable` from another module. The host composes the module through `AddDiscoveryModule<MercuriusDBContext>` and applies its model configuration while the single physical DbContext remains transitional.

Alternatives considered: retaining live union queries would preserve immediate consistency but maintains prohibited cross-module coupling; a separate database/DbContext is deliberately deferred to Phase 15.

### Preserve the public contract through a direct projection query

`IDiscoveryModule.SearchAsync` validates the shared request rules and queries only active User, Team, and Game document types. It reproduces the current exact/prefix/contains relevance ranking, type precedence, normalized-label ordering, and cursor encoding. The endpoint maps the facade's string-valued result contract directly and continues to return no Sponsor results.

The stored `route` and `image_url` fields are projection metadata. Current user/team navigation fields are represented by their titles, and game navigation remains the stored entity ID, so the HTTP JSON shape does not gain fields.

Alternatives considered: retaining the API DTO mapping layer is unnecessary duplication; serializing a result enum risks changing the existing string `type` contract.

### Update projections from durable events using the outbox timestamp as source version

Discovery registers handlers for user-profile changes/deletions, team creates/renames/deletions, game creates/updates/cancellations/deletions, and sponsor creates/updates/deletions. A handler upserts or soft-deletes a document only when `ModuleEventContext.OccurredAtUtc.Ticks` is not older than the stored source version. The existing inbox marker prevents duplicate-message side effects; the version guard also prevents an older failed message retried after a newer message from restoring stale state.

The user-profile event gains an explicit `IsSearchable` flag, and game deletion emits a deletion event. These internal integration facts are necessary to preserve the current exclusion of incomplete users and removed games without Discovery reading source tables.

Alternatives considered: adding version columns to every source aggregate would increase this phase's persistence surface. The durable outbox occurrence timestamp already orders source facts and safely resolves retried older messages.

### Rebuild through source-module contracts and a persistent job record

`discovery.search_index_rebuild_jobs` records an ID, pending/running/completed/failed state, timestamps, and a bounded error. `POST /internal/discovery/search-index-rebuild-jobs` is restricted to the existing `admin` role and coalesces with an active job. A module hosted worker claims and runs pending jobs; `GET /internal/discovery/search-index-rebuild-jobs/{jobId}` returns the persisted status.

The rebuild coordinator reads privacy-safe search snapshots from Identity, Teams, and Competition facades, and uses the existing Sponsorship summary facade. It writes documents with the job start time as their source version. This makes an event produced before the rebuild stale, while an event produced during or after the rebuild can update the document. Source modules expose bounded purpose-specific snapshot contracts instead of leaking entities or queryables.

Alternatives considered: rebuilding directly from the host DbContext would be shorter but violates the intended Discovery boundary. An in-request synchronous rebuild would make status observation and large-data operation unreliable.

## Risks / Trade-offs

- [Projection lag after a source mutation] → Search remains eventually consistent; the durable dispatcher retries failed deliveries and administrators can request a rebuild.
- [A source event can arrive out of order after retry] → The outbox occurrence timestamp is compared with `source_version`; older facts cannot overwrite newer documents.
- [A rebuild and an event overlap] → Rebuild writes use its start time as the source version, preserving newer events and rejecting superseded pre-rebuild events.
- [Search documents contain private data] → Event handlers and rebuild snapshots persist only the fields permitted by existing public-search requirements; user documents are created only for complete, active profiles.
- [Large rebuild workload] → The hosted worker runs outside the request path, batches writes, and exposes terminal failure details through the job record.

## Migration Plan

1. Add Discovery document and rebuild-job model configuration, a migration, and indexes under the `discovery` schema.
2. Register Discovery composition, event handlers, and the rebuild worker alongside the existing outbox dispatcher.
3. Move search endpoint behavior into Discovery and remove the API-host search service after contract tests pass.
4. Deploy the migration before the application release. The first rebuild job backfills active documents; subsequent source changes flow through the outbox.
5. Roll back application code only after stopping the worker; the new tables are additive and can remain until a later cleanup migration.

## Open Questions

- None. Sponsor documents are deliberately excluded from the contract-frozen public search result set and may be exposed only by a future OpenSpec change.
