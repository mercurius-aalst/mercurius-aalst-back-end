# DEVELOPMENT NOTES — Issue #48 (developer stage)

## Current implemented state

1. External identity persistence is provider-scoped:
   - `ExternalIdentity` entity stores `Provider` + `ProviderSubject` per link.
   - `ExternalAuthProvider` enum currently includes `Google` and `Facebook`.
   - `User` includes `ExternalIdentities` navigation.
2. EF Core model + migration changes are in place:
   - `DbSet<ExternalIdentity>` in `MercuriusDBContext`.
   - Required `ProviderSubject`.
   - Unique indexes on (`Provider`, `ProviderSubject`) and (`UserId`, `Provider`).
   - Cascade FK from `ExternalIdentity` to `User`.
   - Migration `RevertExternalIdentityToProviderRows` and snapshot updates added.
3. External auth API surface is implemented:
   - DTO: `ExternalAuthRequest`.
   - Endpoints:
     - `POST /auth/external/login`
     - `POST /auth/external/link`
   - Service methods:
     - `ExternalLoginAsync`
     - `LinkExternalIdentityAsync`
4. External token validation abstraction is wired:
   - `IExternalTokenValidationService` + `ExternalTokenValidationService`.
   - DI registration added.
   - Configuration contract added in `appsettings*.json`:
     - `Authentication:External:{Provider}:Audience`
     - `Authentication:External:{Provider}:Issuer`
     - `Authentication:External:{Provider}:SigningKey`
5. Documentation artifact added:
   - `docs/auth-register-login-flow.mmd`
   - `docs/auth-register-login-flow.svg`

## Verification executed

- `dotnet restore` ✅
- `dotnet build --no-restore` ✅ (passes with existing repository warnings)
- `dotnet test --no-build` ✅
- `dotnet format --verify-no-changes` ✅

## Remaining work for Issue #48

- Replace symmetric-key config-based JWT validation with provider-grade validation strategy:
  - Google: OIDC/JWKS-based signature validation + strict issuer/audience.
  - Facebook: token introspection/validation against Meta endpoints.
- Implement robust social-first registration decision flow when external login has no existing link.
- Add structured audit logging events for external login/link success/failure.
- Add dedicated unit/integration tests for external login/link edge cases and takeover prevention.
- Review and trim migration scope if needed (current generated migration includes broad schema diffs due existing model/snapshot divergence).
