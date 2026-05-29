## Why

Anonymous public endpoints currently reuse full user and team DTOs in game, placement, and team responses. That can expose email, names, platform IDs, deleted state, timestamps, invite data, or Auth0-linked fields to visitors who only need safe display/navigation data.

## What Changes

- Introduce privacy-safe public participant DTOs for users, teams, placements, and embedded game participants.
- Map anonymous responses to least-privileged participant data by default.
- Allow authenticated public responses to include platform identifiers only where explicitly permitted.
- Keep admin/current-user APIs on full DTOs where authorization allows them.
- Add privacy regression tests for anonymous, authenticated, and admin response shapes.

## Capabilities

### New Capabilities
- `public-participant-privacy`: Privacy-safe participant shapes for anonymous and authenticated public API responses.

### Modified Capabilities
- None.

## Impact

- `GetGameDTO`, `GetPlacementDTO`, `GetTeamDTO`, and related user/team DTO mappings.
- Anonymous routes in `GameEndpoints`, `TeamEndpoints`, and placement data returned through game completion/detail.
- Service projection/query shape in `GameService` and `TeamService`.
- Tests that assert private fields are absent from public responses.
