## Why

Game, Sponsor, and Team image mutations can leave newly stored files orphaned when persistence fails, while replaced or deleted records leave their former files indefinitely. Team logos also serve as historical tournament-registration snapshots, so cleanup must preserve those references while respecting the database and durable-event commit boundaries.

## What Changes

- Compensate newly stored Game, Sponsor, and Team images when the following mutation or commit fails or is cancelled, using non-cancelled best-effort cleanup that preserves the original failure.
- Retire replaced or deleted owned images only after the related database and durable-event transaction commits.
- Keep committed mutations successful when post-commit file deletion fails, while logging the residual orphan for operations.
- Protect Team logos referenced by any tournament-registration snapshot through one bounded Competition read contract.
- Continue treating blank, unchanged, external, default, traversal, and arbitrary file references as non-deletable.
- Retain synchronous idempotent deletion; do not introduce cleanup outbox events, ownership tables, a reaper, replay UI, schema changes, or last-reference reclamation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `media-storage-boundary`: Define compensating and post-commit image cleanup, safe no-op references, and best-effort residual behavior.
- `user-owned-team-management`: Preserve tournament-registration logo snapshots while retiring replaced, removed, or soft-deleted Team logos after commit.
- `competition-module-boundary`: Expose one bounded historical Team-logo reference query through the existing Teams contract boundary.
- `sponsorship-module-boundary`: Compensate failed Sponsor logo saves and retire replaced or deleted logos only after the Sponsorship state and event commit.

## Impact

- Affects Media file deletion safety tests; Game, Sponsor, and Team mutation services; the Team durable-event decorator; the existing Teams-to-Competition read contract; focused module tests; and dependency-injection constructor wiring.
- Public routes, authorization, DTOs, JSON shapes, durable business events, SignalR behavior, database schema, migrations, and configuration remain unchanged.
- Because database and filesystem operations cannot commit atomically, process termination or persistent storage failure can still leave a manually recoverable orphan.
