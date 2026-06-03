## Context

Teams currently have `Name`, `CaptainUserId`, `Members`, and `TeamInvites`. Anonymous team reads use `GetTeamDTO`, which is not tailored for public profile pages. Public team profiles need route lookup by team name, member usernames, and tournament links from registered games without exposing invites or private user data.

## Goals / Non-Goals

**Goals:**
- Provide a public team profile endpoint by team name.
- Return only public-safe team, captain, member, and tournament data.
- Use case-insensitive lookup with normalized team-name support.
- Keep public response ordering predictable for UI rendering.

**Non-Goals:**
- Exposing team invite data.
- Returning full user DTOs for members.
- Replacing admin team management endpoints.

## Decisions

- Add dedicated public team profile DTOs instead of adapting the admin team DTO.
- Use normalized team name lookup when the team-name normalization capability is implemented. If this change lands first, isolate the lookup behind a service helper so it can switch to the normalized column later.
- Project members to username-only objects and ignore members without valid usernames.
- Project tournaments from games where the team is registered, returning only `gameId` and name.
- Sort members and tournaments deterministically, for example by username and tournament name.

## Risks / Trade-offs

- This capability depends on team-name normalization for efficient and unambiguous routing.
- Existing anonymous team endpoints may still need separate privacy work; this endpoint should be safe regardless.
- Teams with captains missing a valid username need defined output behavior; null captain username is safer than leaking other fields.
