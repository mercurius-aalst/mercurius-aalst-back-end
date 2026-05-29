using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.PlacementDTOs;

public class GetPublicPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<GetPublicUserDTO> Users { get; set; } = [];
    public IEnumerable<GetPublicTeamDTO> Teams { get; set; } = [];

    public GetPublicPlacementDTO()
    {
    }

    public GetPublicPlacementDTO(Placement placement, ParticipationMode participationMode, bool includePlatformIds = false)
    {
        Place = placement.Place;
        switch (participationMode)
        {
            case ParticipationMode.Individual:
                Users = placement.Users.Select(user => new GetPublicUserDTO(user, includePlatformIds)).ToList();
                break;
            case ParticipationMode.Team:
                Teams = placement.Teams.Select(team => new GetPublicTeamDTO(team, includePlatformIds)).ToList();
                break;
        }
    }

    public GetPublicPlacementDTO(GetPlacementDTO placement, bool includePlatformIds = false)
    {
        Place = placement.Place;
        Users = placement.Users.Select(user => new GetPublicUserDTO(user, includePlatformIds)).ToList();
        Teams = placement.Teams.Select(team => new GetPublicTeamDTO(team, includePlatformIds)).ToList();
    }
}
