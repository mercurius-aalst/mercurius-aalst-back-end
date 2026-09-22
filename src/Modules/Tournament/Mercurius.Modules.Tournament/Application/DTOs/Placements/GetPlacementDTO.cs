using Mercurius.Modules.Tournament.Application.DTOs.Participants;
using Mercurius.Modules.Tournament.Application.DTOs.Leaderboards;

namespace Mercurius.Modules.Tournament.Application.DTOs.Placements;

internal class GetPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<PublicUserDTO> Users { get; set; } = [];
    public IEnumerable<TeamParticipantDTO> Teams { get; set; } = [];
    public IEnumerable<LeaderboardRowDTO> LeaderboardParticipants { get; set; } = [];

    public GetPlacementDTO()
    {

    }

}

