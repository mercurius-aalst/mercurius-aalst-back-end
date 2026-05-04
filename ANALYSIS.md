# ANALYSIS — Issue #48: Social login providers with internal user management

## Updated direction

User management remains internal to this API. External providers (Google/Facebook) are authentication sources only.

## Goal

Enable secure Google/Facebook sign-in while preserving internal user management as source of truth and hardening login/registration flows.

## Acceptance criteria (current status)

1. Internal user management remains source of truth — **Met**.
2. Social auth via Google/Facebook — **Partially met** (provider-grade validation still needed).
3. Social identities link to one internal account — **Partially met** (explicit linking exists; complete social-first matrix pending).
4. Registration/login hardening — **Partially met**.
5. Protected endpoint authorization remains intact — **Likely met** (regression verification still required).
6. Backward compatibility for non-social users — **Met**.

## Scope

- Keep internal user management and existing user entity lifecycle.
- Implement backend-owned OIDC integration for Google/Facebook.
- Implement secure social-first login behavior (link/create/reject policy).
- Harden external auth token validation and error behavior.
- Add structured external-auth audit events.
- Add/update tests for social login/link and abuse scenarios.

## Out of scope

- Migrating user management to external IdP.
- Adding providers beyond Google/Facebook in this issue.
- Frontend UX redesign outside of required API contracts.
- Broad unrelated refactors in auth/user domains.

## Required interaction model (backend-owned OIDC)

- Backend owns OIDC responsibilities: redirect, callback, code exchange, token validation.
- Frontend initiates backend auth start endpoints and consumes backend-issued auth artifacts only.

### Register / first social sign-in flow
1. Frontend calls backend `GET /auth/external/{provider}/start`.
2. Backend generates correlation data (`state`, optional `nonce`) and redirects browser to provider.
3. Provider redirects to backend callback with authorization code.
4. Backend validates correlation data and exchanges code for provider tokens.
5. Backend validates provider token and resolves `(Provider, Subject)` link.
6. If link exists, sign in user and issue internal auth.
7. If no link exists, execute social-first policy (auto-link/create/reject) and issue internal auth only on success.

### Social-first auto-link policy
- Auto-link by email allowed only when:
  - provider token is valid,
  - provider email is present and provider-verified,
  - exactly one internal user matches normalized unique email.
- Unverified/missing email must not auto-link.
- Duplicate email matches are integrity/security anomalies (reject + audit + operator remediation).

## Verification semantics clarification

- Provider `email_verified` is external-provider evidence.
- Internal user email verification lifecycle remains governed by internal policy unless explicitly designed otherwise.

## Risks and constraints

- Incorrect provider token trust model can enable forged/invalid token acceptance.
- Account linking mistakes can create takeover risk.
- OIDC callback/correlation handling errors can create CSRF/session vulnerabilities.
- Existing clients may depend on current error semantics and flow contracts.
- Migration/snapshot drift may introduce unrelated schema churn.

## Impacted areas

- Auth endpoint routing and OIDC callback handling.
- Auth services (external login/link + policy engine).
- External token validation abstraction/strategies.
- External identity persistence and supporting constraints.
- Audit logging, validation, and error mapping layers.
- Unit/integration tests covering happy + abuse paths.

## Test strategy (analysis-level)

- Unit tests for provider claim mapping and policy decisions.
- Integration tests for:
  - linked social login success,
  - social-first verified unique-email auto-link,
  - unverified/missing email rejection,
  - duplicate email anomaly rejection,
  - external link collision/takeover prevention,
  - regression for local login + authorization behavior.

## Next step

Update/execute implementation plan in `ARCHITECTURE.md` and route implementation to `developer-senior` because of cross-cutting auth/security/data changes.
