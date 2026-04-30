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

    public GetPlacementDTO(Placement placement, ParticipantType participantType)
    {
        Place = placement.Place;
        switch (participantType)
        {
            case ParticipantType.Player:
                Participants = placement.Participants.Select(p => new GetParticipantUserDTO((User)p)).ToList();
                break;
            case ParticipantType.Team:
                Participants = placement.Participants.Select(t => new GetTeamDTO((Team)t)).ToList();
                break;
        }
    }
}

