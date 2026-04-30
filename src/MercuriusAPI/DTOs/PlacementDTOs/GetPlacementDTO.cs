using Mercurius.LAN.API.DTOs.ParticipantDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.PlacementDTOs;

public class GetPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<GetParticipantDTO> Participants { get; set; }

    public GetPlacementDTO()
    {

    }

    public GetPlacementDTO(Placement placement, ParticipationMode participationMode)
    {
        Place = placement.Place;
        switch (participationMode)
        {
            case ParticipationMode.Individual:
                Participants = placement.Participants.Select(p => new GetParticipantUserDTO((User)p)).ToList();
                break;
            case ParticipationMode.Team:
                Participants = placement.Participants.Select(t => new GetTeamDTO((Team)t)).ToList();
                break;
        }
    }
}

