using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.DTOs.ParticipantDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.DTOs.PlayerDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class GetGameDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public GameStatus Status { get; set; }
    public BracketType BracketType { get; set; }
    public GameFormat Format { get; set; }
    public GameFormat FinalsFormat { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public string? ImageUrl { get; set; }

    public string RegisterFormUrl { get; set; }
    public IEnumerable<GetPlacementDTO> Placements { get; set; } = [];

    public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
    public IEnumerable<GetParticipantDTO> Participants { get; set; } = [];

    public GetGameDTO(Game game)
    {
        Id = game.Id;
        Name = game.Name;
        StartTime = game.StartTime;
        EndTime = game.EndTime;
        Status = game.Status;
        BracketType = game.BracketType;
        Format = game.Format;
        FinalsFormat = game.FinalsFormat;
        ImageUrl = game.ImageUrl;
        RegisterFormUrl = game.RegisterFormUrl;
        ParticipationMode = game.ParticipationMode;
        Placements = game.Placements.Select(p => new GetPlacementDTO(p, game.ParticipationMode.ToParticipantType()));
        Matches = game.Matches.Select(m => new GetMatchDTO(m));
        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                Participants = game.Participants.Select(p => new GetPlayerDTO((Player)p)).ToList();
                break;
            case ParticipationMode.Team:
                Participants = game.Participants.Select(t => new GetTeamDTO((Team)t)).ToList();
                break;
        }
    }

    public GetGameDTO()
    {
    }
}

