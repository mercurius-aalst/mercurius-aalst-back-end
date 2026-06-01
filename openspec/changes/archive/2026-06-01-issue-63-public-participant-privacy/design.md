## Context

`GameEndpoints` and `TeamEndpoints` have anonymous reads, but their DTOs embed `GetUserDTO` and `GetTeamDTO`, which are also used by admin/current-user flows. `GetPlacementDTO` similarly maps full users and teams. The API needs public read models that are intentionally smaller than admin read models.

## Goals / Non-Goals

**Goals:**
- Make anonymous participant data safe by construction.
- Preserve admin/current-user functionality that legitimately needs full profile data.
- Keep response shapes stable enough for front-end navigation and bracket display.
- Add tests that fail when private fields leak into anonymous responses.

**Non-Goals:**
- Removing admin DTO fields.
- Adding broad field-level authorization middleware.
- Changing account deletion/anonymization semantics.

## Decisions

- Add one privacy-safe `PublicUserDTO` for embedded participant data.
- Reuse the existing game, placement, and team DTO graph instead of maintaining parallel public variants.
- Use username as the safe display label and keep internal user IDs for bracket and navigation resolution.
- Include Discord, Steam, and Riot IDs in `PublicUserDTO`; the website privacy policy explicitly treats these linked identifiers as public.
- Exclude invite collections from the shared team DTO. Invite workflows remain available through their dedicated authorized endpoint.
- Keep the remaining full `GetUserDTO` fields limited to authorized user-management and current-user workflows.

## Risks / Trade-offs

- Existing front-end or tests may depend on full DTOs from anonymous endpoints. The implementation should coordinate response shape changes with the redesigned front-end contract.
- Existing clients that relied on private embedded participant fields must use authorized user APIs instead.
