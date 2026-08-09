## Why

The API still exposes several action-style routes that obscure the resources being changed and make authorization and OpenAPI contracts harder to reason about. With the module and persistence boundaries now stable, Phase 16 can simplify those contracts in place without mixing route changes with extraction work.

## What Changes

- **BREAKING** Replace Games lifecycle action routes with one resource-state update endpoint.
- **BREAKING** Replace Teams leave, invitation-response, invitation-creation, and logo-upload action routes with member, invitation, and logo resource routes.
- **BREAKING** Replace Competition registration and roster-confirmation action routes with registration and roster-member resource routes, and clarify the proposed-roster eligibility route.
- **BREAKING** Remove the current-user profile completion compatibility route in favour of the established profile update route, and model verification-email and password-reset requests as resources.
- Preserve existing API versioning, authorization policies, response DTO JSON shapes, domain behavior, and database schema.
- Add route, authorization, OpenAPI, and removed-route contract coverage for every replacement.

## Capabilities

### New Capabilities

- `resource-oriented-api-routes`: Defines the Phase 16 in-place resource-oriented endpoint conventions and intentional removal of their action-style predecessors.

### Modified Capabilities

- `current-user-profile`: Removes the profile-completion compatibility endpoint and clarifies the canonical current-user profile update route.
- `user-owned-team-management`: Changes the externally visible routes for team memberships, invitations, and logos while preserving their authorization and lifecycle rules.
- `tournament-registration`: Changes the externally visible routes for individual and team registration, eligibility, and roster confirmation while preserving registration rules.

## Impact

- Affects endpoint mapping in the Identity, Teams, and Competition modules and any route/OpenAPI contract tests.
- Clients must use the replacement v1 routes; no `/v2` or parallel compatibility routes will be provided.
- No persistence, migration, DTO JSON-shape, module-dependency, or authorization-policy redesign is included.
