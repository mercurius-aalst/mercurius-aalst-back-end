using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using System.Text.Json.Serialization;

namespace MercuriusAPI.DTOs.LAN.ParticipantDTOs
{
    [JsonDerivedType(typeof(GetPlayerDTO))]
    [JsonDerivedType(typeof(GetTeamDTO))]
    public abstract class GetParticipantDTO
    {
        public int Id { get; set; }
    }
}
