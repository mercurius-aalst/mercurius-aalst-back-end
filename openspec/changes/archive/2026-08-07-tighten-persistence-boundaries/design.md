## Context

The API has one physical `MercuriusDBContext`. Its module configurations are already called from the context, but the host still owns `User` mapping and module mapping is grouped into broad `ModelBuilder` extension classes. Legacy business tables remain in the default schema, while Discovery and Platform already use their own schemas. Several foreign keys are intentionally cross-module, including team membership and competition participant references.

The migration must preserve live data and the public HTTP contract. It must also avoid combining this persistence change with route, authorization, DTO, or multi-DbContext work.

## Goals / Non-Goals

**Goals:**

- Give each module ownership of its EF configuration classes and make the host DbContext a composition root for those configurations.
- Retain one physical DbContext and all existing query/service behaviour.
- Move module-owned tables into PostgreSQL schemas with a reversible, data-preserving migration.
- Represent cross-module persistence relationships as explicit foreign-key boundaries without exposing new cross-module data through public contracts.
- Guard the resulting model with focused tests for schema, tables, indexes, and cross-module delete behaviour.

**Non-Goals:**

- Splitting the database into module-specific DbContexts or databases.
- Changing API routes, authorization, request/response shapes, or discovery behaviour.
- Renaming database columns, adding speculative indexes, or changing data-retention policy.
- Eliminating every cross-module foreign key. Referential integrity remains required for currently related records.

## Decisions

### Module-owned entity configuration classes

Each module will define `IEntityTypeConfiguration<T>` classes in its `Infrastructure` area. A module-level composition method will apply its own configurations, and `MercuriusDBContext.OnModelCreating` will only compose module mappings plus Platform eventing and unavoidable cross-module FK configuration.

This replaces broad mapping extension implementations with named configuration classes, which makes the persistence owner of each table explicit. Using `ApplyConfigurationsFromAssembly` was considered, but explicit module composition is chosen because the host must remain deterministic and testable without assembly-scanning side effects.

### One DbContext with module schemas

`MercuriusDBContext` remains the sole EF context. Existing default-schema tables move to `identity`, `teams`, `competition`, and `sponsorship`; Discovery and Platform retain `discovery` and `platform`. Tables use lower-case, module-local names so ownership is visible in the physical database. Existing columns, indexes, and constraints are retained.

Many DbContexts were rejected for this phase because their transaction, migration, and service registration changes would obscure the persistence-boundary refactor and amplify deployment risk.

### Explicit cross-module foreign-key boundary

Module configuration owns relationships among entities in its own module. The host persistence composition configures existing foreign keys that span module-owned types using relationship metadata, without adding new navigations or sharing a module DbContext interface. Cross-module records continue to store only IDs; the database keeps the existing referential constraints and delete behaviours.

Removing all object navigations and rewriting every service in this phase was rejected: those navigations currently support established query and domain flows, and wholesale removal would combine a behavioural rewrite with a physical migration. The migration removes cross-module relationship configuration from module-owned mappings and makes the host-owned boundary explicit; further domain-level navigation removal can be performed after transitional persistence adapters are removed.

### SQL table relocation migration

The migration will create schemas and use PostgreSQL `ALTER TABLE ... SET SCHEMA` operations. This moves existing tables, indexes, constraints, and data atomically without a copy/delete cycle. The migration will be hand-authored because the EF model snapshot has known unrelated drift from earlier hand-authored migrations; it will not attempt to rebaseline unrelated history.

The reverse migration moves each table back to `public` and drops only empty module schemas. Deployment must be performed against a backup-capable database in a transaction as usual for EF migrations.

## Risks / Trade-offs

- [Schema move locks tables briefly] → Run migration during a maintenance window and validate against a restored test database before production deployment.
- [Existing model snapshot drift] → Keep the migration scoped to table relocation, validate the generated runtime model separately, and do not rewrite unrelated snapshot history.
- [Cross-schema FK naming or delete behaviour regression] → Preserve constraints through `SET SCHEMA` and assert the EF model’s foreign keys and delete behaviours in tests.
- [Accidental public contract change while changing entity mapping] → Keep endpoint and DTO code untouched and run the existing route, OpenAPI, serialization, and privacy test suites.

## Migration Plan

1. Deploy code and the migration together after a database backup.
2. Apply the migration, which creates schemas and relocates module tables without altering data, columns, indexes, or constraints.
3. Run API startup, OpenAPI generation, and contract smoke tests against the migrated database.
4. If a rollback is required before subsequent writes depend on the new schema locations, apply the down migration to return tables to `public`.

## Open Questions

- None. The phase deliberately defers the already-known EF model snapshot rebaseline to the final migration cleanup rather than mixing it with table ownership relocation.
