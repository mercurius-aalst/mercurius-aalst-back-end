
using Mercurius.Modules.Teams.Domain;

namespace Mercurius.Modules.Teams.DTOs;

public class GetTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<TeamPublicUserDTO> Members { get; set; } = [];

    public GetTeamDTO()
    {
    }

    public GetTeamDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        LogoUrl = team.LogoUrl;
        Members = team.Members.Select(member => new TeamPublicUserDTO(member));
        CaptainUserId = team.CaptainUserId ?? Guid.Empty;
    }
}

