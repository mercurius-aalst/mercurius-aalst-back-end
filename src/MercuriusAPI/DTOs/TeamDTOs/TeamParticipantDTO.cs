using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class TeamParticipantDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<PublicUserDTO> Members { get; set; } = [];

    public TeamParticipantDTO()
    {
    }

    public TeamParticipantDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        CaptainUserId = team.CaptainUserId ?? Guid.Empty;
        LogoUrl = team.LogoUrl;
        Members = team.Members.Select(member => new PublicUserDTO(member)).ToList();
    }
}
