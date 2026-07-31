using Mercurius.Modules.Competition.Application.DTOs.Participants;

namespace Mercurius.Modules.Competition.Application.DTOs.Placements;

public class GetPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<PublicUserDTO> Users { get; set; } = [];
    public IEnumerable<TeamParticipantDTO> Teams { get; set; } = [];

    public GetPlacementDTO()
    {

    }

}

