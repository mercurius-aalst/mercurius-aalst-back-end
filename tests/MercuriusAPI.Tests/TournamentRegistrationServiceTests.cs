using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.RegistrationDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Migrations;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.RegistrationServices;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.LAN.API.Tests;

public class TournamentRegistrationServiceTests
{
    [Fact]
    public void InternalTournamentRegistrationMigration_ReplacesLegacyRegistrationState()
    {
        var migration = new InternalTournamentRegistration();
        var operations = migration.UpOperations.ToList();

        Assert.Contains(operations.OfType<DropTableOperation>(), operation => operation.Name == "GameUser");
        Assert.Contains(operations.OfType<DropTableOperation>(), operation => operation.Name == "GameTeam");
        Assert.Contains(operations.OfType<DropColumnOperation>(), operation => operation.Table == "Games" && operation.Name == "RegisterFormUrl");
        Assert.Contains(operations.OfType<AddColumnOperation>(), operation => operation.Table == "Games" && operation.Name == "TeamSize");
        Assert.Contains(operations.OfType<CreateTableOperation>(), operation => operation.Name == "TournamentRegistrations");
        Assert.Contains(operations.OfType<CreateTableOperation>(), operation => operation.Name == "TournamentRegistrationRosterMembers");
    }

    [Fact]
    public async Task RegisterIndividualAsync_CreatesActiveRegistrationAndBlocksDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("solo");
        var game = CreateIndividualGame();
        dbContext.Users.Add(user);
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var registration = await service.RegisterIndividualAsync(user.Auth0UserId, game.Id);

        Assert.Equal(TournamentRegistrationStatus.Active, registration.Status);
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
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        await service.RegisterIndividualAsync(user.Auth0UserId, game.Id);

        await service.UnregisterIndividualAsync(user.Auth0UserId, game.Id);

        Assert.False(await dbContext.TournamentRegistrations.AnyAsync());
        var eligibility = await service.CheckIndividualEligibilityAsync(user.Auth0UserId, game.Id);
        Assert.True(eligibility.Eligible);
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_CreatesPendingRosterNotificationsAndConfirmingActivatesTeam()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var member = CreateUser("member");
        var team = CreateTeam(captain, member);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
        var publisher = new RecordingTeamEventPublisher();
        var service = CreateService(dbContext, publisher);

        var pending = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, member.Id]));

        Assert.Equal(TournamentRegistrationStatus.PendingConfirmation, pending.Status);
        Assert.Contains(pending.RosterMembers, roster => roster.User.Id == captain.Id && roster.ConfirmationStatus == RosterMemberConfirmationStatus.AutoConfirmed);
        var memberRoster = Assert.Single(pending.RosterMembers.Where(roster => roster.User.Id == member.Id));
        Assert.Equal(RosterMemberConfirmationStatus.Pending, memberRoster.ConfirmationStatus);
        Assert.Single(await dbContext.TeamInvites.Where(invite => invite.Purpose == TeamInvitePurpose.TournamentRosterConfirmation).ToListAsync());
        Assert.Contains(publisher.InviteEvents, evt => evt.TeamId == team.Id && evt.UserId == member.Id && evt.Status == nameof(TeamInviteStatus.Pending));

        var active = await service.ConfirmRosterAsync(member.Auth0UserId, memberRoster.Id);

        Assert.Equal(TournamentRegistrationStatus.Active, active.Status);
        Assert.Contains(active.RosterMembers, roster => roster.User.Id == member.Id && roster.ConfirmationStatus == RosterMemberConfirmationStatus.Confirmed);
    }

    [Fact]
    public async Task SubmitTeamRosterAsync_ReplacingPendingRosterDeletesOldConfirmationNotification()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("captain");
        var firstMember = CreateUser("first");
        var secondMember = CreateUser("second");
        var team = CreateTeam(captain, firstMember, secondMember);
        var game = CreateTeamGame(teamSize: 2);
        dbContext.Users.AddRange(captain, firstMember, secondMember);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);
        var firstRoster = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, firstMember.Id]));
        var firstRosterMemberId = Assert.Single(firstRoster.RosterMembers.Where(roster => roster.User.Id == firstMember.Id)).Id;

        var replacement = await service.SubmitTeamRosterAsync(captain.Auth0UserId, game.Id, new SubmitTeamRosterDTO(team.Id, [captain.Id, secondMember.Id]));

        Assert.Equal(TournamentRegistrationStatus.PendingConfirmation, replacement.Status);
        Assert.False(await dbContext.TournamentRegistrationRosterMembers.AnyAsync(member => member.Id == firstRosterMemberId));
        Assert.False(await dbContext.TeamInvites.AnyAsync(invite => invite.UserId == firstMember.Id && invite.Purpose == TeamInvitePurpose.TournamentRosterConfirmation));
        Assert.True(await dbContext.TeamInvites.AnyAsync(invite => invite.UserId == secondMember.Id && invite.Purpose == TeamInvitePurpose.TournamentRosterConfirmation));
    }

    private static TournamentRegistrationService CreateService(MercuriusDBContext dbContext, ITeamEventPublisher? publisher = null)
    {
        return new TournamentRegistrationService(dbContext, publisher ?? new NullTeamEventPublisher());
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

    private sealed class RecordingTeamEventPublisher : ITeamEventPublisher
    {
        public List<TeamInviteChangedEvent> InviteEvents { get; } = [];

        public Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status)
        {
            InviteEvents.Add(new TeamInviteChangedEvent(teamId, inviteId, affectedUserId, status));
            return Task.CompletedTask;
        }

        public Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action) => Task.CompletedTask;

        public Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId) => Task.CompletedTask;
    }
}
