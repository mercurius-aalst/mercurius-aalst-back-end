# DEVELOPMENT NOTES — Issue #48

## Implemented state (observed)

- External identity table model and uniqueness constraints are in place.
- External auth endpoints and service methods for external login/link are wired and reachable.
- Local login lockout and refresh-token lifecycle logic remain intact.

## Critical findings retained from implementation review

1. External token validation model is not production-grade.
   - Current implementation relies on locally configured symmetric signing key validation.
   - Required target is provider-grade validation (JWKS/OIDC metadata, issuer/audience/lifetime enforcement).
2. Social-first continuation path is incomplete.
   - Current external login path rejects when no link exists instead of deterministic create/link/reject policy.
3. External-auth audit telemetry is incomplete.
   - Structured events for success/failure/decision branches need implementation.
4. Security-hardening is partial.
   - Local lockout is in place; external-auth takeover/collision protections need full policy + tests.

## Implementation gaps against updated architecture

1. Backend-owned OIDC start/callback flow is not yet implemented as canonical path.
2. Provider-grade token validation is not yet implemented.
3. Social-first policy engine matrix (link/create/reject) is incomplete.
4. Email verification decision policy needs explicit implementation (provider evidence vs internal verification lifecycle).
5. External-auth structured audit events are incomplete.

## Required implementation flow updates

### External login (backend-owned OIDC)
1. Implement `GET /auth/external/{provider}/start` and `GET /auth/external/{provider}/callback`.
2. Perform code exchange server-side.
3. Validate provider token server-side (JWKS/OIDC, issuer, audience, lifetime, subject).
4. Resolve existing external link or run social-first policy.
5. Issue internal auth artifact only on successful outcome.

### Social-first and email-based auto-link safeguards
- Allow auto-link only for verified provider email + exact unique internal email match.
- Reject unverified/missing email for auto-link.
- Treat duplicate email matches as integrity anomaly (reject + audit + operator remediation).
- Do not implicitly mutate internal email verification state from provider claim alone.

### External link
1. Require active backend-authenticated internal user context.
2. Implement `GET /auth/external/{provider}/link/start` with link intent binding.
3. On callback, exchange code, validate provider token, enforce uniqueness/non-takeover.
4. Create link or no-op when already linked to same user.

## Priority-ordered remediation list

1. Replace provider validation implementation with provider-appropriate trust checks.
2. Implement social-first decision flow with explicit policy outcomes.
3. Add structured audit logging for all external auth outcomes.
4. Add tests for collision/takeover/unverified/missing email/duplicate anomaly flows.
5. Validate and trim migration scope.

## Definition of Done (next pass)

- All acceptance criteria in `ANALYSIS.md` are fully met.
- Backend-owned OIDC flow implemented as canonical path.
- External auth tests cover both happy paths and abuse paths.
- Review-ready notes include explicit threat-model assumptions and failure behavior.

## Verification

- Keep running: `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build`, `dotnet format --verify-no-changes`.
