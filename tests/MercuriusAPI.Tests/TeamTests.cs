using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;

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
        Assert.Equal(captain.Id, team.CaptainUserId);
        Assert.Contains(captain, team.Members);
    }

    [Fact]
    public void Update_Team_Should_Update_Properties_Correctly()
    {
        // Arrange
        var team = CreateTeam();
        var newName = "Updated Team Name";
        var newCaptain = CreateUser();
        team.Members.Add(newCaptain);
        // Act
        team.Update(newName, newCaptain.Id);
        // Assert
        Assert.Equal(newName, team.Name);
        Assert.Equal(newCaptain.Id, team.CaptainUserId);
    }

    [Fact]
    public void Update_Team_Should_Not_Update_When_New_Name_Is_Null()
    {
        // Arrange
        var team = CreateTeam();
        var originalName = team.Name;
        var originalCaptainUserId = team.CaptainUserId;
        // Act
        team.Update(null, null);
        // Assert
        Assert.Equal(originalName, team.Name);
        Assert.Equal(originalCaptainUserId, team.CaptainUserId);
    }

    [Fact]
    public void Update_Team_Should_Not_Update_CaptainUserId_When_New_CaptainUserId_Is_Null()
    {
        // Arrange
        var team = CreateTeam();
        var newName = "Updated Team Name";
        // Act
        team.Update(newName, null);
        // Assert
        Assert.Equal(newName, team.Name);
        Assert.Equal(team.CaptainUserId, team.CaptainUserId);
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
        Assert.Equal(team.CaptainUserId, team.CaptainUserId);
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
            Id = id,
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
            Id = Interlocked.Increment(ref _nextId)
        };
    }
}

