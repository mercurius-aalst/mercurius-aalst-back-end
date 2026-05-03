# Technology Stack

**Analysis Date:** 2026-04-29

## Languages

**Primary:**
- C# 12 / .NET 8 - ASP.NET Core backend source in `src/MercuriusAPI/**/*.cs`; project targets `net8.0` in `src/MercuriusAPI/Mercurius.LAN.API.csproj`.

**Secondary:**
- JavaScript - Swagger UI customization in `src/MercuriusAPI/staticfiles/swagger-custom.js`.
- YAML - GitHub Actions workflows in `.github/workflows/ci-application.yml` and `.github/workflows/cd.yml`.
- Dockerfile - Container build definition in `Dockerfile`.
- JSON - Runtime and launch configuration in `src/MercuriusAPI/appsettings.json`, `src/MercuriusAPI/appsettings.Development.json`, and `src/MercuriusAPI/Properties/launchSettings.json`.

## Runtime

**Environment:**
- ASP.NET Core on .NET 8. The application project uses `Microsoft.NET.Sdk.Web` and `<TargetFramework>net8.0</TargetFramework>` in `src/MercuriusAPI/Mercurius.LAN.API.csproj`.
- Local machine has `dotnet --version` reporting `10.0.200`, while the repo itself targets .NET 8.
- Docker build image: `mcr.microsoft.com/dotnet/sdk:8.0.415-azurelinux3.0` in `Dockerfile`.
- Docker runtime image: `mcr.microsoft.com/dotnet/aspnet:8.0.21-azurelinux3.0` in `Dockerfile`.

**Package Manager:**
- NuGet via SDK-style `.csproj` package references in `src/MercuriusAPI/Mercurius.LAN.API.csproj` and `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- Lockfile: missing. No `packages.lock.json`, `Directory.Packages.props`, `NuGet.config`, or `global.json` detected at repository root.

## Frameworks

**Core:**
- ASP.NET Core 8 - Minimal API application entry point in `src/MercuriusAPI/Program.cs`; endpoints are mapped with `app.MapGameEndpoints()`, `app.MapMatchEndpoints()`, `app.MapPlayerEndpoints()`, `app.MapTeamEndpoints()`, `app.MapSponsorEndpoints()`, `app.MapUserEndpoints()`, and `app.MapAuthEndpoints()`.
- Entity Framework Core 9.0.5 tooling/design packages - database access through `MercuriusDBContext` in `src/MercuriusAPI/Data/MercuriusDBContext.cs`; migrations live in `src/MercuriusAPI/Migrations/`.
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4 - PostgreSQL provider configured through `options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB"))` in `src/MercuriusAPI/Program.cs`.
- ASP.NET Core Authentication JwtBearer 8.0.19 - bearer authentication configured in `src/MercuriusAPI/Program.cs` and token generation in `src/MercuriusAPI/Services/Auth/Token/TokenService.cs`.
- Asp.Versioning.Mvc 8.1.0 and Asp.Versioning.Mvc.ApiExplorer 8.1.0 - API versioning/OpenAPI explorer support referenced in `src/MercuriusAPI/Mercurius.LAN.API.csproj`; Swagger option wiring is in `src/MercuriusAPI/Extensions/ConfigureSwaggerOptions.cs`.
- Swashbuckle.AspNetCore 8.1.2 and Microsoft.AspNetCore.OpenApi 8.0.11 - Swagger/OpenAPI generation configured in `src/MercuriusAPI/Program.cs`, `src/MercuriusAPI/Extensions/JWTBuilder.cs`, and `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- Imageflow.Net 0.14.1 and Imageflow.Server 0.9.0 - image conversion and serving configured in `src/MercuriusAPI/Services/FileServices/FileService.cs` and `src/MercuriusAPI/Program.cs`.
- Scrutor 6.1.0 - decorator-based service composition in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.

**Testing:**
- xUnit 2.5.3 - test framework referenced in `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- Microsoft.NET.Test.Sdk 17.8.0 - test runner integration referenced in `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- AutoFixture 4.18.1 - test data generation package referenced in `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- coverlet.collector 6.0.0 - coverage collector referenced in `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.

**Build/Dev:**
- .NET CLI - CI runs `dotnet restore`, `dotnet build`, and `dotnet test` in `.github/workflows/ci-application.yml`.
- Docker - CI builds the image from `Dockerfile` in `.github/workflows/ci-application.yml`; CD publishes with Docker Buildx in `.github/workflows/cd.yml`.
- Trivy - container vulnerability scan via `aquasecurity/trivy-action@0.29.0` in `.github/workflows/ci-application.yml`.
- Release Please - semantic release creation via `googleapis/release-please-action@v4` in `.github/workflows/cd.yml`.

## Key Dependencies

**Critical:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.19 - validates API bearer tokens in `src/MercuriusAPI/Program.cs`.
- `System.IdentityModel.Tokens.Jwt` 8.13.0 - creates JWT access tokens in `src/MercuriusAPI/Services/Auth/Token/TokenService.cs`.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 - connects EF Core to PostgreSQL in `src/MercuriusAPI/Program.cs`.
- `Microsoft.EntityFrameworkCore.Design` 9.0.5 and `Microsoft.EntityFrameworkCore.Tools` 9.0.5 - migration generation and database tooling for `src/MercuriusAPI/Migrations/`.
- `Imageflow.Net` 0.14.1 and `Imageflow.Server` 0.9.0 - image upload normalization to WebP and `/images` serving in `src/MercuriusAPI/Services/FileServices/FileService.cs` and `src/MercuriusAPI/Program.cs`.

**Infrastructure:**
- `Swashbuckle.AspNetCore` 8.1.2 - Swagger UI and OpenAPI docs in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- `Asp.Versioning.Mvc` 8.1.0 and `Asp.Versioning.Mvc.ApiExplorer` 8.1.0 - version-aware API documentation support in `src/MercuriusAPI/Extensions/ConfigureSwaggerOptions.cs`.
- `Scrutor` 6.1.0 - service decorators for validation layers in `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs`.
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` 8.0.7 - Visual Studio scaffolding support referenced in `src/MercuriusAPI/Mercurius.LAN.API.csproj`.

## Configuration

**Environment:**
- Configuration loads `src/MercuriusAPI/appsettings.json` and environment variables prefixed with `Mercurius.LAN.API_` in `src/MercuriusAPI/Program.cs`.
- Development overrides are stored in `src/MercuriusAPI/appsettings.Development.json`.
- User Secrets are enabled through `<UserSecretsId>` in `src/MercuriusAPI/Mercurius.LAN.API.csproj`; use secrets for local values that should not be committed.
- Required configuration sections detected: `ConnectionStrings:MercuriusDB`, `FileStorage:Location`, `FileStorage:MaxFileSizeInMB`, `TeamInvite:ResendCooldownDays`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiresInMinutes`, and `InitialUser`.
- Launch profiles are defined in `src/MercuriusAPI/Properties/launchSettings.json` for HTTPS, IIS Express, and Docker.

**Build:**
- Solution file: `LAN.API.sln`.
- Application project: `src/MercuriusAPI/Mercurius.LAN.API.csproj`.
- Test project: `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- Container definition: `Dockerfile`.
- CI workflow: `.github/workflows/ci-application.yml`.
- CD workflow: `.github/workflows/cd.yml`.
- Release configuration: `release-please-config.json` and `.release-please-manifest.json`.
- Local .NET tool manifest exists at `src/MercuriusAPI/.config/dotnet-tools.json`, with no tools registered.

## Platform Requirements

**Development:**
- Install .NET SDK compatible with `net8.0`; CI uses `actions/setup-dotnet@v3` with `dotnet-version: '8.x'` in `.github/workflows/ci-application.yml`.
- Provide a PostgreSQL database reachable via `ConnectionStrings:MercuriusDB`; EF Core applies pending migrations at startup in `src/MercuriusAPI/Program.cs`.
- Provide file storage path configuration via `FileStorage:Location`; uploads are written to local disk by `src/MercuriusAPI/Services/FileServices/FileService.cs`.
- Run all tests with `dotnet test` from the repository root, matching `.github/workflows/ci-application.yml`.

**Production:**
- Containerized Linux deployment is the intended packaging path. CD builds and pushes `livingwooods/mercurius-backend:{VERSION}` to Docker Hub in `.github/workflows/cd.yml`.
- Runtime container uses ASP.NET Core 8 on Azure Linux in `Dockerfile`.
- Production configuration must be supplied through environment variables using the `Mercurius.LAN.API_` prefix or mounted configuration compatible with `src/MercuriusAPI/Program.cs`.

---

*Stack analysis: 2026-04-29*
