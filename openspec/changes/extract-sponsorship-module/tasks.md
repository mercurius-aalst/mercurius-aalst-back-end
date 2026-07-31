## 1. Sponsorship contracts and project boundaries

- [x] 1.1 Add V1 SponsorCreated, SponsorUpdated, SponsorDeleted, and GameSponsorPlacementChanged integration-event contracts without exposing EF entities.
- [x] 1.2 Add the required Sponsorship implementation project references for Media contracts and Platform eventing, without adding Competition or Media implementation references.
- [x] 1.3 Move Sponsor and GameSponsorPlacement implementation types into Sponsorship and keep all implementation-only types internal.

## 2. Shared persistence composition

- [x] 2.1 Add Sponsorship-owned EF model configuration that preserves existing table names, scalar constraints, enum conversions, indexes, and cascade relationships.
- [x] 2.2 Add an internal Sponsorship DbContext adapter and module facade that implement the existing `ISponsorshipModule` contract through no-tracking read projections and bounded batch placement lookup.
- [x] 2.3 Update the API DbContext to compose Sponsorship entity sets and model configuration while preserving the existing Competition game-to-placement relationship.
- [x] 2.4 Remove the API-host Sponsor and GameSponsorPlacement models and the legacy Sponsorship adapter after all consumers use the module implementation.

## 3. Sponsorship application and HTTP composition

- [x] 3.1 Move Sponsor create, read, update, and delete behavior and DTOs into Sponsorship, preserving validation, form binding, JSON shape, and existing delete behavior.
- [x] 3.2 Store sponsor logos through `IMediaModule` and retain the existing host Media adapter as the temporary Phase 13 bridge.
- [x] 3.3 Move the Sponsor endpoint group into `MapSponsorshipModule`, preserving routes, API versioning, tags, authorization, anonymous reads, and antiforgery metadata.
- [x] 3.4 Register Sponsorship through `AddSponsorshipModule` in the API composition root and remove obsolete legacy sponsor service registrations and endpoint mapping.
- [x] 3.5 Keep Competition's existing sponsor-placement endpoint dependent only on `ISponsorshipModule` and verify create, replace, validation, and removal behavior through the module facade.

## 4. Durable Sponsorship event publication

- [x] 4.1 Publish the matching Sponsor lifecycle event in the shared transaction for every successful Sponsor create, update, and delete mutation.
- [x] 4.2 Publish the GameSponsorPlacementChanged event in the shared transaction for placement creation, replacement, and removal.
- [x] 4.3 Ensure Sponsorship state and outbox messages use the existing shared transaction/save path without adding a module-specific outbox or consumer.

## 5. Regression and boundary coverage

- [x] 5.1 Update Sponsor feature tests for the extracted module, preserving CRUD validation, response DTO, and placement behavior coverage.
- [x] 5.2 Add focused tests for Sponsorship event payloads and transactional outbox persistence for all supported mutation paths.
- [x] 5.3 Update endpoint-contract and OpenAPI tests to prove existing sponsor and game-placement HTTP metadata and JSON contracts remain stable.
- [x] 5.4 Add composition and architecture tests proving Sponsorship implementation is internal, Competition depends only on Sponsorship contracts, and Sponsorship depends on Media contracts only.
- [x] 5.5 Retain or extend Competition performance coverage to prove batched sponsor placement enrichment does not regress to N+1 queries.

## 6. Phase completion and handoff

- [x] 6.1 Confirm no migration or EF model-snapshot change is required because the physical Sponsorship schema is unchanged.
- [x] 6.2 Run `openspec validate extract-sponsorship-module --strict`, `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` from the repository root.
- [x] 6.3 Update this task checklist, the modular-monolith progress tracker, and the phase PR description with route, authorization, DTO/JSON, database, configuration, startup/OpenAPI, validation, and known-risk outcomes.
