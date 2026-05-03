# ARCHITECTURE — Issue #48

## Decision summary

- Keep internal user management as system of record.
- Add Google and Facebook as external social identity providers for authentication only.
- Persist social identity links to internal users using a dedicated external-identity mapping model.
- Harden login/registration flows with explicit security controls and auditability.
- Route implementation to `developer-senior` due to cross-cutting auth, data, security, and migration impact.

## Current-state assumptions to validate in code

1. API currently supports internal authentication and protected endpoints.
2. User domain model exists and is authoritative for profile/business data.
3. No complete external-identity link model currently exists, or it is incomplete for multi-provider use.

## Target architecture

## Identifier strategy (user ids)

- Use GUID/UUID for externally exposed user identifiers (API route ids, DTO references, tokens where applicable).
- If internal PKs are currently integers, prefer one of:
  1. Full migration to GUID PKs for user-related aggregates, or
  2. Keep internal int PK and introduce immutable public `UserPublicId` (GUID) with unique index, using GUID for all external references.
- Recommendation: **Option 2** for lower migration risk unless there is strong reason for full PK migration now.

Security rationale:
- Non-sequential identifiers reduce trivial enumeration and guessing of neighboring user ids.
- This is defense-in-depth and must be combined with authorization checks on every resource access.

### 1) Authentication boundary

- External providers (Google/Facebook) authenticate end users.
- API validates provider tokens and converts provider identity into an internal user principal.
- Authorization inside API remains unchanged (role/claims/policies from internal model).

### 2) Identity-link model

Add a persistence model equivalent to:

- `ExternalIdentity`
  - `Id`
  - `UserId` (FK to internal user)
  - `Provider` (Google/Facebook enum/string)
  - `ProviderSubject` (stable provider user id)
  - `EmailAtLinkTime`
  - `EmailVerifiedAtLinkTime` (bool)
  - `CreatedAtUtc`
  - `LastLoginAtUtc`
  - unique index: (`Provider`, `ProviderSubject`)
  - optional unique guard for trusted linking policy (e.g., verified email constraints as applicable)

Rationale:
- Supports one internal user linked to many providers.
- Prevents duplicate mapping of same provider identity.

### 3) Login/registration/link flows

#### A. Social sign-in (existing user via existing link)
1. Receive social token/assertion.
2. Validate token cryptographically and semantically (issuer/audience/exp).
3. Resolve (`Provider`, `ProviderSubject`) in `ExternalIdentity`.
4. If found, authenticate as linked internal user.
5. Update `LastLoginAtUtc`, emit audit event.

#### B. Social first sign-in (no existing link)
1. Validate token and required claims.
2. Apply linking policy:
   - If verified email matches exactly one eligible internal user and policy allows auto-link, create link.
   - Else create controlled registration/link-intent flow requiring explicit confirmation.
3. Persist initial profile fields when creating new user (first/last/email) per domain rules.
4. Emit audit events for create/link outcome.

#### C. Linking additional provider to authenticated account
1. Require active internal authenticated session.
2. Validate provider token.
3. Ensure provider identity is not already linked elsewhere.
4. Create `ExternalIdentity` row for current user.
5. Audit success/failure.

### 4) Security controls

- Token validation:
  - Verify signature via provider JWKS/official middleware.
  - Enforce issuer and audience allow-lists.
  - Enforce expiry/not-before and clock-skew bounds.
- Callback/session protections:
  - CSRF/state validation for auth initiation/callback patterns.
  - Nonce validation where applicable.
- Account takeover prevention:
  - Never auto-link on unverified email.
  - Block link when provider identity already belongs to different user.
  - Use deterministic, reviewable linking policy.
- Abuse resistance:
  - Keep/extend local login rate limiting and lockout.
  - Standardized auth errors (no user enumeration).
- Observability:
  - Structured security/audit logs for login, link, unlink, failures.

### 4b) PasswordHash / PasswordSalt policy for social-first accounts

For users created via social-first registration:
- `PasswordHash` and `PasswordSalt` are nullable and remain `NULL` initially.
- Account is flagged as social-auth-capable via linked `ExternalIdentity` records.
- Local password login is disabled unless the user explicitly sets a password through a secure "set password" flow.

When user sets a local password later:
- Generate fresh salt and hash using current approved password hashing algorithm/work factor.
- Store hash/salt and enable local-password auth for that account (if product policy allows hybrid auth).
- Audit-log password set/reset and provider link state changes.

Constraints:
- Never create placeholder/default passwords for social-first users.
- Enforce same password policy and reset protections as native accounts.

### 5) API surface changes (conceptual)

- Add/extend endpoints for:
  - Social login callback/token exchange handling.
  - Link social account to current internal account.
  - Optional unlink endpoint with safety rules.
- Keep response contracts backward compatible where possible.
- Ensure cancellation token propagation and existing auth attributes remain consistent.

### 6) Data and migration strategy

- Add migration for `ExternalIdentity` table and indexes.
- No destructive migration to internal user table required unless missing profile fields.
- Backfill not required initially; links are created on-demand at first social login/link.

### 7) Test strategy

#### Unit tests
- Token claim mapping and provider normalization.
- Linking policy decisions (verified/unverified email, collisions).
- Error mapping and non-enumerating responses.

#### Integration tests
- Existing link login succeeds.
- First social login creates/links correctly under policy.
- Duplicate provider-subject rejected.
- Attempted takeover scenarios rejected.
- Protected endpoints remain authorized as before with internal principal.

#### Regression checks
- Existing non-social login still works.
- Existing authorization policies unaffected.
- Social-first accounts cannot use local-password login before password is explicitly set.
- After setting password, local login path works under normal policy controls.

## Implementation plan (developer stage)

1. Inspect current auth configuration and user model.
2. Introduce `ExternalIdentity` entity + EF configuration + migration.
3. Implement provider token validation abstraction.
4. Implement social login and link flows in auth service/controller.
5. Add audit logging and standardized error handling.
6. Add/adjust unit + integration tests.
7. Run verification commands from AGENTS.md subset.

## Rollout notes

- Feature-flag social login if operational risk is high.
- Start with Google/Facebook only; keep provider abstraction extensible.
- Monitor auth failure and link-collision metrics after release.
