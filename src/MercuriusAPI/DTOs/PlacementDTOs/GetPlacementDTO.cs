using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.Modules.Teams.DTOs;
using Mercurius.LAN.API.Models;
using ApiPublicUserDTO = Mercurius.LAN.API.DTOs.UserDTOs.PublicUserDTO;

namespace Mercurius.LAN.API.DTOs.PlacementDTOs;

public class GetPlacementDTO
{
    public int Place { get; set; }
    public IEnumerable<ApiPublicUserDTO> Users { get; set; } = [];
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
                Users = placement.Users.Select(user => new ApiPublicUserDTO(user)).ToList();
                break;
            case ParticipationMode.Team:
                Teams = placement.Teams.Select(team => new GetTeamDTO(team)).ToList();
                break;
        }
    }
}

