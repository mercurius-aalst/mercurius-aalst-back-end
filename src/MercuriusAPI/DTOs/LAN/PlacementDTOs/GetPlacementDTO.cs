using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.PlacementDTOs
{
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
            switch(participantType)
            {
                case ParticipantType.Player:
                    Participants = placement.Participants.Select(p => new GetPlayerDTO((Player)p)).ToList();
                    break;
                case ParticipantType.Team:
                    Participants = placement.Participants.Select(t => new GetTeamDTO((Team)t)).ToList();
                    break;
            }
        }
    }
}
