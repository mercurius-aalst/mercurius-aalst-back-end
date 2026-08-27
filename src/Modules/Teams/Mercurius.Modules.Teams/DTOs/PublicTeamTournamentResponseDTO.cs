namespace Mercurius.Modules.Teams.DTOs;

internal sealed class PublicTeamTournamentResponseDTO
{
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
