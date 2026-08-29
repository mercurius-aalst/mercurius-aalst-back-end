## OpenSpec-first implementation

- [x] Add lifecycle enums, request DTOs, persisted match state, and migration/model snapshot updates.
- [x] Implement domain transitions, deadline expiry, participant/captain authorization, assignment-scoped notification/privacy behavior, global-admin administrative actions, and transactional bracket-safe commands.
- [x] Map lifecycle state and privacy-safe metadata through the match DTO and expose versioned command routes.
- [x] Redact private reports in public/nested projections, scope protected reports by participant or assigned-admin policy, and keep match reads targeted.
- [x] Return authoritative resolve, administrative-forfeit, and reversal capabilities with stable blocked reasons.
- [x] Preserve and harden the administrative compatibility score route.
- [x] Process expired windows through the hosted outbox-backed deadline processor and backfill legacy completed results.
- [x] Persist resolution-required notifications for the typed assigned-admin/global-admin recipient contract with stable event-consumer idempotency.

## Tests and validation

- [x] Add domain tests for confirmations, matching/differing scores, deadline expiry, forfeit, resolution, and reversal guards.
- [x] Add endpoint contract tests for route methods, authentication, admin-only commands, and anonymous projection behavior.
- [x] Add privacy mapper/serialization and large-bracket targeted-query regressions.
- [x] Remove assignment-based command denial: assigned admins remain the primary notification/private-report recipient while any authenticated global admin may perform eligible administrative actions.
- [x] Verify assigned and unassigned global-admin resolution records the actual resolver identity and server UTC time.
- [x] Verify durable assigned-admin notification delivery, global-admin fallback routing, and duplicate event retry behavior.
- [x] Run OpenSpec validation, restore, build, focused tests, full tests, and formatting checks.
