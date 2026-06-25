namespace Mercurius.Modules.Teams.DTOs;

public sealed class PublicTeamProfileResponseDTO
{
    public string TeamName { get; set; } = string.Empty;
    public string? CaptainUsername { get; set; }
    public string? LogoUrl { get; set; }
    public IReadOnlyList<PublicTeamMemberResponseDTO> Members { get; set; } = [];
    public IReadOnlyList<PublicTeamTournamentResponseDTO> Tournaments { get; set; } = [];
}
