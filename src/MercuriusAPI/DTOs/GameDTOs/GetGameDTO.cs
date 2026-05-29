using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class GetGameDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
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
    public string? ImageUrl { get; set; }

    public string RegisterFormUrl { get; set; }
    public IEnumerable<GetPlacementDTO> Placements { get; set; } = [];
    public GetGameSponsorPlacementDTO? SponsorPlacement { get; set; }

    public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
    public IEnumerable<GetUserDTO> Users { get; set; } = [];
    public IEnumerable<GetTeamDTO> Teams { get; set; } = [];

    public GetGameDTO(Game game)
    {
        Id = game.Id;
        Name = game.Name;
        StartTime = game.StartTime;
        EndTime = game.EndTime;
        PlannedStartTime = game.PlannedStartTime;
        AverageGameDurationMinutes = game.AverageGameDurationMinutes;
        RoundBreakDurationMinutes = game.RoundBreakDurationMinutes;
        EstimatedEndTime = game.EstimatedEndTime;
        Status = game.Status;
        BracketType = game.BracketType;
        Format = game.Format;
        FinalsFormat = game.FinalsFormat;
        ImageUrl = game.ImageUrl;
        RegisterFormUrl = game.RegisterFormUrl;
        ParticipationMode = game.ParticipationMode;
        Placements = game.Placements.Select(p => new GetPlacementDTO(p, game.ParticipationMode));
        SponsorPlacement = game.SponsorPlacement is null
            ? null
            : new GetGameSponsorPlacementDTO(game.SponsorPlacement);
        Matches = game.Matches.Select(m => new GetMatchDTO(m));
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                Users = game.RegisteredUsers.Select(user => new GetUserDTO(user)).ToList();
                break;
            case ParticipationMode.Team:
                Teams = game.RegisteredTeams.Select(team => new GetTeamDTO(team)).ToList();
                break;
        }
    }

    public GetGameDTO()
    {
    }
}

