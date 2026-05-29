using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class GetPublicTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
    public IEnumerable<GetPublicTeamMemberDTO> Members { get; set; } = [];

    public GetPublicTeamDTO()
    {
    }

    public GetPublicTeamDTO(Team team, bool includePlatformIds = false)
    {
        Id = team.Id;
        Name = team.Name;
        CaptainUserId = team.CaptainUserId;
        Members = team.Members.Select(member => new GetPublicTeamMemberDTO(member, includePlatformIds)).ToList();
    }

    public GetPublicTeamDTO(GetTeamDTO team, bool includePlatformIds = false)
    {
        Id = team.Id;
        Name = team.Name;
        CaptainUserId = team.CaptainUserId;
        Members = team.Members.Select(member => new GetPublicTeamMemberDTO(member, includePlatformIds)).ToList();
    }
}
