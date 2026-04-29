# Codebase Concerns

**Analysis Date:** 2026-04-29

## Tech Debt

**Startup owns database migration and user seeding (Risk: High):**
- Issue: Application boot applies pending EF migrations and seeds an initial user synchronously during `Program.Main`.
- Files: `src/MercuriusAPI/Program.cs:82`, `src/MercuriusAPI/Program.cs:84`, `src/MercuriusAPI/Program.cs:87`, `src/MercuriusAPI/Services/UserServices/UserService.cs:90`
- Impact: Production startup can fail or stall on migration locks, multiple instances can race during deploys, and operational rollback is coupled to API process startup.
- Fix approach: Move migrations and initial user provisioning to a deployment job or explicit admin command. Keep runtime startup limited to dependency registration and health checks.

**Compiler nullability warnings are suppressed globally (Risk: Medium):**
- Issue: `CS8602` and `CS8618` are disabled for the web project while DTOs and EF navigation properties contain many non-nullable members initialized by model binding or EF.
- Files: `src/MercuriusAPI/Mercurius.LAN.API.csproj:13`, `src/MercuriusAPI/DTOs/GameDTOs/CreateGameDTO.cs:7`, `src/MercuriusAPI/DTOs/GameDTOs/UpdateGameDTO.cs:7`, `src/MercuriusAPI/Models/Match.cs:34`
- Impact: Real null dereferences are hidden, especially around uploaded files, model-bound DTOs, and navigation properties loaded through selective `Include` chains.
- Fix approach: Remove broad `CS8602`/`CS8618` suppression incrementally. Use `required`, nullable annotations, constructors, and endpoint validation for DTOs and EF entities.

**Generated build output is present under tests (Risk: Low):**
- Issue: `tests/MercuriusAPI.Tests/obj/` files are present in the repository tree.
- Files: `tests/MercuriusAPI.Tests/obj/Debug/net8.0/MercuriusAPI.Tests.AssemblyInfo.cs`, `tests/MercuriusAPI.Tests/obj/Release/net8.0/MercuriusAPI.Tests.GlobalUsings.g.cs`, `.gitignore`
- Impact: Generated files create review noise, can conflict across SDK versions, and make codebase searches less accurate.
- Fix approach: Ensure `bin/` and `obj/` are ignored for all projects and remove committed/generated test output from source control.

## Known Bugs

**Swiss bracket type is exposed but unsupported by factory (Risk: High):**
- Symptoms: `BracketType.Swiss` exists and can be selected on `CreateGameDTO`/`UpdateGameDTO`, but `MatchModeratorFactory` does not return `SwissStageMatchModerator`; starting a Swiss game throws `NotSupportedException`.
- Files: `src/MercuriusAPI/Models/BracketType.cs:8`, `src/MercuriusAPI/DTOs/GameDTOs/CreateGameDTO.cs:9`, `src/MercuriusAPI/DTOs/GameDTOs/UpdateGameDTO.cs:10`, `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs:16`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SwissStageMatchModerator.cs:112`
- Trigger: Create or update a game with `BracketType.Swiss`, then call `POST /lan/games/{id}/start`.
- Workaround: Do not expose Swiss in clients until the factory and placement logic support it.

**Unhandled exceptions can produce inconsistent responses (Risk: Medium):**
- Symptoms: The custom exception middleware catches known domain exceptions only; generic exceptions such as `NotSupportedException`, `NotImplementedException`, image decode failures, or database failures fall through to the platform handler.
- Files: `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs:15`, `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs:43`, `src/MercuriusAPI/Services/MatchServices/MatchModeratorFactory.cs:20`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SwissStageMatchModerator.cs:112`
- Trigger: Unsupported bracket type, failed image decode, database connectivity failure, or incomplete bracket placement path.
- Workaround: None in code. Add a final `catch (Exception)` that logs and returns a consistent problem response without leaking internals.

**Game reset clears participants as well as matches and placements (Risk: Medium):**
- Symptoms: Resetting a completed or canceled game deletes the registration list in memory and then persists the changed aggregate.
- Files: `src/MercuriusAPI/Models/Game.cs:79`, `src/MercuriusAPI/Models/Game.cs:86`, `src/MercuriusAPI/Models/Game.cs:87`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:125`
- Trigger: Call `POST /lan/games/{id}/reset` for a completed or canceled game.
- Workaround: Re-add every participant after reset. Clarify intended semantics; if reset means "rerun tournament", clear only matches, placements, status, and times.

## Security Considerations

**Refresh token storage and revocation are bearer-token based (Risk: High):**
- Risk: Refresh tokens are stored as plaintext database values and `/auth/revoke` revokes any submitted token without checking that it belongs to the authenticated user.
- Files: `src/MercuriusAPI/Models/Auth/RefreshToken.cs:6`, `src/MercuriusAPI/Data/MercuriusDBContext.cs:107`, `src/MercuriusAPI/Data/MercuriusDBContext.cs:110`, `src/MercuriusAPI/Services/Auth/AuthService.cs:76`, `src/MercuriusAPI/Services/Auth/AuthService.cs:92`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs:33`
- Current mitigation: Tokens are random 64-byte values generated by `RandomNumberGenerator` in `src/MercuriusAPI/Services/Auth/Token/TokenService.cs:45`, and refresh rotates tokens in `src/MercuriusAPI/Services/Auth/AuthService.cs:84`.
- Recommendations: Store a keyed hash of refresh tokens, add a database uniqueness index, bind revoke to `ClaimsPrincipal`, and track created/revoked metadata instead of deleting records immediately.

**Login lockout is process-local and username-only (Risk: Medium):**
- Risk: Failed login state lives in an in-memory `ConcurrentDictionary`, so lockouts reset on restart and do not coordinate across multiple API instances.
- Files: `src/MercuriusAPI/Services/Auth/Login/LoginAttemptService.cs:10`, `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs:56`, `src/MercuriusAPI/Services/Auth/AuthService.cs:39`
- Current mitigation: Five attempts within a five-minute window produce a five-minute lockout through singleton service registration.
- Recommendations: Move throttling to a distributed store or gateway rate limiter. Include IP/device dimensions and avoid username enumeration through attempt-count response details.

**Image uploads rely on size validation only (Risk: Medium):**
- Risk: Uploaded files are decoded by Imageflow after only null, zero-length, and total size checks. MIME type, extension, image dimensions, and decode work limits are not enforced before processing.
- Files: `src/MercuriusAPI/Services/FileServices/FileValidationService.cs:20`, `src/MercuriusAPI/Services/FileServices/FileValidationService.cs:26`, `src/MercuriusAPI/Services/FileServices/FileService.cs:14`, `src/MercuriusAPI/Services/FileServices/FileService.cs:33`
- Current mitigation: Files are saved under randomized names and converted to `.webp` in `src/MercuriusAPI/Services/FileServices/FileService.cs:24`.
- Recommendations: Enforce content type allowlists, inspect image headers, cap dimensions/pixels, catch decoder exceptions, and consider asynchronous processing for large uploads.

**Swagger is always enabled (Risk: Medium):**
- Risk: Swagger and Swagger UI are registered and served unconditionally.
- Files: `src/MercuriusAPI/Program.cs:90`, `src/MercuriusAPI/Program.cs:91`, `src/MercuriusAPI/Extensions/DepedencyConfiguration.cs:32`
- Current mitigation: Secured Swagger helpers exist in `src/MercuriusAPI/Extensions/SecuredSwaggerOptions.cs` and `src/MercuriusAPI/Program.cs:112`.
- Recommendations: Gate Swagger by environment and require authentication before exposing API metadata outside development.

**Password hash comparison is not constant-time (Risk: Low):**
- Risk: `SequenceEqual` short-circuits and can leak timing differences in hash comparison paths.
- Files: `src/MercuriusAPI/Services/Auth/PasswordHelper.cs:20`, `src/MercuriusAPI/Services/Auth/PasswordHelper.cs:24`
- Current mitigation: PBKDF2 with SHA-256, per-user salt, and 100,000 iterations are used in `src/MercuriusAPI/Services/Auth/PasswordHelper.cs:5`.
- Recommendations: Replace `SequenceEqual` with `CryptographicOperations.FixedTimeEquals`.

## Performance Bottlenecks

**Unpaged public list endpoints load complete tables (Risk: Medium):**
- Problem: Public list endpoints return all games/sponsors/teams and services materialize full EF query results without pagination.
- Files: `src/MercuriusAPI/Endpoints/GameEndpoints.cs:18`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:50`, `src/MercuriusAPI/Services/SponsorServices/SponsorService.cs:17`, `src/MercuriusAPI/Services/TeamServices/TeamService.cs:38`
- Cause: Services expose `IEnumerable` or `IQueryable` style read methods without `skip`/`take` or projection-specific includes.
- Improvement path: Add pagination parameters, use `AsNoTracking()`, project directly to DTOs, and avoid loading matches/participants for summary list views unless explicitly requested.

**Match update loads a deep graph for every score change (Risk: Medium):**
- Problem: Updating one match eagerly includes next matches, participant slots, and nested next-match links.
- Files: `src/MercuriusAPI/Services/MatchServices/MatchService.cs:27`, `src/MercuriusAPI/Services/MatchServices/MatchService.cs:29`, `src/MercuriusAPI/Services/MatchServices/MatchService.cs:34`, `src/MercuriusAPI/Services/MatchServices/MatchService.cs:42`
- Cause: Propagation logic mutates adjacent matches through navigation properties in `src/MercuriusAPI/Models/Match.cs:116`.
- Improvement path: Load only the current match plus direct `WinnerNextMatch` and `LoserNextMatch` rows required for the selected bracket type. Move propagation into a service that can issue targeted queries.

**Synchronous file conversion runs on request path (Risk: Medium):**
- Problem: Image uploads are decoded and encoded to WebP before the request returns.
- Files: `src/MercuriusAPI/Services/FileServices/FileService.cs:28`, `src/MercuriusAPI/Services/FileServices/FileService.cs:33`, `src/MercuriusAPI/Services/FileServices/FileService.cs:35`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:34`, `src/MercuriusAPI/Services/SponsorServices/SponsorService.cs:40`
- Cause: File storage and image processing are coupled directly to create/update service methods.
- Improvement path: Keep upload validation synchronous, then enqueue conversion or bound it with cancellation and resource limits. Return structured failure when conversion fails.

## Fragile Areas

**Tournament bracket generation and propagation (Risk: High):**
- Files: `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs:11`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs:46`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs:235`, `src/MercuriusAPI/Models/Match.cs:116`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs:72`
- Why fragile: Bracket topology, BYE propagation, match numbering, and winner/loser propagation are encoded with positional math and mutable navigation properties. Double elimination is the largest non-migration file at 401 lines.
- Safe modification: Add scenario tests before changing bracket code. Cover participant counts of 2, 3, 4, 5, 8, and 16 for each bracket type; assert generated graph links and placement output, not just match counts.
- Test coverage: Current tests focus on `Match` domain methods in `tests/MercuriusAPI.Tests/MatchTests.cs`; direct moderator tests are not detected for `DoubleEliminationMatchModerator`, `SingleEliminationMatchModerator`, `RoundRobinMatchModerator`, or `SwissStageMatchModerator`.

**EF model lacks indexes/unique constraints for identity-like fields (Risk: Medium):**
- Files: `src/MercuriusAPI/Data/MercuriusDBContext.cs:34`, `src/MercuriusAPI/Data/MercuriusDBContext.cs:45`, `src/MercuriusAPI/Data/MercuriusDBContext.cs:84`, `src/MercuriusAPI/Data/MercuriusDBContext.cs:107`, `src/MercuriusAPI/Services/Auth/AuthService.cs:26`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:141`
- Why fragile: Code checks uniqueness in application queries, but database constraints are not declared for usernames, role names, game names, refresh tokens, or participant identity fields.
- Safe modification: Add explicit `HasIndex(...).IsUnique()` where business rules require uniqueness and handle database conflicts as validation errors.
- Test coverage: No database integration tests are detected; tests instantiate domain models directly.

**Domain operations depend on EF-loaded navigation shape (Risk: Medium):**
- Files: `src/MercuriusAPI/Services/GameServices/GameServices.cs:43`, `src/MercuriusAPI/Services/MatchServices/MatchService.cs:27`, `src/MercuriusAPI/Models/Game.cs:92`, `src/MercuriusAPI/Models/Match.cs:116`
- Why fragile: Methods like `Game.AddParticipant`, `Game.Reset`, and `Match.UpdateParticipantsNextMatch` assume related collections or navigation properties are loaded and mutable.
- Safe modification: Treat aggregate methods as pure invariants and keep persistence graph loading in services. Add tests that exercise service methods with EF, not only object instances.
- Test coverage: `tests/MercuriusAPI.Tests/GameTests.cs` and `tests/MercuriusAPI.Tests/MatchTests.cs` cover direct model behavior but not EF tracking behavior.

## Scaling Limits

**API instances cannot share lockout or in-memory auth state (Risk: Medium):**
- Current capacity: One process has one `LoginAttemptService` dictionary.
- Limit: Horizontal scale, process restart, or blue-green deploys reset lockout state.
- Scaling path: Store login attempts in Redis, database, or ASP.NET Core rate-limiting middleware backed by distributed counters.

**Local filesystem image storage limits deployment topology (Risk: Medium):**
- Current capacity: One configured filesystem location from `FileStorage:Location`.
- Limit: Multiple API instances need shared volume semantics; container restarts or ephemeral storage can orphan images unless the location is durable.
- Scaling path: Move images to object storage or a managed shared volume; store image metadata in the database and serve through CDN or authenticated static middleware.

**Unbounded public reads scale with table size (Risk: Medium):**
- Current capacity: Works while game, team, sponsor, and match counts are small.
- Limit: `GetAllGames()` includes participants and matches for every game in one request.
- Scaling path: Add paged summary endpoints and separate detail endpoints for expanded graph data.

## Dependencies at Risk

**Mixed EF Core major versions with .NET 8 target (Risk: Medium):**
- Risk: The app targets `net8.0` while EF packages are version `9.0.x`, and ASP.NET packages are version `8.0.x`.
- Impact: Runtime behavior and tooling expectations can diverge between framework and EF versions, especially in migrations and provider compatibility.
- Migration plan: Align EF provider/tooling versions with the target runtime policy, or explicitly document and test the EF 9-on-.NET 8 support matrix in CI.

**Imageflow processing is a security-sensitive dependency (Risk: Medium):**
- Risk: Image decoding handles untrusted upload bytes and is reachable through game and sponsor create/update paths.
- Impact: Decoder vulnerabilities or resource-exhaustion behavior affect authenticated admin endpoints and image serving.
- Migration plan: Keep Imageflow packages monitored, pin known-good versions, add image validation boundaries around `IFileService`, and maintain an alternative processor path if package maintenance becomes a blocker.

## Missing Critical Features

**No API-level integration tests (Risk: High):**
- Problem: Endpoint authorization, model binding, middleware responses, database mapping, CORS, Swagger security, and file upload behavior are not covered by tests.
- Blocks: Safe changes to auth, endpoint routes, exception middleware, EF includes, and upload validation.
- Files: `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`, `src/MercuriusAPI/Program.cs`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`, `src/MercuriusAPI/Endpoints/GameEndpoints.cs`

**No structured logging in exception handling (Risk: Medium):**
- Problem: The middleware writes error messages to clients but does not log exceptions.
- Blocks: Production diagnosis of validation failures, image decode failures, auth lockouts, and unexpected 500s.
- Files: `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs:9`, `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs:15`

**No database uniqueness constraints for business keys (Risk: Medium):**
- Problem: Application checks enforce some uniqueness, but concurrent writes can create duplicates.
- Blocks: Reliable user registration, game creation, role assignment, and refresh-token lookup under concurrency.
- Files: `src/MercuriusAPI/Data/MercuriusDBContext.cs`, `src/MercuriusAPI/Services/Auth/AuthService.cs:26`, `src/MercuriusAPI/Services/GameServices/GameServices.cs:141`

## Test Coverage Gaps

**Authentication and authorization services/endpoints (Priority: High):**
- What's not tested: Register/login/refresh/revoke flows, lockout timing, role authorization, token rotation, password change, and initial user seeding.
- Files: `src/MercuriusAPI/Services/Auth/AuthService.cs`, `src/MercuriusAPI/Services/Auth/Login/LoginAttemptService.cs`, `src/MercuriusAPI/Services/Auth/Token/TokenService.cs`, `src/MercuriusAPI/Endpoints/AuthEndpoints.cs`, `src/MercuriusAPI/Endpoints/UserEndpoints.cs`
- Risk: Auth regressions can ship unnoticed and security behavior is mostly unverified.
- Priority: High

**File upload and image processing (Priority: High):**
- What's not tested: File size limits, null/empty file rejection, invalid image decode handling, path configuration, and WebP conversion failures.
- Files: `src/MercuriusAPI/Services/FileServices/FileValidationService.cs`, `src/MercuriusAPI/Services/FileServices/FileService.cs`, `src/MercuriusAPI/Services/GameServices/GameServices.cs`, `src/MercuriusAPI/Services/SponsorServices/SponsorService.cs`
- Risk: Upload failures can break admin workflows or expose resource exhaustion.
- Priority: High

**Bracket moderators and placement calculation (Priority: High):**
- What's not tested: Match graph generation for single elimination, double elimination, round robin, Swiss support state, BYE propagation across full brackets, and placement output.
- Files: `src/MercuriusAPI/Services/MatchServices/BracketTypes/SingleEliminationMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/DoubleEliminationMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/RoundRobinMatchModerator.cs`, `src/MercuriusAPI/Services/MatchServices/BracketTypes/SwissStageMatchModerator.cs`
- Risk: Tournament results can be incorrect while existing unit tests still pass.
- Priority: High

**EF persistence and endpoint integration (Priority: Medium):**
- What's not tested: EF relationships, cascade deletes, many-to-many participant links, migrations, `GetAllGames()` query shape, and middleware status codes.
- Files: `src/MercuriusAPI/Data/MercuriusDBContext.cs`, `src/MercuriusAPI/Services/GameServices/GameServices.cs`, `src/MercuriusAPI/Services/MatchServices/MatchService.cs`, `src/MercuriusAPI/Middleware/ExceptionHandlingMiddleware.cs`
- Risk: Object-level tests miss tracking, query, and serialization defects.
- Priority: Medium

---

*Concerns audit: 2026-04-29*
