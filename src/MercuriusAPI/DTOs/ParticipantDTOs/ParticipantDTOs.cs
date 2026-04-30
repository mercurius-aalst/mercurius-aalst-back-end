using Mercurius.LAN.API.DTOs.PlayerDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;
using System.Text.Json.Serialization;

namespace Mercurius.LAN.API.DTOs.ParticipantDTOs;

[JsonDerivedType(typeof(GetParticipantUserDTO))]
[JsonDerivedType(typeof(GetPlayerDTO))]
[JsonDerivedType(typeof(GetTeamDTO))]
public abstract class GetParticipantDTO
{
    [JsonPropertyName("$type")]

    public ParticipantType Type { get; set; }
    public int Id { get; set; }
}

