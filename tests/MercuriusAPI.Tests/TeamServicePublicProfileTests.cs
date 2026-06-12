using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Mercurius.LAN.API.Tests;

public class TeamServicePublicProfileTests
{
    [Fact]
    public async Task GetPublicTeamProfileAsync_ReturnsProfile_ForCaseInsensitiveLookup()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("CaptainMerc");
        var team = new Team("Mercury Wolves", captain) { Id = Guid.NewGuid() };
        team.Members.Add(CreateUser("zeta"));
        team.Members.Add(CreateUser("Alpha"));
        dbContext.Teams.Add(team);

        var tournament = CreateGame(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alpha Cup");
        dbContext.Games.Add(tournament);
        AddActiveTeamRegistration(dbContext, tournament, team, captain);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var profile = await service.GetPublicTeamProfileAsync("  mErCuRy WoLvEs  ");

        Assert.Equal("Mercury Wolves", profile.TeamName);
        Assert.Equal("CaptainMerc", profile.CaptainUsername);
        Assert.Equal(["Alpha", "CaptainMerc", "zeta"], profile.Members.Select(member => member.Username).ToList());
    }

    [Fact]
    public async Task GetPublicTeamProfileAsync_Throws_WhenTeamDoesNotExist()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicTeamProfileAsync("missing-team"));
    }

    [Fact]
    public async Task GetPublicTeamProfileAsync_ValidatesAndNormalizesTeamNameInput()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.GetPublicTeamProfileAsync("   "));
        Assert.Equal("Team name is required.", exception.Message);
    }

    [Fact]
    public async Task GetPublicTeamProfileAsync_OmitsPrivateAndInviteData()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("CaptainOne");
        var team = new Team("Privacy Squad", captain) { Id = Guid.NewGuid() };
        team.Members.Add(CreateUser("Bravo"));
        team.Members.Add(CreateUser(null));
        team.Members.Add(CreateUser(" "));
        team.TeamInvites.Add(new TeamInvite
        {
            TeamId = team.Id,
            UserId = Guid.NewGuid(),
            Status = TeamInviteStatus.Pending
        });
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var profile = await service.GetPublicTeamProfileAsync("privacy squad");

        Assert.Equal(["Bravo", "CaptainOne"], profile.Members.Select(member => member.Username).ToList());
        Assert.Null(typeof(PublicTeamProfileDTO).GetProperty("TeamInvites"));
        Assert.Equal(["Username"], typeof(PublicTeamMemberDTO).GetProperties().Select(property => property.Name).ToArray());
        Assert.Null(typeof(PublicTeamMemberDTO).GetProperty("Email"));
    }

    [Fact]
    public async Task GetPublicTeamProfileAsync_ReturnsRegisteredTournamentsInStableOrder()
    {
        await using var dbContext = CreateDbContext();
        var captain = CreateUser("CaptainTournament");
        var team = new Team("Tournament Squad", captain) { Id = Guid.NewGuid() };
        dbContext.Teams.Add(team);

        var alphaOne = CreateGame(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alpha Cup");
        var alphaTwo = CreateGame(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Alpha Cup");
        var zeta = CreateGame(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Zeta Clash");

        var otherTeam = new Team("Other Team", CreateUser("CaptainOther")) { Id = Guid.NewGuid() };
        var hiddenTournament = CreateGame(Guid.Parse("00000000-0000-0000-0000-000000000004"), "Aardvark Cup");

        dbContext.Teams.Add(otherTeam);
        dbContext.Games.AddRange(alphaOne, alphaTwo, zeta, hiddenTournament);
        AddActiveTeamRegistration(dbContext, alphaOne, team, captain);
        AddActiveTeamRegistration(dbContext, alphaTwo, team, captain);
        AddActiveTeamRegistration(dbContext, zeta, team, captain);
        AddActiveTeamRegistration(dbContext, hiddenTournament, otherTeam, otherTeam.Captain!);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var profile = await service.GetPublicTeamProfileAsync("tournament squad");

        Assert.Equal(
            [
                "00000000-0000-0000-0000-000000000001",
                "00000000-0000-0000-0000-000000000002",
                "00000000-0000-0000-0000-000000000003"
            ],
            profile.Tournaments.Select(tournament => tournament.GameId.ToString()).ToList());

        Assert.Equal(["Alpha Cup", "Alpha Cup", "Zeta Clash"], profile.Tournaments.Select(tournament => tournament.Name).ToList());
    }

    private static TeamService CreateService(MercuriusDBContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeamInvite:ResendCooldownDays"] = "7"
            })
            .Build();

        return new TeamService(dbContext, configuration);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static User CreateUser(string? username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{Guid.NewGuid():N}",
            Username = username,
            Firstname = "First",
            Lastname = "Last",
            Email = "user@example.com",
            DiscordId = "discord",
            SteamId = "steam",
            RiotId = "riot"
        };
    }

    private static Game CreateGame(Guid id, string name)
    {
        return new Game(
            name,
            BracketType.SingleElimination,
            GameFormat.BestOf3,
            GameFormat.BestOf5,
            ParticipationMode.Team,
            5)
        {
            Id = id
        };
    }

    private static void AddActiveTeamRegistration(MercuriusDBContext dbContext, Game game, Team team, User captain)
    {
        dbContext.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            RosterMembers =
            [
                new TournamentRegistrationRosterMember
                {
                    Id = Guid.NewGuid(),
                    Game = game,
                    GameId = game.Id,
                    Team = team,
                    TeamId = team.Id,
                    User = captain,
                    UserId = captain.Id,
                    IsCaptain = true,
                    ConfirmationStatus = RosterMemberConfirmationStatus.AutoConfirmed
                }
            ]
        });
    }
}
