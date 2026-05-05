# Implementation TODO — Issue #48 External Auth + Module Extraction

> Last updated: 2026-05-05
> 
> This plan re-aligns implementation with the previously agreed analysis/architecture direction:
> - API remains source of truth for users/roles/permissions.
> - External auth via OIDC (Google first), API-managed flow.
> - Internal JWT + refresh tokens remain in use.
> - Move auth endpoints + auth DI registration into `Auth.Module`.
> - Keep user/profile data in `User`; keep `ExternalIdentity` minimal for linking/auth.

---

## Current validation of what is already implemented

### Completed
- [x] Created `src/Auth.Module` project and added to solution.
- [x] Created `src/Mercurius.Shared` project and added to solution.
- [x] API references both projects.
- [x] Added initial `AddAuthServices(...)` and `MapAuthEndpoints(...)` extension stubs.
- [x] Added project readme docs for module boundaries.

### Not completed yet
- [ ] Auth endpoint mapping is still in API project.
- [ ] Auth DI registration is still in API project.
- [x] User/auth entities extracted to shared library and wired in API/Module.
- [ ] `ExternalIdentity` model/migration not implemented.
- [ ] OIDC external flow not implemented.
- [ ] Link/unlink provider flows not implemented.

---

## Phase 0 — Baseline and safety rails

### Goal
Lock current behavior before move/refactor.

### Steps
1. [x] Add/expand tests around existing local auth endpoints (`register/login/refresh/revoke`) as regression baseline.
2. [x] Document current endpoint contracts and expected auth responses.
3. [x] Confirm build/test/format are green before extraction step.

### Exit criteria
- Existing auth behavior has regression coverage and can be validated quickly.

---

## Phase 1 — Move local auth mapping + DI registration into Auth.Module (no behavior changes)

### Goal
Physically relocate auth composition responsibility from API host into module.

### Steps
1. [x] Move local auth endpoint mapping implementation to `Auth.Module`.
2. [x] Move auth-related DI registrations to `Auth.Module` (`IAuthService`, decorators, token service, login attempts, etc.).
3. [x] Update API host startup to call module entrypoints instead of API-local auth mapping/registration.
4. [x] Remove obsolete API-local auth composition code.
5. [x] Run regression tests and compare runtime behavior.

### Exit criteria
- No contract changes; endpoint routes and behavior remain identical.

---

## Phase 2 — Shared model extraction for cohesive auth/user domain

### Goal
Extract cohesive auth/user entities into `Mercurius.Shared` for reuse by API + module.

### Steps
1. [x] Move `User`, `Role`, `RefreshToken` (and required supporting types/configurations) into shared project.
2. [x] Update namespaces/references and DbContext mappings.
3. [x] Keep migrations consistent; avoid unintended schema changes.
4. [x] Validate compile/runtime and tests.

### Exit criteria
- [x] Auth.Module and API depend on shared entity definitions cleanly.

---

## Phase 3 — ExternalIdentity persistence and linking primitives

### Goal
Introduce minimal external identity model for auth/linking.

### Steps
1. [ ] Add `ExternalIdentity` entity with minimal fields:
   - Provider
   - ProviderSubject
   - Email
   - EmailVerified
   - LinkedAtUtc
   - LastLoginAtUtc
2. [ ] Add unique constraint on `(Provider, ProviderSubject)` and index on `UserId`.
3. [ ] Add migration + tests for uniqueness/linking constraints.

### Exit criteria
- Schema supports safe external identity linking with enforced uniqueness.

---

## Phase 4 — Google OIDC login (API-managed) + internal token issuance

### Goal
Implement external authentication via API-managed OIDC flow.

### Steps
1. [ ] Implement auth start endpoint (`state`, `nonce`, `pkce` generation + storage).
2. [ ] Implement callback endpoint (code exchange, token validation, claim extraction).
3. [ ] Apply linking policy:
   1. provider subject match,
   2. verified-email fallback,
   3. deterministic create/fail behavior.
4. [ ] Issue internal JWT + refresh token after successful external auth.
5. [ ] Add integration tests for success/failure paths.

### Exit criteria
- Users can authenticate via Google and receive internal tokens.

---

## Phase 5 — Link/Unlink provider flows

### Goal
Support account linking lifecycle and prevent account duplication.

### Steps
1. [ ] Implement provider link start/callback for authenticated users.
2. [ ] Enforce one external account -> one internal user.
3. [ ] Implement unlink with safety guard (cannot remove last viable login method).
4. [ ] Add tests for duplicate link rejection and unlink guardrails.

### Exit criteria
- Multi-provider linking works and enforces ownership/security rules.

---

## Phase 6 — Hardening, observability, and release readiness

### Goal
Finalize security/operational readiness.

### Steps
1. [ ] Add structured audit logs for login/link/unlink outcomes.
2. [ ] Verify redirect allow-list, state/nonce expiration, token validation edge cases.
3. [ ] Review config/secrets handling for provider credentials.
4. [ ] Run full verification:
   - `dotnet restore`
   - `dotnet build --no-restore`
   - `dotnet test --no-build`
   - `dotnet format --verify-no-changes`
5. [ ] Update implementation notes and review checklist.

### Exit criteria
- Change is production-ready with documented risks and mitigations.

---

## Tracking log
- 2026-05-05: TODO plan created; baseline scaffolding validated; implementation phases defined.
- 2026-05-05: Added local auth endpoint mapping regression tests (routes + anonymous/admin metadata).
- 2026-05-05: Added auth endpoint contract baseline document and completed Phase 0 checklist.
- 2026-05-05: Moved local auth endpoint mapping to Auth.Module and moved auth contract DTOs/interface needed by mapping into Mercurius.Shared.
- 2026-05-05: Moved auth DI registration to Auth.Module, wired Program to AddAuthServices(), and validated regression tests/build.

- 2026-05-05: Phase 2 checklist completed (shared User/Role/RefreshToken, IAuthDbContext abstraction, auth services + DI moved to Auth.Module).
