## 1. Media Lifecycle Implementation

- [x] 1.1 Add logged, non-cancelled compensation and post-commit retirement to Game image create, replace, and delete flows.
- [x] 1.2 Add logged, non-cancelled compensation and post-commit retirement around Sponsor state and durable-event commits.
- [x] 1.3 Add the bounded historical Team-logo query contract and Competition implementation.
- [x] 1.4 Add fail-safe current/historical reference protection, compensation, and post-commit retirement to Team logo replace, remove, and soft-delete flows without changing SignalR ordering.

## 2. Focused Regression Coverage

- [x] 2.1 Cover Media no-op deletion for external, default/static, traversal, arbitrary, blank, and idempotent generated references.
- [x] 2.2 Cover Game and Sponsor compensation, commit ordering, old/current preservation on failure, and non-fatal logged post-commit deletion failures.
- [x] 2.3 Cover Team compensation, unchanged/current/historical retention, reference-query failure, and soft-delete commit/SignalR/cleanup ordering.

## 3. Validation And Contract Audit

- [x] 3.1 Run focused tests for Media, Competition, Sponsorship, Teams, and Platform eventing/realtime behavior.
- [x] 3.2 Run restore, full build/test, format verification, strict OpenSpec validation, and EF pending-model checks.
- [x] 3.3 Confirm no route, authorization, DTO, JSON, schema, migration, durable-business-event, or SignalR contract drift and audit the final diff/status.
