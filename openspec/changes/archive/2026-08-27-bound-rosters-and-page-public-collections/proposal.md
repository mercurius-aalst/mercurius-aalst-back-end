## Why

Tournament roster requests can currently trigger identity, team, and database work with an arbitrarily large list of user IDs, while the anonymous game and team collection endpoints materialize every active row. These public and authenticated request paths need explicit, compatible bounds before their cost can grow with uncontrolled input or data volume.

## What Changes

- Reject roster eligibility and roster submission payloads containing more than 50 user IDs, empty GUIDs, duplicate IDs, or a missing `userIds` collection before invoking any application service, module, transaction, or database work.
- Limit team-tournament configuration to a maximum team size of 50 so every valid configured roster remains submit-able.
- Add optional `page` and `pageSize` query parameters to the existing anonymous game and team collection routes, defaulting to page 1 and 20 items and capping a positive page size at 50.
- Apply deterministic database ordering before overflow-safe `Skip`/`Take` paging while preserving cancellation propagation and batched cross-module lookups.
- Preserve routes, item JSON shapes, and the existing raw JSON array response; the minimal paging contract does not add a total-count or pagination envelope.
- **BREAKING**: Invalid roster IDs, roster arrays above 50, team sizes above 50, and non-positive collection paging parameters are rejected; collection requests without paging parameters return only the first 20 items instead of every item.

## Capabilities

### New Capabilities

- `public-collection-paging`: Bounded, fully navigable, deterministically ordered paging for the existing public game and team collection routes without changing their raw-array JSON shape.

### Modified Capabilities

- `tournament-registration`: Bound and validate roster payloads before downstream work and cap configured team-tournament sizes at the same supported maximum.

## Impact

- API contracts: `GET /v1/lan/games`, `GET /v1/lan/teams`, roster eligibility, roster submission, and admin game create/update validation.
- Modules: Competition endpoint/query/domain services and Teams endpoint/query adapter services.
- Tests and OpenAPI: endpoint short-circuit behavior, query paging/order/batching, JSON-array preservation, team-size validation, and documented query parameters.
- Persistence: no schema or migration change; existing game rows are not rewritten.
