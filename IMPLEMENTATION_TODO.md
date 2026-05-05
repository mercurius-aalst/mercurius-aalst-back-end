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
- [x] Auth endpoint mapping is in `Auth.Module`.
- [x] Auth DI registration is in `Auth.Module`.
- [x] Auth user lifecycle is exposed via `IAuthUserService`.
- [x] User/profile CRUD now lives in `MercuriusAPI`.
- [x] `Auth.Module` no longer contains profile DTOs/endpoints/store abstractions.
- [x] `ExternalIdentity` model/migration implemented.
- [x] Google OIDC external flow implemented.
- [x] Link/unlink provider flows implemented.

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

## Phase 2.5 — Complete auth/business separation

### Goal
Make `Auth.Module` auth-only and move business/profile concerns back to the API host.

### Steps
1. [x] Remove duplicate/stale auth DTOs from `Mercurius.Shared`.
2. [x] Extract `IAuthUserService` in `Auth.Module` for auth-user lifecycle operations.
3. [x] Move profile DTOs, user service, validation, and user endpoint mapping to `MercuriusAPI`.
4. [x] Remove profile DTOs/endpoints/store abstractions from `Auth.Module`.
5. [x] Run build/test/format verification after the split.

### Exit criteria
- [x] `Auth.Module` contains only auth concerns.
- [x] `MercuriusAPI` owns user/profile CRUD.

---

## Phase 3 — ExternalIdentity persistence and linking primitives

### Goal
Introduce minimal external identity model for auth/linking.

### Steps
1. [x] Add `ExternalIdentity` entity with minimal fields:
   - Provider
   - ProviderSubject
   - Email
   - EmailVerified
   - LinkedAtUtc
   - LastLoginAtUtc
2. [x] Add unique constraint on `(Provider, ProviderSubject)` and index on `UserId`.
3. [x] Add migration + tests for uniqueness/linking constraints.

### Exit criteria
- Schema supports safe external identity linking with enforced uniqueness.

---

## Phase 4 — Google OIDC login (API-managed) + internal token issuance

### Goal
Implement external authentication via API-managed OIDC flow.

### Steps
1. [x] Implement auth start endpoint (`state`, `nonce`, `pkce` generation + storage).
2. [x] Implement callback endpoint (code exchange, token validation, claim extraction).
3. [x] Apply linking policy:
   1. provider subject match,
   2. verified-email fallback,
   3. auto-create minimal profile when no match exists.
4. [x] Issue internal JWT + refresh token after successful external auth.
5. [ ] Add deeper success/failure path tests beyond endpoint/state model coverage.

### Exit criteria
- Users can authenticate via Google and receive internal tokens.

---

## Phase 5 — Link/Unlink provider flows

### Goal
Support account linking lifecycle and prevent account duplication.

### Steps
1. [x] Implement provider link start/callback for authenticated users.
2. [x] Enforce one external account -> one internal user.
3. [x] Implement unlink with safety guard (cannot remove last viable login method).
4. [ ] Add tests for duplicate link rejection and unlink guardrails.

### Exit criteria
- Multi-provider linking works and enforces ownership/security rules.

---

## Phase 6 — Hardening, observability, and release readiness

### Goal
Finalize security/operational readiness.

### Steps
1. [x] Add structured audit logs for login/link/unlink outcomes.
2. [x] Verify state/nonce expiration and token validation edge cases. Redirect handling uses a fixed configured Google redirect URI rather than caller-supplied redirects.
3. [x] Review config/secrets handling for provider credentials.
4. [x] Run full verification:
   - `dotnet restore`
   - `dotnet build --no-restore`
   - `dotnet test --no-build`
   - `dotnet format --verify-no-changes`
5. [x] Update implementation notes and review checklist.

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
- 2026-05-05: Phase 2.5A completed (removed duplicate `Mercurius.Shared` auth DTOs, fixed stale references, and aligned exception handling/tests).
- 2026-05-05: Phase 2.5B completed (extracted `IAuthUserService` and auth-only user lifecycle logic inside `Auth.Module`).
- 2026-05-05: Phase 2.5C completed (moved user profile DTOs/services/endpoints to `MercuriusAPI` and removed profile concerns from `Auth.Module`).
- 2026-05-05: Phase 3 completed (`ExternalIdentity` entity, EF mapping, migration, and model tests added).
- 2026-05-05: Phase 4/5 implemented (Google OIDC login start/callback, auto-provisioning minimal profiles, provider link/unlink endpoints, and last-sign-in-method guard).
