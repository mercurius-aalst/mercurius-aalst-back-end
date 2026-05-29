using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests.Contracts;

public class MatchContractTests
{
    [Fact]
    public void GetMatchDto_ContainsExpectedContractFields()
    {
        var game = FixtureBuilders.CreateGame(mode: ParticipationMode.Individual);
        var match = FixtureBuilders.CreateMatch(game, round: 2, matchNumber: 4);
        match.UserParticipant1Id = Guid.NewGuid();
        match.UserParticipant2Id = Guid.NewGuid();

        var dto = new GetMatchDTO(match);
        var json = JsonContractAssertions.SerializeToElement(dto);

        JsonContractAssertions.AssertHasProperty(json, "id");
        JsonContractAssertions.AssertHasProperty(json, "gameId");
        JsonContractAssertions.AssertHasProperty(json, "roundNumber");
        JsonContractAssertions.AssertHasProperty(json, "matchNumber");
        JsonContractAssertions.AssertHasProperty(json, "format");
        JsonContractAssertions.AssertHasProperty(json, "bracketType");
        JsonContractAssertions.AssertHasProperty(json, "participationMode");
    }

    [Fact]
    public void GetMatchDto_DoesNotEmbedParticipantObjects()
    {
        var game = FixtureBuilders.CreateGame(mode: ParticipationMode.Team);
        var match = FixtureBuilders.CreateMatch(game);
        var dto = new GetMatchDTO(match);
        var json = JsonContractAssertions.SerializeToElement(dto);

        JsonContractAssertions.AssertDoesNotHaveProperty(json, "userParticipant1");
        JsonContractAssertions.AssertDoesNotHaveProperty(json, "userParticipant2");
        JsonContractAssertions.AssertDoesNotHaveProperty(json, "teamParticipant1");
        JsonContractAssertions.AssertDoesNotHaveProperty(json, "teamParticipant2");
    }
}
