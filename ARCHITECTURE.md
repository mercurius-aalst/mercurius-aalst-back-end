# ARCHITECTURE — Issue #48

## Decision summary

- Keep internal user management as system of record.
- Use backend-owned OIDC flow for Google/Facebook authentication.
- Persist external links in `ExternalIdentity` with unique provider-subject identity.
- Apply explicit social-first auto-link policy with strict verification controls.

## Current-state assumptions to validate in code

1. API currently supports internal authentication and protected endpoints.
2. User domain model exists and is authoritative for profile/business data.
3. External identity support exists but current validation/flow behavior is incomplete.

## Endpoint organization rule

- External social OIDC routes under `/auth/external/*` should be defined in a dedicated endpoints file separate from `AuthEndpoints` (e.g. `ExternalAuthEndpoints.cs`).
- `AuthEndpoints` should remain focused on local/core auth concerns (login, refresh, revoke, registration/admin-local flows).

## End-to-end architecture (backend-owned OIDC)

1. Frontend requests backend social start endpoint.
2. Backend persists correlation data and redirects to provider authorize endpoint.
3. Provider redirects back to backend callback with authorization code.
4. Backend validates state/nonce, exchanges code for provider token(s), and validates token.
5. Backend resolves link/social-first policy outcome.
6. Backend issues internal auth artifact (JWT+refresh or session cookie).
7. Frontend uses backend-issued auth only.

## Required API surface

- `GET /auth/external/{provider}/start`
- `GET /auth/external/{provider}/callback`
- `GET /auth/external/{provider}/link/start`

## Token validation responsibilities (backend)

- Parse token header and select key by `kid`.
- Verify signature via provider JWKS/OIDC metadata.
- Enforce issuer allow-list.
- Enforce audience/client-id match.
- Enforce lifetime checks (`exp`, `nbf`) with bounded skew.
- Require stable subject (`sub`).
- Optional hardening: `nonce`, `azp`, domain restrictions.

## Social-first policy engine

Inputs: `Provider`, `Subject`, `Email`, `EmailVerified`, existing links, internal user lookup.

Outcomes:
- `LoginLinkedUser`
- `AutoLinkExistingUser` (verified-email + unique single user match only)
- `CreateNewUser` (if policy allows)
- `Reject`

Policy rules:
- Unverified/missing email never auto-links existing accounts.
- Internal `User.Email` is normalized and unique; duplicate matches are integrity anomalies and rejected.
- Provider `email_verified` is external evidence and does not automatically mutate internal verification state.

## Audit events

- `external_login_success`
- `external_login_failed`
- `external_link_success`
- `external_link_failed`
- `external_social_first_created`
- `external_email_verification_rejected`
- `external_email_duplicate_match_anomaly`

## Data and migration strategy

- Keep existing `ExternalIdentity` schema shape and uniqueness guarantees.
- Add targeted migrations only for required auth changes.
- Avoid broad unrelated migration diffs caused by snapshot drift; reconcile before release.

## Test strategy

### Unit tests
- Provider claim mapping/normalization.
- Token validation behavior per provider strategy.
- Social-first decision matrix.
- Error mapping/non-enumeration behavior.

### Integration tests
- Existing link login succeeds.
- Social-first verified unique-email auto-link succeeds.
- Unverified/missing email rejected.
- Duplicate email anomaly rejected + audit signal.
- Link flow uniqueness/takeover prevention.
- Existing local login/refresh/revoke behavior unaffected.

## Implementation plan

1. Split external routes into dedicated endpoints file (`ExternalAuthEndpoints`).
2. Implement backend-owned OIDC start/callback/link-start endpoints.
3. Replace token validation with provider-grade JWKS/OIDC validation strategies.
4. Implement social-first policy engine and explicit decision outcomes.
5. Implement external-auth audit events and hardened error responses.
6. Add/adjust unit and integration tests for full matrix.
7. Execute standard verification commands and document residual risks.

## Rollout notes

- Feature-flag backend-owned OIDC flow if rollout risk is high.
- Monitor login/link failure metrics and anomaly events post-release.
- Preserve backward compatibility for existing local-auth clients.
