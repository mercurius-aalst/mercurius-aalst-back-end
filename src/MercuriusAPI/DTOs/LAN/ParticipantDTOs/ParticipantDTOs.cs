using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;
using System.Text.Json.Serialization;

namespace MercuriusAPI.DTOs.LAN.ParticipantDTOs
{
    [JsonDerivedType(typeof(GetPlayerDTO))]
    [JsonDerivedType(typeof(GetTeamDTO))]
    public abstract class GetParticipantDTO
    {
        [JsonPropertyName("$type")]

        public ParticipantType Type { get; set; }
        public int Id { get; set; }
    }
}
