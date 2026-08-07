## Why

Global search currently reads several module-owned tables in the API host on every request. That coupling makes search expensive to extend and prevents each module from owning its own data while preserving a reliable public discovery experience.

## What Changes

- Move the existing global-search endpoint, request handling, and response mapping into `Mercurius.Modules.Discovery` while preserving its public route, authorization, rate limiting, JSON shape, ordering, and cursor semantics.
- Add a Discovery-owned search-document projection that is updated from durable module integration events and ignores duplicate or stale event versions.
- Add an admin-only, internal search-index rebuild job API so projections can be rebuilt deliberately and its status observed.
- Add focused API, projection, rebuild, and architecture tests for Discovery.

## Capabilities

### New Capabilities

- `discovery-search-projections`: Discovery-owned, version-safe search projections and an internal rebuild-job lifecycle for public search documents.

### Modified Capabilities

- None.

## Impact

- Affects the API host's search composition, the existing search service and endpoint, and the Discovery implementation and contracts projects.
- Adds a `discovery.search_documents` persistence table and a migration, plus a rebuild-job persistence model if required for observable job status.
- Consumes Identity, Teams, Competition, and Sponsorship event contracts through the existing durable outbox/inbox infrastructure; Discovery does not reference their implementation projects or live tables at query time.
- Adds internal admin endpoints without changing the existing public search API contract.
