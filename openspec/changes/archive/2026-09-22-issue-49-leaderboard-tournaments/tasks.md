## OpenSpec-first implementation

- [x] Add leaderboard bracket/metric contracts, metric-aware tournament validation, DTO projection, and persistence migration.
- [x] Add linked-user/guest participant and attempt persistence with uniqueness and concurrency safeguards.
- [x] Implement deterministic ranking and public/admin query contracts.
- [x] Implement admin-only add, correct, and remove attempt commands with authoritative metric/state/identity validation.
- [x] Integrate zero-participant start, match-free execution, ranking-derived completion/placements, immutability, and reset cleanup.
- [x] Reject registration flows for leaderboard tournaments while preserving all non-leaderboard behavior.

## Tests and validation

- [x] Cover configuration, identity, metric values/precision, ranking/ties, worse attempts, correction/removal, lifecycle, reset, authorization routes, guest placements, and public/admin projections.
- [x] Add migration/model snapshot coverage and run focused/full builds and tests.
- [x] Strictly validate OpenSpec before implementation and after task synchronization.
