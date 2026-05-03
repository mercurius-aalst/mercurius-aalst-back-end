# ANALYSIS — Issue #48: Social login providers with internal user management

## Updated direction

Per latest product direction, user management remains internal to this API. The change is to add Google and Facebook as social login providers while improving security in login and registration flows.

## Goal

Enable sign-in/sign-up via Google and Facebook, map them to internally managed user accounts, and harden authentication and registration security controls.

## Acceptance criteria

1. Existing internal user management remains the source of truth for user records and profile data.
2. Users can authenticate via Google and Facebook.
3. Social identities are linked to a single internal user account model.
4. Registration and login flows are hardened with explicit security controls (see below).
5. Existing protected endpoints continue to enforce authorization correctly.
6. Backward compatibility is preserved for existing non-social login users.

## Security requirements for login/registration improvements

- Strict token validation for external providers (issuer, audience, signature, expiry, nonce/state where applicable).
- Verified-email handling policy for account creation/linking.
- Safe account-linking rules to prevent accidental merges or account takeover.
- Brute-force protection for local credential login (rate limiting/lockout policy aligned with current architecture).
- Consistent error responses that do not leak account existence.
- Audit logging for authentication and identity-link events.
- CSRF/state protections for external auth callback flows.

## Scope

- Keep internal user management and existing user entity lifecycle.
- Integrate Google and Facebook auth provider handling.
- Add/update identity-link persistence between provider identities and internal users.
- Update login/registration flows to support secure social sign-in and linking.
- Add/update validation and security checks in auth endpoints/services.
- Add tests for happy paths and security edge cases.

## Out of scope

- Migrating user management to external IdP.
- Adding providers beyond Google/Facebook in this issue.
- Frontend-specific UX redesign beyond required API contract changes.


## Additional requirements from review

- Prefer non-sequential public user identifiers (GUID/UUID) for externally exposed user references to reduce enumeration risk.
- Define clear lifecycle for `PasswordHash`/`PasswordSalt` when a user is created through social-first registration.

## Risks and constraints

- Account linking mistakes could create duplicate users or takeover risk.
- Provider claim differences and missing verified-email claims can complicate linking.
- Callback/state handling errors can introduce auth vulnerabilities.
- Existing clients may depend on current response shapes and error semantics.

## Impacted areas (expected)

- ASP.NET Core authentication configuration.
- Auth controller/service login and registration workflows.
- User and external-identity persistence model.
- Validation, rate-limiting/lockout hooks, and auth audit logging.
- Integration/unit tests for auth flows and security cases.

## Next step in delivery flow

Create `ARCHITECTURE.md` for implementation design (provider integration, identity-link model, secure flow updates, migration/backward-compat plan, and test matrix), then route to `developer-senior` due to cross-cutting auth/security/data changes.
