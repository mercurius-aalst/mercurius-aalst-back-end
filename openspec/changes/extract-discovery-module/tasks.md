## 1. Contracts and persistence foundation

- [x] 1.1 Extend source module event/snapshot contracts so Discovery can preserve incomplete-user and deleted-game search behavior without source implementation references.
- [x] 1.2 Define Discovery-owned search document and rebuild-job models, DbContext adapter, model configuration, contracts, and project dependencies.
- [x] 1.3 Add the `discovery` schema migration and search-document indexes without merging unrelated deferred model-snapshot drift.

## 2. Projection and search implementation

- [x] 2.1 Implement version-safe Discovery projection writers and durable event handlers for Identity, Teams, Competition, and Sponsorship.
- [x] 2.2 Implement the `IDiscoveryModule` search facade against active search documents with compatible relevance, pagination, cursor, and privacy behavior.

## 3. Rebuild and API composition

- [x] 3.1 Implement idempotent persisted rebuild-job creation, execution through source-module contracts, status retrieval, and the hosted worker.
- [x] 3.2 Map public search and admin rebuild-job endpoints through Discovery, register the module in the API host, and remove the retired API-host search implementation.

## 4. Verification

- [x] 4.1 Add focused coverage for public search compatibility, projection lifecycle, duplicate/stale events, rebuild jobs, endpoint metadata, and module boundaries.
- [x] 4.2 Run the required OpenSpec, build, test, format, OpenAPI, and audit validation; resolve all high and medium touched-scope findings.

## 5. PostgreSQL search hardening

- [x] 5.1 Add projection ordering metadata and query-aligned PostgreSQL indexes; implement bounded exact/prefix/contains rank-bucket search while preserving the public cursor contract.
- [x] 5.2 Replace unbounded rebuild snapshot contracts with stable-ID keyset pages and add Discovery-owned rebuild staging persistence.
- [x] 5.3 Stage rebuild pages and atomically merge them into the live projection with source-version protection and safe failure cleanup.
- [x] 5.4 Add search pagination, staged-rebuild, migration, and version-race regression coverage; run required validation and rerun the phase PR audit.
