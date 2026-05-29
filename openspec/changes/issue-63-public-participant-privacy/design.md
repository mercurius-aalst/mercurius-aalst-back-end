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

- Add dedicated public DTOs instead of trying to hide fields from existing full DTOs.
- Pass caller visibility into public mapping only when a route explicitly supports authenticated enrichment.
- Use username and a safe display label as the anonymous user representation. Keep internal IDs only where bracket or match resolution requires them.
- Exclude invite collections and invite history from all public team/participant DTOs.
- Prefer projection-based queries for public list/detail paths so private user graphs are not loaded just to be discarded.

## Risks / Trade-offs

- Existing front-end or tests may depend on full DTOs from anonymous endpoints. The implementation should coordinate response shape changes with the redesigned front-end contract.
- Some public routes may need to be split from admin routes to keep authorization and DTO selection simple.
- EF includes are currently broad; tightening them may expose missing navigation assumptions in existing service tests.
