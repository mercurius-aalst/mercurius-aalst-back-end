using Mercurius.LAN.API.Data;
using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Application.Services;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.TestInfrastructure;
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
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([assignedAdmin, resolvingAdmin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc),
            new MatchBracketImpactAnalyzer(new TournamentDbContextAdapter<MercuriusDBContext>(dbContext)));

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
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([assignedAdmin, resolvingAdmin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc),
            new MatchBracketImpactAnalyzer(new TournamentDbContextAdapter<MercuriusDBContext>(dbContext)));

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
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new MatchService(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([admin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(nowUtc),
            new MatchBracketImpactAnalyzer(new TournamentDbContextAdapter<MercuriusDBContext>(dbContext)));

        var actionState = await service.GetMatchActionStateAsync(source.Id, admin.Auth0UserId, true);

        Assert.Equal(ContractLifecycleState.Completed, actionState.Match.LifecycleState);
        Assert.False(actionState.CanReverse);
        Assert.Equal("match_reversal_blocked", actionState.ReverseBlockedReason);
    }

    [Fact]
    public async Task ReverseAsync_FailsClosedWhenLegacyDownstreamAssignmentHasNoProvenance()
    {
        var admin = CreateUser("legacy-admin");
        var tournament = CreateTournament("Legacy provenance tournament", BracketType.SingleElimination, admin);
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        var source = CreateCompletedMatch(tournament, winner, loser);
        var downstream = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = winner,
            UserParticipant2Id = Guid.NewGuid()
        };
        source.WinnerNextMatchId = downstream.Id;
        source.WinnerNextMatch = downstream;
        tournament.Matches.Add(source);
        tournament.Matches.Add(downstream);

        await using var dbContext = CreateDbContext();
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = CreateMatchService(dbContext, admin);
        var actionState = await service.GetMatchActionStateAsync(source.Id, admin.Auth0UserId, true);

        Assert.False(actionState.CanReverse);
        Assert.Equal("match_reversal_blocked", actionState.ReverseBlockedReason);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ReverseAsync(source.Id, admin.Auth0UserId));

        Assert.Equal("match_reversal_blocked", exception.Code);
        var persistedSource = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == source.Id);
        var persistedDownstream = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == downstream.Id);
        Assert.Equal(MatchLifecycleState.Completed, persistedSource.LifecycleState);
        Assert.Equal(winner, persistedDownstream.UserParticipant1Id);
        Assert.Null(persistedDownstream.Participant1SourceMatchId);
    }

    [Fact]
    public async Task ReverseAsync_ClearsOnlyBackfilledSingleEliminationSourceSlot()
    {
        var admin = CreateUser("single-backfill-admin");
        var tournament = CreateTournament("Single elimination provenance tournament", BracketType.SingleElimination, admin);
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        var unrelated = Guid.NewGuid();
        var source = CreateCompletedMatch(tournament, winner, loser);
        var downstream = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = winner,
            Participant1SourceMatchId = source.Id,
            UserParticipant2Id = unrelated
        };
        source.WinnerNextMatchId = downstream.Id;
        source.WinnerNextMatch = downstream;
        tournament.Matches.Add(source);
        tournament.Matches.Add(downstream);

        await using var dbContext = CreateDbContext();
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = CreateMatchService(dbContext, admin);
        await service.ReverseAsync(source.Id, admin.Auth0UserId);

        var persistedSource = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == source.Id);
        var persistedDownstream = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == downstream.Id);
        Assert.Equal(MatchLifecycleState.Reversed, persistedSource.LifecycleState);
        Assert.Null(persistedDownstream.UserParticipant1Id);
        Assert.Null(persistedDownstream.Participant1SourceMatchId);
        Assert.Equal(unrelated, persistedDownstream.UserParticipant2Id);
    }

    [Fact]
    public async Task ReverseAsync_ClearsBackfilledDoubleEliminationEdgesThroughNestedGraph()
    {
        var admin = CreateUser("double-backfill-admin");
        var tournament = CreateTournament("Double elimination provenance tournament", BracketType.DoubleElimination, admin);
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();
        var unrelatedWinnerSlot = Guid.NewGuid();
        var unrelatedLoserSlot = Guid.NewGuid();
        var unrelatedNestedSlot = Guid.NewGuid();
        var source = CreateCompletedMatch(tournament, winner, loser);
        var winnerTarget = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            BracketType = BracketType.DoubleElimination,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = winner,
            Participant1SourceMatchId = source.Id,
            UserParticipant2Id = unrelatedWinnerSlot
        };
        var loserTarget = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            BracketType = BracketType.DoubleElimination,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = unrelatedLoserSlot,
            UserParticipant2Id = loser,
            Participant2SourceMatchId = source.Id,
            IsLowerBracketMatch = true
        };
        var nestedTarget = new Match
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            Tournament = tournament,
            BracketType = BracketType.DoubleElimination,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual,
            UserParticipant1Id = unrelatedNestedSlot
        };
        source.WinnerNextMatchId = winnerTarget.Id;
        source.WinnerNextMatch = winnerTarget;
        source.LoserNextMatchId = loserTarget.Id;
        source.LoserNextMatch = loserTarget;
        winnerTarget.WinnerNextMatchId = nestedTarget.Id;
        winnerTarget.WinnerNextMatch = nestedTarget;
        tournament.Matches.Add(source);
        tournament.Matches.Add(winnerTarget);
        tournament.Matches.Add(loserTarget);
        tournament.Matches.Add(nestedTarget);

        await using var dbContext = CreateDbContext();
        dbContext.AddReferencedUsers(tournament);
        dbContext.Set<TournamentAggregate>().Add(tournament);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = CreateMatchService(dbContext, admin);
        await service.ReverseAsync(source.Id, admin.Auth0UserId);

        var persistedSource = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == source.Id);
        var persistedWinnerTarget = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == winnerTarget.Id);
        var persistedLoserTarget = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == loserTarget.Id);
        var persistedNestedTarget = await dbContext.Set<Match>().AsNoTracking().SingleAsync(match => match.Id == nestedTarget.Id);
        Assert.Equal(MatchLifecycleState.Reversed, persistedSource.LifecycleState);
        Assert.Null(persistedWinnerTarget.UserParticipant1Id);
        Assert.Null(persistedWinnerTarget.Participant1SourceMatchId);
        Assert.Equal(unrelatedWinnerSlot, persistedWinnerTarget.UserParticipant2Id);
        Assert.Equal(unrelatedLoserSlot, persistedLoserTarget.UserParticipant1Id);
        Assert.Null(persistedLoserTarget.UserParticipant2Id);
        Assert.Null(persistedLoserTarget.Participant2SourceMatchId);
        Assert.Equal(unrelatedNestedSlot, persistedNestedTarget.UserParticipant1Id);
    }

    private static MercuriusDBContext CreateDbContext()
        => PostgresTestDatabase.CreateDbContext();

    private static MatchService CreateMatchService(MercuriusDBContext dbContext, User admin) =>
        new(
            new TournamentDbContextAdapter<MercuriusDBContext>(dbContext),
            TournamentTestSupport.CreateIdentityModule([admin]),
            TournamentTestSupport.CreateTeamsModule(),
            TournamentTestSupport.CreateModuleEventPublisher(),
            new FixedTimeProvider(new DateTime(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc)),
            new MatchBracketImpactAnalyzer(new TournamentDbContextAdapter<MercuriusDBContext>(dbContext)));

    private static TournamentAggregate CreateTournament(string name, BracketType bracketType, User admin) => new(
        name,
        bracketType,
        GameFormat.BestOf1,
        GameFormat.BestOf1,
        ParticipationMode.Individual)
    {
        Id = Guid.NewGuid(),
        Status = TournamentStatus.InProgress,
        AssignedAdminUserId = admin.Id
    };

    private static Match CreateCompletedMatch(TournamentAggregate tournament, Guid winner, Guid loser) => new()
    {
        Id = Guid.NewGuid(),
        TournamentId = tournament.Id,
        Tournament = tournament,
        BracketType = tournament.BracketType,
        Format = GameFormat.BestOf1,
        ParticipationMode = ParticipationMode.Individual,
        LifecycleState = MatchLifecycleState.Completed,
        UserParticipant1Id = winner,
        UserParticipant2Id = loser,
        UserWinnerId = winner,
        UserLoserId = loser,
        Participant1Score = 1,
        Participant2Score = 0
    };

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
