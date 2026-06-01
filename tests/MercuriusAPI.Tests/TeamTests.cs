using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Migrations;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.EntityFrameworkCore;
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
        var captain = team.Captain;
        team.CaptainUserId = captain.Id;
        team.Members.Add(captain);
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.RemoveMember(team.CaptainUserId));
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
    public void InviteUser_Should_Throw_When_Last_Invite_Was_Declined_Too_Soon()
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
        // Act & Assert
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


    private static User CreateUser()
    {
        var id = Interlocked.Increment(ref _nextId);
        return new User
        {
            Id = Guid.NewGuid(),
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

    private static UniqueConstraintDbContext CreateUniqueConstraintDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new UniqueConstraintDbContext(options);
    }

    private static TeamService CreateTeamService(MercuriusDBContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeamInvite:ResendCooldownDays"] = "7"
            })
            .Build();

        return new TeamService(dbContext, configuration);
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
}

