## 1. Competition ownership

- [x] 1.1 Move Competition domain entities, application services, and bracket logic into the module.
- [x] 1.2 Add the Competition DbContext port, generic adapter, and model-builder configuration.
- [x] 1.3 Register Competition services and facade through `AddCompetitionModule`.
- [x] 1.4 Make Competition implementation types internal and add architecture enforcement.

## 2. Cross-module boundaries

- [x] 2.1 Replace direct User and Team entity access with Identity and Teams contracts.
- [x] 2.2 Replace Sponsor and file implementation access with Sponsorship and Media contracts.
- [x] 2.3 Batch participant enrichment and preserve privacy-safe DTO projections.
- [x] 2.4 Replace Teams-owned roster events with Competition-owned realtime and integration events.

## 3. Persistence

- [x] 3.1 Add registration and roster historical display snapshot properties and EF configuration.
- [x] 3.2 Add a hand-authored migration with backfill SQL without changing the EF model snapshot.
- [x] 3.3 Verify table names, keys, foreign keys, indexes, and enum storage remain compatible.

## 4. Contracts and tests

- [x] 4.1 Preserve route, authorization, antiforgery, OpenAPI, and JSON-shape tests.
- [x] 4.2 Move or update domain/service tests for the new module namespaces and internal surfaces.
- [x] 4.3 Add facade, snapshot persistence, cancellation, eventing, and architecture regression tests.

## 5. Validation

- [x] 5.1 Run restore, build, tests, format verification, OpenAPI generation, and diff checks.
- [x] 5.2 Run independent performance, clean-code, and security audits and resolve all High/Medium findings.
- [x] 5.3 Document migration, configuration, startup, route, DTO/JSON, and residual risks in the PR.
