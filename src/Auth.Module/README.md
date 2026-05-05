# Auth.Module

Authentication module for Mercurius.

Current responsibilities:

- `AddAuthServices(...)` service registration entrypoint
- `MapAuthEndpoints(...)` endpoint mapping entrypoint
- auth entities: `AuthUser`, `Role`, `RefreshToken`
- auth services: login/register/refresh/revoke, token issuance, lockout handling
- auth-user lifecycle via `IAuthUserService` for username, password, and role management
- provider-agnostic OIDC login/link/unlink orchestration through a strategy-backed provider registry

Non-auth profile concerns live in `MercuriusAPI`.

OIDC providers are discovered from `ExternalAuth:<ProviderName>` configuration sections. A provider is enabled when its `ClientId` is set.

For standard OIDC providers, adding another provider is configuration-only:

1. Add a new `ExternalAuth:<ProviderName>` section with `ClientId`, `ClientSecret`, `RedirectUri`, and `MetadataAddress`.
2. Call the existing `/api/v{version}/auth/external/{provider}/...` endpoints with the provider name from that section.

Google keeps compatibility defaults for metadata, valid issuers, and consent parameters, but those values can still be overridden in configuration.

Keep provider client secrets out of source control and provide them via user-secrets or environment variables.
