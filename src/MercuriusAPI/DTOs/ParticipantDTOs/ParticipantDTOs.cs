using Mercurius.LAN.API.DTOs.TeamDTOs;
using System.Text.Json.Serialization;

namespace Mercurius.LAN.API.DTOs.ParticipantDTOs;

[JsonDerivedType(typeof(GetParticipantUserDTO))]
[JsonDerivedType(typeof(GetTeamDTO))]
public abstract class GetParticipantDTO
{
    public int Id { get; set; }
}

