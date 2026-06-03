using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class GetTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
    public IEnumerable<PublicUserDTO> Members { get; set; } = [];

    public GetTeamDTO()
    {
    }

    public GetTeamDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        Members = team.Members.Select(member => new PublicUserDTO(member));
        CaptainUserId = team.CaptainUserId;
    }
}

