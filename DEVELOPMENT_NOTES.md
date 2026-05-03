# DEVELOPMENT NOTES — Issue #48 (developer stage)

## Implemented slice

1. Added a non-sequential public identifier to `User`:
   - `PublicId : Guid` defaulted at entity creation.
   - Exposed via `GetUserDTO`.
2. Made local password fields nullable for social-first users:
   - `PasswordHash` and `Salt` are now nullable.
   - EF model marks these columns optional.
3. Hardened username/password login behavior:
   - Login now rejects accounts without local password material (`PasswordHash`/`Salt`) as invalid credentials.

## Security intent

- `PublicId` supports using non-sequential external identifiers to reduce user-id enumeration risk.
- Nullable password fields avoid fake/default passwords for social-first users.
- Explicit login guard prevents accidental local-password auth for social-only accounts.

## Environment limitation

- Could not run build/tests because `dotnet` CLI is unavailable in this environment.

## Dotnet CLI installation attempts

Tried multiple installation paths to enable local validation:

1. `curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh`
   - Failed with HTTP 403.
2. `wget -qO /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh`
   - Failed due network/proxy restrictions.
3. `apt-get update && apt-get install -y dotnet-sdk-8.0`
   - Failed because apt repositories are blocked by proxy (HTTP 403).

Given current environment network policy, .NET SDK cannot be installed from this container.

## Retry after environment update

Re-checked after environment update request:

- `dotnet --info` still returns `dotnet: command not found`.
- No `dotnet` binary found in common install locations (`/usr/share/dotnet`, `/usr/local/share/dotnet`) or shallow system search.

Conclusion: .NET SDK/CLI is still not present in this container, so build/test validation remains blocked here.

## Additional SDK acquisition attempts

Further attempts made:

- `curl https://aka.ms/dotnet-install.sh`
- `curl https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh`
- `curl https://raw.githubusercontent.com/dotnet/install-scripts/main/src/dotnet-install.sh`

All returned HTTP 403 in this environment, preventing SDK bootstrap.
