# Testing Patterns

**Analysis Date:** 2026-04-29

## Test Framework

**Runner:**
- xUnit `2.5.3`
- Config: `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`
- .NET test SDK: `Microsoft.NET.Test.Sdk` `17.8.0`

**Assertion Library:**
- xUnit assertions: `Assert.Equal`, `Assert.Throws<T>`, `Assert.Contains`, `Assert.Empty`, `Assert.True`, `Assert.Null`.
- AutoFixture `4.18.1` is used for object creation in larger domain-model tests.

**Run Commands:**
```bash
dotnet test LAN.API.sln              # Run all tests in the solution
dotnet test tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj              # Run the test project
dotnet test LAN.API.sln --collect:"XPlat Code Coverage"              # Run tests with coverlet collector
```

## Test File Organization

**Location:**
- Tests live in a separate test project under `tests/MercuriusAPI.Tests/`.
- The test project references the API project through `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`.
- Shared test customizations live under `tests/MercuriusAPI.Tests/Customizations/`.

**Naming:**
- Test classes use `{Domain}Tests`: `TeamTests`, `PlayerTests`, `MatchTests`, `GameTests`.
- Test methods use behavior-oriented names with underscores in most files: `InvitePlayer_Should_Throw_When_Player_Already_In_Team` in `tests/MercuriusAPI.Tests/TeamTests.cs`; `SetScoresAndWinner_ThrowsValidationException_WhenScoreIsNegative` in `tests/MercuriusAPI.Tests/MatchTests.cs`.
- Some tests use shorter method names without full `Should` phrasing, as in `Update_UpdatesPlayerProperties` in `tests/MercuriusAPI.Tests/PlayerTests.cs`; prefer the more explicit behavior pattern for new tests.

**Structure:**
```text
tests/MercuriusAPI.Tests/
├── GameTests.cs
├── MatchTests.cs
├── PlayerTests.cs
├── TeamTests.cs
├── Customizations/
│   └── MatchParticipantCustomization.cs
└── Mercurius.LAN.API.Tests.csproj
```

## Test Structure

**Suite Organization:**
```csharp
public class TeamTests
{
    [Fact]
    public void InvitePlayer_Should_Add_Invite_When_Valid()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear();

        // Act
        team.InvitePlayer(playerToInvite.Id, 7);

        // Assert
        Assert.Single(team.TeamInvites);
        Assert.Equal(playerToInvite.Id, team.TeamInvites.First().PlayerId);
    }
}
```

**Patterns:**
- Use `[Fact]` for single-case behavior tests: `tests/MercuriusAPI.Tests/TeamTests.cs`, `tests/MercuriusAPI.Tests/PlayerTests.cs`, `tests/MercuriusAPI.Tests/GameTests.cs`.
- Use `[Theory]` with `[InlineData]` for multiple input cases: score validation in `tests/MercuriusAPI.Tests/MatchTests.cs` and status validation in `tests/MercuriusAPI.Tests/GameTests.cs`.
- Prefer Arrange/Act/Assert comments in non-trivial tests. Short tests may omit comments when setup and assertion are obvious, as in `tests/MercuriusAPI.Tests/PlayerTests.cs`.
- Keep private factory helpers at the bottom of test classes: `CreateTeam`, `CreatePlayer`, `CreateMatch`, `GetFixture`.
- Current tests are domain-model unit tests. They instantiate models directly and do not boot the ASP.NET host, dependency injection container, EF Core DbContext, or HTTP pipeline.

## Mocking

**Framework:** Not detected

**Patterns:**
```csharp
private Fixture GetFixture()
{
    var fixture = new Fixture();
    fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
       .ForEach(b => fixture.Behaviors.Remove(b));
    fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

    return fixture;
}
```

**What to Mock:**
- No mocking library is configured. For pure domain behavior, create real model objects directly as in `tests/MercuriusAPI.Tests/GameTests.cs` and `tests/MercuriusAPI.Tests/PlayerTests.cs`.
- Use AutoFixture customization when EF navigation properties or abstract base classes make object construction noisy, as in `tests/MercuriusAPI.Tests/TeamTests.cs` and `tests/MercuriusAPI.Tests/MatchTests.cs`.
- Use small in-test stubs for abstract base types when behavior does not require a concrete production subtype, such as `TestParticipant` in `tests/MercuriusAPI.Tests/GameTests.cs`.

**What NOT to Mock:**
- Do not mock domain models under `src/MercuriusAPI/Models/`; tests exercise their real behavior directly.
- Do not mock xUnit assertions or AutoFixture-generated entities. Adjust factories/customizations instead.
- Service and endpoint tests are not present. If added, prefer EF Core test infrastructure or explicit fakes over introducing a mocking framework unless the new service behavior needs collaborator verification.

## Fixtures and Factories

**Test Data:**
```csharp
private Match CreateMatch(GameFormat format = GameFormat.BestOf1)
{
    var fixture = GetFixture();
    fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Player)));
    fixture.Customizations.Add(new TypeRelay(typeof(Participant), typeof(Team)));
    fixture.Customize(new MatchParticipantCustomization());
    var match = fixture.Build<Match>()
        .Without(m => m.Winner)
        .Without(m => m.Loser)
        .Without(m => m.WinnerId)
        .Without(m => m.LoserId)
        .With(m => m.Format, format)
        .Create();

    return match;
}
```

**Location:**
- Test-specific factories are private methods in the relevant test class: `tests/MercuriusAPI.Tests/TeamTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`, `tests/MercuriusAPI.Tests/GameTests.cs`.
- Shared AutoFixture customization lives in `tests/MercuriusAPI.Tests/Customizations/MatchParticipantCustomization.cs`.
- No global fixture directory, test data files, or test database seed fixtures are present.

## Coverage

**Requirements:** None enforced

**View Coverage:**
```bash
dotnet test LAN.API.sln --collect:"XPlat Code Coverage"
```

- `coverlet.collector` `6.0.0` is referenced in `tests/MercuriusAPI.Tests/Mercurius.LAN.API.Tests.csproj`, so coverage collection is available through `dotnet test`.
- No coverage threshold, coverage report generation config, or CI coverage gate is present in the repository.
- Coverage signal is concentrated on domain models: `Team`, `Player`, `Match`, and `Game`. There are no tests for `src/MercuriusAPI/Endpoints/`, `src/MercuriusAPI/Services/`, `src/MercuriusAPI/Middleware/`, `src/MercuriusAPI/Data/`, authentication, file handling, migrations, or Swagger configuration.

## Test Types

**Unit Tests:**
- Main test type. Current tests exercise domain object behavior synchronously and directly.
- Files: `tests/MercuriusAPI.Tests/TeamTests.cs`, `tests/MercuriusAPI.Tests/PlayerTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`, `tests/MercuriusAPI.Tests/GameTests.cs`.
- Scope includes constructor defaults, update methods, status transitions, score validation, invite rules, and exception behavior.

**Integration Tests:**
- Not used.
- No `WebApplicationFactory`, `TestServer`, test database provider, in-memory EF Core setup, or HTTP client tests are configured.
- Service methods that depend on `MercuriusDBContext` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`, `src/MercuriusAPI/Services/PlayerServices/PlayerService.cs`, and `src/MercuriusAPI/Services/GameServices/GameServices.cs` are not currently covered by integration tests.

**E2E Tests:**
- Not used.
- No Playwright, Selenium, Newman, k6, or external API test runner is configured.

## Common Patterns

**Async Testing:**
```csharp
[Fact]
public async Task ServiceMethod_Should_Return_Dto_When_Valid()
{
    var result = await service.CreateTeamAsync(dto, captain);

    Assert.Equal(dto.Name, result.Name);
}
```

- No async tests are currently present. Use `async Task` xUnit tests when adding service or endpoint tests for methods such as `CreateTeamAsync` in `src/MercuriusAPI/Services/TeamServices/TeamService.cs`.
- Do not block on async calls with `.Result` or `.Wait()` in tests.

**Error Testing:**
```csharp
var ex = Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(p1Score, p2Score));
Assert.Equal("Scores cannot be negative", ex.Message);
```

- Use `Assert.Throws<T>` for domain exception paths, as in `tests/MercuriusAPI.Tests/TeamTests.cs`, `tests/MercuriusAPI.Tests/MatchTests.cs`, and `tests/MercuriusAPI.Tests/GameTests.cs`.
- Assert exception messages only when the message is part of the observable behavior being protected. `tests/MercuriusAPI.Tests/MatchTests.cs` checks specific messages for score validation.

---

*Testing analysis: 2026-04-29*
