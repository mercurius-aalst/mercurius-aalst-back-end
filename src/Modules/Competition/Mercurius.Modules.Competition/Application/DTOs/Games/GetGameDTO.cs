using Mercurius.Modules.Competition.Application.DTOs.Matches;
using Mercurius.Modules.Competition.Application.DTOs.Placements;
using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

public class GetGameDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime PlannedStartTime { get; set; }
    public int AverageGameDurationMinutes { get; set; }
    public int RoundBreakDurationMinutes { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public int? TeamSize { get; set; }
    public string? ImageUrl { get; set; }

    public IEnumerable<GetPlacementDTO> Placements { get; set; } = [];
    public GetGameSponsorPlacementDTO? SponsorPlacement { get; set; }

    public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
    public IEnumerable<PublicTournamentRegistrationDTO> Registrations { get; set; } = [];

    public GetGameDTO()
    {
    }
}

