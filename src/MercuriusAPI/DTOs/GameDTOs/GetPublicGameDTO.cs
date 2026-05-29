using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class GetPublicGameDTO
{
    public Guid Id { get; set; }
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
    public IEnumerable<GetPublicPlacementDTO> Placements { get; set; } = [];
    public GetGameSponsorPlacementDTO? SponsorPlacement { get; set; }

    public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
    public IEnumerable<GetPublicUserDTO> Users { get; set; } = [];
    public IEnumerable<GetPublicTeamDTO> Teams { get; set; } = [];

    public GetPublicGameDTO()
    {
    }

    public GetPublicGameDTO(Game game, bool includePlatformIds = false)
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
        Placements = game.Placements
            .Select(placement => new GetPublicPlacementDTO(placement, game.ParticipationMode, includePlatformIds))
            .ToList();
        SponsorPlacement = game.SponsorPlacement is null
            ? null
            : new GetGameSponsorPlacementDTO(game.SponsorPlacement);
        Matches = game.Matches.Select(match => new GetMatchDTO(match)).ToList();

        switch (ParticipationMode)
        {
            case ParticipationMode.Individual:
                Users = game.RegisteredUsers.Select(user => new GetPublicUserDTO(user, includePlatformIds)).ToList();
                break;
            case ParticipationMode.Team:
                Teams = game.RegisteredTeams.Select(team => new GetPublicTeamDTO(team, includePlatformIds)).ToList();
                break;
        }
    }

    public GetPublicGameDTO(GetGameDTO game, bool includePlatformIds = false)
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
        Placements = game.Placements
            .Select(placement => new GetPublicPlacementDTO(placement, includePlatformIds))
            .ToList();
        SponsorPlacement = game.SponsorPlacement;
        Matches = game.Matches.ToList();
        Users = game.Users.Select(user => new GetPublicUserDTO(user, includePlatformIds)).ToList();
        Teams = game.Teams.Select(team => new GetPublicTeamDTO(team, includePlatformIds)).ToList();
    }
}
