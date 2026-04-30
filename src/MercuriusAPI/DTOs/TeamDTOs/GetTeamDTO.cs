using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class GetTeamDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CaptainUserId { get; set; }
    public IEnumerable<GetTeamUserDTO> Members { get; set; } = [];
    public IEnumerable<TeamInviteDTO> TeamInvites { get; set; } = [];

    public GetTeamDTO()
    {
    }

    public GetTeamDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        Members = team.Members.Select(p => new GetTeamUserDTO(p));
        TeamInvites = team.TeamInvites.Where(i => i.Status == TeamInviteStatus.Pending).Select(i => new TeamInviteDTO(i));
        CaptainUserId = team.CaptainUserId;
    }
}

