## Why

The two remaining unbounded administrative collection APIs can materialize an entire user or tournament-registration set in one request. They need compatible page bounds so administrative operations remain predictable as data grows.

## What Changes

- Add optional `page` and `pageSize` query parameters to the existing no-query admin user list and admin tournament-registration list.
- Default to page 1 and 20 items, reject non-positive values before any service invocation, and cap positive page sizes at 50.
- Return the existing raw JSON arrays, routes, authorization, and item shapes; do not add totals, envelopes, or route versions.
- Keep cursor-based user search exactly as it is whenever the `query` key is present, including its current page-size behavior; ignore `page` in that mode.
- Apply deterministic ordering and overflow-safe database-level paging before user or registration DTO enrichment.

## Capabilities

### New Capabilities

- `admin-collection-paging`: Bounded and deterministic paging for the existing administrative user and tournament-registration collection APIs while preserving raw-array response contracts.

### Modified Capabilities

- `tournament-registration`: Bound the existing admin registration-list operation without changing its route, authorization, or registration response data.

## Impact

- API contracts: `GET /v1/lan/users` without a `query` key and `GET /v1/lan/games/{gameId}/registrations/admin` gain optional paging query parameters.
- Modules: Identity user endpoint/service decorators and Competition registration endpoint/service/read model contracts.
- Tests and OpenAPI: validation short-circuiting, default/custom/capped/overflow pages, deterministic ordering, raw-array schemas, and cancellation propagation.
- Persistence: no schema, migration, or data rewrite.
