using MercuriusAPI.DTOs.LAN.ParticipantDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.TeamDTOs;

public class GetTeamDTO : GetParticipantDTO
{
    public string Name { get; set; }
    public int CaptainId { get; set; }
    public IEnumerable<GetTeamPlayerDTO> Players { get; set; } = [];
    public IEnumerable<TeamInviteDTO> TeamInvites { get; set; } = [];
    public GetTeamDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        Players = team.Players.Select(p => new GetTeamPlayerDTO(p));
        TeamInvites = team.TeamInvites.Where(i => i.Status == TeamInviteStatus.Pending).Select(i => new TeamInviteDTO(i));
        CaptainId = team.CaptainId;
        Type = ParticipantType.Team;
    }
}
