---
phase: 01-net-10-baseline
plan: 01
subsystem: platform
tags: [dotnet10, aspnetcore, docker, ci]
requires: []
provides:
  - .NET 10 target framework baseline for application and tests
  - .NET 10 SDK pinning through global.json
  - .NET 10 CI and container toolchain alignment
affects: [phase-01, platform-baseline, ci, docker]
tech-stack:
  added: [.NET 10 SDK pin]
  patterns: [runtime/toolchain versions aligned across project, CI, and Docker]
key-files:
  created: [global.json]
  modified:
    - src/MercuriusAPI/Mercurius.LAN.API.csproj
    - tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj
    - Dockerfile
    - .github/workflows/ci-application.yml
key-decisions:
  - "Pinned the repository SDK to the installed .NET 10.0.200 SDK with latestFeature roll-forward."
  - "Kept Swashbuckle as the active OpenAPI provider and removed the unused Microsoft.AspNetCore.OpenApi package to avoid incompatible Microsoft.OpenApi v2 transitive APIs."
  - "Corrected Dockerfile project and entrypoint names so the existing project can build and run in the .NET 10 container."
patterns-established:
  - "Keep application, tests, CI setup, and Docker images on the same .NET major version."
requirements-completed: [PLAT-01]
duration: 9min
completed: 2026-04-29
---

# Phase 1 Plan 01: .NET 10 Baseline Summary

**Application, test, CI, and Docker toolchains aligned on .NET 10 with verified restore/build/container gates**

## Performance

- **Duration:** 9 min
- **Started:** 2026-04-29T20:40:00+02:00
- **Completed:** 2026-04-29T20:49:04+02:00
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments

- Updated the application and test projects to `net10.0`.
- Added `global.json` pinning SDK `10.0.200`.
- Updated CI setup-dotnet to `10.x`.
- Updated Docker SDK/runtime images to .NET 10 and fixed the Dockerfile's project/entrypoint names.
- Verified restore, build, tests, and Docker build under the .NET 10 baseline.

## Task Commits

1. **Task 1: Update target frameworks and SDK pinning** - `ee57e05` (feat)
2. **Task 2: Align CI and Docker toolchains** - `4c77304` (chore)
3. **Task 3: Execute baseline verification gates** - `2f6dec6` (chore)

## Files Created/Modified

- `global.json` - Pins the repository to SDK `10.0.200` with latest feature roll-forward.
- `src/MercuriusAPI/Mercurius.LAN.API.csproj` - Targets `net10.0` and aligns framework-coupled packages.
- `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj` - Targets `net10.0`.
- `Dockerfile` - Uses .NET 10 SDK/runtime images and the correct project/entrypoint names.
- `.github/workflows/ci-application.yml` - Uses setup-dotnet `10.x`.

## Verification

- `dotnet --version` - PASS (`10.0.200`)
- `dotnet restore LAN.API.sln` - PASS with existing NuGet vulnerability warnings.
- `dotnet build LAN.API.sln` - PASS with existing compiler/NuGet warnings.
- `dotnet test LAN.API.sln` - FAIL on host due Windows application-control policy blocking the generated `Mercurius.LAN.API.dll` (`0x800711C7`) before assertions ran.
- `docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0 dotnet test LAN.API.sln` - PASS, 62 passed / 0 failed.
- `docker build --tag mercurius-backend-dotnet10 --file Dockerfile .` - PASS.

## Decisions Made

- Removed the unused `Microsoft.AspNetCore.OpenApi` package after the .NET 10 package introduced `Microsoft.OpenApi` v2 namespaces that conflicted with existing Swashbuckle filters.
- Left `cd.yml` functionally unchanged because it builds through the repository Dockerfile and has no separate .NET SDK setup.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Removed unused OpenAPI package that broke Swagger filter compilation**
- **Found during:** Task 2 (Docker build verification)
- **Issue:** `Microsoft.AspNetCore.OpenApi` 10 pulled an incompatible `Microsoft.OpenApi` API surface while the project uses Swashbuckle filters with the existing namespace layout.
- **Fix:** Removed the unused package reference and kept Swashbuckle as the active OpenAPI provider.
- **Files modified:** `src/MercuriusAPI/Mercurius.LAN.API.csproj`
- **Verification:** Docker build and solution build compile successfully.
- **Committed in:** `ee57e05`

**2. [Rule 3 - Blocking] Corrected Dockerfile project and entrypoint names**
- **Found during:** Task 2 (Docker build verification)
- **Issue:** Dockerfile referenced `MercuriusAPI.csproj` and `MercuriusAPI.dll`, but the actual project/output is `Mercurius.LAN.API`.
- **Fix:** Updated restore/build/publish paths and entrypoint.
- **Files modified:** `Dockerfile`
- **Verification:** `docker build --tag mercurius-backend-dotnet10 --file Dockerfile .` passes.
- **Committed in:** `4c77304`

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both fixes were required for the planned .NET 10 verification gates. No domain/API restructuring was introduced.

## Issues Encountered

- Host `dotnet test` is blocked by Windows application-control policy for generated test output assemblies. The same solution test command passes inside the .NET 10 SDK Linux container, confirming the upgraded code/test baseline is functional outside the local host policy restriction.
- Existing NuGet vulnerability warnings remain for transitive packages such as `NuGet.Packaging`, `NuGet.Protocol`, and `System.Security.Cryptography.Xml`. They were not introduced as a separate feature fix in this plan and should be handled in a dedicated dependency/security pass.

## Known Stubs

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

PLAT-01 is complete for application, tests, CI, and container build/runtime definitions. Plan 01-02 can proceed with PLAT-02 validation feature adoption while keeping domain restructuring deferred.

## Self-Check: PASSED

- Summary file exists: `.planning/phases/01-net-10-baseline/01-01-SUMMARY.md`
- Task commits exist: `ee57e05`, `4c77304`, `2f6dec6`
- Required changed files exist: `global.json`, `src/MercuriusAPI/Mercurius.LAN.API.csproj`, `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`, `Dockerfile`, `.github/workflows/ci-application.yml`

---
*Phase: 01-net-10-baseline*
*Completed: 2026-04-29*
