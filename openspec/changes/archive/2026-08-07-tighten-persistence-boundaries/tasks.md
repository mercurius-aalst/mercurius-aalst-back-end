## 1. Module-owned mapping configuration

- [x] 1.1 Move the Identity user mapping into `Identity.Infrastructure.UserConfiguration`.
- [x] 1.2 Replace broad Teams, Competition, Sponsorship, and Discovery mapping extensions with entity-specific module infrastructure configurations.
- [x] 1.3 Reduce `MercuriusDBContext` to module mapping composition, Platform eventing mapping, and explicit cross-module relationship composition.

## 2. Schema ownership migration

- [x] 2.1 Map legacy module tables and join tables to their owning Identity, Teams, Competition, and Sponsorship schemas while retaining Discovery and Platform schema mappings.
- [x] 2.2 Add a reversible, data-preserving PostgreSQL migration that creates module schemas and relocates existing tables into them.

## 3. Boundary and performance validation

- [x] 3.1 Verify cross-module foreign-key and delete behaviour in the EF model without module configuration importing another module entity type.
- [x] 3.2 Apply split queries to changed multi-collection read paths where needed to avoid cartesian expansion.
- [x] 3.3 Add focused architecture, migration, model, and public-contract regression coverage.

## 4. Validation

- [x] 4.1 Run restore, build, tests, format verification, and EF model/migration checks; document the accepted exception that development-database API startup/OpenAPI validation could not run because Docker/PostgreSQL was unavailable.
- [x] 4.2 Update the OpenSpec checklist and phase tracker when the phase PR is created.
