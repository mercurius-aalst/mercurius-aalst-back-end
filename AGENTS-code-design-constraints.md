# AGENTS code design constraints

These supplemental instructions are intended to be folded into `AGENTS.md` under a dedicated code design section.

- Keep implementations straightforward and avoid unnecessary abstractions, wrapper methods, wrapper classes, and indirection that do not add clear readability, testability, performance, or maintainability value.
- Avoid code duplication, but also avoid unnecessary code de-duplication that creates brittle shared abstractions or hides feature-specific behavior.
- Reuse existing endpoints, services, DTOs, EF Core mappings, tests, and validation patterns where possible, but verify behavior to avoid regression failures.
- Keep one primary class, record, entity, DTO, endpoint group, or service per file unless the additional type is a small private nested implementation detail.
- Apply industry-standard design patterns only where they are appropriate and necessary for code cleanliness, performance, or long-term maintainability.
- Avoid N+1 database queries, repeated service calls, unnecessary materialization, redundant method invocations, and other performance bottlenecks caused by query or code invocation patterns.
- Keep dependencies between feature domains minimal. Prefer explicit contracts at boundaries rather than cross-domain coupling or shared mutable state.
