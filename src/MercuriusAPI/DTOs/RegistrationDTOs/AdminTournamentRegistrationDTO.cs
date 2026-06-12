using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class AdminTournamentRegistrationDTO : TournamentRegistrationDTO
{
    public AdminTournamentRegistrationDTO()
    {
    }

    public AdminTournamentRegistrationDTO(TournamentRegistration registration)
        : base(registration)
    {
    }
}
