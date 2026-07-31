# Development Notes

## What changed

- Completed Phase 11 validation work for the Competition extraction slice.
- Centralized shared Competition eligibility logic in `CompetitionEligibilityEvaluator`.
- Split registration persistence concerns into `TournamentRegistrationPersistenceCoordinator`.
- Split registration read-model concerns into `TournamentRegistrationReadModelService`.
- Split DTO context-building concerns into `RegistrationMappingContextBuilder` and `RegistrationMappingContext`.
- Tightened public participant privacy in Competition DTOs so anonymous responses no longer expose platform handles, deleted-user name fallbacks, or deleted-team-member deletion labels.
- Optimized Competition search and registration/game read paths:
  - bounded Competition game search to the active page window,
  - removed unsafe concurrent same-`DbContext` search fanout,
  - added `AsNoTracking`/`AsSplitQuery` protections on read paths,
  - narrowed the game list query to active registrations for the public list route,
  - reduced unnecessary simple-mutation graph loading in `GameService`.
- Updated or added regression coverage for search, privacy, architecture, sponsor placement, registration, and DTO mapping behavior.
- Marked OpenSpec validation task boxes `5.1`, `5.2`, and `5.3` complete in `openspec/changes/extract-competition-module/tasks.md`.

## Why it changed

- Phase 11 step 5 required authoritative validation plus independent clean-code, performance, and security audits, with remediation of all High and Medium findings.
- The remediations addressed concrete risks found during auditing:
  - duplicated eligibility ownership,
  - over-broad registration-service responsibilities,
  - redundant roster-profile fetches,
  - unsafe parallel EF Core access on a shared context,
  - public privacy leaks in Competition participant DTOs.

## Commits made

- None.

## Commands run and results

- `dotnet restore`: passed
- `dotnet build`: passed multiple times during remediation; final full build passed
- `dotnet test`: passed multiple times during remediation; final full test run passed with `374/374`
- `dotnet test --filter OpenApiDocumentTests`: passed `1/1`
- `dotnet test --filter "FullyQualifiedName~ModuleArchitectureTests|FullyQualifiedName~TournamentRegistrationServiceTests|FullyQualifiedName~OpenApiDocumentTests|FullyQualifiedName~PublicParticipantPrivacyDTOTests|FullyQualifiedName~SponsorFeatureTests"`: passed `56/56`
- `dotnet test --filter "FullyQualifiedName~SearchServiceTests|FullyQualifiedName~TournamentRegistrationServiceTests|FullyQualifiedName~GameTests|FullyQualifiedName~GameScheduleTests|FullyQualifiedName~ModuleArchitectureTests"`: passed `89/89`
- `dotnet test --filter "FullyQualifiedName~PublicParticipantPrivacyDTOTests|FullyQualifiedName~DtoSerializationShapeTests|FullyQualifiedName~OpenApiDocumentTests|FullyQualifiedName~SearchServiceTests|FullyQualifiedName~TournamentRegistrationServiceTests"`: passed `36/36`
- `dotnet test --filter "FullyQualifiedName~GameTests|FullyQualifiedName~GameScheduleTests|FullyQualifiedName~TournamentRegistrationServiceTests|FullyQualifiedName~SearchServiceTests|FullyQualifiedName~PublicParticipantPrivacyDTOTests|FullyQualifiedName~OpenApiDocumentTests"`: passed `67/67`
- `dotnet test --filter "FullyQualifiedName~SearchServiceTests|FullyQualifiedName~TournamentRegistrationServiceTests"`: passed `26/26`
- `dotnet test --filter "FullyQualifiedName~PublicParticipantPrivacyDTOTests|FullyQualifiedName~SearchServiceTests|FullyQualifiedName~DtoSerializationShapeTests|FullyQualifiedName~TournamentRegistrationServiceTests|FullyQualifiedName~OpenApiDocumentTests"`: passed `37/37`
- `openspec validate extract-competition-module --strict`: passed before the final remediation loop
- `git diff -- src/MercuriusAPI/Migrations/MercuriusDBContextModelSnapshot.cs`: no output before the final remediation loop
- `dotnet run --project src\\MercuriusAPI --no-build`: intentionally not executed because startup applies migrations and the action was rejected as unsafe against an unknown configured database
- Final reruns of `dotnet format --verify-no-changes`, strict OpenSpec validation, and the snapshot diff check were blocked by the account usage-limit gate after the code was already green

## Known limitations or follow-up items

- Final fresh child-agent re-audits were partially blocked by the account usage-limit gate after the code and targeted regressions were green. The last successful security re-audit reported no High or Medium findings. Earlier successful clean-code, performance, and security findings were remediated in this slice.
- API startup was validated indirectly through build plus the dedicated `OpenApiDocumentTests`; direct `dotnet run` startup remained intentionally blocked because `Program.cs` applies migrations on boot.
- No migration or snapshot regeneration was performed, and `src/MercuriusAPI/Migrations/MercuriusDBContextModelSnapshot.cs` was not intentionally modified in this slice.
