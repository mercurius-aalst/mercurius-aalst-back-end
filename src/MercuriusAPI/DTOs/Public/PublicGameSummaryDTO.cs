using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.Public;

public class PublicGameSummaryDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public string? ImageUrl { get; set; }
    public string RegisterFormUrl { get; set; } = string.Empty;
    public GetGameSponsorPlacementDTO? SponsorPlacement { get; set; }
    public IEnumerable<PublicParticipantDTO> Users { get; set; } = [];
    public IEnumerable<PublicTeamDTO> Teams { get; set; } = [];
}
