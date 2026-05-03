using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.PlacementDTOs;

public class GetPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<GetUserDTO> Users { get; set; } = [];
    public IEnumerable<GetTeamDTO> Teams { get; set; } = [];

    public GetPlacementDTO()
    {

    }

    public GetPlacementDTO(Placement placement, ParticipationMode participationMode)
    {
        Place = placement.Place;
        switch (participationMode)
        {
            case ParticipationMode.Individual:
                Users = placement.Users.Select(user => new GetUserDTO(user)).ToList();
                break;
            case ParticipationMode.Team:
                Teams = placement.Teams.Select(team => new GetTeamDTO(team)).ToList();
                break;
        }
    }
}

