using AutoFixture;
using AutoFixture.Kernel;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Tests;

public class TeamTests
{
    [Fact]
    public void Team_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange
        var teamName = "Test Team";
        var captain = CreatePlayer();
        var team = new Team(teamName, captain);
        // Act & Assert
        Assert.Equal(teamName, team.Name);
        Assert.Equal(captain.Id, team.CaptainId);
        Assert.Contains(captain, team.Players);
    }

    [Fact]
    public void Update_Team_Should_Update_Properties_Correctly()
    {
        // Arrange
        var team = CreateTeam();
        var newName = "Updated Team Name";
        var newCaptain = CreatePlayer();
        team.Players.Add(newCaptain);
        // Act
        team.Update(newName, newCaptain.Id);
        // Assert
        Assert.Equal(newName, team.Name);
        Assert.Equal(newCaptain.Id, team.CaptainId);
    }

    [Fact]
    public void Update_Team_Should_Not_Update_When_New_Name_Is_Null()
    {
        // Arrange
        var team = CreateTeam();
        var originalName = team.Name;
        var originalCaptainId = team.CaptainId;
        var newCaptain = CreatePlayer();
        // Act
        team.Update(null, null);
        // Assert
        Assert.Equal(originalName, team.Name);
        Assert.Equal(originalCaptainId, team.CaptainId);
    }

    [Fact]
    public void Update_Team_Should_Not_Update_CaptainId_When_New_CaptainId_Is_Null()
    {
        // Arrange
        var team = CreateTeam();
        var newName = "Updated Team Name";
        // Act
        team.Update(newName, null);
        // Assert
        Assert.Equal(newName, team.Name);
        Assert.Equal(team.CaptainId, team.CaptainId);
    }

    [Fact]
    public void Update_Team_Should_Not_Update_When_Both_Properties_Are_Null()
    {
        // Arrange
        var team = CreateTeam();
        // Act
        team.Update(null, null);
        // Assert
        Assert.Equal(team.Name, team.Name);
        Assert.Equal(team.CaptainId, team.CaptainId);
    }

    [Fact]
    public void RemovePlayer_Should_Remove_Player_From_Team()
    {
        // Arrange
        var team = CreateTeam();
        var playerToRemove = CreatePlayer();
        team.Players.Add(playerToRemove);
        // Act
        team.RemovePlayer(playerToRemove.Id);
        // Assert
        Assert.DoesNotContain(playerToRemove, team.Players);
    }
    [Fact]
    public void RemovePlayer_Should_Not_Remove_Player_If_Not_In_Team()
    {
        // Arrange
        var team = CreateTeam();
        var playerToRemove = CreatePlayer();
        // Act & Assert
        Assert.Throws<NotFoundException>(() => team.RemovePlayer(playerToRemove.Id));
    }

    [Fact]
    public void RemovePlayer_Should_Not_Remove_Captain_From_Team()
    {
        // Arrange
        var team = CreateTeam();
        var captain = team.Captain;
        team.CaptainId = captain.Id;
        team.Players.Add(captain);
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.RemovePlayer(team.CaptainId));
    }

    [Fact]
    public void InvitePlayer_Should_Throw_When_Player_Already_In_Team()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.Players.Add(playerToInvite);
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.InvitePlayer(playerToInvite.Id, 7));
    }

    [Fact]
    public void InvitePlayer_Should_Throw_When_Pending_Invite_Exists()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Add(new TeamInvite { PlayerId = playerToInvite.Id, TeamId = team.Id, Status = TeamInviteStatus.Pending });
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.InvitePlayer(playerToInvite.Id, 7));
    }

    [Fact]
    public void InvitePlayer_Should_Add_Invite_When_Valid()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear(); // Ensure no existing invites
        // Act
        team.InvitePlayer(playerToInvite.Id, 7);
        // Assert
        Assert.Single(team.TeamInvites);
        Assert.Equal(playerToInvite.Id, team.TeamInvites.First().PlayerId);
        Assert.Equal(team.Id, team.TeamInvites.First().TeamId);
        Assert.Equal(TeamInviteStatus.Pending, team.TeamInvites.First().Status);
    }

    [Fact]
    public void InvitePlayer_Should_Throw_When_Last_Invite_Was_Declined_Too_Soon()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear(); // Ensure no existing invites
        team.TeamInvites.Add(new TeamInvite
        {
            PlayerId = playerToInvite.Id,
            TeamId = team.Id,
            Status = TeamInviteStatus.Declined,
            RespondedAt = DateTime.UtcNow.AddDays(-5) // Declined 5 days ago
        });
        // Act & Assert
        Assert.Throws<ValidationException>(() => team.InvitePlayer(playerToInvite.Id, 7));
    }

    [Fact]
    public void TeamInvite_Respond_Accept_Should_Update_Invite_Accepted()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InvitePlayer(playerToInvite.Id, 7);

        //Have to do this manually because Actual references are handled by EF Core
        invite.Team = team;
        invite.Player = playerToInvite;

        // Act
        invite.Respond(true);

        // Assert
        Assert.Equal(TeamInviteStatus.Accepted, invite.Status);
        Assert.Contains(playerToInvite, team.Players);
        Assert.NotNull(invite.RespondedAt);
    }

    [Fact]
    public void TeamInvite_Respond_Decline_Should_Update_Invite_Declined()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InvitePlayer(playerToInvite.Id, 7);
        // Act
        invite.Respond(false);
        // Assert
        Assert.Equal(TeamInviteStatus.Declined, invite.Status);
        Assert.DoesNotContain(playerToInvite, team.Players);
        Assert.NotNull(invite.RespondedAt);
    }

    [Fact]
    public void TeamInvite_Respond_Should_Throw_When_Invite_Is_Not_Pending()
    {
        // Arrange
        var team = CreateTeam();
        var playerToInvite = CreatePlayer();
        team.TeamInvites.Clear(); // Ensure no existing invites
        var invite = team.InvitePlayer(playerToInvite.Id, 7);
        invite.Team = team;
        invite.Player = playerToInvite;
        invite.Status = TeamInviteStatus.Accepted; // Change status to Accepted
        // Act & Assert
        Assert.Throws<ValidationException>(() => invite.Respond(true));
    }


    private Player CreatePlayer()
    {
        var fixture = GetFixture();
        fixture.Customizations.Add(
            new TypeRelay(
            typeof(Participant),
            typeof(Player)));
        return fixture.Create<Player>();
    }

    private Team CreateTeam()
    {
        var fixture = GetFixture();
        fixture.Customizations.Add(
            new TypeRelay(
            typeof(Participant),
            typeof(Team)));
        return fixture.Create<Team>();
    }

    private Fixture GetFixture()
    {
        var fixture = new Fixture();
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
           .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

        return fixture;
    }
}
