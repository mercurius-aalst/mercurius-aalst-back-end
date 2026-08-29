## OpenSpec-first implementation

- [x] Add lifecycle enums, request DTOs, persisted match state, and migration/model snapshot updates.
- [x] Implement domain transitions, deadline expiry, participant/captain authorization, assigned-admin policy, and transactional bracket-safe commands.
- [x] Map lifecycle state and privacy-safe metadata through the match DTO and expose versioned command routes.
- [x] Preserve and harden the administrative compatibility score route.
- [x] Process expired windows through the hosted outbox-backed deadline processor and backfill legacy completed results.

## Tests and validation

- [x] Add domain tests for confirmations, matching/differing scores, deadline expiry, forfeit, resolution, and reversal guards.
- [x] Add endpoint contract tests for route methods, authentication, admin-only commands, and anonymous projection behavior.
- [x] Run OpenSpec validation, restore, build, focused tests, full tests, and formatting checks.
