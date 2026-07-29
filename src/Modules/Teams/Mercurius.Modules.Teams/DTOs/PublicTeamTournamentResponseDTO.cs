namespace Mercurius.Modules.Teams.DTOs;

internal sealed class PublicTeamTournamentResponseDTO
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
}
