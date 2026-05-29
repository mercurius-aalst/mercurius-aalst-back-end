using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.SponsorDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests.Contracts;

public class FrontendResponseContractTests
{
    [Fact]
    public void GameDetailContract_ContainsFrontEndFields()
    {
        var user = FixtureBuilders.CreateUser();
        var game = FixtureBuilders.CreateGame(mode: ParticipationMode.Individual);
        game.Id = Guid.NewGuid();
        game.RegisteredUsers.Add(user);
        game.Matches.Add(FixtureBuilders.CreateMatch(game, round: 1, matchNumber: 1));

        var dto = new GetGameDTO(game);
        var json = JsonContractAssertions.SerializeToElement(dto);

        JsonContractAssertions.AssertHasProperty(json, "id");
        JsonContractAssertions.AssertHasProperty(json, "name");
        JsonContractAssertions.AssertHasProperty(json, "status");
        JsonContractAssertions.AssertHasProperty(json, "bracketType");
        JsonContractAssertions.AssertHasProperty(json, "format");
        JsonContractAssertions.AssertHasProperty(json, "finalsFormat");
        JsonContractAssertions.AssertHasProperty(json, "participationMode");
        JsonContractAssertions.AssertHasProperty(json, "registerFormUrl");
        JsonContractAssertions.AssertHasProperty(json, "matches");
        JsonContractAssertions.AssertHasProperty(json, "users");
        AssertSchedulePropertiesWhenAvailable(json);
    }

    [Fact]
    public void SponsorContracts_ContainListDetailAndPlacementFields()
    {
        var sponsor = FixtureBuilders.CreateSponsor("Mercurius Fiber");
        sponsor.Id = 42;
        sponsor.Description = "Network partner";
        var sponsorDto = new GetSponsorDTO(sponsor);
        var sponsorJson = JsonContractAssertions.SerializeToElement(sponsorDto);

        JsonContractAssertions.AssertHasProperty(sponsorJson, "id");
        JsonContractAssertions.AssertHasProperty(sponsorJson, "name");
        JsonContractAssertions.AssertHasProperty(sponsorJson, "sponsorTier");
        JsonContractAssertions.AssertHasProperty(sponsorJson, "logoUrl");
        JsonContractAssertions.AssertHasProperty(sponsorJson, "infoUrl");
        JsonContractAssertions.AssertHasProperty(sponsorJson, "description");

        var placement = new GameSponsorPlacement
        {
            Id = 5,
            SponsorId = sponsor.Id,
            Sponsor = sponsor,
            Context = SponsorContext.TournamentPartner,
            Headline = "Presented by Mercurius Fiber",
            SupportLine = "Low-latency finals",
            DisplayOrder = 1
        };
        var placementJson = JsonContractAssertions.SerializeToElement(new GetGameSponsorPlacementDTO(placement));

        JsonContractAssertions.AssertHasProperty(placementJson, "sponsorId");
        JsonContractAssertions.AssertHasProperty(placementJson, "sponsorName");
        JsonContractAssertions.AssertHasProperty(placementJson, "sponsorTier");
        JsonContractAssertions.AssertHasProperty(placementJson, "sponsorLogoUrl");
        JsonContractAssertions.AssertHasProperty(placementJson, "context");
        JsonContractAssertions.AssertHasProperty(placementJson, "headline");
        JsonContractAssertions.AssertHasProperty(placementJson, "supportLine");
    }

    [Fact]
    public void CurrentUserContracts_ContainProfileFlowFields()
    {
        var user = FixtureBuilders.CreateUser();
        var userDto = new GetUserDTO(user);
        var currentUserJson = JsonContractAssertions.SerializeToElement(new CurrentUserProfileResponse(true, userDto));
        var availabilityJson = JsonContractAssertions.SerializeToElement(new UsernameAvailabilityResponse
        {
            Username = "playerone",
            NormalizedUsername = "playerone",
            IsAvailable = true
        });

        JsonContractAssertions.AssertHasProperty(currentUserJson, "isComplete");
        JsonContractAssertions.AssertHasProperty(currentUserJson, "user");
        JsonContractAssertions.AssertHasProperty(currentUserJson, "email");
        JsonContractAssertions.AssertHasProperty(currentUserJson, "emailVerified");
        JsonContractAssertions.AssertHasProperty(availabilityJson, "username");
        JsonContractAssertions.AssertHasProperty(availabilityJson, "normalizedUsername");
        JsonContractAssertions.AssertHasProperty(availabilityJson, "isAvailable");
        JsonContractAssertions.AssertHasProperty(availabilityJson, "reason");
    }

    [Fact]
    public void TeamContract_PreservesAdminFieldsForAuthorizedWorkflows()
    {
        var team = FixtureBuilders.CreateTeam();
        team.TeamInvites.Add(new TeamInvite
        {
            TeamId = team.Id,
            UserId = Guid.NewGuid(),
            Status = TeamInviteStatus.Pending
        });

        var json = JsonContractAssertions.SerializeToElement(new GetTeamDTO(team));

        JsonContractAssertions.AssertHasProperty(json, "id");
        JsonContractAssertions.AssertHasProperty(json, "name");
        JsonContractAssertions.AssertHasProperty(json, "captainUserId");
        JsonContractAssertions.AssertHasProperty(json, "members");
        JsonContractAssertions.AssertHasProperty(json, "teamInvites");
    }

    [Fact]
    public void FuturePublicProfileDtos_ArePrivacySafeWhenPresent()
    {
        AssertFutureDtoOmitsPrivateFields("Mercurius.LAN.API.DTOs.Auth.PublicUserProfileDTO");
        AssertFutureDtoOmitsPrivateFields("Mercurius.LAN.API.DTOs.TeamDTOs.PublicTeamProfileDTO");
        AssertFutureDtoOmitsPrivateFields("Mercurius.LAN.API.DTOs.SearchDTOs.PublicSearchResponseDTO");
    }

    private static void AssertSchedulePropertiesWhenAvailable(System.Text.Json.JsonElement json)
    {
        var gameDtoType = typeof(GetGameDTO);
        if (gameDtoType.GetProperty("PlannedStartTime") is null)
            return;

        JsonContractAssertions.AssertHasProperty(json, "plannedStartTime");
        JsonContractAssertions.AssertHasProperty(json, "averageGameDurationMinutes");
        JsonContractAssertions.AssertHasProperty(json, "roundBreakDurationMinutes");
        JsonContractAssertions.AssertHasProperty(json, "estimatedEndTime");
    }

    private static void AssertFutureDtoOmitsPrivateFields(string typeName)
    {
        var dtoType = Type.GetType($"{typeName}, Mercurius.LAN.API");
        if (dtoType is null)
            return;

        var propertyNames = dtoType.GetProperties().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Email", propertyNames);
        Assert.DoesNotContain("EmailVerified", propertyNames);
        Assert.DoesNotContain("Auth0UserId", propertyNames);
        Assert.DoesNotContain("IsDeleted", propertyNames);
        Assert.DoesNotContain("CreatedAtUtc", propertyNames);
        Assert.DoesNotContain("UpdatedAtUtc", propertyNames);
        Assert.DoesNotContain("TeamInvites", propertyNames);
    }
}
