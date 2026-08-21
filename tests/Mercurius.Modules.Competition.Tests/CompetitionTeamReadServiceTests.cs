using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Competition.Tests;

public sealed class CompetitionTeamReadServiceTests
{
    [Fact]
    public async Task IsTeamLogoReferencedAsync_UsesExactHistoricalSnapshotMatchWithoutTrackingEntities()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateGame();
        dbContext.Set<Game>().Add(game);
        dbContext.Set<TournamentRegistration>().AddRange(
            CreateTeamRegistration(game, "images/Team-Logo.webp"),
            CreateTeamRegistration(game, "images/another-logo.webp"));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var service = new CompetitionTeamReadService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext));

        var exactMatch = await service.IsTeamLogoReferencedAsync("images/Team-Logo.webp");
        var casingMismatch = await service.IsTeamLogoReferencedAsync("images/team-logo.webp");
        var absent = await service.IsTeamLogoReferencedAsync("images/missing-logo.webp");

        Assert.True(exactMatch);
        Assert.False(casingMismatch);
        Assert.False(absent);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task IsTeamLogoReferencedAsync_WhenCanceled_PropagatesCancellation()
    {
        await using var dbContext = CreateDbContext();
        var service = new CompetitionTeamReadService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.IsTeamLogoReferencedAsync("images/team-logo.webp", cancellationSource.Token));
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static Game CreateGame()
    {
        return new Game(
            "Historical Logo Cup",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf3,
            ParticipationMode.Team,
            5)
        {
            Id = Guid.NewGuid()
        };
    }

    private static TournamentRegistration CreateTeamRegistration(Game game, string logoUrl)
    {
        return new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = Guid.NewGuid(),
            RegisteredByUsernameAtRegistration = "captain",
            TeamId = Guid.NewGuid(),
            TeamNameAtRegistration = "Historical Team",
            TeamLogoUrlAtRegistration = logoUrl
        };
    }
}
