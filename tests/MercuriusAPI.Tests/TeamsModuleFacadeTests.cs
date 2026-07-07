using Mercurius.LAN.API.Data;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class TeamsModuleFacadeTests
{
    [Fact]
    public void AddTeamsModule_RegistersTeamsModuleContract()
    {
        var services = new ServiceCollection();
        services.AddDbContext<MercuriusDBContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeamInvite:ResendCooldownDays"] = "7",
                ["FileStorage:MaxFileSizeInMB"] = "2"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddTeamsModule<MercuriusDBContext>(configuration);

        using var provider = services.BuildServiceProvider();

        var module = provider.GetRequiredService<ITeamsModule>();
        var logoStorage = provider.GetRequiredService<ITeamLogoStorage>();

        Assert.IsType<TeamsModuleFacade>(module);
        Assert.IsType<TeamLogoStorage>(logoStorage);
    }

    [Fact]
    public async Task GetTeamSummaryAsync_ReturnsContractProjection()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain", "Captain", "Player");
        var team = new Team("Alpha Team", captain)
        {
            Id = Guid.NewGuid(),
            LogoUrl = "https://example.test/logo.png"
        };
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var module = CreateModule(dbContext);

        var summary = await module.GetTeamSummaryAsync(new TeamId(team.Id));

        Assert.NotNull(summary);
        Assert.Equal(team.Id, summary.Id.Value);
        Assert.Equal("Alpha Team", summary.Name);
        Assert.Equal(captain.Id, summary.CaptainUserId!.Value.Value);
        Assert.Equal("https://example.test/logo.png", summary.LogoUrl);
        Assert.False(summary.IsDeleted);
    }

    [Fact]
    public async Task GetTeamRosterSnapshotAsync_ReturnsOrderedMembersAndDisplayNames()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain", "Captain", "Player");
        var team = new Team("Roster Team", captain) { Id = Guid.NewGuid() };
        team.Members.Add(CreateUser("zeta", null, null));
        team.Members.Add(CreateUser("alpha", "Alpha", "Member"));
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var module = CreateModule(dbContext);

        var roster = await module.GetTeamRosterSnapshotAsync(new TeamId(team.Id));

        Assert.NotNull(roster);
        Assert.Equal(team.Id, roster.TeamId.Value);
        Assert.Equal(captain.Id, roster.CaptainUserId!.Value.Value);
        Assert.Equal(["alpha", "captain", "zeta"], roster.Members.Select(member => member.Username).ToList());
        Assert.Equal(["Alpha Member", "Captain Player", "zeta"], roster.Members.Select(member => member.DisplayName).ToList());
        Assert.True(roster.Members.Single(member => member.UserId.Value == captain.Id).IsCaptain);
    }

    [Fact]
    public async Task GetPublicTeamProfileAsync_UsesPublicProjectionAndCompetitionContract()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("CaptainMerc", "Captain", "Merc");
        var team = new Team("Mercury Wolves", captain)
        {
            Id = Guid.NewGuid(),
            LogoUrl = "https://example.test/wolves.png"
        };
        team.Members.Add(CreateUser("zeta", "Zeta", "Member"));
        team.Members.Add(CreateUser("Alpha", "Alpha", "Member"));
        team.Members.Add(CreateUser(null, "Hidden", "Member"));
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var tournaments = new[]
        {
            new PublicTeamTournamentSummary(new GameId(Guid.Parse("00000000-0000-0000-0000-000000000001")), "Alpha Cup")
        };
        var module = CreateModule(dbContext, tournaments);

        var profile = await module.GetPublicTeamProfileAsync("  mErCuRy WoLvEs  ");

        Assert.NotNull(profile);
        Assert.Equal("Mercury Wolves", profile.TeamName);
        Assert.Equal("CaptainMerc", profile.CaptainUsername);
        Assert.Equal("https://example.test/wolves.png", profile.LogoUrl);
        Assert.Equal(["Alpha", "CaptainMerc", "zeta"], profile.Members.Select(member => member.Username).ToList());
        Assert.Equal(tournaments, profile.Tournaments);
    }

    [Fact]
    public async Task Guards_ReturnReasonCodes_ForMissingDeletedAndNonCaptainTeams()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain", "Captain", "Player");
        var outsider = CreateUser("outsider", "Outside", "Player");
        var team = new Team("Guard Team", captain) { Id = Guid.NewGuid() };
        var deletedTeam = new Team("Deleted Team", captain) { Id = Guid.NewGuid() };
        deletedTeam.Delete(DateTime.UtcNow);
        dbContext.Users.Add(outsider);
        dbContext.Teams.AddRange(team, deletedTeam);
        await dbContext.SaveChangesAsync();

        var module = CreateModule(dbContext);

        var missing = await module.GetRegistrationEligibilityAsync(
            new TeamId(Guid.NewGuid()),
            new UserId(captain.Id),
            new GameId(Guid.NewGuid()));
        var nonCaptain = await module.GetRegistrationEligibilityAsync(
            new TeamId(team.Id),
            new UserId(outsider.Id),
            new GameId(Guid.NewGuid()));
        var deleted = await module.CanMutateMembershipAsync(new TeamId(deletedTeam.Id), new UserId(captain.Id));

        Assert.False(missing.Eligible);
        Assert.Equal(["team_not_found"], missing.ReasonCodes);
        Assert.False(nonCaptain.Eligible);
        Assert.Equal(["captain_required"], nonCaptain.ReasonCodes);
        Assert.False(deleted.CanMutate);
        Assert.Equal(["team_deleted", "captain_required"], deleted.ReasonCodes);
    }

    private static TeamsModuleFacade CreateModule(
        MercuriusDBContext dbContext,
        IReadOnlyList<PublicTeamTournamentSummary>? tournaments = null)
    {
        return new TeamsModuleFacade(
            dbContext,
            new StubTeamCompetitionReadService(tournaments ?? Array.Empty<PublicTeamTournamentSummary>()));
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static User CreateUser(string? username, string? firstname, string? lastname)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{Guid.NewGuid():N}",
            Username = username,
            NormalizedUsername = username?.ToLowerInvariant(),
            Firstname = firstname,
            Lastname = lastname,
            Email = "user@example.com"
        };
    }

    private sealed class StubTeamCompetitionReadService : ITeamCompetitionReadService
    {
        private readonly IReadOnlyList<PublicTeamTournamentSummary> _tournaments;

        public StubTeamCompetitionReadService(IReadOnlyList<PublicTeamTournamentSummary> tournaments)
        {
            _tournaments = tournaments;
        }

        public Task<IReadOnlyList<PublicTeamTournamentSummary>> GetPublicTeamTournamentsAsync(
            Guid teamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tournaments);
        }

        public Task<bool> IsUserInProtectedTournamentRosterAsync(
            Guid teamId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> IsTeamInDeleteBlockingTournamentAsync(
            Guid teamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
