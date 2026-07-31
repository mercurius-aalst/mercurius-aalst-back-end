## Why

Competition behavior is still implemented in the API host and depends directly on Teams and
Identity EF entities. Phase 11 must establish Competition as the owner of tournament lifecycle
state while preserving every existing HTTP route and JSON contract.

## What Changes

- Move game, match, placement, tournament-registration, roster, and bracket behavior into the
  Competition module.
- Replace direct Team and User entity access with `Teams.Contracts` and `Identity.Contracts`.
- Persist roster display snapshots alongside external IDs so historical tournament data does not
  require cross-module entity navigation.
- Publish Competition-owned lifecycle events through the module eventing boundary.
- Move Competition EF configuration behind the module registration/configuration extensions.
- Add a hand-authored migration for the new snapshot columns without updating the EF model snapshot.
- Preserve existing routes, authorization metadata, validation outcomes, and public JSON shapes.

## Capabilities

### New Capabilities

- `competition-module-boundary`: Defines Competition ownership, dependency boundaries, persistence
  integration, and lifecycle event publication.

### Modified Capabilities

- `tournament-registration`: Tournament registrations and roster members persist historical user
  and team display snapshots while retaining their current API behavior.

## Impact

The Competition module, API composition root, shared EF model configuration, tournament
registration tables, module contracts, and Competition-focused tests are affected. No endpoint
route, authorization policy, request JSON, response JSON, or existing database column is removed
or renamed.
