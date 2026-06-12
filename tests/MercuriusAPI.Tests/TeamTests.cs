using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Migrations;
using Mercurius.LAN.API.Hubs;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;

namespace Mercurius.LAN.API.Tests;

public class TeamTests
{
    private static int _nextId;

    [Fact]
    public void Team_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange
        var teamName = "Test Team";
        var captain = CreateUser();
        var team = new Team(teamName, captain);
        // Act & Assert
        Assert.Equal(teamName, team.Name);
        Assert.Equal("test team", team.NormalizedName);
        Assert.Equal(captain.Id, team.CaptainUserId);
        Assert.Contains(captain, team.Members);
    }

    [Fact]
    public void UpdateName_ChangesTeamName()
    {
        var team = CreateTeam();
        var newName = "Updated Team Name";

        team.UpdateName(newName);

        Assert.Equal(newName, team.Name);
        Assert.Equal("updated team name", team.NormalizedName);
    }

    [Fact]
    public async Task CreateTeamAsync_Throws_When_Name_Exists_With_Different_Casing()
    {
        await using var dbContext = CreateDbContext();
        var existingCaptain = CreateUser();
        var newCaptain = CreateUser();

        dbContext.Users.AddRange(existingCaptain, newCaptain);
        dbContext.Teams.Add(new Team("Alpha Squad", existingCaptain) { Id = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => teamService.CreateTeamAsync(new CreateTeamDTO
        {
            Name = "alpha squad",
            CaptainUserId = newCaptain.Id
        }));

        Assert.Contains("already in use", exception.Message);
    }

    [Fact]
    public async Task UpdateTeamAsync_Throws_When_Name_Exists_With_Different_Casing()
    {
        await using var dbContext = CreateDbContext();
        var firstCaptain = CreateUser();
        var secondCaptain = CreateUser();
        var firstTeam = new Team("Alpha Squad", firstCaptain) { Id = Guid.NewGuid() };
        var secondTeam = new Team("Beta Squad", secondCaptain) { Id = Guid.NewGuid() };

        dbContext.Users.AddRange(firstCaptain, secondCaptain);
        dbContext.Teams.AddRange(firstTeam, secondTeam);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => teamService.UpdateTeamAsync(secondTeam.Id, new UpdateTeamDTO
        {
            Name = "ALPHA SQUAD"
        }));

        Assert.Contains("already in use", exception.Message);
    }

    [Fact]
    public async Task CreateTeamAsync_ThrowsValidation_When_DatabaseUniqueConstraintFails()
    {
        await using var dbContext = CreateUniqueConstraintDbContext();
        var captain = CreateUser();

        dbContext.Users.Add(captain);
        await dbContext.SaveChangesAsync();
        dbContext.ThrowTeamNameUniqueConstraint = true;

        var teamService = CreateTeamService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => teamService.CreateTeamAsync(new CreateTeamDTO
        {
            Name = "Alpha Squad",
            CaptainUserId = captain.Id
        }));

        Assert.Contains("already in use", exception.Message);
    }

    [Fact]
    public async Task GetTeamByNameAsync_ReturnsTeam_When_CasingDiffers()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var team = new Team("Alpha Squad", captain) { Id = Guid.NewGuid() };

        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        var result = await teamService.GetTeamByNameAsync("ALPHA SQUAD");

        Assert.Equal(team.Id, result.Id);
    }

    [Fact]
    public async Task SearchTeamsByNameAsync_ReturnsMatches_When_QueryCasingDiffers()
    {
        await using var dbContext = CreateDbContext();
        var alphaCaptain = CreateUser();
        var alpineCaptain = CreateUser();
        var betaCaptain = CreateUser();

        dbContext.Users.AddRange(alphaCaptain, alpineCaptain, betaCaptain);
        dbContext.Teams.AddRange(
            new Team("Alpha Squad", alphaCaptain) { Id = Guid.NewGuid() },
            new Team("Alpine Club", alpineCaptain) { Id = Guid.NewGuid() },
            new Team("Beta Squad", betaCaptain) { Id = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        var results = (await teamService.SearchTeamsByNameAsync("ALP")).ToList();

        Assert.Contains(results, team => team.Name == "Alpha Squad");
        Assert.Contains(results, team => team.Name == "Alpine Club");
        Assert.DoesNotContain(results, team => team.Name == "Beta Squad");
    }

    [Fact]
    public void NormalizeName_ThrowsValidation_When_NameIsInvalid()
    {
        Assert.Throws<ValidationException>(() => Team.NormalizeName("   "));
        Assert.Throws<ValidationException>(() => Team.NormalizeName(new string('a', 101)));
        Assert.Throws<ValidationException>(() => Team.NormalizeName("Alpha\nSquad"));
    }

    [Fact]
    public void TeamNameNormalizationMigration_BackfillsNormalizedNamesBeforeUniqueIndex()
    {
        var migration = new TeamNameNormalization();
        var operations = migration.UpOperations.ToList();

        var addColumnIndex = operations.FindIndex(operation =>
            operation is AddColumnOperation addColumn &&
            addColumn.Table == "Teams" &&
            addColumn.Name == "NormalizedName" &&
            addColumn.IsNullable);
        var backfillIndex = operations.FindIndex(operation =>
            operation is SqlOperation sqlOperation &&
            sqlOperation.Sql.Contains("lower(btrim(\"Name\"))", StringComparison.Ordinal));
        var alterColumnIndex = operations.FindIndex(operation =>
            operation is AlterColumnOperation alterColumn &&
            alterColumn.Table == "Teams" &&
            alterColumn.Name == "NormalizedName" &&
            !alterColumn.IsNullable);
        var uniqueIndexIndex = operations.FindIndex(operation =>
            operation is CreateIndexOperation createIndex &&
            createIndex.Table == "Teams" &&
            createIndex.Name == "IX_Teams_NormalizedName" &&
            createIndex.IsUnique);

        Assert.True(addColumnIndex >= 0);
        Assert.True(addColumnIndex < backfillIndex);
        Assert.True(backfillIndex < alterColumnIndex);
        Assert.True(alterColumnIndex < uniqueIndexIndex);
    }

    [Fact]
    public void UserOwnedTeamManagementMigration_AddsLogoInviteStateIndexesAndBackfills()
    {
        var migration = new UserOwnedTeamManagement();
        var operations = migration.UpOperations.ToList();

        Assert.Contains(operations, operation =>
            operation is AddColumnOperation addColumn &&
            addColumn.Table == "Teams" &&
            addColumn.Name == "LogoUrl");
        Assert.Contains(operations, operation =>
            operation is AddColumnOperation addColumn &&
            addColumn.Table == "TeamInvites" &&
            addColumn.Name == "ExpiresAt" &&
            addColumn.IsNullable);
        Assert.Contains(operations, operation =>
            operation is CreateIndexOperation createIndex &&
            createIndex.Table == "TeamInvites" &&
            createIndex.Name == "IX_TeamInvites_TeamId_UserId_Pending" &&
            createIndex.IsUnique);
        Assert.Contains(operations, operation =>
            operation is SqlOperation sqlOperation &&
            sqlOperation.Sql.Contains("WHERE \"ExpiresAt\" IS NULL", StringComparison.Ordinal));
        Assert.Contains(operations, operation =>
            operation is AlterColumnOperation alterColumn &&
            alterColumn.Table == "TeamInvites" &&
            alterColumn.Name == "ExpiresAt" &&
            !alterColumn.IsNullable);
        Assert.Contains(operations, operation =>
            operation is SqlOperation sqlOperation &&
            sqlOperation.Sql.Contains("INSERT INTO \"TeamUser\"", StringComparison.Ordinal));
    }

    

    [Fact]
    public void ChangeCaptain_ChangesCaptain_WhenUserIsTeamMember()
    {
        var team = CreateTeam();
        var newCaptain = CreateUser();
        team.Members.Add(newCaptain);

        team.ChangeCaptain(newCaptain.Id);

        Assert.Equal(newCaptain.Id, team.CaptainUserId);
    }

    [Fact]
    public void ChangeCaptain_Throws_WhenUserIsNotTeamMember()
    {
        var team = CreateTeam();
        var outsider = CreateUser();

        Assert.Throws<ValidationException>(() => team.ChangeCaptain(outsider.Id));
    }

    [Fact]
    public void RemoveMember_Should_Remove_User_From_Team()
    {
        // Arrange
        var team = CreateTeam();
        var memberToRemove = CreateUser();
        team.Members.Add(memberToRemove);
        // Act
        team.RemoveMember(memberToRemove.Id);
        // Assert
        Assert.DoesNotContain(memberToRemove, team.Members);
    }
    [Fact]
    public void RemoveMember_Should_Not_Remove_User_If_Not_In_Team()
    {
        // Arrange
        var team = CreateTeam();
        var memberToRemove = CreateUser();
        // Act & Assert
        Assert.Throws<NotFoundException>(() => team.RemoveMember(memberToRemove.Id));
    }

    [Fact]
    public void RemoveMember_Should_Not_Remove_Captain_From_Team()
    {
        // Arrange
        var team = CreateTeam();
        var captain = team.Captain!;
        team.CaptainUserId = captain.Id;
        team.Members.Add(captain);
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.RemoveMember(team.CaptainUserId!.Value));
    }

    [Fact]
    public void InviteUser_Should_Throw_When_User_Already_In_Team()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.Members.Add(userToInvite);
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.InviteUser(userToInvite.Id, 7));
    }

    [Fact]
    public void InviteUser_Should_Throw_When_Pending_Invite_Exists()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Add(new TeamInvite { UserId = userToInvite.Id, TeamId = team.Id, Status = TeamInviteStatus.Pending });
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.InviteUser(userToInvite.Id, 7));
    }

    [Fact]
    public void InviteUser_Should_Add_Invite_When_Valid()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear(); // Ensure no existing invites
        // Act
        team.InviteUser(userToInvite.Id, 7);
        // Assert
        Assert.Single(team.TeamInvites);
        Assert.Equal(userToInvite.Id, team.TeamInvites.First().UserId);
        Assert.Equal(team.Id, team.TeamInvites.First().TeamId);
        Assert.Equal(TeamInviteStatus.Pending, team.TeamInvites.First().Status);
    }

    [Fact]
    public void InviteUser_Should_Allow_Resend_When_Declined_Limit_Not_Reached()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear(); // Ensure no existing invites
        team.TeamInvites.Add(new TeamInvite
        {
            UserId = userToInvite.Id,
            TeamId = team.Id,
            Status = TeamInviteStatus.Declined,
            RespondedAt = DateTime.UtcNow.AddDays(-5) // Declined 5 days ago
        });

        var invite = team.InviteUser(userToInvite.Id, 7);

        Assert.Equal(TeamInviteStatus.Pending, invite.Status);
    }

    [Fact]
    public void InviteUser_Should_Throw_When_Declined_Limit_Reached_Too_Soon()
    {
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear();
        foreach (var index in Enumerable.Range(0, 3))
        {
            team.TeamInvites.Add(new TeamInvite
            {
                UserId = userToInvite.Id,
                TeamId = team.Id,
                Status = TeamInviteStatus.Declined,
                RespondedAt = DateTime.UtcNow.AddDays(-index - 1)
            });
        }

        Assert.Throws<ValidationException>(() => team.InviteUser(userToInvite.Id, 7));
    }

    [Fact]
    public void TeamInvite_Respond_Accept_Should_Update_Invite_Accepted()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InviteUser(userToInvite.Id, 7);

        //Have to do this manually because Actual references are handled by EF Core
        invite.Team = team;
        invite.User = userToInvite;

        // Act
        invite.Respond(true);

        // Assert
        Assert.Equal(TeamInviteStatus.Accepted, invite.Status);
        Assert.Contains(userToInvite, team.Members);
        Assert.NotNull(invite.RespondedAt);
    }

    [Fact]
    public void TeamInvite_Respond_Decline_Should_Update_Invite_Declined()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InviteUser(userToInvite.Id, 7);
        // Act
        invite.Respond(false);
        // Assert
        Assert.Equal(TeamInviteStatus.Declined, invite.Status);
        Assert.DoesNotContain(userToInvite, team.Members);
        Assert.NotNull(invite.RespondedAt);
    }

    [Fact]
    public void TeamInvite_Respond_Should_Throw_When_Invite_Is_Not_Pending()
    {
        // Arrange
        var team = CreateTeam();
        var userToInvite = CreateUser();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InviteUser(userToInvite.Id, 7);
        invite.Team = team;
        invite.User = userToInvite;
        invite.Status = TeamInviteStatus.Accepted; // Change status to Accepted
        // Act & Assert
        Assert.Throws<ValidationException>(() => invite.Respond(true));
    }

    [Fact]
    public void TeamInvite_Respond_Should_Expire_When_Invite_Is_PastExpiration()
    {
        var team = CreateTeam();
        var userToInvite = CreateUser();
        var invite = team.InviteUser(userToInvite.Id, 7);
        invite.Team = team;
        invite.User = userToInvite;
        invite.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        Assert.Throws<ValidationException>(() => invite.Respond(true));
        Assert.Equal(TeamInviteStatus.Expired, invite.Status);
        Assert.NotNull(invite.ExpiredAt);
    }

    [Fact]
    public async Task CreateCurrentUserTeamAsync_UsesCurrentUserAndEnforcesCaptainLimit()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        dbContext.Users.Add(captain);
        dbContext.Teams.AddRange(
            new Team("One", captain) { Id = Guid.NewGuid() },
            new Team("Two", captain) { Id = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.CreateCurrentUserTeamAsync(captain.Auth0UserId, new CreateTeamDTO { Name = "Three" }));
    }

    [Fact]
    public async Task InviteUserAsync_RequiresCurrentCaptain()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var outsider = CreateUser();
        var invited = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(captain, outsider, invited);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            teamService.InviteUserAsync(outsider.Auth0UserId, team.Id, invited.Id));
    }

    [Fact]
    public async Task CancelInviteAsync_MarksPendingInviteCancelled()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var invited = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(captain, invited);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);
        var invite = await teamService.InviteUserAsync(captain.Auth0UserId, team.Id, invited.Id);

        var result = await teamService.CancelInviteAsync(captain.Auth0UserId, team.Id, invite.Id);

        Assert.Equal(nameof(TeamInviteStatus.Cancelled), result.Status);
        Assert.NotNull(result.CancelledAt);
    }

    [Fact]
    public async Task RespondToInviteAsync_OnlyAllowsRecipient()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var invited = CreateUser();
        var otherUser = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(captain, invited, otherUser);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);
        var invite = await teamService.InviteUserAsync(captain.Auth0UserId, team.Id, invited.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            teamService.RespondToInviteAsync(otherUser.Auth0UserId, invite.Id, true));
    }

    [Theory]
    [InlineData(GameStatus.InProgress)]
    public async Task LeaveTeamAsync_BlocksProtectedTournamentStatuses(GameStatus status)
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        var game = new Game("Game", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 2)
        {
            Id = Guid.NewGuid(),
            Status = status
        };
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        AddTeamRegistration(dbContext, game, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.LeaveTeamAsync(member.Auth0UserId, team.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_AllowsCaptainToRemoveNonCaptainMember()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        var publisher = new RecordingTeamEventPublisher();
        var teamService = CreateTeamService(dbContext, eventPublisher: publisher);

        var result = await teamService.RemoveMemberAsync(captain.Auth0UserId, team.Id, member.Id);

        Assert.DoesNotContain(result.Members, teamMember => teamMember.Id == member.Id);
        Assert.DoesNotContain(team.Members, teamMember => teamMember.Id == member.Id);
        Assert.Contains(publisher.MembershipEvents, evt => evt.TeamId == team.Id && evt.UserId == member.Id && evt.Action == "Removed");
    }

    [Fact]
    public async Task RemoveMemberAsync_RequiresCurrentCaptain()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var outsider = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        dbContext.Users.AddRange(captain, member, outsider);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            teamService.RemoveMemberAsync(outsider.Auth0UserId, team.Id, member.Id));

        Assert.Contains(team.Members, teamMember => teamMember.Id == member.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_RejectsCaptainRemoval()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.RemoveMemberAsync(captain.Auth0UserId, team.Id, captain.Id));

        Assert.Contains(team.Members, teamMember => teamMember.Id == captain.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_BlocksInProgressTournamentRoster()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        var game = new Game("Game", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 2)
        {
            Id = Guid.NewGuid(),
            Status = GameStatus.InProgress
        };
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        AddTeamRegistration(dbContext, game, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        await dbContext.SaveChangesAsync();
        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.RemoveMemberAsync(captain.Auth0UserId, team.Id, member.Id));

        Assert.Contains(team.Members, teamMember => teamMember.Id == member.Id);
    }

    [Theory]
    [InlineData(GameStatus.Completed)]
    [InlineData(GameStatus.Canceled)]
    public async Task RemoveMemberAsync_AllowsCompletedAndCanceledTournamentRosters(GameStatus status)
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        var game = new Game("Game", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 2)
        {
            Id = Guid.NewGuid(),
            Status = status
        };
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        AddTeamRegistration(dbContext, game, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        await dbContext.SaveChangesAsync();
        var teamService = CreateTeamService(dbContext);

        await teamService.RemoveMemberAsync(captain.Auth0UserId, team.Id, member.Id);

        Assert.DoesNotContain(team.Members, teamMember => teamMember.Id == member.Id);
    }

    [Fact]
    public async Task DeleteTeamAsync_RequiresCurrentCaptain()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var outsider = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(captain, outsider);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            teamService.DeleteTeamAsync(outsider.Auth0UserId, team.Id));

        Assert.False((await dbContext.Teams.FindAsync(team.Id))!.IsDeleted);
    }

    [Theory]
    [InlineData(GameStatus.Scheduled)]
    [InlineData(GameStatus.InProgress)]
    public async Task DeleteTeamAsync_BlocksActiveTournamentStatuses(GameStatus status)
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        var game = new Game("Game", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 1)
        {
            Id = Guid.NewGuid(),
            Status = status
        };
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        AddTeamRegistration(dbContext, game, team, captain, [captain], TournamentRegistrationStatus.Active);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.DeleteTeamAsync(captain.Auth0UserId, team.Id));

        Assert.False((await dbContext.Teams.FindAsync(team.Id))!.IsDeleted);
    }

    [Fact]
    public async Task DeleteTeamAsync_SoftDeletesAndPreservesHistoricalReferences()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.LogoUrl = "/images/alpha.webp";
        team.Members.Add(member);
        var game = new Game("Completed Game", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 2)
        {
            Id = Guid.NewGuid(),
            Status = GameStatus.Completed
        };
        var placement = new Placement { Id = Guid.NewGuid(), Game = game, GameId = game.Id, Place = 1 };
        var match = new Match
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            ParticipationMode = ParticipationMode.Team,
            TeamParticipant1 = team,
            TeamParticipant1Id = team.Id
        };
        AddTeamRegistration(dbContext, game, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        placement.Teams = [team];
        var invite = new TeamInvite
        {
            Id = Guid.NewGuid(),
            Team = team,
            TeamId = team.Id,
            User = member,
            UserId = member.Id,
            Status = TeamInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.TeamInvites.Add(invite);
        dbContext.Games.Add(game);
        dbContext.Placements.Add(placement);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await teamService.DeleteTeamAsync(captain.Auth0UserId, team.Id);

        var deletedTeam = await dbContext.Teams.FindAsync(team.Id);
        Assert.NotNull(deletedTeam);
        Assert.True(deletedTeam.IsDeleted);
        Assert.NotNull(deletedTeam.DeletedAtUtc);
        Assert.StartsWith("deleted-team-", deletedTeam.Name);
        Assert.Equal(deletedTeam.Name, deletedTeam.NormalizedName);
        Assert.Null(deletedTeam.CaptainUserId);
        Assert.Null(deletedTeam.LogoUrl);
        Assert.Empty(team.Members);
        Assert.False(await dbContext.TeamInvites.AnyAsync(teamInvite => teamInvite.TeamId == team.Id));
        Assert.True(await dbContext.TournamentRegistrations.AnyAsync(registration => registration.GameId == game.Id && registration.TeamId == team.Id));
        Assert.True(await dbContext.Matches.AnyAsync(m => m.Id == match.Id && m.TeamParticipant1Id == team.Id));
        Assert.True(await dbContext.Placements.AnyAsync(p => p.Id == placement.Id && p.Teams.Any(t => t.Id == team.Id)));
    }

    [Fact]
    public async Task DeleteTeamAsync_AllowsDeletedTeamNameToBeReused()
    {
        await using var dbContext = CreateDbContext();
        var originalCaptain = CreateUser();
        var newCaptain = CreateUser();
        var team = new Team("Alpha", originalCaptain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(originalCaptain, newCaptain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();
        var teamService = CreateTeamService(dbContext);

        await teamService.DeleteTeamAsync(originalCaptain.Auth0UserId, team.Id);
        var created = await teamService.CreateCurrentUserTeamAsync(newCaptain.Auth0UserId, new CreateTeamDTO { Name = "Alpha" });

        Assert.Equal("Alpha", created.Name);
        Assert.NotEqual(team.Id, created.Id);
    }

    [Fact]
    public async Task DeletedTeams_AreHiddenFromActiveTeamSurfaces()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await teamService.DeleteTeamAsync(captain.Auth0UserId, team.Id);

        Assert.Empty(teamService.GetAllTeams());
        Assert.Empty(await teamService.SearchTeamsByNameAsync("alp"));
        await Assert.ThrowsAsync<NotFoundException>(() => teamService.GetTeamByIdAsync(team.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => teamService.GetPublicTeamProfileAsync("alpha"));
        var summary = await teamService.GetCurrentUserTeamSummaryAsync(captain.Auth0UserId);
        Assert.Empty(summary.CaptainedTeams);
        Assert.Empty(summary.MemberTeams);
    }

    [Fact]
    public async Task TransferCaptainAsync_RejectsRecipientCaptainLimit()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var target = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        team.Members.Add(target);
        dbContext.Users.AddRange(captain, target);
        dbContext.Teams.AddRange(
            team,
            new Team("Target One", target) { Id = Guid.NewGuid() },
            new Team("Target Two", target) { Id = Guid.NewGuid() });
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await Assert.ThrowsAsync<ValidationException>(() =>
            teamService.TransferCaptainAsync(captain.Auth0UserId, team.Id, target.Id));
    }

    [Fact]
    public async Task UploadTeamLogoAsync_StoresSafeReferenceAndPublicProfileReturnsIt()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.Add(captain);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext, new StubFileService("/images/team-logo.webp"));

        var result = await teamService.UploadTeamLogoAsync(captain.Auth0UserId, team.Id, CreateFormFile());
        var profile = await teamService.GetPublicTeamProfileAsync("alpha");

        Assert.Equal("/images/team-logo.webp", result.LogoUrl);
        Assert.Equal("/images/team-logo.webp", profile.LogoUrl);
    }

    [Fact]
    public async Task GetCurrentUserTeamSummaryAsync_ReturnsCaptainedMemberAndInviteViews()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var otherCaptain = CreateUser();
        var captainedTeam = new Team("Captained", captain) { Id = Guid.NewGuid() };
        var memberTeam = new Team("Member", otherCaptain) { Id = Guid.NewGuid() };
        memberTeam.Members.Add(member);
        dbContext.Users.AddRange(captain, member, otherCaptain);
        dbContext.Teams.AddRange(captainedTeam, memberTeam);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);
        await teamService.InviteUserAsync(captain.Auth0UserId, captainedTeam.Id, member.Id);

        var summary = await teamService.GetCurrentUserTeamSummaryAsync(member.Auth0UserId);

        Assert.Contains(summary.MemberTeams, team => team.Id == memberTeam.Id);
        Assert.Contains(summary.ReceivedPendingInvites, invite => invite.TeamId == captainedTeam.Id);
    }

    [Fact]
    public async Task GetCurrentUserTeamSummaryAsync_DoesNotReturnRosterConfirmationsAsTeamInvites()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var member = CreateUser();
        var team = new Team("Tournament Team", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        var game = new Game("Team Cup", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 2)
        {
            Id = Guid.NewGuid()
        };
        var rosterMember = new TournamentRegistrationRosterMember
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Team = team,
            TeamId = team.Id,
            User = member,
            UserId = member.Id,
            ConfirmationStatus = RosterMemberConfirmationStatus.Pending
        };
        dbContext.Users.AddRange(captain, member);
        dbContext.Teams.Add(team);
        dbContext.Games.Add(game);
        dbContext.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.PendingConfirmation,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            RosterMembers = [rosterMember]
        });
        dbContext.TournamentRosterConfirmationNotifications.Add(new TournamentRosterConfirmationNotification
        {
            Id = Guid.NewGuid(),
            RosterMember = rosterMember,
            TournamentRegistrationRosterMemberId = rosterMember.Id,
            Team = team,
            TeamId = team.Id,
            User = member,
            UserId = member.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        var summary = await teamService.GetCurrentUserTeamSummaryAsync(member.Auth0UserId);

        Assert.Empty(summary.ReceivedPendingInvites);
        Assert.Empty(await teamService.GetCurrentUserInvitesAsync(member.Auth0UserId));
    }

    [Fact]
    public async Task GetCurrentUserTeamSummaryAsync_CleansUpExpiredTerminalInvites()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var invited = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        var oldInvite = new TeamInvite
        {
            Id = Guid.NewGuid(),
            Team = team,
            TeamId = team.Id,
            User = invited,
            UserId = invited.Id,
            Status = TeamInviteStatus.Declined,
            CreatedAt = DateTime.UtcNow.AddDays(-120),
            ExpiresAt = DateTime.UtcNow.AddDays(-100),
            RespondedAt = DateTime.UtcNow.AddDays(-100)
        };
        dbContext.Users.AddRange(captain, invited);
        dbContext.Teams.Add(team);
        dbContext.TeamInvites.Add(oldInvite);
        await dbContext.SaveChangesAsync();

        var teamService = CreateTeamService(dbContext);

        await teamService.GetCurrentUserTeamSummaryAsync(invited.Auth0UserId);

        Assert.False(await dbContext.TeamInvites.AnyAsync(invite => invite.Id == oldInvite.Id));
    }

    [Fact]
    public async Task TeamManagementEvents_ArePublishedForInviteMembershipAndCaptainTransfer()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser();
        var invited = CreateUser();
        var team = new Team("Alpha", captain) { Id = Guid.NewGuid() };
        dbContext.Users.AddRange(captain, invited);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var publisher = new RecordingTeamEventPublisher();
        var teamService = CreateTeamService(dbContext, eventPublisher: publisher);

        var invite = await teamService.InviteUserAsync(captain.Auth0UserId, team.Id, invited.Id);
        await teamService.RespondToInviteAsync(invited.Auth0UserId, invite.Id, true);
        await teamService.TransferCaptainAsync(captain.Auth0UserId, team.Id, invited.Id);

        Assert.Contains(publisher.InviteEvents, evt => evt.TeamId == team.Id && evt.InviteId == invite.Id && evt.Status == nameof(TeamInviteStatus.Pending));
        Assert.Contains(publisher.InviteEvents, evt => evt.TeamId == team.Id && evt.InviteId == invite.Id && evt.Status == nameof(TeamInviteStatus.Accepted));
        Assert.Contains(publisher.MembershipEvents, evt => evt.TeamId == team.Id && evt.UserId == invited.Id && evt.Action == "Joined");
        Assert.Contains(publisher.CaptainEvents, evt => evt.TeamId == team.Id && evt.NewCaptainUserId == invited.Id);
    }

    [Fact]
    public async Task SignalRTeamEventPublisher_PushesRosterConfirmationNotificationsToUserAndTeamGroups()
    {
        var teamId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var affectedUserId = Guid.NewGuid();
        var hubContext = new RecordingHubContext();
        var publisher = new SignalRTeamEventPublisher(hubContext);

        await publisher.RosterConfirmationChangedAsync(teamId, notificationId, affectedUserId, "Pending");

        var send = Assert.Single(hubContext.HubClients.Proxy.Sends);
        Assert.Equal("TournamentRosterConfirmationChanged", send.Method);
        Assert.Contains(SignalRTeamEventPublisher.GetUserGroup(affectedUserId), hubContext.HubClients.RecordedGroups);
        Assert.Contains(SignalRTeamEventPublisher.GetTeamGroup(teamId), hubContext.HubClients.RecordedGroups);
        var payload = Assert.IsType<TournamentRosterConfirmationChangedEvent>(Assert.Single(send.Args));
        Assert.Equal(teamId, payload.TeamId);
        Assert.Equal(notificationId, payload.NotificationId);
        Assert.Equal(affectedUserId, payload.UserId);
        Assert.Equal("Pending", payload.Status);
    }

    [Fact]
    public async Task FileValidationService_RejectsUnsupportedLogoContentType()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:MaxFileSizeInMB"] = "5"
            })
            .Build();
        var validationService = new FileValidationService(new StubFileService("/images/team-logo.webp"), configuration);
        var file = CreateFormFile("text/plain");

        await Assert.ThrowsAsync<ValidationException>(() => validationService.SaveImageAsync(file));
    }


    private static User CreateUser()
    {
        var id = Interlocked.Increment(ref _nextId);
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|user{id}",
            Username = $"user{id}",
            Firstname = $"First{id}",
            Lastname = $"Last{id}",
            Email = $"user{id}@example.com",
            DiscordId = $"discord-{id}",
            SteamId = $"steam-{id}",
            RiotId = $"riot-{id}"
        };
    }

    private static Team CreateTeam()
    {
        var captain = CreateUser();
        return new Team("Test Team", captain)
        {
            Id = Guid.NewGuid()
        };
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static void AddTeamRegistration(
        MercuriusDBContext dbContext,
        Game game,
        Team team,
        User captain,
        IReadOnlyCollection<User> rosterMembers,
        TournamentRegistrationStatus status)
    {
        dbContext.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = status,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            RosterMembers = rosterMembers.Select(member => new TournamentRegistrationRosterMember
            {
                Id = Guid.NewGuid(),
                Game = game,
                GameId = game.Id,
                Team = team,
                TeamId = team.Id,
                User = member,
                UserId = member.Id,
                IsCaptain = member.Id == captain.Id,
                ConfirmationStatus = member.Id == captain.Id
                    ? RosterMemberConfirmationStatus.AutoConfirmed
                    : RosterMemberConfirmationStatus.Confirmed,
                ConfirmedAtUtc = DateTime.UtcNow
            }).ToList()
        });
    }

    private static UniqueConstraintDbContext CreateUniqueConstraintDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UniqueConstraintDbContext(options);
    }

    private static TeamService CreateTeamService(
        MercuriusDBContext dbContext,
        IFileService? fileService = null,
        ITeamEventPublisher? eventPublisher = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeamInvite:ResendCooldownDays"] = "7",
                ["TeamInvite:ExpirationDays"] = "14",
                ["TeamInvite:RetentionDays"] = "90",
                ["TeamInvite:DeclinedResendLimit"] = "3"
            })
            .Build();

        return new TeamService(dbContext, configuration, fileService, eventPublisher);
    }

    private static IFormFile CreateFormFile(string contentType = "image/png")
    {
        var bytes = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "logo", "logo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class StubFileService : IFileService
    {
        private readonly string _imageUrl;

        public StubFileService(string imageUrl)
        {
            _imageUrl = imageUrl;
        }

        public Task<string> SaveImageAsync(IFormFile image)
        {
            return Task.FromResult(_imageUrl);
        }

        public Task DeleteImageAsync(string? imageUrl)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTeamEventPublisher : ITeamEventPublisher
    {
        public List<TeamInviteChangedEvent> InviteEvents { get; } = [];
        public List<TournamentRosterConfirmationChangedEvent> RosterConfirmationEvents { get; } = [];
        public List<TeamMembershipChangedEvent> MembershipEvents { get; } = [];
        public List<TeamCaptainTransferredEvent> CaptainEvents { get; } = [];

        public Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status)
        {
            InviteEvents.Add(new TeamInviteChangedEvent(teamId, inviteId, affectedUserId, status));
            return Task.CompletedTask;
        }

        public Task RosterConfirmationChangedAsync(Guid teamId, Guid notificationId, Guid affectedUserId, string status)
        {
            RosterConfirmationEvents.Add(new TournamentRosterConfirmationChangedEvent(teamId, notificationId, affectedUserId, status));
            return Task.CompletedTask;
        }

        public Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action)
        {
            MembershipEvents.Add(new TeamMembershipChangedEvent(teamId, affectedUserId, action));
            return Task.CompletedTask;
        }

        public Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId)
        {
            CaptainEvents.Add(new TeamCaptainTransferredEvent(teamId, newCaptainUserId));
            return Task.CompletedTask;
        }
    }

    private sealed class UniqueConstraintDbContext(DbContextOptions<MercuriusDBContext> options) : MercuriusDBContext(options)
    {
        public bool ThrowTeamNameUniqueConstraint { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowTeamNameUniqueConstraint)
                throw new DbUpdateException(
                    "Unique constraint violation.",
                    new InvalidOperationException("IX_Teams_NormalizedName"));

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class RecordingHubContext : IHubContext<TeamManagementHub>
    {
        public RecordingHubClients HubClients { get; } = new();
        IHubClients IHubContext<TeamManagementHub>.Clients => HubClients;
        public IGroupManager Groups { get; } = new NoopGroupManager();
    }

    private sealed class RecordingHubClients : IHubClients
    {
        public RecordingClientProxy Proxy { get; } = new();
        public IReadOnlyList<string> RecordedGroups { get; private set; } = [];

        public IClientProxy All => Proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Client(string connectionId) => Proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
        public IClientProxy Group(string groupName)
        {
            RecordedGroups = [groupName];
            return Proxy;
        }
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            RecordedGroups = groupNames;
            return Proxy;
        }
        public IClientProxy User(string userId) => Proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        public List<RecordedSend> Sends { get; } = [];

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Sends.Add(new RecordedSend(method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoopGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record RecordedSend(string Method, object?[] Args);
}

