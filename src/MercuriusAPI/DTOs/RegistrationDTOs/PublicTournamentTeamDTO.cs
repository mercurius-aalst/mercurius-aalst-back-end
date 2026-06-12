using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class PublicTournamentTeamDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
    public string? LogoUrl { get; set; }

    public PublicTournamentTeamDTO()
    {
    }

    public PublicTournamentTeamDTO(Team team)
    {
        Id = team.Id;
        Name = team.Name;
        CaptainUserId = team.CaptainUserId ?? Guid.Empty;
        LogoUrl = team.LogoUrl;
    }
}
