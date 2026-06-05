## Why

Teams are currently admin-managed, which prevents players from forming and maintaining their own teams without staff intervention. The front end needs authenticated self-service team ownership while the API continues to enforce captain limits, roster safety, invite anti-spam rules, and upload security server-side.

## What Changes

- Add authenticated team creation where the creator automatically becomes the captain.
- Enforce that a user can captain at most two teams.
- Add captain-managed team invitations with pending, accepted, declined, and cancelled states.
- Prevent duplicate pending invitations and repeated invite spam after declines.
- Allow invited users to accept or decline only their own invitations.
- Allow team members to leave teams only when tournament roster rules allow it.
- Allow captains to transfer captainship to another current team member while preserving exactly one captain.
- Allow captains to upload, replace, and remove an optional team logo with server-side validation and safe serving semantics.
- Expose current-user team and invite data needed by the front end for captained teams, member teams, received invites, and sent pending invites.
- Publish real-time team events for invite, membership, and captain transfer changes so the Blazor front end can update without polling.
- Add invite expiration and retention rules so stale invites are not kept indefinitely.
- Remove admin-only team management endpoints in favor of authenticated user-owned team management flows.

## Capabilities

### New Capabilities

- `user-owned-team-management`: Authenticated user team ownership, membership, invites, captain transfer, leave rules, current-user team/invite projections, and logo management.

### Modified Capabilities

- `team-name-normalization`: User-created and captain-managed team names must follow existing validation, normalization, and uniqueness requirements.
- `public-team-profile`: Public team profiles must remain privacy-safe while optionally exposing a safe public logo reference.

## Impact

- Minimal API endpoints under `src/MercuriusAPI/Endpoints/` for authenticated team creation, membership, invites, captain transfer, logo management, and current-user team/invite projections.
- DTOs under `src/MercuriusAPI/DTOs/` for team ownership, invite state, current-user team summaries, and upload responses.
- Services and validation under `src/MercuriusAPI/Services/` for captain authorization, membership transitions, invite anti-spam, invite retention, roster blocking checks, and logo storage safety.
- Entity Framework models, `MercuriusDBContext`, and migrations for team membership/invite state, captain ownership, logo metadata, and supporting indexes/constraints.
- SignalR hub or equivalent ASP.NET Core real-time endpoint for invite, membership, and captain transfer notifications.
- Tests under `tests/MercuriusAPI.Tests/` covering authorization boundaries, validation, privacy, invite state transitions, invite retention, real-time event publication, roster leave rules, captain transfer, logo upload validation, and efficient projections.
- No new external package is expected unless existing ASP.NET Core and EF Core APIs cannot satisfy safe file validation/storage requirements.
