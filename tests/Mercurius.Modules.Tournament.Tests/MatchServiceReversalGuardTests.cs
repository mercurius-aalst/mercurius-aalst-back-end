using Mercurius.LAN.API.Data;
using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Application.Services;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ContractLifecycleState = Mercurius.Modules.Tournament.Contracts.MatchLifecycleState;

namespace Mercurius.Modules.Tournament.Tests;

public class MatchServiceReversalGuardTests
{
    [Fact]
    public async Task UnassignedGlobalAdmin_CanResolveAndIsRecordedAsTheActor()
    {
        var nowUtc = new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        var assignedAdmin = CreateUser("assigned-admin");
        var resolvingAdmin = CreateUser("resolving-admin");
        var tournament = new TournamentAggregate(
            "Resolution tournament",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual)
        {
            Id = Guid.NewGuid(),
            Status = TournamentStatus.InProgress,
            AssignedAdminUserId = assignedAdmin.Id
        };
        var assignedMatch = CreateDisputedMatch(tournament);
        var unassignedMatch = CreateDisputedMatch(tournament);
        tournament.Matches.Add(assignedMatch);
        tournament.Matches.Add(unassignedMatch);

        await using var dbContext = CreateDbContext();
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([assignedAdmin, resolvingAdmin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc));

        var actionState = await service.GetMatchActionStateAsync(
            assignedMatch.Id,
            assignedAdmin.Auth0UserId,
            isAdmin: true);

        var unassignedActionState = await service.GetMatchActionStateAsync(
            unassignedMatch.Id,
            resolvingAdmin.Auth0UserId,
            isAdmin: true);

        Assert.True(actionState.CanResolve);
        Assert.Null(actionState.ResolveBlockedReason);
        Assert.NotNull(actionState.Participant1ReportedScore1);
        Assert.NotNull(actionState.Participant2ReportedScore1);
        Assert.True(unassignedActionState.CanResolve);
        Assert.Null(unassignedActionState.ResolveBlockedReason);
        Assert.Null(unassignedActionState.Participant1ReportedScore1);
        Assert.Null(unassignedActionState.Participant2ReportedScore1);

        var nonAdminActionState = await service.GetMatchActionStateAsync(
            unassignedMatch.Id,
            resolvingAdmin.Auth0UserId,
            isAdmin: false);
        Assert.False(nonAdminActionState.CanResolve);
        Assert.Equal("admin_required", nonAdminActionState.ResolveBlockedReason);

        await service.ResolveAsync(
            assignedMatch.Id,
            assignedAdmin.Auth0UserId,
            new ResolveMatchDTO { Participant1Score = 1, Participant2Score = 0 });
        await service.ResolveAsync(
            unassignedMatch.Id,
            resolvingAdmin.Auth0UserId,
            new ResolveMatchDTO { Participant1Score = 1, Participant2Score = 0 });

        var persisted = await dbContext.Set<Match>()
            .AsNoTracking()
            .Where(candidate => candidate.Id == assignedMatch.Id || candidate.Id == unassignedMatch.Id)
            .ToDictionaryAsync(candidate => candidate.Id);
        Assert.Equal(MatchLifecycleState.Completed, persisted[assignedMatch.Id].LifecycleState);
        Assert.Equal(assignedAdmin.Id, persisted[assignedMatch.Id].ResultRecordedByUserId);
        Assert.Equal(nowUtc, persisted[assignedMatch.Id].ResultRecordedAtUtc);
        Assert.Equal(MatchLifecycleState.Completed, persisted[unassignedMatch.Id].LifecycleState);
        Assert.Equal(resolvingAdmin.Id, persisted[unassignedMatch.Id].ResultRecordedByUserId);
        Assert.Equal(nowUtc, persisted[unassignedMatch.Id].ResultRecordedAtUtc);
    }

    [Fact]
    public async Task UnassignedGlobalAdmin_RetainsForfeitAndReverseCapabilities()
    {
        var nowUtc = new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        var assignedAdmin = CreateUser("assigned-admin");
        var resolvingAdmin = CreateUser("resolving-admin");
        var tournament = new TournamentAggregate(
            "Administrative actions tournament",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual)
        {
            Id = Guid.NewGuid(),
            Status = TournamentStatus.InProgress,
            AssignedAdminUserId = assignedAdmin.Id
        };
        var readyMatch = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = Guid.NewGuid(),
            UserParticipant2Id = Guid.NewGuid()
        };
        var completedMatch = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            LifecycleState = MatchLifecycleState.Completed,
            UserParticipant1Id = Guid.NewGuid(),
            UserParticipant2Id = Guid.NewGuid(),
            UserWinnerId = Guid.NewGuid(),
            UserLoserId = Guid.NewGuid(),
            Participant1Score = 1,
            Participant2Score = 0
        };
        tournament.Matches.Add(readyMatch);
        tournament.Matches.Add(completedMatch);

        await using var dbContext = CreateDbContext();
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([assignedAdmin, resolvingAdmin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc));

        var readyState = await service.GetMatchActionStateAsync(
            readyMatch.Id,
            resolvingAdmin.Auth0UserId,
            isAdmin: true);
        var completedState = await service.GetMatchActionStateAsync(
            completedMatch.Id,
            resolvingAdmin.Auth0UserId,
            isAdmin: true);

        Assert.True(readyState.CanForceForfeit);
        Assert.Null(readyState.ForceForfeitBlockedReason);
        Assert.True(completedState.CanReverse);
        Assert.Null(completedState.ReverseBlockedReason);
    }

    [Fact]
    public async Task ActionState_BlocksReversalWhenDownstreamMatchStartedAtItsEstimatedTime()
    {
        var nowUtc = new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = "auth0|match-admin",
            Username = "match-admin"
        };
        var tournament = new TournamentAggregate(
            "Reversal tournament",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual)
        {
            Id = Guid.NewGuid(),
            Status = TournamentStatus.InProgress,
            AssignedAdminUserId = admin.Id
        };
        var source = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            LifecycleState = MatchLifecycleState.Completed,
            UserParticipant1Id = Guid.NewGuid(),
            UserParticipant2Id = Guid.NewGuid(),
            UserWinnerId = Guid.NewGuid(),
            UserLoserId = Guid.NewGuid(),
            Participant1Score = 1,
            Participant2Score = 0
        };
        var downstream = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            EstimatedStartTime = nowUtc,
            StartTime = nowUtc
        };
        source.WinnerNextMatchId = downstream.Id;
        source.WinnerNextMatch = downstream;
        tournament.Matches.Add(source);
        tournament.Matches.Add(downstream);

        await using var dbContext = CreateDbContext();
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([admin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc));

        var actionState = await service.GetMatchActionStateAsync(source.Id, admin.Auth0UserId, true);

        Assert.Equal(ContractLifecycleState.Completed, actionState.Match.LifecycleState);
        Assert.False(actionState.CanReverse);
        Assert.Equal("match_reversal_blocked", actionState.ReverseBlockedReason);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static Match CreateDisputedMatch(TournamentAggregate tournament) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournament.Id,
        Tournament = tournament,
        Format = GameFormat.BestOf1,
        ParticipationMode = ParticipationMode.Individual,
        LifecycleState = MatchLifecycleState.AdminResolutionRequired,
        UserParticipant1Id = Guid.NewGuid(),
        UserParticipant2Id = Guid.NewGuid(),
        Participant1ReportedScore1 = 1,
        Participant1ReportedScore2 = 0,
        Participant2ReportedScore1 = 0,
        Participant2ReportedScore2 = 1
    };

    private static User CreateUser(string name) => new()
    {
        Id = Guid.NewGuid(),
        Auth0UserId = $"auth0|{name}",
        Username = name
    };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
