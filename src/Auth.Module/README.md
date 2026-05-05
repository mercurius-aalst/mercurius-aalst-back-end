# Auth.Module

Authentication module for Mercurius.

Current responsibilities:

- `AddAuthServices(...)` service registration entrypoint
- `MapAuthEndpoints(...)` endpoint mapping entrypoint
- auth entities: `AuthUser`, `Role`, `RefreshToken`
- auth services: login/register/refresh/revoke, token issuance, lockout handling
- auth-user lifecycle via `IAuthUserService` for username, password, and role management
- Google OIDC login/link/unlink orchestration

Non-auth profile concerns live in `MercuriusAPI`.

Google credentials are configuration-driven. Keep `ExternalAuth:Google:ClientSecret` out of source control and provide it via user-secrets or environment variables.
