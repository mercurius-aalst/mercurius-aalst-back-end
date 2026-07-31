using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Application;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.LAN.API.Migrations;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Infrastructure;
using Mercurius.Modules.Teams.Services;
using Mercurius.Modules.Identity;
using Mercurius.Modules.Sponsorship.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Platform.Eventing;
using CompetitionRegistrationStatus = Mercurius.Modules.Competition.Contracts.TournamentRegistrationStatus;
using CompetitionRosterStatus = Mercurius.Modules.Competition.Contracts.RosterMemberConfirmationStatus;

namespace Mercurius.LAN.API.Tests;

public class TournamentRegistrationServiceTests
{

    [Fact]
    public async Task RegisterIndividualAsync_CreatesActiveRegistrationAndBlocksDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("solo");
        var game = CreateIndividualGame();
        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var registration = await service.RegisterIndividualAsync(user.Auth0UserId, game.Id);

        Assert.Equal(CompetitionRegistrationStatus.Active, registration.Status);
        Assert.Equal(user.Id, registration.User!.Id);
        var duplicate = await Assert.ThrowsAsync<ValidationException>(() => service.RegisterIndividualAsync(user.Auth0UserId, game.Id));
        Assert.Contains("duplicate_participation", duplicate.Message);
    }

    [Fact]
    public async Task UnregisterIndividualAsync_RemovesUserFromActiveParticipation()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("solo");
        var game = CreateIndividualGame();
        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        await service.RegisterIndividualAsync(user.Auth0UserId, game.Id);

        await service.UnregisterIndividualAsync(user.Auth0UserId, game.Id);

        Assert.False(await dbContext.Set<TournamentRegistration>().AnyAsync());
        var eligibility = await service.CheckIndividualEligibilityAsync(user.Auth0UserId, game.Id);
        Assert.True(eligibility.Eligible);
    }

    [Fact]
    public async Task UnregisterIndividualAsync_RejectsStartedTournament()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("solo");
        var game = CreateIndividualGame();
        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        await service.RegisterIndividualAsync(user.Auth0UserId, game.Id);
        game.Status = GameStatus.InProgress;
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UnregisterIndividualAsync(user.Auth0UserId, game.Id));

        Assert.Contains("scheduled", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(await dbContext.Set<TournamentRegistration>().AnyAsync(registration => registration.GameId == game.Id && registration.UserId == user.Id));
    }

    [Fact]
    public async Task RegisterIndividualAsync_BlocksPendingRosterParticipation()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var teamGame = CreateTeamGame(teamSize: 2);
        var individualGame = CreateIndividualGame();
        individualGame.Id = teamGame.Id;
        individualGame.ParticipationMode = ParticipationMode.Individual;
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(individualGame);
        AddTeamRegistration(dbContext, individualGame, team, captain, [captain, member], TournamentRegistrationStatus.PendingConfirmation);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.RegisterIndividualAsync(member.Auth0UserId, individualGame.Id));

        Assert.Contains("duplicate_participation", exception.Message);
    }

    [Fact]
    public async Task RegistrationMutations_AreSpecificToTournamentParticipationMode()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var individualGame = CreateIndividualGame();
        var teamGame = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().AddRange(individualGame, teamGame);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var individualOnTeam = await Assert.ThrowsAsync<ValidationException>(() => service.RegisterIndividualAsync(captain.Auth0UserId, teamGame.Id));
        Assert.Contains("not_individual_tournament", individualOnTeam.Message);

        var teamOnIndividualEligibility = await service.CheckTeamEligibilityAsync(captain.Auth0UserId, individualGame.Id, team.Id);
        Assert.False(teamOnIndividualEligibility.Eligible);
        Assert.Contains("not_team_tournament", teamOnIndividualEligibility.ReasonCodes);

        var teamOnIndividual = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SubmitTeamRosterAsync(captain.Auth0UserId, individualGame.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id])));
        Assert.Contains("not_team_tournament", teamOnIndividual.Message);
    }

    [Fact]
    public async Task GetCurrentUserStateAsync_DoesNotOfferIndividualRegistrationForTeamTournament()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("captain");
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var state = await service.GetCurrentUserStateAsync(user.Auth0UserId, game.Id);

        Assert.False(state.CanRegisterIndividual);
    }

    [Fact]
    public async Task GetCurrentUserStateAsync_ReturnsPendingConfirmationAndCaptainManagedRegistration()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var pending = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id]));

        var memberState = await service.GetCurrentUserStateAsync(member.Auth0UserId, game.Id);
        var captainState = await service.GetCurrentUserStateAsync(captain.Auth0UserId, game.Id);

        Assert.True(memberState.CanConfirmRoster);
        Assert.NotNull(memberState.PendingRosterConfirmation);
        Assert.Equal(CompetitionRosterStatus.Pending, memberState.PendingRosterConfirmation.ConfirmationStatus);
        Assert.False(memberState.CanRegisterIndividual);
        Assert.Contains(captainState.CaptainManagedRegistrations, registration => registration.Id == pending.Id);
        Assert.True(captainState.CanUnregister);
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_PublishesPendingRosterEventsAndConfirmingActivatesTeam()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var publisher = CompetitionTestSupport.CreateRealtimePublisher();
        var service = CreateService(dbContext, publisher);

        var pending = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id]));

        Assert.Equal(CompetitionRegistrationStatus.PendingConfirmation, pending.Status);
        Assert.Contains(pending.RosterMembers, roster => roster.User.Id == captain.Id && roster.ConfirmationStatus == CompetitionRosterStatus.AutoConfirmed);
        var memberRoster = Assert.Single(pending.RosterMembers.Where(roster => roster.User.Id == member.Id));
        Assert.Equal(CompetitionRosterStatus.Pending, memberRoster.ConfirmationStatus);
        Assert.Empty(await dbContext.Set<TeamInvite>().ToListAsync());
        Assert.True(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(roster =>
            roster.UserId == member.Id &&
            roster.ConfirmationStatus == RosterMemberConfirmationStatus.Pending));
        Assert.Contains(publisher.Events, evt => evt.TeamId == team.Id && evt.UserId == member.Id && evt.Status == nameof(RosterMemberConfirmationStatus.Pending));

        var active = await service.ConfirmRosterAsync(member.Auth0UserId, memberRoster.Id);

        Assert.Equal(CompetitionRegistrationStatus.Active, active.Status);
        Assert.Contains(active.RosterMembers, roster => roster.User.Id == member.Id && roster.ConfirmationStatus == CompetitionRosterStatus.Confirmed);
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_ValidatesCaptainExactSizeMembershipAndDuplicates()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var outsider = CreateUser("outsider");
        var registeredElsewhere = CreateUser("registered");
        var team = CreateTeam(captain, member, registeredElsewhere);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member, outsider, registeredElsewhere);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        AddIndividualRegistration(dbContext, game, registeredElsewhere);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var nonCaptain = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SubmitTeamRosterAsync(member.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id])));
        var missingCaptain = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [member.Id, outsider.Id])));
        var wrongSize = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id])));
        var duplicate = await Assert.ThrowsAsync<ValidationException>(() =>
            service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, registeredElsewhere.Id])));

        Assert.Contains("captain_required", nonCaptain.Message);
        Assert.Contains("captain_required", missingCaptain.Message);
        Assert.Contains("not_team_member", missingCaptain.Message);
        Assert.Contains("exact_roster_size_required", wrongSize.Message);
        Assert.Contains("duplicate_participation", duplicate.Message);
        Assert.False(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync());
    }

    [Fact]
    public async Task CheckRosterEligibilityAsync_ReturnsPerCandidateReasonCodes()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var outsider = CreateUser("outsider");
        var deleted = CreateUser("deleted");
        deleted.IsDeleted = true;
        var team = CreateTeam(captain, member);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member, outsider, deleted);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var eligibility = await service.CheckRosterEligibilityAsync(captain.Auth0UserId, game.Id, team.Id, [captain.Id, member.Id, outsider.Id, deleted.Id]);

        Assert.False(eligibility.Eligible);
        Assert.Contains("exact_roster_size_required", eligibility.ReasonCodes);
        Assert.Contains("roster_candidate_ineligible", eligibility.ReasonCodes);
        Assert.Contains(eligibility.Candidates, candidate => candidate.UserId == outsider.Id && candidate.ReasonCodes.Contains("not_team_member"));
        Assert.Contains(eligibility.Candidates, candidate => candidate.UserId == deleted.Id && candidate.ReasonCodes.Contains("user_not_found"));
        Assert.Contains(eligibility.Candidates, candidate => candidate.UserId == deleted.Id && candidate.User is null);
    }

    [Fact]
    public async Task ConfirmRosterAsync_OnlyAllowsSelectedMember()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var other = CreateUser("other");
        var team = CreateTeam(captain, member, other);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member, other);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var pending = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id]));
        var memberRoster = Assert.Single(pending.RosterMembers.Where(roster => roster.User.Id == member.Id));

        await Assert.ThrowsAsync<NotFoundException>(() => service.ConfirmRosterAsync(other.Auth0UserId, memberRoster.Id));
        Assert.True(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(roster => roster.Id == memberRoster.Id && roster.ConfirmationStatus == RosterMemberConfirmationStatus.Pending));
        var active = await service.ConfirmRosterAsync(member.Auth0UserId, memberRoster.Id);

        Assert.Equal(CompetitionRegistrationStatus.Active, active.Status);
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_PersistsRosterBeforePublishingEvents()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new ThrowingCompetitionRealtimePublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id])));

        dbContext.ChangeTracker.Clear();
        Assert.True(await dbContext.Set<TournamentRegistration>().AnyAsync(registration => registration.GameId == game.Id && registration.TeamId == team.Id && registration.Status == TournamentRegistrationStatus.PendingConfirmation));
        Assert.True(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(roster =>
            roster.TeamId == team.Id &&
            roster.UserId == member.Id &&
            roster.ConfirmationStatus == RosterMemberConfirmationStatus.Pending));
        Assert.False(await dbContext.Set<TeamInvite>().AnyAsync());
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_ReplacingPendingRosterDeletesOldPendingRoster()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var firstMember = CreateUser("first");
        var secondMember = CreateUser("second");
        var team = CreateTeam(captain, firstMember, secondMember);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, firstMember, secondMember);
        dbContext.Teams.Add(team);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var firstRoster = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, firstMember.Id]));
        var firstRosterMemberId = Assert.Single(firstRoster.RosterMembers.Where(roster => roster.User.Id == firstMember.Id)).Id;

        var replacement = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, secondMember.Id]));

        Assert.Equal(CompetitionRegistrationStatus.PendingConfirmation, replacement.Status);
        Assert.False(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(member => member.Id == firstRosterMemberId));
        Assert.True(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(member =>
            member.UserId == secondMember.Id &&
            member.TeamId == team.Id &&
            member.ConfirmationStatus == RosterMemberConfirmationStatus.Pending));
        Assert.False(await dbContext.Set<TeamInvite>().AnyAsync());
    }

    [Fact]
    public async Task CaptainUnregisterAndAdminPendingRemoval_DeletePendingRosterMembers()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var firstMember = CreateUser("first");
        var secondMember = CreateUser("second");
        var firstTeam = CreateTeam(captain, firstMember);
        var secondTeam = new Team("Team Beta", captain) { Id = Guid.NewGuid() };
        secondTeam.Members.Add(secondMember);
        var unregisterGame = CreateTeamGame(teamSize: 2);
        var adminGame = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, firstMember, secondMember);
        dbContext.Teams.AddRange(firstTeam, secondTeam);
        dbContext.Set<Game>().AddRange(unregisterGame, adminGame);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        await service.SubmitTeamRosterAsync(captain.Auth0UserId, unregisterGame.Id, new SubmitTeamRosterDTO(firstTeam.Id, [captain.Id, firstMember.Id]));
        await service.SubmitTeamRosterAsync(captain.Auth0UserId, adminGame.Id, new SubmitTeamRosterDTO(secondTeam.Id, [captain.Id, secondMember.Id]));

        await service.UnregisterTeamAsync(captain.Auth0UserId, unregisterGame.Id, firstTeam.Id);
        await service.RemoveTeamAsAdminAsync(adminGame.Id, secondTeam.Id, "invalid roster", captain.Auth0UserId);

        Assert.False(await dbContext.Set<TournamentRegistration>().AnyAsync(registration => registration.GameId == unregisterGame.Id || registration.GameId == adminGame.Id));
        Assert.False(await dbContext.Set<TournamentRegistrationRosterMember>().AnyAsync(member => member.GameId == unregisterGame.Id || member.GameId == adminGame.Id));
    }

    [Fact]
    public async Task GetAdminRegistrationsAsync_ReturnsPendingAndActiveRosterState()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var pendingMember = CreateUser("pending");
        var activeCaptain = CreateUser("active-captain");
        var activeMember = CreateUser("active");
        var pendingTeam = CreateTeam(captain, pendingMember);
        var activeTeam = new Team("Team Beta", activeCaptain) { Id = Guid.NewGuid() };
        activeTeam.Members.Add(activeMember);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, pendingMember, activeCaptain, activeMember);
        dbContext.Teams.AddRange(pendingTeam, activeTeam);
        dbContext.Set<Game>().Add(game);
        AddTeamRegistration(dbContext, game, activeTeam, activeCaptain, [activeCaptain, activeMember], TournamentRegistrationStatus.Active);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var pending = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(pendingTeam.Id, [captain.Id, pendingMember.Id]));

        var registrations = await service.GetAdminRegistrationsAsync(game.Id);

        Assert.Contains(registrations, registration =>
            registration.Id == pending.Id &&
            registration.Status == CompetitionRegistrationStatus.PendingConfirmation &&
            registration.RosterMembers.Any(member => member.ConfirmationStatus == CompetitionRosterStatus.Pending));
        Assert.Contains(registrations, registration =>
            registration.Status == CompetitionRegistrationStatus.Active &&
            registration.RosterMembers.Any(member => member.User.Id == activeMember.Id && member.ConfirmationStatus == CompetitionRosterStatus.Confirmed));
    }

    private static TournamentRegistrationService CreateService(
        MercuriusDBContext dbContext,
        ICompetitionRealtimePublisher? publisher = null,
        IModuleEventPublisher? moduleEventPublisher = null)
    {
        var identityModule = new IdentityModuleFacade(dbContext);
        var teamsModule = new TeamsModuleFacade(
            new TeamsDbContextAdapter<MercuriusDBContext>(dbContext),
            new NullTeamCompetitionReadService());

        return new TournamentRegistrationService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            identityModule,
            teamsModule,
            new CompetitionEligibilityEvaluator(new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext)),
            new RegistrationMappingContextBuilder(identityModule, teamsModule),
            new TournamentRegistrationPersistenceCoordinator(new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext)),
            new TournamentRegistrationReadModelService(
                new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
                teamsModule,
                new RegistrationMappingContextBuilder(identityModule, teamsModule),
                new CompetitionDtoMapper(
                    new RegistrationMappingContextBuilder(identityModule, teamsModule),
                    new NullSponsorshipModule())),
            new CompetitionDtoMapper(
                new RegistrationMappingContextBuilder(identityModule, teamsModule),
                new NullSponsorshipModule()),
            publisher ?? CompetitionTestSupport.CreateRealtimePublisher(),
            moduleEventPublisher ?? CompetitionTestSupport.CreateModuleEventPublisher());
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static Game CreateIndividualGame()
    {
        return new Game("Solo Cup", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Individual)
        {
            Id = Guid.NewGuid()
        };
    }

    private static Game CreateTeamGame(int teamSize)
    {
        return new Game("Team Cup", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, teamSize)
        {
            Id = Guid.NewGuid()
        };
    }

    private static Team CreateTeam(User captain, params User[] members)
    {
        var team = new Team("Team Alpha", captain) { Id = Guid.NewGuid() };
        foreach (var member in members)
            team.Members.Add(member);
        return team;
    }

    private static User CreateUser(string username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{username}",
            Username = username,
            NormalizedUsername = username,
            Firstname = "First",
            Lastname = "Last",
            Email = $"{username}@example.test"
        };
    }

    private static void AddIndividualRegistration(MercuriusDBContext dbContext, Game game, User user)
    {
        dbContext.Set<TournamentRegistration>().Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = user.Id,
            RegisteredByUsernameAtRegistration = user.Username ?? string.Empty,
            UserId = user.Id,
            UsernameAtRegistration = user.Username
        });
    }

    private static void AddTeamRegistration(
        MercuriusDBContext dbContext,
        Game game,
        Team team,
        User captain,
        IReadOnlyCollection<User> rosterMembers,
        TournamentRegistrationStatus status)
    {
        dbContext.Set<TournamentRegistration>().Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = status,
            RegisteredByUserId = captain.Id,
            RegisteredByUsernameAtRegistration = captain.Username ?? string.Empty,
            TeamId = team.Id,
            TeamNameAtRegistration = team.Name,
            TeamCaptainUserIdAtRegistration = team.CaptainUserId,
            RosterMembers = rosterMembers.Select(member => new TournamentRegistrationRosterMember
            {
                Id = Guid.NewGuid(),
                Game = game,
                GameId = game.Id,
                TeamId = team.Id,
                TeamNameAtRegistration = team.Name,
                UserId = member.Id,
                UsernameAtRegistration = member.Username ?? string.Empty,
                DisplayNameAtRegistration = member.DisplayName,
                IsCaptain = member.Id == captain.Id,
                ConfirmationStatus = member.Id == captain.Id
                    ? RosterMemberConfirmationStatus.AutoConfirmed
                    : status == TournamentRegistrationStatus.Active
                        ? RosterMemberConfirmationStatus.Confirmed
                        : RosterMemberConfirmationStatus.Pending,
                ConfirmedAtUtc = member.Id == captain.Id || status == TournamentRegistrationStatus.Active
                    ? DateTime.UtcNow
                    : null
            }).ToList()
        });
    }

    private sealed class NullSponsorshipModule : ISponsorshipModule
    {
        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default)
            => Task.FromResult<SponsorSummary?>(null);

        public Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SponsorSummary>>([]);

        public Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
            IReadOnlyCollection<GameId> gameIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(new Dictionary<GameId, SponsorPlacementSummary>());

        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(GameId gameId, CancellationToken cancellationToken = default)
            => Task.FromResult<SponsorPlacementSummary?>(null);

        public Task ReplaceSponsorPlacementAsync(GameId gameId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingCompetitionRealtimePublisher : ICompetitionRealtimePublisher
    {
        public Task RosterConfirmationChangedAsync(Guid teamId, Guid notificationId, Guid affectedUserId, string status, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("event publishing failed");
        }
    }
}
