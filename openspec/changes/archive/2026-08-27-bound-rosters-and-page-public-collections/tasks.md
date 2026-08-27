## 1. Roster and tournament bounds

- [x] 1.1 Validate null, oversized, empty-GUID, and duplicate roster user-ID collections in both HTTP handlers before service invocation.
- [x] 1.2 Enforce the shared maximum of 50 for team-mode game sizes while preserving positive-size and individual-mode behavior.
- [x] 1.3 Add endpoint and domain tests for early roster rejection, zero service calls, valid forwarding, and team-size bounds.

## 2. Public collection paging

- [x] 2.1 Add defaulted and capped `page`/`pageSize` handling with non-positive validation to the game and team collection endpoints.
- [x] 2.2 Thread normalized paging through Competition and Teams query contracts and apply deterministic, overflow-safe database `Skip`/`Take` before batched enrichment.
- [x] 2.3 Add tests for default, custom, capped, invalid, overflow, ordering, page navigation, raw-array shape, cancellation, and no-N+1 behavior.
- [x] 2.4 Update OpenAPI assertions for the optional collection query parameters and unchanged array response schemas.

## 3. Verification

- [x] 3.1 Run focused tests, restore, build, full tests, formatting verification, strict OpenSpec validation, and the EF pending-model check.
- [x] 3.2 Audit the final diff, changed paths, migration/model impact, API contract impact, and clean worktree state.
