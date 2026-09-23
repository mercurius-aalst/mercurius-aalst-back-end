## Implementation

- [x] Move the admin attempt-history GET route to `/leaderboard/attempts` while preserving its admin authorization and response projection.
- [x] Update the backend route contract test to recognize the new GET route and retain admin-role checks.
- [x] Assert that the former `/leaderboard/admin` route is not exposed.
- [x] Update the front-end admin history API client to use `/leaderboard/attempts` while preserving the public ranking and finalized-placement integrations.

## Validation

- [x] Strictly validate this OpenSpec change before continuing implementation.
- [x] Run focused backend route/API tests and relevant front-end contract tests.
- [x] Strictly validate the change again after implementation and task synchronization.
