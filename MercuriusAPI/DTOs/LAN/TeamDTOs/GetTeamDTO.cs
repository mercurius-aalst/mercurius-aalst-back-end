using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.TeamDTOs
{
    public class GetTeamDTO: GetParticipantDTO
    {
        public string Name { get; set; }
        public int CaptainId { get; set; }
        public IEnumerable<GetTeamPlayerDTO> Players { get; set; } = [];
        public GetTeamDTO(Team team)
        {
            Id = team.Id;
            Name = team.Name;
            Players = team.Players.Select(p => new GetTeamPlayerDTO(p));
            CaptainId = team.CaptainId;
        }
    }
}
