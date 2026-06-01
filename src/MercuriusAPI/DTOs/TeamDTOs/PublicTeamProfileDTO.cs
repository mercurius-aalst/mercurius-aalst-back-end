namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class PublicTeamProfileDTO
{
    public string TeamName { get; set; } = string.Empty;
    public string? CaptainUsername { get; set; }
    public IEnumerable<PublicTeamMemberDTO> Members { get; set; } = [];
    public IEnumerable<PublicTeamTournamentDTO> Tournaments { get; set; } = [];
}
