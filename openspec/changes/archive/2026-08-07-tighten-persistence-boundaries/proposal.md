## Why

Module extraction has separated application behaviour, but the host DbContext still contains Identity mapping and owns the composition of every module's persistence mapping. Several module mappings also use EF relationships to entities owned by other modules. This keeps the physical persistence model coupled even though the application boundaries are now explicit.

Phase 15 makes persistence ownership match the existing module boundaries while preserving the current database data, routes, authorization, and JSON contracts.

## What Changes

- Move Identity, Teams, Competition, Sponsorship, and Discovery EF mappings into entity-specific configuration classes owned by their modules.
- Keep one physical `MercuriusDBContext`, but make it compose the module-owned configuration entry points instead of defining entity mapping details itself.
- Map module-owned tables to the existing module schemas and add a migration that moves the legacy tables and join tables into the corresponding schemas without changing their columns, indexes, or constraints.
- Replace cross-module EF navigation mappings with ID-based references where they are not needed inside the owning module; preserve required cross-module database referential constraints.
- Add focused architecture and persistence-model tests that guard configuration ownership, schema mapping, foreign-key delete behaviours, and unchanged public API contracts.

## Capabilities

### New Capabilities

- `module-persistence-boundaries`: Module-owned EF mapping, schema ownership, and ID-based cross-module persistence boundaries.

### Modified Capabilities

- None.

## Impact

- Affects `MercuriusDBContext`, module infrastructure mappings and domain relationship representations, the EF migration history, and persistence/architecture tests.
- The physical PostgreSQL database receives schema/table relocation only; existing routes, request/response JSON, authorization policies, and external integrations are unchanged.
