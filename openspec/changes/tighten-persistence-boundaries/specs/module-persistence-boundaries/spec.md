## ADDED Requirements

### Requirement: Module-owned EF configuration
The system MUST define entity mapping configuration in the infrastructure area of the module that owns the entity. The physical `MercuriusDBContext` MUST compose those module-owned configurations and MUST NOT define module entity mapping details itself.

#### Scenario: Building the runtime model
- **WHEN** the API creates `MercuriusDBContext`
- **THEN** the EF model contains the configurations supplied by Identity, Teams, Competition, Sponsorship, Discovery, and Platform composition

### Requirement: Schema-aligned persistence ownership
The system MUST map Identity, Teams, Competition, Sponsorship, Discovery, and Platform tables to their respective PostgreSQL schemas. The migration moving legacy module tables to those schemas MUST preserve existing data, columns, indexes, and foreign-key constraints.

#### Scenario: Applying the persistence-boundary migration
- **WHEN** the migration is applied to a database containing legacy default-schema module tables
- **THEN** each module table is available in its owning schema with its existing data and constraints preserved

### Requirement: Explicit cross-module persistence references
The system MUST configure relationships between entities owned by different modules at the persistence-composition boundary. Module-owned configuration MUST NOT introduce a dependency on another module's entity type for a cross-module relationship.

#### Scenario: Validating a cross-module relationship
- **WHEN** the runtime EF model is inspected for a relationship from a module-owned dependent to an entity owned by another module
- **THEN** the relationship retains its existing foreign-key and delete behaviour while the owning module configuration remains independent of the other module entity type

### Requirement: Public contract preservation
The persistence-boundary refactor MUST preserve the existing API routes, authorization requirements, request and response JSON shapes, and public-search behaviour.

#### Scenario: Exercising an existing public API contract
- **WHEN** an existing API route is invoked after the persistence-boundary migration
- **THEN** it returns the same route, authorization outcome, and JSON contract as before the migration
