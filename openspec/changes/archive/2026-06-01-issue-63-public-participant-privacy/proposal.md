## Why

Anonymous public endpoints currently reuse full user and team DTOs in game, placement, and team responses. That can expose email, names, deleted state, timestamps, invite data, or Auth0-linked fields to visitors who only need safe display/navigation data.

## What Changes

- Introduce one privacy-safe DTO for users embedded in games, placements, and teams.
- Map shared participant responses to least-privileged user data by default.
- Include Discord, Steam, and Riot IDs as public profile fields covered by the website privacy policy.
- Keep admin/current-user APIs on full DTOs where authorization allows them.
- Add privacy regression tests for embedded participant and authorized user response shapes.

## Capabilities

### New Capabilities
- `public-participant-privacy`: Privacy-safe participant shapes for anonymous and authenticated public API responses.

### Modified Capabilities
- None.

## Impact

- `GetGameDTO`, `GetPlacementDTO`, `GetTeamDTO`, and embedded user mappings.
- Anonymous routes in `GameEndpoints`, `TeamEndpoints`, and placement data returned through game completion/detail.
- Tests that assert private fields are absent from public responses.
