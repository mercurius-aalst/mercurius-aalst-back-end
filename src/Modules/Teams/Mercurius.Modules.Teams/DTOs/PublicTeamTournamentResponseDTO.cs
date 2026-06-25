namespace Mercurius.Modules.Teams.DTOs;

public sealed class PublicTeamTournamentResponseDTO
{
    public Guid GameId { get; set; }
    public string Name { get; set; } = string.Empty;
}
